using CSMSL.Proteomics;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Nova.Data;
using Nova.Io.Read;
using PSI_Interface.MSData;

namespace GlyCounter
{
    public class ProcessTimsTOF
    {
        // Message for writer task
        private readonly record struct WriteMessage(string OxoLine, string PeakDepthLine, string? periscopeLine);

        public static async Task<(GlyCounterSettings, RawFileInfo)> processTimsTOFAsync(
            string fileName,
            GlyCounterSettings glySettings,
            RawFileInfo rawFileInfo,
            StreamWriter outputOxo,
            StreamWriter outputPeakDepth,
            StreamWriter? outputPeriscope,
            IProgress<DateTime>? progress = null)
        {
            // Configure parallelism and bounded memory
            int workerCount = Math.Max(1, Environment.ProcessorCount - 1);
            int boundedCapacity = Math.Max(4, workerCount * 4);

            var spectraChannel = Channel.CreateBounded<RawSpectrum>(new BoundedChannelOptions(boundedCapacity)
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

            // Cancellation support local to this method (optional)
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            Exception? producerException = null;
            Exception? workerException = null;
            Exception? writerException = null;

            // Lock used when updating shared oxonium-ion counts on glySettings
            var ionCountsLock = new object();

            // Start writer task: serializes writes to the StreamWriters
            var writerTask = Task.Run(async () =>
            {
                try
                {
                    // write header once
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
                        if (msg.periscopeLine is not null && outputPeriscope != null)
                            await outputPeriscope.WriteLineAsync(msg.periscopeLine).ConfigureAwait(false);
                    }
                    await outputOxo.FlushAsync().ConfigureAwait(false);
                    await outputPeakDepth.FlushAsync().ConfigureAwait(false);
                    if (outputPeriscope != null) await outputPeriscope.FlushAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    writerException = ex;
                    // propagate by cancelling producers/workers
                    cts.Cancel();
                    throw;
                }
            }, token);

            // Producer: read spectra lazily and write into the spectra channel
            var producerTask = Task.Run(async () =>
            {
                try
                {
                    var writer = spectraChannel.Writer;
                    foreach (RawSpectrum spectrum in Native.ReadMsnSpectraLazy(fileName))
                    {
                        // respect cancellation
                        token.ThrowIfCancellationRequested();
                        await writer.WriteAsync(spectrum, token).ConfigureAwait(false);
                    }
                    writer.Complete();
                }
                catch (Exception ex)
                {
                    producerException = ex;
                    spectraChannel.Writer.TryComplete(ex);
                    // also stop other tasks
                    cts.Cancel();
                }
            }, token);

            // Worker factory
            for (int w = 0; w < workerCount; w++)
            {
                var localStats = new RawFileInfo();
                workerStats.Add(localStats);

                var worker = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var spectrum in spectraChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                        {
                            if (spectrum.intensity == null || spectrum.intensity.Length == 0
                                                           || spectrum.mz == null || spectrum.mz.Length == 0
                                                           || spectrum.precursors == null || spectrum.precursors.Length == 0)
                            {
                                try
                                {
                                    var logPath = Path.Combine(Path.GetTempPath(), "glycounter_bad_spectra.log");
                                    File.AppendAllText(logPath, $"{DateTime.Now:O}\tSkipped spectrum id={spectrum?.id ?? "null"}\n");
                                }
                                catch { /* never let logging throw */ }

                                continue;
                            }

                            // per-spectrum processing (mirrors original logic, using localStats)
                            int numberOfOxoIons = 0;
                            double totalOxoSignal = 0;
                            bool test366 = false;
                            int countOxoWithinPeakDepthThreshold = 0;
                            List<double> oxoniumIonFoundPeaks = new List<double>();
                            List<double> oxoniumIonFoundMassErrors = new List<double>();

                            // treat as HCD, assuming PASEF
                            var dissociationType = Fragmentation.Type.HCD;
                            localStats.numberOfHCDscans++;
                            localStats.numberOfMS2scans++; // count per-worker scans
                            var peakDepthThreshold = glySettings.peakDepthThreshold_hcd;

                            spectrum.peaks = PeakProcessing.ListsToPeaks(spectrum.mz.ToList(), spectrum.intensity.ToList());

                            double basePeak = spectrum.intensity.Length > 0 ? spectrum.intensity.Max() : 0.0;
                            Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                            if (basePeak > 0)
                            {
                                sortedPeakDepths = PeakProcessing.RankOrderPeaks(new Dictionary<double, int>(), spectrum);

                                var localOxonia = glySettings.oxoniumIonHashSet
                                    .Select(o => new
                                    {
                                        TheoMZ = o.theoMZ,
                                        Description = o.description
                                    })
                                    .ToList();

                                foreach (var local in localOxonia)
                                {
                                    SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, local.TheoMZ, glySettings.usingda,
                                        glySettings.tol);

                                    if (!peak.Equals(new SpecDataPointEx()) && peak.Intensity > glySettings.intensityThreshold)
                                    {
                                        double measuredMz = peak.Mz;
                                        double intensity = peak.Intensity;
                                        int peakDepth = sortedPeakDepths[peak.Intensity];

                                        numberOfOxoIons++;
                                        totalOxoSignal += intensity;

                                        if (Math.Abs(local.TheoMZ - 366.1395) < 0.0001 &&
                                            sortedPeakDepths[peak.Intensity] <= peakDepthThreshold)
                                            test366 = true;

                                        oxoniumIonFoundPeaks.Add(local.TheoMZ);
                                        var massError = measuredMz - local.TheoMZ;
                                        oxoniumIonFoundMassErrors.Add(massError);

                                        // --- THREAD-SAFE increment of shared oxonium ion counters ---
                                        // Update the shared glySettings oxonium ion counts so the summary reflects per-ion totals.
                                        lock (ionCountsLock)
                                        {
                                            // find the matching OxoniumIon instance in the shared set
                                            var sharedIon = glySettings.oxoniumIonHashSet.FirstOrDefault(i => Math.Abs(i.theoMZ - local.TheoMZ) < 1e-6);
                                            if (sharedIon != null)
                                            {
                                                switch (dissociationType)
                                                {
                                                    case Fragmentation.Type.HCD:
                                                        sharedIon.hcdCount++;
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // only produce output if oxonium ions found (same as original)
                            if (numberOfOxoIons > 0)
                            {
                                switch (numberOfOxoIons)
                                {
                                    // update per-worker counters (mirrors original increments)
                                    case 1:
                                        {
                                            localStats.numberOfMS2scansWithOxo_1++;
                                            switch (dissociationType)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_1_hcd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    case 2:
                                        {
                                            localStats.numberOfMS2scansWithOxo_2++;
                                            switch (dissociationType)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_2_hcd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    case 3:
                                        {
                                            localStats.numberOfMS2scansWithOxo_3++;
                                            switch (dissociationType)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_3_hcd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    case 4:
                                        {
                                            localStats.numberOfMS2scansWithOxo_4++;
                                            switch (dissociationType)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_4_hcd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    // >4
                                    default:
                                        {
                                            localStats.numberOfMS2scansWithOxo_5plus++;
                                            switch (dissociationType)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_5plus_hcd++;
                                                    break;
                                            }

                                            break;
                                        }
                                }

                                string parentScan = spectrum.precursors[0].spectrum_ref;
                                localStats.nce = Convert.ToDouble(spectrum.collision_energy);
                                double scanTIC = spectrum.intensity.Sum();
                                float? scanInjTime = 0;
                                string fragmentationType = dissociationType.ToString();
                                float? retentionTime = spectrum.scan_start_time;
                                double precursormz = spectrum.precursors[0].mz;
                                string peakString = string.Join("; ", oxoniumIonFoundPeaks);
                                string errorString = string.Join("; ", oxoniumIonFoundMassErrors.Select(e => e.ToString("F6")));

                                var halfTotal = glySettings.oxoniumIonHashSet.Count / 2;

                                localStats.numRequiredIons = glySettings.oxoniumIonHashSet.Count switch
                                {
                                    < 6 => 4,
                                    >= 6 and <= 15 => halfTotal,
                                    > 15 => 8,
                                };

                                double oxoTICfraction = totalOxoSignal / scanTIC;

                                double oxoCountRequirement = 0;

                                switch (dissociationType)
                                {
                                    case Fragmentation.Type.HCD:
                                        oxoCountRequirement = glySettings.oxoCountRequirement_hcd_user > 0
                                            ? glySettings.oxoCountRequirement_hcd_user
                                            : localStats.numRequiredIons;
                                        break;
                                }
                                
                                if (!glySettings.using204)
                                    test366 = true;

                                // Determine peak depths and countWithin (re-checking peaks like original)
                                int countWithin = 0;
                                var sbOxo = new StringBuilder();
                                var sbPeakDepth = new StringBuilder();

                                foreach (var oxoIon in glySettings.oxoniumIonHashSet)
                                {
                                    SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, oxoIon.theoMZ, glySettings.usingda, glySettings.tol);
                                    double intensity = (!peak.Equals(new SpecDataPointEx()) && peak.Intensity > glySettings.intensityThreshold) ? peak.Intensity : 0;
                                    sbOxo.Append(intensity).Append('\t');

                                    if (peak.Equals(new SpecDataPointEx()) || peak.Intensity <= glySettings.intensityThreshold)
                                    {
                                        sbPeakDepth.Append("NotFound\t");
                                    }
                                    else
                                    {
                                        int pd = sortedPeakDepths.ContainsKey(peak.Intensity) ? sortedPeakDepths[peak.Intensity] : glySettings.arbitraryPeakDepthIfNotFound;
                                        sbPeakDepth.Append(pd).Append('\t');

                                        switch (dissociationType)
                                        {
                                            case Fragmentation.Type.HCD when pd <= glySettings.peakDepthThreshold_hcd:
                                                countWithin++;
                                                break;
                                        }
                                    }
                                }

                                bool isLikelyGlyco = false;
                                var requiredTICFraction = 0.0;
                                switch (dissociationType)
                                {
                                    case Fragmentation.Type.HCD:
                                        requiredTICFraction = glySettings.oxoTICfractionThreshold_hcd;
                                        break;
                                }

                                switch (dissociationType)
                                {
                                    case Fragmentation.Type.HCD when countWithin >= oxoCountRequirement && test366 && oxoTICfraction >= glySettings.oxoTICfractionThreshold_hcd:
                                        isLikelyGlyco = true;
                                        localStats.numberScansCountedLikelyGlyco_hcd++;
                                        break;
                                }

                                // persist into per-worker flag (keep true if previously set)
                                if (isLikelyGlyco) localStats.likelyGlycoSpectrum = true;

                                // Final summary columns
                                string oxoSummary = $"{countWithin}\t{oxoCountRequirement}\t{oxoTICfraction}\t{localStats.likelyGlycoSpectrum}";

                                float? im = spectrum.precursors[0].ion_mobility;

                                // Prepare lines (tab-separated) for writer
                                var oxoLine = new StringBuilder();
                                oxoLine.Append(spectrum.id).Append('\t')
                                       .Append(retentionTime).Append('\t')
                                       .Append(spectrum.precursors[0].ion_mobility).Append('\t')
                                       .Append(spectrum.ms_level).Append('\t')
                                       .Append(precursormz).Append('\t')
                                       .Append(spectrum.precursors[0].charge).Append('\t')
                                       .Append(localStats.nce).Append('\t')
                                       .Append(scanTIC).Append('\t')
                                       .Append(basePeak).Append('\t')
                                       .Append(scanInjTime).Append('\t')
                                       .Append(fragmentationType).Append('\t')
                                       .Append(parentScan).Append('\t')
                                       .Append(numberOfOxoIons).Append('\t')
                                       .Append(totalOxoSignal).Append('\t')
                                       .Append(sbOxo.ToString())
                                       .Append(oxoSummary);

                                var peakDepthLine = new StringBuilder();
                                peakDepthLine.Append(spectrum.id).Append('\t')
                                             .Append(retentionTime).Append('\t')
                                             .Append(spectrum.precursors[0].ion_mobility).Append('\t')
                                             .Append(spectrum.ms_level).Append('\t')
                                             .Append(precursormz).Append('\t')
                                             .Append(spectrum.precursors[0].charge).Append('\t')
                                             .Append(localStats.nce).Append('\t')
                                             .Append(scanTIC).Append('\t')
                                             .Append(basePeak).Append('\t')
                                             .Append(scanInjTime).Append('\t')
                                             .Append(fragmentationType).Append('\t')
                                             .Append(parentScan).Append('\t')
                                             .Append(numberOfOxoIons).Append('\t')
                                             .Append(totalOxoSignal).Append('\t')
                                             .Append(sbPeakDepth.ToString())
                                             .Append(oxoSummary);

                                string? periscopeLine = null;
                                if (outputPeriscope != null)
                                    periscopeLine = $"{spectrum.id}\t{peakString}\t{errorString}\t";

                                // enqueue write message
                                await writeChannel.Writer.WriteAsync(new WriteMessage(oxoLine.ToString(), peakDepthLine.ToString(), periscopeLine), token).ConfigureAwait(false);
                            }
                            try { progress?.Report(DateTime.Now); } catch { /* ignore */ }

                        }
                    }
                    catch (Exception ex)
                    {
                        workerException = ex;
                        spectraChannel.Reader.Completion.ContinueWith(_ => { }, TaskScheduler.Default);
                        cts.Cancel();
                        throw;
                    }
                }, token);

                workerTasks.Add(worker);
            }

            // Complete writer when all workers finish
            var finishTask = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(workerTasks).ConfigureAwait(false);
                    writeChannel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    // If workers faulted, surface exception
                    writeChannel.Writer.TryComplete(ex);
                    throw;
                }
            }, token);

            // Wait for producer, workers, writer
            try
            {
                await Task.WhenAll(producerTask, finishTask, writerTask).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Prefer to throw original exceptions if present
                if (producerException != null)
                {
                    throw producerException;
                }
                if (workerException != null)
                {
                    throw workerException;
                }
                if (writerException != null)
                {
                    throw writerException;
                }
                throw;
            }

            // Merge per-worker stats into provided rawFileInfo
            MergeWorkerStatsInto(rawFileInfo, workerStats);

            return (glySettings, rawFileInfo);

            // local helpers
            static void MergeWorkerStatsInto(RawFileInfo target, List<RawFileInfo> workers)
            {
                // sum integer counters
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

                    // booleans: if any worker set likelyGlycoSpectrum true, keep true
                    if (w.likelyGlycoSpectrum) target.likelyGlycoSpectrum = true;

                    // doubles: choose consistent strategy - sum where appropriate or keep existing
                    // nce and halfTotalList are file-level settings; prefer the value already present in target.
                }
            }
        }
    }
}
