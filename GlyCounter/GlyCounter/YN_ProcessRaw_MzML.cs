using CSMSL.Proteomics;
using CSMSL;
using CSMSL.Chemistry;
using LumenWorks.Framework.IO.Csv;
using Nova.Data;
using Nova.Io.Read;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public class YN_ProcessRaw_MzML
    {
        static Form1 update = new Form1();
        public static (YnaughtSettings, RawFileInfo) yNprocessRawMzML(YnaughtSettings yNsettings, GlyCounterSettings glySettings, RawFileInfo rawFileInfo, StreamWriter outputYion, StreamWriter? outputIPSA)
        {
            // TODO: DELETE LOGGING - Debug logging for Yion discovery issue
            var debugLog = new StringBuilder();
            debugLog.AppendLine("========== YNAUGHT DEBUG LOG START ==========");
            debugLog.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

            //set the rawfile path and open it
            FileReader rawFile = new FileReader(yNsettings.rawFilePath);
            FileReader typeCheck = new FileReader();
            bool thermo = true;
            if (typeCheck.CheckFileFormat(yNsettings.rawFilePath).ToString().Contains("MzML"))
                thermo = false;

            // TODO: DELETE LOGGING
            debugLog.AppendLine($"File type - Thermo: {thermo}");

            //update the timer
            update.UpdateTimerYn();

            //create PSM list to add each entry to
            List<PSM> psmList = new List<PSM>();

            //this is currently set up for MSFragger data form the psms file
            //we might want to write a converter for different data types
            using StreamReader pepIDtxtFile = new StreamReader(yNsettings.pepIDFilePath);
            using (var txt = new CsvReader(pepIDtxtFile, true, '\t'))
            {
                while (txt.ReadNextRecord())
                {
                    //create a new PSM object
                    PSM psm = new PSM();

                    //read in peptide sequence and create peptide objects
                    string peptideSeq = txt["Peptide"];
                    Peptide peptide = new Peptide(peptideSeq); //create new peptide object with this PSM's ID'ed sequence that will have all mods (useful for subtraction)
                    Peptide peptideNoGlycanMods = new Peptide(peptideSeq); //create new peptide object with this PSM's ID'ed sequence but will not have glycan attached (useful for addition)

                    //add these items to the PSM object
                    psm.peptide = peptide;
                    psm.peptideNoGlycanMods = peptideNoGlycanMods;

                    //read in other details
                    string spectrumToBeParsed = txt["Spectrum"];
                    int charge = int.Parse(txt["Charge"]);
                    string totalGlycanComp = txt["Total Glycan Composition"];
                    string assignedMods = txt["Assigned Modifications"];

                    foreach (string item in assignedMods.Split(','))
                    {
                        string trimmed = item.Trim();
                        string modLocation = trimmed.Split('(')[0]; //format: AA##
                        char AA = modLocation[0];
                        int position = int.Parse(modLocation.Substring(1));
                        double modMass = double.Parse(trimmed.Split('(')[1].Split(')')[0]);
                        IMass mass = new Mass(modMass);
                        //assume any mod on N, S, or T is a glycan. Right now we have no way to verify this, would need to add more columns to the input.
                        if (AA != 'N' && AA != 'S' && AA != 'T') peptideNoGlycanMods.AddModification(mass, position);
                    }

                    //only process if it's a glycopeptide
                    if (totalGlycanComp.Equals("")) continue;

                    //set spectrum number
                    string[] spectrumArray = spectrumToBeParsed.Split('.');
                    int spectrumNum = Convert.ToInt32(spectrumArray[1]);

                    //add the rest of the information to the PSM object
                    psm.charge = charge;
                    psm.spectrumNumber = spectrumNum;
                    psm.totalGlycanComposition = totalGlycanComp;

                    //add PSM to list
                    psmList.Add(psm);

                }
            }

            // TODO: DELETE LOGGING
            debugLog.AppendLine($"Total PSMs loaded: {psmList.Count}");

            // Build a list of all Y-ion/charge state combinations to use for header and data
            var yIonHeaderColumns = new List<string>();
            var yIonChargeStatePairs = new List<(Yion yIon, int charge)>();

            //the same yion is often added from multiple sources. This should combine them all and add their sources to a string
            yNsettings.yIonHashSet = Form1.CombineDuplicateYions(yNsettings.yIonHashSet);

            // TODO: DELETE LOGGING
            debugLog.AppendLine($"Unique Yions to search for: {yNsettings.yIonHashSet.Count}");
            foreach (var yion in yNsettings.yIonHashSet)
            {
                debugLog.AppendLine($"  - {yion.description} (theoMass: {yion.theoMass}, glycanSource: {yion.glycanSource})");
            }

            // Determine the global charge state bounds for all PSMs
            int globalChargeLowerBound = 1;
            int globalChargeUpperBound = 1;
            foreach (var psm in psmList)
            {
                int precursorCharge = psm.charge;
                int chargeLowerBound = 1;
                int chargeUpperBound = precursorCharge;
                //use the user input to determine what charge states to look for. Set the minimum charge state to 1
                if (yNsettings.chargeLB.Contains('P'))
                {
                    if (yNsettings.chargeLB.Contains('-'))
                    {
                        //user entered 'P-X' so X is the subtracted value
                        var subtractedValue = int.Parse(yNsettings.chargeLB.Split('-')[1]);
                        chargeLowerBound = precursorCharge - subtractedValue;
                    }
                    //user entered 'P'
                    else chargeLowerBound = precursorCharge;
                }
                else
                {
                    try
                    {
                        //user entered a number
                        var num = int.Parse(yNsettings.chargeLB);
                        chargeLowerBound = num;
                    }
                    catch (Exception exception) { } //catch the error but don't do anything. The default value should be used.
                }
                if (yNsettings.chargeUB.Contains('P'))
                {
                    if (yNsettings.chargeUB.Contains('-'))
                    {
                        //user entered 'P-X' so X is the subtracted value
                        var subtractedValue = int.Parse(yNsettings.chargeUB.Split('-')[1]);
                        chargeUpperBound = precursorCharge - subtractedValue;
                    }
                    //user entered 'P'
                    else chargeUpperBound = precursorCharge;
                }
                else
                {
                    try
                    {
                        //user entered a number
                        var num = int.Parse(yNsettings.chargeUB);
                        chargeUpperBound = num;
                    }
                    catch (Exception exception) { } //catch the error but don't do anything. The default value should be used.
                }

                //handle weird integer inputs, set back to defaults
                if (chargeLowerBound > chargeUpperBound)
                {
                    chargeUpperBound = precursorCharge;
                    chargeLowerBound = 1;
                }
                if (chargeUpperBound < chargeLowerBound)
                {
                    chargeUpperBound = precursorCharge;
                    chargeLowerBound = 1;
                }
                if (chargeLowerBound < globalChargeLowerBound) globalChargeLowerBound = chargeLowerBound;
                if (chargeUpperBound > globalChargeUpperBound) globalChargeUpperBound = chargeUpperBound;
            }

            // TODO: DELETE LOGGING
            debugLog.AppendLine($"Global charge bounds - Lower: {globalChargeLowerBound}, Upper: {globalChargeUpperBound}");
            debugLog.AppendLine($"Charge settings - LB: '{yNsettings.chargeLB}', UB: '{yNsettings.chargeUB}'");
            debugLog.AppendLine($"Condense charge states: {yNsettings.condenseChargeStates}");
            debugLog.AppendLine($"Tolerance settings - DA: {yNsettings.usingda}, Tol: {yNsettings.tol}");
            debugLog.AppendLine($"Threshold settings - SN: {yNsettings.SNthreshold}, Intensity: {yNsettings.intensityThreshold}");

            // Build header columns
            if (yNsettings.condenseChargeStates)
            {
                foreach (var yIon in yNsettings.yIonHashSet)
                {
                    yIonHeaderColumns.Add(yIon.description);
                    yIonChargeStatePairs.Add((yIon, 0)); // 0 means condensed
                }
            }
            else
            {
                foreach (var yIon in yNsettings.yIonHashSet)
                {
                    for (int charge = globalChargeLowerBound; charge <= globalChargeUpperBound; charge++)
                    {
                        yIonHeaderColumns.Add($"{yIon.description}_+{charge}");
                        yIonChargeStatePairs.Add((yIon, charge));
                    }
                }
            }

            // TODO: DELETE LOGGING
            debugLog.AppendLine($"Header columns created: {yIonHeaderColumns.Count}");

            int psmProcessingIndex = 0;
            foreach (PSM psm in psmList)
            {
                psmProcessingIndex++;
                // TODO: DELETE LOGGING
                debugLog.AppendLine($"\n--- Processing PSM {psmProcessingIndex}/{psmList.Count}: Scan {psm.spectrumNumber}, Seq: {psm.peptideNoGlycanMods.SequenceWithModifications}, Charge: {psm.charge} ---");

                SpectrumEx spectrum = rawFile.ReadSpectrumEx(scanNumber: psm.spectrumNumber);

                // TODO: DELETE LOGGING
                debugLog.AppendLine($"Spectrum loaded - MSLevel: {spectrum.MsLevel}, TIC: {spectrum.TotalIonCurrent}, Analyzer: {spectrum.Analyzer}");

                if (spectrum.MsLevel == 2 && !spectrum.Analyzer.Contains("ITMS") && spectrum.TotalIonCurrent > 0)
                {
                    int numberOfYions = 0;
                    double totalYionSignal = 0;
                    int numberOfChargeStatesConsidered = 1;

                    bool hcdTrue = false;
                    bool etdTrue = false;

                    bool Y0_found = false;
                    bool intactGlycoPep_found = false;

                    rawFileInfo.numberOfMS2scans++;
                    if (spectrum.ScanFilter.Contains("etd") || spectrum.Precursors[0].FramentationMethod.ToString().Contains("ETD"))
                    {
                        rawFileInfo.numberOfETDscans++;
                        etdTrue = true;
                    }
                    if (spectrum.ScanFilter.Contains("hcd") || spectrum.Precursors[0].FramentationMethod.ToString().Contains("HCD"))
                    {
                        rawFileInfo.numberOfHCDscans++;
                        hcdTrue = true;
                    }

                    // TODO: DELETE LOGGING
                    debugLog.AppendLine($"Fragmentation - HCD: {hcdTrue}, ETD: {etdTrue}");

                    Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                    PeakProcessing.RankOrderPeaks(sortedPeakDepths, spectrum);

                    //set up peptide and glycopeptide masses to look for
                    double peptideNoGlycan_MonoMass = psm.peptideNoGlycanMods.MonoisotopicMass;
                    double peptideNoGlycan_firstIsoMass = psm.peptideNoGlycanMods.MonoisotopicMass + (1 * Constants.C13C12Difference);
                    double peptideNoGlycan_secondIsoMass = psm.peptideNoGlycanMods.MonoisotopicMass + (2 * Constants.C13C12Difference);

                    double glycopeptide_MonoMass = spectrum.Precursors[0].MonoisotopicMz * psm.charge - (psm.charge * Constants.Proton);
                    double glycopeptide_firstIsoMass = glycopeptide_MonoMass + (1 * Constants.C13C12Difference);
                    double glycopeptide_secondIsoMass = glycopeptide_MonoMass + (2 * Constants.C13C12Difference);

                    // TODO: DELETE LOGGING
                    debugLog.AppendLine($"Masses - PeptideNoGlycan: {peptideNoGlycan_MonoMass:F6}, Glycopeptide: {glycopeptide_MonoMass:F6}");

                    //look for each Y-ion
                    List<Yion> finalYionList = new List<Yion>(); //creating this to store charge separately

                    int yionIndex = 0;
                    foreach (Yion yIon in yNsettings.yIonHashSet)
                    {
                        yionIndex++;
                        // TODO: DELETE LOGGING
                        debugLog.AppendLine($"\n  [Yion {yionIndex}/{yNsettings.yIonHashSet.Count}] Searching for: {yIon.description} (theoMass: {yIon.theoMass}, glycanSource: {yIon.glycanSource})");

                        Yion foundYion = new Yion();
                        foundYion.description = yIon.description;
                        foundYion.theoMass = yIon.theoMass;
                        foundYion.glycanSource = yIon.glycanSource;
                        //I think these are not necessary to update but just in case
                        foundYion.hcdCount = yIon.hcdCount;
                        foundYion.etdCount = yIon.etdCount;

                        bool countYion = false;

                        //use the user input to determine what charge states to look for. Set the minimum charge state to 1
                        int precursorCharge = psm.charge;
                        int chargeLowerBound = 1;
                        if (yNsettings.chargeLB.Contains('P'))
                        {
                            if (yNsettings.chargeLB.Contains('-'))
                            {
                                //user entered 'P-X' so X is the subtracted value
                                var subtractedValue = int.Parse(yNsettings.chargeLB.Split('-')[1]);
                                chargeLowerBound = precursorCharge - subtractedValue;
                            }
                            //user entered 'P'
                            else chargeLowerBound = precursorCharge;
                        }
                        else
                        {
                            try
                            {
                                //user entered a number
                                var num = int.Parse(yNsettings.chargeLB);
                                chargeLowerBound = num;
                            }
                            catch (Exception exception) { } //catch the error but don't do anything. The default value should be used.
                        }

                        int chargeUpperBound = precursorCharge;
                        if (yNsettings.chargeUB.Contains('P'))
                        {
                            if (yNsettings.chargeUB.Contains('-'))
                            {
                                //user entered 'P-X' so X is the subtracted value
                                var subtractedValue = int.Parse(yNsettings.chargeUB.Split('-')[1]);
                                chargeUpperBound = precursorCharge - subtractedValue;
                            }
                            //user entered 'P'
                            else chargeUpperBound = precursorCharge;
                        }
                        else
                        {
                            try
                            {
                                //user entered a number
                                var num = int.Parse(yNsettings.chargeUB);
                                chargeUpperBound = num;
                            }
                            catch (Exception exception) { } //catch the error but don't do anything. The default value should be used.
                        }

                        //handle weird integer inputs, set back to defaults
                        if (chargeLowerBound > chargeUpperBound)
                        {
                            chargeUpperBound = precursorCharge;
                            chargeLowerBound = 1;
                        }

                        // TODO: DELETE LOGGING
                        debugLog.AppendLine($"    Charge range for this Yion: {chargeLowerBound} to {chargeUpperBound}");

                        //how many charge states are we looking for?
                        numberOfChargeStatesConsidered = chargeUpperBound - chargeLowerBound + 1;

                        //find all the Yions for each charge state considered
                        for (int i = chargeUpperBound; i >= chargeLowerBound; i--)
                        {
                            // TODO: DELETE LOGGING
                            debugLog.AppendLine($"      [Charge +{i}]");

                            //this is for eveything where we add glycan mass to the peptide itself
                            if (!yIon.glycanSource.Contains("Subtraction"))
                            {
                                double yIon_mz = (peptideNoGlycan_MonoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                // TODO: DELETE LOGGING
                                debugLog.AppendLine($"        Calculated m/z (Addition mode): {yIon_mz:F6}");

                                SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, yIon_mz, yNsettings.usingda, yNsettings.tol, thermo);

                                // TODO: DELETE LOGGING
                                debugLog.AppendLine($"        Peak result - Found: {!peak.Equals(new SpecDataPointEx())}, Intensity: {peak.Intensity:F6}, Noise: {peak.Noise:F6}");

                                if ((thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > 0 && (peak.Intensity / peak.Noise) > yNsettings.SNthreshold)
                                    || (!thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > yNsettings.intensityThreshold))
                                {
                                    // TODO: DELETE LOGGING
                                    debugLog.AppendLine($"        ✓ PEAK FOUND - S/N: {(peak.Intensity / peak.Noise):F6}");

                                    countYion = true; //this is to know if we can count the Y-ion as being found for keeping track of scans
                                    if (yIon.description.Contains("Y0"))
                                        Y0_found = true;

                                    //look for isotopes if user selected option
                                    double firstIsotopeIntensity = 0;
                                    double secondIsotopeIntensity = 0;
                                    if (yNsettings.firstIsotope)
                                    {
                                        double yIon_mzfirstIso = (peptideNoGlycan_firstIsoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                        SpecDataPointEx firstIsotopePeak = PeakProcessing.GetPeak(spectrum, yIon_mzfirstIso, yNsettings.usingda, yNsettings.tol, thermo);
                                        // TODO: DELETE LOGGING
                                        debugLog.AppendLine($"          First isotope m/z: {yIon_mzfirstIso:F6}, Found: {!firstIsotopePeak.Equals(new SpecDataPointEx())}, Intensity: {firstIsotopePeak.Intensity:F6}");

                                        if ((thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > 0 && (firstIsotopePeak.Intensity / firstIsotopePeak.Noise) > yNsettings.SNthreshold)
                                            || (!thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > yNsettings.intensityThreshold))
                                            firstIsotopeIntensity = firstIsotopePeak.Intensity;
                                    }
                                    if (yNsettings.secondIsotope)
                                    {
                                        double yIon_mzSecondIso = (peptideNoGlycan_secondIsoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                        SpecDataPointEx secondIsotopePeak = PeakProcessing.GetPeak(spectrum, yIon_mzSecondIso, yNsettings.usingda, yNsettings.tol, thermo);
                                        // TODO: DELETE LOGGING
                                        debugLog.AppendLine($"          Second isotope m/z: {yIon_mzSecondIso:F6}, Found: {!secondIsotopePeak.Equals(new SpecDataPointEx())}, Intensity: {secondIsotopePeak.Intensity:F6}");

                                        if ((thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > 0 && (secondIsotopePeak.Intensity / secondIsotopePeak.Noise) > yNsettings.SNthreshold)
                                            || (!thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > yNsettings.intensityThreshold))
                                            secondIsotopeIntensity = secondIsotopePeak.Intensity;
                                    }

                                    //store info in yIon object
                                    foundYion.intensities.Add(peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity);
                                    foundYion.mz.Add(peak.Mz);
                                    foundYion.chargeStates.Add(i);
                                    finalYionList.Add(foundYion);
                                    numberOfYions++;
                                    totalYionSignal += peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;

                                    // TODO: DELETE LOGGING
                                    debugLog.AppendLine($"        Total intensity (with isotopes): {(peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity):F6}");
                                }
                                else
                                {
                                    // TODO: DELETE LOGGING
                                    if (peak.Equals(new SpecDataPointEx()))
                                        debugLog.AppendLine($"        ✗ NO PEAK FOUND at m/z {yIon_mz:F6}");
                                    else if (peak.Intensity <= 0)
                                        debugLog.AppendLine($"        ✗ PEAK INTENSITY TOO LOW: {peak.Intensity:F6}");
                                    else if (thermo)
                                        debugLog.AppendLine($"        ✗ S/N THRESHOLD FAILED: {(peak.Intensity / peak.Noise):F6} < {yNsettings.SNthreshold}");
                                    else
                                        debugLog.AppendLine($"        ✗ INTENSITY THRESHOLD FAILED: {peak.Intensity:F6} < {yNsettings.intensityThreshold}");
                                }
                            }

                            //this is for glycan neutral losses from the intact glycopeptide
                            else
                            {
                                double yIon_mz = (glycopeptide_MonoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                // TODO: DELETE LOGGING
                                debugLog.AppendLine($"        Calculated m/z (Subtraction mode): {yIon_mz:F6}");

                                SpecDataPointEx peak = PeakProcessing.GetPeak(spectrum, yIon_mz, yNsettings.usingda, yNsettings.tol, thermo);

                                // TODO: DELETE LOGGING
                                debugLog.AppendLine($"        Peak result - Found: {!peak.Equals(new SpecDataPointEx())}, Intensity: {peak.Intensity:F6}, Noise: {peak.Noise:F6}");

                                if ((thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > 0 && (peak.Intensity / peak.Noise) > yNsettings.SNthreshold)
                                    || (!thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > yNsettings.intensityThreshold))
                                {
                                    // TODO: DELETE LOGGING
                                    debugLog.AppendLine($"        ✓ PEAK FOUND - S/N: {(peak.Intensity / peak.Noise):F6}");

                                    countYion = true; //this is to know if we can count the Y-ion as being found for keeping track of scans
                                    if (yIon.description.Contains("Intact Mass"))
                                        intactGlycoPep_found = true;

                                    double firstIsotopeIntensity = 0;
                                    double secondIsotopeIntensity = 0;
                                    if (yNsettings.firstIsotope)
                                    {
                                        double yIon_mzfirstIso = (glycopeptide_firstIsoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                        SpecDataPointEx firstIsotopePeak = PeakProcessing.GetPeak(spectrum, yIon_mzfirstIso, yNsettings.usingda, yNsettings.tol, thermo);
                                        // TODO: DELETE LOGGING
                                        debugLog.AppendLine($"          First isotope m/z: {yIon_mzfirstIso:F6}, Found: {!firstIsotopePeak.Equals(new SpecDataPointEx())}, Intensity: {firstIsotopePeak.Intensity:F6}");

                                        if ((thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > 0 && (firstIsotopePeak.Intensity / firstIsotopePeak.Noise) > yNsettings.SNthreshold)
                                            || (!thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > yNsettings.intensityThreshold)) ;
                                    }
                                    if (yNsettings.secondIsotope)
                                    {
                                        double yIon_mzSecondIso = (glycopeptide_secondIsoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                        SpecDataPointEx secondIsotopePeak = PeakProcessing.GetPeak(spectrum, yIon_mzSecondIso, yNsettings.usingda, yNsettings.tol, thermo);
                                        // TODO: DELETE LOGGING
                                        debugLog.AppendLine($"          Second isotope m/z: {yIon_mzSecondIso:F6}, Found: {!secondIsotopePeak.Equals(new SpecDataPointEx())}, Intensity: {secondIsotopePeak.Intensity:F6}");

                                        if ((thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > 0 && (secondIsotopePeak.Intensity / secondIsotopePeak.Noise) > yNsettings.SNthreshold)
                                            || (!thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > yNsettings.intensityThreshold))
                                            secondIsotopeIntensity = secondIsotopePeak.Intensity;
                                    }

                                    foundYion.intensities.Add(peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity);
                                    foundYion.mz.Add(peak.Mz);
                                    foundYion.chargeStates.Add(i);
                                    finalYionList.Add(foundYion);
                                    numberOfYions++;
                                    totalYionSignal = totalYionSignal + peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;

                                    // TODO: DELETE LOGGING
                                    debugLog.AppendLine($"        Total intensity (with isotopes): {(peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity):F6}");
                                }
                                else
                                {
                                    // TODO: DELETE LOGGING
                                    if (peak.Equals(new SpecDataPointEx()))
                                        debugLog.AppendLine($"        ✗ NO PEAK FOUND at m/z {yIon_mz:F6}");
                                    else if (peak.Intensity <= 0)
                                        debugLog.AppendLine($"        ✗ PEAK INTENSITY TOO LOW: {peak.Intensity:F6}");
                                    else if (thermo)
                                        debugLog.AppendLine($"        ✗ S/N THRESHOLD FAILED: {(peak.Intensity / peak.Noise):F6} < {yNsettings.SNthreshold}");
                                    else
                                        debugLog.AppendLine($"        ✗ INTENSITY THRESHOLD FAILED: {peak.Intensity:F6} < {yNsettings.intensityThreshold}");
                                }
                            }

                        }
                        if (hcdTrue && countYion)
                            yIon.hcdCount++;
                        if (etdTrue && countYion)
                            yIon.etdCount++;

                        // TODO: DELETE LOGGING
                        string result = "";
                        if (countYion) result = "FOUND";
                        else result = "NOT FOUND";
                        debugLog.AppendLine($"  [{yIon.description}] Final result: {result} ({foundYion.chargeStates.Count} charge states)");
                    }

                    // TODO: DELETE LOGGING
                    debugLog.AppendLine($"Total Yions found in this scan: {numberOfYions}");

                    //update counts
                    if (Y0_found)
                    {
                        rawFileInfo.numberOfMS2scansWithY0++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithY0_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithY0_etd++;
                    }
                    if (intactGlycoPep_found)
                    {
                        rawFileInfo.numberOfMS2scansWithIntactGlycoPep++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithIntactGlycoPep_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithIntactGlycoPep_etd++;
                    }


                    //print out the headers for each Y-ion searched for, with the last column being a ratio of total TIC we will calculate
                    if (rawFileInfo.firstSpectrumInFile)
                    {
                        outputYion.Write("ScanNumber\tPeptideSequence\tTotalGlycanComposition\tPrecursorMZ\tPrecursorCharge\tRetentionTime\t#ChargeStatesConsidered\tIonsFound\tScanInjTime\tDissociationType\tParentScan\tNumYions\tScanTIC\tTotalYionSignal\tYionTICfraction\t");
                        outputYion.WriteLine(string.Join("\t", yIonHeaderColumns));
                        rawFileInfo.firstSpectrumInFile = false;
                    }

                    //get some spectral features
                    double parentScan = spectrum.PrecursorMasterScanNumber;
                    double scanTIC = spectrum.TotalIonCurrent;
                    double scanInjTime = spectrum.IonInjectionTime;
                    string fragmentationType = "unknown";
                    if (spectrum.ScanFilter.Contains("etd") || spectrum.Precursors[0].FramentationMethod.ToString().Contains("ETD"))
                        fragmentationType = "ETD";
                    if (spectrum.ScanFilter.Contains("hcd") || spectrum.Precursors[0].FramentationMethod.ToString().Contains("HCD"))
                        fragmentationType = "HCD";

                    double retentionTime = spectrum.RetentionTime;

                    //calculate fraction of TIC
                    double yIonTICfraction = totalYionSignal / scanTIC;

                    //get all the charge states found into a string
                    string chargeStatesFinal = "";
                    foreach (Yion yion in finalYionList)
                        //should look like: 'Pep (Y0):3,4;203.0794, Pep+[HexNAc]:1;4066.1588, Pep+[HexNAc2]:1,2;'
                        chargeStatesFinal += yion.description + ":" + String.Join(",", yion.chargeStates) + ";";


                    //print out information for this scan that is not Y-ions
                    outputYion.Write(psm.spectrumNumber + "\t" + psm.peptideNoGlycanMods.SequenceWithModifications + "\t" +
                        psm.totalGlycanComposition + "\t" + spectrum.Precursors[0].MonoisotopicMz + "\t" + psm.charge + "\t" + retentionTime + "\t" + numberOfChargeStatesConsidered + "\t" + chargeStatesFinal + "\t" +
                        scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfYions + "\t" + scanTIC + "\t" + totalYionSignal + "\t" + yIonTICfraction + "\t");

                    var yIonIntensityDict = new Dictionary<(string, int), double>();
                    foreach (var yion in finalYionList)
                    {
                        for (int i = 0; i < yion.chargeStates.Count; i++)
                        {
                            var key = (yion.description, yion.chargeStates[i]);
                            yIonIntensityDict[key] = yion.intensities[i];
                        }
                    }

                    //write out peak depth and intensity info for each found Y-ion
                    foreach (var (yIon, charge) in yIonChargeStatePairs)
                    {
                        double intensity = 0;
                        if (yNsettings.condenseChargeStates)
                        {
                            intensity = finalYionList
                                .Where(y => y.description == yIon.description)
                                .SelectMany(y => y.intensities)
                                .Sum();
                        }
                        else
                        {
                            yIonIntensityDict.TryGetValue((yIon.description, charge), out intensity);
                        }
                        outputYion.Write($"{intensity}\t");
                    }

                    //ipsa output stuff
                    if (outputIPSA != null)
                    {
                        var seen = new HashSet<string>();
                        foreach (Yion yion in finalYionList)
                        {
                            for (int i = 0; i < yion.intensities.Count; i++)
                            {
                                int chargestate = yion.chargeStates[i];
                                double mz = yion.mz[i];
                                string yionName = yion.description + "+" + chargestate;
                                double massDiff = 0;
                                if (yion.glycanSource.Contains("Subtraction"))
                                {
                                    double theomass = (spectrum.Precursors[0].MonoisotopicMz * psm.charge) - (psm.charge * Constants.Proton) - yion.theoMass;
                                    massDiff = theomass - ((mz * chargestate) - (chargestate * Constants.Proton));
                                }
                                else
                                {
                                    double mass = psm.peptideNoGlycanMods.MonoisotopicMass + yion.theoMass;
                                    massDiff = mass - ((mz * chargestate) - (chargestate * Constants.Proton));
                                }

                                // Use a string key for uniqueness (you can use a tuple if you prefer)
                                string key = $"{psm.spectrumNumber}|{yionName}|{mz:F6}|{massDiff:F6}";
                                if (seen.Add(key))
                                {
                                    outputIPSA.WriteLine($"{psm.spectrumNumber}\t{yionName}\t{mz}\t{massDiff}");
                                }
                            }
                        }
                    }

                    outputYion.WriteLine();

                    if (numberOfYions > 0)
                    {
                        rawFileInfo.numberOfMS2scansWithYions++;
                        if (hcdTrue)
                            rawFileInfo.numberOfMS2scansWithYions_hcd++;
                        if (etdTrue)
                            rawFileInfo.numberOfMS2scansWithYions_etd++;
                    }

                }
                update.UpdateTimerYn();
            }

            // TODO: DELETE LOGGING - Write debug log to file
            debugLog.AppendLine("\n========== YNAUGHT DEBUG LOG END ==========");
            string logFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(yNsettings.rawFilePath) ?? "", "YNaught_Debug.log");
            System.IO.File.WriteAllText(logFilePath, debugLog.ToString());
            return (yNsettings, rawFileInfo);
        }
    }
}
