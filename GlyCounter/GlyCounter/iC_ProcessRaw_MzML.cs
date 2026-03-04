using Nova.Data;
using Nova.Io.Read;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace GlyCounter
{
    public class iC_ProcessRaw_MzML
    {
        private readonly record struct WriteMessage(string SignalLine, string PeakDepthLine, string? IpsaLine);

        public static async Task<(GlyCounterSettings, RawFileInfo)> processRaw_MzML(string fileName,
            GlyCounterSettings glySettings, iCounterSettings iCsettings,
            RawFileInfo rawFileInfo, StreamWriter outputSignal, StreamWriter outputPeakDepth, StreamWriter? outputIPSA,
            IProgress<DateTime>? progress = null)
        {
            // Configure parallelism and bounded memory
            int workerCount = Math.Max(1, Environment.ProcessorCount - 1);
            int boundedCapacity = Math.Max(4, workerCount * 4);

            var spectraChannel = Channel.CreateBounded<SpectrumEx>(new BoundedChannelOptions(boundedCapacity)
            {
                SingleWriter = true, SingleReader = false, FullMode = BoundedChannelFullMode.Wait
            });

            var writeChannel = Channel.CreateUnbounded<WriteMessage>(new UnboundedChannelOptions
            {
                SingleReader = true, SingleWriter = false
            });

            // Per-worker accumulators to avoid frequent locking
            var workerStats = new List<RawFileInfo>();
            var workerTasks = new List<Task>();

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
                    string oxoHeader = string.Concat(iCsettings._ionHashSet.Select(o => o.description + "\t"));
                    string peakDepthHeader =
                        string.Concat(glySettings.oxoniumIonHashSet.Select(o => o.description + "\t"));

                    await outputSignal.WriteLineAsync(oxoHeader).ConfigureAwait(false);
                    await outputPeakDepth.WriteLineAsync(peakDepthHeader).ConfigureAwait(false);

                    await foreach (var msg in writeChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                    {
                        if (msg.SignalLine is not null)
                            await outputSignal.WriteLineAsync(msg.SignalLine).ConfigureAwait(false);
                        if (msg.PeakDepthLine is not null)
                            await outputPeakDepth.WriteLineAsync(msg.PeakDepthLine).ConfigureAwait(false);
                        if (msg.IpsaLine is not null && outputIPSA != null)
                            await outputIPSA.WriteLineAsync(msg.IpsaLine).ConfigureAwait(false);
                    }

                    await outputSignal.FlushAsync().ConfigureAwait(false);
                    await outputPeakDepth.FlushAsync().ConfigureAwait(false);
                    if (outputIPSA != null) await outputIPSA.FlushAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    writerException = ex;
                    // propagate by cancelling producers/workers
                    cts.Cancel();
                    throw;
                }
            }, token);

            FileReader rawFile = new FileReader(fileName);
            FileReader typeCheck = new FileReader();
            string fileType = typeCheck.CheckFileFormat(fileName).ToString(); //either "ThermoRaw" or "MzML"
            bool thermo = true;
            if (fileType == "MzML")
                thermo = false;

            // Producer: read spectra lazily and write into the spectra channel
            var producerTask = Task.Run(async () =>
            {
                try
                {
                    var writer = spectraChannel.Writer;
                    for (int i = 1; i <= rawFile.LastScan; i++)
                    {
                        // respect cancellation
                        token.ThrowIfCancellationRequested();
                        SpectrumEx spectrum = rawFile.ReadSpectrumEx(scanNumber: i);
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
                            if (spectrum.TotalIonCurrent == null || spectrum.TotalIonCurrent == 0) continue;

                            bool IT = spectrum.Analyzer.Contains("ITMS");
                            int numberOfIons = 0;
                            double totalSignal = 0;
                            bool hcdTrue = false;
                            bool etdTrue = false;
                            bool uvpdTrue = false;
                            List<double> ionFoundPeaks = [];
                            List<double> ionFoundMassErrors = [];


                            //custom ms levels
                            List<int> levels = [];

                            if (glySettings.msLevelLB == glySettings.msLevelUB)
                                levels.Add(Convert.ToInt32(glySettings.msLevelLB));

                            int lowestval;
                            int highestval;

                            if (glySettings.msLevelLB < glySettings.msLevelUB)
                            {
                                lowestval = Convert.ToInt32(glySettings.msLevelLB);
                                highestval = Convert.ToInt32(glySettings.msLevelUB);

                                levels = Enumerable.Range(lowestval, (highestval - lowestval + 1)).ToList();
                            }

                            //if user puts values in backwards for some reason
                            else
                            {
                                lowestval = Convert.ToInt32(glySettings.msLevelUB);
                                highestval = Convert.ToInt32(glySettings.msLevelLB);

                                levels = Enumerable.Range(lowestval, (highestval - lowestval + 1)).ToList();
                            }

                            //if ignore ms levels is checked ignore levels list
                            if (glySettings.ignoreMSLevel)
                                if (!levels.Contains(spectrum.MsLevel))
                                    continue;

                            //figure out dissociation type
                            if (thermo)
                            {
                                //using this order means ethcd will count as etd (since both show in scan filter)
                                if (spectrum.ScanFilter.Contains("etd"))
                                {
                                    localStats.numberOfETDscans++;
                                    etdTrue = true;
                                }
                                else if (spectrum.ScanFilter.Contains("hcd"))
                                {
                                    localStats.numberOfHCDscans++;
                                    hcdTrue = true;
                                }
                                else if (spectrum.ScanFilter.Contains("uvpd") || spectrum.ScanFilter.Contains("ci"))
                                {
                                    localStats.numberOfUVPDscans++;
                                    uvpdTrue = true;
                                }
                            }
                            else
                            {
                                string dt = spectrum.Precursors[0].FramentationMethod.ToString();

                                if (dt.Equals("HCD"))
                                {
                                    localStats.numberOfHCDscans++;
                                    hcdTrue = true;
                                }

                                if (dt.Equals("ETD"))
                                {
                                    localStats.numberOfETDscans++;
                                    etdTrue = true;
                                }

                                if (dt.Equals("CI") || dt.Equals("UVPD"))
                                {
                                    localStats.numberOfUVPDscans++;
                                    uvpdTrue = true;
                                }

                            }

                            localStats.numberOfMS2scans++; // count per-worker scans

                            string ionHeader = "";

                            if (spectrum is { TotalIonCurrent: > 0, BasePeakIntensity: > 0 })
                            {
                                Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();
                                PeakProcessing.RankOrderPeaks(sortedPeakDepths, spectrum);
                                var localIons = iCsettings._ionHashSet
                                    .Select(o => new { TheoMZ = o.theoMZ, Description = o.description })
                                    .ToList();

                                if (thermo)
                                {
                                    string scanFilter = spectrum.ScanFilter ?? "";
                                    string[] hcdHeader = scanFilter.Split('@', StringSplitOptions.RemoveEmptyEntries);
                                    localStats.nce = double.NaN;
                                    if (hcdHeader.Length >= 2)
                                    {
                                        string candidate = hcdHeader.Length >= 3 && hcdHeader[1].Contains("ptr")
                                            ? hcdHeader[2]
                                            : hcdHeader[1];
                                        var m = System.Text.RegularExpressions.Regex.Match(candidate, @"(\d+(\.\d+)?)");
                                        if (m.Success && double.TryParse(m.Value, NumberStyles.Any,
                                                CultureInfo.InvariantCulture, out double parsed))
                                            localStats.nce = parsed;
                                    }
                                }
                                else localStats.nce = spectrum.Precursors[0].CollisionEnergy;

                                foreach (var ion in localIons)
                                {
                                    SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, ion.TheoMZ,
                                        glySettings.usingda, glySettings.tol, thermo, IT);

                                    if (!IT && thermo)
                                    {
                                        if (peak.Equals(new SpecDataPointEx()) || !(peak.Intensity > 0) ||
                                            !((peak.Intensity / peak.Noise) > glySettings.SNthreshold)) continue;

                                        double measuredMz = peak.Mz;
                                        numberOfIons++;
                                        totalSignal += peak.Intensity;

                                        ionFoundPeaks.Add(ion.TheoMZ);
                                        double massError = measuredMz - ion.TheoMZ;
                                        ionFoundMassErrors.Add(massError);

                                        // --- THREAD-SAFE increment of shared oxonium ion counters ---
                                        // Update the shared glySettings oxonium ion counts so the summary reflects per-ion totals.
                                        lock (ionCountsLock)
                                        {
                                            // find the matching OxoniumIon instance in the shared set
                                            var sharedIon = glySettings.oxoniumIonHashSet.FirstOrDefault(i =>
                                                Math.Abs(i.theoMZ - ion.TheoMZ) < 1e-6);
                                            if (sharedIon != null)
                                            {
                                                if (hcdTrue) sharedIon.hcdCount++;
                                                if (etdTrue) sharedIon.etdCount++;
                                                if (uvpdTrue) sharedIon.uvpdCount++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (peak.Equals(new SpecDataPointEx()) ||
                                            !(peak.Intensity > glySettings.intensityThreshold)) continue;

                                        double measuredMz = peak.Mz;
                                        numberOfIons++;
                                        totalSignal += peak.Intensity;

                                        ionFoundPeaks.Add(ion.TheoMZ);
                                        double massError = measuredMz - ion.TheoMZ;
                                        ionFoundMassErrors.Add(massError);

                                        // --- THREAD-SAFE increment of shared oxonium ion counters ---
                                        // Update the shared glySettings oxonium ion counts so the summary reflects per-ion totals.
                                        lock (ionCountsLock)
                                        {
                                            // find the matching OxoniumIon instance in the shared set
                                            var sharedIon = glySettings.oxoniumIonHashSet.FirstOrDefault(i =>
                                                Math.Abs(i.theoMZ - ion.TheoMZ) < 1e-6);
                                            if (sharedIon != null)
                                            {
                                                if (hcdTrue) sharedIon.hcdCount++;
                                                if (etdTrue) sharedIon.etdCount++;
                                                if (uvpdTrue) sharedIon.uvpdCount++;
                                            }
                                        }
                                    }
                                }
                            }

                            if (numberOfIons > 0)
                            {

                                if (localStats.firstSpectrumInFile)
                                {
                                    outputSignal.WriteLine(ionHeader);
                                    outputPeakDepth.WriteLine(ionHeader);
                                    localStats.firstSpectrumInFile = false;
                                }

                                if (numberOfIons == 1)
                                {
                                    localStats.numberOfMS2scansWithOxo_1++;
                                    if (hcdTrue)
                                        localStats.numberOfMS2scansWithOxo_1_hcd++;
                                    if (etdTrue)
                                        localStats.numberOfMS2scansWithOxo_1_etd++;
                                    if (uvpdTrue)
                                        localStats.numberOfMS2scansWithOxo_1_uvpd++;
                                }

                                if (numberOfIons == 2)
                                {
                                    localStats.numberOfMS2scansWithOxo_2++;
                                    if (hcdTrue)
                                        localStats.numberOfMS2scansWithOxo_2_hcd++;
                                    if (etdTrue)
                                        localStats.numberOfMS2scansWithOxo_2_etd++;
                                    if (uvpdTrue)
                                        localStats.numberOfMS2scansWithOxo_2_uvpd++;
                                }

                                if (numberOfIons == 3)
                                {
                                    localStats.numberOfMS2scansWithOxo_3++;
                                    if (hcdTrue)
                                        localStats.numberOfMS2scansWithOxo_3_hcd++;
                                    if (etdTrue)
                                        localStats.numberOfMS2scansWithOxo_3_etd++;
                                    if (uvpdTrue)
                                        localStats.numberOfMS2scansWithOxo_3_uvpd++;
                                }

                                if (numberOfIons == 4)
                                {
                                    localStats.numberOfMS2scansWithOxo_4++;
                                    if (hcdTrue)
                                        localStats.numberOfMS2scansWithOxo_4_hcd++;
                                    if (etdTrue)
                                        localStats.numberOfMS2scansWithOxo_4_etd++;
                                    if (uvpdTrue)
                                        localStats.numberOfMS2scansWithOxo_4_uvpd++;
                                }

                                if (numberOfIons > 4)
                                {
                                    localStats.numberOfMS2scansWithOxo_5plus++;
                                    if (hcdTrue)
                                        localStats.numberOfMS2scansWithOxo_5plus_hcd++;
                                    if (etdTrue)
                                        localStats.numberOfMS2scansWithOxo_5plus_etd++;
                                    if (uvpdTrue)
                                        localStats.numberOfMS2scansWithOxo_5plus_uvpd++;
                                }

                                double parentScan = spectrum.PrecursorMasterScanNumber;
                                double scanTIC = spectrum.TotalIonCurrent;
                                double scanInjTime = spectrum.IonInjectionTime;
                                string fragmentationType = "";
                                if (spectrum.MsLevel == 1) fragmentationType = "MS1";
                                if (hcdTrue) fragmentationType = "HCD";
                                if (etdTrue) fragmentationType = "ETD";
                                if (uvpdTrue) fragmentationType = "UVPD";
                                double retentionTime = spectrum.RetentionTime;
                                double precursormz = double.NaN;
                                if (spectrum.Precursors != null && spectrum.Precursors.Count > 0)
                                {
                                    var precursor = spectrum.Precursors.Last();
                                    precursormz = precursor.IsolationMz;
                                }

                                string peakString = "";
                                foreach (double theoMZ in ionFoundPeaks)
                                    peakString = peakString + theoMZ.ToString() + "; ";
                                string errorString = new string("");
                                foreach (double error in ionFoundMassErrors)
                                    errorString = errorString + error.ToString("F6") + "; ";


                                // Prepare lines (tab-separated) for writer
                                var oxoLine = new StringBuilder();
                                oxoLine.Append(spectrum.ScanNumber).Append('\t')
                                    .Append(spectrum.MsLevel).Append('\t')
                                    .Append(retentionTime).Append('\t')
                                    .Append(precursormz).Append('\t')
                                    .Append(localStats.nce).Append('\t')
                                    .Append(scanTIC).Append('\t')
                                    .Append(totalSignal).Append('\t')
                                    .Append(scanInjTime).Append('\t')
                                    .Append(fragmentationType).Append('\t')
                                    .Append(parentScan).Append('\t')
                                    .Append(numberOfIons).Append('\t')
                                    .Append(totalSignal).Append('\t');

                                var peakDepthLine = new StringBuilder();
                                peakDepthLine.Append(spectrum.ScanNumber).Append('\t')
                                    .Append(spectrum.MsLevel).Append('\t')
                                    .Append(retentionTime).Append('\t')
                                    .Append(precursormz).Append('\t')
                                    .Append(localStats.nce).Append('\t')
                                    .Append(scanTIC).Append('\t')
                                    .Append(totalSignal).Append('\t')
                                    .Append(scanInjTime).Append('\t')
                                    .Append(fragmentationType).Append('\t')
                                    .Append(parentScan).Append('\t')
                                    .Append(numberOfIons).Append('\t')
                                    .Append(totalSignal).Append('\t');

                                string? ipsaLine = null;
                                if (outputIPSA != null)
                                    ipsaLine = $"{spectrum.ScanNumber}\t{peakString}\t{errorString}\t";

                                // enqueue write message
                                await writeChannel.Writer
                                    .WriteAsync(
                                        new WriteMessage(oxoLine.ToString(), peakDepthLine.ToString(), ipsaLine), token)
                                    .ConfigureAwait(false);
                            }

                            try { progress?.Report(DateTime.Now); }
                            catch
                            {
                                /* ignore */
                            }
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
                }
            }
        }
    }
}
