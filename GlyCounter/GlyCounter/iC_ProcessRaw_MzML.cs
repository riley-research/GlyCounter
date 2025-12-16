using Nova.Data;
using Nova.Io.Read;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace GlyCounter
{
    public class iC_ProcessRaw_MzML
    {
        static Form1 update = new Form1();

        public static (GlyCounterSettings, RawFileInfo) processRaw_MzML(string fileName, GlyCounterSettings glySettings, iCounterSettings iCsettings,
            RawFileInfo rawFileInfo, StreamWriter outputSignal, StreamWriter outputPeakDepth, StreamWriter? outputIPSA)
        {
            FileReader rawFile = new FileReader(fileName);
            FileReader typeCheck = new FileReader();
            string fileType = typeCheck.CheckFileFormat(fileName).ToString(); //either "ThermoRaw" or "MzML"
            bool thermo = true;
            if (fileType == "MzML")
                thermo = false;

            for (int i = rawFile.FirstScan; i <= rawFile.LastScan; i++)
            {
                SpectrumEx spectrum = rawFile.ReadSpectrumEx(scanNumber: i);
                bool IT = spectrum.Analyzer.Contains("ITMS");

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
                if (glySettings.msLevelLB > glySettings.msLevelUB)
                {
                    lowestval = Convert.ToInt32(glySettings.msLevelUB);
                    highestval = Convert.ToInt32(glySettings.msLevelLB);

                    levels = Enumerable.Range(lowestval, (highestval - lowestval + 1)).ToList();
                }

                //if ignore ms levels is checked ignore levels list
                if (glySettings.ignoreMSLevel)
                    if (!levels.Contains(spectrum.MsLevel))
                        continue;

                rawFileInfo.numberOfMS2scans++;
                int numberOfIons = 0;
                double totalSignal = 0;
                bool hcdTrue = false;
                bool etdTrue = false;
                bool uvpdTrue = false;
                List<double> ionFoundPeaks = [];
                List<double> ionFoundMassErrors = [];

                //figure out dissociation type
                if (thermo)
                {
                    //using this order means ethcd will count as etd (since both show in scan filter)
                    if (spectrum.ScanFilter.Contains("etd"))
                    {
                        rawFileInfo.numberOfETDscans++;
                        etdTrue = true;
                    }
                    else if (spectrum.ScanFilter.Contains("hcd"))
                    {
                        rawFileInfo.numberOfHCDscans++;
                        hcdTrue = true;
                    }
                    else if (spectrum.ScanFilter.Contains("uvpd") || spectrum.ScanFilter.Contains("ci"))
                    {
                        rawFileInfo.numberOfUVPDscans++;
                        uvpdTrue = true;
                    }
                }
                else
                {
                    string dt = spectrum.Precursors[0].FramentationMethod.ToString();

                    if (dt.Equals("HCD"))
                    {
                        rawFileInfo.numberOfHCDscans++;
                        hcdTrue = true;
                    }

                    if (dt.Equals("ETD"))
                    {
                        rawFileInfo.numberOfETDscans++;
                        etdTrue = true;
                    }

                    if (dt.Equals("CI") || dt.Equals("UVPD"))
                    {
                        rawFileInfo.numberOfUVPDscans++;
                        uvpdTrue = true;
                    }

                }

                string ionHeader = "";

                if (spectrum is { TotalIonCurrent: > 0, BasePeakIntensity: > 0 })
                {
                    Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();
                    PeakProcessing.RankOrderPeaks(sortedPeakDepths, spectrum);

                    if (thermo)
                    {
                        string scanFilter = spectrum.ScanFilter ?? "";
                        string[] hcdHeader = scanFilter.Split('@', StringSplitOptions.RemoveEmptyEntries);
                        rawFileInfo.nce = double.NaN;
                        if (hcdHeader.Length >= 2)
                        {
                            string candidate = hcdHeader.Length >= 3 && hcdHeader[1].Contains("ptr") ? hcdHeader[2] : hcdHeader[1];
                            var m = System.Text.RegularExpressions.Regex.Match(candidate, @"(\d+(\.\d+)?)");
                            if (m.Success && double.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                                rawFileInfo.nce = parsed;
                        }
                    }
                    else rawFileInfo.nce = spectrum.Precursors[0].CollisionEnergy;

                    foreach (Ion ion in iCsettings._ionHashSet)
                    {
                        ion.intensity = 0;
                        ion.peakDepth = glySettings.arbitraryPeakDepthIfNotFound;
                        ionHeader = ionHeader + ion.description + "\t";
                        ion.measuredMZ = 0;
                        ion.intensity = 0;

                        SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, ion.theoMZ, glySettings.usingda, glySettings.tol, thermo, IT);

                        if (!IT && thermo)
                        {
                            if (peak.Equals(new SpecDataPointEx()) || !(peak.Intensity > 0) ||
                                !((peak.Intensity / peak.Noise) > glySettings.SNthreshold)) continue;

                            ion.measuredMZ = peak.Mz;
                            ion.intensity = peak.Intensity;
                            ion.peakDepth = sortedPeakDepths[peak.Intensity];
                            numberOfIons++;
                            totalSignal = totalSignal + peak.Intensity;

                            if (hcdTrue)
                                ion.hcdCount++;
                            if (etdTrue)
                                ion.etdCount++;
                            if (uvpdTrue)
                                ion.uvpdCount++;

                            ionFoundPeaks.Add(ion.theoMZ);
                            double massError = ion.measuredMZ - ion.theoMZ;
                            ionFoundMassErrors.Add(massError);
                        }
                        else
                        {
                            if (peak.Equals(new SpecDataPointEx()) ||
                                !(peak.Intensity > glySettings.intensityThreshold)) continue;

                            ion.measuredMZ = peak.Mz;
                            ion.intensity = peak.Intensity;
                            ion.peakDepth = sortedPeakDepths[peak.Intensity];
                            numberOfIons++;
                            totalSignal = totalSignal + peak.Intensity;

                            if (hcdTrue)
                                ion.hcdCount++;
                            if (etdTrue)
                                ion.etdCount++;
                            if (uvpdTrue)
                                ion.uvpdCount++;

                            ionFoundPeaks.Add(ion.theoMZ);
                            double massError = ion.measuredMZ - ion.theoMZ;
                            ionFoundMassErrors.Add(massError);
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

                    //write scan info
                    outputSignal.Write(i + "\t" + spectrum.MsLevel + '\t' + retentionTime + "\t" +
                                       precursormz + "\t" + rawFileInfo.nce + "\t" + scanTIC + "\t" + totalSignal +
                                       "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan +
                                       "\t" + numberOfIons + "\t" + totalSignal + "\t");
                    outputPeakDepth.Write(i + "\t" + spectrum.MsLevel + '\t' + retentionTime + "\t" +
                                          scanTIC + "\t" + totalSignal + "\t" + scanInjTime + "\t" +
                                          fragmentationType + "\t" + parentScan + "\t" + numberOfIons +
                                          "\t" + totalSignal + "\t");
                    outputIPSA?.WriteLine(i + "\t" + peakString + "\t" + errorString + "\t");

                    foreach (Ion ion in iCsettings._ionHashSet)
                    {
                        outputSignal.Write(ion.intensity + "\t");

                        if (ion.peakDepth == glySettings.arbitraryPeakDepthIfNotFound)
                            outputPeakDepth.Write("NotFound\t");
                        else
                            outputPeakDepth.Write(ion.peakDepth + "\t");
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
