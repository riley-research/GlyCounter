using System.Drawing.Text;

namespace GlyCounter
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private string[] HexNAcPos = { "84.0444, HexNAc - C2H8O4", "126.055, HexNAc - C2H6O3", "138.055, HexNAc - CH6O3", "144.0655, HexNAc - C2H4O2", "168.0655, HexNAc - 2H2O", "186.0761, HexNAc - H2O", "204.0867, HexNAc" };
        private string[] HexNAcNeg = { "154.0510, HexNAc - CH2O (B,Z)", "160.0610, HexNAc - C2H2O (Z)", "162.0767, HexNAc - C2H4O2", "166.0511, HexNAc - H2O", "178.0710, HexNAc - C2H2O (Y)", "184.0616, HexNAc (B,Z)", "202.0716, HexNAc, (B)", "204.0878, HexNAc (Z)", "220.0816, HexNAc (C)", "222.0978, HexNAc, (Y)" };

        private string[] HexPos = { "85.0284, Hex - C2H6O3", "97.0284, Hex - CH6O3", "127.0390, Hex - 2H2O", "145.0495, Hex - H2O", "163.0601, Hex" };
        private string[] HexNeg = { "161.0450, Hex (B)", "179.0550, Hex (C)" };

        private string[] ManPos = { "243.0264, Man-P", "405.0798, Man2-P" };
        private string[] ManNeg = { "241.0113, Man-P", "403.0647, Man2-P" };

        private string[] SialicPos = { "274.0921, NeuAc-H2O", "292.1027, NeuAc", "316.103, NeuAc[Ac] - H2O", "334.113, NeuAc[Ac]", "290.0870, NeuGc - H2O", "308.0976, NeuGc", "332.098, NeuGc[Ac] - H2O", "350.1081, NeuGc[Ac]" };
        private string[] SialicNeg = { "290.0876, NeuAc (B)", "308.0976, NeuAc (C)" };

        private string[] FucosePos = { "350.1446, HexNAc-dHex", "512.1974, HexNAc-Hex-dHex (LeX/A)", "674.2502, HexNAc-Hex2-dHex", "803.2928, HexNAc-Hex-dHex-NeuAc (sLeX/A)", "819.2908, HexNAc-Hex-dHex-NeuGc", "877.3296, HexNAc2-Hex2-dHex (diLacNAc-Fuc)" };
        private string[] FucoseNeg = { "163.0601, dHex (C)", "165.0762, dHex (Y)", "350.1457, HexNAc-dHex (Z)", "368.1557, HexNAc-dHex (Y)", "307.1029, Hex-dHex (B)", "325.1129, Hex-dHex (C)", "488.1979, HexNAc-Hex-dHex", "510.1823, HexNAc-Hex-dHex (B)", 
            "553.2251, HexNAc2-dHex (Y,Z)", "697.2678, HexNAc2-Hex-dHex (Z,Z)", "715.2778, HexNAc2-Hex-dHex (Z)", "733.2879, HexNAc2-Hex-dHex (Y)", "895.3407, HexNAc2-Hex2-dHex (Y,Y)", "1057.3935, HexNAc2-Hex3 (Y,Y)", "1080.4101, HexNAc3-Hex2-dHex (Z)", "1098.4201, HexNAc3-Hex2-dHex (Y)" };

        private string[] OligoPos = { "325.1129, Hex2", "366.1395, HexNAc-Hex", "407.1660, HexNAc2", "454.1555, Hex-NeuAc", "470.1503, Hex-NeuGc", "495.1821, HexNAc-NeuAc", "511.1769, HexNAc-NeuGc", "528.1923, HexNAc-Hex2", 
            "537.1927, HexNAc-NeuAc[Ac]", "553.1875, HexNAc-NeuGc[Ac]", "569.2188, HexNAc2-Hex", "657.2349, HexNAc-Hex-NeuAc", "673.2297, HexNAc-Hex-NeuGc", "690.2451, HexNAc-Hex3", "731.2717, HexNAc2-Hex2 (diLacNAc)", 
            "819.2877, HexNAc-Hex2-NeuAc", "835.2825, HexNAc-Hex2-NeuGc", "860.3143, HexNAc2-Hex-NeuAc", "876.3091, HexNAc2-Hex-NeuGc", "893.3245, HexNAc2-Hex3", "948.3303, HexNAc-Hex-NeuAc2", "964.3251, HexNAc-Hex-NeuGc2", 
            "1022.3671, HexNAc2-Hex2-NeuAc1", "1038.3619, HexNAc2-Hex2-NeuGc1", "1313.4625, HexNAc2-Hex2-NeuAc2", "1329.4573, HexNAc2-Hex2-NeuGc2" };
        private string[] OligoNeg = { "364.1244, HexNAc-Hex (B)", "366.1406, HexNAc-Hex (Z)", "382.1344, HexNAc-Hex (C)", "384.1506, HexNAc-Hex (Y)", "389.1572, HexNAc2 (Z,Z)", "407.1672, HexNAc2 (Z)",
            "425.1772, HexNAc2 (Y)", "544.4, HexNAc-Hex2 (C,Y)", "551.2100, HexNAc2-Hex (Z,Z)", "569.2200, HexNAc2-Hex (Z)", "587.2300, HexNAc2-Hex (Y)", "675.2460, HexNAc-Hex-NeuAc (Y)", "731.2728, HexNAc2-Hex2 (Z)",
            "749.2828, HexNAc2-Hex2 (Y)", "829.2396, HexNAc2-Hex2 (Y)", "873.2994, HexNAc2-Hex3 (B,Z)", "934.3522, HexNAc3-Hex2 (Y,Z)", "952.3622, HexNAc3-Hex2 (Y,Y)", "1096.4050, HexNAc3-Hex3 (Y,Z)", "1114.415, HexNAc3-Hex3 (Y)",
            "1120.3350, HexNAc2-Hex2-NeuAc (Y)", "1299.4844, HexNAc3-Hex3 (Z)" };

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            textBox1 = new TextBox();
            browseButton = new Button();
            HexNAcCheckedListBox = new CheckedListBox();
            HexCheckedListBox = new CheckedListBox();
            SialicAcidCheckedListBox = new CheckedListBox();
            OligosaccharideCheckedListBox = new CheckedListBox();
            HexNAc_ions = new Label();
            Hex_ions = new Label();
            Sia_ions = new Label();
            Oligosaccharide_ions = new Label();
            M6PCheckedListBox = new CheckedListBox();
            M6P_ions = new Label();
            StartButton = new Button();
            ClearButton = new Button();
            ppmTol_textBox = new TextBox();
            ppmTol_label = new Label();
            CheckAll_Button = new Button();
            CheckAll_Hex_Button = new Button();
            CheckAll_HexNAc_Button = new Button();
            CheckAll_Sialic_Button = new Button();
            CheckAll_M6P_Button = new Button();
            CheckAll_Oligo_Button = new Button();
            SN_textBox = new TextBox();
            SN_label = new Label();
            PeakDepth_Box_HCD = new TextBox();
            PeakDepth_label_HCD = new Label();
            hcdTICfraction = new TextBox();
            etdTICfraction = new TextBox();
            hcdTICfraction_Label = new Label();
            etdTICfraction_Label = new Label();
            PeakDepth_Box_ETD = new TextBox();
            HCDsettingLabel = new Label();
            ETDsettingsLabel = new Label();
            PeakDepth_label_ETD = new Label();
            MostCommonButton = new Button();
            FucoseCheckedListBox = new CheckedListBox();
            Fucose_ions_label = new Label();
            CheckAll_Fucose_Button = new Button();
            OxoCountThreshold_hcd_label = new Label();
            OxoCountRequireBox_hcd = new TextBox();
            OxoCountThreshold_etd_label = new Label();
            OxoCountRequireBox_etd = new TextBox();
            uploadCustomTextBox = new TextBox();
            UploadCustomBrowseButton = new Button();
            timer1 = new System.Windows.Forms.Timer(components);
            StatusLabel = new Label();
            FinishTimeLabel = new Label();
            StartTimeLabel = new Label();
            GlyCounterLogo = new PictureBox();
            GlyCounter_AllTabs = new TabControl();
            GlyCounter_Tab = new TabPage();
            ignoreMSLevelCB = new CheckBox();
            label3 = new Label();
            label2 = new Label();
            MSLevelUB = new NumericUpDown();
            MSLevelLB = new NumericUpDown();
            polarityCB = new CheckBox();
            Gly_outputButton = new Button();
            Gly_outputTextBox = new TextBox();
            ipsaCheckBox = new CheckBox();
            DaltonCheckBox = new CheckBox();
            OxoCountThreshold_uvpd_label = new Label();
            uvpdTICfraction_Label = new Label();
            PeakDepth_label_UVPD = new Label();
            OxoCountRequireBox_uvpd = new TextBox();
            uvpdTICfraction = new TextBox();
            PeakDepth_Box_UVPD = new TextBox();
            UVPDsettingslabel = new Label();
            label1 = new Label();
            intensityThresholdLabel = new Label();
            intensityThresholdTextBox = new TextBox();
            YnaughtTab = new TabPage();
            Ynaught_intLabel = new Label();
            Ynaught_intTextBox = new TextBox();
            Ynaught_outputButton = new Button();
            Ynaught_outputTextBox = new TextBox();
            YNaught_IPSAcheckbox = new CheckBox();
            ChargeExplanationLabel = new Label();
            UpperBoundLabel = new Label();
            LowerBoundLabel = new Label();
            dashLabel = new Label();
            UpperBoundTextBox = new TextBox();
            LowerBoundTextBox = new TextBox();
            panel1 = new Panel();
            SeparateChargeStates = new RadioButton();
            GroupChargeStates = new RadioButton();
            Ynaught_DaCheckBox = new CheckBox();
            Ynaught_GlyCounterLogo = new PictureBox();
            Ynaught_FinishTimeLabel = new Label();
            Ynaught_startTimeLabel = new Label();
            Ynaught_StartButton = new Button();
            NeutralLosses_Label = new Label();
            ClearAllSelections_Button = new Button();
            Ynaught_SNlabel = new Label();
            Ynaught_SNthresholdTextBox = new TextBox();
            CommonOglyco_Label = new Label();
            FucoseYions_Label = new Label();
            CommonNglycoLabel = new Label();
            CheckAllOglyco_Button = new Button();
            CheckAllNeutralLosses_Button = new Button();
            CheckAllFucose_Button = new Button();
            CheckAllNglyco_Button = new Button();
            Ynaught_ppmTolLabel = new Label();
            Ynaught_ppmTolTextBox = new TextBox();
            Yions_CheckAllButton = new Button();
            Ynaught_ChargeStatesHeader = new Label();
            SecondIsotopeCheckBox = new CheckBox();
            MonoisotopeLabel2 = new Label();
            MonoisotopeLabel1 = new Label();
            Yions_CheckNglycoSialylButton = new Button();
            Yions_CheckNglycoFucoseButton = new Button();
            BrowseCustomSubtractions_Button = new Button();
            BrowseCustomAdditions_Button = new Button();
            Ynaught_CustomSubtractions_TextBox = new TextBox();
            Ynaught_CustomAdditions_TextBox = new TextBox();
            LoadInGlycoPepRawFile_TextBox = new TextBox();
            BrowseGlycoPepRawFiles_Button = new Button();
            Yions_OlinkedChecklistBox = new CheckedListBox();
            Yions_CheckNglycoMannoseButton = new Button();
            Yions_LossFromPepChecklistBox = new CheckedListBox();
            Yions_FucoseNlinkedCheckedBox = new CheckedListBox();
            Yions_NlinkedCheckBox = new CheckedListBox();
            FirstIsotopeCheckBox = new CheckBox();
            BrowseGlycoPepIDs = new Button();
            LoadInGlycoPepIDs_TextBox = new TextBox();
            timer2 = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)GlyCounterLogo).BeginInit();
            GlyCounter_AllTabs.SuspendLayout();
            GlyCounter_Tab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MSLevelUB).BeginInit();
            ((System.ComponentModel.ISupportInitialize)MSLevelLB).BeginInit();
            YnaughtTab.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)Ynaught_GlyCounterLogo).BeginInit();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(16, 18);
            textBox1.Margin = new Padding(4, 3, 4, 3);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(1085, 25);
            textBox1.TabIndex = 0;
            textBox1.Text = "Upload .raw or .mzML files";
            // 
            // browseButton
            // 
            browseButton.Location = new Point(1109, 18);
            browseButton.Margin = new Padding(4, 3, 4, 3);
            browseButton.Name = "browseButton";
            browseButton.Size = new Size(88, 25);
            browseButton.TabIndex = 3;
            browseButton.Text = "Browse";
            browseButton.UseVisualStyleBackColor = true;
            browseButton.Click += button1_Click;
            // 
            // HexNAcCheckedListBox
            // 
            HexNAcCheckedListBox.BackColor = SystemColors.Window;
            HexNAcCheckedListBox.CheckOnClick = true;
            HexNAcCheckedListBox.FormattingEnabled = true;
            HexNAcCheckedListBox.Location = new Point(16, 158);
            HexNAcCheckedListBox.Margin = new Padding(4, 3, 4, 3);
            HexNAcCheckedListBox.Name = "HexNAcCheckedListBox";
            HexNAcCheckedListBox.Size = new Size(276, 130);
            HexNAcCheckedListBox.Items.AddRange(HexNAcPos);
            HexNAcCheckedListBox.TabIndex = 1;
            HexNAcCheckedListBox.SelectedIndexChanged += HexNAcCheckedListBox_SelectedIndexChanged;
            // 
            // HexCheckedListBox
            // 
            HexCheckedListBox.CheckOnClick = true;
            HexCheckedListBox.FormattingEnabled = true;
            HexCheckedListBox.Location = new Point(16, 367);
            HexCheckedListBox.Margin = new Padding(4, 3, 4, 3);
            HexCheckedListBox.Name = "HexCheckedListBox";
            HexCheckedListBox.Size = new Size(276, 94);
            HexCheckedListBox.Items.AddRange(HexPos);
            HexCheckedListBox.TabIndex = 4;
            HexCheckedListBox.SelectedIndexChanged += HexCheckedListBox_SelectedIndexChanged;
            // 
            // SialicAcidCheckedListBox
            // 
            SialicAcidCheckedListBox.CheckOnClick = true;
            SialicAcidCheckedListBox.FormattingEnabled = true;
            SialicAcidCheckedListBox.Location = new Point(315, 158);
            SialicAcidCheckedListBox.Margin = new Padding(4, 3, 4, 3);
            SialicAcidCheckedListBox.Name = "SialicAcidCheckedListBox";
            SialicAcidCheckedListBox.Size = new Size(278, 130);
            SialicAcidCheckedListBox.Items.AddRange(SialicPos);
            SialicAcidCheckedListBox.TabIndex = 2;
            SialicAcidCheckedListBox.SelectedIndexChanged += SialicAcidCheckedListBox_SelectedIndexChanged;
            // 
            // OligosaccharideCheckedListBox
            // 
            OligosaccharideCheckedListBox.CheckOnClick = true;
            OligosaccharideCheckedListBox.FormattingEnabled = true;
            OligosaccharideCheckedListBox.Location = new Point(619, 158);
            OligosaccharideCheckedListBox.Margin = new Padding(4, 3, 4, 3);
            OligosaccharideCheckedListBox.Name = "OligosaccharideCheckedListBox";
            OligosaccharideCheckedListBox.Size = new Size(278, 346);
            OligosaccharideCheckedListBox.Items.AddRange(OligoPos);
            OligosaccharideCheckedListBox.TabIndex = 3;
            OligosaccharideCheckedListBox.SelectedIndexChanged += OligosaccharideCheckedListBox_SelectedIndexChanged;
            // 
            // HexNAc_ions
            // 
            HexNAc_ions.AutoSize = true;
            HexNAc_ions.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            HexNAc_ions.Location = new Point(16, 126);
            HexNAc_ions.Margin = new Padding(4, 0, 4, 0);
            HexNAc_ions.Name = "HexNAc_ions";
            HexNAc_ions.Size = new Size(87, 17);
            HexNAc_ions.TabIndex = 14;
            HexNAc_ions.Text = "HexNAc ions";
            // 
            // Hex_ions
            // 
            Hex_ions.AutoSize = true;
            Hex_ions.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Hex_ions.Location = new Point(16, 335);
            Hex_ions.Margin = new Padding(4, 0, 4, 0);
            Hex_ions.Name = "Hex_ions";
            Hex_ions.Size = new Size(62, 17);
            Hex_ions.TabIndex = 15;
            Hex_ions.Text = "Hex ions";
            // 
            // Sia_ions
            // 
            Sia_ions.AutoSize = true;
            Sia_ions.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Sia_ions.Location = new Point(315, 126);
            Sia_ions.Margin = new Padding(4, 0, 4, 0);
            Sia_ions.Name = "Sia_ions";
            Sia_ions.Size = new Size(101, 17);
            Sia_ions.TabIndex = 16;
            Sia_ions.Text = "Sialic Acid ions";
            // 
            // Oligosaccharide_ions
            // 
            Oligosaccharide_ions.AutoSize = true;
            Oligosaccharide_ions.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Oligosaccharide_ions.Location = new Point(619, 126);
            Oligosaccharide_ions.Margin = new Padding(4, 0, 4, 0);
            Oligosaccharide_ions.Name = "Oligosaccharide_ions";
            Oligosaccharide_ions.Size = new Size(136, 17);
            Oligosaccharide_ions.TabIndex = 17;
            Oligosaccharide_ions.Text = "Oligosaccharide ions";
            // 
            // M6PCheckedListBox
            // 
            M6PCheckedListBox.CheckOnClick = true;
            M6PCheckedListBox.FormattingEnabled = true;
            M6PCheckedListBox.Location = new Point(16, 551);
            M6PCheckedListBox.Margin = new Padding(4, 3, 4, 3);
            M6PCheckedListBox.Name = "M6PCheckedListBox";
            M6PCheckedListBox.Size = new Size(276, 40);
            M6PCheckedListBox.Items.AddRange(ManPos);
            M6PCheckedListBox.TabIndex = 5;
            M6PCheckedListBox.SelectedIndexChanged += M6PCheckedListBox_SelectedIndexChanged;
            // 
            // M6P_ions
            // 
            M6P_ions.AutoSize = true;
            M6P_ions.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            M6P_ions.Location = new Point(16, 520);
            M6P_ions.Margin = new Padding(4, 0, 4, 0);
            M6P_ions.Name = "M6P_ions";
            M6P_ions.Size = new Size(65, 17);
            M6P_ions.TabIndex = 19;
            M6P_ions.Text = "M6P ions";
            // 
            // StartButton
            // 
            StartButton.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            StartButton.Location = new Point(619, 542);
            StartButton.Margin = new Padding(4, 3, 4, 3);
            StartButton.Name = "StartButton";
            StartButton.Size = new Size(276, 62);
            StartButton.TabIndex = 0;
            StartButton.Text = "Start";
            StartButton.UseVisualStyleBackColor = true;
            StartButton.Click += StartButton_Click;
            // 
            // ClearButton
            // 
            ClearButton.Location = new Point(619, 76);
            ClearButton.Margin = new Padding(4, 3, 4, 3);
            ClearButton.Name = "ClearButton";
            ClearButton.Size = new Size(278, 36);
            ClearButton.TabIndex = 0;
            ClearButton.Text = "Clear Selections";
            ClearButton.UseVisualStyleBackColor = true;
            ClearButton.Click += ClearButton_Click;
            // 
            // ppmTol_textBox
            // 
            ppmTol_textBox.Location = new Point(919, 109);
            ppmTol_textBox.Margin = new Padding(4, 3, 4, 3);
            ppmTol_textBox.Name = "ppmTol_textBox";
            ppmTol_textBox.Size = new Size(61, 23);
            ppmTol_textBox.TabIndex = 20;
            ppmTol_textBox.Text = "15";
            // 
            // ppmTol_label
            // 
            ppmTol_label.AutoSize = true;
            ppmTol_label.Location = new Point(981, 112);
            ppmTol_label.Margin = new Padding(4, 0, 4, 0);
            ppmTol_label.Name = "ppmTol_label";
            ppmTol_label.Size = new Size(145, 15);
            ppmTol_label.TabIndex = 21;
            ppmTol_label.Text = "Tolerance (default = ppm)";
            // 
            // CheckAll_Button
            // 
            CheckAll_Button.Location = new Point(16, 76);
            CheckAll_Button.Margin = new Padding(4, 3, 4, 3);
            CheckAll_Button.Name = "CheckAll_Button";
            CheckAll_Button.Size = new Size(279, 36);
            CheckAll_Button.TabIndex = 22;
            CheckAll_Button.Text = "Check All Ions";
            CheckAll_Button.UseVisualStyleBackColor = true;
            CheckAll_Button.Click += CheckAll_Button_Click;
            // 
            // CheckAll_Hex_Button
            // 
            CheckAll_Hex_Button.Location = new Point(136, 325);
            CheckAll_Hex_Button.Margin = new Padding(2);
            CheckAll_Hex_Button.Name = "CheckAll_Hex_Button";
            CheckAll_Hex_Button.Size = new Size(156, 36);
            CheckAll_Hex_Button.TabIndex = 23;
            CheckAll_Hex_Button.Text = "Check all Hex ions";
            CheckAll_Hex_Button.UseVisualStyleBackColor = true;
            CheckAll_Hex_Button.Click += CheckAll_Hex_Button_Click;
            // 
            // CheckAll_HexNAc_Button
            // 
            CheckAll_HexNAc_Button.Location = new Point(136, 117);
            CheckAll_HexNAc_Button.Margin = new Padding(2);
            CheckAll_HexNAc_Button.Name = "CheckAll_HexNAc_Button";
            CheckAll_HexNAc_Button.Size = new Size(156, 36);
            CheckAll_HexNAc_Button.TabIndex = 24;
            CheckAll_HexNAc_Button.Text = "Check all HexNAc ions";
            CheckAll_HexNAc_Button.UseVisualStyleBackColor = true;
            CheckAll_HexNAc_Button.Click += CheckAll_HexNAc_Button_Click;
            // 
            // CheckAll_Sialic_Button
            // 
            CheckAll_Sialic_Button.Location = new Point(449, 116);
            CheckAll_Sialic_Button.Margin = new Padding(2);
            CheckAll_Sialic_Button.Name = "CheckAll_Sialic_Button";
            CheckAll_Sialic_Button.Size = new Size(142, 36);
            CheckAll_Sialic_Button.TabIndex = 25;
            CheckAll_Sialic_Button.Text = "Check all Sia ions";
            CheckAll_Sialic_Button.UseVisualStyleBackColor = true;
            CheckAll_Sialic_Button.Click += CheckAll_Sialic_Button_Click;
            // 
            // CheckAll_M6P_Button
            // 
            CheckAll_M6P_Button.Location = new Point(136, 509);
            CheckAll_M6P_Button.Margin = new Padding(2);
            CheckAll_M6P_Button.Name = "CheckAll_M6P_Button";
            CheckAll_M6P_Button.Size = new Size(156, 36);
            CheckAll_M6P_Button.TabIndex = 26;
            CheckAll_M6P_Button.Text = "Check all M6P ions";
            CheckAll_M6P_Button.UseVisualStyleBackColor = true;
            CheckAll_M6P_Button.Click += CheckAll_M6P_Button_Click;
            // 
            // CheckAll_Oligo_Button
            // 
            CheckAll_Oligo_Button.Location = new Point(755, 116);
            CheckAll_Oligo_Button.Margin = new Padding(2);
            CheckAll_Oligo_Button.Name = "CheckAll_Oligo_Button";
            CheckAll_Oligo_Button.Size = new Size(142, 36);
            CheckAll_Oligo_Button.TabIndex = 27;
            CheckAll_Oligo_Button.Text = "Check all Oligo ions";
            CheckAll_Oligo_Button.UseVisualStyleBackColor = true;
            CheckAll_Oligo_Button.Click += CheckAll_Oligo_Button_Click;
            // 
            // SN_textBox
            // 
            SN_textBox.Location = new Point(919, 137);
            SN_textBox.Margin = new Padding(2);
            SN_textBox.Name = "SN_textBox";
            SN_textBox.Size = new Size(61, 23);
            SN_textBox.TabIndex = 28;
            SN_textBox.Text = "3";
            // 
            // SN_label
            // 
            SN_label.AutoSize = true;
            SN_label.Location = new Point(982, 140);
            SN_label.Margin = new Padding(2, 0, 2, 0);
            SN_label.Name = "SN_label";
            SN_label.Size = new Size(161, 15);
            SN_label.TabIndex = 29;
            SN_label.Text = "Signal-to-Noise Requirement";
            // 
            // PeakDepth_Box_HCD
            // 
            PeakDepth_Box_HCD.Location = new Point(919, 221);
            PeakDepth_Box_HCD.Margin = new Padding(2);
            PeakDepth_Box_HCD.Name = "PeakDepth_Box_HCD";
            PeakDepth_Box_HCD.Size = new Size(61, 23);
            PeakDepth_Box_HCD.TabIndex = 30;
            PeakDepth_Box_HCD.Text = "25";
            // 
            // PeakDepth_label_HCD
            // 
            PeakDepth_label_HCD.AutoSize = true;
            PeakDepth_label_HCD.Location = new Point(981, 224);
            PeakDepth_label_HCD.Margin = new Padding(2, 0, 2, 0);
            PeakDepth_label_HCD.Name = "PeakDepth_label_HCD";
            PeakDepth_label_HCD.Size = new Size(202, 15);
            PeakDepth_label_HCD.TabIndex = 31;
            PeakDepth_label_HCD.Text = "Must be within N most intense peaks";
            // 
            // hcdTICfraction
            // 
            hcdTICfraction.Location = new Point(919, 250);
            hcdTICfraction.Margin = new Padding(4, 3, 4, 3);
            hcdTICfraction.Name = "hcdTICfraction";
            hcdTICfraction.Size = new Size(61, 23);
            hcdTICfraction.TabIndex = 32;
            hcdTICfraction.Text = "0.20";
            // 
            // etdTICfraction
            // 
            etdTICfraction.Location = new Point(919, 375);
            etdTICfraction.Margin = new Padding(4, 3, 4, 3);
            etdTICfraction.Name = "etdTICfraction";
            etdTICfraction.Size = new Size(61, 23);
            etdTICfraction.TabIndex = 33;
            etdTICfraction.Text = "0.05";
            // 
            // hcdTICfraction_Label
            // 
            hcdTICfraction_Label.AutoSize = true;
            hcdTICfraction_Label.Location = new Point(981, 253);
            hcdTICfraction_Label.Margin = new Padding(4, 0, 4, 0);
            hcdTICfraction_Label.Name = "hcdTICfraction_Label";
            hcdTICfraction_Label.Size = new Size(97, 15);
            hcdTICfraction_Label.TabIndex = 34;
            hcdTICfraction_Label.Text = "HCD TIC fraction";
            // 
            // etdTICfraction_Label
            // 
            etdTICfraction_Label.AutoSize = true;
            etdTICfraction_Label.Location = new Point(982, 378);
            etdTICfraction_Label.Margin = new Padding(4, 0, 4, 0);
            etdTICfraction_Label.Name = "etdTICfraction_Label";
            etdTICfraction_Label.Size = new Size(93, 15);
            etdTICfraction_Label.TabIndex = 35;
            etdTICfraction_Label.Text = "ETD TIC fraction";
            // 
            // PeakDepth_Box_ETD
            // 
            PeakDepth_Box_ETD.Location = new Point(919, 345);
            PeakDepth_Box_ETD.Margin = new Padding(4, 3, 4, 3);
            PeakDepth_Box_ETD.Name = "PeakDepth_Box_ETD";
            PeakDepth_Box_ETD.Size = new Size(61, 23);
            PeakDepth_Box_ETD.TabIndex = 36;
            PeakDepth_Box_ETD.Text = "50";
            // 
            // HCDsettingLabel
            // 
            HCDsettingLabel.AutoSize = true;
            HCDsettingLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 0);
            HCDsettingLabel.Location = new Point(919, 198);
            HCDsettingLabel.Margin = new Padding(4, 0, 4, 0);
            HCDsettingLabel.Name = "HCDsettingLabel";
            HCDsettingLabel.Size = new Size(168, 16);
            HCDsettingLabel.TabIndex = 37;
            HCDsettingLabel.Text = "HCD MS/MS Scan Settings";
            // 
            // ETDsettingsLabel
            // 
            ETDsettingsLabel.AutoSize = true;
            ETDsettingsLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 0);
            ETDsettingsLabel.Location = new Point(919, 323);
            ETDsettingsLabel.Margin = new Padding(4, 0, 4, 0);
            ETDsettingsLabel.Name = "ETDsettingsLabel";
            ETDsettingsLabel.Size = new Size(167, 16);
            ETDsettingsLabel.TabIndex = 38;
            ETDsettingsLabel.Text = "ETD MS/MS Scan Settings";
            // 
            // PeakDepth_label_ETD
            // 
            PeakDepth_label_ETD.AutoSize = true;
            PeakDepth_label_ETD.Location = new Point(982, 348);
            PeakDepth_label_ETD.Margin = new Padding(2, 0, 2, 0);
            PeakDepth_label_ETD.Name = "PeakDepth_label_ETD";
            PeakDepth_label_ETD.Size = new Size(202, 15);
            PeakDepth_label_ETD.TabIndex = 39;
            PeakDepth_label_ETD.Text = "Must be within N most intense peaks";
            // 
            // MostCommonButton
            // 
            MostCommonButton.Location = new Point(314, 76);
            MostCommonButton.Margin = new Padding(4, 3, 4, 3);
            MostCommonButton.Name = "MostCommonButton";
            MostCommonButton.Size = new Size(278, 36);
            MostCommonButton.TabIndex = 40;
            MostCommonButton.Text = "Check Common Ions";
            MostCommonButton.UseVisualStyleBackColor = true;
            MostCommonButton.Click += MostCommonButton_Click;
            // 
            // FucoseCheckedListBox
            // 
            FucoseCheckedListBox.CheckOnClick = true;
            FucoseCheckedListBox.FormattingEnabled = true;
            FucoseCheckedListBox.Location = new Point(314, 367);
            FucoseCheckedListBox.Margin = new Padding(4, 3, 4, 3);
            FucoseCheckedListBox.Name = "FucoseCheckedListBox";
            FucoseCheckedListBox.Size = new Size(278, 94);
            FucoseCheckedListBox.Items.AddRange(FucosePos);
            FucoseCheckedListBox.TabIndex = 41;
            FucoseCheckedListBox.SelectedIndexChanged += FucoseCheckedListBox_SelectedIndexChanged;
            // 
            // Fucose_ions_label
            // 
            Fucose_ions_label.AutoSize = true;
            Fucose_ions_label.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Fucose_ions_label.Location = new Point(314, 334);
            Fucose_ions_label.Margin = new Padding(4, 0, 4, 0);
            Fucose_ions_label.Name = "Fucose_ions_label";
            Fucose_ions_label.Size = new Size(131, 17);
            Fucose_ions_label.TabIndex = 42;
            Fucose_ions_label.Text = "Fucose-specific ions";
            // 
            // CheckAll_Fucose_Button
            // 
            CheckAll_Fucose_Button.Location = new Point(453, 325);
            CheckAll_Fucose_Button.Margin = new Padding(4, 3, 4, 3);
            CheckAll_Fucose_Button.Name = "CheckAll_Fucose_Button";
            CheckAll_Fucose_Button.Size = new Size(140, 36);
            CheckAll_Fucose_Button.TabIndex = 43;
            CheckAll_Fucose_Button.Text = "Check all Fucose ions";
            CheckAll_Fucose_Button.UseVisualStyleBackColor = true;
            CheckAll_Fucose_Button.Click += CheckAll_Fucose_Button_Click;
            // 
            // OxoCountThreshold_hcd_label
            // 
            OxoCountThreshold_hcd_label.AutoSize = true;
            OxoCountThreshold_hcd_label.Location = new Point(981, 281);
            OxoCountThreshold_hcd_label.Margin = new Padding(2, 0, 2, 0);
            OxoCountThreshold_hcd_label.Name = "OxoCountThreshold_hcd_label";
            OxoCountThreshold_hcd_label.Size = new Size(163, 30);
            OxoCountThreshold_hcd_label.TabIndex = 45;
            OxoCountThreshold_hcd_label.Text = "Oxonium Count Requirement\r\n0 = default";
            // 
            // OxoCountRequireBox_hcd
            // 
            OxoCountRequireBox_hcd.Location = new Point(919, 281);
            OxoCountRequireBox_hcd.Margin = new Padding(2);
            OxoCountRequireBox_hcd.Name = "OxoCountRequireBox_hcd";
            OxoCountRequireBox_hcd.Size = new Size(61, 23);
            OxoCountRequireBox_hcd.TabIndex = 44;
            OxoCountRequireBox_hcd.Text = "0";
            // 
            // OxoCountThreshold_etd_label
            // 
            OxoCountThreshold_etd_label.AutoSize = true;
            OxoCountThreshold_etd_label.Location = new Point(982, 406);
            OxoCountThreshold_etd_label.Margin = new Padding(2, 0, 2, 0);
            OxoCountThreshold_etd_label.Name = "OxoCountThreshold_etd_label";
            OxoCountThreshold_etd_label.Size = new Size(163, 30);
            OxoCountThreshold_etd_label.TabIndex = 46;
            OxoCountThreshold_etd_label.Text = "Oxonium Count Requirement\r\n0 = default";
            // 
            // OxoCountRequireBox_etd
            // 
            OxoCountRequireBox_etd.Location = new Point(919, 406);
            OxoCountRequireBox_etd.Margin = new Padding(2);
            OxoCountRequireBox_etd.Name = "OxoCountRequireBox_etd";
            OxoCountRequireBox_etd.Size = new Size(62, 23);
            OxoCountRequireBox_etd.TabIndex = 47;
            OxoCountRequireBox_etd.Text = "0";
            // 
            // uploadCustomTextBox
            // 
            uploadCustomTextBox.Location = new Point(16, 663);
            uploadCustomTextBox.Margin = new Padding(4, 3, 4, 3);
            uploadCustomTextBox.Multiline = true;
            uploadCustomTextBox.Name = "uploadCustomTextBox";
            uploadCustomTextBox.Size = new Size(1085, 25);
            uploadCustomTextBox.TabIndex = 48;
            uploadCustomTextBox.Text = "Upload custom ions here - csv with headers \"m/z\" and \"Description\"";
            uploadCustomTextBox.TextChanged += uploadCustomTextBox_TextChanged_1;
            // 
            // UploadCustomBrowseButton
            // 
            UploadCustomBrowseButton.Location = new Point(1106, 663);
            UploadCustomBrowseButton.Margin = new Padding(4, 3, 4, 3);
            UploadCustomBrowseButton.Name = "UploadCustomBrowseButton";
            UploadCustomBrowseButton.Size = new Size(88, 25);
            UploadCustomBrowseButton.TabIndex = 49;
            UploadCustomBrowseButton.Text = "Browse";
            UploadCustomBrowseButton.UseVisualStyleBackColor = true;
            UploadCustomBrowseButton.Click += UploadCustomBrowseButton_Click;
            // 
            // StatusLabel
            // 
            StatusLabel.AutoSize = true;
            StatusLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            StatusLabel.Location = new Point(16, 637);
            StatusLabel.Margin = new Padding(4, 0, 4, 0);
            StatusLabel.Name = "StatusLabel";
            StatusLabel.Size = new Size(225, 16);
            StatusLabel.TabIndex = 50;
            StatusLabel.Text = "Status updates will appear here";
            // 
            // FinishTimeLabel
            // 
            FinishTimeLabel.AutoSize = true;
            FinishTimeLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FinishTimeLabel.Location = new Point(316, 607);
            FinishTimeLabel.Margin = new Padding(4, 0, 4, 0);
            FinishTimeLabel.Name = "FinishTimeLabel";
            FinishTimeLabel.Size = new Size(148, 17);
            FinishTimeLabel.TabIndex = 51;
            FinishTimeLabel.Text = "Finish Time: Not Yet Run";
            // 
            // StartTimeLabel
            // 
            StartTimeLabel.AutoSize = true;
            StartTimeLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            StartTimeLabel.Location = new Point(316, 587);
            StartTimeLabel.Name = "StartTimeLabel";
            StartTimeLabel.Size = new Size(143, 17);
            StartTimeLabel.TabIndex = 52;
            StartTimeLabel.Text = "Start Time: Not Yet Run";
            // 
            // GlyCounterLogo
            // 
            GlyCounterLogo.Image = (Image)resources.GetObject("GlyCounterLogo.Image");
            GlyCounterLogo.Location = new Point(314, 509);
            GlyCounterLogo.Name = "GlyCounterLogo";
            GlyCounterLogo.Size = new Size(276, 71);
            GlyCounterLogo.TabIndex = 53;
            GlyCounterLogo.TabStop = false;
            // 
            // GlyCounter_AllTabs
            // 
            GlyCounter_AllTabs.Controls.Add(GlyCounter_Tab);
            GlyCounter_AllTabs.Controls.Add(YnaughtTab);
            GlyCounter_AllTabs.Location = new Point(13, 13);
            GlyCounter_AllTabs.Name = "GlyCounter_AllTabs";
            GlyCounter_AllTabs.SelectedIndex = 0;
            GlyCounter_AllTabs.Size = new Size(1209, 736);
            GlyCounter_AllTabs.TabIndex = 54;
            // 
            // GlyCounter_Tab
            // 
            GlyCounter_Tab.Controls.Add(ignoreMSLevelCB);
            GlyCounter_Tab.Controls.Add(label3);
            GlyCounter_Tab.Controls.Add(label2);
            GlyCounter_Tab.Controls.Add(MSLevelUB);
            GlyCounter_Tab.Controls.Add(MSLevelLB);
            GlyCounter_Tab.Controls.Add(polarityCB);
            GlyCounter_Tab.Controls.Add(Gly_outputButton);
            GlyCounter_Tab.Controls.Add(Gly_outputTextBox);
            GlyCounter_Tab.Controls.Add(ipsaCheckBox);
            GlyCounter_Tab.Controls.Add(DaltonCheckBox);
            GlyCounter_Tab.Controls.Add(OxoCountThreshold_uvpd_label);
            GlyCounter_Tab.Controls.Add(uvpdTICfraction_Label);
            GlyCounter_Tab.Controls.Add(PeakDepth_label_UVPD);
            GlyCounter_Tab.Controls.Add(OxoCountRequireBox_uvpd);
            GlyCounter_Tab.Controls.Add(uvpdTICfraction);
            GlyCounter_Tab.Controls.Add(PeakDepth_Box_UVPD);
            GlyCounter_Tab.Controls.Add(UVPDsettingslabel);
            GlyCounter_Tab.Controls.Add(label1);
            GlyCounter_Tab.Controls.Add(intensityThresholdLabel);
            GlyCounter_Tab.Controls.Add(intensityThresholdTextBox);
            GlyCounter_Tab.Controls.Add(textBox1);
            GlyCounter_Tab.Controls.Add(hcdTICfraction_Label);
            GlyCounter_Tab.Controls.Add(OxoCountThreshold_hcd_label);
            GlyCounter_Tab.Controls.Add(hcdTICfraction);
            GlyCounter_Tab.Controls.Add(OxoCountThreshold_etd_label);
            GlyCounter_Tab.Controls.Add(PeakDepth_label_HCD);
            GlyCounter_Tab.Controls.Add(OxoCountRequireBox_hcd);
            GlyCounter_Tab.Controls.Add(OxoCountRequireBox_etd);
            GlyCounter_Tab.Controls.Add(HCDsettingLabel);
            GlyCounter_Tab.Controls.Add(Fucose_ions_label);
            GlyCounter_Tab.Controls.Add(CheckAll_Fucose_Button);
            GlyCounter_Tab.Controls.Add(PeakDepth_Box_HCD);
            GlyCounter_Tab.Controls.Add(FucoseCheckedListBox);
            GlyCounter_Tab.Controls.Add(UploadCustomBrowseButton);
            GlyCounter_Tab.Controls.Add(StatusLabel);
            GlyCounter_Tab.Controls.Add(SN_label);
            GlyCounter_Tab.Controls.Add(uploadCustomTextBox);
            GlyCounter_Tab.Controls.Add(SN_textBox);
            GlyCounter_Tab.Controls.Add(FinishTimeLabel);
            GlyCounter_Tab.Controls.Add(ppmTol_label);
            GlyCounter_Tab.Controls.Add(StartTimeLabel);
            GlyCounter_Tab.Controls.Add(ppmTol_textBox);
            GlyCounter_Tab.Controls.Add(GlyCounterLogo);
            GlyCounter_Tab.Controls.Add(browseButton);
            GlyCounter_Tab.Controls.Add(PeakDepth_label_ETD);
            GlyCounter_Tab.Controls.Add(etdTICfraction_Label);
            GlyCounter_Tab.Controls.Add(HexCheckedListBox);
            GlyCounter_Tab.Controls.Add(ETDsettingsLabel);
            GlyCounter_Tab.Controls.Add(etdTICfraction);
            GlyCounter_Tab.Controls.Add(PeakDepth_Box_ETD);
            GlyCounter_Tab.Controls.Add(M6PCheckedListBox);
            GlyCounter_Tab.Controls.Add(CheckAll_M6P_Button);
            GlyCounter_Tab.Controls.Add(M6P_ions);
            GlyCounter_Tab.Controls.Add(OligosaccharideCheckedListBox);
            GlyCounter_Tab.Controls.Add(CheckAll_Oligo_Button);
            GlyCounter_Tab.Controls.Add(CheckAll_Button);
            GlyCounter_Tab.Controls.Add(MostCommonButton);
            GlyCounter_Tab.Controls.Add(ClearButton);
            GlyCounter_Tab.Controls.Add(HexNAc_ions);
            GlyCounter_Tab.Controls.Add(CheckAll_HexNAc_Button);
            GlyCounter_Tab.Controls.Add(CheckAll_Hex_Button);
            GlyCounter_Tab.Controls.Add(Hex_ions);
            GlyCounter_Tab.Controls.Add(SialicAcidCheckedListBox);
            GlyCounter_Tab.Controls.Add(Sia_ions);
            GlyCounter_Tab.Controls.Add(CheckAll_Sialic_Button);
            GlyCounter_Tab.Controls.Add(Oligosaccharide_ions);
            GlyCounter_Tab.Controls.Add(StartButton);
            GlyCounter_Tab.Controls.Add(HexNAcCheckedListBox);
            GlyCounter_Tab.Location = new Point(4, 24);
            GlyCounter_Tab.Name = "GlyCounter_Tab";
            GlyCounter_Tab.Padding = new Padding(3);
            GlyCounter_Tab.Size = new Size(1201, 708);
            GlyCounter_Tab.TabIndex = 0;
            GlyCounter_Tab.Text = "Pre-ID";
            GlyCounter_Tab.UseVisualStyleBackColor = true;
            // 
            // ignoreMSLevelCB
            // 
            ignoreMSLevelCB.AutoSize = true;
            ignoreMSLevelCB.Location = new Point(962, 629);
            ignoreMSLevelCB.Name = "ignoreMSLevelCB";
            ignoreMSLevelCB.Size = new Size(221, 19);
            ignoreMSLevelCB.TabIndex = 75;
            ignoreMSLevelCB.Text = "Ignore MS Level and Search All Scans";
            ignoreMSLevelCB.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(1012, 602);
            label3.Name = "label3";
            label3.Size = new Size(18, 15);
            label3.TabIndex = 74;
            label3.Text = "to";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(962, 576);
            label2.Name = "label2";
            label2.Size = new Size(111, 15);
            label2.TabIndex = 73;
            label2.Text = "MS Levels to Search";
            // 
            // MSLevelUB
            // 
            MSLevelUB.Location = new Point(1036, 600);
            MSLevelUB.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            MSLevelUB.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            MSLevelUB.Name = "MSLevelUB";
            MSLevelUB.Size = new Size(44, 23);
            MSLevelUB.TabIndex = 72;
            MSLevelUB.TextAlign = HorizontalAlignment.Center;
            MSLevelUB.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // MSLevelLB
            // 
            MSLevelLB.Location = new Point(962, 600);
            MSLevelLB.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            MSLevelLB.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            MSLevelLB.Name = "MSLevelLB";
            MSLevelLB.Size = new Size(44, 23);
            MSLevelLB.TabIndex = 71;
            MSLevelLB.TextAlign = HorizontalAlignment.Center;
            MSLevelLB.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // polarityCB
            // 
            polarityCB.AutoSize = true;
            polarityCB.Location = new Point(783, 612);
            polarityCB.Name = "polarityCB";
            polarityCB.Size = new Size(146, 19);
            polarityCB.TabIndex = 70;
            polarityCB.Text = "Toggle Negative Mode";
            polarityCB.UseVisualStyleBackColor = true;
            polarityCB.CheckedChanged += polarityCB_CheckedChanged;
            // 
            // Gly_outputButton
            // 
            Gly_outputButton.Location = new Point(1109, 49);
            Gly_outputButton.Name = "Gly_outputButton";
            Gly_outputButton.Size = new Size(88, 23);
            Gly_outputButton.TabIndex = 69;
            Gly_outputButton.Text = "Browse";
            Gly_outputButton.UseVisualStyleBackColor = true;
            Gly_outputButton.Click += Gly_outputButton_Click;
            // 
            // Gly_outputTextBox
            // 
            Gly_outputTextBox.Location = new Point(16, 47);
            Gly_outputTextBox.Name = "Gly_outputTextBox";
            Gly_outputTextBox.Size = new Size(1085, 23);
            Gly_outputTextBox.TabIndex = 55;
            Gly_outputTextBox.Text = "Select output directory";
            // 
            // ipsaCheckBox
            // 
            ipsaCheckBox.AutoSize = true;
            ipsaCheckBox.Location = new Point(618, 612);
            ipsaCheckBox.Name = "ipsaCheckBox";
            ipsaCheckBox.Size = new Size(159, 19);
            ipsaCheckBox.TabIndex = 68;
            ipsaCheckBox.Text = "Output IPSA Annotations";
            ipsaCheckBox.UseVisualStyleBackColor = true;
            ipsaCheckBox.CheckedChanged += ipsaCheckBox_CheckedChanged;
            // 
            // DaltonCheckBox
            // 
            DaltonCheckBox.AutoSize = true;
            DaltonCheckBox.Location = new Point(1132, 112);
            DaltonCheckBox.Name = "DaltonCheckBox";
            DaltonCheckBox.Size = new Size(40, 19);
            DaltonCheckBox.TabIndex = 67;
            DaltonCheckBox.Text = "Da";
            DaltonCheckBox.UseVisualStyleBackColor = true;
            DaltonCheckBox.CheckedChanged += DaltonCheckBox_CheckedChanged;
            // 
            // OxoCountThreshold_uvpd_label
            // 
            OxoCountThreshold_uvpd_label.AutoSize = true;
            OxoCountThreshold_uvpd_label.Location = new Point(982, 522);
            OxoCountThreshold_uvpd_label.Margin = new Padding(2, 0, 2, 0);
            OxoCountThreshold_uvpd_label.Name = "OxoCountThreshold_uvpd_label";
            OxoCountThreshold_uvpd_label.Size = new Size(163, 30);
            OxoCountThreshold_uvpd_label.TabIndex = 64;
            OxoCountThreshold_uvpd_label.Text = "Oxonium Count Requirement\r\n0 = default";
            // 
            // uvpdTICfraction_Label
            // 
            uvpdTICfraction_Label.AutoSize = true;
            uvpdTICfraction_Label.Location = new Point(981, 501);
            uvpdTICfraction_Label.Margin = new Padding(4, 0, 4, 0);
            uvpdTICfraction_Label.Name = "uvpdTICfraction_Label";
            uvpdTICfraction_Label.Size = new Size(102, 15);
            uvpdTICfraction_Label.TabIndex = 63;
            uvpdTICfraction_Label.Text = "UVPD TIC fraction";
            // 
            // PeakDepth_label_UVPD
            // 
            PeakDepth_label_UVPD.AutoSize = true;
            PeakDepth_label_UVPD.Location = new Point(982, 472);
            PeakDepth_label_UVPD.Margin = new Padding(2, 0, 2, 0);
            PeakDepth_label_UVPD.Name = "PeakDepth_label_UVPD";
            PeakDepth_label_UVPD.Size = new Size(202, 15);
            PeakDepth_label_UVPD.TabIndex = 62;
            PeakDepth_label_UVPD.Text = "Must be within N most intense peaks";
            // 
            // OxoCountRequireBox_uvpd
            // 
            OxoCountRequireBox_uvpd.Location = new Point(918, 527);
            OxoCountRequireBox_uvpd.Margin = new Padding(4, 3, 4, 3);
            OxoCountRequireBox_uvpd.Name = "OxoCountRequireBox_uvpd";
            OxoCountRequireBox_uvpd.Size = new Size(61, 23);
            OxoCountRequireBox_uvpd.TabIndex = 61;
            OxoCountRequireBox_uvpd.Text = "0";
            // 
            // uvpdTICfraction
            // 
            uvpdTICfraction.Location = new Point(918, 498);
            uvpdTICfraction.Margin = new Padding(4, 3, 4, 3);
            uvpdTICfraction.Name = "uvpdTICfraction";
            uvpdTICfraction.Size = new Size(61, 23);
            uvpdTICfraction.TabIndex = 60;
            uvpdTICfraction.Text = "0.20";
            // 
            // PeakDepth_Box_UVPD
            // 
            PeakDepth_Box_UVPD.Location = new Point(918, 469);
            PeakDepth_Box_UVPD.Margin = new Padding(4, 3, 4, 3);
            PeakDepth_Box_UVPD.Name = "PeakDepth_Box_UVPD";
            PeakDepth_Box_UVPD.Size = new Size(61, 23);
            PeakDepth_Box_UVPD.TabIndex = 59;
            PeakDepth_Box_UVPD.Text = "25";
            // 
            // UVPDsettingslabel
            // 
            UVPDsettingslabel.AutoSize = true;
            UVPDsettingslabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 0);
            UVPDsettingslabel.Location = new Point(919, 445);
            UVPDsettingslabel.Margin = new Padding(4, 0, 4, 0);
            UVPDsettingslabel.Name = "UVPDsettingslabel";
            UVPDsettingslabel.Size = new Size(177, 16);
            UVPDsettingslabel.TabIndex = 58;
            UVPDsettingslabel.Text = "UVPD MS/MS Scan Settings";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(981, 175);
            label1.Name = "label1";
            label1.Size = new Size(213, 15);
            label1.TabIndex = 57;
            label1.Text = "used if mass analyzer does not have SN";
            // 
            // intensityThresholdLabel
            // 
            intensityThresholdLabel.AutoSize = true;
            intensityThresholdLabel.Location = new Point(982, 160);
            intensityThresholdLabel.Name = "intensityThresholdLabel";
            intensityThresholdLabel.Size = new Size(108, 15);
            intensityThresholdLabel.TabIndex = 56;
            intensityThresholdLabel.Text = "Intensity Threshold";
            // 
            // intensityThresholdTextBox
            // 
            intensityThresholdTextBox.Location = new Point(919, 167);
            intensityThresholdTextBox.Name = "intensityThresholdTextBox";
            intensityThresholdTextBox.Size = new Size(61, 23);
            intensityThresholdTextBox.TabIndex = 55;
            intensityThresholdTextBox.Text = "1000";
            // 
            // YnaughtTab
            // 
            YnaughtTab.Controls.Add(Ynaught_intLabel);
            YnaughtTab.Controls.Add(Ynaught_intTextBox);
            YnaughtTab.Controls.Add(Ynaught_outputButton);
            YnaughtTab.Controls.Add(Ynaught_outputTextBox);
            YnaughtTab.Controls.Add(YNaught_IPSAcheckbox);
            YnaughtTab.Controls.Add(ChargeExplanationLabel);
            YnaughtTab.Controls.Add(UpperBoundLabel);
            YnaughtTab.Controls.Add(LowerBoundLabel);
            YnaughtTab.Controls.Add(dashLabel);
            YnaughtTab.Controls.Add(UpperBoundTextBox);
            YnaughtTab.Controls.Add(LowerBoundTextBox);
            YnaughtTab.Controls.Add(panel1);
            YnaughtTab.Controls.Add(Ynaught_DaCheckBox);
            YnaughtTab.Controls.Add(Ynaught_GlyCounterLogo);
            YnaughtTab.Controls.Add(Ynaught_FinishTimeLabel);
            YnaughtTab.Controls.Add(Ynaught_startTimeLabel);
            YnaughtTab.Controls.Add(Ynaught_StartButton);
            YnaughtTab.Controls.Add(NeutralLosses_Label);
            YnaughtTab.Controls.Add(ClearAllSelections_Button);
            YnaughtTab.Controls.Add(Ynaught_SNlabel);
            YnaughtTab.Controls.Add(Ynaught_SNthresholdTextBox);
            YnaughtTab.Controls.Add(CommonOglyco_Label);
            YnaughtTab.Controls.Add(FucoseYions_Label);
            YnaughtTab.Controls.Add(CommonNglycoLabel);
            YnaughtTab.Controls.Add(CheckAllOglyco_Button);
            YnaughtTab.Controls.Add(CheckAllNeutralLosses_Button);
            YnaughtTab.Controls.Add(CheckAllFucose_Button);
            YnaughtTab.Controls.Add(CheckAllNglyco_Button);
            YnaughtTab.Controls.Add(Ynaught_ppmTolLabel);
            YnaughtTab.Controls.Add(Ynaught_ppmTolTextBox);
            YnaughtTab.Controls.Add(Yions_CheckAllButton);
            YnaughtTab.Controls.Add(Ynaught_ChargeStatesHeader);
            YnaughtTab.Controls.Add(SecondIsotopeCheckBox);
            YnaughtTab.Controls.Add(MonoisotopeLabel2);
            YnaughtTab.Controls.Add(MonoisotopeLabel1);
            YnaughtTab.Controls.Add(Yions_CheckNglycoSialylButton);
            YnaughtTab.Controls.Add(Yions_CheckNglycoFucoseButton);
            YnaughtTab.Controls.Add(BrowseCustomSubtractions_Button);
            YnaughtTab.Controls.Add(BrowseCustomAdditions_Button);
            YnaughtTab.Controls.Add(Ynaught_CustomSubtractions_TextBox);
            YnaughtTab.Controls.Add(Ynaught_CustomAdditions_TextBox);
            YnaughtTab.Controls.Add(LoadInGlycoPepRawFile_TextBox);
            YnaughtTab.Controls.Add(BrowseGlycoPepRawFiles_Button);
            YnaughtTab.Controls.Add(Yions_OlinkedChecklistBox);
            YnaughtTab.Controls.Add(Yions_CheckNglycoMannoseButton);
            YnaughtTab.Controls.Add(Yions_LossFromPepChecklistBox);
            YnaughtTab.Controls.Add(Yions_FucoseNlinkedCheckedBox);
            YnaughtTab.Controls.Add(Yions_NlinkedCheckBox);
            YnaughtTab.Controls.Add(FirstIsotopeCheckBox);
            YnaughtTab.Controls.Add(BrowseGlycoPepIDs);
            YnaughtTab.Controls.Add(LoadInGlycoPepIDs_TextBox);
            YnaughtTab.Location = new Point(4, 24);
            YnaughtTab.Name = "YnaughtTab";
            YnaughtTab.Padding = new Padding(3);
            YnaughtTab.Size = new Size(1201, 708);
            YnaughtTab.TabIndex = 1;
            YnaughtTab.Text = "Ynaught";
            YnaughtTab.UseVisualStyleBackColor = true;
            // 
            // Ynaught_intLabel
            // 
            Ynaught_intLabel.AutoSize = true;
            Ynaught_intLabel.Location = new Point(1007, 374);
            Ynaught_intLabel.Name = "Ynaught_intLabel";
            Ynaught_intLabel.Size = new Size(170, 15);
            Ynaught_intLabel.TabIndex = 82;
            Ynaught_intLabel.Text = "Intensity Requirement (.mzML)";
            // 
            // Ynaught_intTextBox
            // 
            Ynaught_intTextBox.Location = new Point(951, 371);
            Ynaught_intTextBox.Name = "Ynaught_intTextBox";
            Ynaught_intTextBox.Size = new Size(50, 23);
            Ynaught_intTextBox.TabIndex = 81;
            Ynaught_intTextBox.Text = "1000";
            // 
            // Ynaught_outputButton
            // 
            Ynaught_outputButton.Location = new Point(1095, 80);
            Ynaught_outputButton.Name = "Ynaught_outputButton";
            Ynaught_outputButton.Size = new Size(90, 23);
            Ynaught_outputButton.TabIndex = 80;
            Ynaught_outputButton.Text = "Browse";
            Ynaught_outputButton.UseVisualStyleBackColor = true;
            Ynaught_outputButton.Click += Ynaught_outputButton_Click;
            // 
            // Ynaught_outputTextBox
            // 
            Ynaught_outputTextBox.Location = new Point(15, 81);
            Ynaught_outputTextBox.Name = "Ynaught_outputTextBox";
            Ynaught_outputTextBox.Size = new Size(1074, 23);
            Ynaught_outputTextBox.TabIndex = 79;
            Ynaught_outputTextBox.Text = "Select output directory";
            // 
            // YNaught_IPSAcheckbox
            // 
            YNaught_IPSAcheckbox.AutoSize = true;
            YNaught_IPSAcheckbox.Location = new Point(320, 601);
            YNaught_IPSAcheckbox.Name = "YNaught_IPSAcheckbox";
            YNaught_IPSAcheckbox.Size = new Size(159, 19);
            YNaught_IPSAcheckbox.TabIndex = 78;
            YNaught_IPSAcheckbox.Text = "Output IPSA Annotations";
            YNaught_IPSAcheckbox.UseVisualStyleBackColor = true;
            // 
            // ChargeExplanationLabel
            // 
            ChargeExplanationLabel.AutoSize = true;
            ChargeExplanationLabel.Location = new Point(955, 486);
            ChargeExplanationLabel.Name = "ChargeExplanationLabel";
            ChargeExplanationLabel.Size = new Size(232, 45);
            ChargeExplanationLabel.TabIndex = 77;
            ChargeExplanationLabel.Text = "Use \"P\" to specify precursor charge. \r\nEx: 1 to P-1 would search from charge 1 to \r\nthe precursor charge minus 1";
            ChargeExplanationLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // UpperBoundLabel
            // 
            UpperBoundLabel.AutoSize = true;
            UpperBoundLabel.ForeColor = SystemColors.ControlDarkDark;
            UpperBoundLabel.Location = new Point(1092, 531);
            UpperBoundLabel.Name = "UpperBoundLabel";
            UpperBoundLabel.Size = new Size(77, 15);
            UpperBoundLabel.TabIndex = 76;
            UpperBoundLabel.Text = "Upper Bound";
            // 
            // LowerBoundLabel
            // 
            LowerBoundLabel.AutoSize = true;
            LowerBoundLabel.ForeColor = SystemColors.ControlDarkDark;
            LowerBoundLabel.Location = new Point(985, 531);
            LowerBoundLabel.Name = "LowerBoundLabel";
            LowerBoundLabel.Size = new Size(77, 15);
            LowerBoundLabel.TabIndex = 75;
            LowerBoundLabel.Text = "Lower Bound";
            // 
            // dashLabel
            // 
            dashLabel.AutoSize = true;
            dashLabel.Font = new Font("Segoe UI", 9F);
            dashLabel.Location = new Point(1068, 552);
            dashLabel.Name = "dashLabel";
            dashLabel.Size = new Size(18, 15);
            dashLabel.TabIndex = 74;
            dashLabel.Text = "to";
            // 
            // UpperBoundTextBox
            // 
            UpperBoundTextBox.Location = new Point(1092, 549);
            UpperBoundTextBox.Name = "UpperBoundTextBox";
            UpperBoundTextBox.Size = new Size(79, 23);
            UpperBoundTextBox.TabIndex = 73;
            UpperBoundTextBox.Text = "P";
            // 
            // LowerBoundTextBox
            // 
            LowerBoundTextBox.Location = new Point(983, 549);
            LowerBoundTextBox.Name = "LowerBoundTextBox";
            LowerBoundTextBox.Size = new Size(79, 23);
            LowerBoundTextBox.TabIndex = 72;
            LowerBoundTextBox.Text = "1";
            // 
            // panel1
            // 
            panel1.Controls.Add(SeparateChargeStates);
            panel1.Controls.Add(GroupChargeStates);
            panel1.Location = new Point(936, 575);
            panel1.Name = "panel1";
            panel1.Size = new Size(270, 50);
            panel1.TabIndex = 71;
            // 
            // SeparateChargeStates
            // 
            SeparateChargeStates.AutoSize = true;
            SeparateChargeStates.Location = new Point(6, 28);
            SeparateChargeStates.Name = "SeparateChargeStates";
            SeparateChargeStates.Size = new Size(232, 19);
            SeparateChargeStates.TabIndex = 70;
            SeparateChargeStates.Text = "Separate columns for each charge state";
            SeparateChargeStates.UseVisualStyleBackColor = true;
            SeparateChargeStates.CheckedChanged += SeparateChargeStates_CheckedChanged;
            // 
            // GroupChargeStates
            // 
            GroupChargeStates.AutoSize = true;
            GroupChargeStates.Checked = true;
            GroupChargeStates.Location = new Point(6, 8);
            GroupChargeStates.Name = "GroupChargeStates";
            GroupChargeStates.Size = new Size(240, 19);
            GroupChargeStates.TabIndex = 69;
            GroupChargeStates.TabStop = true;
            GroupChargeStates.Text = "Group charge state info into one column";
            GroupChargeStates.UseVisualStyleBackColor = true;
            GroupChargeStates.CheckedChanged += GroupChargeStates_CheckedChanged;
            // 
            // Ynaught_DaCheckBox
            // 
            Ynaught_DaCheckBox.AutoSize = true;
            Ynaught_DaCheckBox.Location = new Point(1116, 315);
            Ynaught_DaCheckBox.Name = "Ynaught_DaCheckBox";
            Ynaught_DaCheckBox.Size = new Size(40, 19);
            Ynaught_DaCheckBox.TabIndex = 68;
            Ynaught_DaCheckBox.Text = "Da";
            Ynaught_DaCheckBox.UseVisualStyleBackColor = true;
            Ynaught_DaCheckBox.CheckedChanged += Ynaught_DaCheckBox_CheckedChanged;
            // 
            // Ynaught_GlyCounterLogo
            // 
            Ynaught_GlyCounterLogo.Image = (Image)resources.GetObject("Ynaught_GlyCounterLogo.Image");
            Ynaught_GlyCounterLogo.Location = new Point(330, 385);
            Ynaught_GlyCounterLogo.Name = "Ynaught_GlyCounterLogo";
            Ynaught_GlyCounterLogo.Size = new Size(283, 77);
            Ynaught_GlyCounterLogo.TabIndex = 46;
            Ynaught_GlyCounterLogo.TabStop = false;
            // 
            // Ynaught_FinishTimeLabel
            // 
            Ynaught_FinishTimeLabel.AutoSize = true;
            Ynaught_FinishTimeLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Ynaught_FinishTimeLabel.Location = new Point(320, 502);
            Ynaught_FinishTimeLabel.Name = "Ynaught_FinishTimeLabel";
            Ynaught_FinishTimeLabel.Size = new Size(148, 17);
            Ynaught_FinishTimeLabel.TabIndex = 45;
            Ynaught_FinishTimeLabel.Text = "Finish Time: Not Run Yet";
            // 
            // Ynaught_startTimeLabel
            // 
            Ynaught_startTimeLabel.AutoSize = true;
            Ynaught_startTimeLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Ynaught_startTimeLabel.Location = new Point(320, 480);
            Ynaught_startTimeLabel.Name = "Ynaught_startTimeLabel";
            Ynaught_startTimeLabel.Size = new Size(143, 17);
            Ynaught_startTimeLabel.TabIndex = 44;
            Ynaught_startTimeLabel.Text = "Start Time: Not Run Yet";
            // 
            // Ynaught_StartButton
            // 
            Ynaught_StartButton.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Ynaught_StartButton.Location = new Point(320, 522);
            Ynaught_StartButton.Name = "Ynaught_StartButton";
            Ynaught_StartButton.Size = new Size(305, 73);
            Ynaught_StartButton.TabIndex = 43;
            Ynaught_StartButton.Text = "Start";
            Ynaught_StartButton.UseVisualStyleBackColor = true;
            Ynaught_StartButton.Click += Ynaught_StartButton_Click;
            // 
            // NeutralLosses_Label
            // 
            NeutralLosses_Label.AutoSize = true;
            NeutralLosses_Label.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            NeutralLosses_Label.Location = new Point(631, 124);
            NeutralLosses_Label.Name = "NeutralLosses_Label";
            NeutralLosses_Label.Size = new Size(127, 15);
            NeutralLosses_Label.TabIndex = 42;
            NeutralLosses_Label.Text = "Glycan Neutral Losses";
            // 
            // ClearAllSelections_Button
            // 
            ClearAllSelections_Button.Location = new Point(950, 276);
            ClearAllSelections_Button.Name = "ClearAllSelections_Button";
            ClearAllSelections_Button.Size = new Size(237, 36);
            ClearAllSelections_Button.TabIndex = 39;
            ClearAllSelections_Button.Text = "Clear All Selections";
            ClearAllSelections_Button.UseVisualStyleBackColor = true;
            ClearAllSelections_Button.Click += ClearAllSelections_Button_Click;
            // 
            // Ynaught_SNlabel
            // 
            Ynaught_SNlabel.AutoSize = true;
            Ynaught_SNlabel.Location = new Point(1007, 345);
            Ynaught_SNlabel.Name = "Ynaught_SNlabel";
            Ynaught_SNlabel.Size = new Size(194, 15);
            Ynaught_SNlabel.TabIndex = 38;
            Ynaught_SNlabel.Text = "Signal-to-Noise Requirement (.raw)";
            // 
            // Ynaught_SNthresholdTextBox
            // 
            Ynaught_SNthresholdTextBox.Location = new Point(950, 342);
            Ynaught_SNthresholdTextBox.Name = "Ynaught_SNthresholdTextBox";
            Ynaught_SNthresholdTextBox.Size = new Size(51, 23);
            Ynaught_SNthresholdTextBox.TabIndex = 37;
            Ynaught_SNthresholdTextBox.Text = "3";
            // 
            // CommonOglyco_Label
            // 
            CommonOglyco_Label.AutoSize = true;
            CommonOglyco_Label.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            CommonOglyco_Label.Location = new Point(15, 395);
            CommonOglyco_Label.Name = "CommonOglyco_Label";
            CommonOglyco_Label.Size = new Size(135, 15);
            CommonOglyco_Label.TabIndex = 36;
            CommonOglyco_Label.Text = "Common Oglyco Y-ions";
            // 
            // FucoseYions_Label
            // 
            FucoseYions_Label.AutoSize = true;
            FucoseYions_Label.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            FucoseYions_Label.Location = new Point(320, 124);
            FucoseYions_Label.Name = "FucoseYions_Label";
            FucoseYions_Label.Size = new Size(129, 15);
            FucoseYions_Label.TabIndex = 35;
            FucoseYions_Label.Text = "Fucose-specific Y-ions";
            // 
            // CommonNglycoLabel
            // 
            CommonNglycoLabel.AutoSize = true;
            CommonNglycoLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            CommonNglycoLabel.Location = new Point(15, 124);
            CommonNglycoLabel.Name = "CommonNglycoLabel";
            CommonNglycoLabel.Size = new Size(135, 15);
            CommonNglycoLabel.TabIndex = 34;
            CommonNglycoLabel.Text = "Common Nglyco Y-ions";
            // 
            // CheckAllOglyco_Button
            // 
            CheckAllOglyco_Button.Location = new Point(157, 387);
            CheckAllOglyco_Button.Name = "CheckAllOglyco_Button";
            CheckAllOglyco_Button.Size = new Size(157, 31);
            CheckAllOglyco_Button.TabIndex = 33;
            CheckAllOglyco_Button.Text = "Check All Oglyco Y-ions";
            CheckAllOglyco_Button.UseVisualStyleBackColor = true;
            CheckAllOglyco_Button.Click += CheckAllOglyco_Button_Click;
            // 
            // CheckAllNeutralLosses_Button
            // 
            CheckAllNeutralLosses_Button.Location = new Point(779, 116);
            CheckAllNeutralLosses_Button.Name = "CheckAllNeutralLosses_Button";
            CheckAllNeutralLosses_Button.Size = new Size(157, 31);
            CheckAllNeutralLosses_Button.TabIndex = 32;
            CheckAllNeutralLosses_Button.Text = "Check All Neutral Losses";
            CheckAllNeutralLosses_Button.UseVisualStyleBackColor = true;
            CheckAllNeutralLosses_Button.Click += CheckAllNeutralLosses_Button_Click;
            // 
            // CheckAllFucose_Button
            // 
            CheckAllFucose_Button.Location = new Point(468, 116);
            CheckAllFucose_Button.Name = "CheckAllFucose_Button";
            CheckAllFucose_Button.Size = new Size(157, 31);
            CheckAllFucose_Button.TabIndex = 31;
            CheckAllFucose_Button.Text = "Check All Fucose Y-ions";
            CheckAllFucose_Button.UseVisualStyleBackColor = true;
            CheckAllFucose_Button.Click += CheckAllFucose_Button_Click;
            // 
            // CheckAllNglyco_Button
            // 
            CheckAllNglyco_Button.Location = new Point(157, 116);
            CheckAllNglyco_Button.Name = "CheckAllNglyco_Button";
            CheckAllNglyco_Button.Size = new Size(157, 31);
            CheckAllNglyco_Button.TabIndex = 30;
            CheckAllNglyco_Button.Text = "Check All Nglyco Y-ions";
            CheckAllNglyco_Button.UseVisualStyleBackColor = true;
            CheckAllNglyco_Button.Click += CheckAllNglyco_Button_Click;
            // 
            // Ynaught_ppmTolLabel
            // 
            Ynaught_ppmTolLabel.AutoSize = true;
            Ynaught_ppmTolLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Ynaught_ppmTolLabel.Location = new Point(1007, 316);
            Ynaught_ppmTolLabel.Name = "Ynaught_ppmTolLabel";
            Ynaught_ppmTolLabel.Size = new Size(92, 15);
            Ynaught_ppmTolLabel.TabIndex = 29;
            Ynaught_ppmTolLabel.Text = "tolerance (ppm)";
            // 
            // Ynaught_ppmTolTextBox
            // 
            Ynaught_ppmTolTextBox.Location = new Point(951, 313);
            Ynaught_ppmTolTextBox.Name = "Ynaught_ppmTolTextBox";
            Ynaught_ppmTolTextBox.Size = new Size(50, 23);
            Ynaught_ppmTolTextBox.TabIndex = 28;
            Ynaught_ppmTolTextBox.Text = "15";
            // 
            // Yions_CheckAllButton
            // 
            Yions_CheckAllButton.Location = new Point(950, 113);
            Yions_CheckAllButton.Name = "Yions_CheckAllButton";
            Yions_CheckAllButton.Size = new Size(237, 36);
            Yions_CheckAllButton.TabIndex = 26;
            Yions_CheckAllButton.Text = "Check All Ions";
            Yions_CheckAllButton.UseVisualStyleBackColor = true;
            Yions_CheckAllButton.Click += Yions_CheckAllButton_Click;
            // 
            // Ynaught_ChargeStatesHeader
            // 
            Ynaught_ChargeStatesHeader.AutoSize = true;
            Ynaught_ChargeStatesHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            Ynaught_ChargeStatesHeader.Location = new Point(979, 471);
            Ynaught_ChargeStatesHeader.Name = "Ynaught_ChargeStatesHeader";
            Ynaught_ChargeStatesHeader.Size = new Size(177, 15);
            Ynaught_ChargeStatesHeader.TabIndex = 19;
            Ynaught_ChargeStatesHeader.Text = "What charge states to include?";
            // 
            // SecondIsotopeCheckBox
            // 
            SecondIsotopeCheckBox.AutoSize = true;
            SecondIsotopeCheckBox.Location = new Point(1007, 449);
            SecondIsotopeCheckBox.Name = "SecondIsotopeCheckBox";
            SecondIsotopeCheckBox.Size = new Size(143, 19);
            SecondIsotopeCheckBox.TabIndex = 18;
            SecondIsotopeCheckBox.Text = "Second Isotope (M+2)";
            SecondIsotopeCheckBox.UseVisualStyleBackColor = true;
            // 
            // MonoisotopeLabel2
            // 
            MonoisotopeLabel2.AutoSize = true;
            MonoisotopeLabel2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            MonoisotopeLabel2.Location = new Point(971, 409);
            MonoisotopeLabel2.Name = "MonoisotopeLabel2";
            MonoisotopeLabel2.Size = new Size(183, 15);
            MonoisotopeLabel2.TabIndex = 17;
            MonoisotopeLabel2.Text = "What other isotopes to include?";
            // 
            // MonoisotopeLabel1
            // 
            MonoisotopeLabel1.AutoSize = true;
            MonoisotopeLabel1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            MonoisotopeLabel1.Location = new Point(941, 394);
            MonoisotopeLabel1.Name = "MonoisotopeLabel1";
            MonoisotopeLabel1.Size = new Size(230, 15);
            MonoisotopeLabel1.TabIndex = 16;
            MonoisotopeLabel1.Text = "Default is look for the monoisotope (M).";
            // 
            // Yions_CheckNglycoSialylButton
            // 
            Yions_CheckNglycoSialylButton.Location = new Point(950, 194);
            Yions_CheckNglycoSialylButton.Name = "Yions_CheckNglycoSialylButton";
            Yions_CheckNglycoSialylButton.Size = new Size(237, 36);
            Yions_CheckNglycoSialylButton.TabIndex = 15;
            Yions_CheckNglycoSialylButton.Text = "Check Common Sialyl Ions";
            Yions_CheckNglycoSialylButton.UseVisualStyleBackColor = true;
            Yions_CheckNglycoSialylButton.Click += Yions_CheckNglycoSialylButton_Click;
            // 
            // Yions_CheckNglycoFucoseButton
            // 
            Yions_CheckNglycoFucoseButton.Location = new Point(950, 235);
            Yions_CheckNglycoFucoseButton.Name = "Yions_CheckNglycoFucoseButton";
            Yions_CheckNglycoFucoseButton.Size = new Size(237, 36);
            Yions_CheckNglycoFucoseButton.TabIndex = 14;
            Yions_CheckNglycoFucoseButton.Text = "Check Common Fucose Ions";
            Yions_CheckNglycoFucoseButton.UseVisualStyleBackColor = true;
            Yions_CheckNglycoFucoseButton.Click += Yions_CheckNglycoFucoseButton_Click;
            // 
            // BrowseCustomSubtractions_Button
            // 
            BrowseCustomSubtractions_Button.Location = new Point(1095, 674);
            BrowseCustomSubtractions_Button.Name = "BrowseCustomSubtractions_Button";
            BrowseCustomSubtractions_Button.Size = new Size(90, 23);
            BrowseCustomSubtractions_Button.TabIndex = 13;
            BrowseCustomSubtractions_Button.Text = "Browse";
            BrowseCustomSubtractions_Button.UseVisualStyleBackColor = true;
            BrowseCustomSubtractions_Button.Click += BrowseCustomSubtractions_Button_Click;
            // 
            // BrowseCustomAdditions_Button
            // 
            BrowseCustomAdditions_Button.Location = new Point(1095, 644);
            BrowseCustomAdditions_Button.Name = "BrowseCustomAdditions_Button";
            BrowseCustomAdditions_Button.Size = new Size(90, 23);
            BrowseCustomAdditions_Button.TabIndex = 12;
            BrowseCustomAdditions_Button.Text = "Browse";
            BrowseCustomAdditions_Button.UseVisualStyleBackColor = true;
            BrowseCustomAdditions_Button.Click += BrowseCustomAdditions_Button_Click;
            // 
            // Ynaught_CustomSubtractions_TextBox
            // 
            Ynaught_CustomSubtractions_TextBox.Location = new Point(15, 674);
            Ynaught_CustomSubtractions_TextBox.Name = "Ynaught_CustomSubtractions_TextBox";
            Ynaught_CustomSubtractions_TextBox.Size = new Size(1074, 23);
            Ynaught_CustomSubtractions_TextBox.TabIndex = 11;
            Ynaught_CustomSubtractions_TextBox.Text = "(Optional) Upload custom Y-ion masses to subtract from intact glycopeptide mass here: csv with headers \"Mass\" and \"Description\"";
            Ynaught_CustomSubtractions_TextBox.TextChanged += Ynaught_CustomSubtractions_TextBox_TextChanged;
            // 
            // Ynaught_CustomAdditions_TextBox
            // 
            Ynaught_CustomAdditions_TextBox.Location = new Point(15, 645);
            Ynaught_CustomAdditions_TextBox.Name = "Ynaught_CustomAdditions_TextBox";
            Ynaught_CustomAdditions_TextBox.Size = new Size(1074, 23);
            Ynaught_CustomAdditions_TextBox.TabIndex = 10;
            Ynaught_CustomAdditions_TextBox.Text = "(Optional) Upload custom Y-ion masses to add to unmodified peptide mass here: csv with headers \"Mass\" and \"Description\"";
            Ynaught_CustomAdditions_TextBox.TextChanged += Ynaught_CustomAdditions_TextBox_TextChanged;
            // 
            // LoadInGlycoPepRawFile_TextBox
            // 
            LoadInGlycoPepRawFile_TextBox.Location = new Point(15, 52);
            LoadInGlycoPepRawFile_TextBox.Name = "LoadInGlycoPepRawFile_TextBox";
            LoadInGlycoPepRawFile_TextBox.Size = new Size(1074, 23);
            LoadInGlycoPepRawFile_TextBox.TabIndex = 9;
            LoadInGlycoPepRawFile_TextBox.Text = "Upload .raw or .mzML file here";
            LoadInGlycoPepRawFile_TextBox.TextChanged += LoadInGlycoPepRawFile_TextBox_TextChanged;
            // 
            // BrowseGlycoPepRawFiles_Button
            // 
            BrowseGlycoPepRawFiles_Button.Location = new Point(1095, 52);
            BrowseGlycoPepRawFiles_Button.Name = "BrowseGlycoPepRawFiles_Button";
            BrowseGlycoPepRawFiles_Button.Size = new Size(90, 23);
            BrowseGlycoPepRawFiles_Button.TabIndex = 8;
            BrowseGlycoPepRawFiles_Button.Text = "Browse";
            BrowseGlycoPepRawFiles_Button.UseVisualStyleBackColor = true;
            BrowseGlycoPepRawFiles_Button.Click += BrowseGlycoPepRawFiles_Button_Click;
            // 
            // Yions_OlinkedChecklistBox
            // 
            Yions_OlinkedChecklistBox.CheckOnClick = true;
            Yions_OlinkedChecklistBox.FormattingEnabled = true;
            Yions_OlinkedChecklistBox.Items.AddRange(new object[] { "0, Pep (Y0)", "203.0794, Pep+[HexNAc]", "365.1322, Pep+[HexNAc-Hex]", "406.1588, Pep+[HexNAc2]", "568.2116, Pep+[HexNAc2-Hex]", "730.2644, Pep+[HexNAc2-Hex2]", "494.1748, Pep+[HexNAc-NeuAc]", "510.1697, Pep+[HexNAc-NeuGc]", "656.2276, Pep+[HexNAc-Hex-NeuAc]", "672.2225, Pep+[HexNAc-Hex-NeuGc]" });
            Yions_OlinkedChecklistBox.Location = new Point(15, 424);
            Yions_OlinkedChecklistBox.Name = "Yions_OlinkedChecklistBox";
            Yions_OlinkedChecklistBox.Size = new Size(299, 184);
            Yions_OlinkedChecklistBox.TabIndex = 7;
            // 
            // Yions_CheckNglycoMannoseButton
            // 
            Yions_CheckNglycoMannoseButton.Location = new Point(950, 153);
            Yions_CheckNglycoMannoseButton.Name = "Yions_CheckNglycoMannoseButton";
            Yions_CheckNglycoMannoseButton.Size = new Size(237, 36);
            Yions_CheckNglycoMannoseButton.TabIndex = 6;
            Yions_CheckNglycoMannoseButton.Text = "Check Common High Mannose Ions";
            Yions_CheckNglycoMannoseButton.UseVisualStyleBackColor = true;
            Yions_CheckNglycoMannoseButton.Click += Yions_NglycoMannoseButton_Click;
            // 
            // Yions_LossFromPepChecklistBox
            // 
            Yions_LossFromPepChecklistBox.CheckOnClick = true;
            Yions_LossFromPepChecklistBox.FormattingEnabled = true;
            Yions_LossFromPepChecklistBox.Items.AddRange(new object[] { "GlycoPep (Intact Mass), 0", "GlycoPep-[Hex], 162.0528", "GlycoPep-[Hex2], 324.1057", "GlycoPep-[Hex3], 486.1585", "GlycoPep-[Hex4], 648.2113", "GlycoPep-[Hex5], 810.2641", "GlycoPep-[Hex6], 972.3169", "GlycoPep-[NeuAc], 291.0954", "GlycoPep-[NeuAc-Hex], 453.1482", "GlycoPep-[NeuAc-Hex-HexNAc], 656.2276", "GlycoPep-[NeuAc2], 582.1903", "GlycoPep-[NeuAc2-Hex2], 906.2965", "GlycoPep-[NeuAc2-Hex2-HexNAc2], 1312.4552", "GlycoPep-[NeuGc], 307.1903", "GlycoPep-[NeuGc-Hex], 469.1431", "GlycoPep-[NeuGc-Hex-HexNAc], 672.2225", "GlycoPep-[NeuGc2], 614.1806", "GlycoPep-[NeuGc2-Hex2], 938.2862", "GlycoPep-[NeuGc2-Hex2-HexNAc2], 1344.4450", "GlycoPep-[NeuAc-Hex-HexNAc-dHex], 802.2855", "GlycoPep-[Hex-HexNAc-dHex], 511.1901" });
            Yions_LossFromPepChecklistBox.Location = new Point(631, 153);
            Yions_LossFromPepChecklistBox.Name = "Yions_LossFromPepChecklistBox";
            Yions_LossFromPepChecklistBox.Size = new Size(305, 454);
            Yions_LossFromPepChecklistBox.TabIndex = 5;
            // 
            // Yions_FucoseNlinkedCheckedBox
            // 
            Yions_FucoseNlinkedCheckedBox.CheckOnClick = true;
            Yions_FucoseNlinkedCheckedBox.FormattingEnabled = true;
            Yions_FucoseNlinkedCheckedBox.Items.AddRange(new object[] { "0, Pep (Y0)", "349.1373, Pep+[HexNAc-dHex]", "552.2167, Pep+[HexNAc2-dHex]", "714.2695, Pep+[HexNAc2-Hex-dHex]", "876.3223, Pep+[HexNAc2-Hex2-dHex]", "1038.3751, Pep+[HexNAc2-Hex3-dHex]", "917.3486, Pep+[HexNAc3-Hex-dHex] (bisecting)", "1241.4545, Pep+[HexNAc3-Hex3-dHex]", "1403.5073, Pep+[HexNAc3-Hex4-dHex]", "1606.5867, Pep+[HexNAc4-Hex4-dHex]", "1768.6395, Pep+[HexNAc4-Hex5-dHex]" });
            Yions_FucoseNlinkedCheckedBox.Location = new Point(320, 153);
            Yions_FucoseNlinkedCheckedBox.Name = "Yions_FucoseNlinkedCheckedBox";
            Yions_FucoseNlinkedCheckedBox.Size = new Size(305, 202);
            Yions_FucoseNlinkedCheckedBox.TabIndex = 4;
            // 
            // Yions_NlinkedCheckBox
            // 
            Yions_NlinkedCheckBox.CheckOnClick = true;
            Yions_NlinkedCheckBox.FormattingEnabled = true;
            Yions_NlinkedCheckBox.Items.AddRange(new object[] { "0, Pep (Y0)", "203.0794, Pep+[HexNAc]", "406.1588, Pep+[HexNAc2]", "568.2116, Pep+[HexNAc2-Hex]", "730.2644, Pep+[HexNAc2-Hex2] ", "892.3172, Pep+[HexNAc2-Hex3] ", "771.2909, Pep+[HexNAc3-Hex] (bisecting GlcNAc)", "1095.3966, Pep+[HexNAc3-Hex3]", "1257.4494, Pep+[HexNAc3-Hex4]", "1460.5288, Pep+[HexNAc4-Hex4]", "1622.5816, Pep+[HexNAc4-Hex5]" });
            Yions_NlinkedCheckBox.Location = new Point(15, 153);
            Yions_NlinkedCheckBox.Name = "Yions_NlinkedCheckBox";
            Yions_NlinkedCheckBox.Size = new Size(299, 202);
            Yions_NlinkedCheckBox.TabIndex = 3;
            Yions_NlinkedCheckBox.SelectedIndexChanged += Yions_NlinkedCheckBox_SelectedIndexChanged;
            // 
            // FirstIsotopeCheckBox
            // 
            FirstIsotopeCheckBox.AutoSize = true;
            FirstIsotopeCheckBox.Location = new Point(1007, 424);
            FirstIsotopeCheckBox.Name = "FirstIsotopeCheckBox";
            FirstIsotopeCheckBox.Size = new Size(126, 19);
            FirstIsotopeCheckBox.TabIndex = 2;
            FirstIsotopeCheckBox.Text = "First Isotope (M+1)";
            FirstIsotopeCheckBox.UseVisualStyleBackColor = true;
            // 
            // BrowseGlycoPepIDs
            // 
            BrowseGlycoPepIDs.Location = new Point(1095, 23);
            BrowseGlycoPepIDs.Name = "BrowseGlycoPepIDs";
            BrowseGlycoPepIDs.Size = new Size(90, 23);
            BrowseGlycoPepIDs.TabIndex = 1;
            BrowseGlycoPepIDs.Text = "Browse";
            BrowseGlycoPepIDs.UseVisualStyleBackColor = true;
            BrowseGlycoPepIDs.Click += BrowseGlycoPepIDs_Click;
            // 
            // LoadInGlycoPepIDs_TextBox
            // 
            LoadInGlycoPepIDs_TextBox.Location = new Point(15, 23);
            LoadInGlycoPepIDs_TextBox.Name = "LoadInGlycoPepIDs_TextBox";
            LoadInGlycoPepIDs_TextBox.Size = new Size(1074, 23);
            LoadInGlycoPepIDs_TextBox.TabIndex = 0;
            LoadInGlycoPepIDs_TextBox.Text = "Upload glycopeptide IDs (e.g., PSMs file) here: tab-delimited .txt with headers \"Spectrum\", \"Peptide\", \"Charge\", and \"Total Glycan Composition\"";
            LoadInGlycoPepIDs_TextBox.TextChanged += LoadInGlycoPepIDs_TextBox_TextChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1233, 758);
            Controls.Add(GlyCounter_AllTabs);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "RRG GlyCounter";
            ((System.ComponentModel.ISupportInitialize)GlyCounterLogo).EndInit();
            GlyCounter_AllTabs.ResumeLayout(false);
            GlyCounter_Tab.ResumeLayout(false);
            GlyCounter_Tab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)MSLevelUB).EndInit();
            ((System.ComponentModel.ISupportInitialize)MSLevelLB).EndInit();
            YnaughtTab.ResumeLayout(false);
            YnaughtTab.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)Ynaught_GlyCounterLogo).EndInit();
            ResumeLayout(false);
        }

        private void ipsaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void DaltonCheckBox_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void Ynaught_DaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.CheckedListBox HexNAcCheckedListBox;
        private System.Windows.Forms.CheckedListBox HexCheckedListBox;
        private System.Windows.Forms.CheckedListBox SialicAcidCheckedListBox;
        private System.Windows.Forms.CheckedListBox OligosaccharideCheckedListBox;
        private System.Windows.Forms.Label HexNAc_ions;
        private System.Windows.Forms.Label Hex_ions;
        private System.Windows.Forms.Label Sia_ions;
        private System.Windows.Forms.Label Oligosaccharide_ions;
        private System.Windows.Forms.CheckedListBox M6PCheckedListBox;
        private System.Windows.Forms.Label M6P_ions;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.Button ClearButton;
        private System.Windows.Forms.TextBox ppmTol_textBox;
        private System.Windows.Forms.Label ppmTol_label;
        private System.Windows.Forms.Button CheckAll_Button;
        private System.Windows.Forms.Button CheckAll_Hex_Button;
        private System.Windows.Forms.Button CheckAll_HexNAc_Button;
        private System.Windows.Forms.Button CheckAll_Sialic_Button;
        private System.Windows.Forms.Button CheckAll_M6P_Button;
        private System.Windows.Forms.Button CheckAll_Oligo_Button;
        private System.Windows.Forms.TextBox SN_textBox;
        private System.Windows.Forms.Label SN_label;
        private System.Windows.Forms.TextBox PeakDepth_Box_HCD;
        private System.Windows.Forms.Label PeakDepth_label_HCD;
        private System.Windows.Forms.TextBox hcdTICfraction;
        private System.Windows.Forms.TextBox etdTICfraction;
        private System.Windows.Forms.Label hcdTICfraction_Label;
        private System.Windows.Forms.Label etdTICfraction_Label;
        private System.Windows.Forms.TextBox PeakDepth_Box_ETD;
        private System.Windows.Forms.Label HCDsettingLabel;
        private System.Windows.Forms.Label ETDsettingsLabel;
        private System.Windows.Forms.Label PeakDepth_label_ETD;
        private System.Windows.Forms.Button MostCommonButton;
        private System.Windows.Forms.CheckedListBox FucoseCheckedListBox;
        private System.Windows.Forms.Label Fucose_ions_label;
        private System.Windows.Forms.Button CheckAll_Fucose_Button;
        private System.Windows.Forms.Label OxoCountThreshold_hcd_label;
        private System.Windows.Forms.TextBox OxoCountRequireBox_hcd;
        private System.Windows.Forms.Label OxoCountThreshold_etd_label;
        private System.Windows.Forms.TextBox OxoCountRequireBox_etd;
        private System.Windows.Forms.TextBox uploadCustomTextBox;
        private System.Windows.Forms.Button UploadCustomBrowseButton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.Label FinishTimeLabel;
        private Label StartTimeLabel;
        private PictureBox GlyCounterLogo;
        private TabControl GlyCounter_AllTabs;
        private TabPage GlyCounter_Tab;
        private TabPage YnaughtTab;
        private CheckedListBox Yions_NlinkedCheckBox;
        private CheckBox FirstIsotopeCheckBox;
        private Button BrowseGlycoPepIDs;
        private TextBox LoadInGlycoPepIDs_TextBox;
        private CheckedListBox Yions_FucoseNlinkedCheckedBox;
        private Button Yions_CheckNglycoMannoseButton;
        private CheckedListBox Yions_LossFromPepChecklistBox;
        private CheckedListBox Yions_OlinkedChecklistBox;
        private Button BrowseGlycoPepRawFiles_Button;
        private Button BrowseCustomAdditions_Button;
        private TextBox Ynaught_CustomSubtractions_TextBox;
        private TextBox Ynaught_CustomAdditions_TextBox;
        private TextBox LoadInGlycoPepRawFile_TextBox;
        private Button Yions_CheckNglycoFucoseButton;
        private Button BrowseCustomSubtractions_Button;
        private Button Yions_CheckNglycoSialylButton;
        private Label MonoisotopeLabel2;
        private Label MonoisotopeLabel1;
        private Label Ynaught_ChargeStatesHeader;
        private CheckBox SecondIsotopeCheckBox;
        private Button Yions_CheckAllButton;
        private Label Ynaught_ppmTolLabel;
        private TextBox Ynaught_ppmTolTextBox;
        private Label CommonNglycoLabel;
        private Button CheckAllOglyco_Button;
        private Button CheckAllNeutralLosses_Button;
        private Button CheckAllFucose_Button;
        private Button CheckAllNglyco_Button;
        private Label CommonOglyco_Label;
        private Label FucoseYions_Label;
        private Button ClearAllSelections_Button;
        private Label Ynaught_SNlabel;
        private TextBox Ynaught_SNthresholdTextBox;
        private Label NeutralLosses_Label;
        private Label Ynaught_FinishTimeLabel;
        private Label Ynaught_startTimeLabel;
        private Button Ynaught_StartButton;
        private System.Windows.Forms.Timer timer2;
        private PictureBox Ynaught_GlyCounterLogo;
        private TextBox intensityThresholdTextBox;
        private Label UVPDsettingslabel;
        private Label label1;
        private Label intensityThresholdLabel;
        private TextBox OxoCountRequireBox_uvpd;
        private TextBox uvpdTICfraction;
        private TextBox PeakDepth_Box_UVPD;
        private Label OxoCountThreshold_uvpd_label;
        private Label uvpdTICfraction_Label;
        private Label PeakDepth_label_UVPD;
        private CheckBox DaltonCheckBox;
        private CheckBox Ynaught_DaCheckBox;
        private CheckBox ipsaCheckBox;
        private RadioButton SeparateChargeStates;
        private RadioButton GroupChargeStates;
        private Panel panel1;
        private Label dashLabel;
        private TextBox UpperBoundTextBox;
        private TextBox LowerBoundTextBox;
        private Label LowerBoundLabel;
        private Label ChargeExplanationLabel;
        private Label UpperBoundLabel;
        private CheckBox YNaught_IPSAcheckbox;
        private TextBox Ynaught_outputTextBox;
        private Button Ynaught_outputButton;
        private Label Ynaught_intLabel;
        private TextBox Ynaught_intTextBox;
        private Button Gly_outputButton;
        private TextBox Gly_outputTextBox;
        private Label label3;
        private Label label2;
        private NumericUpDown MSLevelUB;
        private NumericUpDown MSLevelLB;
        private CheckBox polarityCB;
        private CheckBox ignoreMSLevelCB;
    }
}
