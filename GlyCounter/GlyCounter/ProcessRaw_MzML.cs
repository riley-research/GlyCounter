using GlyCounter;
using Nova.Data;
using Nova.Io.Read;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace GlyCounter
{
    public class ProcessRaw_MzML
    {
        private readonly record struct WriteMessage(string OxoLine, string PeakDepthLine, string? PeriscopeLine);

        public static async Task<(GlyCounterSettings, RawFileInfo)> processRaw_MzML(string fileName,
            GlyCounterSettings glySettings, RawFileInfo rawFileInfo, StreamWriter outputOxo,
            StreamWriter outputPeakDepth, StreamWriter? outputPeriscope, IProgress<DateTime>? progress = null)
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
                    string oxoHeader = string.Concat(glySettings.oxoniumIonHashSet.Select(o => o.description + "\t")) +
                                       FileHeaders.LikelyGlycoHeader;
                    string peakDepthHeader =
                        string.Concat(glySettings.oxoniumIonHashSet.Select(o => o.description + "\t")) +
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
                        await foreach (var spectrum in spectraChannel.Reader.ReadAllAsync(token)
                                           .ConfigureAwait(false))
                        {
                            if (spectrum.TotalIonCurrent == null || spectrum.TotalIonCurrent == 0) continue;

                            int numberOfOxoIons = 0;
                            double totalOxoSignal = 0;
                            bool test204 = false;
                            bool test163 = false;
                            int countOxoWithinPeakDepthThreshold = 0;
                            var dissociationMethod = Fragmentation.Type.Unknown;
                            var peakDepthThreshold = 0.0;
                            List<double> oxoniumIonFoundPeaks = new List<double>();
                            List<double> oxoniumIonFoundMassErrors = new List<double>();
                            double parentScan = spectrum.PrecursorMasterScanNumber;
                            double precursormz = 0;
                            if (spectrum.Precursors != null && spectrum.Precursors.Count > 0 &&
                                spectrum.Precursors[0] != null)
                            {
                                precursormz = spectrum.Precursors[0].IsolationMz;
                            }

                            double scanTIC = spectrum.TotalIonCurrent;
                            double scanInjTime = spectrum.IonInjectionTime;
                            double retentionTime = spectrum.RetentionTime;
                            string peakString = "";

                            bool IT = spectrum.Analyzer.Contains("ITMS");

                            //custom ms levels
                            if (!glySettings.ignoreMSLevel) //if ignore then no need to check ms level
                            {
                                var levels = GlyCounterSettings.GetLevelsList(glySettings);
                                if (!levels.Contains(spectrum.MsLevel))
                                    continue;
                            }

                            //figure out dissociation type
                            (dissociationMethod, localStats.nce) = Fragmentation.GetFragmentationType(thermo, spectrum);
                            string fragmentationType = dissociationMethod.ToString();


                            switch (dissociationMethod)
                            {
                                case Fragmentation.Type.HCD:
                                    localStats.numberOfHCDscans++;
                                    peakDepthThreshold = glySettings.peakDepthThreshold_hcd;
                                    break;
                                case Fragmentation.Type.ETD:
                                    localStats.numberOfETDscans++;
                                    peakDepthThreshold = glySettings.peakDepthThreshold_etd;
                                    break;
                                case Fragmentation.Type.UVPD:
                                    localStats.numberOfUVPDscans++;
                                    peakDepthThreshold = glySettings.peakDepthThreshold_uvpd;
                                    break;
                                case Fragmentation.Type.Unknown:
                                    break;
                            }

                            localStats.numberOfMS2scans++; // count per-worker scans

                            double basePeak = spectrum.BasePeakIntensity;
                            Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                            if (basePeak > 0)
                            {
                                PeakProcessing.RankOrderPeaks(sortedPeakDepths, spectrum);

                                var localOxonia = glySettings.oxoniumIonHashSet
                                    .Select(o => new { TheoMZ = o.theoMZ, Description = o.description })
                                    .ToList();

                                string oxoIonHeader = "";
                                foreach (var oxoIon in localOxonia)
                                {
                                    SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, oxoIon.TheoMZ,
                                        glySettings.usingda, glySettings.tol, IT);

                                    if (peak.Equals(new SpecDataPointEx()) || peak.Intensity <= 0) continue;

                                    //this switch checks for the negative cases and continues to the next oxonium ion if any of the conditions are met
                                    switch (IT)
                                    {
                                        case false when thermo && (peak.Intensity / peak.Noise) < glySettings.SNthreshold: //orbitrap below S/N threshold
                                        case true or false when peak.Intensity < glySettings.intensityThreshold: //ion trap or mzml below intensity threshold
                                            continue;
                                    }

                                    double measuredMz = peak.Mz;
                                    double intensity = peak.Intensity;
                                    int peakDepth = sortedPeakDepths[peak.Intensity];

                                    numberOfOxoIons++;
                                    totalOxoSignal += intensity;

                                    if (Math.Abs(oxoIon.TheoMZ - 163.0601) < 0.0001 && peakDepth <= peakDepthThreshold)
                                        test163 = true;
                                    if (Math.Abs(oxoIon.TheoMZ - 204.0867) < 0.0001 && peakDepth <= peakDepthThreshold)
                                        test204 = true;

                                    oxoniumIonFoundPeaks.Add(oxoIon.TheoMZ);
                                    var massError = measuredMz - oxoIon.TheoMZ;
                                    oxoniumIonFoundMassErrors.Add(massError);

                                    // --- THREAD-SAFE increment of shared oxonium ion counters ---
                                    // Update the shared glySettings oxonium ion counts so the summary reflects per-ion totals.
                                    lock (ionCountsLock)
                                    {
                                        // find the matching OxoniumIon instance in the shared set
                                        var sharedIon = glySettings.oxoniumIonHashSet.FirstOrDefault(i =>
                                            Math.Abs(i.theoMZ - oxoIon.TheoMZ) < 1e-6);
                                        if (sharedIon != null)
                                        {
                                            switch (dissociationMethod)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    sharedIon.hcdCount++;
                                                    break;
                                                case Fragmentation.Type.ETD:
                                                    sharedIon.etdCount++;
                                                    break;
                                                case Fragmentation.Type.UVPD:
                                                    sharedIon.uvpdCount++;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }

                            // only produce output if oxonium ions found
                            if (numberOfOxoIons > 0)
                            {
                                switch (numberOfOxoIons)
                                {
                                    // update per-worker counters
                                    case 1:
                                        {
                                            localStats.numberOfMS2scansWithOxo_1++;
                                            switch (dissociationMethod)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_1_hcd++;
                                                    break;
                                                case Fragmentation.Type.ETD:
                                                    localStats.numberOfMS2scansWithOxo_1_etd++;
                                                    break;
                                                case Fragmentation.Type.UVPD:
                                                    localStats.numberOfMS2scansWithOxo_1_uvpd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    case 2:
                                        {
                                            localStats.numberOfMS2scansWithOxo_2++;
                                            switch (dissociationMethod)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_2_hcd++;
                                                    break;
                                                case Fragmentation.Type.ETD:
                                                    localStats.numberOfMS2scansWithOxo_2_etd++;
                                                    break;
                                                case Fragmentation.Type.UVPD:
                                                    localStats.numberOfMS2scansWithOxo_2_uvpd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    case 3:
                                        {
                                            localStats.numberOfMS2scansWithOxo_3++;
                                            switch (dissociationMethod)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_3_hcd++;
                                                    break;
                                                case Fragmentation.Type.ETD:
                                                    localStats.numberOfMS2scansWithOxo_3_etd++;
                                                    break;
                                                case Fragmentation.Type.UVPD:
                                                    localStats.numberOfMS2scansWithOxo_3_uvpd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    case 4:
                                        {
                                            localStats.numberOfMS2scansWithOxo_4++;
                                            switch (dissociationMethod)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_4_hcd++;
                                                    break;
                                                case Fragmentation.Type.ETD:
                                                    localStats.numberOfMS2scansWithOxo_4_etd++;
                                                    break;
                                                case Fragmentation.Type.UVPD:
                                                    localStats.numberOfMS2scansWithOxo_4_uvpd++;
                                                    break;
                                            }

                                            break;
                                        }
                                    // >4
                                    default:
                                        {
                                            localStats.numberOfMS2scansWithOxo_5plus++;
                                            switch (dissociationMethod)
                                            {
                                                case Fragmentation.Type.HCD:
                                                    localStats.numberOfMS2scansWithOxo_5plus_hcd++;
                                                    break;
                                                case Fragmentation.Type.ETD:
                                                    localStats.numberOfMS2scansWithOxo_5plus_etd++;
                                                    break;
                                                case Fragmentation.Type.UVPD:
                                                    localStats.numberOfMS2scansWithOxo_5plus_uvpd++;
                                                    break;
                                            }

                                            break;
                                        }
                                }

                                foreach (double theoMZ in oxoniumIonFoundPeaks)
                                    peakString = peakString + theoMZ.ToString() + "; ";
                                string errorString = new string("");
                                foreach (double error in oxoniumIonFoundMassErrors)
                                    errorString = errorString + error.ToString("F6") + "; ";

                                //if ions are not checked don't rely on them to change likelyglyco
                                if (!glySettings.using204) test204 = true;
                                if (!glySettings.using163) test163 = false;

                                var halfTotal = glySettings.oxoniumIonHashSet.Count / 2;

                                localStats.numRequiredIons = glySettings.oxoniumIonHashSet.Count switch
                                {
                                    < 6 => 4,
                                    >= 6 and <= 15 when test163 && halfTotal > 5 => 5,
                                    >= 6 and <= 15 => halfTotal,
                                    > 15 when test163 => 5,
                                    > 15 => 8,
                                };

                                double oxoTICfraction = totalOxoSignal / scanTIC;

                                double oxoCountRequirement = dissociationMethod switch
                                {
                                    Fragmentation.Type.HCD => glySettings.oxoCountRequirement_hcd_user > 0
                                        ? glySettings.oxoCountRequirement_hcd_user
                                        : localStats.numRequiredIons,
                                    Fragmentation.Type.ETD => glySettings.oxoCountRequirement_etd_user > 0
                                        ? glySettings.oxoCountRequirement_etd_user
                                        : localStats.numRequiredIons / 2,
                                    Fragmentation.Type.UVPD => glySettings.oxoCountRequirement_uvpd_user > 0
                                        ? glySettings.oxoCountRequirement_uvpd_user
                                        : localStats.numRequiredIons
                                };

                                // Determine peak depths and countWithin
                                int countWithin = 0; // number of oxonium ions in the required peak depth for likelyglyco
                                var sbOxo = new StringBuilder();
                                var sbPeakDepth = new StringBuilder();

                                foreach (var oxoIon in glySettings.oxoniumIonHashSet)
                                {
                                    SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, oxoIon.theoMZ,
                                        glySettings.usingda, glySettings.tol, thermo, IT);
                                    double intensity = 0;
                                    if (peak.Equals(new SpecDataPointEx()) || peak.Intensity <= 0)
                                    {
                                        sbPeakDepth.Append("NotFound\t");
                                        sbOxo.Append("0\t");
                                        continue;
                                    }

                                    if (!IT && thermo)
                                    {
                                        if ((peak.Intensity / peak.Noise) > glySettings.SNthreshold) 
                                            intensity = peak.Intensity;
                                    }
                                    else
                                        if (peak.Intensity > glySettings.intensityThreshold) 
                                            intensity = peak.Intensity;

                                    sbOxo.Append(intensity).Append('\t');

                                    int pd = sortedPeakDepths.ContainsKey(peak.Intensity)
                                        ? sortedPeakDepths[peak.Intensity]
                                        : glySettings.arbitraryPeakDepthIfNotFound;
                                    sbPeakDepth.Append(pd).Append('\t');

                                    switch (dissociationMethod)
                                    {
                                        case Fragmentation.Type.HCD when pd <= glySettings.peakDepthThreshold_hcd:
                                        case Fragmentation.Type.ETD when pd <= glySettings.peakDepthThreshold_etd:
                                        case Fragmentation.Type.UVPD when pd <= glySettings.peakDepthThreshold_uvpd:
                                            countWithin++;
                                            break;
                                    }
                                    
                                }

                                bool isLikelyGlyco = false;
                                if (test204)
                                {
                                    switch (dissociationMethod)
                                    {
                                        case Fragmentation.Type.HCD when countWithin >= oxoCountRequirement && oxoTICfraction >= glySettings.oxoTICfractionThreshold_hcd:
                                            isLikelyGlyco = true;
                                            localStats.numberScansCountedLikelyGlyco_hcd++;
                                            break;
                                        case Fragmentation.Type.ETD when countWithin >= oxoCountRequirement && oxoTICfraction >= glySettings.oxoTICfractionThreshold_etd:
                                            isLikelyGlyco = true;
                                            localStats.numberScansCountedLikelyGlyco_etd++;
                                            break;
                                        case Fragmentation.Type.UVPD when countWithin >= oxoCountRequirement && oxoTICfraction >= glySettings.oxoTICfractionThreshold_uvpd:
                                            isLikelyGlyco = true;
                                            localStats.numberScansCountedLikelyGlyco_uvpd++;
                                            break;
                                    }
                                }
                                
                                if (isLikelyGlyco) localStats.likelyGlycoSpectrum = true;

                                string oxoSummary =
                                    $"{countWithin}\t{oxoCountRequirement}\t{oxoTICfraction}\t{isLikelyGlyco}";

                                // Prepare lines (tab-separated) for writer
                                var oxoLine = new StringBuilder();
                                oxoLine.Append(spectrum.ScanNumber).Append('\t')
                                    .Append(retentionTime).Append('\t')
                                    .Append(spectrum.MsLevel).Append('\t')
                                    .Append(precursormz).Append('\t')
                                    .Append(spectrum.Precursors[0].Charge).Append('\t')
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
                                peakDepthLine.Append(spectrum.ScanNumber).Append('\t')
                                    .Append(retentionTime).Append('\t')
                                    .Append(spectrum.MsLevel).Append('\t')
                                    .Append(precursormz).Append('\t')
                                    .Append(spectrum.Precursors[0].Charge).Append('\t')
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
                                    periscopeLine = $"{spectrum.ScanNumber}\t{peakString}\t{errorString}\t";

                                // enqueue write message
                                await writeChannel.Writer
                                    .WriteAsync(
                                        new WriteMessage(oxoLine.ToString(), peakDepthLine.ToString(),
                                            periscopeLine),
                                        token).ConfigureAwait(false);
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