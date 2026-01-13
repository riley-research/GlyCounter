using LumenWorks.Framework.IO.Csv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace GlyCounter
{
    public partial class Form1
    {
        private async void iC_startButton_Click(object sender, EventArgs e)
        {
            // If the user didn't enter an output path, default to the folder of the first uploaded raw file (no prompt).
            string userOutput = iC_outputTB.Text?.Trim();

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
                await Task.Run(async () =>
                {
                    glySettings.usingda = false;

                    if (string.IsNullOrEmpty(glySettings.outputPath) || !Directory.Exists(glySettings.outputPath))
                    {
                        if (glySettings.fileList.Count > 0)
                            glySettings.outputPath = Path.GetDirectoryName(glySettings.fileList[0]) ??
                                                     glySettings.defaultOutput;
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
                    if (iC_daCB.Checked)
                    {
                        glySettings.usingda = true;
                        if (CanConvertDouble(iC_toleranceTB.Text, glySettings.daTolerance))
                        {
                            glySettings.daTolerance =
                                Convert.ToDouble(iC_toleranceTB.Text, CultureInfo.InvariantCulture);
                        }
                    }
                    else if (CanConvertDouble(iC_toleranceTB.Text, glySettings.ppmTolerance))
                        glySettings.ppmTolerance = Convert.ToDouble(iC_toleranceTB.Text, CultureInfo.InvariantCulture);

                    glySettings.tol = glySettings.usingda ? glySettings.daTolerance : glySettings.ppmTolerance;
                    if (CanConvertDouble(iC_SNTB.Text, glySettings.SNthreshold))
                        glySettings.SNthreshold = Convert.ToDouble(iC_SNTB.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(iC_intensityTB.Text, glySettings.intensityThreshold))
                        glySettings.intensityThreshold = Convert.ToDouble(iC_intensityTB.Text,
                            CultureInfo.InvariantCulture);

                    if (CanConvertDouble(iC_singleIonMZ.Text, iCsettings.singleIonMz))
                        iCsettings.singleIonMz = Convert.ToDouble(iC_singleIonMZ.Text, CultureInfo.InvariantCulture);

                    glySettings.msLevelLB = iC_msLevelLow.Value;
                    glySettings.msLevelUB = iC_msLevelHigh.Value;
                    glySettings.ignoreMSLevel = iC_noMSnFilterCB.Checked;

                    string toleranceString = "ppmTol: ";
                    if (glySettings.usingda)
                        toleranceString = "DaTol: ";

                    //popup with settings to user
                    MessageBox.Show("You are using these settings:\r\n" + toleranceString + glySettings.tol + "\r\nSNthreshold: " +
                                    glySettings.SNthreshold + "\r\nIntensityTheshold: " + glySettings.intensityThreshold);

                    foreach (var item in iC_tmt11CBList.CheckedItems)
                        iCsettings._ionHashSet.Add(Ion.ProcessIon(item, source: "TMT11", glySettings));

                    foreach (var item in iC_acylCBList.CheckedItems)
                        iCsettings._ionHashSet.Add(Ion.ProcessIon(item, source: "Acyl-Lysine", glySettings));

                    foreach (var item in iC_tmt16CBList.CheckedItems)
                        iCsettings._ionHashSet.Add(Ion.ProcessIon(item, source: "TMT16", glySettings));

                    foreach (var item in iC_miscIonsCBList.CheckedItems)
                        iCsettings._ionHashSet.Add(Ion.ProcessIon(item, source: "Misc", glySettings));

                    if (!glySettings.csvCustomFile.Equals("empty"))
                    {
                        using StreamReader csvFile = new StreamReader(glySettings.csvCustomFile);

                        using var csv = new CsvReader(csvFile, true);
                        while (csv.ReadNextRecord())
                        {
                            string userDescription = csv["Description"];
                            Ion newIon = new Ion
                            {
                                theoMZ = double.Parse(csv["m/z"], CultureInfo.InvariantCulture),
                                description = double.Parse(csv["m/z"], CultureInfo.InvariantCulture) + ", " +
                                              userDescription,
                                ionSource = "Custom",
                                hcdCount = 0,
                                etdCount = 0,
                                uvpdCount = 0,
                                peakDepth = glySettings.arbitraryPeakDepthIfNotFound
                            };

                            //If an ion with the same theoretical m/z value exists, replace it with the one from the custom csv
                            List<Ion> ionsToRemove = new List<Ion>();
                            foreach (Ion ion in iCsettings._ionHashSet)
                            {
                                if (ion.Equals(newIon))
                                    ionsToRemove.Add(ion);
                            }

                            foreach (Ion ion in ionsToRemove)
                                iCsettings._ionHashSet.Remove(ion);

                            iCsettings._ionHashSet.Add(newIon);
                        }
                    }

                    if (iCsettings.singleIonMz != 0)
                    {
                        Ion ion = new Ion
                        {
                            theoMZ = iCsettings.singleIonMz,
                            description = iCsettings.singleIonMz + ", " + iC_singleIonDesc.Text,
                            ionSource = "Custom",
                            hcdCount = 0,
                            etdCount = 0,
                            uvpdCount = 0,
                            peakDepth = glySettings.arbitraryPeakDepthIfNotFound
                        };

                        //If an ion with the same theoretical m/z value exists, replace it with the one from the custom csv
                        List<Ion> ionsToRemove = [];
                        foreach (Ion checkedIon in iCsettings._ionHashSet)
                            if (ion.Equals(checkedIon))
                                iCsettings._ionHashSet.Remove(checkedIon);

                        iCsettings._ionHashSet.Add(ion);
                    }

                    if (iC_ipsaCB.Checked)
                        glySettings.ipsa = true;

                    foreach (var fileName in glySettings.fileList)
                    {
                        //reset ions
                        foreach (Ion ion in iCsettings._ionHashSet)
                        {
                            ion.intensity = 0;
                            ion.peakDepth = glySettings.arbitraryPeakDepthIfNotFound;
                            ion.hcdCount = 0;
                            ion.etdCount = 0;
                            ion.uvpdCount = 0;
                            ion.measuredMZ = 0;
                        }

                        if (StatusLabel.InvokeRequired)
                        {
                            StatusLabel.Invoke(new Action(() => iC_statusUpdatesLabel.Text = "Current file: " + fileName));
                        }
                        else
                        {
                            iC_statusUpdatesLabel.Text = "Current file: " + fileName;
                        }

                        if (iC_finishTimeLabel.InvokeRequired)
                        {
                            iC_finishTimeLabel.Invoke(new Action(() =>
                                iC_finishTimeLabel.Text = "Finish time: still running as of " +
                                                       DateTime.Now.ToString("HH:mm:ss")));
                        }
                        else
                        {
                            iC_finishTimeLabel.Text =
                                "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                        }

                        RawFileInfo rawFileInfo = new RawFileInfo();

                        //initialize streamwriter output files
                        string fileNameShort = Path.GetFileNameWithoutExtension(fileName);
                        StreamWriter outputSignal = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" +
                                                                               fileNameShort +
                                                                               "_iCounter_Signal.txt"));
                        StreamWriter outputPeakDepth = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" +
                            fileNameShort + "_iCounter_PeakDepth.txt"));
                        StreamWriter outputIPSA = null;

                        if (ipsaCheckBox.Checked)
                        {
                            outputIPSA = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort +
                                                                       "_iCounter_IPSA.txt"));
                        }

                        StreamWriter outputSummary = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" +
                            fileNameShort + "_iCounter_Summary.txt"));

                        //write headers
                        outputSignal.Write(
                            "ScanNumber\tMSLevel\tRetentionTime\tPrecursorMZ\tNCE\tScanTIC\tTotalFoundIonSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumIonsFound\tTotalFoundIonSignal\t");
                        outputPeakDepth.Write(
                            "ScanNumber\tMSLevel\tRetentionTime\tPrecursorMZ\tNCE\tScanTIC\tTotalFoundIonSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumIonsFound\tTotalFoundIonSignal\t");
                        outputIPSA?.WriteLine("ScanNumber\tIons\tMassError\t");
                        outputSummary.WriteLine("Settings:\t" + toleranceString + ", SNthreshold=" + glySettings.SNthreshold +
                                                ", IntensityThreshold=" + glySettings.intensityThreshold);
                        outputSummary.WriteLine("Started At: " + iC_startTimeLabel.Text);
                        outputSummary.WriteLine();

                        //start processing file
                        var progress = new Progress<DateTime>(_ => UpdateTimer()); //for timer updates on the UI thread
                        if (fileName.EndsWith(".d"))
                        {
                            (glySettings, rawFileInfo) = await iC_ProcessTimsTOF.iC_processTimsTOFAsync(fileName, glySettings, iCsettings,
                                rawFileInfo, outputSignal, outputPeakDepth, outputIPSA, progress);
                        }
                        else
                        {
                            (glySettings, rawFileInfo) = iC_ProcessRaw_MzML.processRaw_MzML(fileName, glySettings, iCsettings,
                                rawFileInfo, outputSignal, outputPeakDepth, outputIPSA);
                        }

                        //all scans have been processed, get some total stats
                        CalculatedRawFileInfo cRawFileInfo = new CalculatedRawFileInfo(rawFileInfo);
                        cRawFileInfo.numberofMS2scansWithOxo = Math.Max(0, cRawFileInfo.numberofMS2scansWithOxo);
                        cRawFileInfo.numberofHCDscansWithOxo = Math.Max(0, cRawFileInfo.numberofHCDscansWithOxo);
                        cRawFileInfo.numberofETDscansWithOxo = Math.Max(0, cRawFileInfo.numberofETDscansWithOxo);
                        cRawFileInfo.numberofUVPDscansWithOxo = Math.Max(0, cRawFileInfo.numberofUVPDscansWithOxo);

                        outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");
                        outputSummary.WriteLine("MSn Scans\t" + rawFileInfo.numberOfMS2scans + "\t" + rawFileInfo.numberOfHCDscans + "\t" +
                                                rawFileInfo.numberOfETDscans + "\t" + rawFileInfo.numberOfUVPDscans
                                                + "\t" + 100 + "\t" + cRawFileInfo.percentageHCD + "\t" + cRawFileInfo.percentageETD + "\t" +
                                                cRawFileInfo.percentageUVPD);
                        outputSummary.WriteLine("MSn Scans with Found Ions\t" + cRawFileInfo.numberofMS2scansWithOxo + "\t" +
                                                cRawFileInfo.numberofHCDscansWithOxo + "\t" + cRawFileInfo.numberofETDscansWithOxo + "\t" +
                                                cRawFileInfo.numberofUVPDscansWithOxo
                                                + "\t" + cRawFileInfo.percentageSum + "\t" + cRawFileInfo.percentageSum_hcd + "\t" +
                                                cRawFileInfo.percentageSum_etd + "\t" + cRawFileInfo.percentageSum_uvpd);
                        outputSummary.WriteLine("IonCount_1\t" + rawFileInfo.numberOfMS2scansWithOxo_1 + "\t" +
                                                rawFileInfo.numberOfMS2scansWithOxo_1_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_1_etd +
                                                "\t" + rawFileInfo.numberOfMS2scansWithOxo_1_uvpd
                                                + "\t" + cRawFileInfo.percentage1ox + "\t" + cRawFileInfo.percentage1ox_hcd + "\t" +
                                                cRawFileInfo.percentage1ox_etd + "\t" + cRawFileInfo.percentage1ox_uvpd);
                        outputSummary.WriteLine("IonCount_2\t" + rawFileInfo.numberOfMS2scansWithOxo_2 + "\t" +
                                                rawFileInfo.numberOfMS2scansWithOxo_2_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_2_etd +
                                                "\t" + rawFileInfo.numberOfMS2scansWithOxo_2_uvpd
                                                + "\t" + cRawFileInfo.percentage2ox + "\t" + cRawFileInfo.percentage2ox_hcd + "\t" +
                                                cRawFileInfo.percentage2ox_etd + "\t" + cRawFileInfo.percentage2ox_uvpd);
                        outputSummary.WriteLine("IonCount_3\t" + rawFileInfo.numberOfMS2scansWithOxo_3 + "\t" +
                                                rawFileInfo.numberOfMS2scansWithOxo_3_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_3_etd +
                                                "\t" + rawFileInfo.numberOfMS2scansWithOxo_3_uvpd
                                                + "\t" + cRawFileInfo.percentage3ox + "\t" + cRawFileInfo.percentage3ox_hcd + "\t" +
                                                cRawFileInfo.percentage3ox_etd + "\t" + cRawFileInfo.percentage3ox_uvpd);
                        outputSummary.WriteLine("IonCount_4\t" + rawFileInfo.numberOfMS2scansWithOxo_4 + "\t" +
                                                rawFileInfo.numberOfMS2scansWithOxo_4_hcd + "\t" + rawFileInfo.numberOfMS2scansWithOxo_4_etd +
                                                "\t" + rawFileInfo.numberOfMS2scansWithOxo_4_uvpd
                                                + "\t" + cRawFileInfo.percentage4ox + "\t" + cRawFileInfo.percentage4ox_hcd + "\t" +
                                                cRawFileInfo.percentage4ox_etd + "\t" + cRawFileInfo.percentage4ox_uvpd);
                        outputSummary.WriteLine("IonCount_5+\t" + rawFileInfo.numberOfMS2scansWithOxo_5plus + "\t" +
                                                rawFileInfo.numberOfMS2scansWithOxo_5plus_hcd + "\t" +
                                                rawFileInfo.numberOfMS2scansWithOxo_5plus_etd + "\t" +
                                                rawFileInfo.numberOfMS2scansWithOxo_5plus_uvpd
                                                + "\t" + cRawFileInfo.percentage5plusox + "\t" + cRawFileInfo.percentage5plusox_hcd + "\t" +
                                                cRawFileInfo.percentage5plusox_etd + "\t" + cRawFileInfo.percentage5plusox_uvpd);

                        outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                        outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");

                        string currentSource = "";
                        foreach (Ion ion in iCsettings._ionHashSet)
                        {
                            int total = ion.hcdCount + ion.etdCount + ion.uvpdCount;

                            double percentTotal = (double)total / (double)rawFileInfo.numberOfMS2scans * 100;
                            double percentHCD = (double)ion.hcdCount / (double)rawFileInfo.numberOfHCDscans * 100;
                            double percentETD = (double)ion.etdCount / (double)rawFileInfo.numberOfETDscans * 100;
                            double percentUVPD = (double)ion.uvpdCount / (double)rawFileInfo.numberOfUVPDscans * 100;

                            if (!currentSource.Equals(ion.ionSource))
                            {
                                outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + ion.ionSource +
                                                        @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                        @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" +
                                                        "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                        @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                                currentSource = ion.ionSource;
                            }

                            outputSummary.WriteLine(ion.description + "\t" + total + "\t" + ion.hcdCount + "\t" +
                                                    ion.etdCount + "\t" + ion.uvpdCount
                                                    + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD +
                                                    "\t" + percentUVPD);
                        }

                        outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                        //output elapsed time
                        stopwatch.Stop();
                        TimeSpan ts = stopwatch.Elapsed;

                        string elapsedTime = $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
                        outputSummary.WriteLine("Total search time: " + elapsedTime);

                        outputSummary.Close();
                        outputSignal.Close();
                        outputPeakDepth.Close();
                        outputIPSA?.Close();
                    }
                });
            }

            finally
            {
                timer1.Stop();
                iC_statusUpdatesLabel.Text = @"Finished";
                iC_finishTimeLabel.Text = @"Finished at: " + DateTime.Now.ToString("HH:mm:ss");
                MessageBox.Show(@"Finished.");
                iCsettings._ionHashSet.Clear();
            }

        }

        public void iC_UpdateTimer()
        {
            if (iC_finishTimeLabel.InvokeRequired)
            {
                iC_finishTimeLabel.Invoke(new Action(() =>
                    iC_finishTimeLabel.Text =
                        "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
            }
            else
            {
                iC_finishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
            }
        }
    }
}
