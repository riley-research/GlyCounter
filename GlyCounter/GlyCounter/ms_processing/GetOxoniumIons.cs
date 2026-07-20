using Nova.Data;
using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public static class GetIons
    {
        public class OxoniumProcessResult
        {
            public bool ContainsIons { get; set; }
            public int NumberOfOxoIons { get; set; }
            public double TotalOxoSignal { get; set; }
            public List<double> OxoniumIonFoundPeaks { get; set; } = new();
            public List<double> OxoniumIonFoundMassErrors { get; set; } = new();
            public string OxoIntensitiesString { get; set; } = "";
            public string PeakDepthsString { get; set; } = "";
            public string OxoSummary { get; set; } = "";
        }

        public static OxoniumProcessResult GetOxoniumIons(SpectrumInfo spectrum,
            GlyCounterSettings glySettings,
            RawFileInfo localStats,
            object ionCountsLock)
        {
            var result = new OxoniumProcessResult();
            if (spectrum.TotalIonCurrent == 0.0) return result;

            int numberOfOxoIons = 0;
            double totalOxoSignal = 0;
            bool test204 = false;
            bool test163 = false;
            int countOxoWithinPeakDepthThreshold = 0;
            List<double> oxoniumIonFoundPeaks = new List<double>();
            List<double> oxoniumIonFoundMassErrors = new List<double>();
            string peakString = "";

            //custom ms levels
            if (!glySettings.ignoreMSLevel) //if ignore then no need to check ms level
            {
                var levels = GlyCounterSettings.GetLevelsList(glySettings);
                if (!levels.Contains(spectrum.MsLevel))
                    return result;
            }

            localStats.numberOfMS2scans++; // count per-worker scans

            var peakDepthThreshold = GetPeakDepthThreshold(spectrum.DissociationMethod, glySettings);
            UpdateFragmentationStats(spectrum.DissociationMethod, localStats);

            var sortedPeakDepths = new Dictionary<double, int>();
            PeakProcessing.RankOrderPeaks(sortedPeakDepths, spectrum);

            // Find oxonium ions in spectrum
            (bool _163, bool _204, bool _366) = DetectOxoniumIons(spectrum, glySettings, localStats, result, sortedPeakDepths, peakDepthThreshold, ionCountsLock);

            if (result.NumberOfOxoIons == 0)
                return result;
            result.ContainsIons = true;

            // Update oxonium count statistics
            UpdateOxoniumCountStats(spectrum.DissociationMethod, result.NumberOfOxoIons, localStats);

            // Calculate required ions and count
            var halfTotal = glySettings.oxoniumIonHashSet.Count / 2;
            localStats.numRequiredIons = CalculateRequiredIons(glySettings, result, halfTotal, _163);

            double oxoTICfraction = result.TotalOxoSignal / spectrum.TotalIonCurrent;
            double oxoCountRequirement = CalculateCountRequirement(spectrum.DissociationMethod,
                glySettings, localStats);

            // Reprocess peaks to build output strings
            BuildOutputLines(spectrum, glySettings, result, sortedPeakDepths, peakDepthThreshold,
                oxoTICfraction, oxoCountRequirement, localStats, _204, _366);

            return result;
        }

        private static double GetPeakDepthThreshold(Fragmentation.Type dissociationMethod,
            GlyCounterSettings glySettings)
        {
            return dissociationMethod switch
            {
                Fragmentation.Type.HCD => glySettings.peakDepthThreshold_hcd,
                Fragmentation.Type.ETD => glySettings.peakDepthThreshold_etd,
                Fragmentation.Type.UVPD => glySettings.peakDepthThreshold_uvpd,
                _ => 0.0
            };
        }

        private static void UpdateFragmentationStats(Fragmentation.Type dissociationMethod,
            RawFileInfo localStats)
        {
            switch (dissociationMethod)
            {
                case Fragmentation.Type.HCD:
                    localStats.numberOfHCDscans++;
                    break;
                case Fragmentation.Type.ETD:
                    localStats.numberOfETDscans++;
                    break;
                case Fragmentation.Type.UVPD:
                    localStats.numberOfUVPDscans++;
                    break;
            }
        }

        private static (bool _163, bool _204, bool _366) DetectOxoniumIons(SpectrumInfo spectrum, GlyCounterSettings glySettings,
            RawFileInfo localStats, OxoniumProcessResult result, Dictionary<double, int> sortedPeakDepths,
            double peakDepthThreshold, object ionCountsLock)
        {
            var testIons = new Dictionary<double, bool>();

            foreach (var oxoIon in glySettings.oxoniumIonHashSet)
            {
                SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, oxoIon.theoMZ,
                    glySettings.usingda, glySettings.tol);

                if (peak.Equals(new SpecDataPointEx()) || peak.Intensity <= 0)
                    continue;

                // Check S/N or intensity thresholds
                if (spectrum.HasNoise)
                {
                    if ((peak.Intensity / peak.Noise) < glySettings.SNthreshold)
                        continue;
                }
                else
                {
                    if (peak.Intensity < glySettings.intensityThreshold)
                        continue;
                }

                double measuredMz = peak.Mz;
                double intensity = peak.Intensity;
                int peakDepth = sortedPeakDepths.ContainsKey(peak.Intensity)
                    ? sortedPeakDepths[peak.Intensity]
                    : glySettings.arbitraryPeakDepthIfNotFound;

                result.NumberOfOxoIons++;
                result.TotalOxoSignal += intensity;

                // Track specific test ions
                TrackTestIon(oxoIon.theoMZ, peakDepth <= peakDepthThreshold, testIons);

                result.OxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                result.OxoniumIonFoundMassErrors.Add(measuredMz - oxoIon.theoMZ);

                // Thread-safe update of shared ion counts
                lock (ionCountsLock)
                {
                    var sharedIon = glySettings.oxoniumIonHashSet.FirstOrDefault(i =>
                        Math.Abs(i.theoMZ - oxoIon.theoMZ) < 1e-6);
                    if (sharedIon != null)
                    {
                        switch (spectrum.DissociationMethod)
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

            // Determine if spectrum is likely glycopeptide based on test ions
            var test163 = testIons.GetValueOrDefault(163.0601, false);
            var test204 = testIons.GetValueOrDefault(204.0867, true);
            var test366 = testIons.GetValueOrDefault(366.1395, true);

            if (!glySettings.using366)
                test366 = true;
            if (!glySettings.using204)
                test204 = true;
            if (!glySettings.using163)
                test163 = false;

            return (test163, test204, test366);
        }

        private static void TrackTestIon(double mz, bool withinThreshold, Dictionary<double, bool> testIons)
        {
            // Track 163.0601, 204.0867, and 366.1396
            if (Math.Abs(mz - 163.0601) < 0.0001)
                testIons[163.0601] = withinThreshold || testIons.GetValueOrDefault(163.0601, false);
            if (Math.Abs(mz - 204.0867) < 0.0001)
                testIons[204.0867] = withinThreshold || testIons.GetValueOrDefault(204.0867, false);
            if (Math.Abs(mz - 366.1395) < 0.0001)
                testIons[366.1395] = withinThreshold || testIons.GetValueOrDefault(366.1395, false);
        }

        private static int CalculateRequiredIons(GlyCounterSettings glySettings,
            OxoniumProcessResult result, int halfTotal, bool test163)
        {
            return glySettings.oxoniumIonHashSet.Count switch
            {
                < 6 => 4,
                >= 6 and <= 15 when test163 && halfTotal > 5 => 5,
                >= 6 and <= 15 => halfTotal,
                > 15 when test163 => 5,
                > 15 => 8,
            };
        }

        private static double CalculateCountRequirement(Fragmentation.Type dissociationMethod,
            GlyCounterSettings glySettings, RawFileInfo localStats)
        {
            return dissociationMethod switch
            {
                Fragmentation.Type.HCD => glySettings.oxoCountRequirement_hcd_user > 0
                    ? glySettings.oxoCountRequirement_hcd_user
                    : localStats.numRequiredIons,
                Fragmentation.Type.ETD => glySettings.oxoCountRequirement_etd_user > 0
                    ? glySettings.oxoCountRequirement_etd_user
                    : localStats.numRequiredIons / 2,
                Fragmentation.Type.UVPD => glySettings.oxoCountRequirement_uvpd_user > 0
                    ? glySettings.oxoCountRequirement_uvpd_user
                    : localStats.numRequiredIons,
                _ => localStats.numRequiredIons
            };
        }

        private static void UpdateOxoniumCountStats(Fragmentation.Type dissociationMethod,
            int numberOfOxoIons, RawFileInfo localStats)
        {
            switch (numberOfOxoIons)
            {
                case 1:
                    localStats.numberOfMS2scansWithOxo_1++;
                    if (dissociationMethod == Fragmentation.Type.HCD)
                        localStats.numberOfMS2scansWithOxo_1_hcd++;
                    else if (dissociationMethod == Fragmentation.Type.ETD)
                        localStats.numberOfMS2scansWithOxo_1_etd++;
                    else if (dissociationMethod == Fragmentation.Type.UVPD)
                        localStats.numberOfMS2scansWithOxo_1_uvpd++;
                    break;
                case 2:
                    localStats.numberOfMS2scansWithOxo_2++;
                    if (dissociationMethod == Fragmentation.Type.HCD)
                        localStats.numberOfMS2scansWithOxo_2_hcd++;
                    else if (dissociationMethod == Fragmentation.Type.ETD)
                        localStats.numberOfMS2scansWithOxo_2_etd++;
                    else if (dissociationMethod == Fragmentation.Type.UVPD)
                        localStats.numberOfMS2scansWithOxo_2_uvpd++;
                    break;
                case 3:
                    localStats.numberOfMS2scansWithOxo_3++;
                    if (dissociationMethod == Fragmentation.Type.HCD)
                        localStats.numberOfMS2scansWithOxo_3_hcd++;
                    else if (dissociationMethod == Fragmentation.Type.ETD)
                        localStats.numberOfMS2scansWithOxo_3_etd++;
                    else if (dissociationMethod == Fragmentation.Type.UVPD)
                        localStats.numberOfMS2scansWithOxo_3_uvpd++;
                    break;
                case 4:
                    localStats.numberOfMS2scansWithOxo_4++;
                    if (dissociationMethod == Fragmentation.Type.HCD)
                        localStats.numberOfMS2scansWithOxo_4_hcd++;
                    else if (dissociationMethod == Fragmentation.Type.ETD)
                        localStats.numberOfMS2scansWithOxo_4_etd++;
                    else if (dissociationMethod == Fragmentation.Type.UVPD)
                        localStats.numberOfMS2scansWithOxo_4_uvpd++;
                    break;
                default: // 5+
                    localStats.numberOfMS2scansWithOxo_5plus++;
                    if (dissociationMethod == Fragmentation.Type.HCD)
                        localStats.numberOfMS2scansWithOxo_5plus_hcd++;
                    else if (dissociationMethod == Fragmentation.Type.ETD)
                        localStats.numberOfMS2scansWithOxo_5plus_etd++;
                    else if (dissociationMethod == Fragmentation.Type.UVPD)
                        localStats.numberOfMS2scansWithOxo_5plus_uvpd++;
                    break;
            }
        }

        private static void BuildOutputLines(SpectrumInfo spectrum, GlyCounterSettings glySettings,
            OxoniumProcessResult result, Dictionary<double, int> sortedPeakDepths,
            double peakDepthThreshold, double oxoTICfraction, double oxoCountRequirement,
            RawFileInfo localStats, bool _204, bool _366)
        {
            var sbOxo = new StringBuilder();
            var sbPeakDepth = new StringBuilder();
            int countWithin = 0;

            foreach (var oxoIon in glySettings.oxoniumIonHashSet)
            {
                SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, oxoIon.theoMZ,
                    glySettings.usingda, glySettings.tol);

                double intensity = 0;
                if (!peak.Equals(new SpecDataPointEx()) && peak.Intensity > 0)
                {
                    if (spectrum.HasNoise)
                    {
                        if ((peak.Intensity / peak.Noise) > glySettings.SNthreshold)
                            intensity = peak.Intensity;
                    }
                    else if (peak.Intensity > glySettings.intensityThreshold)
                        intensity = peak.Intensity;
                }

                sbOxo.Append(intensity).Append('\t');

                if (peak.Equals(new SpecDataPointEx()) || peak.Intensity <= 0)
                {
                    sbPeakDepth.Append("NotFound\t");
                    continue;
                }

                int peakdepth = sortedPeakDepths.ContainsKey(peak.Intensity)
                    ? sortedPeakDepths[peak.Intensity]
                    : glySettings.arbitraryPeakDepthIfNotFound;
                sbPeakDepth.Append(peakdepth).Append('\t');

                if (peakdepth <= peakDepthThreshold)
                    countWithin++;
            }

            bool hasDiagnosticIon;
            if (spectrum.Tims)
                hasDiagnosticIon = _366;
            else hasDiagnosticIon = _204;
            
            bool isLikelyGlyco = countWithin >= oxoCountRequirement && oxoTICfraction >= GetTICThreshold(
                spectrum.DissociationMethod, glySettings) && hasDiagnosticIon;

            if (isLikelyGlyco)
            {
                UpdateLikelyGlycoStats(spectrum.DissociationMethod, localStats);
                localStats.likelyGlycoSpectrum = true;
            }

            result.OxoIntensitiesString = sbOxo.ToString();
            result.PeakDepthsString = sbPeakDepth.ToString();
            result.OxoSummary = $"{countWithin}\t{oxoCountRequirement}\t{oxoTICfraction:F6}\t{isLikelyGlyco}";
        }

        private static double GetTICThreshold(Fragmentation.Type dissociationMethod,
            GlyCounterSettings glySettings)
        {
            return dissociationMethod switch
            {
                Fragmentation.Type.HCD => glySettings.oxoTICfractionThreshold_hcd,
                Fragmentation.Type.ETD => glySettings.oxoTICfractionThreshold_etd,
                Fragmentation.Type.UVPD => glySettings.oxoTICfractionThreshold_uvpd,
                _ => 0.0
            };
        }

        private static void UpdateLikelyGlycoStats(Fragmentation.Type dissociationMethod,
            RawFileInfo localStats)
        {
            switch (dissociationMethod)
            {
                case Fragmentation.Type.HCD:
                    localStats.numberScansCountedLikelyGlyco_hcd++;
                    break;
                case Fragmentation.Type.ETD:
                    localStats.numberScansCountedLikelyGlyco_etd++;
                    break;
                case Fragmentation.Type.UVPD:
                    localStats.numberScansCountedLikelyGlyco_uvpd++;
                    break;
            }
        }
    }
}
