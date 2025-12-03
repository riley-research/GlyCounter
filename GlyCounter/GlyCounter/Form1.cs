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
        public static GlyCounterSettings glySettings = new GlyCounterSettings();
        public static YnaughtSettings yNsettings = new YnaughtSettings();
        private bool restart = false;
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
            this.Text = $"GlyCounter v{versionString}";
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
            FinishTimeLabel.Text = "Finish time: still running as of " + DateTime.Now.ToString("HH:mm:ss");
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
            return new HashSet<Yion>(combined);
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
            restart = true;
            Properties.Settings1.Default.LastTabIndex = GlyCounter_AllTabs.SelectedIndex;
            Properties.Settings1.Default.RestoreTabOnReset = true;
            Properties.Settings1.Default.Save();
            Application.Restart();
            Environment.Exit(0);
        }
    }
}
