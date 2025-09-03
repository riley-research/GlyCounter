using CSMSL;
using CSMSL.Proteomics;
using LumenWorks.Framework.IO.Csv;
using MathNet.Numerics.Statistics;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Text;
using Nova.Data;
using Nova.Io.Read;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.CodeDom;
using System.Globalization;


namespace GlyCounter
{
    public partial class Form1 : Form
    {
        //here we build variables
        string outputPath = "";
        List<string> fileList = [];
        HashSet<OxoniumIon> oxoniumIonHashSet = new HashSet<OxoniumIon>();
        string filePath = "";
        string defaultOutput = @"C:\";
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
        double Ynaught_tol = 0;
        double Ynaught_SNthreshold = 3;
        double Ynaught_intensityThreshold = 1000;
        double Ynaught_ppmTolerance;
        double Ynaught_daTolerance;
        string Ynaught_chargeLB = "1";
        string Ynaught_chargeUB = "P";
        string Ynaught_csvCustomAdditions = "empty";
        string Ynaught_csvCustomSubtractions = "empty";
        bool Ynaught_condenseChargeStates = true;
        bool Ynaught_ipsa;

        private Color normalBackColor = Color.White;
        private Color alternateBackColor = Color.Lavender;

        // For application updates
        private UpdateManager _updateManager;

        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);

            // Initialize the update manager
            _updateManager = UpdateManager.Instance;

