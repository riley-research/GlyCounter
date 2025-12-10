using Nova.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class iC_ProcessTimsTOF
    {
        public static Form1 update = new Form1();

        public static (GlyCounterSettings, RawFileInfo) processTimsTOF(string fileName, GlyCounterSettings glySettings,
            iCounterSettings iCsettings, RawFileInfo rawFileInfo, StreamWriter outputSignal, StreamWriter outputPeakDepth, StreamWriter? outputIPSA)
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
                int numberOfIons = 0;
                double totalSignal = 0;
                bool hcdTrue = false;
                bool etdTrue = false;
                bool uvpdTrue = false;
                List<double> oxoniumIonFoundPeaks = new List<double>();
                List<double> oxoniumIonFoundMassErrors = new List<double>();

                //could add support for ECD and UVPD here if it becomes possible
                rawFileInfo.numberOfHCDscans++;
                hcdTrue = true;

                spectrum.peaks = PeakProcessing.ListsToPeaks(spectrum.mz.ToList(), spectrum.intensity.ToList());

                string ionHeader = "";
                double basePeak = spectrum.intensity.Max();
                if (basePeak > 0) //spectrum.total_ion_current > 0 && 
                {
                    Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();
                    sortedPeakDepths = PeakProcessing.RankOrderPeaks(sortedPeakDepths, spectrum);

                    //add nce data here if it becomes possible

                    foreach (Ion ion in iCsettings._ionHashSet)
                    {
                        ion.intensity = 0;
                        ion.peakDepth = glySettings.arbitraryPeakDepthIfNotFound;
                        ionHeader = ionHeader + ion.description + "\t";
                        ion.measuredMZ = 0;
                        ion.intensity = 0;

                        SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, ion.theoMZ, glySettings.usingda,
                            glySettings.tol);

                        if (!peak.Equals(new SpecDataPointEx()) && peak.Intensity > glySettings.intensityThreshold)
                        {
                            ion.measuredMZ = peak.Mz;
                            ion.intensity = peak.Intensity;
                            ion.peakDepth = sortedPeakDepths[peak.Intensity];
                            numberOfIons++;
                            totalSignal += peak.Intensity;

                            if (hcdTrue)
                                ion.hcdCount++;
                            if (etdTrue)
                                ion.etdCount++;
                            if (uvpdTrue)
                                ion.uvpdCount++;

                            oxoniumIonFoundPeaks.Add(ion.theoMZ);
                            var massError = ion.measuredMZ - ion.theoMZ;
                            oxoniumIonFoundMassErrors.Add(massError);
                        }
                    }
                }

                if (numberOfIons > 0)
                {

                    if (rawFileInfo.firstSpectrumInFile)
                    {
                        outputSignal.WriteLine(ionHeader);
                        outputPeakDepth.WriteLine(ionHeader);
                        rawFileInfo.firstSpectrumInFile = false;
                    }

                    if (numberOfIons == 1)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_1++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_1_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_1_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_1_uvpd++;
                    }

                    if (numberOfIons == 2)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_2++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_2_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_2_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_2_uvpd++;
                    }

                    if (numberOfIons == 3)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_3++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_3_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_3_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_3_uvpd++;
                    }

                    if (numberOfIons == 4)
                    {
                        rawFileInfo.numberOfMS2scansWithOxo_4++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_4_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_4_etd++;
                        if (uvpdTrue)
                            rawFileInfo.numberOfMS2scansWithOxo_4_uvpd++;
                    }

                    if (numberOfIons > 4)
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
                    string peakString = "";
                    foreach (double theoMZ in oxoniumIonFoundPeaks)
                        peakString = peakString + theoMZ.ToString() + "; ";
                    string errorString = new string("");
                    foreach (double error in oxoniumIonFoundMassErrors)
                        errorString = errorString + error.ToString("F6") + "; ";

                    //write scan info
                    outputSignal.Write(spectrum.id + "\t" + 2 + '\t' + retentionTime + "\t" +
                                       precursormz + "\t" + rawFileInfo.nce + "\t" + scanTIC + "\t" + totalSignal +
                                       "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan +
                                       "\t" + numberOfIons + "\t" + totalSignal + "\t");
                    outputPeakDepth.Write(spectrum.id + "\t" + 2 + '\t' + retentionTime + "\t" +
                                          scanTIC + "\t" + totalSignal + "\t" + scanInjTime + "\t" +
                                          fragmentationType + "\t" + parentScan + "\t" + numberOfIons +
                                          "\t" + totalSignal + "\t");
                    if (outputIPSA != null)
                        outputIPSA.WriteLine(spectrum.id + "\t" + peakString + "\t" + errorString + "\t");

                    foreach (Ion ion in iCsettings._ionHashSet)
                    {
                        outputSignal.Write(ion.intensity + "\t");

                        if (ion.peakDepth == glySettings.arbitraryPeakDepthIfNotFound)
                        {
                            outputPeakDepth.Write("NotFound\t");
                        }
                        else
                        {
                            outputPeakDepth.Write(ion.peakDepth + "\t");
                        }

                    }

                    outputSignal.WriteLine();
                    outputPeakDepth.WriteLine();
                }
                update.UpdateTimer();
            }

            return (glySettings, rawFileInfo);
        }
    }
}
