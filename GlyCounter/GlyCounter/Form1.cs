using CSMSL;
using CSMSL.Proteomics;
using LumenWorks.Framework.IO.Csv;
using MathNet.Numerics.Statistics;
using Nova.Data;
using Nova.Io.Read;
using System.CodeDom;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.RawFileReader;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;


namespace GlyCounter
{
    public partial class Form1 : Form
    {
        //todo add to glycountersettings class
        //iCounter variables
        private HashSet<Ion> _ionHashSet = [];
        private string _singleIonDesc = "";
        private double _singleIonMZ;

        public static GlyCounterSettings glySettings = new GlyCounterSettings();
        public static YnaughtSettings yNsettings = new YnaughtSettings();
        private bool restart = false;

        //for negative mode
        private Color normalBackColor = Color.White;
        private Color alternateBackColor = Color.Lavender;

        // For application updates
        private readonly UpdateManager _updateManager;

        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);

            HexNAcCheckedListBox.Items.AddRange(HexNAcPos);
            HexCheckedListBox.Items.AddRange(HexPos);
            SialicAcidCheckedListBox.Items.AddRange(SialicPos);
            OligosaccharideCheckedListBox.Items.AddRange(OligoPos);
            M6PCheckedListBox.Items.AddRange(ManPos);
            FucoseCheckedListBox.Items.AddRange(FucosePos);


            if (Properties.Settings1.Default.RestoreTabOnReset)
            {
                int lastTabIndex = Properties.Settings1.Default.LastTabIndex;
                if (lastTabIndex >= 0 && lastTabIndex < GlyCounter_AllTabs.TabPages.Count)
                {
                    GlyCounter_AllTabs.SelectedIndex = lastTabIndex;
                }
                Properties.Settings1.Default.RestoreTabOnReset = false;
                Properties.Settings1.Default.Save();
            }
            else
            {
                GlyCounter_AllTabs.SelectedIndex = 0; // Default to first tab
            }

            // Initialize the update manager
            _updateManager = UpdateManager.Instance;


            // Set window title to include version
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "?.?.?";
            Text = $@"GlyCounter v{versionString}";
            // Check for updates on startup (silently)
            Task.Run(() => CheckForUpdatesAsync(true));


