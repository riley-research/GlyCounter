using Nova.Data;
using Nova.Io.Read;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class ProcessTimsTOF
    {
        static Form1 update = new Form1();
        public static (GlyCounterSettings, RawFileInfo) processTimsTOF(string fileName, GlyCounterSettings glySettings,
            RawFileInfo rawFileInfo, StreamWriter outputOxo, StreamWriter outputPeakDepth, StreamWriter? outputIPSA)
        {
            var ptr = Native.read_msn_spectra(fileName);
            if (ptr == IntPtr.Zero) { throw new Exception("read failed"); }

            string json = Marshal.PtrToStringAnsi(ptr);
            Native.tr_free_cstring(ptr);
            var spectra = System.Text.Json.JsonSerializer.Deserialize<RawSpectrum[]>(json);

            foreach (RawSpectrum spectrum in spectra)
            {
                //could add MSn support here in the future if it becomes possible

                rawFileInfo.numberOfMS2scans++;
                int numberOfOxoIons = 0;
                double totalOxoSignal = 0;
                rawFileInfo.likelyGlycoSpectrum = false;
                bool test204 = false;
                int countOxoWithinPeakDepthThreshold = 0;
                bool hcdTrue = false;
                bool etdTrue = false;
                bool uvpdTrue = false;
                List<double> oxoniumIonFoundPeaks = new List<double>();
                List<double> oxoniumIonFoundMassErrors = new List<double>();

                //could add support for ECD and UVPD here if it becomes possible
                rawFileInfo.numberOfHCDscans++;
                hcdTrue = true;

                spectrum.peaks = PeakProcessing.ListsToPeaks(spectrum.mz.ToList(), spectrum.intensity.ToList());

                string oxoIonHeader = "";
                double basePeak = spectrum.intensity.Max();
                if (basePeak > 0) //spectrum.total_ion_current > 0 && 
                {
                    Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();
                    sortedPeakDepths = PeakProcessing.RankOrderPeaks(sortedPeakDepths, spectrum);

                    //add nce data here if it becomes possible

                    foreach (OxoniumIon oxoIon in glySettings.oxoniumIonHashSet)
                    {
                        oxoIon.intensity = 0;
                        oxoIon.peakDepth = glySettings.arbitraryPeakDepthIfNotFound;
                        oxoIonHeader = oxoIonHeader + oxoIon.description + "\t";
                        oxoIon.measuredMZ = 0;
                        oxoIon.intensity = 0;

                        SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, oxoIon.theoMZ, glySettings.usingda,
                            glySettings.tol);

                        if (!peak.Equals(new SpecDataPointEx()) && peak.Intensity > glySettings.intensityThreshold)
                        {
                            oxoIon.measuredMZ = peak.Mz;
                            oxoIon.intensity = peak.Intensity;
                            oxoIon.peakDepth = sortedPeakDepths[peak.Intensity];
                            numberOfOxoIons++;
                            totalOxoSignal += peak.Intensity;

                            if (hcdTrue)
                                oxoIon.hcdCount++;
                            if (etdTrue)
                                oxoIon.etdCount++;
                            if (uvpdTrue)
                                oxoIon.uvpdCount++;

                            if (oxoIon.theoMZ == 204.0867 &&
                                sortedPeakDepths[peak.Intensity] <= glySettings.peakDepthThreshold_hcd && hcdTrue)
                                test204 = true;

                            if (oxoIon.theoMZ == 204.0867 &&
                                sortedPeakDepths[peak.Intensity] <= glySettings.peakDepthThreshold_etd && etdTrue)
                                test204 = true;

                            if (oxoIon.theoMZ == 204.0867 &&
                                sortedPeakDepths[peak.Intensity] <= glySettings.peakDepthThreshold_uvpd && uvpdTrue)
                                test204 = true;

                            oxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                            var massError = oxoIon.measuredMZ - oxoIon.theoMZ;
                            oxoniumIonFoundMassErrors.Add(massError);
                        }
                    }
                }

                if (rawFileInfo.firstSpectrumInFile)
                {
                    outputOxo.WriteLine(oxoIonHeader +
                                        "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                    outputPeakDepth.WriteLine(oxoIonHeader +
                                              "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                    rawFileInfo.firstSpectrumInFile = false;
                }

                if (numberOfOxoIons > 0)
                {
                    if (numberOfOxoIons == 1)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_1++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_1_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_1_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_1_uvpd++;
                    }

                    if (numberOfOxoIons == 2)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_2++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_2_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_2_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_2_uvpd++;
                    }

                    if (numberOfOxoIons == 3)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_3++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_3_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_3_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_3_uvpd++;
                    }

                    if (numberOfOxoIons == 4)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_4++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_4_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_4_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_4_uvpd++;
                    }

                    if (numberOfOxoIons > 4)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_5plus++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_5plus_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_5plus_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_5plus_uvpd++;
                    }

                    //this doesn't seem to actually be the parent scan number ?
                    string parentScan = spectrum.precursors[0].spectrum_ref;
                    double scanTIC = spectrum.intensity.Sum();
                    float? scanInjTime = spectrum.ion_injection_time;
                    string fragmentationType = "";
                    if (hcdTrue) fragmentationType = "HCD";
                    if (etdTrue) fragmentationType = "ETD";
                    if (uvpdTrue) fragmentationType = "UVPD";
                    float? retentionTime = spectrum.scan_start_time;
                    double precursormz = spectrum.precursors[0].mz;
                    List<double> oxoRanks = new List<double>();
                    string peakString = "";
                    foreach (double theoMZ in oxoniumIonFoundPeaks)
                        peakString = peakString + theoMZ.ToString() + "; ";
                    string errorString = new string("");
                    foreach (double error in oxoniumIonFoundMassErrors)
                        errorString = errorString + error.ToString("F6") + "; ";

                    //write scan info
                    outputOxo.Write(spectrum.id + "\t" + retentionTime + "\t" + spectrum.ms_level + "\t" + precursormz +
                                    "\t" +
                                    rawFileInfo.nce + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime +
                                    "\t" +
                                    fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" +
                                    totalOxoSignal + "\t");
                    outputPeakDepth.Write(spectrum.id + "\t" + retentionTime + "\t" + spectrum.ms_level + "\t" +
                                          precursormz +
                                          "\t" + rawFileInfo.nce + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" +
                                          scanInjTime +
                                          "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" +
                                          totalOxoSignal + "\t");
                    if (outputIPSA != null)
                        outputIPSA.WriteLine(spectrum.id + "\t" + peakString + "\t" + errorString + "\t");

                    foreach (OxoniumIon oxoIon in glySettings.oxoniumIonHashSet)
                    {
                        outputOxo.Write(oxoIon.intensity + "\t");

                        if (oxoIon.peakDepth == glySettings.arbitraryPeakDepthIfNotFound)
                        {
                            outputPeakDepth.Write("NotFound\t");
                        }
                        else
                        {
                            outputPeakDepth.Write(oxoIon.peakDepth + "\t");
                            oxoRanks.Add(oxoIon.peakDepth);
                            if (hcdTrue && oxoIon.peakDepth <= glySettings.peakDepthThreshold_hcd)
                                countOxoWithinPeakDepthThreshold++;

                            if (etdTrue && oxoIon.peakDepth <= glySettings.peakDepthThreshold_etd)
                                countOxoWithinPeakDepthThreshold++;

                            if (uvpdTrue && oxoIon.peakDepth <= glySettings.peakDepthThreshold_uvpd)
                                countOxoWithinPeakDepthThreshold++;

                        }

                    }

                    //double medianRanks = Statistics.Median(oxoRanks);
                    //the median peak depth has to be "higher" (i.e., less than) the peak depth threshold 
                    //considered also using the number of oxonium ions found has to be at least half to the total list looked for, but decided against it for now (what if big list?)
                    if (glySettings.oxoniumIonHashSet.Count < 6)
                    {
                        rawFileInfo.halfTotalList = 4;
                    }

                    if (glySettings.oxoniumIonHashSet.Count > 15)
                    {
                        rawFileInfo.halfTotalList = 8;
                    }

                    //if not using 204, the below test will fail by default, so we need to add this in to make sure we check the calculation even if 204 isn't being used.
                    if (!glySettings.using204)
                        test204 = true;

                    double oxoTICfraction = totalOxoSignal / scanTIC;

                    //Check if there is a user input oxonium count requirement. If not, use default values
                    double oxoCountRequirement = 0;
                    if (hcdTrue)
                        oxoCountRequirement = glySettings.oxoCountRequirement_hcd_user > 0
                            ? glySettings.oxoCountRequirement_hcd_user
                            : rawFileInfo.halfTotalList;
                    if (etdTrue)
                        oxoCountRequirement = glySettings.oxoCountRequirement_etd_user > 0
                            ? glySettings.oxoCountRequirement_etd_user
                            : rawFileInfo.halfTotalList / 2;
                    if (uvpdTrue)
                        oxoCountRequirement = glySettings.oxoCountRequirement_uvpd_user > 0
                            ? glySettings.oxoCountRequirement_uvpd_user
                            : rawFileInfo.halfTotalList;

                    //intensity differences for HCD and ETD means we need to have two different % TIC threshold values.
                    //changed this to not use median, but instead say the number of oxonium ions with peakdepth within user-deined threshold
                    //needs to be greater than half the total list (or its definitions given above
                    if (hcdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 &&
                        oxoTICfraction >= glySettings.oxoTICfractionThreshold_hcd)
                    {
                        rawFileInfo.likelyGlycoSpectrum = true;
                        rawFileInfo.numberScansCountedLikelyGlyco_hcd++;
                    }

                    //etd also differs in peak depth, so changed scaled this by 1.5
                    if (etdTrue && numberOfOxoIons >= oxoCountRequirement && test204 &&
                        oxoTICfraction >= glySettings.oxoTICfractionThreshold_etd)
                    {
                        rawFileInfo.likelyGlycoSpectrum = true;
                        rawFileInfo.numberScansCountedLikelyGlyco_etd++;
                    }

                    if (uvpdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 &&
                        oxoTICfraction >= glySettings.oxoTICfractionThreshold_uvpd)
                    {
                        rawFileInfo.likelyGlycoSpectrum = true;
                        rawFileInfo.numberScansCountedLikelyGlyco_uvpd++;
                    }

                    outputOxo.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" +
                                    oxoTICfraction + "\t" + rawFileInfo.likelyGlycoSpectrum);
                    outputPeakDepth.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" +
                                          oxoTICfraction + "\t" + rawFileInfo.likelyGlycoSpectrum);

                    outputOxo.WriteLine();
                    outputPeakDepth.WriteLine();
                }
                update.UpdateTimer();
            }

            return (glySettings, rawFileInfo);
        }
    }
}
