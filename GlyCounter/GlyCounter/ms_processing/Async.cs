using Nova.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static GlyCounter.SpectrumInfo;

namespace GlyCounter
{
    /// <summary>
    /// Generic async processor for mass spectrometry raw files supporting multiple formats.
    /// Consolidates MzML and TIMS-TOF processing into a single extensible pipeline.
    /// </summary>
    public sealed class SpectrumProcessor<T> where T : SpectrumInfo
    {
        private readonly IRawFileReader<T> _reader;
        private readonly object _ionCountsLock = new object();

        private readonly record struct WriteMessage(string OxoLine, string PeakDepthLine, string? PeriscopeLine);

        public SpectrumProcessor(IRawFileReader<T> reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <summary>
        /// Processes a raw file asynchronously using a producer-consumer pattern with multiple workers.
        /// </summary>
        public async Task<(GlyCounterSettings, RawFileInfo)> ProcessRawFileAsync(
            string fileName,
            GlyCounterSettings glySettings,
            RawFileInfo rawFileInfo,
            StreamWriter outputOxo,
            StreamWriter outputPeakDepth,
            StreamWriter? outputPeriscope = null,
            IProgress<DateTime>? progress = null,
            CancellationToken cancellationToken = default)
        {

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (glySettings is null)
                throw new ArgumentNullException(nameof(glySettings));
            if (rawFileInfo is null)
                throw new ArgumentNullException(nameof(rawFileInfo));
            if (outputOxo is null)
                throw new ArgumentNullException(nameof(outputOxo));
            if (outputPeakDepth is null)
                throw new ArgumentNullException(nameof(outputPeakDepth));

            // Configure parallelism and bounded memory
            int workerCount = Math.Max(1, Environment.ProcessorCount - 1);
            int boundedCapacity = Math.Max(4, workerCount * 4);

            var spectraChannel = Channel.CreateBounded<T>(new BoundedChannelOptions(boundedCapacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });

            var writeChannel = Channel.CreateUnbounded<WriteMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            // Per-worker accumulators to avoid frequent locking
            var workerStats = new List<RawFileInfo>();
            var workerTasks = new List<Task>();

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cts.Token;

            Exception? producerException = null;
            Exception? workerException = null;
            Exception? writerException = null;

            try
            {
                // Start writer task
                var writerTask = StartWriterTask(writeChannel, outputOxo, outputPeakDepth, outputPeriscope,
                    glySettings, token, (ex) =>
                    {
                        writerException = ex;
                        cts.Cancel();
                    });

                // Start producer task
                var producerTask = StartProducerTask(spectraChannel, fileName, token, (ex) =>
                {
                    producerException = ex;
                    spectraChannel.Writer.TryComplete(ex);
                    cts.Cancel();
                });

                // Start worker tasks
                for (int w = 0; w < workerCount; w++)
                {
                    var localStats = new RawFileInfo();
                    workerStats.Add(localStats);

                    var worker = StartWorkerTask(spectraChannel, writeChannel, localStats, glySettings,
                        progress, token, (ex) =>
                        {
                            workerException = ex;
                            spectraChannel.Reader.Completion.ContinueWith(_ => { }, TaskScheduler.Default);
                            cts.Cancel();
                        });

                    workerTasks.Add(worker);
                }

                // Complete writer when all workers finish
                var finishTask = CompleteWriterWhenDoneAsync(workerTasks, writeChannel, token);

                // Wait for all tasks
                await Task.WhenAll(producerTask, finishTask, writerTask).ConfigureAwait(false);

                // Merge per-worker stats
                MergeWorkerStats(rawFileInfo, workerStats);

                return (glySettings, rawFileInfo);
            }
            catch (OperationCanceledException)
            {
                if (producerException != null)
                    throw producerException;
                if (workerException != null)
                    throw workerException;
                if (writerException != null)
                    throw writerException;
                throw;
            }
            catch (Exception)
            {
                // Prefer to throw original exceptions if present
                if (producerException != null)
                    throw producerException;
                if (workerException != null)
                    throw workerException;
                if (writerException != null)
                    throw writerException;
                throw;
            }
            finally
            {
                cts.Dispose();
            }
        }

        private Task StartWriterTask(
            Channel<WriteMessage> writeChannel,
            StreamWriter outputOxo,
            StreamWriter outputPeakDepth,
            StreamWriter? outputPeriscope,
            GlyCounterSettings glySettings,
            CancellationToken token,
            Action<Exception> onError)
        {
            return Task.Run(async () =>
            {
                try
                {
                    string oxoHeader = string.Concat(glySettings.oxoniumIonHashSet.Select(o => o.description + "\t")) +
                                       FileHeaders.LikelyGlycoHeader;
                    string peakDepthHeader = string.Concat(glySettings.oxoniumIonHashSet.Select(o => o.description + "\t")) +
                                             FileHeaders.LikelyGlycoHeader;

                    await outputOxo.WriteLineAsync(oxoHeader).ConfigureAwait(false);
                    await outputPeakDepth.WriteLineAsync(peakDepthHeader).ConfigureAwait(false);

                    await foreach (var msg in writeChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                    {
                        if (msg.OxoLine is not null)
                            await outputOxo.WriteLineAsync(msg.OxoLine).ConfigureAwait(false);
                        if (msg.PeakDepthLine is not null)
                            await outputPeakDepth.WriteLineAsync(msg.PeakDepthLine).ConfigureAwait(false);
                        if (msg.PeriscopeLine is not null && outputPeriscope != null)
                            await outputPeriscope.WriteLineAsync(msg.PeriscopeLine).ConfigureAwait(false);
                    }

                    await outputOxo.FlushAsync().ConfigureAwait(false);
                    await outputPeakDepth.FlushAsync().ConfigureAwait(false);
                    if (outputPeriscope != null)
                        await outputPeriscope.FlushAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    onError(ex);
                    throw;
                }
            }, token);
        }

        private Task StartProducerTask(
            Channel<T> spectraChannel,
            string fileName,
            CancellationToken token,
            Action<Exception> onError)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var writer = spectraChannel.Writer;
                    await foreach (var spectrum in _reader.ReadSpectraAsync(fileName, token).ConfigureAwait(false))
                    {
                        token.ThrowIfCancellationRequested();
                        await writer.WriteAsync(spectrum, token).ConfigureAwait(false);
                    }
                    writer.Complete();
                }
                catch (Exception ex)
                {
                    onError(ex);
                    throw;
                }
            }, token);
        }

        private Task StartWorkerTask(
            Channel<T> spectraChannel,
            Channel<WriteMessage> writeChannel,
            RawFileInfo localStats,
            GlyCounterSettings glySettings,
            IProgress<DateTime>? progress,
            CancellationToken token,
            Action<Exception> onError)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await foreach (var spectrum in spectraChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            // Use the dedicated oxonium processing method
                            var processResult = GetIons.GetOxoniumIons(
                                spectrum, glySettings, localStats, _ionCountsLock);

                            if (processResult.ContainsIons)
                            {
                                // Build output lines
                                var oxoLine = BuildOxoLine(spectrum, processResult);
                                var peakDepthLine = BuildPeakDepthLine(spectrum, processResult);
                                var periscopeLine = BuildPeriscopeLine(spectrum, processResult);

                                await writeChannel.Writer.WriteAsync(
                                    new WriteMessage(oxoLine, peakDepthLine, periscopeLine), token)
                                    .ConfigureAwait(false);
                            }
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            Debug.WriteLine($"IndexOutOfRangeException on spectrum {spectrum.ScanNumber}: {ex}");
                            // Optionally log to file for debugging
                            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "glycounter_processing_errors.log");
                            try
                            {
                                System.IO.File.AppendAllText(logPath,
                                    $"{DateTime.Now:O}\tScan {spectrum.ScanNumber}: {ex.StackTrace}\n");
                            }
                            catch { }
                            continue; // Skip this spectrum and continue processing
                        }

                        try { progress?.Report(DateTime.Now); }
                        catch { /* ignore progress reporting errors */ }
                    }
                }
                catch (Exception ex)
                {
                    onError(ex);
                    throw;
                }
            }, token);
        }

        private static Task CompleteWriterWhenDoneAsync(
            List<Task> workerTasks,
            Channel<WriteMessage> writeChannel,
            CancellationToken token)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(workerTasks).ConfigureAwait(false);
                    writeChannel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    writeChannel.Writer.TryComplete(ex);
                    throw;
                }
            }, token);
        }

        private static string BuildOxoLine(T spectrum, GetIons.OxoniumProcessResult result)
        {
            var sb = new StringBuilder();
            sb.Append(spectrum.ScanNumber).Append('\t')
                .Append(spectrum.RetentionTime).Append('\t')
                .Append(spectrum.MsLevel).Append('\t')
                .Append(spectrum.PrecursorScanNumber).Append('\t')
                .Append(spectrum.PrecursorMz).Append('\t')
                .Append(spectrum.Charge).Append('\t')
                .Append(spectrum.DissociationMethod).Append('\t')
                .Append(spectrum.CollisionEnergy).Append('\t')
                .Append(spectrum.TotalIonCurrent).Append('\t')
                .Append(spectrum.BasePeakIntensity).Append('\t')
                .Append(spectrum.ScanInjTime).Append('\t')
                .Append(result.NumberOfOxoIons).Append('\t')
                .Append(result.TotalOxoSignal).Append('\t');

            if (spectrum is TimsSpectrumInfo timsSpectrum)
            {
                sb.Append(timsSpectrum.IonMobility).Append('\t');
            }

            sb.Append(result.OxoIntensitiesString);
            sb.Append(result.OxoSummary);

            return sb.ToString();
        }

        private static string BuildPeakDepthLine(T spectrum, GetIons.OxoniumProcessResult result)
        {
            var sb = new StringBuilder();
            sb.Append(spectrum.ScanNumber).Append('\t')
                .Append(spectrum.RetentionTime).Append('\t')
                .Append(spectrum.MsLevel).Append('\t')
                .Append(spectrum.PrecursorScanNumber).Append('\t')
                .Append(spectrum.PrecursorMz).Append('\t')
                .Append(spectrum.Charge).Append('\t')
                .Append(spectrum.DissociationMethod).Append('\t')
                .Append(spectrum.CollisionEnergy).Append('\t')
                .Append(spectrum.TotalIonCurrent).Append('\t')
                .Append(spectrum.BasePeakIntensity).Append('\t')
                .Append(spectrum.ScanInjTime).Append('\t')
                .Append(result.NumberOfOxoIons).Append('\t')
                .Append(result.TotalOxoSignal).Append('\t');

            if (spectrum is TimsSpectrumInfo timsSpectrum)
            {
                sb.Append(timsSpectrum.IonMobility).Append('\t');
            }

            sb.Append(result.PeakDepthsString);
            sb.Append(result.OxoSummary);

            return sb.ToString();
        }

        private static string? BuildPeriscopeLine(T spectrum, GetIons.OxoniumProcessResult result)
        {
            if (result.OxoniumIonFoundPeaks.Count == 0)
                return null;

            string peakString = string.Join("; ", result.OxoniumIonFoundPeaks);
            string errorString = string.Join("; ", result.OxoniumIonFoundMassErrors.Select(e => e.ToString("F6")));
            return $"{spectrum.ScanNumber}\t{peakString}\t{errorString}\t";
        }

        private static void MergeWorkerStats(RawFileInfo target, List<RawFileInfo> workers)
        {
            foreach (var w in workers)
            {
                target.numberOfMS2scans += w.numberOfMS2scans;
                target.numberOfHCDscans += w.numberOfHCDscans;
                target.numberOfETDscans += w.numberOfETDscans;
                target.numberOfUVPDscans += w.numberOfUVPDscans;

                target.numberOfMS2scansWithOxo_1 += w.numberOfMS2scansWithOxo_1;
                target.numberOfMS2scansWithOxo_2 += w.numberOfMS2scansWithOxo_2;
                target.numberOfMS2scansWithOxo_3 += w.numberOfMS2scansWithOxo_3;
                target.numberOfMS2scansWithOxo_4 += w.numberOfMS2scansWithOxo_4;
                target.numberOfMS2scansWithOxo_5plus += w.numberOfMS2scansWithOxo_5plus;

                target.numberOfMS2scansWithOxo_1_hcd += w.numberOfMS2scansWithOxo_1_hcd;
                target.numberOfMS2scansWithOxo_2_hcd += w.numberOfMS2scansWithOxo_2_hcd;
                target.numberOfMS2scansWithOxo_3_hcd += w.numberOfMS2scansWithOxo_3_hcd;
                target.numberOfMS2scansWithOxo_4_hcd += w.numberOfMS2scansWithOxo_4_hcd;
                target.numberOfMS2scansWithOxo_5plus_hcd += w.numberOfMS2scansWithOxo_5plus_hcd;

                target.numberOfMS2scansWithOxo_1_etd += w.numberOfMS2scansWithOxo_1_etd;
                target.numberOfMS2scansWithOxo_2_etd += w.numberOfMS2scansWithOxo_2_etd;
                target.numberOfMS2scansWithOxo_3_etd += w.numberOfMS2scansWithOxo_3_etd;
                target.numberOfMS2scansWithOxo_4_etd += w.numberOfMS2scansWithOxo_4_etd;
                target.numberOfMS2scansWithOxo_5plus_etd += w.numberOfMS2scansWithOxo_5plus_etd;

                target.numberOfMS2scansWithOxo_1_uvpd += w.numberOfMS2scansWithOxo_1_uvpd;
                target.numberOfMS2scansWithOxo_2_uvpd += w.numberOfMS2scansWithOxo_2_uvpd;
                target.numberOfMS2scansWithOxo_3_uvpd += w.numberOfMS2scansWithOxo_3_uvpd;
                target.numberOfMS2scansWithOxo_4_uvpd += w.numberOfMS2scansWithOxo_4_uvpd;
                target.numberOfMS2scansWithOxo_5plus_uvpd += w.numberOfMS2scansWithOxo_5plus_uvpd;

                target.numberScansCountedLikelyGlyco_hcd += w.numberScansCountedLikelyGlyco_hcd;
                target.numberScansCountedLikelyGlyco_etd += w.numberScansCountedLikelyGlyco_etd;
                target.numberScansCountedLikelyGlyco_uvpd += w.numberScansCountedLikelyGlyco_uvpd;

                if (w.likelyGlycoSpectrum)
                    target.likelyGlycoSpectrum = true;
            }
        }
    }
}