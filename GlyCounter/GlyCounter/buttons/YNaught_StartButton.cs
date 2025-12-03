using CSMSL.Proteomics;
using CSMSL;
using LumenWorks.Framework.IO.Csv;
using Nova.Data;
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
        //start the Y-ion processing
        private async void Ynaught_StartButton_Click(object sender, EventArgs e)
        {
            GlyCounterSettings? getOutput = DefaultOutput.getDefaultOutput(Ynaught_outputTextBox.Text, glySettings);
            //TODO check if message box shows up if you don't select a folder
            if (getOutput != null)
            {
                glySettings = getOutput;
                if (Gly_outputTextBox.InvokeRequired)
                    Gly_outputTextBox.Invoke(new Action(() => Gly_outputTextBox.Text = glySettings.outputPath));
                else
                    Gly_outputTextBox.Text = glySettings.outputPath;
            }
            else return;

            timer2.Interval = 1000;
            timer2.Tick += new EventHandler(OnTimerTick);
            timer2.Start();
            Ynaught_startTimeLabel.Text = "Start Time: " + DateTime.Now.ToString("HH:mm:ss");

            try
            {
                await Task.Run(() =>
                {
                    yNsettings.usingda = false;

                    //make sure output path is real otherwise set to default
                    if (string.IsNullOrEmpty(glySettings.outputPath) || !Directory.Exists(glySettings.outputPath))
                    {
                        if (!string.IsNullOrEmpty(yNsettings.rawFilePath) && File.Exists(yNsettings.rawFilePath))
                        {
                            if (string.IsNullOrEmpty(glySettings.outputPath) || !Directory.Exists(glySettings.outputPath))
                                glySettings.outputPath = Path.GetDirectoryName(yNsettings.rawFilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                            glySettings.outputPath = Path.GetDirectoryName(yNsettings.rawFilePath) ?? glySettings.defaultOutput;
                            if (Ynaught_outputTextBox.InvokeRequired)
                            {
                                Ynaught_outputTextBox.Invoke(new Action(() => Ynaught_outputTextBox.Text = glySettings.outputPath));
                            }
                            else
                            {
                                Ynaught_outputTextBox.Text = glySettings.outputPath;
                            }
                        }
                        else
                        {
                            glySettings.outputPath = glySettings.defaultOutput;
                            if (Ynaught_outputTextBox.InvokeRequired)
                            {
                                Ynaught_outputTextBox.Invoke(new Action(() => Ynaught_outputTextBox.Text = glySettings.outputPath));
                            }
                            else
                            {
                                Ynaught_outputTextBox.Text = glySettings.outputPath;
                            }
                        }
                    }

                    Stopwatch stopwatch2 = new Stopwatch();
                    stopwatch2.Start();


                    //clear out Y-ions
                    yNsettings.yIonHashSet = new HashSet<Yion>();

                    //either take in custom values or use defaults
                    if (Ynaught_DaCheckBox.Checked)
                    {
                        if (CanConvertDouble(Ynaught_ppmTolTextBox.Text, yNsettings.daTolerance))
                            yNsettings.daTolerance = Convert.ToDouble(Ynaught_ppmTolTextBox.Text, CultureInfo.InvariantCulture);
                        yNsettings.usingda = true;
                    }
                    else
                    {
                        if (CanConvertDouble(Ynaught_ppmTolTextBox.Text, yNsettings.ppmTolerance))
                            yNsettings.ppmTolerance = Convert.ToDouble(Ynaught_ppmTolTextBox.Text, CultureInfo.InvariantCulture);
                    }

                    if (yNsettings.usingda)
                        yNsettings.tol = yNsettings.daTolerance;
                    else
                        yNsettings.tol = yNsettings.ppmTolerance;

                    if (CanConvertDouble(Ynaught_SNthresholdTextBox.Text, yNsettings.SNthreshold))
                        yNsettings.SNthreshold = Convert.ToDouble(Ynaught_SNthresholdTextBox.Text, CultureInfo.InvariantCulture);

                    if (YNaught_IPSAcheckbox.Checked) yNsettings.ipsa = true;

                    //add checked items to yIonHashSet to use for creating ions to look for
                    //note that subtraction is its own "Source"
                    foreach (var item in Yions_NlinkedCheckBox.CheckedItems)
                    {
                        string[] yIonArray = item.ToString().Split(',');
                        Yion yIon = new Yion();
                        yIon.theoMass = Convert.ToDouble(yIonArray[0], CultureInfo.InvariantCulture);
                        yIon.description = item.ToString();
                        yIon.glycanSource = "Nglycan";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
                        if (item.ToString().Contains("Y0"))
                        {
                            //stops Y0 from being added to the hashset multiple times
                            bool hashSetContainsY0 = false;
                            foreach (Yion entry in yNsettings.yIonHashSet)
                            {
                                if (entry.description.Contains("Y0"))
                                {
                                    hashSetContainsY0 = true;
                                }
                            }
                            if (!hashSetContainsY0)
                                yNsettings.yIonHashSet.Add(yIon);
                        }
                        else
                        {
                            yNsettings.yIonHashSet.Add(yIon);
                        }

                    }

                    foreach (var item in Yions_FucoseNlinkedCheckedBox.CheckedItems)
                    {
                        string[] yIonArray = item.ToString().Split(',');
                        Yion yIon = new Yion();
                        yIon.theoMass = Convert.ToDouble(yIonArray[0], CultureInfo.InvariantCulture);
                        yIon.description = item.ToString();
                        yIon.glycanSource = "Nglycan_Fucose";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
                        if (item.ToString().Contains("Y0"))
                        {
                            bool hashSetContainsY0 = false;
                            foreach (Yion entry in yNsettings.yIonHashSet)
                            {
                                if (entry.description.Contains("Y0"))
                                {
                                    hashSetContainsY0 = true;
                                }
                            }
                            if (!hashSetContainsY0)
                                yNsettings.yIonHashSet.Add(yIon);
                        }
                        else
                        {
                            yNsettings.yIonHashSet.Add(yIon);
                        }
                    }

                    foreach (var item in Yions_OlinkedChecklistBox.CheckedItems)
                    {
                        string[] yIonArray = item.ToString().Split(',');
                        Yion yIon = new Yion();
                        yIon.theoMass = Convert.ToDouble(yIonArray[0], CultureInfo.InvariantCulture);
                        yIon.description = item.ToString();
                        yIon.glycanSource = "Oglycan";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
                        if (item.ToString().Contains("Y0"))
                        {
                            bool hashSetContainsY0 = false;
                            foreach (Yion entry in yNsettings.yIonHashSet)
                            {
                                if (entry.description.Contains("Y0"))
                                {
                                    hashSetContainsY0 = true;
                                }
                            }
                            if (!hashSetContainsY0)
                                yNsettings.yIonHashSet.Add(yIon);
                        }
                        else
                        {
                            yNsettings.yIonHashSet.Add(yIon);
                        }
                    }

                    foreach (var item in Yions_LossFromPepChecklistBox.CheckedItems)
                    {
                        string[] yIonArray = item.ToString().Split(',');
                        Yion yIon = new Yion();
                        yIon.theoMass = Convert.ToDouble(yIonArray[1].Substring(1), CultureInfo.InvariantCulture);
                        yIon.description = item.ToString();
                        yIon.glycanSource = "Subtraction";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
                        yNsettings.yIonHashSet.Add(yIon);
                    }

                    //process the custom additions and add to HashSet
                    //this will only execute if the user uploaded a file and changed the text from being empty
                    if (!yNsettings.csvCustomAdditions.Equals("empty"))
                    {
                        using StreamReader csvFile = new StreamReader(yNsettings.csvCustomAdditions);
                        using (var csv = new CsvReader(csvFile, true))
                        {
                            while (csv.ReadNextRecord())
                            {
                                Yion yIon = new Yion();
                                yIon.theoMass = double.Parse(csv["Mass"], CultureInfo.InvariantCulture);
                                string userDescription = csv["Description"];
                                yIon.description = double.Parse(csv["Mass"], CultureInfo.InvariantCulture) + ", " + userDescription;
                                yIon.glycanSource = "CustomAddition";
                                yIon.hcdCount = 0;
                                yIon.etdCount = 0;
                                yNsettings.yIonHashSet.Add(yIon);
                            }
                        }
                    }

                    //process the custom subtractions and add to HashSet
                    //this will only execute if the user uploaded a file and changed the text from being empty
                    if (!yNsettings.csvCustomSubtractions.Equals("empty"))
                    {
                        using StreamReader csvFile = new StreamReader(yNsettings.csvCustomSubtractions);
                        using (var csv = new CsvReader(csvFile, true))
                        {
                            while (csv.ReadNextRecord())
                            {
                                Yion yIon = new Yion();
                                yIon.theoMass = double.Parse(csv["Mass"], CultureInfo.InvariantCulture);
                                string userDescription = csv["Description"];
                                yIon.description = double.Parse(csv["Mass"], CultureInfo.InvariantCulture) + ", " + userDescription;
                                yIon.glycanSource = "CustomSubtraction";
                                yIon.hcdCount = 0;
                                yIon.etdCount = 0;
                                yNsettings.yIonHashSet.Add(yIon);
                            }
                        }
                    }

                    RawFileInfo rawFileInfo = new RawFileInfo();

                    //set up each output stream
                    string fileNameShort = Path.GetFileNameWithoutExtension(yNsettings.rawFilePath);
                    StreamWriter outputYion = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_YionSignal.txt"));
                    StreamWriter outputSummary = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_GlyCounter_YionSummary.txt"));
                    StreamWriter outputIPSA = null;

                    if (yNsettings.ipsa)
                    {
                        outputIPSA = new StreamWriter(Path.Combine(glySettings.outputPath + @"\" + fileNameShort + "_Glycounter_YionIPSA.txt"));
                        outputIPSA.WriteLine("ScanNumber" + '\t' + "Yion" + '\t' + "m/z" + '\t' + "MassError");
                    }

                    string toleranceString = "ppmTol= ";
                    if (yNsettings.usingda)
                        toleranceString = "daTol= ";

                    outputSummary.WriteLine("Settings:\t" + toleranceString + yNsettings.tol + ", SNthreshold= " + yNsettings.SNthreshold +
                        "IntensityThreshold= " + yNsettings.intensityThreshold + "Charge states checked: " + yNsettings.chargeLB + " to " + yNsettings.chargeUB + ", First isotope checked: "
                        + FirstIsotopeCheckBox.Checked + ", Second isotope checked: " + SecondIsotopeCheckBox.Checked);
                    outputSummary.WriteLine(Ynaught_startTimeLabel.Text);
                    outputSummary.WriteLine();


                    //use intensity threshold for mzml files
                    if (CanConvertDouble(Ynaught_intTextBox.Text, yNsettings.intensityThreshold))
                        yNsettings.intensityThreshold = Convert.ToDouble(Ynaught_intTextBox.Text, CultureInfo.InvariantCulture);

                    if (FirstIsotopeCheckBox.Checked) yNsettings.firstIsotope = true;
                    if (SecondIsotopeCheckBox.Checked) yNsettings.secondIsotope = true;

                    if (yNsettings.rawFilePath.EndsWith(".d"))
                    {
                        //TODO
                    }
                    else
                    {
                        
                        (yNsettings, rawFileInfo) = YN_ProcessRaw_MzML.yNprocessRawMzML(yNsettings, glySettings, rawFileInfo, outputYion, outputIPSA);
                    }


                    double percentageYions = (double)rawFileInfo.numberOfMS2scansWithYions / (double)rawFileInfo.numberOfMS2scans * 100;
                    double percentageY0 = (double)rawFileInfo.numberOfMS2scansWithY0 / (double)rawFileInfo.numberOfMS2scans * 100;
                    double percentageGlycoPep = (double)rawFileInfo.numberOfMS2scansWithIntactGlycoPep / (double)rawFileInfo.numberOfMS2scans * 100;

                    double percentageYions_hcd = 0;
                    double percentageYions_etd = 0;
                    double percentageY0_hcd = 0;
                    double percentageY0_etd = 0;
                    double percentageGlycoPep_hcd = 0;
                    double percentageGlycoPep_etd = 0;

                    if (!(rawFileInfo.numberOfHCDscans == 0))
                    {
                        percentageYions_hcd = (double)rawFileInfo.numberOfMS2scansWithYions_hcd / (double)rawFileInfo.numberOfHCDscans * 100;
                        percentageY0_hcd = (double)rawFileInfo.numberOfMS2scansWithY0_hcd / (double)rawFileInfo.numberOfHCDscans * 100;
                        percentageGlycoPep_hcd = (double)rawFileInfo.numberOfMS2scansWithIntactGlycoPep_hcd / (double)rawFileInfo.numberOfHCDscans * 100;
                    }
                    if (!(rawFileInfo.numberOfETDscans == 0))
                    {
                        percentageYions_etd = (double)rawFileInfo.numberOfMS2scansWithYions_etd / (double)rawFileInfo.numberOfETDscans * 100;
                        percentageY0_etd = (double)rawFileInfo.numberOfMS2scansWithY0_etd / (double)rawFileInfo.numberOfETDscans * 100;
                        percentageGlycoPep_etd = (double)rawFileInfo.numberOfMS2scansWithIntactGlycoPep_etd / (double)rawFileInfo.numberOfETDscans * 100;
                    }

                    outputSummary.WriteLine("\tTotal\tHCD\tETD\t%Total\t%HCD\t%ETD");
                    outputSummary.WriteLine("All GlycoPSMs\t" + rawFileInfo.numberOfMS2scans + "\t" + rawFileInfo.numberOfHCDscans + "\t" + rawFileInfo.numberOfETDscans + "\tNA\tNA\tNA");

                    outputSummary.WriteLine("GlycoPSMs with Y-ions\t" + rawFileInfo.numberOfMS2scansWithYions + "\t" + rawFileInfo.numberOfMS2scansWithYions_hcd + "\t" + rawFileInfo.numberOfMS2scansWithYions_etd
                        + "\t" + percentageYions + "\t" + percentageYions_hcd + "\t" + percentageYions_etd);

                    outputSummary.WriteLine("GlycoPSMs with Y0\t" + rawFileInfo.numberOfMS2scansWithY0 + "\t" + rawFileInfo.numberOfMS2scansWithY0_hcd + "\t" + rawFileInfo.numberOfMS2scansWithY0_etd
                        + "\t" + percentageY0 + "\t" + percentageY0_hcd + "\t" + percentageY0_etd);
                    outputSummary.WriteLine("GlycoPSMs with IntactGlycoPep\t" + rawFileInfo.numberOfMS2scansWithIntactGlycoPep + "\t" + rawFileInfo.numberOfMS2scansWithIntactGlycoPep_hcd + "\t" + rawFileInfo.numberOfMS2scansWithIntactGlycoPep_etd
                        + "\t" + percentageGlycoPep + "\t" + percentageGlycoPep_hcd + "\t" + percentageGlycoPep_etd);


                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                    outputSummary.WriteLine("\tTotal\tHCD\tETD\t%Total\t%HCD\t%ETD");

                    string currentGlycanSource = "first";
                    foreach (Yion yIon in yNsettings.yIonHashSet)
                    {
                        int total = yIon.hcdCount + yIon.etdCount;

                        double percentTotal = (double)total / (double)rawFileInfo.numberOfMS2scans * 100;
                        double percentHCD = (double)yIon.hcdCount / (double)rawFileInfo.numberOfHCDscans * 100;
                        double percentETD = (double)yIon.etdCount / (double)rawFileInfo.numberOfETDscans * 100;

                        foreach (string source in yIon.glycanSource.Split(','))
                        {
                            if (!currentGlycanSource.Equals(source) && !source.Equals(""))
                            {
                                outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + source + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                                currentGlycanSource = source;
                            }

                            if (!source.Equals(""))
                            {
                                outputSummary.WriteLine(yIon.description + "\t" + total + "\t" + yIon.hcdCount + "\t" + yIon.etdCount
                                                    + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD);
                            }
                        }
                    }

                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                    //output elapsed time
                    stopwatch2.Stop();
                    TimeSpan ts = stopwatch2.Elapsed;

                    string elapsedTime = String.Format("{0:00}:{1:00}.{2:00}",
                        ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    outputSummary.WriteLine("Total search time: " + elapsedTime);

                    outputSummary.Close();
                    outputYion.Close();
                    if (outputIPSA != null)
                        outputIPSA.Close();
                });
            }



            finally
            {
                timer1.Stop();
                Ynaught_FinishTimeLabel.Text = "Finished at: " + DateTime.Now.ToString("HH:mm:ss");
                MessageBox.Show("Finished.");
                yNsettings.yIonHashSet.Clear();
            }
        }

        public void UpdateTimerYn()
        {
            if (Ynaught_FinishTimeLabel.InvokeRequired)
            {
                Ynaught_FinishTimeLabel.Invoke(new Action(() => Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
            }
            else
            {
                Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
            }
        }
    }
}