            // Set window title to include version
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "?.?.?";
            this.Text = $"GlyCounter v{versionString}";
            // Check for updates on startup (silently)
            Task.Run(() => CheckForUpdatesAsync(true));
        }

        private async void CheckForUpdatesAsync(bool silent = false)
        {
            try
            {
                // Call the method in your GlyCounter.UpdateManager class
                await _updateManager.CheckForUpdatesAsync(silent);
            }
            catch (Exception ex)
            {
                // Optional: Log or show error if the check itself fails unexpectedly
                Debug.WriteLine($"Error during update check process: {ex}");
                if (!silent)
                {
                    MessageBox.Show($"Failed to initiate update check: {ex.Message}",
                                    "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            fileList = [];
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Multiselect = true;
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

            fdlg.Filter = "RAW and mzML files (*.raw;*.mzML)|*.raw;*.mzML|RAW files (*.raw*)|*.raw*|mzML files (*.mzML)|*.mzML";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = "Successfully uploaded " + fdlg.FileNames.Count() + " file(s)";

                //set the most recent folder to the path of the last file selected
                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileNames.LastOrDefault());
                Properties.Settings1.Default.Save();

                //also set a default output directory to the path of the last file saved
                defaultOutput = Path.GetDirectoryName(fdlg.FileNames.LastOrDefault());
            }

            //add file paths to file list
            foreach (string filePath in fdlg.FileNames)
                fileList.Add(filePath);
        }

        private void Gly_outputButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Output Directory";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
                {
                    dialog.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
                }
                else
                {
                    dialog.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Gly_outputTextBox.Text = dialog.SelectedPath;
                    Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(dialog.SelectedPath);
                    Properties.Settings1.Default.Save();
                }
            }
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(Gly_outputTextBox.Text))
            {
                outputPath = Gly_outputTextBox.Text + @"\";
            }
            else
            {
                outputPath = Gly_outputTextBox.Text + @"\";
                DialogResult result = MessageBox.Show(
                    "Folder does not exist. Do you want to create a new one?",
                    "Create Folder",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.OK)
                {
                    try
                    {
                        Directory.CreateDirectory(Gly_outputTextBox.Text);
                    }
                    catch { }
                }
                else
                {
                    return;
                }

            }

            timer1.Interval = 1000;
            timer1.Tick += new EventHandler(OnTimerTick);
            timer1.Start();
            StatusLabel.Text = "Processing...";
            StartTimeLabel.Text = "Start Time: " + DateTime.Now.ToString("HH:mm:ss");

            try
            {
                await Task.Run(() =>
                {
                    bool usingda = false;
                    using204 = false;

                    if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                    {
                        if (fileList.Count > 0)
                            outputPath = Path.GetDirectoryName(fileList[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        else
                            outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }

                    if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                    {
                        if (fileList.Count > 0)
                        {
                            outputPath = Path.GetDirectoryName(fileList[0]) ?? defaultOutput;
                        }
                        else
                        {
                            outputPath = defaultOutput;
                        }

                        if (Gly_outputTextBox.InvokeRequired)
                        {
                            Gly_outputTextBox.Invoke(new Action(() => Gly_outputTextBox.Text = outputPath));
                        }
                        else
                        {
                            Gly_outputTextBox.Text = outputPath;
                        }
                    }
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    //make sure all user inputs are in the correct format, otherwise use defaults
                    if (DaltonCheckBox.Checked)
                    {
                        if (CanConvertDouble(ppmTol_textBox.Text, daTolerance))
                        {
                            daTolerance = Convert.ToDouble(ppmTol_textBox.Text, CultureInfo.InvariantCulture);
                            usingda = true;
                        }

                    }
                    else
                        if (CanConvertDouble(ppmTol_textBox.Text, ppmTolerance))
                            ppmTolerance = Convert.ToDouble(ppmTol_textBox.Text, CultureInfo.InvariantCulture);

                    if (usingda)
                        tol = daTolerance;
                    else
                        tol = ppmTolerance;

                    if (CanConvertDouble(SN_textBox.Text, SNthreshold))
                        SNthreshold = Convert.ToDouble(SN_textBox.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(PeakDepth_Box_HCD.Text, peakDepthThreshold_hcd))
                        peakDepthThreshold_hcd = Convert.ToDouble(PeakDepth_Box_HCD.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(PeakDepth_Box_ETD.Text, peakDepthThreshold_etd))
                        peakDepthThreshold_etd = Convert.ToDouble(PeakDepth_Box_ETD.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(PeakDepth_Box_UVPD.Text, peakDepthThreshold_uvpd))
                        peakDepthThreshold_uvpd = Convert.ToDouble(PeakDepth_Box_UVPD.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(hcdTICfraction.Text, oxoTICfractionThreshold_hcd))
                        oxoTICfractionThreshold_hcd = Convert.ToDouble(hcdTICfraction.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(etdTICfraction.Text, oxoTICfractionThreshold_etd))
                        oxoTICfractionThreshold_etd = Convert.ToDouble(etdTICfraction.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(uvpdTICfraction.Text, oxoTICfractionThreshold_uvpd))
                        oxoTICfractionThreshold_uvpd = Convert.ToDouble(uvpdTICfraction.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(OxoCountRequireBox_hcd.Text, oxoCountRequirement_hcd_user))
                        oxoCountRequirement_hcd_user = Convert.ToDouble(OxoCountRequireBox_hcd.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(OxoCountRequireBox_etd.Text, oxoCountRequirement_etd_user))
                        oxoCountRequirement_etd_user = Convert.ToDouble(OxoCountRequireBox_etd.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(OxoCountRequireBox_uvpd.Text, oxoCountRequirement_uvpd_user))
                        oxoCountRequirement_uvpd_user = Convert.ToDouble(OxoCountRequireBox_uvpd.Text, CultureInfo.InvariantCulture);

                    if (CanConvertDouble(intensityThresholdTextBox.Text, intensityThreshold))
                        intensityThreshold = Convert.ToDouble(intensityThresholdTextBox.Text, CultureInfo.InvariantCulture);

                    string toleranceString = "ppmTol: ";
                    if (usingda)
                        toleranceString = "DaTol: ";

                    //popup with settings to user
                    MessageBox.Show("You are using these settings:\r\n" + toleranceString + tol + "\r\nSNthreshold: " + SNthreshold + "\r\nIntensityTheshold: " + intensityThreshold
                        + "\r\nPeakDepthThreshold_HCD: " + peakDepthThreshold_hcd + "\r\nPeakDepthThreshold_ETD: " + peakDepthThreshold_etd + "\r\nPeakDepthThreshold_UVPD: " + peakDepthThreshold_uvpd
                        + "\r\nTICfraction_HCD: " + oxoTICfractionThreshold_hcd + "\r\nTICfraction_ETD: " + oxoTICfractionThreshold_etd + "\r\nTICfraction_UVPD: " + oxoTICfractionThreshold_uvpd);


                    foreach (var item in HexNAcCheckedListBox.CheckedItems)
                        oxoniumIonHashSet.Add(ProcessOxoIon(item, glycanSource: "HexNAc", true));

                    foreach (var item in HexCheckedListBox.CheckedItems)
                        oxoniumIonHashSet.Add(ProcessOxoIon(item, glycanSource: "Hex"));

                    foreach (var item in SialicAcidCheckedListBox.CheckedItems)
                        oxoniumIonHashSet.Add(ProcessOxoIon(item, glycanSource: "Sialic"));

                    foreach (var item in M6PCheckedListBox.CheckedItems)
                        oxoniumIonHashSet.Add(ProcessOxoIon(item, glycanSource: "M6P"));

                    foreach (var item in OligosaccharideCheckedListBox.CheckedItems)
                        oxoniumIonHashSet.Add(ProcessOxoIon(item, glycanSource: "Oligo"));

                    foreach (var item in FucoseCheckedListBox.CheckedItems)
                        oxoniumIonHashSet.Add(ProcessOxoIon(item, glycanSource: "Fucose"));

                    if (!csvCustomFile.Equals("empty"))
                    {
                        using StreamReader csvFile = new StreamReader(csvCustomFile);
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

                    foreach (var fileName in fileList)
                    {
                        //reset oxonium ions
                        foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                        {
                            oxoIon.intensity = 0;
                            oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                            oxoIon.hcdCount = 0;
                            oxoIon.etdCount = 0;
                            oxoIon.uvpdCount = 0;
                            oxoIon.measuredMZ = 0;
                        }

                        FileReader rawFile = new FileReader(fileName);
                        FileReader typeCheck = new FileReader();
                        string fileType = typeCheck.CheckFileFormat(fileName).ToString(); //either "ThermoRaw" or "MzML"
                        bool thermo = true;
                        if (fileType == "MzML")
                            thermo = false;

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

                        //set vars
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
                        double nce = 0.0;
                        double halfTotalList = (double)oxoniumIonHashSet.Count / 2.0;

                        //initialize streamwriter output files
                        string fileNameShort = Path.GetFileNameWithoutExtension(fileName);
                        StreamWriter outputOxo = new StreamWriter(Path.Combine(outputPath + @"\" + fileNameShort + "_GlyCounter_OxoSignal.txt"));
                        StreamWriter outputPeakDepth = new StreamWriter(Path.Combine(outputPath + @"\" + fileNameShort + "_GlyCounter_OxoPeakDepth.txt"));
                        StreamWriter outputIPSA = null;

                        if (ipsaCheckBox.Checked)
                        {
                            outputIPSA = new StreamWriter(Path.Combine(outputPath + @"\" + fileNameShort + "_Glycounter_IPSA.txt"));
                        }
                        StreamWriter outputSummary = new StreamWriter(Path.Combine(outputPath + @"\" + fileNameShort + "_GlyCounter_Summary.txt"));

                        //write headers
                        outputOxo.Write("ScanNumber\tRetentionTime\tMSLevel\tPrecursorMZ\tNCE\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumOxonium\tTotalOxoSignal\t");
                        outputPeakDepth.Write("ScanNumber\tRetentionTime\tPrecursorMZ\tNCE\tScanTIC\tTotalOxoSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumOxonium\tTotalOxoSignal\t");
                        if (outputIPSA != null)
                            outputIPSA.WriteLine("ScanNumber\tOxoniumIons\tMassError\t");
                        outputSummary.WriteLine("Settings:\t" + toleranceString + ", SNthreshold=" + SNthreshold + ", IntensityThreshold=" + intensityThreshold + ", PeakDepthThreshold_HCD=" + peakDepthThreshold_hcd + ", PeakDepthThreshold_ETD=" + peakDepthThreshold_etd + ", PeakDepthThreshold_UVPD=" + peakDepthThreshold_uvpd
                                + ", TICfraction_HCD=" + oxoTICfractionThreshold_hcd + ", TICfraction_ETD=" + oxoTICfractionThreshold_etd + ", TICfraction_UVPD=" + oxoTICfractionThreshold_uvpd);
                        outputSummary.WriteLine(StartTimeLabel.Text);
                        outputSummary.WriteLine();

                        //start processing file
                        for (int i = rawFile.FirstScan; i <= rawFile.LastScan; i++)
                        {
                            SpectrumEx spectrum = rawFile.ReadSpectrumEx(scanNumber: i);
                            bool IT = spectrum.Analyzer.ToString().Contains("ITMS");

                            //custom ms levels
                            List<int> levels = [];

                            if (MSLevelLB.Value == MSLevelUB.Value)
                                levels.Add(Convert.ToInt32(MSLevelLB.Value));

                            int lowestval;
                            int highestval;

                            if (MSLevelLB.Value < MSLevelUB.Value)
                            {
                                lowestval = Convert.ToInt32(MSLevelLB.Value);
                                highestval = Convert.ToInt32(MSLevelUB.Value);

                                levels = Enumerable.Range(lowestval, (highestval - lowestval + 1)).ToList();
                            }

                            //if user puts values in backwards for some reason
                            if (MSLevelLB.Value > MSLevelUB.Value)
                            {
                                lowestval = Convert.ToInt32(MSLevelUB.Value);
                                highestval = Convert.ToInt32(MSLevelLB.Value);

                                levels = Enumerable.Range(lowestval, (highestval - lowestval + 1)).ToList();
                            }

                            //if ignore ms levels is checked ignore levels list
                            if (!ignoreMSLevelCB.Checked)
                                if (!levels.Contains(spectrum.MsLevel)) continue;
                            
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

                            //figure out dissociation type
                            if (thermo)
                            {
                                //using this order means ethcd will count as etd (since both show in scan filter)
                                if (spectrum.ScanFilter.Contains("etd"))
                                {
                                    numberOfETDscans++;
                                    etdTrue = true;
                                }
                                else if (spectrum.ScanFilter.Contains("hcd"))
                                {
                                    numberOfHCDscans++;
                                    hcdTrue = true;
                                }
                                else if (spectrum.ScanFilter.Contains("uvpd") || spectrum.ScanFilter.Contains("ci"))
                                {
                                    numberOfUVPDscans++;
                                    uvpdTrue = true;
                                }
                            }
                            else
                            {
                                string dt = spectrum.Precursors[0].FramentationMethod.ToString();

                                if (dt.Equals("HCD"))
                                {
                                    numberOfHCDscans++;
                                    hcdTrue = true;
                                }
                                if (dt.Equals("ETD"))
                                {
                                    numberOfETDscans++;
                                    etdTrue = true;
                                }
                                if (dt.Equals("CI") || dt.Equals("UVPD"))
                                {
                                    numberOfUVPDscans++;
                                    uvpdTrue = true;
                                }

                            }
                            string oxoIonHeader = "";

                            if (spectrum.TotalIonCurrent > 0 && spectrum.BasePeakIntensity > 0)
                            {
                                Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();
                                RankOrderPeaks(sortedPeakDepths, spectrum);

                                if (thermo)
                                {
                                    string scanFilter = spectrum.ScanFilter;
                                    string[] hcdHeader = scanFilter.Split('@');
                                    string[] splitHCDheader = hcdHeader[1].Split('d');
                                    string[] collisionEnergyArray = splitHCDheader[1].Split('.');
                                    nce = Convert.ToDouble(collisionEnergyArray[0]);
                                }
                                else nce = spectrum.Precursors[0].CollisionEnergy;
                                foreach (OxoniumIon oxoIon in oxoniumIonHashSet)
                                {
                                    oxoIon.intensity = 0;
                                    oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
                                    oxoIonHeader = oxoIonHeader + oxoIon.description + "\t";
                                    oxoIon.measuredMZ = 0;
                                    oxoIon.intensity = 0;

                                    SpecDataPointEx peak = GetPeak(spectrum, oxoIon.theoMZ, usingda, tol, IT);

                                    if (!IT && thermo)
                                    {
                                        if (!peak.Equals(new SpecDataPointEx()) && peak.Intensity > 0 && (peak.Intensity / peak.Noise) > SNthreshold)
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
                                    else
                                    {
                                        if (!peak.Equals(new SpecDataPointEx()) && peak.Intensity > intensityThreshold)
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
                            }

                            if (firstSpectrumInFile)
                            {
                                outputOxo.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
                                outputPeakDepth.WriteLine(oxoIonHeader + "OxoInPeakDepthThresh\tOxoRequired\tOxoTICfraction\tLikelyGlycoSpectrum");
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
                                double parentScan = spectrum.PrecursorMasterScanNumber;
                                double scanTIC = spectrum.TotalIonCurrent;
                                double scanInjTime = spectrum.IonInjectionTime;
                                string fragmentationType = "";
                                if (hcdTrue) fragmentationType = "HCD";
                                if (etdTrue) fragmentationType = "ETD";
                                if (uvpdTrue) fragmentationType = "UVPD";
                                double retentionTime = spectrum.RetentionTime;
                                double precursormz = spectrum.Precursors[0].IsolationMz;
                                List<double> oxoRanks = new List<double>();
                                string peakString = "";
                                foreach (double theoMZ in oxoniumIonFoundPeaks)
                                    peakString = peakString + theoMZ.ToString() + "; ";
                                string errorString = new string("");
                                foreach (double error in oxoniumIonFoundMassErrors)
                                    errorString = errorString + error.ToString("F6") + "; ";

                                //write scan info
                                outputOxo.Write(i + "\t" + retentionTime + "\t" + spectrum.MsLevel + "\t" + precursormz + "\t" + nce + "\t" + scanTIC + "\t" + totalOxoSignal + "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan + "\t" + numberOfOxoIons + "\t" + totalOxoSignal + "\t");
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

                                //double medianRanks = Statistics.Median(oxoRanks);
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
                                    oxoCountRequirement = oxoCountRequirement_hcd_user > 0 ? oxoCountRequirement_hcd_user : halfTotalList;
                                if (etdTrue)
                                    oxoCountRequirement = oxoCountRequirement_etd_user > 0 ? oxoCountRequirement_etd_user : halfTotalList / 2;
                                if (uvpdTrue)
                                    oxoCountRequirement = oxoCountRequirement_uvpd_user > 0 ? oxoCountRequirement_uvpd_user : halfTotalList;

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

                            if (FinishTimeLabel.InvokeRequired)
                            {
                                FinishTimeLabel.Invoke(new Action(() => FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
                            }
                            else
                            {
                                FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                            }
                            
                        }

                        //all scans have been processed, get some total stats
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

                        double percentageHCD = (double)numberOfHCDscans / numberOfMS2scans * 100;
                        double percentageETD = (double)numberOfETDscans / numberOfMS2scans * 100;
                        double percentageUVPD = (double)numberOfUVPDscans / numberOfMS2scans * 100;

                        int numberofMS2scansWithOxo = (int)Math.Round(numberofMS2scansWithOxo_double);
                        int numberofHCDscansWithOxo = (int)Math.Round(numberofHCDscansWithOxo_double);
                        int numberofETDscansWithOxo = (int)Math.Round(numberofETDscansWithOxo_double);
                        int numberofUVPDscansWithOxo = (int)Math.Round(numberofUVPDscansWithOxo_double);

                        numberofMS2scansWithOxo = Math.Max(0, numberofMS2scansWithOxo);
                        numberofHCDscansWithOxo = Math.Max(0, numberofHCDscansWithOxo);
                        numberofETDscansWithOxo = Math.Max(0, numberofETDscansWithOxo);
                        numberofUVPDscansWithOxo = Math.Max(0, numberofUVPDscansWithOxo);

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
                StatusLabel.Text = "Output to: " + outputPath;
                FinishTimeLabel.Text = "Finished at: " + DateTime.Now.ToString("HH:mm:ss");
                MessageBox.Show("Finished.");
                oxoniumIonHashSet.Clear();
            }

        }

        public OxoniumIon ProcessOxoIon(object item, string glycanSource, bool check204 = false)
        {
            string[] oxoniumIonArray = item.ToString().Split(',');
            OxoniumIon oxoIon = new OxoniumIon();
            oxoIon.theoMZ = Convert.ToDouble(oxoniumIonArray[0], CultureInfo.InvariantCulture);
            oxoIon.description = item.ToString();
            oxoIon.glycanSource = glycanSource;
            oxoIon.hcdCount = 0;
            oxoIon.etdCount = 0;
            oxoIon.uvpdCount = 0;
            oxoIon.peakDepth = arbitraryPeakDepthIfNotFound;
            //only need to check for 204 in hexnac ions and custom ions
            if (check204)
                if (Convert.ToDouble(oxoniumIonArray[0], CultureInfo.InvariantCulture) == 204.0867)
                    using204 = true;
            return oxoIon;
        }

        public static SpecDataPointEx GetPeak(SpectrumEx spectrum, double mz, bool usingda, double tolerance, bool thermo, bool IT = false)
        {
            DoubleRange rangeOxonium = usingda
                ? DoubleRange.FromDa(mz, tolerance)
                : DoubleRange.FromPPM(mz, tolerance);

            List<SpecDataPointEx> peaks = spectrum.DataPoints.ToList();

            List<SpecDataPointEx> peakList = peaks.Where(peak => rangeOxonium.Contains(peak.Mz)).ToList();


            if (!IT && thermo)
                peakList = peakList.OrderByDescending(peak => (peak.Intensity / peak.Noise)).ToList();
            else
                peakList = peakList.OrderByDescending(peak => (peak.Intensity)).ToList();

            return peakList.FirstOrDefault();
        }

        public static Dictionary<double, int> RankOrderPeaks(Dictionary<double, int> dictionary, SpectrumEx spectrum)
        {
            var peaks = spectrum.DataPoints;
            List<double> intensities = new List<double>();
            foreach (var peak in peaks)
                intensities.Add(peak.Intensity);

            var sortedpeakIntensities = intensities.OrderByDescending(x => x);

            int i = 1;
            foreach (double value in sortedpeakIntensities)
            {
                if (!dictionary.ContainsKey(value))
                {
                    dictionary.Add(value, i);
                    i++;
                }
            }
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
            fdlg.Filter = "*.txt|*.txt|*.tsv|*.tsv";
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
            fdlg.Filter = "RAW files (*.raw*)|*.raw*|mzML files (*.mzML)|*.mzML";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadInGlycoPepRawFile_TextBox.Text = fdlg.FileName;

                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
                Properties.Settings1.Default.Save();
            }

        }

        //set up output directory
        private void Ynaught_outputButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Output Directory";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
                {
                    dialog.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
                }
                else
                {
                    dialog.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Ynaught_outputTextBox.Text = dialog.SelectedPath;
                    Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(dialog.SelectedPath);
                    Properties.Settings1.Default.Save();
                }
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

                defaultOutput = Path.GetDirectoryName(fdlg.FileName);
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


        //set up charge states
        private void GroupChargeStates_CheckedChanged(object sender, EventArgs e)
        {
            Ynaught_condenseChargeStates = true;
        }

        private void SeparateChargeStates_CheckedChanged(object sender, EventArgs e)
        {
            Ynaught_condenseChargeStates = false;
        }

        private void LowerBoundTextBox_TextChanged(object sender, EventArgs e)
        {
            Ynaught_chargeLB = LowerBoundTextBox.Text; //these are stored as strings until later
        }

        private void UpperBoundTextBox_TextChanged(object sender, EventArgs e)
        {
            Ynaught_chargeUB = UpperBoundTextBox.Text;
        }


        //start the Y-ion processing
        private async void Ynaught_StartButton_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(Ynaught_outputTextBox.Text))
            {
                outputPath = Ynaught_outputTextBox.Text + @"\";
            }
            else
            {
                DialogResult result = MessageBox.Show(
                    "Folder does not exist. Do you want to create a new one?",
                    "Create Folder",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.OK)
                {
                    try
                    {
                        Directory.CreateDirectory(Ynaught_outputTextBox.Text);
                    }
                    catch { }
                }
                else
                {
                    return;
                }

            }

            timer2.Interval = 1000;
            timer2.Tick += new EventHandler(OnTimerTick);
            timer2.Start();
            Ynaught_startTimeLabel.Text = "Start Time: " + DateTime.Now.ToString("HH:mm:ss");

            try
            {
                await Task.Run(() =>
                {
                    bool Ynaught_usingda = false;
                    //make sure output path is real otherwise set to default
                    if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                    {
                        if (!string.IsNullOrEmpty(Ynaught_rawFilePath) && File.Exists(Ynaught_rawFilePath))
                        {
                            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                            {
                                if (fileList.Count > 0)
                                    outputPath = Path.GetDirectoryName(fileList[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                                else
                                    outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                            }

                            outputPath = Path.GetDirectoryName(Ynaught_rawFilePath) ?? defaultOutput;
                            if (Ynaught_outputTextBox.InvokeRequired)
                            {
                                Ynaught_outputTextBox.Invoke(new Action(() => Ynaught_outputTextBox.Text = outputPath));
                            }
                            else
                            {
                                Ynaught_outputTextBox.Text = outputPath;
                            }
                        }
                        else
                        {
                            outputPath = defaultOutput;
                            if (Ynaught_outputTextBox.InvokeRequired)
                            {
                                Ynaught_outputTextBox.Invoke(new Action(() => Ynaught_outputTextBox.Text = outputPath));
                            }
                            else
                            {
                                Ynaught_outputTextBox.Text = outputPath;
                            }
                        }
                    }

                    Stopwatch stopwatch2 = new Stopwatch();
                    stopwatch2.Start();


                    //clear out Y-ions
                    yIonHashSet = new HashSet<Yion>();

                    //either take in custom values or use defaults
                    if (Ynaught_DaCheckBox.Checked)
                    {
                        if (CanConvertDouble(Ynaught_ppmTolTextBox.Text, daTolerance))
                            Ynaught_daTolerance = Convert.ToDouble(Ynaught_ppmTolTextBox.Text, CultureInfo.InvariantCulture);
                        Ynaught_usingda = true;
                    }
                    else
                    {
                        if (CanConvertDouble(Ynaught_ppmTolTextBox.Text, ppmTolerance))
                            Ynaught_ppmTolerance = Convert.ToDouble(Ynaught_ppmTolTextBox.Text, CultureInfo.InvariantCulture);
                    }

                    if (Ynaught_usingda)
                        Ynaught_tol = daTolerance;
                    else
                        Ynaught_tol = ppmTolerance;

                    if (CanConvertDouble(Ynaught_SNthresholdTextBox.Text, SNthreshold))
                        Ynaught_SNthreshold = Convert.ToDouble(Ynaught_SNthresholdTextBox.Text, CultureInfo.InvariantCulture);

                    if (YNaught_IPSAcheckbox.Checked) Ynaught_ipsa = true;

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
                        yIon.theoMass = Convert.ToDouble(yIonArray[0], CultureInfo.InvariantCulture);
                        yIon.description = item.ToString();
                        yIon.glycanSource = "Nglycan_Fucose";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
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
                        yIon.theoMass = Convert.ToDouble(yIonArray[0], CultureInfo.InvariantCulture);
                        yIon.description = item.ToString();
                        yIon.glycanSource = "Oglycan";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
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
                        yIon.theoMass = Convert.ToDouble(yIonArray[1].Substring(1), CultureInfo.InvariantCulture);
                        yIon.description = item.ToString();
                        yIon.glycanSource = "Subtraction";
                        yIon.hcdCount = 0;
                        yIon.etdCount = 0;
                        yIonHashSet.Add(yIon);
                    }

                    //process the custom additions and add to HashSet
                    //this will only execute if the user uploaded a file and changed the text from being empty
                    if (!Ynaught_csvCustomAdditions.Equals("empty"))
                    {
                        using StreamReader csvFile = new StreamReader(Ynaught_csvCustomAdditions);
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
                                yIonHashSet.Add(yIon);
                            }
                        }
                    }

                    //process the custom subtractions and add to HashSet
                    //this will only execute if the user uploaded a file and changed the text from being empty
                    if (!Ynaught_csvCustomSubtractions.Equals("empty"))
                    {
                        using StreamReader csvFile = new StreamReader(Ynaught_csvCustomSubtractions);
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
                                yIonHashSet.Add(yIon);
                            }
                        }
                    }

                    //create dictionary for glycan masses and populate from the user uploaded file
                    Dictionary<double, string> glycanMassDictionary = new Dictionary<double, string>();
                    using StreamReader glycanMassesTxtFile = new StreamReader(Ynaught_glycanMassesFilePath);
                    using (var txt = new CsvReader(glycanMassesTxtFile, true, '\t'))
                    {
                        while (txt.ReadNextRecord())
                        {
                            string glycanName = txt["Glycan"];
                            double glycanMass = double.Parse(txt["Mass"], CultureInfo.InvariantCulture);
                            if (!glycanMassDictionary.ContainsKey(glycanMass))
                                glycanMassDictionary.Add(glycanMass, glycanName);
                        }
                    }

                    //set the rawfile path and open it
                    FileReader rawFile = new FileReader(Ynaught_rawFilePath);
                    FileReader typeCheck = new FileReader();
                    bool thermo = true;
                    if (typeCheck.CheckFileFormat(Ynaught_rawFilePath).ToString().Contains("MzML"))
                        thermo = false;

                    //update the timer
                    if (Ynaught_FinishTimeLabel.InvokeRequired)
                    {
                        Ynaught_FinishTimeLabel.Invoke(new Action(() => Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
                    }
                    else
                    {
                        Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                    }

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
                    string fileNameShort = Path.GetFileNameWithoutExtension(Ynaught_rawFilePath);
                    StreamWriter outputYion = new StreamWriter(Path.Combine(outputPath + @"\" + fileNameShort + "_GlyCounter_YionSignal.txt"));
                    StreamWriter outputSummary = new StreamWriter(Path.Combine(outputPath + @"\" + fileNameShort + "_GlyCounter_YionSummary.txt"));
                    StreamWriter outputIPSA = null;
                    if (Ynaught_ipsa)
                    {
                        outputIPSA = new StreamWriter(Path.Combine(outputPath + @"\" + fileNameShort + "_Glycounter_YionIPSA.txt"));

                        outputIPSA.WriteLine("ScanNumber" + '\t' + "Yion" + '\t' + "m/z" + '\t' + "MassError");
                    }

                    string toleranceString = "ppmTol= ";
                    if (Ynaught_usingda)
                        toleranceString = "daTol= ";

                    outputSummary.WriteLine("Settings:\t" + toleranceString + Ynaught_tol + ", SNthreshold= " + Ynaught_SNthreshold +
                        "IntensityThreshold= " + Ynaught_intensityThreshold + "Charge states checked: " + Ynaught_chargeLB + " to " + Ynaught_chargeUB + ", First isotope checked: "
                        + FirstIsotopeCheckBox.Checked + ", Second isotope checked: " + SecondIsotopeCheckBox.Checked);
                    outputSummary.WriteLine(Ynaught_startTimeLabel.Text);
                    outputSummary.WriteLine();

                    //create PSM list to add each entry to
                    List<PSM> psmList = new List<PSM>();
                    psmList.Clear();

                    //this is currently set up for MSFragger data form the psms file
                    //we might want to write a converter for different data types
                    using StreamReader pepIDtxtFile = new StreamReader(Ynaught_pepIDFilePath);
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
                            double precursorMZ = double.Parse(txt["Observed M/Z"], CultureInfo.InvariantCulture);

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
                                        double modMass = Convert.ToDouble(modsArray3[0], CultureInfo.InvariantCulture);

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

                    // Build a list of all Y-ion/charge state combinations to use for header and data
                    var yIonHeaderColumns = new List<string>();
                    var yIonChargeStatePairs = new List<(Yion yIon, int charge)>();

                    //the same yion is often added from multiple sources. This should combine them all and add their sources to a string
                    yIonHashSet = CombineDuplicateYions(yIonHashSet);

                    // Determine the global charge state bounds for all PSMs
                    int globalChargeLowerBound = 1;
                    int globalChargeUpperBound = 1;
                    foreach (var psm in psmList)
                    {
                        int precursorCharge = psm.charge;
                        int chargeLowerBound = 1;
                        int chargeUpperBound = precursorCharge;
                        //use the user input to determine what charge states to look for. Set the minimum charge state to 1
                        if (Ynaught_chargeLB.Contains('P'))
                        {
                            if (Ynaught_chargeLB.Contains('-'))
                            {
                                //user entered 'P-X' so X is the subtracted value
                                var subtractedValue = int.Parse(Ynaught_chargeLB.Split('-')[1]);
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
                                var num = int.Parse(Ynaught_chargeLB);
                                chargeLowerBound = num;
                            }
                            catch (Exception exception) { } //catch the error but don't do anything. The default value should be used.
                        }
                        if (Ynaught_chargeUB.Contains('P'))
                        {
                            if (Ynaught_chargeUB.Contains('-'))
                            {
                                //user entered 'P-X' so X is the subtracted value
                                var subtractedValue = int.Parse(Ynaught_chargeUB.Split('-')[1]);
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
                                var num = int.Parse(Ynaught_chargeUB);
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

                    // Build header columns
                    if (Ynaught_condenseChargeStates)
                    {
                        foreach (var yIon in yIonHashSet)
                        {
                            yIonHeaderColumns.Add(yIon.description);
                            yIonChargeStatePairs.Add((yIon, 0)); // 0 means condensed
                        }
                    }
                    else
                    {
                        foreach (var yIon in yIonHashSet)
                        {
                            for (int charge = globalChargeLowerBound; charge <= globalChargeUpperBound; charge++)
                            {
                                yIonHeaderColumns.Add($"{yIon.description}_+{charge}");
                                yIonChargeStatePairs.Add((yIon, charge));
                            }
                        }
                    }

                    foreach (PSM psm in psmList)
                    {
                        SpectrumEx spectrum = rawFile.ReadSpectrumEx(scanNumber: psm.spectrumNumber);
                        if (spectrum.MsLevel == 2 && !spectrum.Analyzer.Contains("ITMS") && spectrum.TotalIonCurrent > 0)
                        {
                            int numberOfYions = 0;
                            double totalYionSignal = 0;
                            int numberOfChargeStatesConsidered = 1;

                            bool hcdTrue = false;
                            bool etdTrue = false;

                            bool Y0_found = false;
                            bool intactGlycoPep_found = false;

                            numberOfMS2scans++;
                            if (spectrum.ScanFilter.Contains("etd") || spectrum.Precursors[0].FramentationMethod.ToString().Contains("ETD"))
                            {
                                numberOfETDscans++;
                                etdTrue = true;
                            }
                            if (spectrum.ScanFilter.Contains("hcd") || spectrum.Precursors[0].FramentationMethod.ToString().Contains("HCD"))
                            {
                                numberOfHCDscans++;
                                hcdTrue = true;
                            }

                            Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();

                            RankOrderPeaks(sortedPeakDepths, spectrum);

                            List<SpecDataPointEx> yIonFoundPeaks = new List<SpecDataPointEx>();

                            //set up peptide and glycopeptide masses to look for
                            double peptideNoGlycan_MonoMass = psm.peptideNoGlycanMods.MonoisotopicMass;
                            double peptideNoGlycan_firstIsoMass = psm.peptideNoGlycanMods.MonoisotopicMass + (1 * Constants.C13C12Difference);
                            double peptideNoGlycan_secondIsoMass = psm.peptideNoGlycanMods.MonoisotopicMass + (2 * Constants.C13C12Difference);

                            double glycopeptide_MonoMass = psm.peptide.MonoisotopicMass;
                            double glycopeptide_firstIsoMass = psm.peptide.MonoisotopicMass + (1 * Constants.C13C12Difference);
                            double glycopeptide_secondIsoMass = psm.peptide.MonoisotopicMass + (2 * Constants.C13C12Difference);

                            //look for each Y-ion
                            List<Yion> finalYionList = new List<Yion>(); //creating this to store charge separately

                            foreach (Yion yIon in yIonHashSet)
                            {
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
                                if (Ynaught_chargeLB.Contains('P'))
                                {
                                    if (Ynaught_chargeLB.Contains('-'))
                                    {
                                        //user entered 'P-X' so X is the subtracted value
                                        var subtractedValue = int.Parse(Ynaught_chargeLB.Split('-')[1]);
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
                                        var num = int.Parse(Ynaught_chargeLB);
                                        chargeLowerBound = num;
                                    }
                                    catch (Exception exception) { } //catch the error but don't do anything. The default value should be used.
                                }

                                int chargeUpperBound = precursorCharge;
                                if (Ynaught_chargeUB.Contains('P'))
                                {
                                    if (Ynaught_chargeUB.Contains('-'))
                                    {
                                        //user entered 'P-X' so X is the subtracted value
                                        var subtractedValue = int.Parse(Ynaught_chargeUB.Split('-')[1]);
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
                                        var num = int.Parse(Ynaught_chargeUB);
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

                                //how many charge states are we looking for?
                                numberOfChargeStatesConsidered = chargeUpperBound - chargeLowerBound + 1;

                                //use intensity threshold for mzml files
                                if (CanConvertDouble(Ynaught_intTextBox.Text, Ynaught_intensityThreshold))
                                    Ynaught_intensityThreshold = Convert.ToDouble(Ynaught_intTextBox.Text, CultureInfo.InvariantCulture);

                                //find all the Yions for each charge state considered
                                for (int i = chargeUpperBound; i >= chargeLowerBound; i--)
                                {
                                    //this is for eveything where we add glycan mass to the peptide itself
                                    if (!yIon.glycanSource.Contains("Subtraction"))
                                    {
                                        double yIon_mz = (peptideNoGlycan_MonoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                        SpecDataPointEx peak = GetPeak(spectrum, yIon_mz, Ynaught_usingda, Ynaught_tol, thermo);

                                        if ((thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > 0 && (peak.Intensity / peak.Noise) > SNthreshold)
                                            || (!thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > Ynaught_intensityThreshold))
                                        {
                                            countYion = true; //this is to know if we can count the Y-ion as being found for keeping track of scans
                                            if (yIon.description.Contains("Y0"))
                                                Y0_found = true;

                                            //look for isotopes if user selected option
                                            double firstIsotopeIntensity = 0;
                                            double secondIsotopeIntensity = 0;
                                            if (FirstIsotopeCheckBox.Checked)
                                            {
                                                double yIon_mzfirstIso = (peptideNoGlycan_firstIsoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                                SpecDataPointEx firstIsotopePeak = GetPeak(spectrum, yIon_mzfirstIso, Ynaught_usingda, Ynaught_tol, thermo);
                                                if ((thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > 0 && (firstIsotopePeak.Intensity / firstIsotopePeak.Noise) > SNthreshold)
                                                    || (!thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > Ynaught_intensityThreshold))
                                                    firstIsotopeIntensity = firstIsotopePeak.Intensity;
                                            }
                                            if (SecondIsotopeCheckBox.Checked)
                                            {
                                                double yIon_mzSecondIso = (peptideNoGlycan_secondIsoMass + yIon.theoMass + (i * Constants.Proton)) / i;
                                                SpecDataPointEx secondIsotopePeak = GetPeak(spectrum, yIon_mzSecondIso, Ynaught_usingda, Ynaught_tol, thermo);
                                                if ((thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > 0 && (secondIsotopePeak.Intensity / secondIsotopePeak.Noise) > SNthreshold)
                                                    || (!thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > Ynaught_intensityThreshold))
                                                    secondIsotopeIntensity = secondIsotopePeak.Intensity;
                                            }

                                            //store info in yIon object
                                            foundYion.intensities.Add(peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity);
                                            foundYion.mz.Add(peak.Mz);
                                            foundYion.chargeStates.Add(i);
                                            finalYionList.Add(foundYion);
                                            numberOfYions++;
                                            totalYionSignal += peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;
                                        }
                                    }

                                    //this is for glycan neutral losses from the intact glycopeptide
                                    else
                                    {
                                        double yIon_mz = (glycopeptide_MonoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                        SpecDataPointEx peak = GetPeak(spectrum, yIon_mz, Ynaught_usingda, Ynaught_tol, thermo);

                                        if ((thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > 0 && (peak.Intensity / peak.Noise) > SNthreshold)
                                            || (!thermo && !peak.Equals(new SpecDataPointEx()) && peak.Intensity > Ynaught_intensityThreshold))
                                        {
                                            countYion = true; //this is to know if we can count the Y-ion as being found for keeping track of scans
                                            if (yIon.description.Contains("Intact Mass"))
                                                intactGlycoPep_found = true;

                                            double firstIsotopeIntensity = 0;
                                            double secondIsotopeIntensity = 0;
                                            if (FirstIsotopeCheckBox.Checked)
                                            {
                                                double yIon_mzfirstIso = (glycopeptide_firstIsoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                                SpecDataPointEx firstIsotopePeak = GetPeak(spectrum, yIon_mzfirstIso, Ynaught_usingda, Ynaught_tol, thermo);
                                                if ((thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > 0 && (firstIsotopePeak.Intensity / firstIsotopePeak.Noise) > SNthreshold)
                                                    || (!thermo && !firstIsotopePeak.Equals(new SpecDataPointEx()) && firstIsotopePeak.Intensity > Ynaught_intensityThreshold)) ;
                                            }
                                            if (SecondIsotopeCheckBox.Checked)
                                            {
                                                double yIon_mzSecondIso = (glycopeptide_secondIsoMass - yIon.theoMass + (i * Constants.Proton)) / i;
                                                SpecDataPointEx secondIsotopePeak = GetPeak(spectrum, yIon_mzSecondIso, Ynaught_usingda, Ynaught_tol, thermo);
                                                if ((thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > 0 && (secondIsotopePeak.Intensity / secondIsotopePeak.Noise) > SNthreshold)
                                                    || (!thermo && !secondIsotopePeak.Equals(new SpecDataPointEx()) && secondIsotopePeak.Intensity > Ynaught_intensityThreshold))
                                                    secondIsotopeIntensity = secondIsotopePeak.Intensity;
                                            }

                                            foundYion.intensities.Add(peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity);
                                            foundYion.mz.Add(peak.Mz);
                                            foundYion.chargeStates.Add(i);
                                            finalYionList.Add(foundYion);
                                            numberOfYions++;
                                            totalYionSignal = totalYionSignal + peak.Intensity + firstIsotopeIntensity + secondIsotopeIntensity;


                                        }
                                    }

                                }
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
                                outputYion.Write("ScanNumber\tPeptideNoGlycan\tPeptideWithGlycan\tTotalGlycanComposition\tPrecursorMZ\tPrecursorCharge\tRetentionTime\t#ChargeStatesConsidered\tIonsFound\tScanInjTime\tDissociationType\tParentScan\tNumYions\tScanTIC\tTotalYionSignal\tYionTICfraction\t");
                                outputYion.WriteLine(string.Join("\t", yIonHeaderColumns));
                                firstSpectrumInFile = false;
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
                            outputYion.Write(psm.spectrumNumber + "\t" + psm.peptideNoGlycanMods.SequenceWithModifications + "\t" + psm.peptide.SequenceWithModifications + "\t" +
                                psm.totalGlycanComposition + "\t" + psm.precursorMZ + "\t" + psm.charge + "\t" + retentionTime + "\t" + numberOfChargeStatesConsidered + "\t" + chargeStatesFinal + "\t" +
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
                                if (Ynaught_condenseChargeStates)
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
                                            double theomass = (psm.precursorMZ * psm.charge) - (psm.charge * Constants.Proton) - yion.theoMass;
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
                                numberOfMS2scansWithYions++;
                                if (hcdTrue)
                                    numberOfMS2scansWithYions_hcd++;
                                if (etdTrue)
                                    numberOfMS2scansWithYions_etd++;
                            }

                        }
                        if (Ynaught_FinishTimeLabel.InvokeRequired)
                        {
                            Ynaught_FinishTimeLabel.Invoke(new Action(() => Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
                        }
                        else
                        {
                            Ynaught_FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                        }
                    }

                    double percentageYions = (double)numberOfMS2scansWithYions / (double)numberOfMS2scans * 100;
                    double percentageY0 = (double)numberOfMS2scansWithY0 / (double)numberOfMS2scans * 100;
                    double percentageGlycoPep = (double)numberOfMS2scansWithIntactGlycoPep / (double)numberOfMS2scans * 100;

                    double percentageYions_hcd = 0;
                    double percentageYions_etd = 0;
                    double percentageY0_hcd = 0;
                    double percentageY0_etd = 0;
                    double percentageGlycoPep_hcd = 0;
                    double percentageGlycoPep_etd = 0;

                    if (!(numberOfHCDscans == 0))
                    {
                        percentageYions_hcd = (double)numberOfMS2scansWithYions_hcd / (double)numberOfHCDscans * 100;
                        percentageY0_hcd = (double)numberOfMS2scansWithY0_hcd / (double)numberOfHCDscans * 100;
                        percentageGlycoPep_hcd = (double)numberOfMS2scansWithIntactGlycoPep_hcd / (double)numberOfHCDscans * 100;
                    }
                    if (!(numberOfETDscans == 0))
                    {
                        percentageYions_etd = (double)numberOfMS2scansWithYions_etd / (double)numberOfETDscans * 100;
                        percentageY0_etd = (double)numberOfMS2scansWithY0_etd / (double)numberOfETDscans * 100;
                        percentageGlycoPep_etd = (double)numberOfMS2scansWithIntactGlycoPep_etd / (double)numberOfETDscans * 100;
                    }

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
                yIonHashSet.Clear();
            }
        }
        private HashSet<Yion> CombineDuplicateYions(HashSet<Yion> yIonHashSet)
        {
            var combined = yIonHashSet
                .GroupBy(y => new { y.description, y.theoMass })
                .Select(g =>
                {
                    var first = g.First();
                    // Combine glycan sources, remove duplicates, and join as comma-separated string
                    first.glycanSource = string.Join(",", g.Select(y => y.glycanSource).Distinct());
                    return first;
                });
            return new HashSet<Yion>(combined);
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (file.EndsWith("raw") || file.EndsWith("mzML"))
                    fileList.Add(file);
            }
            textBox1.Text = "Successfully uploaded " + fileList.Count() + " file(s)";
        }

        private void polarityCB_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox toggle = sender as CheckBox;

            if (toggle.Checked)
            {
                SetNegativeMode();
            }
            else
            {
                SetPositiveMode();
            }
        }

        private void SetNegativeMode()
        {
            HexNAcCheckedListBox.BackColor = alternateBackColor;
            HexCheckedListBox.BackColor = alternateBackColor;
            M6PCheckedListBox.BackColor = alternateBackColor;
            FucoseCheckedListBox.BackColor = alternateBackColor;
            SialicAcidCheckedListBox.BackColor = alternateBackColor;
            OligosaccharideCheckedListBox.BackColor = alternateBackColor;

            HexNAcCheckedListBox.Items.Clear();
            HexCheckedListBox.Items.Clear();
            M6PCheckedListBox.Items.Clear();
            FucoseCheckedListBox.Items.Clear();
            SialicAcidCheckedListBox.Items.Clear();
            OligosaccharideCheckedListBox.Items.Clear();

            HexNAcCheckedListBox.Items.AddRange(HexNAcNeg);
            HexCheckedListBox.Items.AddRange(HexNeg);
            M6PCheckedListBox.Items.AddRange(ManNeg);
            FucoseCheckedListBox.Items.AddRange(FucoseNeg);
            SialicAcidCheckedListBox.Items.AddRange(SialicNeg);
            OligosaccharideCheckedListBox.Items.AddRange(OligoNeg);
        }

        private void SetPositiveMode()
        {
            HexNAcCheckedListBox.BackColor = normalBackColor;
            HexCheckedListBox.BackColor = normalBackColor;
            M6PCheckedListBox.BackColor = normalBackColor;
            FucoseCheckedListBox.BackColor = normalBackColor;
            SialicAcidCheckedListBox.BackColor = normalBackColor;
            OligosaccharideCheckedListBox.BackColor = normalBackColor;

            HexNAcCheckedListBox.Items.Clear();
            HexCheckedListBox.Items.Clear();
            M6PCheckedListBox.Items.Clear();
            FucoseCheckedListBox.Items.Clear();
            SialicAcidCheckedListBox.Items.Clear();
            OligosaccharideCheckedListBox.Items.Clear();

            HexNAcCheckedListBox.Items.AddRange(HexNAcPos);
            HexCheckedListBox.Items.AddRange(HexPos);
            M6PCheckedListBox.Items.AddRange(ManPos);
            FucoseCheckedListBox.Items.AddRange(FucosePos);
            SialicAcidCheckedListBox.Items.AddRange(SialicPos);
            OligosaccharideCheckedListBox.Items.AddRange(OligoPos);
        }
    }
}