            //event listener to update total uploaded files
            glySettings.fileList.CollectionChanged += FileList_CollectionChanged;
        }

        //helper function for parsing settings
        private bool CanConvertDouble(string input, double type)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            return converter.IsValid(input);
        }

        //set up timer
        private void OnTimerTick(object sender, EventArgs e)
        {
            FinishTimeLabel.Text = @"Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
            FinishTimeLabel.Refresh();
        }

        public static HashSet<Yion> CombineDuplicateYions(HashSet<Yion> yIonHashSet)
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
            return [..combined];
        }

        private void Gly_Reset_Click(object sender, EventArgs e)
        {
            restart = true;
            Properties.Settings1.Default.LastTabIndex = GlyCounter_AllTabs.SelectedIndex;
            Properties.Settings1.Default.RestoreTabOnReset = true;
            Properties.Settings1.Default.Save();
            Application.Restart();
            Environment.Exit(0);
        }

        private void Yn_reset_Click(object sender, EventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (file.EndsWith("raw") || file.EndsWith("mzML"))
                    _fileList.Add(file);
            }
            textBox1.Text = "Successfully uploaded " + _fileList.Count() + " file(s)";
        }

        /////////////////////////////////////////////////////
        /// This starts the code for iCounter
        /////////////////////////////////////////////////////

        private void iC_uploadButton_Click(object sender, EventArgs e)
        {
            _fileList = [];
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Multiselect = true;
            fdlg.Title = @"C# Corner Open File Dialog";

            // Set the initial directory to the last open folder, if it exists
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
            {
                fdlg.InitialDirectory = @"c:\"; // Default directory if no previous directory is found
            }

            fdlg.Filter = @"RAW and mzML files (*.raw;*.mzML)|*.raw;*.mzML|RAW files (*.raw*)|*.raw*|mzML files (*.mzML)|*.mzML";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                iC_uploadTB.Text = @"Successfully uploaded " + fdlg.FileNames.Length + @" file(s)";

                //set the most recent folder to the path of the last file selected
                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileNames.LastOrDefault());
                Properties.Settings1.Default.Save();

                //also set a default output directory to the path of the last file saved
                _defaultOutput = Path.GetDirectoryName(fdlg.FileNames.LastOrDefault());
            }

            //add file paths to file list
            foreach (string filePath in fdlg.FileNames)
                _fileList.Add(filePath);
        }

        private void iC_outputButton_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = @"Select Output Directory";
            dialog.UseDescriptionForTitle = true;
            dialog.ShowNewFolderButton = true;

            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
                dialog.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            else
                dialog.InitialDirectory = @"c:\"; // Default directory if no previous directory is found

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                iC_outputTB.Text = dialog.SelectedPath;
                Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(dialog.SelectedPath);
                Properties.Settings1.Default.Save();
            }
        }
        private void iC_customIonUploadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = @"C# Corner Open File Dialog";
            if (!string.IsNullOrEmpty(Properties.Settings1.Default.LastOpenFolder) && Directory.Exists(Properties.Settings1.Default.LastOpenFolder))
            {
                fdlg.InitialDirectory = Properties.Settings1.Default.LastOpenFolder;
            }
            else
                fdlg.InitialDirectory = @"c:\";
            fdlg.Filter = @"*.csv|*.csv";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() != DialogResult.OK) return;

            iC_customIonUploadTB.Text = fdlg.FileName;

            Properties.Settings1.Default.LastOpenFolder = Path.GetDirectoryName(fdlg.FileName);
            Properties.Settings1.Default.Save();
        }
        private void iC_customIonUploadTB_TextChanged(object sender, EventArgs e)
        {
            _csvCustomFile = iC_customIonUploadTB.Text;
        }

        private async void iC_startButton_Click(object sender, EventArgs e)
        {

            timer1.Interval = 1000;
            timer1.Tick += OnTimerTick;
            timer1.Start();
            iC_statusUpdatesLabel.Text = @"Processing...";
            iC_startTimeLabel.Text = @"Start Time: " + DateTime.Now.ToString("HH:mm:ss");
            try
            {
                await Task.Run(() =>
                {
                    bool usingda = false;

                    if (string.IsNullOrEmpty(_outputPath) || !Directory.Exists(_outputPath))
                    {
                        if (_fileList.Count > 0)
                            _outputPath = Path.GetDirectoryName(_fileList[0]) ??
                                          Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        else
                            _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }

                    if (string.IsNullOrEmpty(_outputPath) || !Directory.Exists(_outputPath))
                    {
                        if (_fileList.Count > 0)
                        {
                            _outputPath = Path.GetDirectoryName(_fileList[0]) ?? _defaultOutput;
                        }
                        else
                        {
                            _outputPath = _defaultOutput;
                        }

                        if (iC_outputTB.InvokeRequired)
                        {
                            iC_outputTB.Invoke(new Action(() => iC_outputTB.Text = _outputPath));
                        }
                        else
                        {
                            iC_outputTB.Text = _outputPath;
                        }
                    }

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    //make sure all user inputs are in the correct format, otherwise use defaults
                    if (iC_daCB.Checked)
                    {
                        if (CanConvertDouble(iC_toleranceTB.Text, _daTolerance))
                        {
                            _daTolerance = Convert.ToDouble(iC_toleranceTB.Text);
                            usingda = true;
                        }

                    }
                    else if (CanConvertDouble(iC_toleranceTB.Text, _ppmTolerance))
                        _ppmTolerance = Convert.ToDouble(iC_toleranceTB.Text);

                    if (usingda)
                        _tol = _daTolerance;
                    else
                        _tol = _ppmTolerance;

                    if (CanConvertDouble(iC_SNTB.Text, _SNthreshold))
                        _SNthreshold = Convert.ToDouble(iC_SNTB.Text);

                    if (CanConvertDouble(iC_intensityTB.Text, _intensityThreshold))
                        _intensityThreshold = Convert.ToDouble(iC_intensityTB.Text);

                    string toleranceString = "ppmTol: ";
                    if (usingda)
                        toleranceString = "DaTol: ";
                    //popup with settings to user
                    MessageBox.Show("You are using these settings:\r\n" + toleranceString + _tol + "\r\nSNthreshold: " +
                                    _SNthreshold + "\r\nIntensityTheshold: " + _intensityThreshold);

                    foreach (var item in iC_tmt11CBList.CheckedItems)
                        _ionHashSet.Add(ProcessIon(item, source: "TMT11"));

                    foreach (var item in iC_acylCBList.CheckedItems)
                        _ionHashSet.Add(ProcessIon(item, source: "Acyl-Lysine"));

                    foreach (var item in iC_tmt16CBList.CheckedItems)
                        _ionHashSet.Add(ProcessIon(item, source: "TMT16"));

                    foreach (var item in iC_miscIonsCBList.CheckedItems)
                        _ionHashSet.Add(ProcessIon(item, source: "Misc"));

                    if (!_csvCustomFile.Equals("empty"))
                    {
                        using StreamReader csvFile = new(_csvCustomFile);
                        using var csv = new CsvReader(csvFile, true);
                        while (csv.ReadNextRecord())
                        {
                            string userDescription = csv["Description"];
                            Ion ion = new Ion
                            {
                                theoMZ = double.Parse(csv["m/z"]),
                                description = double.Parse(csv["m/z"]) + ", " + userDescription,
                                ionSource = "Custom",
                                hcdCount = 0,
                                etdCount = 0,
                                uvpdCount = 0,
                                peakDepth = ArbitraryPeakDepthIfNotFound
                            };

                            //If an ion with the same theoretical m/z value exists, replace it with the one from the custom csv
                            List<Ion> ionsToRemove = [];
                            foreach (Ion checkedIon in _ionHashSet)
                                if (ion.Equals(checkedIon))
                                    ionsToRemove.Add(checkedIon);

                            foreach (Ion removeIon in ionsToRemove)
                                _ionHashSet.Remove(removeIon);

                            _ionHashSet.Add(ion);
                        }
                    }

                    if (_singleIonMZ != 0)
                    {
                        string userDescription = _singleIonDesc;
                        Ion ion = new Ion
                        {
                            theoMZ = _singleIonMZ,
                            description = _singleIonMZ + ", " + userDescription,
                            ionSource = "Custom",
                            hcdCount = 0,
                            etdCount = 0,
                            uvpdCount = 0,
                            peakDepth = ArbitraryPeakDepthIfNotFound
                        };

                        //If an ion with the same theoretical m/z value exists, replace it with the one from the custom csv
                        List<Ion> ionsToRemove = [];
                        foreach (Ion checkedIon in _ionHashSet)
                            if (ion.Equals(checkedIon))
                                _ionHashSet.Remove(checkedIon);

                        _ionHashSet.Add(ion);
                    }

                    if (iC_ipsaCB.Checked)
                        _ipsa = true;

                    foreach (string fileName in _fileList)
                    {
                        //reset ions
                        foreach (Ion ion in _ionHashSet)
                        {
                            ion.intensity = 0;
                            ion.peakDepth = ArbitraryPeakDepthIfNotFound;
                            ion.hcdCount = 0;
                            ion.etdCount = 0;
                            ion.uvpdCount = 0;
                            ion.measuredMZ = 0;
                        }

                        FileReader rawFile = new FileReader(fileName);
                        FileReader typeCheck = new FileReader();
                        string fileType = typeCheck.CheckFileFormat(fileName).ToString(); //either "ThermoRaw" or "MzML"
                        bool thermo = fileType != "MzML";

                        if (iC_statusUpdatesLabel.InvokeRequired)
                        {
                            iC_statusUpdatesLabel.Invoke(new Action(() => iC_statusUpdatesLabel.Text = "Current file: " + fileName));
                        }
                        else
                        {
                            iC_statusUpdatesLabel.Text = "Current file: " + fileName;

                        }

                        if (iC_finishTimeLabel.InvokeRequired)
                        {
                            iC_finishTimeLabel.Invoke(new Action(() => iC_finishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
                        }
                        else
                        {
                            iC_finishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");

                        }

                        //set vars - I can't be bothered to rename all these
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
                        int numberOfMSnscans = 0;
                        int numberOfHCDscans = 0;
                        int numberOfETDscans = 0;
                        int numberOfUVPDscans = 0;
                        double nce = 0.0;
                        bool firstSpectrumInFile = true;

                        //initialize streamwriter output files
                        string fileNameShort = Path.GetFileNameWithoutExtension(fileName);
                        StreamWriter outputSignal =
                            new StreamWriter(_outputPath + @"\" + fileNameShort + "_iCounter_Signal.txt");
                        StreamWriter outputPeakDepth =
                            new StreamWriter(_outputPath + @"\" + fileNameShort + "_iCounter_PeakDepth.txt");
                        StreamWriter outputIPSA = null;

                        if (ipsaCheckBox.Checked)
                            outputIPSA = new StreamWriter(_outputPath + @"\" + fileNameShort + "_iCounter_IPSA.txt");
                        StreamWriter outputSummary =
                            new StreamWriter(_outputPath + @"\" + fileNameShort + "_iCounter_Summary.txt");

                        //write headers
                        outputSignal.Write(
                            "ScanNumber\tMSLevel\tRetentionTime\tPrecursorMZ\tNCE\tScanTIC\tTotalFoundIonSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumIonsFound\tTotalFoundIonSignal\t");
                        outputPeakDepth.Write(
                            "ScanNumber\tMSLevel\tRetentionTime\tPrecursorMZ\tNCE\tScanTIC\tTotalFoundIonSignal\tScanInjTime\tDissociationType\tPrecursorScan\tNumIonsFound\tTotalFoundIonSignal\t");
                        outputIPSA?.WriteLine("ScanNumber\tIons\tMassError\t");
                        outputSummary.WriteLine("Settings:\t" + toleranceString + ", SNthreshold=" + _SNthreshold +
                                                ", IntensityThreshold=" + _intensityThreshold);
                        outputSummary.WriteLine("Started At: " + iC_startTimeLabel.Text);
                        outputSummary.WriteLine();
                        //start processing file
                        for (int i = rawFile.FirstScan; i <= rawFile.LastScan; i++)
                        {
                            SpectrumEx spectrum = rawFile.ReadSpectrumEx(scanNumber: i);
                            bool IT = spectrum.Analyzer.Contains("ITMS");

                            //custom ms levels
                            List<int> levels = [];

                            if (iC_msLevelLow.Value == iC_msLevelHigh.Value)
                                levels.Add(Convert.ToInt32(iC_msLevelLow.Value));

                            int lowestval;
                            int highestval;

                            if (iC_msLevelLow.Value < iC_msLevelHigh.Value)
                            {
                                lowestval = Convert.ToInt32(iC_msLevelLow.Value);
                                highestval = Convert.ToInt32(iC_msLevelHigh.Value);

                                levels = Enumerable.Range(lowestval, (highestval - lowestval + 1)).ToList();
                            }

                            //if user puts values in backwards for some reason
                            if (iC_msLevelLow.Value > iC_msLevelHigh.Value)
                            {
                                lowestval = Convert.ToInt32(iC_msLevelHigh.Value);
                                highestval = Convert.ToInt32(iC_msLevelLow.Value);

                                levels = Enumerable.Range(lowestval, (highestval - lowestval + 1)).ToList();
                            }

                            //if ignore ms levels is checked ignore levels list
                            if (!iC_noMSnFilterCB.Checked)
                                if (!levels.Contains(spectrum.MsLevel))
                                    continue;

                            numberOfMSnscans++;
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

                            string ionHeader = "";

                            if (spectrum is { TotalIonCurrent: > 0, BasePeakIntensity: > 0 })
                            {
                                Dictionary<double, int> sortedPeakDepths = new Dictionary<double, int>();
                                RankOrderPeaks(sortedPeakDepths, spectrum);

                                if (thermo)
                                {
                                    string scanFilter = spectrum.ScanFilter;
                                    string[] hcdHeader = scanFilter.Split('@');
                                    string[] splitHCDheader = [];
                                    splitHCDheader = hcdHeader[1].Contains("ptr")
                                        ? hcdHeader[2].Split('d')
                                        : hcdHeader[1].Split('d');
                                    string[] collisionEnergyArray = splitHCDheader[1].Split('.');
                                    nce = Convert.ToDouble(collisionEnergyArray[0]);
                                }
                                else nce = spectrum.Precursors[0].CollisionEnergy;

                                foreach (Ion ion in _ionHashSet)
                                {
                                    ion.intensity = 0;
                                    ion.peakDepth = ArbitraryPeakDepthIfNotFound;
                                    ionHeader = ionHeader + ion.description + "\t";
                                    ion.measuredMZ = 0;
                                    ion.intensity = 0;

                                    SpecDataPointEx peak = GetPeak(spectrum, ion.theoMZ, usingda, _tol, thermo, IT);

                                    if (!IT && thermo)
                                    {
                                        if (peak.Equals(new SpecDataPointEx()) || !(peak.Intensity > 0) ||
                                            !((peak.Intensity / peak.Noise) > _SNthreshold)) continue;

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
                                            !(peak.Intensity > _intensityThreshold)) continue;

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

                                if (firstSpectrumInFile)
                                {
                                    outputSignal.WriteLine(ionHeader);
                                    outputPeakDepth.WriteLine(ionHeader);
                                    firstSpectrumInFile = false;
                                }

                                if (numberOfIons == 1)
                                {
                                    numberOfMS2scansWithOxo_1++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_1_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_1_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_1_uvpd++;
                                }

                                if (numberOfIons == 2)
                                {
                                    numberOfMS2scansWithOxo_2++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_2_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_2_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_2_uvpd++;
                                }

                                if (numberOfIons == 3)
                                {
                                    numberOfMS2scansWithOxo_3++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_3_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_3_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_3_uvpd++;
                                }

                                if (numberOfIons == 4)
                                {
                                    numberOfMS2scansWithOxo_4++;
                                    if (hcdTrue)
                                        numberOfMS2scansWithOxo_4_hcd++;
                                    if (etdTrue)
                                        numberOfMS2scansWithOxo_4_etd++;
                                    if (uvpdTrue)
                                        numberOfMS2scansWithOxo_4_uvpd++;
                                }

                                if (numberOfIons > 4)
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
                                if (spectrum.MsLevel == 1) fragmentationType = "MS1";
                                if (hcdTrue) fragmentationType = "HCD";
                                if (etdTrue) fragmentationType = "ETD";
                                if (uvpdTrue) fragmentationType = "UVPD";
                                double retentionTime = spectrum.RetentionTime;
                                double precursormz = spectrum.Precursors[0].IsolationMz;
                                string peakString = "";
                                foreach (double theoMZ in ionFoundPeaks)
                                    peakString = peakString + theoMZ.ToString() + "; ";
                                string errorString = new string("");
                                foreach (double error in ionFoundMassErrors)
                                    errorString = errorString + error.ToString("F6") + "; ";

                                //write scan info
                                outputSignal.Write(i + "\t" + spectrum.MsLevel + '\t' + retentionTime + "\t" +
                                                   precursormz + "\t" + nce + "\t" + scanTIC + "\t" + totalSignal +
                                                   "\t" + scanInjTime + "\t" + fragmentationType + "\t" + parentScan +
                                                   "\t" + numberOfIons + "\t" + totalSignal + "\t");
                                outputPeakDepth.Write(i + "\t" + spectrum.MsLevel + '\t' + retentionTime + "\t" +
                                                      scanTIC + "\t" + totalSignal + "\t" + scanInjTime + "\t" +
                                                      fragmentationType + "\t" + parentScan + "\t" + numberOfIons +
                                                      "\t" + totalSignal + "\t");
                                outputIPSA?.WriteLine(i + "\t" + peakString + "\t" + errorString + "\t");

                                foreach (Ion ion in _ionHashSet)
                                {
                                    outputSignal.Write(ion.intensity + "\t");

                                    if (ion.peakDepth == ArbitraryPeakDepthIfNotFound)
                                        outputPeakDepth.Write("NotFound\t");
                                    else
                                        outputPeakDepth.Write(ion.peakDepth + "\t");
                                }

                                outputSignal.WriteLine();
                                outputPeakDepth.WriteLine();
                            }
                            if (iC_finishTimeLabel.InvokeRequired)
                            {
                                iC_finishTimeLabel.Invoke(new Action(() => iC_finishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss")));
                            }
                            else
                            {
                                iC_finishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
                            }
                        }

                        //all scans have been processed, get some total stats
                        double percentage1ox = (double)numberOfMS2scansWithOxo_1 / (double)numberOfMSnscans * 100;
                        double percentage2ox = (double)numberOfMS2scansWithOxo_2 / (double)numberOfMSnscans * 100;
                        double percentage3ox = (double)numberOfMS2scansWithOxo_3 / (double)numberOfMSnscans * 100;
                        double percentage4ox = (double)numberOfMS2scansWithOxo_4 / (double)numberOfMSnscans * 100;
                        double percentage5plusox =
                            (double)numberOfMS2scansWithOxo_5plus / (double)numberOfMSnscans * 100;
                        double percentageSum = percentage1ox + percentage2ox + percentage3ox + percentage4ox +
                                               percentage5plusox;
                        double numberofMS2scansWithOxo_double = (double)percentageSum / 100 * numberOfMSnscans;

                        double percentage1ox_hcd =
                            (double)numberOfMS2scansWithOxo_1_hcd / (double)numberOfHCDscans * 100;
                        double percentage2ox_hcd =
                            (double)numberOfMS2scansWithOxo_2_hcd / (double)numberOfHCDscans * 100;
                        double percentage3ox_hcd =
                            (double)numberOfMS2scansWithOxo_3_hcd / (double)numberOfHCDscans * 100;
                        double percentage4ox_hcd =
                            (double)numberOfMS2scansWithOxo_4_hcd / (double)numberOfHCDscans * 100;
                        double percentage5plusox_hcd =
                            (double)numberOfMS2scansWithOxo_5plus_hcd / (double)numberOfHCDscans * 100;
                        double percentageSum_hcd = percentage1ox_hcd + percentage2ox_hcd + percentage3ox_hcd +
                                                   percentage4ox_hcd + percentage5plusox_hcd;
                        double numberofHCDscansWithOxo_double = percentageSum_hcd / 100 * numberOfHCDscans;

                        double percentage1ox_etd =
                            (double)numberOfMS2scansWithOxo_1_etd / (double)numberOfETDscans * 100;
                        double percentage2ox_etd =
                            (double)numberOfMS2scansWithOxo_2_etd / (double)numberOfETDscans * 100;
                        double percentage3ox_etd =
                            (double)numberOfMS2scansWithOxo_3_etd / (double)numberOfETDscans * 100;
                        double percentage4ox_etd =
                            (double)numberOfMS2scansWithOxo_4_etd / (double)numberOfETDscans * 100;
                        double percentage5plusox_etd =
                            (double)numberOfMS2scansWithOxo_5plus_etd / (double)numberOfETDscans * 100;
                        double percentageSum_etd = percentage1ox_etd + percentage2ox_etd + percentage3ox_etd +
                                                   percentage4ox_etd + percentage5plusox_etd;
                        double numberofETDscansWithOxo_double = percentageSum_etd / 100 * numberOfETDscans;

                        double percentage1ox_uvpd =
                            (double)numberOfMS2scansWithOxo_1_uvpd / (double)numberOfUVPDscans * 100;
                        double percentage2ox_uvpd =
                            (double)numberOfMS2scansWithOxo_2_uvpd / (double)numberOfUVPDscans * 100;
                        double percentage3ox_uvpd =
                            (double)numberOfMS2scansWithOxo_3_uvpd / (double)numberOfUVPDscans * 100;
                        double percentage4ox_uvpd =
                            (double)numberOfMS2scansWithOxo_4_uvpd / (double)numberOfUVPDscans * 100;
                        double percentage5plusox_uvpd =
                            (double)numberOfMS2scansWithOxo_5plus_uvpd / (double)numberOfUVPDscans * 100;
                        double percentageSum_uvpd = percentage1ox_uvpd + percentage2ox_uvpd + percentage3ox_uvpd +
                                                    percentage4ox_uvpd + percentage5plusox_uvpd;
                        double numberofUVPDscansWithOxo_double = percentageSum_uvpd / 100 * numberOfUVPDscans;

                        double percentageHCD = (double)numberOfHCDscans / numberOfMSnscans * 100;
                        double percentageETD = (double)numberOfETDscans / numberOfMSnscans * 100;
                        double percentageUVPD = (double)numberOfUVPDscans / numberOfMSnscans * 100;

                        int numberofMS2scansWithOxo = (int)Math.Round(numberofMS2scansWithOxo_double);
                        int numberofHCDscansWithOxo = (int)Math.Round(numberofHCDscansWithOxo_double);
                        int numberofETDscansWithOxo = (int)Math.Round(numberofETDscansWithOxo_double);
                        int numberofUVPDscansWithOxo = (int)Math.Round(numberofUVPDscansWithOxo_double);

                        numberofMS2scansWithOxo = Math.Max(0, numberofMS2scansWithOxo);
                        numberofHCDscansWithOxo = Math.Max(0, numberofHCDscansWithOxo);
                        numberofETDscansWithOxo = Math.Max(0, numberofETDscansWithOxo);
                        numberofUVPDscansWithOxo = Math.Max(0, numberofUVPDscansWithOxo);

                        outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");
                        outputSummary.WriteLine("MSn Scans\t" + numberOfMSnscans + "\t" + numberOfHCDscans + "\t" +
                                                numberOfETDscans + "\t" + numberOfUVPDscans
                                                + "\t" + 100 + "\t" + percentageHCD + "\t" + percentageETD + "\t" +
                                                percentageUVPD);
                        outputSummary.WriteLine("MSn Scans with Found Ions\t" + numberofMS2scansWithOxo + "\t" +
                                                numberofHCDscansWithOxo + "\t" + numberofETDscansWithOxo + "\t" +
                                                numberofUVPDscansWithOxo
                                                + "\t" + percentageSum + "\t" + percentageSum_hcd + "\t" +
                                                percentageSum_etd + "\t" + percentageSum_uvpd);
                        outputSummary.WriteLine("IonCount_1\t" + numberOfMS2scansWithOxo_1 + "\t" +
                                                numberOfMS2scansWithOxo_1_hcd + "\t" + numberOfMS2scansWithOxo_1_etd +
                                                "\t" + numberOfMS2scansWithOxo_1_uvpd
                                                + "\t" + percentage1ox + "\t" + percentage1ox_hcd + "\t" +
                                                percentage1ox_etd + "\t" + percentage1ox_uvpd);
                        outputSummary.WriteLine("IonCount_2\t" + numberOfMS2scansWithOxo_2 + "\t" +
                                                numberOfMS2scansWithOxo_2_hcd + "\t" + numberOfMS2scansWithOxo_2_etd +
                                                "\t" + numberOfMS2scansWithOxo_2_uvpd
                                                + "\t" + percentage2ox + "\t" + percentage2ox_hcd + "\t" +
                                                percentage2ox_etd + "\t" + percentage2ox_uvpd);
                        outputSummary.WriteLine("IonCount_3\t" + numberOfMS2scansWithOxo_3 + "\t" +
                                                numberOfMS2scansWithOxo_3_hcd + "\t" + numberOfMS2scansWithOxo_3_etd +
                                                "\t" + numberOfMS2scansWithOxo_3_uvpd
                                                + "\t" + percentage3ox + "\t" + percentage3ox_hcd + "\t" +
                                                percentage3ox_etd + "\t" + percentage3ox_uvpd);
                        outputSummary.WriteLine("IonCount_4\t" + numberOfMS2scansWithOxo_4 + "\t" +
                                                numberOfMS2scansWithOxo_4_hcd + "\t" + numberOfMS2scansWithOxo_4_etd +
                                                "\t" + numberOfMS2scansWithOxo_4_uvpd
                                                + "\t" + percentage4ox + "\t" + percentage4ox_hcd + "\t" +
                                                percentage4ox_etd + "\t" + percentage4ox_uvpd);
                        outputSummary.WriteLine("IonCount_5+\t" + numberOfMS2scansWithOxo_5plus + "\t" +
                                                numberOfMS2scansWithOxo_5plus_hcd + "\t" +
                                                numberOfMS2scansWithOxo_5plus_etd + "\t" +
                                                numberOfMS2scansWithOxo_5plus_uvpd
                                                + "\t" + percentage5plusox + "\t" + percentage5plusox_hcd + "\t" +
                                                percentage5plusox_etd + "\t" + percentage5plusox_uvpd);

                        outputSummary.WriteLine(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\" + "\t" +
                                                @"\\\\\\\\\\" + "\t" + @"\\\\\\\\\\");
                        outputSummary.WriteLine("\tTotal\tHCD\tETD\tUVPD\t%Total\t%HCD\t%ETD\t%UVPD");

                        string currentSource = "";
                        foreach (Ion ion in _ionHashSet)
                        {
                            int total = ion.hcdCount + ion.etdCount + ion.uvpdCount;

                            double percentTotal = (double)total / (double)numberOfMSnscans * 100;
                            double percentHCD = (double)ion.hcdCount / (double)numberOfHCDscans * 100;
                            double percentETD = (double)ion.etdCount / (double)numberOfETDscans * 100;
                            double percentUVPD = (double)ion.uvpdCount / (double)numberOfUVPDscans * 100;

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
                _ionHashSet.Clear();
            }
        }

        private void iC_outputTB_TextChanged(object sender, EventArgs e)
        {
            _outputPath = iC_outputTB.Text + @"\";
        }

        private void iC_tmt11Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(iC_tmt11CBList);
        }

        private void iC_acylButton_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(iC_acylCBList);
        }

        private void iC_tmt16Button_Click(object sender, EventArgs e)
        {
            SelectAllItems_CheckedBox(iC_tmt16CBList);
        }

        private void iC_clearButton_Click(object sender, EventArgs e)
        {
            while (iC_tmt16CBList.CheckedIndices.Count > 0)
                iC_tmt16CBList.SetItemChecked(iC_tmt16CBList.CheckedIndices[0], false);

            while (iC_acylCBList.CheckedIndices.Count > 0)
                iC_acylCBList.SetItemChecked(iC_acylCBList.CheckedIndices[0], false);

            while (iC_tmt11CBList.CheckedIndices.Count > 0)
                iC_tmt11CBList.SetItemChecked(iC_tmt11CBList.CheckedIndices[0], false);

            iC_tmt16CBList.ClearSelected();
            iC_acylCBList.ClearSelected();
            iC_tmt11CBList.ClearSelected();
        }

        private void iC_singleIonDesc_TextChanged(object sender, EventArgs e)
        {
            _singleIonDesc = iC_singleIonDesc.Text;
        }

        private void iC_singleIonMZ_TextChanged(object sender, EventArgs e)
        {
            if (double.TryParse(iC_singleIonMZ.Text, out double result))
            {
                _singleIonMZ = result;
            }
        }
    }
}
