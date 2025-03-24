using CSMSL;
using CSMSL.IO.Thermo;
using CSMSL.Proteomics;
using CSMSL.Spectral;
using LumenWorks.Framework.IO.Csv;
using MathNet.Numerics.Statistics;
using PSI_Interface.MSData;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;


namespace GlyCounter
{
    public partial class Form1 : Form
    {
        //here we build variables
        HashSet<OxoniumIon> oxoniumIonHashSet = new HashSet<OxoniumIon>();
        string filePath = "";
        string csvCustomFile = "empty";
        double daTolerance = 1;
        double ppmTolerance = 15;
        double SNthreshold = 3;
        double peakDepthThreshold_hcd = 25;
        double peakDepthThreshold_etd = 50;
        double peakDepthThreshold_uvpd = 25;
        int arbitraryPeakDepthIfNotFound = 10000;
        double oxoTICfractionThreshold_hcd = 0.20;
        double oxoTICfractionThreshold_etd = 0.05;
        double oxoTICfractionThreshold_uvpd = 0.20;
        double oxoCountRequirement_hcd_user = 0;
        double oxoCountRequirement_etd_user = 0;
        double oxoCountRequirement_uvpd_user = 0;
        double intensityThreshold = 1000;
        double tol = new double();
        bool using204 = false;
        bool ipsa = false;

        //Ynaught variables
        HashSet<Yion> yIonHashSet = new HashSet<Yion>();
        string Ynaught_pepIDFilePath = "";
        string Ynaught_glycanMassesFilePath = "";
        string Ynaught_rawFilePath = "";
        double Ynaught_daTolerance = 1;
        double Ynaught_ppmTolerance = 15;
        double Ynaught_tol = new double();
        double Ynaught_SNthreshold = 3;
        int Ynaught_chargeStateMod_X = 0;
        int Ynaught_chargeStateMod_Y = 1;
        string Ynaught_csvCustomAdditions = "empty";
        string Ynaught_csvCustomSubtractions = "empty";

        // For application updates
        private UpdateManager _updateManager;

        public Form1()
        {
            InitializeComponent();
            
            // Initialize the menu items
            // InitializeMenus(); // Uncomment later
            
            // Initialize the update manager
            _updateManager = UpdateManager.Instance;
            
            // Set window title to include version
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = $"GlyCounter v{version.Major}.{version.Minor}.{version.Build}";
            
            // Check for updates on startup (silently)
            CheckForUpdatesAsync(true);
        }
        
        private void InitializeMenus()
        {
            // Create main menu
            MenuStrip mainMenu = new MenuStrip();
            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);
            
            // Help menu only (removed File menu)
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            ToolStripMenuItem checkUpdatesMenuItem = new ToolStripMenuItem("Check for Updates", null, new EventHandler(CheckUpdatesMenuItem_Click));
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("About", null, new EventHandler(AboutMenuItem_Click));
            helpMenu.DropDownItems.Add(checkUpdatesMenuItem);
            helpMenu.DropDownItems.Add(aboutMenuItem);
            mainMenu.Items.Add(helpMenu);
        }
        
        private async void CheckForUpdatesAsync(bool silent = false)
        {
            await _updateManager.CheckAndPromptForUpdate(silent);
        }
        
        private void CheckUpdatesMenuItem_Click(object sender, EventArgs e)
        {
            CheckForUpdatesAsync(false);
        }
        
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            
            MessageBox.Show(
                $"GlyCounter v{version.Major}.{version.Minor}.{version.Build}\n\n" +
                "A tool for glycan analysis.\n\n" +
                "© 2025 Glyco Labs",
                "About GlyCounter",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";

            // Set the initial directory to the last open folder, if it exists
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }

            fdlg.Filter = "RAW files (*.raw*)|*.raw*|mzML files (*.mzML)|*.mzML";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            filePath = textBox1.Text;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            bool usingda = false;
            bool using204 = false;

            timer1.Interval = 1000;
            timer1.Tick += new EventHandler(OnTimerTick);
            timer1.Start();
            StatusLabel.Text = "Processing...";
            StartTimeLabel.Text = "Start Time: " + DateTime.Now.ToString("HH:mm:ss");

            //make sure all user inputs are in the correct format, otherwise use defaults
            if (DaltonCheckBox.Checked)
            {
                if (CanConvertDouble(ppmTol_textBox.Text, daTolerance))
                {
                    daTolerance = Convert.ToDouble(ppmTol_textBox.Text);
                    usingda = true;
                }

            }
            else
            {
                if (CanConvertDouble(ppmTol_textBox.Text, ppmTolerance))
                    ppmTolerance = Convert.ToDouble(ppmTol_textBox.Text);
            }

            if (usingda)
                tol = daTolerance;
            else
            {
                tol = ppmTolerance;
            }


            if (CanConvertDouble(SN_textBox.Text, SNthreshold))
                SNthreshold = Convert.ToDouble(SN_textBox.Text);

            if (CanConvertDouble(PeakDepth_Box_HCD.Text, peakDepthThreshold_hcd))
                peakDepthThreshold_hcd = Convert.ToDouble(PeakDepth_Box_HCD.Text);

            if (CanConvertDouble(PeakDepth_Box_ETD.Text, peakDepthThreshold_etd))
                peakDepthThreshold_etd = Convert.ToDouble(PeakDepth_Box_ETD.Text);

            if (CanConvertDouble(PeakDepth_Box_UVPD.Text, peakDepthThreshold_uvpd))
                peakDepthThreshold_uvpd = Convert.ToDouble(PeakDepth_Box_UVPD.Text);

            if (CanConvertDouble(hcdTICfraction.Text, oxoTICfractionThreshold_hcd))
                oxoTICfractionThreshold_hcd = Convert.ToDouble(hcdTICfraction.Text);

            if (CanConvertDouble(etdTICfraction.Text, oxoTICfractionThreshold_etd))
                oxoTICfractionThreshold_etd = Convert.ToDouble(etdTICfraction.Text);

            if (CanConvertDouble(uvpdTICfraction.Text, oxoTICfractionThreshold_uvpd))
                oxoTICfractionThreshold_uvpd = Convert.ToDouble(uvpdTICfraction.Text);

            if (CanConvertDouble(OxoCountRequireBox_hcd.Text, oxoCountRequirement_hcd_user))
                oxoCountRequirement_hcd_user = Convert.ToDouble(OxoCountRequireBox_hcd.Text);

            if (CanConvertDouble(OxoCountRequireBox_etd.Text, oxoCountRequirement_etd_user))
                oxoCountRequirement_etd_user = Convert.ToDouble(OxoCountRequireBox_etd.Text);

            if (CanConvertDouble(OxoCountRequireBox_uvpd.Text, oxoCountRequirement_uvpd_user))
                oxoCountRequirement_uvpd_user = Convert.ToDouble(OxoCountRequireBox_uvpd.Text);

            if (CanConvertDouble(intensityThresholdTextBox.Text, intensityThreshold))
                intensityThreshold = Convert.ToDouble(intensityThresholdTextBox.Text);

            string toleranceString = "ppmTol: ";
            if (usingda)
            {
                toleranceString = "DaTol: ";
            }

            MessageBox.Show("You are using these settings:\r\n" + toleranceString + tol + "\r\nSNthreshold: " + SNthreshold + "\r\nIntensityTheshold: " + intensityThreshold
                + "\r\nPeakDepthThreshold_HCD: " + peakDepthThreshold_hcd + "\r\nPeakDepthThreshold_ETD: " + peakDepthThreshold_etd + "\r\nPeakDepthThreshold_UVPD: " + peakDepthThreshold_uvpd
                + "\r\nTICfraction_HCD: " + oxoTICfractionThreshold_hcd + "\r\nTICfraction_ETD: " + oxoTICfractionThreshold_etd + "\r\nTICfraction_UVPD: " + oxoTICfractionThreshold_uvpd);


