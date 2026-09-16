using LumenWorks.Framework.IO.Csv;
using Nova.Io.Read;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        private async void StartButton_Click(object sender, EventArgs e)
        {
            // If the user didn't enter an output path, default to the folder of the first uploaded raw file (no prompt).
            string userOutput = Gly_outputTextBox.Text?.Trim();

            GlyCounterSettings? getOutput = DefaultOutput.getDefaultOutput(userOutput, glySettings);
            if (getOutput != null)
            {
                glySettings = getOutput;
                if (Gly_outputTextBox.InvokeRequired)
                    Gly_outputTextBox.Invoke(new Action(() => Gly_outputTextBox.Text = glySettings.outputPath));
                else
                    Gly_outputTextBox.Text = glySettings.outputPath;
            }
            else return;

            string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogMessage($"Processing {glySettings.fileList.Count} files");

            try
            {
                await Task.Run(async () =>
                {
                    glySettings.usingda = false;
                    glySettings.using204 = false;

                    if (string.IsNullOrEmpty(glySettings.outputPath) || !Directory.Exists(glySettings.outputPath))
                    {
                        if (glySettings.fileList.Count > 0)
                            glySettings.outputPath = Path.GetDirectoryName(glySettings.fileList[0]) ?? glySettings.defaultOutput;
                        else
                            glySettings.outputPath = glySettings.defaultOutput;

                        if (Gly_outputTextBox.InvokeRequired)
                            Gly_outputTextBox.Invoke(new Action(() => Gly_outputTextBox.Text = glySettings.outputPath));
                        else
                            Gly_outputTextBox.Text = glySettings.outputPath;
                    }

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    //make sure all user inputs are in the correct format, otherwise use defaults
                    if (DaltonCheckBox.Checked)
                    {
                        if (CanConvertDouble(ppmTol_textBox.Text, glySettings.daTolerance))
                        {
                            glySettings.daTolerance = Convert.ToDouble(ppmTol_textBox.Text, CultureInfo.InvariantCulture);
                            glySettings.usingda = true;
                        }

                    }
                    else
                        if (CanConvertDouble(ppmTol_textBox.Text, glySettings.ppmTolerance))
                        glySettings.ppmTolerance = Convert.ToDouble(ppmTol_textBox.Text, CultureInfo.InvariantCulture);

                    glySettings.tol = glySettings.usingda ? glySettings.daTolerance : glySettings.ppmTolerance;

                    if (CanConvertDouble(SN_textBox.Text, glySettings.SNthreshold))
                        glySettings.SNthreshold = Convert.ToDouble(SN_textBox.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(PeakDepth_Box_HCD.Text, glySettings.peakDepthThreshold_hcd))
                        glySettings.peakDepthThreshold_hcd = Convert.ToDouble(PeakDepth_Box_HCD.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(PeakDepth_Box_ETD.Text, glySettings.peakDepthThreshold_etd))
                        glySettings.peakDepthThreshold_etd = Convert.ToDouble(PeakDepth_Box_ETD.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(PeakDepth_Box_UVPD.Text, glySettings.peakDepthThreshold_uvpd))
                        glySettings.peakDepthThreshold_uvpd = Convert.ToDouble(PeakDepth_Box_UVPD.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(hcdTICfraction.Text, glySettings.oxoTICfractionThreshold_hcd))
                        glySettings.oxoTICfractionThreshold_hcd = Convert.ToDouble(hcdTICfraction.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(etdTICfraction.Text, glySettings.oxoTICfractionThreshold_etd))
                        glySettings.oxoTICfractionThreshold_etd = Convert.ToDouble(etdTICfraction.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(uvpdTICfraction.Text, glySettings.oxoTICfractionThreshold_uvpd))
                        glySettings.oxoTICfractionThreshold_uvpd = Convert.ToDouble(uvpdTICfraction.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(OxoCountRequireBox_hcd.Text, glySettings.oxoCountRequirement_hcd_user))
                        glySettings.oxoCountRequirement_hcd_user = Convert.ToDouble(OxoCountRequireBox_hcd.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(OxoCountRequireBox_etd.Text, glySettings.oxoCountRequirement_etd_user))
                        glySettings.oxoCountRequirement_etd_user = Convert.ToDouble(OxoCountRequireBox_etd.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(OxoCountRequireBox_uvpd.Text, glySettings.oxoCountRequirement_uvpd_user))
                        glySettings.oxoCountRequirement_uvpd_user = Convert.ToDouble(OxoCountRequireBox_uvpd.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(intensityThresholdTextBox.Text, glySettings.intensityThreshold))
                        glySettings.intensityThreshold = Convert.ToDouble(intensityThresholdTextBox.Text, CultureInfo.InvariantCulture);

                    glySettings.msLevelLB = MSLevelLB.Value;
                    glySettings.msLevelUB = MSLevelUB.Value;
                    glySettings.ignoreMSLevel = ignoreMSLevelCB.Checked;

                    string toleranceString = "ppmTol: ";
                    if (glySettings.usingda)
                        toleranceString = "DaTol: ";

                    //popup with settings to user
                    MessageBox.Show("You are using these settings:\r\n" + toleranceString + glySettings.tol + "\r\nSNthreshold: " + glySettings.SNthreshold + "\r\nIntensityTheshold: " + glySettings.intensityThreshold
                        + "\r\nPeakDepthThreshold_HCD: " + glySettings.peakDepthThreshold_hcd + "\r\nPeakDepthThreshold_ETD: " + glySettings.peakDepthThreshold_etd + "\r\nPeakDepthThreshold_UVPD: " + glySettings.peakDepthThreshold_uvpd
                        + "\r\nTICfraction_HCD: " + glySettings.oxoTICfractionThreshold_hcd + "\r\nTICfraction_ETD: " + glySettings.oxoTICfractionThreshold_etd + "\r\nTICfraction_UVPD: " + glySettings.oxoTICfractionThreshold_uvpd);


                    foreach (var item in HexNAcCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "HexNAc", glySettings, check204:true));

                    foreach (var item in HexCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Hex", glySettings, check163:true));

                    foreach (var item in SialicAcidCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Sialic", glySettings));

                    foreach (var item in M6PCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "M6P", glySettings));

                    foreach (var item in OligosaccharideCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Oligo", glySettings, check366:true));

                    foreach (var item in FucoseCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Fucose", glySettings));

                    foreach (var item in ImmoniumCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Immonium", glySettings));

                    if (!glySettings.csvCustomFile.Equals("empty"))
                    {
                        using StreamReader csvFile = new StreamReader(glySettings.csvCustomFile);

                        using var csv = new CsvReader(csvFile, true);
                        while (csv.ReadNextRecord())
                        {
                            OxoniumIon oxoIon = new OxoniumIon();
                            oxoIon.theoMZ = double.Parse(csv["m/z"], CultureInfo.InvariantCulture);
                            string userDescription = csv["Description"];
                            oxoIon.description = double.Parse(csv["m/z"], CultureInfo.InvariantCulture) + ", " + userDescription;
                            oxoIon.glycanSource = "Custom";
                            oxoIon.hcdCount = 0;
                            oxoIon.etdCount = 0;
                            oxoIon.uvpdCount = 0;
                            oxoIon.peakDepth = glySettings.arbitraryPeakDepthIfNotFound;

                            //If an oxonium ion with the same theoretical m/z value exists, replace it with the one from the custom csv
                            List<OxoniumIon> ionsToRemove = new List<OxoniumIon>();
                            foreach (OxoniumIon ion in glySettings.oxoniumIonHashSet)
                            {
                                if (ion.Equals(oxoIon))
                                    ionsToRemove.Add(ion);
                            }
                            foreach (OxoniumIon ion in ionsToRemove)
                                glySettings.oxoniumIonHashSet.Remove(ion);

                            glySettings.oxoniumIonHashSet.Add(oxoIon);

                            if (oxoIon.theoMZ == 366.1395 || oxoIon.description == "HexNAc-Hex")
                                glySettings.using366 = true;
                            if (oxoIon.theoMZ == 204.0867 || oxoIon.description == "HexNAc")
                                glySettings.using204 = true;
                            if (oxoIon.theoMZ == 163.0601 || oxoIon.description == "Hex")
                                glySettings.using163 = true;
                        }
                    }

                    if (periscopeCheckBox.Checked)
                        glySettings.periscope = true;

                    foreach (var fileName in glySettings.fileList)
                    {
                        //reset oxonium ions
                        foreach (OxoniumIon oxoIon in glySettings.oxoniumIonHashSet)
                        {
                            oxoIon.intensity = 0;
                            oxoIon.peakDepth = glySettings.arbitraryPeakDepthIfNotFound;

                            oxoIon.hcdCount = 0;
                            oxoIon.etdCount = 0;
                            oxoIon.uvpdCount = 0;
                            oxoIon.measuredMZ = 0;
                        }

                        LogMessage($"Current file: {fileName}");

                        RawFileInfo rawFileInfo = new();

                        //initialize streamwriter output files
                        string fileNameShort = Path.GetFileNameWithoutExtension(fileName);
                        using (StreamWriter outputOxo = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_OxoSignal.txt")))
                        using (StreamWriter outputPeakDepth = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_OxoPeakDepth.txt")))
                        using (StreamWriter outputPeriscope = periscopeCheckBox.Checked ? new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_Glycounter_Periscope.txt")) : null)
                        using (StreamWriter outputSummary = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_Summary.txt")))
                        {
                            //write headers
                            outputPeriscope?.WriteLine(FileHeaders.PeriscopeHeader);
                            outputSummary.WriteLine("Settings:\t" + toleranceString + glySettings.tol + ", SNthreshold=" + glySettings.SNthreshold + ", IntensityThreshold=" + glySettings.intensityThreshold + ", PeakDepthThreshold_HCD=" + glySettings.peakDepthThreshold_hcd + ", PeakDepthThreshold_ETD=" + glySettings.peakDepthThreshold_etd + ", PeakDepthThreshold_UVPD=" + glySettings.peakDepthThreshold_uvpd
                                                    + ", TICfraction_HCD=" + glySettings.oxoTICfractionThreshold_hcd + ", TICfraction_ETD=" + glySettings.oxoTICfractionThreshold_etd + ", TICfraction_UVPD=" + glySettings.oxoTICfractionThreshold_uvpd);
                            outputSummary.WriteLine(startTime);
                            outputSummary.WriteLine();

                            //start processing file
                            if (fileName.EndsWith(".d"))
                            {
                                var timsProcessor = new SpectrumProcessor<SpectrumInfo.TimsSpectrumInfo>(new TimsFileReader());
                                foreach (string headerval in TimsFileReader.GetOutputHeaders())
                                {
                                    outputOxo.Write(headerval + '\t');
                                    outputPeakDepth.Write(headerval + '\t');

                                }

                                var timsSource = new TimsFileReader();
                                var processor = new SpectrumProcessor<SpectrumInfo.TimsSpectrumInfo>(timsSource);
                                var (settings, info) = await processor.ProcessRawFileAsync(
                                    fileName, glySettings, rawFileInfo, outputOxo, outputPeakDepth, outputPeriscope);
                            }
                            else
                            {
                                foreach (string headerval in RawMzMLReader.GetOutputHeaders())
                                {
                                    outputOxo.Write(headerval + '\t');
                                    outputPeakDepth.Write(headerval + '\t');

                                }

                                var rawSource = new RawMzMLReader();
                                var processor = new SpectrumProcessor<SpectrumInfo.RawMzmlSpectrumInfo>(rawSource);
                                var (settings, info) = await processor.ProcessRawFileAsync(
                                    fileName, glySettings, rawFileInfo, outputOxo, outputPeakDepth, outputPeriscope);
                            }

                            //all scans have been processed, get some total stats
                            CalculatedRawFileInfo cRawFileInfo = new CalculatedRawFileInfo(rawFileInfo);
                            cRawFileInfo.numberofMS2scansWithOxo = Math.Max(0, cRawFileInfo.numberofMS2scansWithOxo);
                            cRawFileInfo.numberofHCDscansWithOxo = Math.Max(0, cRawFileInfo.numberofHCDscansWithOxo);
                            cRawFileInfo.numberofETDscansWithOxo = Math.Max(0, cRawFileInfo.numberofETDscansWithOxo);
                            cRawFileInfo.numberofUVPDscansWithOxo = Math.Max(0, cRawFileInfo.numberofUVPDscansWithOxo);

                            outputSummary.WriteLine(FileHeaders.SummaryDissociationHeader);
                            outputSummary.WriteLine("MS/MS Scans\t" + rawFileInfo.numberOfMS2scans + "\t" + rawFileInfo.numberOfHCDscans + "\t" + rawFileInfo.numberOfETDscans + "\t" + rawFileInfo.numberOfUVPDscans
                                + "\t" + 100 + "\t" + cRawFileInfo.percentageHCD + "\t" + cRawFileInfo.percentageETD + "\t" + cRawFileInfo.percentageUVPD);
                            outputSummary.WriteLine("MS/MS Scans with OxoIons\t" + cRawFileInfo.numberofMS2scansWithOxo + "\t" + cRawFileInfo.numberofHCDscansWithOxo + "\t" + cRawFileInfo.numberofETDscansWithOxo + "\t" + cRawFileInfo.numberofUVPDscansWithOxo
                                + "\t" + cRawFileInfo.percentageSum + "\t" + cRawFileInfo.percentageSum_hcd + "\t" + cRawFileInfo.percentageSum_etd + "\t" + cRawFileInfo.percentageSum_uvpd);
                            outputSummary.WriteLine("Likely Glyco\t" + cRawFileInfo.numberScansCountedLikelyGlyco_total + "\t" + rawFileInfo.numberScansCountedLikelyGlyco_hcd + "\t" + rawFileInfo.numberScansCountedLikelyGlyco_etd + "\t" + rawFileInfo.numberScansCountedLikelyGlyco_uvpd
                                + "\t" + cRawFileInfo.percentageLikelyGlyco_total + "\t" + cRawFileInfo.percentageLikelyGlyco_hcd + "\t" + cRawFileInfo.percentageLikelyGlyco_etd + "\t" + cRawFileInfo.percentageLikelyGlyco_uvpd);
                            outputSummary.WriteLine("OxoCount_1\t" + rawFileInfo.numberOfMS2scansWithOxo_1 + "\t" + rawFileInfo.numberOfMS2scansWithOxo_1_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_1_etd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_1_uvpd
                                + "\t" + cRawFileInfo.percentage1ox + "\t" + cRawFileInfo.percentage1ox_hcd + "\t" + cRawFileInfo.percentage1ox_etd + "\t" + cRawFileInfo.percentage1ox_uvpd);
                            outputSummary.WriteLine("OxoCount_2\t" + rawFileInfo.numberOfMS2scansWithOxo_2 + "\t" + rawFileInfo.numberOfMS2scansWithOxo_2_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_2_etd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_2_uvpd
                                + "\t" + cRawFileInfo.percentage2ox + "\t" + cRawFileInfo.percentage2ox_hcd + "\t" + cRawFileInfo.percentage2ox_etd + "\t" + cRawFileInfo.percentage2ox_uvpd);
                            outputSummary.WriteLine("OxoCount_3\t" + rawFileInfo.numberOfMS2scansWithOxo_3 + "\t" + rawFileInfo.numberOfMS2scansWithOxo_3_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_3_etd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_3_uvpd
                                + "\t" + cRawFileInfo.percentage3ox + "\t" + cRawFileInfo.percentage3ox_hcd + "\t" + cRawFileInfo.percentage3ox_etd + "\t" + cRawFileInfo.percentage3ox_uvpd);
                            outputSummary.WriteLine("OxoCount_4\t" + rawFileInfo.numberOfMS2scansWithOxo_4 + "\t" + rawFileInfo.numberOfMS2scansWithOxo_4_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_4_etd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_4_uvpd
                                + "\t" + cRawFileInfo.percentage4ox + "\t" + cRawFileInfo.percentage4ox_hcd + "\t" + cRawFileInfo.percentage4ox_etd + "\t" + cRawFileInfo.percentage4ox_uvpd);
                            outputSummary.WriteLine("OxoCount_5+\t" + rawFileInfo.numberOfMS2scansWithOxo_5plus + "\t" + rawFileInfo.numberOfMS2scansWithOxo_5plus_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_5plus_etd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_5plus_uvpd
                                + "\t" + cRawFileInfo.percentage5plusox + "\t" + cRawFileInfo.percentage5plusox_hcd + "\t" + cRawFileInfo.percentage5plusox_etd + "\t" + cRawFileInfo.percentage5plusox_uvpd);

                            outputSummary.WriteLine(FileHeaders.SummarySeparator1);
                            outputSummary.WriteLine(FileHeaders.SummaryDissociationHeader);

                            string currentGlycanSource = "";

                            foreach (OxoniumIon oxoIon in glySettings.oxoniumIonHashSet)
                            {
                                int total = oxoIon.hcdCount + oxoIon.etdCount + oxoIon.uvpdCount;

                                double percentTotal = (double)total / (double)rawFileInfo.numberOfMS2scans * 100;
                                double percentHCD = (double)oxoIon.hcdCount / (double)rawFileInfo.numberOfHCDscans * 100;
                                double percentETD = (double)oxoIon.etdCount / (double)rawFileInfo.numberOfETDscans * 100;
                                double percentUVPD = (double)oxoIon.uvpdCount / (double)rawFileInfo.numberOfUVPDscans * 100;

                                if (!currentGlycanSource.Equals(oxoIon.glycanSource))
                                {
                                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + oxoIon.glycanSource + FileHeaders.SummarySeparator2);
                                    currentGlycanSource = oxoIon.glycanSource;
                                }

                                outputSummary.WriteLine(oxoIon.description + "\t" + total + "\t" + oxoIon.hcdCount + "\t" + oxoIon.etdCount + "\t" + oxoIon.uvpdCount
                                    + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD + "\t" + percentUVPD);
                            }
                            outputSummary.WriteLine(FileHeaders.SummarySeparator3);
                            //output elapsed time
                            stopwatch.Stop();
                            TimeSpan ts = stopwatch.Elapsed;

                            string elapsedTime = String.Format("{0:00}:{1:00}.{2:00}",
                                ts.Minutes, ts.Seconds,
                                ts.Milliseconds / 10);
                            outputSummary.WriteLine("Total search time: " + elapsedTime);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                timer1.Stop();
                LogMessage("Error during processing");

                string errorMessage = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show("An error occurred during processing:\r\n\r\n" + errorMessage, "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                glySettings.oxoniumIonHashSet.Clear();
                return;
            }
            finally
            {
                timer1.Stop();
                LogMessage("Output to: " + glySettings.outputPath);
                LogMessage("Finished Processing");
                MessageBox.Show("Finished.");
                glySettings.oxoniumIonHashSet.Clear();
            }

        }
    }
}
