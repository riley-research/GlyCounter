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

            timer1.Interval = 1000;
            timer1.Tick += new EventHandler(OnTimerTick);
            timer1.Start();
            StatusLabel.Text = "Processing...";
            StartTimeLabel.Text = "Start Time: " + DateTime.Now.ToString("HH:mm:ss");

            try
            {
                await Task.Run(() =>
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
                        glySettings.usingda = true;
                        if (CanConvertDouble(ppmTol_textBox.Text, glySettings.daTolerance))
                        {
                            glySettings.daTolerance = Convert.ToDouble(ppmTol_textBox.Text, CultureInfo.InvariantCulture);
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
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "HexNAc", glySettings, true));

                    foreach (var item in HexCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Hex", glySettings));

                    foreach (var item in SialicAcidCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Sialic", glySettings));

                    foreach (var item in M6PCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "M6P", glySettings));

                    foreach (var item in OligosaccharideCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Oligo", glySettings));

                    foreach (var item in FucoseCheckedListBox.CheckedItems)
                        glySettings.oxoniumIonHashSet.Add(OxoniumIon.ProcessOxoIon(item, glycanSource: "Fucose", glySettings));

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

                            if (oxoIon.theoMZ == 204.0867 || oxoIon.description == "HexNAc")
                                glySettings.using204 = true;
                        }
                    }

                    if (ipsaCheckBox.Checked)
                        glySettings.ipsa = true;

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

                        if (StatusLabel.InvokeRequired)
                        {
                            StatusLabel.Invoke(new Action(() => StatusLabel.Text = "Current file: " + fileName));
                        }
                        else
                        {
                            StatusLabel.Text = "Current file: " + fileName;
                        }

                        if (FinishTimeLabel.InvokeRequired)
                        {
                            FinishTimeLabel.Invoke(new Action(() => FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
                        }
                        else
                        {
                            FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                        }

                        RawFileInfo rawFileInfo = new RawFileInfo
                        {
                            halfTotalList = (double)glySettings.oxoniumIonHashSet.Count / 2.0
                        };

                        //initialize streamwriter output files
                        string fileNameShort = Path.GetFileNameWithoutExtension(fileName);
                        StreamWriter outputOxo = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_OxoSignal.txt"));
                        StreamWriter outputPeakDepth = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_OxoPeakDepth.txt"));
                        StreamWriter outputIPSA = null;

                        if (ipsaCheckBox.Checked)
                        {
                            outputIPSA = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_Glycounter_IPSA.txt"));
                        }
                        StreamWriter outputSummary = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_Summary.txt"));

                        //write headers
                        outputOxo.Write("ScanNumber\tRetentionTime\tMSLevel\tPrecursorMZ\tNCE\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumOxonium\tTotalOxoSignal\t");
                        outputPeakDepth.Write("ScanNumber\tRetentionTime\tMSLevel\tPrecursorMZ\tNCE\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumOxonium\tTotalOxoSignal\t");
                        outputIPSA?.WriteLine("ScanNumber\tOxoniumIons\tMassError\t");

                        outputSummary.WriteLine("Settings:\t" + toleranceString + ", SNthreshold=" + glySettings.SNthreshold + ", IntensityThreshold=" + glySettings.intensityThreshold + ", PeakDepthThreshold_HCD=" + glySettings.peakDepthThreshold_hcd + ", PeakDepthThreshold_ETD=" + glySettings.peakDepthThreshold_etd + ", PeakDepthThreshold_UVPD=" + glySettings.peakDepthThreshold_uvpd
                                                + ", TICfraction_HCD=" + glySettings.oxoTICfractionThreshold_hcd + ", TICfraction_ETD=" + glySettings.oxoTICfractionThreshold_etd + ", TICfraction_UVPD=" + glySettings.oxoTICfractionThreshold_uvpd);
                        outputSummary.WriteLine(StartTimeLabel.Text);
                        outputSummary.WriteLine();

                        //start processing file
                        if (fileName.EndsWith(".d"))
                        {
                            (glySettings, rawFileInfo) = ProcessTimsTOF.processTimsTOF(fileName, glySettings, rawFileInfo, outputOxo, outputPeakDepth, outputIPSA);
                        }
                        else
                        {
                            (glySettings, rawFileInfo) = ProcessRaw_MzML.processRaw_MzML(fileName, glySettings, rawFileInfo, outputOxo, outputPeakDepth, outputIPSA);
                        }

                        //all scans have been processed, get some total stats
                        CalculatedRawFileInfo cRawFileInfo = new CalculatedRawFileInfo(rawFileInfo);
                        cRawFileInfo.numberofMS2scansWithOxo = Math.Max(0, cRawFileInfo.numberofMS2scansWithOxo);
                        cRawFileInfo.numberofHCDscansWithOxo = Math.Max(0, cRawFileInfo.numberofHCDscansWithOxo);
                        cRawFileInfo.numberofETDscansWithOxo = Math.Max(0, cRawFileInfo.numberofETDscansWithOxo);
                        cRawFileInfo.numberofUVPDscansWithOxo = Math.Max(0, cRawFileInfo.numberofUVPDscansWithOxo);

                        outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");
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

                        outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                        outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");

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
                                outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + oxoIon.glycanSource + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                                currentGlycanSource = oxoIon.glycanSource;
                            }

                            outputSummary.WriteLine(oxoIon.description + "\t" + total + "\t" + oxoIon.hcdCount + "\t" + oxoIon.etdCount + "\t" + oxoIon.uvpdCount
                                + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD + "\t" + percentUVPD);
                        }
                        outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                        //output elapsed time
                        stopwatch.Stop();
                        TimeSpan ts = stopwatch.Elapsed;

                        string elapsedTime = String.Format("{0:00}:{1:00}.{2:00}",
                            ts.Minutes, ts.Seconds,
                            ts.Milliseconds / 10);
                        outputSummary.WriteLine("Total search time: " + elapsedTime);

                        outputSummary.Close();
                        outputOxo.Close();
                        outputPeakDepth.Close();
                        if (outputIPSA != null)
                            outputIPSA.Close();
                    }
                });
            }
            finally
            {
                timer1.Stop();
                StatusLabel.Text = "Output to: " + glySettings.outputPath;
                FinishTimeLabel.Text = "Finished at: " + DateTime.Now.ToString("HH:mm:ss");
                MessageBox.Show("Finished.");
                glySettings.oxoniumIonHashSet.Clear();
            }

        }

        public void UpdateTimer()
        {
            if (FinishTimeLabel.InvokeRequired)
            {
                FinishTimeLabel.Invoke(new Action(() =>
                    FinishTimeLabel.Text =
                        "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
            }
            else
            {
                FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
            }
        }
    }
}