            foreach (var item in HexNAcCheckedListBox.CheckedItems)
            {
                string[] oxoniumIonArray = item.ToString().Split(',');
                OxoniumIon oxoIon = new OxoniumIon();
                oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0]);
                oxoIon.description = item.ToString();
                oxoIon.glycanSource = "HexNAc";
                oxoIon.hcdCount = 0;
                oxoIon.etdCount = 0;
                oxoIon.uvpdCount = 0;
                oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                oxoniumIonHashSet.Add(oxoIon);
                //check to see if we are looking for 204, used in determining "likely if glyco" below
                if (Convert.ToDouble(oxoniumIonArray[0]) == 204.0867)
                    using204 = true;
            }
            foreach (var item in HexCheckedListBox.CheckedItems)
            {
                string[] oxoniumIonArray = item.ToString().Split(',');
                OxoniumIon oxoIon = new OxoniumIon();
                oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0]);
                oxoIon.description = item.ToString();
                oxoIon.glycanSource = "Hex";
                oxoIon.hcdCount = 0;
                oxoIon.etdCount = 0;
                oxoIon.uvpdCount = 0;
                oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                oxoniumIonHashSet.Add(oxoIon);
            }
            foreach (var item in SialicAcidCheckedListBox.CheckedItems)
            {
                string[] oxoniumIonArray = item.ToString().Split(',');
                OxoniumIon oxoIon = new OxoniumIon();
                oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0]);
                oxoIon.description = item.ToString();
                oxoIon.glycanSource = "Sialic";
                oxoIon.hcdCount = 0;
                oxoIon.etdCount = 0;
                oxoIon.uvpdCount = 0;
                oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                oxoniumIonHashSet.Add(oxoIon);
            }
            foreach (var item in M6PCheckedListBox.CheckedItems)
            {
                string[] oxoniumIonArray = item.ToString().Split(',');
                OxoniumIon oxoIon = new OxoniumIon();
                oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0]);
                oxoIon.description = item.ToString();
                oxoIon.glycanSource = "M6P";
                oxoIon.hcdCount = 0;
                oxoIon.etdCount = 0;
                oxoIon.uvpdCount = 0;
                oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                oxoniumIonHashSet.Add(oxoIon);
            }
            foreach (var item in OligosaccharideCheckedListBox.CheckedItems)
            {
                string[] oxoniumIonArray = item.ToString().Split(',');
                OxoniumIon oxoIon = new OxoniumIon();
                oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0]);
                oxoIon.description = item.ToString();
                oxoIon.glycanSource = "Oligo";
                oxoIon.hcdCount = 0;
                oxoIon.etdCount = 0;
                oxoIon.uvpdCount = 0;
                oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                oxoniumIonHashSet.Add(oxoIon);
            }
            foreach (var item in FucoseCheckedListBox.CheckedItems)
            {
                string[] oxoniumIonArray = item.ToString().Split(',');
                OxoniumIon oxoIon = new OxoniumIon();
                oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0]);
                oxoIon.description = item.ToString();
                oxoIon.glycanSource = "Fucose";
                oxoIon.hcdCount = 0;
                oxoIon.etdCount = 0;
                oxoIon.uvpdCount = 0;
                oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                oxoniumIonHashSet.Add(oxoIon);
            }

            if (!csvCustomFile.Equals("empty"))
            {
                StreamReader csvFile = new StreamReader(csvCustomFile);
                using var csv = new CsvReader(csvFile, true);
                while (csv.ReadNextRecord())
                {
                    OxoniumIon oxoIon = new OxoniumIon();
                    oxoIon.theoMZ = double.Parse(csv["m/z"]);
                    string userDescription = csv["Description"];
                    oxoIon.description = double.Parse(csv["m/z"]) + ", " + userDescription;
                    oxoIon.glycanSource = "Custom";
                    oxoIon.hcdCount = 0;
                    oxoIon.etdCount = 0;
                    oxoIon.uvpdCount = 0;
                    oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;

                    //If an oxonium ion with the same theoretical m/z value exists, replace it with the one from the custom csv
                    List<OxoniumIon> ionsToRemove = new List<OxoniumIon>();
                    foreach (OxoniumIon ion in oxoniumIonHashSet)
                    {
                        if (ion.Equals(oxoIon))
                            ionsToRemove.Add(ion);
                    }
                    foreach (OxoniumIon ion in ionsToRemove)
                    {
                        Console.WriteLine(ion.ToString());
                        oxoniumIonHashSet.Remove(ion);
                    }

                    oxoniumIonHashSet.Add(oxoIon);

                    if (oxoIon.theoMZ == 204.0867 || oxoIon.description == "HexNAc")
                        using204 = true;
                }
            }

            if (ipsaCheckBox.Checked)
                ipsa = true;

            if (AllRawFilesCheckBox.Checked)
            {
                string[] filePathArray = filePath.Split('\\');
                string rawFilesPath = "";

                for (int i = 0; i < filePathArray.Length - 1; i++)
                {
                    rawFilesPath = rawFilesPath + filePathArray[i] + "\\";
                }

                string[] allRawFilesArray = System.IO.Directory.GetFiles(rawFilesPath, "*.raw");
                string[] allMZMLFilesArray = System.IO.Directory.GetFiles(rawFilesPath, "*.mzML");

                foreach (var fileName in allRawFilesArray)
                {


                    //clear out oxonium ions
                    foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                    {
                        oxoIon.intensity = 0;
                        oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                        oxoIon.hcdCount = 0;
                        oxoIon.etdCount = 0;
                        oxoIon.uvpdCount = 0;
                        oxoIon.measuredMZ = 0;
                    }

                    ThermoRawFile rawFile = new ThermoRawFile(fileName);
                    rawFile.Open();

                    StatusLabel.Text = "Current file: " + rawFile.Name;
                    FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                    //Debug.WriteLine("Current file: " + rawFile.Name);
                    StatusLabel.Refresh();
                    FinishTimeLabel.Refresh();

                    int numberOfMS2scansWithOxo_1 = 0;
                    int numberOfMS2scansWithOxo_2 = 0;
                    int numberOfMS2scansWithOxo_3 = 0;
                    int numberOfMS2scansWithOxo_4 = 0;
                    int numberOfMS2scansWithOxo_5plus = 0;
                    int numberOfMS2scansWithOxo_1_hcd = 0;
                    int numberOfMS2scansWithOxo_2_hcd = 0;
                    int numberOfMS2scansWithOxo_3_hcd = 0;
                    int numberOfMS2scansWithOxo_4_hcd = 0;
                    int numberOfMS2scansWithOxo_5plus_hcd = 0;
                    int numberOfMS2scansWithOxo_1_etd = 0;
                    int numberOfMS2scansWithOxo_2_etd = 0;
                    int numberOfMS2scansWithOxo_3_etd = 0;
                    int numberOfMS2scansWithOxo_4_etd = 0;
                    int numberOfMS2scansWithOxo_5plus_etd = 0;
                    int numberOfMS2scansWithOxo_1_uvpd = 0;
                    int numberOfMS2scansWithOxo_2_uvpd = 0;
                    int numberOfMS2scansWithOxo_3_uvpd = 0;
                    int numberOfMS2scansWithOxo_4_uvpd = 0;
                    int numberOfMS2scansWithOxo_5plus_uvpd = 0;
                    int numberOfMS2scans = 0;
                    int numberOfHCDscans = 0;
                    int numberOfETDscans = 0;
                    int numberOfUVPDscans = 0;
                    int numberScansCountedLikelyGlyco_total = 0;
                    int numberScansCountedLikelyGlyco_hcd = 0;
                    int numberScansCountedLikelyGlyco_etd = 0;
                    int numberScansCountedLikelyGlyco_uvpd = 0;
                    bool firstSpectrumInFile = true;
                    bool likelyGlycoSpectrum = false;

                    double halfTotalList = (double)oxoniumIonHashSet.Count / 2.0;

                    StreamWriter outputOxo = new StreamWriter(fileName + "_GlyCounter_OxoSignal.txt");
                    StreamWriter outputPeakDepth = new StreamWriter(fileName + "_GlyCounter_OxoPeakDepth.txt");
                    StreamWriter outputIPSA = null;
                    if (ipsaCheckBox.Checked)
                    {
                        outputIPSA = new StreamWriter(fileName + "_Glycounter_IPSA.txt");
                    }
                    StreamWriter outputSummary = new StreamWriter(fileName + "_GlyCounter_Summary.txt");

                    outputOxo.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    outputPeakDepth.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    if (outputIPSA != null)
                        outputIPSA.WriteLine("ScanNumber\tOxoniumIons\tMassError\t");

                    /*
                    outputSummary.WriteLine("Settings\tppmTol:\t" + ppmTolerance + "\tSNthreshold:\t" + SNthreshold + "\tHCDPeakDepthThreshold:\t" + peakDepthThreshold_hcd
                        + "\tETDPeakDepthThreshold:\t" + peakDepthThreshold_etd + "\tHCD TIC fraction:\t" + oxoTICfractionThreshold_hcd + "\tETD TIC fraction:\t" + oxoTICfractionThreshold_etd);
                    */
                    outputSummary.WriteLine("Settings:\t" + toleranceString + ", SNthreshold=" + SNthreshold + ", IntensityThreshold=" + intensityThreshold + ", PeakDepthThreshold_HCD=" + peakDepthThreshold_hcd + ", PeakDepthThreshold_ETD=" + peakDepthThreshold_etd + ", PeakDepthThreshold_UVPD=" + peakDepthThreshold_uvpd
                        + ", TICfraction_HCD=" + oxoTICfractionThreshold_hcd + ", TICfraction_ETD=" + oxoTICfractionThreshold_etd + ", TICfraction_UVPD=" + oxoTICfractionThreshold_uvpd);
                    outputSummary.WriteLine(VersionNumber_Label.Text + ", " + StartTimeLabel.Text);
                    outputSummary.WriteLine();

                    for (int i = rawFile.FirstSpectrumNumber; i < rawFile.LastSpectrumNumber; i++)
                    {
                        bool IT = rawFile.GetMzAnalyzer(i).ToString().Contains("IonTrap");

                        if (rawFile.GetMsnOrder(i) == 2)
                        {
                            numberOfMS2scans++;
                            int numberOfOxoIons = 0;
                            double totalOxoSignal = 0;
                            likelyGlycoSpectrum = false;
                            bool test204 = false;
                            int countOxoWithinPeakDepthThreshold = 0;

                            bool hcdTrue = false;
                            bool etdTrue = false;
                            bool uvpdTrue = false;

                            List<double> oxoniumIonFoundPeaks = new List<double>();
                            List<double> oxoniumIonFoundMassErrors = new List<double>();

                            if (rawFile.GetDissociationType(i).ToString().Equals("HCD"))
                            {
                                numberOfHCDscans++;
                                hcdTrue = true;
                            }
                            if (rawFile.GetDissociationType(i).ToString().Equals("ETD"))
                            {
                                numberOfETDscans++;
                                etdTrue = true;
                            }
                            if (rawFile.GetDissociationType(i).ToString().Equals("CI") || rawFile.GetDissociationType(i).ToString().Equals("UVPD"))
                            {
                                numberOfUVPDscans++;
                                uvpdTrue = true;
                            }

                            string oxoIonHeader = "";
                            //Debug.WriteLine("scan " + i + ", " + rawFile.GetMzAnalyzer(i));
                            if (rawFile.GetTIC(i) > 0 && rawFile.GetSpectrum(i).GetBasePeakIntensity() > 0)
                            {
                                //Labeled spectrum only exists for non-IT scans
                                ThermoSpectrum spectrum = IT ? rawFile.GetSpectrum(i) : rawFile.GetLabeledSpectrum(i);

                                Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                                RankOrderPeaks(sortedPeakDepths, spectrum);

                                

                                foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                {
                                    oxoIon.intensity = 0;
                                    oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;

                                    oxoIonHeader = oxoIonHeader + oxoIon.description + "\t";
                                    oxoIon.measuredMZ = 0;
                                    oxoIon.intensity = 0;

                                    //Trace.WriteLine("Scan: " + i);

                                    ThermoMzPeak peak = GetPeak(spectrum, oxoIon.theoMZ, usingda, tol, IT);

                                    if (!IT)
                                    {
                                        if (peak != null && peak.Intensity > 0 && peak.SignalToNoise > SNthreshold)
                                        {
                                            oxoIon.measuredMZ = peak.MZ;
                                            oxoIon.intensity = peak.Intensity;
                                            oxoIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                            numberOfOxoIons++;
                                            totalOxoSignal = totalOxoSignal + peak.Intensity;

                                            if (hcdTrue)
                                                oxoIon.hcdCount++;
                                            if (etdTrue)
                                                oxoIon.etdCount++;
                                            if (uvpdTrue)
                                                oxoIon.uvpdCount++;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_hcd && hcdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_etd && etdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_uvpd && uvpdTrue)
                                                test204 = true;

                                            oxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                                            var massError = oxoIon.measuredMZ - oxoIon.theoMZ;
                                            oxoniumIonFoundMassErrors.Add(massError);
                                        }
                                    }
                                    else
                                    {
                                        if (peak != null && peak.Intensity > intensityThreshold)
                                        {
                                            oxoIon.measuredMZ = peak.MZ;
                                            oxoIon.intensity = peak.Intensity;
                                            oxoIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                            numberOfOxoIons++;
                                            totalOxoSignal = totalOxoSignal + peak.Intensity;

                                            if (hcdTrue)
                                                oxoIon.hcdCount++;
                                            if (etdTrue)
                                                oxoIon.etdCount++;
                                            if (uvpdTrue)
                                                oxoIon.uvpdCount++;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_hcd && hcdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_etd && etdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_uvpd && uvpdTrue)
                                                test204 = true;

                                            oxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                                            var massError = oxoIon.measuredMZ - oxoIon.theoMZ;
                                            oxoniumIonFoundMassErrors.Add(massError);
                                        }
                                    }
                                }
                            }

                            if (firstSpectrumInFile)
                            {
                                outputOxo.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                outputPeakDepth.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                //outputSummary.WriteLine("Oxonium Ions Searched for:\t" + oxoIonHeader);
                                firstSpectrumInFile = false;
                            }

                            if (numberOfOxoIons > 0)
                            {
                                if (numberOfOxoIons == 1)
                                {
                                    numberOfMS2scansWithOxo_1++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_1_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_1_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_1_uvpd++;
                                }
                                if (numberOfOxoIons == 2)
                                {
                                    numberOfMS2scansWithOxo_2++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_2_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_2_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_2_uvpd++;
                                }
                                if (numberOfOxoIons == 3)
                                {
                                    numberOfMS2scansWithOxo_3++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_3_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_3_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_3_uvpd++;
                                }
                                if (numberOfOxoIons == 4)
                                {
                                    numberOfMS2scansWithOxo_4++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_4_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_4_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_4_uvpd++;
                                }
                                if (numberOfOxoIons > 4)
                                {
                                    numberOfMS2scansWithOxo_5plus++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_5plus_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_5plus_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_5plus_uvpd++;
                                }

                                double parentScan = 0;
                                try
                                {
                                    parentScan = rawFile.GetParentSpectrumNumber(i);
                                }
                                catch (Exception ex) { }

                                double scanTIC = rawFile.GetTIC(i);
                                double scanInjTime = rawFile.GetInjectionTime(i);
                                string fragmentationType = rawFile.GetDissociationType(i).ToString();
                                //double parentScan = rawFile.GetParentSpectrumNumber(i);
                                double retentionTime = rawFile.GetRetentionTime(i);

                                List<double> oxoRanks = new List<double>();

                                string peakString = new string("");
                                foreach (double theoMZ in oxoniumIonFoundPeaks)
                                {
                                    peakString = peakString + theoMZ.ToString() + "; ";
                                }

                                string errorString = new string("");
                                foreach (double error in oxoniumIonFoundMassErrors)
                                {
                                    errorString = errorString + error.ToString("F6") + "; ";
                                }

                                outputOxo.Write(i + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                outputPeakDepth.Write(i + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                if (outputIPSA != null)
                                    outputIPSA.WriteLine(i + "\t" + peakString + "\t" + errorString + "\t");
                                

                                foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                {
                                    outputOxo.Write(oxoIon.intensity + "\t");

                                    if (oxoIon.peakDepth == arbitraryPeakDepthIfNotFound)
                                    {
                                        outputPeakDepth.Write("NotFound\t");
                                    }
                                    else
                                    {
                                        outputPeakDepth.Write(oxoIon.peakDepth + "\t");
                                        oxoRanks.Add(oxoIon.peakDepth);
                                        if (hcdTrue && oxoIon.peakDepth <= peakDepthThreshold_hcd)
                                            countOxoWithinPeakDepthThreshold++;

                                        if (etdTrue && oxoIon.peakDepth <= peakDepthThreshold_etd)
                                            countOxoWithinPeakDepthThreshold++;

                                        if (uvpdTrue && oxoIon.peakDepth <= peakDepthThreshold_uvpd)
                                            countOxoWithinPeakDepthThreshold++;

                                    }

                                }

                                double medianRanks = Statistics.Median(oxoRanks);

                                //the median peak depth has to be "higher" (i.e., less than) the peak depth threshold 
                                //considered also using the number of oxonium ions found has to be at least half to the total list looked for, but decided against it for now (what if big list?)
                                if (oxoniumIonHashSet.Count < 6)
                                {
                                    halfTotalList = 4;
                                }
                                if (oxoniumIonHashSet.Count > 15)
                                {
                                    halfTotalList = 8;
                                }

                                //if not using 204, the below test will fail by default, so we need to add this in to make sure we check the calculation even if 204 isn't being used.
                                if (!using204)
                                    test204 = true;

                                double oxoTICfraction = totalOxoSignal / scanTIC;

                                //Check if there is a user input oxonium count requirement. If not, use default values
                                double oxoCountRequirement = 0;
                                if (hcdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_hcd_user > 0 ? oxoCountRequirement_hcd_user : halfTotalList;
                                }
                                if (etdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_etd_user > 0 ? oxoCountRequirement_etd_user : halfTotalList / 2;
                                }
                                if (uvpdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_uvpd_user > 0 ? oxoCountRequirement_uvpd_user : halfTotalList;
                                }

                                //intensity differences for HCD and ETD means we need to have two different % TIC threshold values.
                                //changed this to not use median, but instead say the number of oxonium ions with peakdepth within user-deined threshold
                                //needs to be greater than half the total list (or its definitions given above
                                if (hcdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_hcd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_hcd++;
                                }

                                //etd also differs in peak depth, so changed scaled this by 1.5
                                if (etdTrue && numberOfOxoIons >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_etd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_etd++;
                                }

                                if (uvpdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_uvpd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_uvpd++;
                                }

                                outputOxo.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);
                                outputPeakDepth.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);

                                outputOxo.WriteLine();
                                outputPeakDepth.WriteLine();
                            }
                            FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                            FinishTimeLabel.Refresh();
                        }
                    }

                    double percentage1ox = (double)numberOfMS2scansWithOxo_1 / (double)numberOfMS2scans * 100;
                    double percentage2ox = (double)numberOfMS2scansWithOxo_2 / (double)numberOfMS2scans * 100;
                    double percentage3ox = (double)numberOfMS2scansWithOxo_3 / (double)numberOfMS2scans * 100;
                    double percentage4ox = (double)numberOfMS2scansWithOxo_4 / (double)numberOfMS2scans * 100;
                    double percentage5plusox = (double)numberOfMS2scansWithOxo_5plus / (double)numberOfMS2scans * 100;
                    double percentageSum = percentage1ox + percentage2ox + percentage3ox + percentage4ox + percentage5plusox;
                    double numberofMS2scansWithOxo_double = (double)percentageSum / 100 * numberOfMS2scans;

                    double percentage1ox_hcd = (double)numberOfMS2scansWithOxo_1_hcd / (double)numberOfHCDscans * 100;
                    double percentage2ox_hcd = (double)numberOfMS2scansWithOxo_2_hcd / (double)numberOfHCDscans * 100;
                    double percentage3ox_hcd = (double)numberOfMS2scansWithOxo_3_hcd / (double)numberOfHCDscans * 100;
                    double percentage4ox_hcd = (double)numberOfMS2scansWithOxo_4_hcd / (double)numberOfHCDscans * 100;
                    double percentage5plusox_hcd = (double)numberOfMS2scansWithOxo_5plus_hcd / (double)numberOfHCDscans * 100;
                    double percentageSum_hcd = percentage1ox_hcd + percentage2ox_hcd + percentage3ox_hcd + percentage4ox_hcd + percentage5plusox_hcd;
                    double numberofHCDscansWithOxo_double = percentageSum_hcd / 100 * numberOfHCDscans;

                    double percentage1ox_etd = (double)numberOfMS2scansWithOxo_1_etd / (double)numberOfETDscans * 100;
                    double percentage2ox_etd = (double)numberOfMS2scansWithOxo_2_etd / (double)numberOfETDscans * 100;
                    double percentage3ox_etd = (double)numberOfMS2scansWithOxo_3_etd / (double)numberOfETDscans * 100;
                    double percentage4ox_etd = (double)numberOfMS2scansWithOxo_4_etd / (double)numberOfETDscans * 100;
                    double percentage5plusox_etd = (double)numberOfMS2scansWithOxo_5plus_etd / (double)numberOfETDscans * 100;
                    double percentageSum_etd = percentage1ox_etd + percentage2ox_etd + percentage3ox_etd + percentage4ox_etd + percentage5plusox_etd;
                    double numberofETDscansWithOxo_double = percentageSum_etd / 100 * numberOfETDscans;

                    double percentage1ox_uvpd = (double)numberOfMS2scansWithOxo_1_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage2ox_uvpd = (double)numberOfMS2scansWithOxo_2_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage3ox_uvpd = (double)numberOfMS2scansWithOxo_3_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage4ox_uvpd = (double)numberOfMS2scansWithOxo_4_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage5plusox_uvpd = (double)numberOfMS2scansWithOxo_5plus_uvpd / (double)numberOfUVPDscans * 100;
                    double percentageSum_uvpd = percentage1ox_uvpd + percentage2ox_uvpd + percentage3ox_uvpd + percentage4ox_uvpd + percentage5plusox_uvpd;
                    double numberofUVPDscansWithOxo_double = percentageSum_uvpd / 100 * numberOfUVPDscans;

                    numberScansCountedLikelyGlyco_total = numberScansCountedLikelyGlyco_hcd + numberScansCountedLikelyGlyco_etd + numberScansCountedLikelyGlyco_uvpd;
                    double percentageLikelyGlyco_total = (double)numberScansCountedLikelyGlyco_total / (double)numberOfMS2scans * 100;
                    double percentageLikelyGlyco_hcd = (double)numberScansCountedLikelyGlyco_hcd / (double)numberOfHCDscans * 100;
                    double percentageLikelyGlyco_etd = (double)numberScansCountedLikelyGlyco_etd / (double)numberOfETDscans * 100;
                    double percentageLikelyGlyco_uvpd = (double)numberScansCountedLikelyGlyco_uvpd / (double)numberOfUVPDscans * 100;

                    double percentageHCD = (double)numberOfHCDscans / numberOfMS2scans;
                    double percentageETD = (double)numberOfETDscans / numberOfMS2scans;
                    double percentageUVPD = (double)numberOfUVPDscans / numberOfMS2scans;

                    int numberofMS2scansWithOxo = (int)Math.Round(numberofMS2scansWithOxo_double);
                    int numberofHCDscansWithOxo = (int)Math.Round(numberofHCDscansWithOxo_double);
                    int numberofETDscansWithOxo = (int)Math.Round(numberofETDscansWithOxo_double);
                    int numberofUVPDscansWithOxo = (int)Math.Round(numberofUVPDscansWithOxo_double);

                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");
                    outputSummary.WriteLine("MS/MS Scans\t" + numberOfMS2scans + "\t" + numberOfHCDscans + "\t" + numberOfETDscans + "\t" + numberOfUVPDscans
                        + "\t" + 100 + "\t" + percentageHCD + "\t" + percentageETD + "\t" + percentageUVPD);
                    outputSummary.WriteLine("MS/MS Scans with OxoIons\t" + numberofMS2scansWithOxo + "\t" + numberofHCDscansWithOxo + "\t" + numberofETDscansWithOxo + "\t" + numberofUVPDscansWithOxo
                        + "\t" + percentageSum + "\t" + percentageSum_hcd + "\t" + percentageSum_etd + "\t" + percentageSum_uvpd);
                    outputSummary.WriteLine("Likely Glyco\t" + numberScansCountedLikelyGlyco_total + "\t" + numberScansCountedLikelyGlyco_hcd + "\t" + numberScansCountedLikelyGlyco_etd + "\t" + numberScansCountedLikelyGlyco_uvpd
                        + "\t" + percentageLikelyGlyco_total + "\t" + percentageLikelyGlyco_hcd + "\t" + percentageLikelyGlyco_etd + "\t" + percentageLikelyGlyco_uvpd);
                    outputSummary.WriteLine("OxoCount_1\t" + numberOfMS2scansWithOxo_1 + "\t" + numberOfMS2scansWithOxo_1_hcd + "\t" + numberOfMS2scansWithOxo_1_etd + "\t" + numberOfMS2scansWithOxo_1_uvpd
                        + "\t" + percentage1ox + "\t" + percentage1ox_hcd + "\t" + percentage1ox_etd + "\t" + percentage1ox_uvpd);
                    outputSummary.WriteLine("OxoCount_2\t" + numberOfMS2scansWithOxo_2 + "\t" + numberOfMS2scansWithOxo_2_hcd + "\t" + numberOfMS2scansWithOxo_2_etd + "\t" + numberOfMS2scansWithOxo_2_uvpd
                        + "\t" + percentage2ox + "\t" + percentage2ox_hcd + "\t" + percentage2ox_etd + "\t" + percentage2ox_uvpd);
                    outputSummary.WriteLine("OxoCount_3\t" + numberOfMS2scansWithOxo_3 + "\t" + numberOfMS2scansWithOxo_3_hcd + "\t" + numberOfMS2scansWithOxo_3_etd + "\t" + numberOfMS2scansWithOxo_3_uvpd
                        + "\t" + percentage3ox + "\t" + percentage3ox_hcd + "\t" + percentage3ox_etd + "\t" + percentage3ox_uvpd);
                    outputSummary.WriteLine("OxoCount_4\t" + numberOfMS2scansWithOxo_4 + "\t" + numberOfMS2scansWithOxo_4_hcd + "\t" + numberOfMS2scansWithOxo_4_etd + "\t" + numberOfMS2scansWithOxo_4_uvpd
                        + "\t" + percentage4ox + "\t" + percentage4ox_hcd + "\t" + percentage4ox_etd + "\t" + percentage4ox_uvpd);
                    outputSummary.WriteLine("OxoCount_5+\t" + numberOfMS2scansWithOxo_5plus + "\t" + numberOfMS2scansWithOxo_5plus_hcd + "\t" + numberOfMS2scansWithOxo_5plus_etd + "\t" + numberOfMS2scansWithOxo_5plus_uvpd
                        + "\t" + percentage5plusox + "\t" + percentage5plusox_hcd + "\t" + percentage5plusox_etd + "\t" + percentage5plusox_uvpd);

                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");

                    string currentGlycanSource = "";
                    foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                    {
                        int total = oxoIon.hcdCount + oxoIon.etdCount + oxoIon.uvpdCount;

                        double percentTotal = (double)total / (double)numberOfMS2scans * 100;
                        double percentHCD = (double)oxoIon.hcdCount / (double)numberOfHCDscans * 100;
                        double percentETD = (double)oxoIon.etdCount / (double)numberOfETDscans * 100;
                        double percentUVPD = (double)oxoIon.uvpdCount / (double)numberOfUVPDscans * 100;

                        if (!currentGlycanSource.Equals(oxoIon.glycanSource))
                        {
                            outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + oxoIon.glycanSource + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                            currentGlycanSource = oxoIon.glycanSource;
                        }

                        outputSummary.WriteLine(oxoIon.description + "\t" + total + "\t" + oxoIon.hcdCount + "\t" + oxoIon.etdCount + "\t" + oxoIon.uvpdCount
                            + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD + "\t" + percentUVPD);
                    }

                    outputSummary.Close();
                    outputOxo.Close();
                    outputPeakDepth.Close();
                    if (outputIPSA != null)
                        outputIPSA.Close();
                    rawFile.Dispose();
                }

                //handles bulk mzML files
                foreach (var fileName in allMZMLFilesArray)
                {
                    //clear out oxonium ions
                    foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                    {
                        oxoIon.intensity = 0;
                        oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                        oxoIon.hcdCount = 0;
                        oxoIon.etdCount = 0;
                        oxoIon.uvpdCount = 0;
                        oxoIon.measuredMZ = 0;
                    }

                    int numberOfMS2scansWithOxo_1 = 0;
                    int numberOfMS2scansWithOxo_2 = 0;
                    int numberOfMS2scansWithOxo_3 = 0;
                    int numberOfMS2scansWithOxo_4 = 0;
                    int numberOfMS2scansWithOxo_5plus = 0;
                    int numberOfMS2scansWithOxo_1_hcd = 0;
                    int numberOfMS2scansWithOxo_2_hcd = 0;
                    int numberOfMS2scansWithOxo_3_hcd = 0;
                    int numberOfMS2scansWithOxo_4_hcd = 0;
                    int numberOfMS2scansWithOxo_5plus_hcd = 0;
                    int numberOfMS2scansWithOxo_1_etd = 0;
                    int numberOfMS2scansWithOxo_2_etd = 0;
                    int numberOfMS2scansWithOxo_3_etd = 0;
                    int numberOfMS2scansWithOxo_4_etd = 0;
                    int numberOfMS2scansWithOxo_5plus_etd = 0;
                    int numberOfMS2scansWithOxo_1_uvpd = 0;
                    int numberOfMS2scansWithOxo_2_uvpd = 0;
                    int numberOfMS2scansWithOxo_3_uvpd = 0;
                    int numberOfMS2scansWithOxo_4_uvpd = 0;
                    int numberOfMS2scansWithOxo_5plus_uvpd = 0;
                    int numberOfMS2scans = 0;
                    int numberOfHCDscans = 0;
                    int numberOfETDscans = 0;
                    int numberOfUVPDscans = 0;
                    int numberScansCountedLikelyGlyco_total = 0;
                    int numberScansCountedLikelyGlyco_hcd = 0;
                    int numberScansCountedLikelyGlyco_etd = 0;
                    int numberScansCountedLikelyGlyco_uvpd = 0;
                    bool firstSpectrumInFile = true;
                    bool likelyGlycoSpectrum = false;

                    StreamWriter outputOxo = new StreamWriter(fileName + "_GlyCounter_OxoSignal.txt");
                    StreamWriter outputPeakDepth = new StreamWriter(fileName + "_GlyCounter_OxoPeakDepth.txt");
                    StreamWriter outputIPSA = null;
                    if (ipsaCheckBox.Checked)
                    {
                        outputIPSA = new StreamWriter(filePath + "_Glycounter_IPSA.txt");
                    }
                    StreamWriter outputSummary = new StreamWriter(fileName + "_GlyCounter_Summary.txt");

                    outputOxo.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    outputPeakDepth.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    if (outputIPSA != null)
                        outputIPSA.WriteLine("ScanNumber\tOxoniumIons\tMassError\t");
                    /*
                    outputSummary.WriteLine("Settings\tppmTol:\t" + ppmTolerance + "\tSNthreshold:\t" + SNthreshold + "\tHCDPeakDepthThreshold:\t" + peakDepthThreshold_hcd
                        + "\tETDPeakDepthThreshold:\t" + peakDepthThreshold_etd + "\tHCD TIC fraction:\t" + oxoTICfractionThreshold_hcd + "\tETD TIC fraction:\t" + oxoTICfractionThreshold_etd);
                    */
                    outputSummary.WriteLine("Settings:\t" + toleranceString + tol + ", SNthreshold=" + SNthreshold + ", IntensityThreshold=" + intensityThreshold + ", PeakDepthThreshold_HCD=" + peakDepthThreshold_hcd
                        + ", PeakDepthThreshold_ETD=" + peakDepthThreshold_etd + ", PeakDepthThreshold_UVPD=" + peakDepthThreshold_uvpd + ", TICfraction_HCD=" + oxoTICfractionThreshold_hcd
                        + ", TICfraction_ETD=" + oxoTICfractionThreshold_etd + ", TICfraction_UVPD=" + oxoTICfractionThreshold_uvpd);
                    outputSummary.WriteLine(VersionNumber_Label.Text + ", " + StartTimeLabel.Text);
                    outputSummary.WriteLine();


                    using var reader = new SimpleMzMLReader(fileName, true, true);
                    var specCount = 0;
                    double halfTotalList = (double)oxoniumIonHashSet.Count / 2.0;

                    foreach (var spec in reader.ReadAllSpectra(true))
                    {

                        StatusLabel.Text = "Current file: " + fileName;
                        FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                        //Debug.WriteLine("Current file: " + rawFile.Name);
                        StatusLabel.Refresh();
                        FinishTimeLabel.Refresh();

                        var paramsList = spec.CVParams;

                        if (spec.MsLevel == 2)
                        {
                            numberOfMS2scans++;
                            int numberOfOxoIons = 0;
                            double totalOxoSignal = 0;
                            likelyGlycoSpectrum = false;
                            bool test204 = false;
                            int countOxoWithinPeakDepthThreshold = 0;

                            bool hcdTrue = false;
                            bool etdTrue = false;
                            bool uvpdTrue = false;

                            var precursors = spec.Precursors;
                            var precursor = precursors[0];

                            List<double> oxoniumIonFoundPeaks = new List<double>();
                            List<double> oxoniumIonFoundMassErrors = new List<double>();

                            switch (precursor.ActivationMethod.ToString())
                            {
                                case "beam-type collision-induced dissociation":
                                    numberOfHCDscans++;
                                    hcdTrue = true;
                                    break;
                                case "collision-induced dissociation":
                                    numberOfHCDscans++;
                                    hcdTrue = true;
                                    break;
                                case ", supplemental beam-type collision-induced dissociation":
                                    numberOfETDscans++;
                                    etdTrue = true;
                                    break;
                                case "electron transfer dissociation":
                                    numberOfETDscans++;
                                    etdTrue = true;
                                    break;
                                case "electron capture dissociation":
                                    numberOfETDscans++;
                                    etdTrue = true;
                                    break;
                                case "photodissociation":
                                    numberOfUVPDscans++;
                                    uvpdTrue = true;
                                    break;
                                default:
                                    Debug.WriteLine(precursor.ActivationMethod.ToString());
                                    break;
                            }

                            string oxoIonHeader = "";

                            if (spec.TotalIonCurrent > 0)
                            {
                                Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                                RankOrderPeaks_mzml(sortedPeakDepths, spec);

                                foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                {
                                    oxoIon.intensity = 0;
                                    oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;

                                    oxoIonHeader = oxoIonHeader + oxoIon.description + "\t";
                                    oxoIon.measuredMZ = 0;
                                    oxoIon.intensity = 0;

                                    var peaklist = spec.Peaks;
                                    SimpleMzMLReader.Peak peak = GetPeak_mzml(spec, oxoIon.theoMZ, usingda, tol);

                                    if (peak.Intensity > intensityThreshold)
                                    {
                                        oxoIon.measuredMZ = peak.Mz;
                                        oxoIon.intensity = peak.Intensity;
                                        oxoIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                        numberOfOxoIons++;
                                        totalOxoSignal = totalOxoSignal + peak.Intensity;

                                        if (hcdTrue)
                                            oxoIon.hcdCount++;
                                        if (etdTrue)
                                            oxoIon.etdCount++;
                                        if (uvpdTrue)
                                            oxoIon.uvpdCount++;

                                        if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_hcd && hcdTrue)
                                            test204 = true;

                                        if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_etd && etdTrue)
                                            test204 = true;

                                        if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_uvpd && uvpdTrue)
                                            test204 = true;

                                        oxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                                        var massError = oxoIon.measuredMZ - oxoIon.theoMZ;
                                        oxoniumIonFoundMassErrors.Add(massError);
                                    }
                                }
                            }

                            if (firstSpectrumInFile)
                            {
                                outputOxo.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                outputPeakDepth.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                //outputSummary.WriteLine("Oxonium Ions Searched for:\t" + oxoIonHeader);
                                firstSpectrumInFile = false;
                            }

                            if (numberOfOxoIons > 0)
                            {
                                if (numberOfOxoIons == 1)
                                {
                                    numberOfMS2scansWithOxo_1++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_1_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_1_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_1_etd++;
                                }
                                if (numberOfOxoIons == 2)
                                {
                                    numberOfMS2scansWithOxo_2++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_2_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_2_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_2_uvpd++;
                                }
                                if (numberOfOxoIons == 3)
                                {
                                    numberOfMS2scansWithOxo_3++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_3_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_3_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_3_uvpd++;
                                }
                                if (numberOfOxoIons == 4)
                                {
                                    numberOfMS2scansWithOxo_4++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_4_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_4_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_4_uvpd++;
                                }
                                if (numberOfOxoIons > 4)
                                {
                                    numberOfMS2scansWithOxo_5plus++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_5plus_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_5plus_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_5plus_uvpd++;
                                }

                                double? parentScan = 0;
                                var refSpec = precursor.PrecursorSpectrumRef;
                                string[] refList = refSpec.Split('=');
                                if (refList.Length > 2)
                                {
                                    if (double.TryParse(refList[3], out double result))
                                    {
                                        parentScan = result;
                                    }
                                }
                                else { parentScan = null; }

                                double scanTIC = spec.TotalIonCurrent;

                                string[] ITlist = new string[0];
                                foreach (var param in paramsList)
                                {
                                    if (param.ToString().Contains("ion injection time"))
                                    {
                                        ITlist = param.ToString().Split('"');
                                    }
                                }
                                double scanInjTime = 0;

                                if (ITlist.Length > 0)
                                {
                                    scanInjTime = double.Parse(ITlist[1], System.Globalization.NumberStyles.Float);
                                }

                                string fragmentationType = "";
                                if (hcdTrue)
                                    fragmentationType = "HCD";
                                if (etdTrue)
                                    fragmentationType = "ETD";
                                if (uvpdTrue)
                                    fragmentationType = "UVPD";

                                double retentionTime = spec.ScanStartTime;

                                List<double> oxoRanks = new List<double>();

                                string peakString = new string("");
                                foreach (double theoMZ in oxoniumIonFoundPeaks)
                                {
                                    peakString = peakString + theoMZ.ToString() + "; ";
                                }

                                string errorString = new string("");
                                foreach (double error in oxoniumIonFoundMassErrors)
                                {
                                    errorString = errorString + error.ToString("F6") + "; ";
                                }

                                outputOxo.Write(specCount + 1 + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                outputPeakDepth.Write(specCount + 1 + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                if (outputIPSA != null)
                                    outputIPSA.WriteLine(specCount + 1 + "\t" + peakString + "\t" + errorString + "\t");

                                foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                {
                                    outputOxo.Write(oxoIon.intensity + "\t");

                                    if (oxoIon.peakDepth == arbitraryPeakDepthIfNotFound)
                                    {
                                        outputPeakDepth.Write("NotFound\t");
                                    }
                                    else
                                    {
                                        outputPeakDepth.Write(oxoIon.peakDepth + "\t");
                                        oxoRanks.Add(oxoIon.peakDepth);
                                        if (hcdTrue && oxoIon.peakDepth <= peakDepthThreshold_hcd)
                                            countOxoWithinPeakDepthThreshold++;

                                        if (etdTrue && oxoIon.peakDepth <= peakDepthThreshold_etd)
                                            countOxoWithinPeakDepthThreshold++;

                                        if (uvpdTrue && oxoIon.peakDepth <= peakDepthThreshold_uvpd)
                                            countOxoWithinPeakDepthThreshold++;

                                    }

                                }
                                double medianRanks = Statistics.Median(oxoRanks);

                                //the median peak depth has to be "higher" (i.e., less than) the peak depth threshold 
                                //considered also using the number of oxonium ions found has to be at least half to the total list looked for, but decided against it for now (what if big list?)
                                if (oxoniumIonHashSet.Count < 6)
                                    halfTotalList = 4;
                                if (oxoniumIonHashSet.Count > 15)
                                    halfTotalList = 8;

                                //if not using 204, the below test will fail by default, so we need to add this in to make sure we check the calculation even if 204 isn't being used.
                                if (!using204)
                                    test204 = true;

                                double oxoTICfraction = totalOxoSignal / scanTIC;

                                double oxoCountRequirement = 0;
                                if (hcdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_hcd_user > 0
                                        ? oxoCountRequirement_hcd_user
                                        : halfTotalList;
                                }
                                if (etdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_etd_user > 0
                                        ? oxoCountRequirement_etd_user
                                        : halfTotalList / 2;
                                }
                                if (uvpdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_uvpd_user > 0
                                        ? oxoCountRequirement_uvpd_user
                                        : halfTotalList;
                                }


                                //intensity differences for HCD and ETD means we need to have two different % TIC threshold values.
                                //changed this to not use median, but instead say the number of oxonium ions with peakdepth within user-deined threshold
                                //needs to be greater than half the total list (or its definitions given above
                                if (hcdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_hcd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_hcd++;
                                }


                                //etd also differs in peak depth, so changed scaled this by 1.5
                                if (etdTrue && numberOfOxoIons >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_etd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_etd++;
                                }

                                if (uvpdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_uvpd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_uvpd++;
                                }


                                outputOxo.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);
                                outputPeakDepth.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);

                                outputOxo.WriteLine();
                                outputPeakDepth.WriteLine();
                            }
                            FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                            FinishTimeLabel.Refresh();
                        }
                        specCount++;
                    }

                    double percentage1ox = (double)numberOfMS2scansWithOxo_1 / (double)numberOfMS2scans * 100;
                    double percentage2ox = (double)numberOfMS2scansWithOxo_2 / (double)numberOfMS2scans * 100;
                    double percentage3ox = (double)numberOfMS2scansWithOxo_3 / (double)numberOfMS2scans * 100;
                    double percentage4ox = (double)numberOfMS2scansWithOxo_4 / (double)numberOfMS2scans * 100;
                    double percentage5plusox = (double)numberOfMS2scansWithOxo_5plus / (double)numberOfMS2scans * 100;
                    double percentageSum = percentage1ox + percentage2ox + percentage3ox + percentage4ox + percentage5plusox;
                    double numberofMS2scansWithOxo_double = (double)percentageSum / 100 * numberOfMS2scans;

                    double percentage1ox_hcd = (double)numberOfMS2scansWithOxo_1_hcd / (double)numberOfHCDscans * 100;
                    double percentage2ox_hcd = (double)numberOfMS2scansWithOxo_2_hcd / (double)numberOfHCDscans * 100;
                    double percentage3ox_hcd = (double)numberOfMS2scansWithOxo_3_hcd / (double)numberOfHCDscans * 100;
                    double percentage4ox_hcd = (double)numberOfMS2scansWithOxo_4_hcd / (double)numberOfHCDscans * 100;
                    double percentage5plusox_hcd = (double)numberOfMS2scansWithOxo_5plus_hcd / (double)numberOfHCDscans * 100;
                    double percentageSum_hcd = percentage1ox_hcd + percentage2ox_hcd + percentage3ox_hcd + percentage4ox_hcd + percentage5plusox_hcd;
                    double numberofHCDscansWithOxo_double = percentageSum_hcd / 100 * numberOfHCDscans;

                    double percentage1ox_etd = (double)numberOfMS2scansWithOxo_1_etd / (double)numberOfETDscans * 100;
                    double percentage2ox_etd = (double)numberOfMS2scansWithOxo_2_etd / (double)numberOfETDscans * 100;
                    double percentage3ox_etd = (double)numberOfMS2scansWithOxo_3_etd / (double)numberOfETDscans * 100;
                    double percentage4ox_etd = (double)numberOfMS2scansWithOxo_4_etd / (double)numberOfETDscans * 100;
                    double percentage5plusox_etd = (double)numberOfMS2scansWithOxo_5plus_etd / (double)numberOfETDscans * 100;
                    double percentageSum_etd = percentage1ox_etd + percentage2ox_etd + percentage3ox_etd + percentage4ox_etd + percentage5plusox_etd;
                    double numberofETDscansWithOxo_double = percentageSum_etd / 100 * numberOfETDscans;

                    double percentage1ox_uvpd = (double)numberOfMS2scansWithOxo_1_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage2ox_uvpd = (double)numberOfMS2scansWithOxo_2_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage3ox_uvpd = (double)numberOfMS2scansWithOxo_3_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage4ox_uvpd = (double)numberOfMS2scansWithOxo_4_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage5plusox_uvpd = (double)numberOfMS2scansWithOxo_5plus_uvpd / (double)numberOfUVPDscans * 100;
                    double percentageSum_uvpd = percentage1ox_uvpd + percentage2ox_uvpd + percentage3ox_uvpd + percentage4ox_uvpd + percentage5plusox_uvpd;
                    double numberofUVPDscansWithOxo_double = percentageSum_uvpd / 100 * numberOfUVPDscans;

                    numberScansCountedLikelyGlyco_total = numberScansCountedLikelyGlyco_hcd + numberScansCountedLikelyGlyco_etd + numberScansCountedLikelyGlyco_uvpd;
                    double percentageLikelyGlyco_total = (double)numberScansCountedLikelyGlyco_total / (double)numberOfMS2scans * 100;
                    double percentageLikelyGlyco_hcd = (double)numberScansCountedLikelyGlyco_hcd / (double)numberOfHCDscans * 100;
                    double percentageLikelyGlyco_etd = (double)numberScansCountedLikelyGlyco_etd / (double)numberOfETDscans * 100;
                    double percentageLikelyGlyco_uvpd = (double)numberScansCountedLikelyGlyco_uvpd / (double)numberOfUVPDscans * 100;

                    double percentageHCD = (double)numberOfHCDscans / numberOfMS2scans;
                    double percentageETD = (double)numberOfETDscans / numberOfMS2scans;
                    double percentageUVPD = (double)numberOfUVPDscans / numberOfMS2scans;

                    int numberofMS2scansWithOxo = (int)Math.Round(numberofMS2scansWithOxo_double);
                    int numberofHCDscansWithOxo = (int)Math.Round(numberofHCDscansWithOxo_double);
                    int numberofETDscansWithOxo = (int)Math.Round(numberofETDscansWithOxo_double);
                    int numberofUVPDscansWithOxo = (int)Math.Round(numberofUVPDscansWithOxo_double);

                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");
                    outputSummary.WriteLine("MS/MS Scans\t" + numberOfMS2scans + "\t" + numberOfHCDscans + "\t" + numberOfETDscans + "\t" + numberOfUVPDscans
                        + "\t" + 100 + "\t" + percentageHCD + "\t" + percentageETD + "\t" + percentageUVPD);
                    outputSummary.WriteLine("MS/MS Scans with OxoIons\t" + numberofMS2scansWithOxo + "\t" + numberofHCDscansWithOxo + "\t" + numberofETDscansWithOxo + "\t" + numberofUVPDscansWithOxo
                        + "\t" + percentageSum + "\t" + percentageSum_hcd + "\t" + percentageSum_etd + "\t" + percentageSum_uvpd);
                    outputSummary.WriteLine("Likely Glyco\t" + numberScansCountedLikelyGlyco_total + "\t" + numberScansCountedLikelyGlyco_hcd + "\t" + numberScansCountedLikelyGlyco_etd + "\t" + numberScansCountedLikelyGlyco_uvpd
                        + "\t" + percentageLikelyGlyco_total + "\t" + percentageLikelyGlyco_hcd + "\t" + percentageLikelyGlyco_etd + "\t" + percentageLikelyGlyco_uvpd);
                    outputSummary.WriteLine("OxoCount_1\t" + numberOfMS2scansWithOxo_1 + "\t" + numberOfMS2scansWithOxo_1_hcd + "\t" + numberOfMS2scansWithOxo_1_etd + "\t" + numberOfMS2scansWithOxo_1_uvpd
                        + "\t" + percentage1ox + "\t" + percentage1ox_hcd + "\t" + percentage1ox_etd + "\t" + percentage1ox_uvpd);
                    outputSummary.WriteLine("OxoCount_2\t" + numberOfMS2scansWithOxo_2 + "\t" + numberOfMS2scansWithOxo_2_hcd + "\t" + numberOfMS2scansWithOxo_2_etd + "\t" + numberOfMS2scansWithOxo_2_uvpd
                        + "\t" + percentage2ox + "\t" + percentage2ox_hcd + "\t" + percentage2ox_etd + "\t" + percentage2ox_uvpd);
                    outputSummary.WriteLine("OxoCount_3\t" + numberOfMS2scansWithOxo_3 + "\t" + numberOfMS2scansWithOxo_3_hcd + "\t" + numberOfMS2scansWithOxo_3_etd + "\t" + numberOfMS2scansWithOxo_3_uvpd
                        + "\t" + percentage3ox + "\t" + percentage3ox_hcd + "\t" + percentage3ox_etd + "\t" + percentage3ox_uvpd);
                    outputSummary.WriteLine("OxoCount_4\t" + numberOfMS2scansWithOxo_4 + "\t" + numberOfMS2scansWithOxo_4_hcd + "\t" + numberOfMS2scansWithOxo_4_etd + "\t" + numberOfMS2scansWithOxo_4_uvpd
                        + "\t" + percentage4ox + "\t" + percentage4ox_hcd + "\t" + percentage4ox_etd + "\t" + percentage4ox_uvpd);
                    outputSummary.WriteLine("OxoCount_5+\t" + numberOfMS2scansWithOxo_5plus + "\t" + numberOfMS2scansWithOxo_5plus_hcd + "\t" + numberOfMS2scansWithOxo_5plus_etd + "\t" + numberOfMS2scansWithOxo_5plus_uvpd
                        + "\t" + percentage5plusox + "\t" + percentage5plusox_hcd + "\t" + percentage5plusox_etd + "\t" + percentage5plusox_uvpd);

                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");

                    string currentGlycanSource = "";
                    foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                    {
                        int total = oxoIon.hcdCount + oxoIon.etdCount + oxoIon.uvpdCount;

                        double percentTotal = (double)total / (double)numberOfMS2scans * 100;
                        double percentHCD = (double)oxoIon.hcdCount / (double)numberOfHCDscans * 100;
                        double percentETD = (double)oxoIon.etdCount / (double)numberOfETDscans * 100;
                        double percentUVPD = (double)oxoIon.uvpdCount / (double)numberOfUVPDscans * 100;

                        if (!currentGlycanSource.Equals(oxoIon.glycanSource))
                        {
                            outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + oxoIon.glycanSource + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                            currentGlycanSource = oxoIon.glycanSource;
                        }

                        outputSummary.WriteLine(oxoIon.description + "\t" + total + "\t" + oxoIon.hcdCount + "\t" + oxoIon.etdCount + "\t" + oxoIon.uvpdCount
                            + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD + "\t" + percentUVPD);
                    }
                    outputSummary.Close();
                    outputOxo.Close();
                    outputPeakDepth.Close();
                    if (outputIPSA != null)
                        outputIPSA.Close();

                }

            }

            else
            {
                if (filePath.EndsWith(".raw"))
                {
                    ThermoRawFile rawFile = new ThermoRawFile(filePath);
                    rawFile.Open();

                    StatusLabel.Text = "Current file: " + rawFile.Name;
                    FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                    //Debug.WriteLine("Current file: " + rawFile.Name);
                    StatusLabel.Refresh();
                    FinishTimeLabel.Refresh();

                    int numberOfMS2scansWithOxo_1 = 0;
                    int numberOfMS2scansWithOxo_2 = 0;
                    int numberOfMS2scansWithOxo_3 = 0;
                    int numberOfMS2scansWithOxo_4 = 0;
                    int numberOfMS2scansWithOxo_5plus = 0;
                    int numberOfMS2scansWithOxo_1_hcd = 0;
                    int numberOfMS2scansWithOxo_2_hcd = 0;
                    int numberOfMS2scansWithOxo_3_hcd = 0;
                    int numberOfMS2scansWithOxo_4_hcd = 0;
                    int numberOfMS2scansWithOxo_5plus_hcd = 0;
                    int numberOfMS2scansWithOxo_1_etd = 0;
                    int numberOfMS2scansWithOxo_2_etd = 0;
                    int numberOfMS2scansWithOxo_3_etd = 0;
                    int numberOfMS2scansWithOxo_4_etd = 0;
                    int numberOfMS2scansWithOxo_5plus_etd = 0;
                    int numberOfMS2scansWithOxo_1_uvpd = 0;
                    int numberOfMS2scansWithOxo_2_uvpd = 0;
                    int numberOfMS2scansWithOxo_3_uvpd = 0;
                    int numberOfMS2scansWithOxo_4_uvpd = 0;
                    int numberOfMS2scansWithOxo_5plus_uvpd = 0;
                    int numberOfMS2scans = 0;
                    int numberOfHCDscans = 0;
                    int numberOfETDscans = 0;
                    int numberOfUVPDscans = 0;
                    int numberScansCountedLikelyGlyco_total = 0;
                    int numberScansCountedLikelyGlyco_hcd = 0;
                    int numberScansCountedLikelyGlyco_etd = 0;
                    int numberScansCountedLikelyGlyco_uvpd = 0;
                    bool firstSpectrumInFile = true;
                    bool likelyGlycoSpectrum = false;

                    double halfTotalList = (double)oxoniumIonHashSet.Count / 2.0;

                    StreamWriter outputOxo = new StreamWriter(filePath + "_GlyCounter_OxoSignal.txt");
                    StreamWriter outputPeakDepth = new StreamWriter(filePath + "_GlyCounter_OxoPeakDepth.txt");
                    StreamWriter outputIPSA = null;
                    if (ipsaCheckBox.Checked)
                    {
                        outputIPSA = new StreamWriter(filePath + "_Glycounter_IPSA.txt");
                    }
                        
                    StreamWriter outputSummary = new StreamWriter(filePath + "_GlyCounter_Summary.txt");

                    outputOxo.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    outputPeakDepth.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    if (outputIPSA != null)
                        outputIPSA.WriteLine("ScanNumber\tOxoniumIons\tMassError\t");
                    /*
                    outputSummary.WriteLine("Settings\tppmTol:\t" + ppmTolerance + "\tSNthreshold:\t" + SNthreshold + "\tHCDPeakDepthThreshold:\t" + peakDepthThreshold_hcd
                        + "\tETDPeakDepthThreshold:\t" + peakDepthThreshold_etd + "\tHCD TIC fraction:\t" + oxoTICfractionThreshold_hcd + "\tETD TIC fraction:\t" + oxoTICfractionThreshold_etd);
                    */
                    outputSummary.WriteLine("Settings:\t" + toleranceString + tol + ", SNthreshold=" + SNthreshold + ", IntensityThreshold=" + intensityThreshold + ", PeakDepthThreshold_HCD=" + peakDepthThreshold_hcd
                                            + ", PeakDepthThreshold_ETD=" + peakDepthThreshold_etd + ", PeakDepthThreshold_UVPD=" + peakDepthThreshold_uvpd + ", TICfraction_HCD=" + oxoTICfractionThreshold_hcd
                                            + ", TICfraction_ETD=" + oxoTICfractionThreshold_etd + ", TICfraction_UVPD=" + oxoTICfractionThreshold_uvpd);
                    outputSummary.WriteLine(VersionNumber_Label.Text + ", " + StartTimeLabel.Text);
                    outputSummary.WriteLine();

                    for (int i = rawFile.FirstSpectrumNumber; i < rawFile.LastSpectrumNumber; i++)
                    {
                        bool IT = rawFile.GetMzAnalyzer(i).ToString().Contains("IonTrap");

                        if (rawFile.GetMsnOrder(i) == 2)
                        {
                            numberOfMS2scans++;
                            int numberOfOxoIons = 0;
                            double totalOxoSignal = 0;
                            likelyGlycoSpectrum = false;
                            bool test204 = false;
                            int countOxoWithinPeakDepthThreshold = 0;

                            bool hcdTrue = false;
                            bool etdTrue = false;
                            bool uvpdTrue = false;

                            List<double> oxoniumIonFoundPeaks = new List<double>();
                            List<double> oxoniumIonFoundMassErrors = new List<double>();

                            if (rawFile.GetDissociationType(i).ToString().Equals("HCD"))
                            {
                                numberOfHCDscans++;
                                hcdTrue = true;
                            }
                            if (rawFile.GetDissociationType(i).ToString().Equals("ETD"))
                            {
                                numberOfETDscans++;
                                etdTrue = true;
                            }
                            if (rawFile.GetDissociationType(i).ToString().Equals("CI") || rawFile.GetDissociationType(i).ToString().Equals("UVPD"))
                            {
                                numberOfUVPDscans++;
                                uvpdTrue = true;
                            }

                            //ThermoSpectrum spectrum = null;
                            string oxoIonHeader = "";

                            /*
                            Debug.WriteLine("scan " + i);
                            ThermoSpectrum TestSpectrum = rawFile.GetSpectrum(i);
                            List<ThermoMzPeak> testPeaks = new List<ThermoMzPeak>();
                            TestSpectrum.TryGetPeaks(TestSpectrum.FirstMZ, TestSpectrum.LastMZ, out testPeaks);
                            Debug.WriteLine(rawFile.GetMzAnalyzer(i));
                            Debug.WriteLine(testPeaks.Count);
                            */
                            if (rawFile.GetTIC(i) > 0 && rawFile.GetSpectrum(i).GetBasePeakIntensity() > 0)
                            {
                                //spectrum = rawFile.GetLabeledSpectrum(i);
                                ThermoSpectrum spectrum = null;
                                if (IT)
                                    //In order to make this work I had to change GetSpectrum to only get unlabeled data
                                    spectrum = rawFile.GetSpectrum(i);
                                else
                                    spectrum = rawFile.GetLabeledSpectrum(i);

                                Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                                RankOrderPeaks(sortedPeakDepths, spectrum);

                                foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                {
                                    oxoIon.intensity = 0;
                                    oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;

                                    oxoIonHeader = oxoIonHeader + oxoIon.description + "\t";
                                    oxoIon.measuredMZ = 0;
                                    oxoIon.intensity = 0;

                                    //Trace.WriteLine("Scan: " + i);
                                    ThermoMzPeak peak = GetPeak(spectrum, oxoIon.theoMZ, usingda, tol, IT);

                                    if (!IT)
                                    {
                                        if (peak != null && peak.Intensity > 0 && peak.SignalToNoise > SNthreshold)
                                        {
                                            oxoIon.measuredMZ = peak.MZ;
                                            oxoIon.intensity = peak.Intensity;
                                            oxoIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                            numberOfOxoIons++;
                                            totalOxoSignal = totalOxoSignal + peak.Intensity;

                                            if (hcdTrue)
                                                oxoIon.hcdCount++;
                                            if (etdTrue)
                                                oxoIon.etdCount++;
                                            if (uvpdTrue)
                                                oxoIon.uvpdCount++;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_hcd && hcdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_etd && etdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_uvpd && uvpdTrue)
                                                test204 = true;

                                            oxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                                            var massError = oxoIon.measuredMZ - oxoIon.theoMZ;
                                            oxoniumIonFoundMassErrors.Add(massError);
                                        }
                                    }
                                    else
                                    {
                                        if (peak != null && peak.Intensity > intensityThreshold)
                                        {
                                            oxoIon.measuredMZ = peak.MZ;
                                            oxoIon.intensity = peak.Intensity;
                                            oxoIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                            numberOfOxoIons++;
                                            totalOxoSignal = totalOxoSignal + peak.Intensity;

                                            if (hcdTrue)
                                                oxoIon.hcdCount++;
                                            if (etdTrue)
                                                oxoIon.etdCount++;
                                            if (uvpdTrue)
                                                oxoIon.uvpdCount++;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_hcd && hcdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_etd && etdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_uvpd && uvpdTrue)
                                                test204 = true;

                                            oxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                                            var massError = oxoIon.measuredMZ - oxoIon.theoMZ;
                                            oxoniumIonFoundMassErrors.Add(massError);
                                        }
                                    }

                                }
                            }



                            if (firstSpectrumInFile)
                            {
                                outputOxo.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                outputPeakDepth.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                //outputSummary.WriteLine("Oxonium Ions Searched for:\t" + oxoIonHeader);
                                firstSpectrumInFile = false;
                            }

                            if (numberOfOxoIons > 0)
                            {
                                if (numberOfOxoIons == 1)
                                {
                                    numberOfMS2scansWithOxo_1++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_1_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_1_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_1_uvpd++;
                                }
                                if (numberOfOxoIons == 2)
                                {
                                    numberOfMS2scansWithOxo_2++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_2_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_2_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_2_uvpd++;
                                }
                                if (numberOfOxoIons == 3)
                                {
                                    numberOfMS2scansWithOxo_3++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_3_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_3_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_3_uvpd++;
                                }
                                if (numberOfOxoIons == 4)
                                {
                                    numberOfMS2scansWithOxo_4++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_4_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_4_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_4_uvpd++;
                                }
                                if (numberOfOxoIons > 4)
                                {
                                    numberOfMS2scansWithOxo_5plus++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_5plus_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_5plus_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_5plus_uvpd++;
                                }


                                double parentScan = 0;
                                try
                                {
                                    parentScan = rawFile.GetParentSpectrumNumber(i);
                                }
                                catch (Exception ex) { }

                                double scanTIC = rawFile.GetTIC(i);
                                double scanInjTime = rawFile.GetInjectionTime(i);
                                string fragmenationType = rawFile.GetDissociationType(i).ToString();
                                //double parentScan = rawFile.GetParentSpectrumNumber(i);
                                double retentionTime = rawFile.GetRetentionTime(i);


                                List<double> oxoRanks = new List<double>();

                                string peakString = new string("");
                                foreach (double theoMZ in oxoniumIonFoundPeaks)
                                {
                                    peakString = peakString + theoMZ.ToString() + "; ";
                                }

                                string errorString = new string("");
                                foreach (double error in oxoniumIonFoundMassErrors)
                                {
                                    errorString = errorString + error.ToString("F6") + "; ";
                                }

                                outputOxo.Write(i + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmenationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                outputPeakDepth.Write(i + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmenationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                if (outputIPSA != null)
                                    outputIPSA.WriteLine(i + "\t" + peakString + "\t" + errorString + "\t");

                                foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                {
                                    outputOxo.Write(oxoIon.intensity + "\t");

                                    if (oxoIon.peakDepth == arbitraryPeakDepthIfNotFound)
                                    {
                                        outputPeakDepth.Write("NotFound\t");

                                    }
                                    else
                                    {
                                        outputPeakDepth.Write(oxoIon.peakDepth + "\t");
                                        oxoRanks.Add(oxoIon.peakDepth);
                                        if (hcdTrue && oxoIon.peakDepth <= peakDepthThreshold_hcd)
                                            countOxoWithinPeakDepthThreshold++;

                                        if (etdTrue && oxoIon.peakDepth <= peakDepthThreshold_etd)
                                            countOxoWithinPeakDepthThreshold++;

                                        if (uvpdTrue && oxoIon.peakDepth <= peakDepthThreshold_uvpd)
                                            countOxoWithinPeakDepthThreshold++;
                                    }

                                }

                                double medianRanks = Statistics.Median(oxoRanks);
                                //the median peak depth has to be "higher" (i.e., less than) the peak depth threshold 
                                //considered also using the number of oxonium ions found has to be at least half to the total list looked for, but decided against it for now (what if big list?)
                                if (oxoniumIonHashSet.Count < 6)
                                    halfTotalList = 4;

                                if (oxoniumIonHashSet.Count > 15)
                                    halfTotalList = 8;

                                //if not using 204, the below test will fail by default, so we need to add this in to make sure we check the calculation even if 204 isn't being used.
                                if (!using204)
                                    test204 = true;

                                double oxoTICfraction = totalOxoSignal / scanTIC;

                                double oxoCountRequirement = 0;
                                if (hcdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_hcd_user > 0
                                        ? oxoCountRequirement_hcd_user
                                        : halfTotalList;
                                }
                                if (etdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_etd_user > 0
                                        ? oxoCountRequirement_etd_user
                                        : halfTotalList / 2;
                                }
                                if (uvpdTrue)
                                {
                                    oxoCountRequirement = oxoCountRequirement_uvpd_user > 0
                                        ? oxoCountRequirement_uvpd_user
                                        : halfTotalList;
                                }


                                //intensity differences for HCD and ETD means we need to have two different % TIC threshold values.
                                //changed this to not use median, but instead say the number of oxonium ions with peakdepth within user-deined threshold
                                //needs to be greater than half the total list (or its definitions given above
                                if (hcdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_hcd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_hcd++;
                                }


                                //etd also differs in peak depth, so changed scaled this by 1.5
                                if (etdTrue && numberOfOxoIons >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_etd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_etd++;
                                }

                                if (uvpdTrue && numberOfOxoIons >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_uvpd)
                                {
                                    likelyGlycoSpectrum = true;
                                    numberScansCountedLikelyGlyco_uvpd++;
                                }


                                outputOxo.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);
                                outputPeakDepth.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);

                                outputOxo.WriteLine();
                                outputPeakDepth.WriteLine();
                            }
                            FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                            FinishTimeLabel.Refresh();
                        }
                    }

                    double percentage1ox = (double)numberOfMS2scansWithOxo_1 / (double)numberOfMS2scans * 100;
                    double percentage2ox = (double)numberOfMS2scansWithOxo_2 / (double)numberOfMS2scans * 100;
                    double percentage3ox = (double)numberOfMS2scansWithOxo_3 / (double)numberOfMS2scans * 100;
                    double percentage4ox = (double)numberOfMS2scansWithOxo_4 / (double)numberOfMS2scans * 100;
                    double percentage5plusox = (double)numberOfMS2scansWithOxo_5plus / (double)numberOfMS2scans * 100;
                    double percentageSum = percentage1ox + percentage2ox + percentage3ox + percentage4ox + percentage5plusox;
                    double numberofMS2scansWithOxo_double = (double)percentageSum / 100 * numberOfMS2scans;

                    double percentage1ox_hcd = (double)numberOfMS2scansWithOxo_1_hcd / (double)numberOfHCDscans * 100;
                    double percentage2ox_hcd = (double)numberOfMS2scansWithOxo_2_hcd / (double)numberOfHCDscans * 100;
                    double percentage3ox_hcd = (double)numberOfMS2scansWithOxo_3_hcd / (double)numberOfHCDscans * 100;
                    double percentage4ox_hcd = (double)numberOfMS2scansWithOxo_4_hcd / (double)numberOfHCDscans * 100;
                    double percentage5plusox_hcd = (double)numberOfMS2scansWithOxo_5plus_hcd / (double)numberOfHCDscans * 100;
                    double percentageSum_hcd = percentage1ox_hcd + percentage2ox_hcd + percentage3ox_hcd + percentage4ox_hcd + percentage5plusox_hcd;
                    double numberofHCDscansWithOxo_double = percentageSum_hcd / 100 * numberOfHCDscans;

                    double percentage1ox_etd = (double)numberOfMS2scansWithOxo_1_etd / (double)numberOfETDscans * 100;
                    double percentage2ox_etd = (double)numberOfMS2scansWithOxo_2_etd / (double)numberOfETDscans * 100;
                    double percentage3ox_etd = (double)numberOfMS2scansWithOxo_3_etd / (double)numberOfETDscans * 100;
                    double percentage4ox_etd = (double)numberOfMS2scansWithOxo_4_etd / (double)numberOfETDscans * 100;
                    double percentage5plusox_etd = (double)numberOfMS2scansWithOxo_5plus_etd / (double)numberOfETDscans * 100;
                    double percentageSum_etd = percentage1ox_etd + percentage2ox_etd + percentage3ox_etd + percentage4ox_etd + percentage5plusox_etd;
                    double numberofETDscansWithOxo_double = percentageSum_etd / 100 * numberOfETDscans;

                    double percentage1ox_uvpd = (double)numberOfMS2scansWithOxo_1_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage2ox_uvpd = (double)numberOfMS2scansWithOxo_2_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage3ox_uvpd = (double)numberOfMS2scansWithOxo_3_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage4ox_uvpd = (double)numberOfMS2scansWithOxo_4_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage5plusox_uvpd = (double)numberOfMS2scansWithOxo_5plus_uvpd / (double)numberOfUVPDscans * 100;
                    double percentageSum_uvpd = percentage1ox_uvpd + percentage2ox_uvpd + percentage3ox_uvpd + percentage4ox_uvpd + percentage5plusox_uvpd;
                    double numberofUVPDscansWithOxo_double = percentageSum_uvpd / 100 * numberOfUVPDscans;

                    numberScansCountedLikelyGlyco_total = numberScansCountedLikelyGlyco_hcd + numberScansCountedLikelyGlyco_etd + numberScansCountedLikelyGlyco_uvpd;
                    double percentageLikelyGlyco_total = (double)numberScansCountedLikelyGlyco_total / (double)numberOfMS2scans * 100;
                    double percentageLikelyGlyco_hcd = (double)numberScansCountedLikelyGlyco_hcd / (double)numberOfHCDscans * 100;
                    double percentageLikelyGlyco_etd = (double)numberScansCountedLikelyGlyco_etd / (double)numberOfETDscans * 100;
                    double percentageLikelyGlyco_uvpd = (double)numberScansCountedLikelyGlyco_uvpd / (double)numberOfUVPDscans * 100;

                    double percentageHCD = (double)numberOfHCDscans / numberOfMS2scans;
                    double percentageETD = (double)numberOfETDscans / numberOfMS2scans;
                    double percentageUVPD = (double)numberOfUVPDscans / numberOfMS2scans;

                    int numberofMS2scansWithOxo = (int)Math.Round(numberofMS2scansWithOxo_double);
                    int numberofHCDscansWithOxo = (int)Math.Round(numberofHCDscansWithOxo_double);
                    int numberofETDscansWithOxo = (int)Math.Round(numberofETDscansWithOxo_double);
                    int numberofUVPDscansWithOxo = (int)Math.Round(numberofUVPDscansWithOxo_double);

                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");
                    outputSummary.WriteLine("MS/MS Scans\t" + numberOfMS2scans + "\t" + numberOfHCDscans + "\t" + numberOfETDscans + "\t" + numberOfUVPDscans
                        + "\t" + 100 + "\t" + percentageHCD + "\t" + percentageETD + "\t" + percentageUVPD);
                    outputSummary.WriteLine("MS/MS Scans with OxoIons\t" + numberofMS2scansWithOxo + "\t" + numberofHCDscansWithOxo + "\t" + numberofETDscansWithOxo + "\t" + numberofUVPDscansWithOxo
                        + "\t" + percentageSum + "\t" + percentageSum_hcd + "\t" + percentageSum_etd + "\t" + percentageSum_uvpd);
                    outputSummary.WriteLine("Likely Glyco\t" + numberScansCountedLikelyGlyco_total + "\t" + numberScansCountedLikelyGlyco_hcd + "\t" + numberScansCountedLikelyGlyco_etd + "\t" + numberScansCountedLikelyGlyco_uvpd
                        + "\t" + percentageLikelyGlyco_total + "\t" + percentageLikelyGlyco_hcd + "\t" + percentageLikelyGlyco_etd + "\t" + percentageLikelyGlyco_uvpd);
                    outputSummary.WriteLine("OxoCount_1\t" + numberOfMS2scansWithOxo_1 + "\t" + numberOfMS2scansWithOxo_1_hcd + "\t" + numberOfMS2scansWithOxo_1_etd + "\t" + numberOfMS2scansWithOxo_1_uvpd
                        + "\t" + percentage1ox + "\t" + percentage1ox_hcd + "\t" + percentage1ox_etd + "\t" + percentage1ox_uvpd);
                    outputSummary.WriteLine("OxoCount_2\t" + numberOfMS2scansWithOxo_2 + "\t" + numberOfMS2scansWithOxo_2_hcd + "\t" + numberOfMS2scansWithOxo_2_etd + "\t" + numberOfMS2scansWithOxo_2_uvpd
                        + "\t" + percentage2ox + "\t" + percentage2ox_hcd + "\t" + percentage2ox_etd + "\t" + percentage2ox_uvpd);
                    outputSummary.WriteLine("OxoCount_3\t" + numberOfMS2scansWithOxo_3 + "\t" + numberOfMS2scansWithOxo_3_hcd + "\t" + numberOfMS2scansWithOxo_3_etd + "\t" + numberOfMS2scansWithOxo_3_uvpd
                        + "\t" + percentage3ox + "\t" + percentage3ox_hcd + "\t" + percentage3ox_etd + "\t" + percentage3ox_uvpd);
                    outputSummary.WriteLine("OxoCount_4\t" + numberOfMS2scansWithOxo_4 + "\t" + numberOfMS2scansWithOxo_4_hcd + "\t" + numberOfMS2scansWithOxo_4_etd + "\t" + numberOfMS2scansWithOxo_4_uvpd
                        + "\t" + percentage4ox + "\t" + percentage4ox_hcd + "\t" + percentage4ox_etd + "\t" + percentage4ox_uvpd);
                    outputSummary.WriteLine("OxoCount_5+\t" + numberOfMS2scansWithOxo_5plus + "\t" + numberOfMS2scansWithOxo_5plus_hcd + "\t" + numberOfMS2scansWithOxo_5plus_etd + "\t" + numberOfMS2scansWithOxo_5plus_uvpd
                        + "\t" + percentage5plusox + "\t" + percentage5plusox_hcd + "\t" + percentage5plusox_etd + "\t" + percentage5plusox_uvpd);

                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");

                    string currentGlycanSource = "";
                    foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                    {
                        int total = oxoIon.hcdCount + oxoIon.etdCount;

                        double percentTotal = (double)total / (double)numberOfMS2scans * 100;
                        double percentHCD = (double)oxoIon.hcdCount / (double)numberOfHCDscans * 100;
                        double percentETD = (double)oxoIon.etdCount / (double)numberOfETDscans * 100;
                        double percentUVPD = (double)oxoIon.etdCount / (double)numberOfUVPDscans * 100;

                        if (!currentGlycanSource.Equals(oxoIon.glycanSource))
                        {
                            outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + oxoIon.glycanSource + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                            currentGlycanSource = oxoIon.glycanSource;
                        }

                        outputSummary.WriteLine(oxoIon.description + "\t" + total + "\t" + oxoIon.hcdCount + "\t" + oxoIon.etdCount + "\t" + oxoIon.uvpdCount
                            + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD + "\t" + percentUVPD);
                    }

                    outputSummary.Close();
                    outputOxo.Close();
                    outputPeakDepth.Close();
                    if (outputIPSA != null)
                        outputIPSA.Close();
                    rawFile.Dispose();
                }

                else if (filePath.EndsWith(".mzML"))
                {

                    StatusLabel.Text = "Current file: " + filePath;
                    FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                    //Debug.WriteLine("Current file: " + rawFile.Name);
                    StatusLabel.Refresh();
                    FinishTimeLabel.Refresh();

                    int numberOfMS2scansWithOxo_1 = 0;
                    int numberOfMS2scansWithOxo_2 = 0;
                    int numberOfMS2scansWithOxo_3 = 0;
                    int numberOfMS2scansWithOxo_4 = 0;
                    int numberOfMS2scansWithOxo_5plus = 0;
                    int numberOfMS2scansWithOxo_1_hcd = 0;
                    int numberOfMS2scansWithOxo_2_hcd = 0;
                    int numberOfMS2scansWithOxo_3_hcd = 0;
                    int numberOfMS2scansWithOxo_4_hcd = 0;
                    int numberOfMS2scansWithOxo_5plus_hcd = 0;
                    int numberOfMS2scansWithOxo_1_etd = 0;
                    int numberOfMS2scansWithOxo_2_etd = 0;
                    int numberOfMS2scansWithOxo_3_etd = 0;
                    int numberOfMS2scansWithOxo_4_etd = 0;
                    int numberOfMS2scansWithOxo_5plus_etd = 0;
                    int numberOfMS2scansWithOxo_1_uvpd = 0;
                    int numberOfMS2scansWithOxo_2_uvpd = 0;
                    int numberOfMS2scansWithOxo_3_uvpd = 0;
                    int numberOfMS2scansWithOxo_4_uvpd = 0;
                    int numberOfMS2scansWithOxo_5plus_uvpd = 0;
                    int numberOfMS2scans = 0;
                    int numberOfHCDscans = 0;
                    int numberOfETDscans = 0;
                    int numberOfUVPDscans = 0;
                    int numberScansCountedLikelyGlyco_total = 0;
                    int numberScansCountedLikelyGlyco_hcd = 0;
                    int numberScansCountedLikelyGlyco_etd = 0;
                    int numberScansCountedLikelyGlyco_uvpd = 0;
                    bool firstSpectrumInFile = true;
                    bool likelyGlycoSpectrum = false;

                    double halfTotalList = (double)oxoniumIonHashSet.Count / 2.0;

                    StreamWriter outputOxo = new StreamWriter(filePath + "_GlyCounter_OxoSignal.txt");
                    StreamWriter outputPeakDepth = new StreamWriter(filePath + "_GlyCounter_OxoPeakDepth.txt");
                    StreamWriter outputIPSA = null;
                    if (ipsaCheckBox.Checked)
                    {
                        outputIPSA = new StreamWriter(filePath + "_Glycounter_IPSA.txt");
                    }
                    StreamWriter outputSummary = new StreamWriter(filePath + "_GlyCounter_Summary.txt");

                    outputOxo.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    outputPeakDepth.Write("ScanNumber\tRetentionTime\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tParentScan\tNumOxonium\tTotalOxoSignal\t");
                    if (outputIPSA != null)
                        outputIPSA.WriteLine("ScanNumber\tOxoniumIons\tMassError\t");
                    /*
                    outputSummary.WriteLine("Settings\tppmTol:\t" + ppmTolerance + "\tSNthreshold:\t" + SNthreshold + "\tHCDPeakDepthThreshold:\t" + peakDepthThreshold_hcd
                        + "\tETDPeakDepthThreshold:\t" + peakDepthThreshold_etd + "\tHCD TIC fraction:\t" + oxoTICfractionThreshold_hcd + "\tETD TIC fraction:\t" + oxoTICfractionThreshold_etd);
                    */
                    outputSummary.WriteLine("Settings:\t" + toleranceString + tol + ", SNthreshold=" + SNthreshold + ", IntensityThreshold=" + intensityThreshold + ", PeakDepthThreshold_HCD=" + peakDepthThreshold_hcd
                                            + ", PeakDepthThreshold_ETD=" + peakDepthThreshold_etd + ", PeakDepthThreshold_UVPD=" + peakDepthThreshold_uvpd + ", TICfraction_HCD=" + oxoTICfractionThreshold_hcd
                                            + ", TICfraction_ETD=" + oxoTICfractionThreshold_etd + ", TICfraction_UVPD=" + oxoTICfractionThreshold_uvpd);
                    outputSummary.WriteLine(VersionNumber_Label.Text + ", " + StartTimeLabel.Text);
                    outputSummary.WriteLine();

                    using (var reader = new SimpleMzMLReader(filePath, true, true))
                    {
                        var specCount = 0;
                        foreach (var spec in reader.ReadAllSpectra(true))
                        {
                            var paramsList = spec.CVParams;

                            if (spec.MsLevel == 2)
                            {

                                var precursors = spec.Precursors;
                                var precursor = precursors[0];

                                numberOfMS2scans++;
                                int numberOfOxoIons = 0;
                                double totalOxoSignal = 0;
                                likelyGlycoSpectrum = false;
                                bool test204 = false;
                                int countOxoWithinPeakDepthThreshold = 0;

                                bool hcdTrue = false;
                                bool etdTrue = false;
                                bool uvpdTrue = false;

                                List<double> oxoniumIonFoundPeaks = new List<double>();
                                List<double> oxoniumIonFoundMassErrors = new List<double>();

                                switch (precursor.ActivationMethod)
                                {
                                    case "beam-type collision-induced dissociation":
                                        numberOfHCDscans++;
                                        hcdTrue = true;
                                        break;
                                    case "collision-induced dissociation":
                                        numberOfHCDscans++;
                                        hcdTrue = true;
                                        break;
                                    case ", supplemental beam-type collision-induced dissociation":
                                        numberOfETDscans++;
                                        etdTrue = true;
                                        break;
                                    case "electron transfer dissociation":
                                        numberOfETDscans++;
                                        etdTrue = true;
                                        break;
                                    case "electron capture dissociation":
                                        numberOfETDscans++;
                                        etdTrue = true;
                                        break;
                                    case "photodissociation":
                                        numberOfUVPDscans++;
                                        uvpdTrue = true;
                                        break;
                                    default:
                                        Debug.WriteLine(precursor.ActivationMethod.ToString());
                                        break;
                                }

                                string oxoIonHeader = "";

                                if (spec.TotalIonCurrent > 0)
                                {

                                    Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                                    RankOrderPeaks_mzml(sortedPeakDepths, spec);

                                    foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                    {
                                        oxoIon.intensity = 0;
                                        oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;

                                        oxoIonHeader = oxoIonHeader + oxoIon.description + "\t";
                                        oxoIon.measuredMZ = 0;
                                        oxoIon.intensity = 0;

                                        //Trace.WriteLine("Scan: " + i);
                                        SimpleMzMLReader.Peak peak = GetPeak_mzml(spec, oxoIon.theoMZ, usingda, tol);

                                        if (peak.Intensity > intensityThreshold)
                                        {
                                            oxoIon.measuredMZ = peak.Mz;
                                            oxoIon.intensity = peak.Intensity;
                                            oxoIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                            numberOfOxoIons++;
                                            totalOxoSignal = totalOxoSignal + peak.Intensity;

                                            if (hcdTrue)
                                                oxoIon.hcdCount++;
                                            if (etdTrue)
                                                oxoIon.etdCount++;
                                            if (uvpdTrue)
                                                oxoIon.uvpdCount++;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_hcd && hcdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_etd && etdTrue)
                                                test204 = true;

                                            if (oxoIon.theoMZ == 204.0867 && sortedPeakDepths[peak.Intensity] <= peakDepthThreshold_uvpd && uvpdTrue)
                                                test204 = true;

                                            oxoniumIonFoundPeaks.Add(oxoIon.theoMZ);
                                            var massError = oxoIon.measuredMZ - oxoIon.theoMZ;
                                            oxoniumIonFoundMassErrors.Add(massError);
                                        }
                                    }
                                }



                                if (firstSpectrumInFile)
                                {
                                    outputOxo.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                    outputPeakDepth.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                    //outputSummary.WriteLine("Oxonium Ions Searched for:\t" + oxoIonHeader);
                                    firstSpectrumInFile = false;
                                }


                                if (numberOfOxoIons > 0)
                                {
                                    if (numberOfOxoIons == 1)
                                    {
                                        numberOfMS2scansWithOxo_1++;
                                        if (hcdTrue)
                                            numberOfMS2scansWithOxo_1_hcd++;
                                        if (etdTrue)
                                            numberOfMS2scansWithOxo_1_etd++;
                                        if (uvpdTrue)
                                            numberOfMS2scansWithOxo_1_uvpd++;
                                    }
                                    if (numberOfOxoIons == 2)
                                    {
                                        numberOfMS2scansWithOxo_2++;
                                        if (hcdTrue)
                                            numberOfMS2scansWithOxo_2_hcd++;
                                        if (etdTrue)
                                            numberOfMS2scansWithOxo_2_etd++;
                                        if (uvpdTrue)
                                            numberOfMS2scansWithOxo_2_uvpd++;
                                    }
                                    if (numberOfOxoIons == 3)
                                    {
                                        numberOfMS2scansWithOxo_3++;
                                        if (hcdTrue)
                                            numberOfMS2scansWithOxo_3_hcd++;
                                        if (etdTrue)
                                            numberOfMS2scansWithOxo_3_etd++;
                                        if (uvpdTrue)
                                            numberOfMS2scansWithOxo_3_uvpd++;
                                    }
                                    if (numberOfOxoIons == 4)
                                    {
                                        numberOfMS2scansWithOxo_4++;
                                        if (hcdTrue)
                                            numberOfMS2scansWithOxo_4_hcd++;
                                        if (etdTrue)
                                            numberOfMS2scansWithOxo_4_etd++;
                                        if (uvpdTrue)
                                            numberOfMS2scansWithOxo_4_uvpd++;
                                    }
                                    if (numberOfOxoIons > 4)
                                    {
                                        numberOfMS2scansWithOxo_5plus++;
                                        if (hcdTrue)
                                            numberOfMS2scansWithOxo_5plus_hcd++;
                                        if (etdTrue)
                                            numberOfMS2scansWithOxo_5plus_etd++;
                                        if (uvpdTrue)
                                            numberOfMS2scansWithOxo_5plus_uvpd++;
                                    }

                                    double? parentScan = 0;
                                    var refSpec = precursor.PrecursorSpectrumRef;
                                    string[] refList = refSpec.Split('=');

                                    if (refList.Length > 2)
                                    {
                                        if (double.TryParse(refList[3], out double result))
                                        {
                                            parentScan = result;
                                        }
                                    }

                                    else
                                    {
                                        parentScan = null;
                                    }

                                    try
                                    {
                                        double scanTIC = spec.TotalIonCurrent;

                                        string[] ITlist = new string[0];
                                        foreach (var param in paramsList)
                                        {
                                            if (param.ToString().Contains("ion injection time"))
                                            {
                                                ITlist = param.ToString().Split('"');
                                            }
                                        }

                                        double scanInjTime = 0;

                                        if (ITlist.Length != 0)
                                        {
                                            scanInjTime = double.Parse(ITlist[1], System.Globalization.NumberStyles.Float);
                                        }

                                        string fragmentationType = "";

                                        if (hcdTrue)
                                        {
                                            fragmentationType = "HCD";
                                        }
                                        if (etdTrue)
                                        {
                                            fragmentationType = "ETD";
                                        }
                                        if (uvpdTrue)
                                        {
                                            fragmentationType = "UVPD";
                                        }

                                        double retentionTime = spec.ScanStartTime;

                                        List<double> oxoRanks = new List<double>();

                                        string peakString = new string("");
                                        foreach (double theoMZ in oxoniumIonFoundPeaks)
                                        {
                                            peakString = peakString + theoMZ.ToString() + "; ";
                                        }

                                        string errorString = new string("");
                                        foreach (double error in oxoniumIonFoundMassErrors)
                                        {
                                            errorString = errorString + error.ToString("F6") + "; ";
                                        }

                                        outputOxo.Write(specCount + 1 + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                        outputPeakDepth.Write(specCount + 1 + "\t" + retentionTime + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
                                        if (outputIPSA != null)
                                            outputIPSA.WriteLine(specCount + 1 + "\t" + peakString + "\t" + errorString + "\t");

                                        foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                        {
                                            outputOxo.Write(oxoIon.intensity + "\t");

                                            if (oxoIon.peakDepth == arbitraryPeakDepthIfNotFound)
                                            {
                                                outputPeakDepth.Write("NotFound\t");

                                            }
                                            else
                                            {
                                                outputPeakDepth.Write(oxoIon.peakDepth + "\t");
                                                oxoRanks.Add(oxoIon.peakDepth);
                                                if (hcdTrue && oxoIon.peakDepth <= peakDepthThreshold_hcd)
                                                    countOxoWithinPeakDepthThreshold++;

                                                if (etdTrue && oxoIon.peakDepth <= peakDepthThreshold_etd)
                                                    countOxoWithinPeakDepthThreshold++;

                                                if (uvpdTrue && oxoIon.peakDepth <= peakDepthThreshold_uvpd)
                                                    countOxoWithinPeakDepthThreshold++;
                                            }

                                        }

                                        double medianRanks = Statistics.Median(oxoRanks);
                                        //the median peak depth has to be "higher" (i.e., less than) the peak depth threshold 
                                        //considered also using the number of oxonium ions found has to be at least half to the total list looked for, but decided against it for now (what if big list?)
                                        if (oxoniumIonHashSet.Count < 6)
                                            halfTotalList = 4;
                                        if (oxoniumIonHashSet.Count > 15)
                                            halfTotalList = 8;

                                        //if not using 204, the below test will fail by default, so we need to add this in to make sure we check the calculation even if 204 isn't being used.
                                        if (!using204)
                                            test204 = true;

                                        double oxoTICfraction = totalOxoSignal / scanTIC;

                                        double oxoCountRequirement = 0;
                                        if (hcdTrue)
                                        {
                                            oxoCountRequirement = oxoCountRequirement_hcd_user > 0
                                                ? oxoCountRequirement_hcd_user
                                                : halfTotalList;
                                        }
                                        if (etdTrue)
                                        {
                                            oxoCountRequirement = oxoCountRequirement_etd_user > 0
                                                ? oxoCountRequirement_etd_user
                                                : halfTotalList / 2;
                                        }
                                        if (uvpdTrue)
                                        {
                                            oxoCountRequirement = oxoCountRequirement_uvpd_user > 0
                                                ? oxoCountRequirement_uvpd_user
                                                : halfTotalList;
                                        }


                                        //intensity differences for HCD and ETD means we need to have two different % TIC threshold values.
                                        //changed this to not use median, but instead say the number of oxonium ions with peakdepth within user-deined threshold
                                        //needs to be greater than half the total list (or its definitions given above
                                        if (hcdTrue && countOxoWithinPeakDepthThreshold >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_hcd)
                                        {
                                            likelyGlycoSpectrum = true;
                                            numberScansCountedLikelyGlyco_hcd++;
                                        }


                                        //etd also differs in peak depth, so changed scaled this by 1.5
                                        if (etdTrue && numberOfOxoIons >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_etd)
                                        {
                                            likelyGlycoSpectrum = true;
                                            numberScansCountedLikelyGlyco_etd++;
                                        }

                                        if (uvpdTrue && numberOfOxoIons >= oxoCountRequirement && test204 && oxoTICfraction >= oxoTICfractionThreshold_uvpd)
                                        {
                                            likelyGlycoSpectrum = true;
                                            numberScansCountedLikelyGlyco_uvpd++;
                                        }


                                        outputOxo.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);
                                        outputPeakDepth.Write(countOxoWithinPeakDepthThreshold + "\t" + oxoCountRequirement + "\t" + oxoTICfraction + "\t" + likelyGlycoSpectrum);

                                        outputOxo.WriteLine();
                                        outputPeakDepth.WriteLine();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    }


                                }

                                FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                                FinishTimeLabel.Refresh();
                            }
                            specCount++;
                        }
                    }


                    double percentage1ox = (double)numberOfMS2scansWithOxo_1 / (double)numberOfMS2scans * 100;
                    double percentage2ox = (double)numberOfMS2scansWithOxo_2 / (double)numberOfMS2scans * 100;
                    double percentage3ox = (double)numberOfMS2scansWithOxo_3 / (double)numberOfMS2scans * 100;
                    double percentage4ox = (double)numberOfMS2scansWithOxo_4 / (double)numberOfMS2scans * 100;
                    double percentage5plusox = (double)numberOfMS2scansWithOxo_5plus / (double)numberOfMS2scans * 100;
                    double percentageSum = percentage1ox + percentage2ox + percentage3ox + percentage4ox + percentage5plusox;
                    double numberofMS2scansWithOxo_double = (double)percentageSum / 100 * numberOfMS2scans;

                    double percentage1ox_hcd = (double)numberOfMS2scansWithOxo_1_hcd / (double)numberOfHCDscans * 100;
                    double percentage2ox_hcd = (double)numberOfMS2scansWithOxo_2_hcd / (double)numberOfHCDscans * 100;
                    double percentage3ox_hcd = (double)numberOfMS2scansWithOxo_3_hcd / (double)numberOfHCDscans * 100;
                    double percentage4ox_hcd = (double)numberOfMS2scansWithOxo_4_hcd / (double)numberOfHCDscans * 100;
                    double percentage5plusox_hcd = (double)numberOfMS2scansWithOxo_5plus_hcd / (double)numberOfHCDscans * 100;
                    double percentageSum_hcd = percentage1ox_hcd + percentage2ox_hcd + percentage3ox_hcd + percentage4ox_hcd + percentage5plusox_hcd;
                    double numberofHCDscansWithOxo_double = percentageSum_hcd / 100 * numberOfHCDscans;

                    double percentage1ox_etd = (double)numberOfMS2scansWithOxo_1_etd / (double)numberOfETDscans * 100;
                    double percentage2ox_etd = (double)numberOfMS2scansWithOxo_2_etd / (double)numberOfETDscans * 100;
                    double percentage3ox_etd = (double)numberOfMS2scansWithOxo_3_etd / (double)numberOfETDscans * 100;
                    double percentage4ox_etd = (double)numberOfMS2scansWithOxo_4_etd / (double)numberOfETDscans * 100;
                    double percentage5plusox_etd = (double)numberOfMS2scansWithOxo_5plus_etd / (double)numberOfETDscans * 100;
                    double percentageSum_etd = percentage1ox_etd + percentage2ox_etd + percentage3ox_etd + percentage4ox_etd + percentage5plusox_etd;
                    double numberofETDscansWithOxo_double = percentageSum_etd / 100 * numberOfETDscans;

                    double percentage1ox_uvpd = (double)numberOfMS2scansWithOxo_1_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage2ox_uvpd = (double)numberOfMS2scansWithOxo_2_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage3ox_uvpd = (double)numberOfMS2scansWithOxo_3_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage4ox_uvpd = (double)numberOfMS2scansWithOxo_4_uvpd / (double)numberOfUVPDscans * 100;
                    double percentage5plusox_uvpd = (double)numberOfMS2scansWithOxo_5plus_uvpd / (double)numberOfUVPDscans * 100;
                    double percentageSum_uvpd = percentage1ox_uvpd + percentage2ox_uvpd + percentage3ox_uvpd + percentage4ox_uvpd + percentage5plusox_uvpd;
                    double numberofUVPDscansWithOxo_double = percentageSum_uvpd / 100 * numberOfUVPDscans;

                    numberScansCountedLikelyGlyco_total = numberScansCountedLikelyGlyco_hcd + numberScansCountedLikelyGlyco_etd + numberScansCountedLikelyGlyco_uvpd;
                    double percentageLikelyGlyco_total = (double)numberScansCountedLikelyGlyco_total / (double)numberOfMS2scans * 100;
                    double percentageLikelyGlyco_hcd = (double)numberScansCountedLikelyGlyco_hcd / (double)numberOfHCDscans * 100;
                    double percentageLikelyGlyco_etd = (double)numberScansCountedLikelyGlyco_etd / (double)numberOfETDscans * 100;
                    double percentageLikelyGlyco_uvpd = (double)numberScansCountedLikelyGlyco_uvpd / (double)numberOfUVPDscans * 100;

                    double percentageHCD = (double)numberOfHCDscans / numberOfMS2scans;
                    double percentageETD = (double)numberOfETDscans / numberOfMS2scans;
                    double percentageUVPD = (double)numberOfUVPDscans / numberOfMS2scans;

                    int numberofMS2scansWithOxo = (int)Math.Round(numberofMS2scansWithOxo_double);
                    int numberofHCDscansWithOxo = (int)Math.Round(numberofHCDscansWithOxo_double);
                    int numberofETDscansWithOxo = (int)Math.Round(numberofETDscansWithOxo_double);
                    int numberofUVPDscansWithOxo = (int)Math.Round(numberofUVPDscansWithOxo_double);

                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");
                    outputSummary.WriteLine("MS/MS Scans\t" + numberOfMS2scans + "\t" + numberOfHCDscans + "\t" + numberOfETDscans + "\t" + numberOfUVPDscans
                        + "\t" + 100 + "\t" + percentageHCD + "\t" + percentageETD + "\t" + percentageUVPD);
                    outputSummary.WriteLine("MS/MS Scans with OxoIons\t" + numberofMS2scansWithOxo + "\t" + numberofHCDscansWithOxo + "\t" + numberofETDscansWithOxo + "\t" + numberofUVPDscansWithOxo
                        + "\t" + percentageSum + "\t" + percentageSum_hcd + "\t" + percentageSum_etd + "\t" + percentageSum_uvpd);
                    outputSummary.WriteLine("Likely Glyco\t" + numberScansCountedLikelyGlyco_total + "\t" + numberScansCountedLikelyGlyco_hcd + "\t" + numberScansCountedLikelyGlyco_etd + "\t" + numberScansCountedLikelyGlyco_uvpd
                        + "\t" + percentageLikelyGlyco_total + "\t" + percentageLikelyGlyco_hcd + "\t" + percentageLikelyGlyco_etd + "\t" + percentageLikelyGlyco_uvpd);
                    outputSummary.WriteLine("OxoCount_1\t" + numberOfMS2scansWithOxo_1 + "\t" + numberOfMS2scansWithOxo_1_hcd + "\t" + numberOfMS2scansWithOxo_1_etd + "\t" + numberOfMS2scansWithOxo_1_uvpd
                        + "\t" + percentage1ox + "\t" + percentage1ox_hcd + "\t" + percentage1ox_etd + "\t" + percentage1ox_uvpd);
                    outputSummary.WriteLine("OxoCount_2\t" + numberOfMS2scansWithOxo_2 + "\t" + numberOfMS2scansWithOxo_2_hcd + "\t" + numberOfMS2scansWithOxo_2_etd + "\t" + numberOfMS2scansWithOxo_2_uvpd
                        + "\t" + percentage2ox + "\t" + percentage2ox_hcd + "\t" + percentage2ox_etd + "\t" + percentage2ox_uvpd);
                    outputSummary.WriteLine("OxoCount_3\t" + numberOfMS2scansWithOxo_3 + "\t" + numberOfMS2scansWithOxo_3_hcd + "\t" + numberOfMS2scansWithOxo_3_etd + "\t" + numberOfMS2scansWithOxo_3_uvpd
                        + "\t" + percentage3ox + "\t" + percentage3ox_hcd + "\t" + percentage3ox_etd + "\t" + percentage3ox_uvpd);
                    outputSummary.WriteLine("OxoCount_4\t" + numberOfMS2scansWithOxo_4 + "\t" + numberOfMS2scansWithOxo_4_hcd + "\t" + numberOfMS2scansWithOxo_4_etd + "\t" + numberOfMS2scansWithOxo_4_uvpd
                        + "\t" + percentage4ox + "\t" + percentage4ox_hcd + "\t" + percentage4ox_etd + "\t" + percentage4ox_uvpd);
                    outputSummary.WriteLine("OxoCount_5+\t" + numberOfMS2scansWithOxo_5plus + "\t" + numberOfMS2scansWithOxo_5plus_hcd + "\t" + numberOfMS2scansWithOxo_5plus_etd + "\t" + numberOfMS2scansWithOxo_5plus_uvpd
                        + "\t" + percentage5plusox + "\t" + percentage5plusox_hcd + "\t" + percentage5plusox_etd + "\t" + percentage5plusox_uvpd);

                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                    outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");

                    string currentGlycanSource = "";
                    foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                    {
                        int total = oxoIon.hcdCount + oxoIon.etdCount + oxoIon.uvpdCount;

                        double percentTotal = (double)total / (double)numberOfMS2scans * 100;
                        double percentHCD = (double)oxoIon.hcdCount / (double)numberOfHCDscans * 100;
                        double percentETD = (double)oxoIon.etdCount / (double)numberOfETDscans * 100;
                        double percentUVPD = (double)oxoIon.uvpdCount / (double)numberOfUVPDscans * 100;

                        if (!currentGlycanSource.Equals(oxoIon.glycanSource))
                        {
                            outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + oxoIon.glycanSource + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                            currentGlycanSource = oxoIon.glycanSource;
                        }

                        outputSummary.WriteLine(oxoIon.description + "\t" + total + "\t" + oxoIon.hcdCount + "\t" + oxoIon.etdCount + "\t" + oxoIon.uvpdCount
                            + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD + "\t" + percentUVPD);
                    }

                    outputSummary.Close();
                    outputOxo.Close();
                    outputPeakDepth.Close();
                    if (outputIPSA != null)
                        outputIPSA.Close();
                }
            }

            timer1.Stop();
            StatusLabel.Text = "Finished";
            FinishTimeLabel.Text = "Finished at: " + DateTime.Now.ToString("HH:mm:ss");
            MessageBox.Show("Finished.");
            oxoniumIonHashSet.Clear();
            //MessageBox.Show(filePath);


        }

        public static ThermoMzPeak GetPeak(ThermoSpectrum spectrum, double mz, bool usingda, double tolerance, bool IT = false)
        {
            DoubleRange rangeOxonium = new DoubleRange();

            if (usingda)
            {
                rangeOxonium = DoubleRange.FromDa(mz, tolerance);
            }
            else
            {
                rangeOxonium = DoubleRange.FromPPM(mz, tolerance);
            }

            List<ThermoMzPeak> peaks;

            //Trace.WriteLine("spectrum : " + rangeOxonium.ToString());
            if (!IT)
            {
                if (spectrum.TryGetPeaks(rangeOxonium, out peaks))
                {
                    peaks = peaks.OrderBy(x => x.SignalToNoise).ToList();
                }
            }
            //if the mass analyzer is an ion trap, order by intensity instead
            else
            {
                if (spectrum.TryGetPeaks(rangeOxonium, out peaks))
                {
                    peaks = peaks.OrderBy(x => x.Intensity).ToList();
                }
            }

            double diff = double.MaxValue;
            ThermoMzPeak returnPeak = null;
            foreach (ThermoMzPeak peak in peaks)
            {
                var currDiff = Math.Abs(peak.MZ - mz);
                if (currDiff < diff)
                {
                    diff = currDiff;
                    returnPeak = peak;
                }
            }
            return returnPeak;
        }

        public static SimpleMzMLReader.Peak GetPeak_mzml(SimpleMzMLReader.SimpleSpectrum spectrum, double mz, bool usingda, double tolerance)
        {
            //Create start and end m/z values
            DoubleRange rangeOxonium = new DoubleRange();
            if (usingda)
            {
                rangeOxonium = DoubleRange.FromDa(mz, tolerance);
            }
            else
            {
                rangeOxonium = DoubleRange.FromPPM(mz, tolerance);
            }

            var peaks = spectrum.Peaks;
            List<SimpleMzMLReader.Peak> peakList = new List<SimpleMzMLReader.Peak>();
            //ordering by intensity instead of S/N here  
            foreach (SimpleMzMLReader.Peak peak in peaks)
            {
                DoubleRange rangeExp = DoubleRange.FromPPM(peak.Mz, 0);
                if (rangeExp.IsSubRange(rangeOxonium))
                {
                    peakList.Add(peak);
                }
                peakList = peakList.OrderBy(peak => peak.Intensity).ToList();
            }

            double diff = double.MaxValue;
            SimpleMzMLReader.Peak returnPeak = new SimpleMzMLReader.Peak();

            foreach (SimpleMzMLReader.Peak peak in peakList)
            {
                double currDiff = Math.Abs(peak.Mz - mz);
                if (currDiff < diff)
                {
                    diff = currDiff;
                    returnPeak = peak;
                }
            }
            return returnPeak;
        }

        public static Dictionary<double, int> RankOrderPeaks(Dictionary<double, int> dictionary, ThermoSpectrum spectrum)
        {
            List<double> peakIntensities = spectrum.GetIntensities().ToList<double>();

            var sortedpeakIntensities = peakIntensities.OrderByDescending(x => x);

            int i = 1;
            foreach (double value in sortedpeakIntensities)
            {
                if (!dictionary.ContainsKey(value))
                {
                    dictionary.Add(value, i);
                    i++;
                }
            }

            //MessageBox.Show("Peaks: " + peakIntensities.Count + " , i: " + i);
            return dictionary;
        }

        public static Dictionary<double, int> RankOrderPeaks_mzml(Dictionary<double, int> dictionary, SimpleMzMLReader.SimpleSpectrum spectrum)
        {
            List<double> peakIntensities = spectrum.Intensities.ToList<double>();

            var sortedpeakIntensities = peakIntensities.OrderByDescending(x => x);

            int i = 1;
            foreach (double value in sortedpeakIntensities)
            {
                if (!dictionary.ContainsKey(value))
                {
                    dictionary.Add(value, i);
                    i++;
                }
            }

            //MessageBox.Show("Peaks: " + peakIntensities.Count + " , i: " + i);
            return dictionary;
        }

        private void SelectAllItems_CheckedBox(CheckedListBox cListBox)
        {
            for (int i = 0; i < cListBox.Items.Count; i++)
            {
                cListBox.SetItemChecked(i, true);
            }
        }

        private bool CanConvertDouble(string input, double type)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            return converter.IsValid(input);
        }

        private bool CanConvertInt(string input, int type)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            return converter.IsValid(input);
        }

        //set up check all button
        private void CheckAll_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(HexNAcCheckedListBox);
            SelectAllItems_CheckedBox(HexCheckedListBox);
            SelectAllItems_CheckedBox(SialicAcidCheckedListBox);
            SelectAllItems_CheckedBox(M6PCheckedListBox);
            SelectAllItems_CheckedBox(OligosaccharideCheckedListBox);
            SelectAllItems_CheckedBox(FucoseCheckedListBox);
        }

        //set up common oxonium ion list

        //set up check common buttom
        private void MostCommonButton_Click(object sender, EventArgs e)
        {
            //hexnac
            for (int i = 0; i < HexNAcCheckedListBox.Items.Count; i++)
            {
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("126."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("138."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("144."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("168."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("186."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
                if (HexNAcCheckedListBox.Items[i].ToString().Contains("204."))
                {
                    HexNAcCheckedListBox.SetItemChecked(i, true);
                }
            }

            //hexose
            for (int i = 0; i < HexCheckedListBox.Items.Count; i++)
            {
                if (HexCheckedListBox.Items[i].ToString().Contains("163."))
                {
                    HexCheckedListBox.SetItemChecked(i, true);
                }
            }

            //sialic
            for (int i = 0; i < SialicAcidCheckedListBox.Items.Count; i++)
            {
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("274."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("292."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("290."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
                if (SialicAcidCheckedListBox.Items[i].ToString().Contains("308."))
                {
                    SialicAcidCheckedListBox.SetItemChecked(i, true);
                }
            }

            //fucose
            for (int i = 0; i < FucoseCheckedListBox.Items.Count; i++)
            {
                if (FucoseCheckedListBox.Items[i].ToString().Contains("512."))
                {
                    FucoseCheckedListBox.SetItemChecked(i, true);
                }
            }

            //oligo
            for (int i = 0; i < OligosaccharideCheckedListBox.Items.Count; i++)
            {
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("366."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("657."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("673."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
                if (OligosaccharideCheckedListBox.Items[i].ToString().Contains("893."))
                {
                    OligosaccharideCheckedListBox.SetItemChecked(i, true);
                }
            }

            //M6P
            for (int i = 0; i < M6PCheckedListBox.Items.Count; i++)
            {
                if (M6PCheckedListBox.Items[i].ToString().Contains("243."))
                {
                    M6PCheckedListBox.SetItemChecked(i, true);
                }
            }

            //MessageBox.Show(HexNAcCheckedListBox.CheckedItems[0].ToString());
        }

        //set up clear all button
        private void ClearButton_Click(object sender, EventArgs e)
        {
            while (HexNAcCheckedListBox.CheckedIndices.Count > 0)
                HexNAcCheckedListBox.SetItemChecked(HexNAcCheckedListBox.CheckedIndices[0], false);

            while (HexCheckedListBox.CheckedIndices.Count > 0)
                HexCheckedListBox.SetItemChecked(HexCheckedListBox.CheckedIndices[0], false);

            while (SialicAcidCheckedListBox.CheckedIndices.Count > 0)
                SialicAcidCheckedListBox.SetItemChecked(SialicAcidCheckedListBox.CheckedIndices[0], false);

            while (M6PCheckedListBox.CheckedIndices.Count > 0)
                M6PCheckedListBox.SetItemChecked(M6PCheckedListBox.CheckedIndices[0], false);

            while (OligosaccharideCheckedListBox.CheckedIndices.Count > 0)
                OligosaccharideCheckedListBox.SetItemChecked(OligosaccharideCheckedListBox.CheckedIndices[0], false);

            while (FucoseCheckedListBox.CheckedIndices.Count > 0)
                FucoseCheckedListBox.SetItemChecked(FucoseCheckedListBox.CheckedIndices[0], false);

            HexNAcCheckedListBox.ClearSelected();
            HexCheckedListBox.ClearSelected();
            SialicAcidCheckedListBox.ClearSelected();
            M6PCheckedListBox.ClearSelected();
            OligosaccharideCheckedListBox.ClearSelected();
            FucoseCheckedListBox.ClearSelected();

        }

        //uncheck specific boxes
        private void OligosaccharideCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            OligosaccharideCheckedListBox.ClearSelected();
        }

        private void HexNAcCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexNAcCheckedListBox.ClearSelected();
        }

        private void HexCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexCheckedListBox.ClearSelected();
        }

        private void SialicAcidCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SialicAcidCheckedListBox.ClearSelected();
        }

        private void M6PCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            M6PCheckedListBox.ClearSelected();
        }

        private void FucoseCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FucoseCheckedListBox.ClearSelected();
        }

        //set up check all buttons for specific types
        private void CheckAll_HexNAc_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(HexNAcCheckedListBox);
        }

        private void CheckAll_Hex_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(HexCheckedListBox);
        }

        private void CheckAll_Sialic_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(SialicAcidCheckedListBox);
        }

        private void CheckAll_M6P_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(M6PCheckedListBox);
        }

        private void CheckAll_Oligo_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(OligosaccharideCheckedListBox);
        }

        private void CheckAll_Fucose_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(FucoseCheckedListBox);
        }

        //set up upload custom oxonium ions
        private void UploadCustomBrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\";
            }
            fdlg.Filter = "*.csv|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                uploadCustomTextBox.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }
        }
        //set variable to be custom ions file path
        private void uploadCustomTextBox_TextChanged_1(object sender, EventArgs e)
        {
            csvCustomFile = uploadCustomTextBox.Text;
        }

        //set up timer
        private void OnTimerTick(object sender, EventArgs e)
        {
            FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
            FinishTimeLabel.Refresh();
        }

        /////////////////////////////////////////////////////
        /// This starts the code for Ynaught
        /////////////////////////////////////////////////////

        //find glycopeptide ID .txt file to know what peptides to look for to make Y-ions
        private void BrowseGlycoPepIDs_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "*.txt|*.txt";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadInGlycoPepIDs_TextBox.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }

        //find the glycan mass list
        private void BrowseGlycans_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "*.txt|*.txt";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadInGlycanMasses_TextBox.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }
        //find the raw file to look for Y-ions
        private void BrowseGlycoPepRawFiles_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "All files (*.raw*)|*.raw*|All files (*.raw*)|*.raw*";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadInGlycoPepRawFile_TextBox.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }

        //set up custom additions for Y-ion upload
        private void BrowseCustomAdditions_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "*.csv|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                Ynaught_CustomAdditions_TextBox.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }

        //set up custom substractions for Y-ion upload
        private void BrowseCustomSubtractions_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }
            fdlg.Filter = "*.csv|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                Ynaught_CustomSubtractions_TextBox.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }

        //setting variables to file paths so they can be processed later
        private void LoadInGlycoPepIDs_TextBox_TextChanged(object sender, EventArgs e)
        {
            Ynaught_pepIDFilePath = LoadInGlycoPepIDs_TextBox.Text;
        }

        //setting variables to file paths so they can be processed later
        private void LoadInGlycanMasses_TextBox_TextChanged_1(object sender, EventArgs e)
        {
            Ynaught_glycanMassesFilePath = LoadInGlycanMasses_TextBox.Text;
        }
        private void LoadInGlycoPepRawFile_TextBox_TextChanged(object sender, EventArgs e)
        {
            Ynaught_rawFilePath = LoadInGlycoPepRawFile_TextBox.Text;
        }

        private void Ynaught_CustomAdditions_TextBox_TextChanged(object sender, EventArgs e)
        {
            Ynaught_csvCustomAdditions = Ynaught_CustomAdditions_TextBox.Text;
        }

        private void Ynaught_CustomSubtractions_TextBox_TextChanged(object sender, EventArgs e)
        {
            Ynaught_csvCustomSubtractions = Ynaught_CustomSubtractions_TextBox.Text;
        }

        //set up buttons to check all of certain types of Y-ions
        private void CheckAllNglyco_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_NlinkedCheckBox);
        }

        private void CheckAllFucose_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_FucoseNlinkedCheckedBox);
        }

        private void CheckAllNeutralLosses_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_LossFromPepChecklistBox);
        }

        private void CheckAllOglyco_Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_OlinkedChecklistBox);
        }

        private void Yions_CheckAllButton_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(Yions_NlinkedCheckBox);
            SelectAllItems_CheckedBox(Yions_FucoseNlinkedCheckedBox);
            SelectAllItems_CheckedBox(Yions_LossFromPepChecklistBox);
            SelectAllItems_CheckedBox(Yions_OlinkedChecklistBox);
        }

        private void Yions_NglycoMannoseButton_Click(object sender, EventArgs e)
        {
            //Common Nglyco
            for (int i = 0; i < Yions_NlinkedCheckBox.Items.Count; i++)
            {
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("Y0"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("203.07"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("406.15"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("568.21"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("730.26"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("892.31"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
            }
            //Glycan losses
            for (int i = 0; i < Yions_LossFromPepChecklistBox.Items.Count; i++)
            {
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("Intact Mass"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("162.05"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("324.10"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("486.15"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("648.21"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("810.26"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("972.31"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
            }

        }

        private void Yions_CheckNglycoSialylButton_Click(object sender, EventArgs e)
        {
            //Common Nglyco
            for (int i = 0; i < Yions_NlinkedCheckBox.Items.Count; i++)
            {
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("Y0"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("203.07"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("406.15"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("568.21"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("730.26"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("892.31"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
            }
            //Glycan losses
            for (int i = 0; i < Yions_LossFromPepChecklistBox.Items.Count; i++)
            {
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("Intact Mass"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("291.09"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("453.14"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("656.22"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("582.19"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("906.29"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("1312.45"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
            }
        }

        private void Yions_CheckNglycoFucoseButton_Click(object sender, EventArgs e)
        {
            //Glycan losses
            for (int i = 0; i < Yions_LossFromPepChecklistBox.Items.Count; i++)
            {
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("Intact Mass"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("802.28"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
                if (Yions_LossFromPepChecklistBox.Items[i].ToString().Contains("511.19"))
                {
                    Yions_LossFromPepChecklistBox.SetItemChecked(i, true);
                }
            }
            //check all fucose
            SelectAllItems_CheckedBox(Yions_FucoseNlinkedCheckedBox);
            //Common Nglyco
            for (int i = 0; i < Yions_NlinkedCheckBox.Items.Count; i++)
            {
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("Y0"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("203.07"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("406.15"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("568.21"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("730.26"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
                if (Yions_NlinkedCheckBox.Items[i].ToString().Contains("892.31"))
                {
                    Yions_NlinkedCheckBox.SetItemChecked(i, true);
                }
            }
        }

        //set up clearing of selections
        private void ClearAllSelections_Button_Click(object sender, EventArgs e)
        {
            while (Yions_NlinkedCheckBox.CheckedIndices.Count > 0)
                Yions_NlinkedCheckBox.SetItemChecked(Yions_NlinkedCheckBox.CheckedIndices[0], false);

            while (Yions_FucoseNlinkedCheckedBox.CheckedIndices.Count > 0)
                Yions_FucoseNlinkedCheckedBox.SetItemChecked(Yions_FucoseNlinkedCheckedBox.CheckedIndices[0], false);

            while (Yions_LossFromPepChecklistBox.CheckedIndices.Count > 0)
                Yions_LossFromPepChecklistBox.SetItemChecked(Yions_LossFromPepChecklistBox.CheckedIndices[0], false);

            while (Yions_OlinkedChecklistBox.CheckedIndices.Count > 0)
                Yions_OlinkedChecklistBox.SetItemChecked(Yions_OlinkedChecklistBox.CheckedIndices[0], false);

            Yions_NlinkedCheckBox.ClearSelected();
            Yions_OlinkedChecklistBox.ClearSelected();
            Yions_FucoseNlinkedCheckedBox.ClearSelected();
            Yions_LossFromPepChecklistBox.ClearSelected();
        }

        private void Yions_NlinkedCheckBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Yions_NlinkedCheckBox.ClearSelected();
        }

        //start the Y-ion processing
        private void Ynaught_StartButton_Click(object sender, EventArgs e)
        {
            bool Ynaught_usingda = false;

            timer2.Interval = 1000;
            timer2.Tick += new EventHandler(OnTimerTick);
            timer2.Start();
            Ynaught_startTimeLabel.Text = "Start Time: " + DateTime.Now.ToString("HH:mm:ss");
            Ynaught_startTimeLabel.Refresh();

            //clear out Y-ions
            foreach (Yion yIon in yIonHashSet)
            {
                yIon.intensity = 0;
                yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                yIon.hcdCount = 0;
                yIon.etdCount = 0;
                yIon.theoMass = 0;
                yIon.description = "";
                yIon.glycanSource = "";
            }

            //either take in custom values or use defaults
            if (Ynaught_DaCheckBox.Checked)
            {
                if (CanConvertDouble(Ynaught_ppmTolTextBox.Text, daTolerance))
                    Ynaught_daTolerance = Convert.ToDouble(Ynaught_ppmTolTextBox.Text);
                Ynaught_usingda = true;
            }
            else
            {
                if (CanConvertDouble(Ynaught_ppmTolTextBox.Text, ppmTolerance))
                    Ynaught_ppmTolerance = Convert.ToDouble(Ynaught_ppmTolTextBox.Text);
            }

            if (Ynaught_usingda)
                Ynaught_tol = daTolerance;
            else
                Ynaught_tol = ppmTolerance;

            if (CanConvertDouble(Ynaught_SNthresholdTextBox.Text, SNthreshold))
                Ynaught_SNthreshold = Convert.ToDouble(Ynaught_SNthresholdTextBox.Text);

            if (CanConvertInt(Ynaught_HigherChargeStateTextBox_X.Text, Ynaught_chargeStateMod_X))
                Ynaught_chargeStateMod_X = Convert.ToInt32(Ynaught_HigherChargeStateTextBox_X.Text);

            if (CanConvertInt(Ynaught_LowerChargeStateTextBox_Y.Text, Ynaught_chargeStateMod_Y))
                Ynaught_chargeStateMod_Y = Convert.ToInt32(Ynaught_LowerChargeStateTextBox_Y.Text);

            //add checked items to yIonHashSet to use for creating ions to look for
            //note that subtraction is its own "Source"
            foreach (var item in Yions_NlinkedCheckBox.CheckedItems)
            {
                string[] yIonArray = item.ToString().Split(',');
                Yion yIon = new Yion();
                yIon.theoMass = Convert.ToDouble(yIonArray[0]);
                yIon.description = item.ToString();
                yIon.glycanSource = "Nglycan";
                yIon.hcdCount = 0;
                yIon.etdCount = 0;
                yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                if (item.ToString().Contains("Y0"))
                {
                    bool hashSetContainsY0 = false;
                    foreach (Yion entry in yIonHashSet)
                    {
                        if (entry.description.Contains("Y0"))
                        {
                            hashSetContainsY0 = true;
                        }
                    }
                    if (!hashSetContainsY0)
                        yIonHashSet.Add(yIon);
                }
                else
                {
                    yIonHashSet.Add(yIon);
                }

            }

            foreach (var item in Yions_FucoseNlinkedCheckedBox.CheckedItems)
            {
                string[] yIonArray = item.ToString().Split(',');
                Yion yIon = new Yion();
                yIon.theoMass = Convert.ToDouble(yIonArray[0]);
                yIon.description = item.ToString();
                yIon.glycanSource = "Nglycan_Fucose";
                yIon.hcdCount = 0;
                yIon.etdCount = 0;
                yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                if (item.ToString().Contains("Y0"))
                {
                    bool hashSetContainsY0 = false;
                    foreach (Yion entry in yIonHashSet)
                    {
                        if (entry.description.Contains("Y0"))
                        {
                            hashSetContainsY0 = true;
                        }
                    }
                    if (!hashSetContainsY0)
                        yIonHashSet.Add(yIon);
                }
                else
                {
                    yIonHashSet.Add(yIon);
                }
            }

            foreach (var item in Yions_OlinkedChecklistBox.CheckedItems)
            {
                string[] yIonArray = item.ToString().Split(',');
                Yion yIon = new Yion();
                yIon.theoMass = Convert.ToDouble(yIonArray[0]);
                yIon.description = item.ToString();
                yIon.glycanSource = "Oglycan";
                yIon.hcdCount = 0;
                yIon.etdCount = 0;
                yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                if (item.ToString().Contains("Y0"))
                {
                    bool hashSetContainsY0 = false;
                    foreach (Yion entry in yIonHashSet)
                    {
                        if (entry.description.Contains("Y0"))
                        {
                            hashSetContainsY0 = true;
                        }
                    }
                    if (!hashSetContainsY0)
                        yIonHashSet.Add(yIon);
                }
                else
                {
                    yIonHashSet.Add(yIon);
                }
            }

            foreach (var item in Yions_LossFromPepChecklistBox.CheckedItems)
            {
                string[] yIonArray = item.ToString().Split(',');
                Yion yIon = new Yion();
                yIon.theoMass = Convert.ToDouble(yIonArray[1].Substring(1));
                yIon.description = item.ToString();
                yIon.glycanSource = "Subtraction";
                yIon.hcdCount = 0;
                yIon.etdCount = 0;
                yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                yIonHashSet.Add(yIon);
            }

            //process the custom additions and add to HashSet
            //this will only execute if the user uploaded a file and changed the text from being empty
            if (!Ynaught_csvCustomAdditions.Equals("empty"))
            {
                StreamReader csvFile = new StreamReader(Ynaught_csvCustomAdditions);
                using (var csv = new CsvReader(csvFile, true))
                {
                    while (csv.ReadNextRecord())
                    {
                        Yion yIon = new Yion();
                        yIon.theoMass = double.Parse(csv["Mass"]);
                        string userDescription = csv["Description"];
                        yIon.description = double.Parse(csv["Mass"]) + ", " + userDescription;
                        yIon.glycanSource = "CustomAddition";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
                        yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                        yIonHashSet.Add(yIon);
                    }
                }
            }

            //process the custom subtractions and add to HashSet
            //this will only execute if the user uploaded a file and changed the text from being empty
            if (!Ynaught_csvCustomSubtractions.Equals("empty"))
            {
                StreamReader csvFile = new StreamReader(Ynaught_csvCustomAdditions);
                using (var csv = new CsvReader(csvFile, true))
                {
                    while (csv.ReadNextRecord())
                    {
                        Yion yIon = new Yion();
                        yIon.theoMass = double.Parse(csv["Mass"]);
                        string userDescription = csv["Description"];
                        yIon.description = double.Parse(csv["Mass"]) + ", " + userDescription;
                        yIon.glycanSource = "CustomSubtraction";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
                        yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                        yIonHashSet.Add(yIon);
                    }
                }
            }

            //create dictionary for glycan masses and populate from the user uploaded file
            Dictionary<double, string> glycanMassDictionary = new Dictionary<double, string>();
            StreamReader glycanMassesTxtFile = new StreamReader(Ynaught_glycanMassesFilePath);
            using (var txt = new CsvReader(glycanMassesTxtFile, true, '\t'))
            {
                while (txt.ReadNextRecord())
                {
                    string glycanName = txt["Glycan"];
                    double glycanMass = double.Parse(txt["Mass"]);
                    if (!glycanMassDictionary.ContainsKey(glycanMass))
                        glycanMassDictionary.Add(glycanMass, glycanName);
                }
            }

            //set the rawfile path and open it
            ThermoRawFile rawFile = new ThermoRawFile(Ynaught_rawFilePath);
            rawFile.Open();

            //update the timer
            Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
            Ynaught_FinishTimeLabel.Refresh();

            //define variables for scan counting
            int numberOfMS2scansWithYions = 0;
            int numberOfMS2scansWithY0 = 0;
            int numberOfMS2scansWithIntactGlycoPep = 0;
            int numberOfMS2scansWithYions_hcd = 0;
            int numberOfMS2scansWithY0_hcd = 0;
            int numberOfMS2scansWithIntactGlycoPep_hcd = 0;
            int numberOfMS2scansWithYions_etd = 0;
            int numberOfMS2scansWithY0_etd = 0;
            int numberOfMS2scansWithIntactGlycoPep_etd = 0;
            int numberOfMS2scans = 0;
            int numberOfHCDscans = 0;
            int numberOfETDscans = 0;
            bool firstSpectrumInFile = true;

            //set up each output stream
            StreamWriter outputYion = new StreamWriter(Ynaught_rawFilePath + "_GlyCounter_YionSignal.txt");
            StreamWriter outputPeakDepth = new StreamWriter(Ynaught_rawFilePath + "_GlyCounter_YionPeakDepth.txt");
            StreamWriter outputSummary = new StreamWriter(Ynaught_rawFilePath + "_GlyCounter_YionSummary.txt");

            //print first lines of each output
            outputYion.Write("ScanNumber\tPeptideNoGlycan\tPeptideWithGlycan\tTotalGlycanComposition\tPrecursorMZ\tChargeState\tRetentionTime\t#ChargeStatesConsidered\tScanInjTime\tDissociationType\tParentScan\tNumYions\tScanTIC\tTotalYionSignal\tYionTICfraction\t");
            outputPeakDepth.Write("ScanNumber\tPeptideNoGlycan\tPeptideWithGlycan\tTotalGlycanComposition\tPrecursorMZ\tChargeState\tRetentionTime\t#ChargeStatesConsidered\tScanInjTime\tDissociationType\tParentScan\tNumYions\tScanTIC\tTotalYionSignal\tYionTICfraction\t");
            /*
            outputSummary.WriteLine("Settings\tppmTol:\t" + ppmTolerance + "\tSNthreshold:\t" + SNthreshold + "\tHCDPeakDepthThreshold:\t" + peakDepthThreshold_hcd
                + "\tETDPeakDepthThreshold:\t" + peakDepthThreshold_etd + "\tHCD TIC fraction:\t" + oxoTICfractionThreshold_hcd + "\tETD TIC fraction:\t" + oxoTICfractionThreshold_etd);
            */

            string toleranceString = "ppmTol= ";
            if (Ynaught_usingda)
                toleranceString = "daTol= ";

            outputSummary.WriteLine("Settings:\t" + toleranceString + Ynaught_tol + ", SNthreshold= " + Ynaught_SNthreshold
                + ", X setting for z-X=" + Ynaught_chargeStateMod_X + ", Y setting for z-Y=" + Ynaught_chargeStateMod_Y + ", First isotope checked: "
                + FirstIsotopeCheckBox.Checked + ", Second isotope checked: " + SecondIsotopeCheckBox.Checked);
            outputSummary.WriteLine(VersionNumber_Label.Text + ", " + Ynaught_startTimeLabel.Text);
            outputSummary.WriteLine();

            //create PSM list to add each entry to
            List<PSM> psmList = new List<PSM>();
            psmList.Clear();

            //this is currently set up for MSFragger data form the psms file
            //we might want to write a converter for different data types
            StreamReader pepIDtxtFile = new StreamReader(Ynaught_pepIDFilePath);
            using (var txt = new CsvReader(pepIDtxtFile, true, '\t'))
            {
                while (txt.ReadNextRecord())
                {
                    //create a new PSM object
                    PSM psm = new PSM();
                    psm.modificationDictionary = new Dictionary<int, double>();

                    //read in peptide sequence and create peptide objects
                    string peptideSeq = txt["Peptide"];
                    Peptide peptide = new Peptide(peptideSeq); //create new peptide object with this PSM's ID'ed sequence that will have all mods (useful for subtraction)
                    Peptide peptideNoGlycanMods = new Peptide(peptideSeq); //create new peptide object with this PSM's ID'ed sequence but will not have glycan attached (useful for addition)

                    //add these items to the PSM object
                    psm.peptide = peptide;
                    psm.peptideNoGlycanMods = peptideNoGlycanMods;

                    //read in other details
                    string spectrumToBeParsed = txt["Spectrum"];
                    string modsToBeParsed = txt["Assigned Modifications"];
                    int charge = int.Parse(txt["Charge"]);
                    string totalGlycanCompToBeParsed = txt["Total Glycan Composition"];
                    double precursorMZ = double.Parse(txt["Observed M/Z"]);

                    //only process if it's a glycopeptide
                    if (!totalGlycanCompToBeParsed.Equals(""))
                    {
                        //read in modifications and assign to peptide
                        if (modsToBeParsed.Length > 0)
                        {
                            string[] modsArray1 = modsToBeParsed.Split(',');
                            for (int i = 0; i < modsArray1.Length; i++)
                            {
                                string mod = modsArray1[i].Replace(" ", "");
                                //this is for the first entry in the line, which has no extra space
                                string[] modsArray2 = mod.Split('(');

                                //get the mass of the mod
                                string[] modsArray3 = modsArray2[1].Split(')');
                                double modMass = Convert.ToDouble(modsArray3[0]);

                                Modification modToAdd = new Modification(modMass, modsArray3[0]);
                                int modPosition = 0;
                                if (modsArray2[0].Equals("N-term"))
                                {
                                    peptide.AddModification(modToAdd, Terminus.N);
                                }
                                else
                                {
                                    //get the residue of the mod
                                    string modResidue = modsArray2[0].Substring(modsArray2[0].Length - 1);

                                    //get postion of the mod
                                    modPosition = Convert.ToInt32(modsArray2[0].Substring(0, modsArray2[0].Length - 1));

                                    if (modPosition > 0)
                                    {
                                        //add the modification to the peptide only if it is not a glycan mass
                                        if (!glycanMassDictionary.ContainsKey(modMass))
                                        {
                                            peptide.AddModification(modToAdd, modPosition);
                                        }
                                        //add this to the PSM object dictionary that keeps track of all mods
                                        psm.modificationDictionary.Add(modPosition, modMass);
                                    }
                                }
                            }
                        }

                        //set spectrum number
                        string[] spectrumArray = spectrumToBeParsed.Split('.');
                        int spectrumNum = Convert.ToInt32(spectrumArray[1]);

                        //add the rest of the information to the PSM object
                        psm.charge = charge;
                        psm.spectrumNumber = spectrumNum;
                        psm.totalGlycanComposition = totalGlycanCompToBeParsed;
                        psm.precursorMZ = precursorMZ;

                        //add PSM to list
                        psmList.Add(psm);
                    }

                }
            }


            foreach (PSM psm in psmList)
            {
                if (rawFile.GetMsnOrder(psm.spectrumNumber) == 2 && !rawFile.GetMzAnalyzer(psm.spectrumNumber).ToString().Contains("IonTrap") && rawFile.GetTIC(psm.spectrumNumber) > 0)
                {
                    int numberOfYions = 0;
                    double totalYionSignal = 0;
                    int numberOfChargeStatesConsidered = 1;

                    bool hcdTrue = false;
                    bool etdTrue = false;

                    bool Y0_found = false;
                    bool intactGlycoPep_found = false;

                    numberOfMS2scans++;
                    if (rawFile.GetDissociationType(psm.spectrumNumber).ToString().Equals("HCD"))
                    {
                        numberOfHCDscans++;
                        hcdTrue = true;
                    }
                    if (rawFile.GetDissociationType(psm.spectrumNumber).ToString().Equals("ETD"))
                    {
                        numberOfETDscans++;
                        etdTrue = true;
                    }

                    string yIonHeader = "";

                    ThermoSpectrum spectrum = rawFile.GetLabeledSpectrum(psm.spectrumNumber);

                    Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                    RankOrderPeaks(sortedPeakDepths, spectrum);

                    List<ThermoMzPeak> yIonFoundPeaks = new List<ThermoMzPeak>();

                    //set up peptide and glycopeptide masses to look for
                    double peptideNoGlycan_MonoMass = psm.peptideNoGlycanMods.MonoisotopicMass;
                    double peptideNoGlycan_firstIsoMass = psm.peptideNoGlycanMods.MonoisotopicMass + (1 * Constants.C13C12Difference);
                    double peptideNoGlycan_secondIsoMass = psm.peptideNoGlycanMods.MonoisotopicMass + (2 * Constants.C13C12Difference);

                    double glycopeptide_MonoMass = psm.peptide.MonoisotopicMass;
                    double glycopeptide_firstIsoMass = psm.peptide.MonoisotopicMass + (1 * Constants.C13C12Difference);
                    double glycopeptide_secondIsoMass = psm.peptide.MonoisotopicMass + (2 * Constants.C13C12Difference);

                    //look for each Y-ion
                    foreach (Yion yIon in yIonHashSet)
                    {
                        //just to be safe, set all variable specific to this spectrum to zero before going to find them
                        //this is in case these didn't get cleared from the previous processing
                        yIon.intensity = 0;
                        yIon.peakDepth = arbitraryPeakDepthIfNotFound;
                        yIonHeader = yIonHeader + yIon.description + "\t";

                        bool countYion = false;


                        //use the user input to deteremine what charge states to look for. Set the minimum charge state to 1
                        int chargeUpperBound = psm.charge - Ynaught_chargeStateMod_X;
                        int chargeLowerBound = psm.charge - Ynaught_chargeStateMod_Y;
                        if (chargeLowerBound < 1)
                            chargeLowerBound = 1;

                        //how many charge states are we looking for?
                        numberOfChargeStatesConsidered = chargeUpperBound - chargeLowerBound + 1;

                        //find all the Yions for each charge state considered
                        for (int i = chargeUpperBound; i >= chargeLowerBound; i--)
                        {
                            //this is for eveything where we add glycan mass to the peptide itself
                            if (!yIon.glycanSource.Contains("Subtraction"))
                            {
                                double yIon_mz = (peptideNoGlycan_MonoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                ThermoMzPeak peak = GetPeak(spectrum, yIon_mz, Ynaught_usingda, Ynaught_tol);

                                if (peak != null && peak.Intensity > 0 && peak.SignalToNoise > SNthreshold)
                                {
                                    countYion = true; //this is to know if we can count the Y-ion as being found for keeping track of scans
                                    if (yIon.description.Contains("Y0"))
                                        Y0_found = true;

                                    double firstIsotopeIntensity = 0;
                                    double secondIsotopeIntensity = 0;
                                    if (FirstIsotopeCheckBox.Checked)
                                    {
                                        double yIon_mzfirstIso = (peptideNoGlycan_firstIsoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                        ThermoMzPeak firstIsotopePeak = GetPeak(spectrum, yIon_mzfirstIso, Ynaught_usingda, Ynaught_tol);
                                        if (firstIsotopePeak != null && firstIsotopePeak.Intensity > 0 && firstIsotopePeak.SignalToNoise > SNthreshold)
                                            firstIsotopeIntensity = firstIsotopePeak.Intensity;
                                    }
                                    if (SecondIsotopeCheckBox.Checked)
                                    {
                                        double yIon_mzSecondIso = (peptideNoGlycan_secondIsoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                        ThermoMzPeak secondIsotopePeak = GetPeak(spectrum, yIon_mzSecondIso, Ynaught_usingda, Ynaught_tol);
                                        if (secondIsotopePeak != null && secondIsotopePeak.Intensity > 0 && secondIsotopePeak.SignalToNoise > SNthreshold)
                                            secondIsotopeIntensity = secondIsotopePeak.Intensity;
                                    }

                                    yIon.intensity = peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;
                                    yIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                    numberOfYions++;
                                    totalYionSignal = totalYionSignal + peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;


                                }

                            }
                            //this is for glycan neutral losses from the intact glycopeptide
                            else
                            {
                                double yIon_mz = (glycopeptide_MonoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                ThermoMzPeak peak = GetPeak(spectrum, yIon_mz, Ynaught_usingda, Ynaught_tol);

                                if (peak != null && peak.Intensity > 0 && peak.SignalToNoise > SNthreshold)
                                {
                                    countYion = true; //this is to know if we can count the Y-ion as being found for keeping track of scans
                                    if (yIon.description.Contains("Intact Mass"))
                                        intactGlycoPep_found = true;

                                    double firstIsotopeIntensity = 0;
                                    double secondIsotopeIntensity = 0;
                                    if (FirstIsotopeCheckBox.Checked)
                                    {
                                        double yIon_mzfirstIso = (glycopeptide_firstIsoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                        ThermoMzPeak firstIsotopePeak = GetPeak(spectrum, yIon_mzfirstIso, Ynaught_usingda, Ynaught_tol);
                                        if (firstIsotopePeak != null && firstIsotopePeak.Intensity > 0 && firstIsotopePeak.SignalToNoise > SNthreshold)
                                            firstIsotopeIntensity = firstIsotopePeak.Intensity;
                                    }
                                    if (SecondIsotopeCheckBox.Checked)
                                    {
                                        double yIon_mzSecondIso = (glycopeptide_secondIsoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                        ThermoMzPeak secondIsotopePeak = GetPeak(spectrum, yIon_mzSecondIso, Ynaught_usingda, Ynaught_tol);
                                        if (secondIsotopePeak != null && secondIsotopePeak.Intensity > 0 && secondIsotopePeak.SignalToNoise > SNthreshold)
                                            secondIsotopeIntensity = secondIsotopePeak.Intensity;
                                    }

                                    yIon.intensity = peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;
                                    yIon.peakDepth = sortedPeakDepths[peak.Intensity];
                                    numberOfYions++;
                                    totalYionSignal = totalYionSignal + peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;


                                }
                            }
                        }

                        if (countYion)
                            numberOfYions++;
                        if (hcdTrue && countYion)
                            yIon.hcdCount++;
                        if (etdTrue && countYion)
                            yIon.etdCount++;
                    }

                    //update counts
                    if (Y0_found)
                    {
                        numberOfMS2scansWithY0++;
                        if (hcdTrue)
                            numberOfMS2scansWithY0_hcd++;
                        if (etdTrue)
                            numberOfMS2scansWithY0_etd++;
                    }
                    if (intactGlycoPep_found)
                    {
                        numberOfMS2scansWithIntactGlycoPep++;
                        if (hcdTrue)
                            numberOfMS2scansWithIntactGlycoPep_hcd++;
                        if (etdTrue)
                            numberOfMS2scansWithIntactGlycoPep_etd++;
                    }


                    //print out the headers for each Y-ion searched for, with the last column being a ratio of total TIC we will calculate
                    if (firstSpectrumInFile)
                    {
                        outputYion.WriteLine(yIonHeader);
                        outputPeakDepth.WriteLine(yIonHeader);
                        firstSpectrumInFile = false;
                    }

                    //get some spectral features
                    double parentScan = 0;
                    try
                    {
                        parentScan = rawFile.GetParentSpectrumNumber(psm.spectrumNumber);
                    }
                    catch (Exception ex)
                    {

                    }
                    double scanTIC = rawFile.GetTIC(psm.spectrumNumber);
                    double scanInjTime = rawFile.GetInjectionTime(psm.spectrumNumber);
                    string fragmenationType = rawFile.GetDissociationType(psm.spectrumNumber).ToString();
                    //double parentScan = rawFile.GetParentSpectrumNumber(psm.spectrumNumber);
                    double retentionTime = rawFile.GetRetentionTime(psm.spectrumNumber);

                    //used this for peak depth calculations in GlyCounter, don't actually use here
                    List<double> yIonRanks = new List<double>();

                    //calculate fraction of TIC
                    double yIonTICfraction = totalYionSignal / scanTIC;


                    //print out information for this scan that is not Y-ions
                    outputYion.Write(psm.spectrumNumber + "\t" + psm.peptideNoGlycanMods.SequenceWithModifications + "\t" + psm.peptide.SequenceWithModifications + "\t" +
                        psm.totalGlycanComposition + "\t" + psm.precursorMZ + "\t" + psm.charge + "\t" + retentionTime + "\t" + numberOfChargeStatesConsidered + "\t" +
                        scanInjTime + "\t" + fragmenationType + "\t" + parentScan + "\t" + numberOfYions + "\t" + scanTIC + "\t" + totalYionSignal + "\t" + yIonTICfraction + "\t");
                    outputPeakDepth.Write(psm.spectrumNumber + "\t" + psm.peptideNoGlycanMods.SequenceWithModifications + "\t" + psm.peptide.SequenceWithModifications + "\t" +
                        psm.totalGlycanComposition + "\t" + psm.precursorMZ + "\t" + psm.charge + "\t" + retentionTime + "\t" + numberOfChargeStatesConsidered + "\t" +
                        scanInjTime + "\t" + fragmenationType + "\t" + parentScan + "\t" + numberOfYions + "\t" + scanTIC + "\t" + totalYionSignal + "\t" + yIonTICfraction + "\t");

                    //write out peak depth and intensity info for each found Y-ion
                    foreach (Yion yIon in yIonHashSet)
                    {
                        outputYion.Write(yIon.intensity + "\t");

                        if (yIon.peakDepth == arbitraryPeakDepthIfNotFound)
                        {
                            outputPeakDepth.Write("NotFound\t");

                        }
                        else
                        {
                            outputPeakDepth.Write(yIon.peakDepth + "\t");
                            yIonRanks.Add(yIon.peakDepth); //we currently don't actually use this
                        }

                    }

                    if (numberOfYions > 0)
                    {
                        numberOfMS2scansWithYions++;
                        if (hcdTrue)
                            numberOfMS2scansWithYions_hcd++;
                        if (etdTrue)
                            numberOfMS2scansWithYions_etd++;
                    }

                    outputYion.WriteLine();
                    outputPeakDepth.WriteLine();

                }

                Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                Ynaught_FinishTimeLabel.Refresh();
            }

            double percentageYions = (double)numberOfMS2scansWithYions / (double)numberOfMS2scans * 100;
            double percentageYions_hcd = (double)numberOfMS2scansWithYions_hcd / (double)numberOfHCDscans * 100;
            double percentageYions_etd = (double)numberOfMS2scansWithYions_etd / (double)numberOfETDscans * 100;

            double percentageY0 = (double)numberOfMS2scansWithY0 / (double)numberOfMS2scans * 100;
            double percentageY0_hcd = (double)numberOfMS2scansWithY0_hcd / (double)numberOfHCDscans * 100;
            double percentageY0_etd = (double)numberOfMS2scansWithY0_etd / (double)numberOfETDscans * 100;

            double percentageGlycoPep = (double)numberOfMS2scansWithIntactGlycoPep / (double)numberOfMS2scans * 100;
            double percentageGlycoPep_hcd = (double)numberOfMS2scansWithIntactGlycoPep_hcd / (double)numberOfHCDscans * 100;
            double percentageGlycoPep_etd = (double)numberOfMS2scansWithIntactGlycoPep_etd / (double)numberOfETDscans * 100;

            outputSummary.WriteLine("\tTotal\tHCD\tETD\t%Total\t%HCD\t%ETD");
            outputSummary.WriteLine("All GlycoPSMs\t" + numberOfMS2scans + "\t" + numberOfHCDscans + "\t" + numberOfETDscans + "\tNA\tNA\tNA");

            outputSummary.WriteLine("GlycoPSMs with Y-ions\t" + numberOfMS2scansWithYions + "\t" + numberOfMS2scansWithYions_hcd + "\t" + numberOfMS2scansWithYions_etd
                + "\t" + percentageYions + "\t" + percentageYions_hcd + "\t" + percentageYions_etd);

            outputSummary.WriteLine("GlycoPSMs with Y0\t" + numberOfMS2scansWithY0 + "\t" + numberOfMS2scansWithY0_hcd + "\t" + numberOfMS2scansWithY0_etd
                + "\t" + percentageY0 + "\t" + percentageY0_hcd + "\t" + percentageY0_etd);
            outputSummary.WriteLine("GlycoPSMs with IntactGlycoPep\t" + numberOfMS2scansWithIntactGlycoPep + "\t" + numberOfMS2scansWithIntactGlycoPep_hcd + "\t" + numberOfMS2scansWithIntactGlycoPep_etd
                + "\t" + percentageGlycoPep + "\t" + percentageGlycoPep_hcd + "\t" + percentageGlycoPep_etd);


            outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
            outputSummary.WriteLine("\tTotal\tHCD\tETD\t%Total\t%HCD\t%ETD");

            string currentGlycanSource = "first";
            foreach (Yion yIon in yIonHashSet)
            {
                int total = yIon.hcdCount + yIon.etdCount;

                double percentTotal = (double)total / (double)numberOfMS2scans * 100;
                double percentHCD = (double)yIon.hcdCount / (double)numberOfHCDscans * 100;
                double percentETD = (double)yIon.etdCount / (double)numberOfETDscans * 100;

                if (!currentGlycanSource.Equals(yIon.glycanSource) && !yIon.glycanSource.Equals(""))
                {
                    outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\ " + yIon.glycanSource + @" \\\\\\\\\\\\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                    currentGlycanSource = yIon.glycanSource;
                }

                if (!yIon.glycanSource.Equals(""))
                {
                    outputSummary.WriteLine(yIon.description + "\t" + total + "\t" + yIon.hcdCount + "\t" + yIon.etdCount
                                        + "\t" + percentTotal + "\t" + percentHCD + "\t" + percentETD);
                }

            }

            outputSummary.Close();
            outputYion.Close();
            outputPeakDepth.Close();
            rawFile.Dispose();

            timer1.Stop();
            Ynaught_FinishTimeLabel.Text = "Finished at: " + DateTime.Now.ToString("HH:mm:ss");
            MessageBox.Show("Finished.");
            yIonHashSet.Clear();
        }

        private void ppmTol_label_Click(object sender, EventArgs e)
        {

        }

        private void DaltonCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Ynaught_DaCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
