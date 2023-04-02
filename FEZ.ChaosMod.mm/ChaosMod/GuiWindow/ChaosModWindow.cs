using System;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace FezGame.ChaosMod
{
    //TODO somehow change this from winforms to an in-game UI so we can cut dependencies on winforms
    class ChaosModWindow : Form
    {
        private readonly FezChaosMod chaosMod;
        private SplitContainer splitContainer1;
        private GroupBox EffectsCheckListContainer;
        private TextBox EffectLogger;
        private CheckBox ChaosModeCheckBox;
        private GroupBox EffectsDelaySpinnerContainer;
        private Label EffectsUnit;
        private NumericUpDown EffectsDelaySpinner;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem FileToolStripMenuItem;
        private ToolStripMenuItem OpenToolStripMenuItem;
        private ToolStripMenuItem SaveToolStripMenuItem;
        private ToolStripMenuItem SaveAsToolStripMenuItem;
        private SaveFileDialog saveFileDialog1;
        private OpenFileDialog openFileDialog1;
        private ToolStripMenuItem HelpToolStripMenuItem;
        private ToolStripMenuItem AboutFEZChaosModToolStripMenuItem;
        public static ChaosModWindow Instance { get; private set; }
        private string ActiveSaveFile = null;
        private TabControl tabControl1;
        private TabPage tabPageChaosMod;
        private GroupBox EffectsDurationMultiplierSpinnerContainer;
        private NumericUpDown EffectsDurationMultiplierSpinner;
        private ToolStripMenuItem NewToolStripMenuItem;
        private readonly ChaosModAboutWindow AboutForm = new ChaosModAboutWindow();
        private TabPage tabPage1;
        private CheckBox DebugInfoCheckBox;
        private CheckBox AllowRotateAnywhereCheckBox;
        private CheckBox StereoModeCheckBox;
        private CheckBox FirstPersonAnywhereCheckBox;
        private GroupBox MaxEffectsToDisplaySpinnerContainer;
        private Label MaxEffectsCountUnit;
        private NumericUpDown MaxEffectsToDisplaySpinner;
        private readonly ChaosModEffectListControl GroupedEffectsList;
        private CheckBox ZuSpeakEnglishCheckBox;
        private GroupBox LatestEffectsToDisplaySpinnerContainer;
        private Label LatestEffectsCountUnit;
        private NumericUpDown LatestEffectsToDisplaySpinner;
        private readonly Dictionary<string, Dictionary<string, object>> DefaultSettings;

        public bool Initializing { get; private set; }

        public ChaosModWindow(FezChaosMod chaosMod)
        {
            if (ChaosModWindow.Instance != null)
                ChaosModWindow.Instance.Dispose();
            ChaosModWindow.Instance = this;

            Initializing = true;
            this.Shown += (Object sender, EventArgs e) =>
            {
                Initializing = false;
                LogLine("Initialization complete.");
                LogLine($"Welcome to Chaos Mod version {FezChaosMod.Version}");
            };

            this.chaosMod = chaosMod;
            InitializeComponent();

            DebugInfoCheckBox.Checked = chaosMod.ShowDebugInfo;
            AllowRotateAnywhereCheckBox.Checked = chaosMod.AllowRotateAnywhere;

            this.GroupedEffectsList = new ChaosModEffectListControl(chaosMod)
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                //FormattingEnabled = true;
                //IntegralHeight = false;
                Location = new System.Drawing.Point(3, 22),
                Name = "EffectsCheckList",
                Size = new System.Drawing.Size(244, 201),
                TabIndex = 0
            };
            this.EffectsCheckListContainer.Controls.Add(GroupedEffectsList);
            this.ResumeLayout(false);
            this.PerformLayout();

            this.EffectsDelaySpinner.Value = (decimal)chaosMod.DelayBetweenEffects;
            this.EffectsDurationMultiplierSpinner.Value = (decimal)chaosMod.EffectsDurationMultiplier;
            this.MaxEffectsToDisplaySpinner.Value = (decimal)chaosMod.MaxActiveEffectsToDisplay;
            this.LatestEffectsToDisplaySpinner.Value = (decimal)chaosMod.LatestEffectsToDisplay;


            //TODO add something somewhere that shows the sum of all the effect ratios, and the sum of all the enabled effect ratios


            //this.RoomRandoSeedSpinner.Value = FezRandomizer.Seed;


            DefaultSettings = this.GetAllInputsValues();

            //TODO load settings from last opened valid settings file (gotta figure out where to save the path to the last settings file)

            //RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
            //
            //key.CreateSubKey("FezChaosMod");
            //key = key.OpenSubKey("FezChaosMod", true);

            //var GameState = FezEngine.Tools.ServiceHelper.Get<Services.IGameStateManager>();
            //GameState.ActiveSaveDevice.Load("FezChaosModLastSaveFileDir.bin", (reader)=>
            //{
            //    LoadSettingsFromFile(reader.ReadString());
            //});

            //ActiveSaveFile


            //TODO ask to save if the settings were modified

            Timer updateDataTimer = new Timer
            {
                Interval = 100 //milliseconds
            };
            updateDataTimer.Tick += (object sender, EventArgs e) =>
            {
                var GameState = FezEngine.Tools.ServiceHelper.Get<Services.IGameStateManager>();
                Instance.StereoModeCheckBox.Checked = GameState.StereoMode;
            };
            updateDataTimer.Start();



        }

        private static readonly int MaxLines = 1000;
        public static void LogLine(string text)
        {
            Common.Logger.Log("ChaosMod", text);
            System.Diagnostics.Debug.WriteLine(text);
            if (Instance == null || !Instance.Created || Instance.IsDisposed || !Instance.Visible)
                return;
            _ = Instance.Invoke(new MethodInvoker(delegate
            {
                if (Instance.EffectLogger.Lines.Length > MaxLines)
                {
                    string[] newLines = new string[MaxLines];
                    Array.Copy(Instance.EffectLogger.Lines, 1, newLines, 0, newLines.Length);
                    Instance.EffectLogger.Lines = newLines;
                    Instance.EffectLogger.AppendText(text + Environment.NewLine);
                }
                else
                    Instance.EffectLogger.AppendText(text + Environment.NewLine);
            }));
        }
        public static void LogLineDebug(string text)
        {
            if (Instance == null || !Instance.Created || Instance.IsDisposed || !Instance.Visible)
            {
                Common.Logger.Log("ChaosMod", text);
                return;
            }
            if (Instance.chaosMod.ShowDebugInfo)
                LogLine("Debug: " + text);
        }
        public static void ClearLog()
        {
            Instance.Invoke(new MethodInvoker(delegate { Instance.EffectLogger.Clear(); }));
        }

        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.LatestEffectsToDisplaySpinnerContainer = new System.Windows.Forms.GroupBox();
            this.LatestEffectsCountUnit = new System.Windows.Forms.Label();
            this.LatestEffectsToDisplaySpinner = new System.Windows.Forms.NumericUpDown();
            this.MaxEffectsToDisplaySpinnerContainer = new System.Windows.Forms.GroupBox();
            this.MaxEffectsCountUnit = new System.Windows.Forms.Label();
            this.MaxEffectsToDisplaySpinner = new System.Windows.Forms.NumericUpDown();
            this.EffectsDurationMultiplierSpinnerContainer = new System.Windows.Forms.GroupBox();
            this.EffectsDurationMultiplierSpinner = new System.Windows.Forms.NumericUpDown();
            this.EffectsDelaySpinnerContainer = new System.Windows.Forms.GroupBox();
            this.EffectsUnit = new System.Windows.Forms.Label();
            this.EffectsDelaySpinner = new System.Windows.Forms.NumericUpDown();
            this.ChaosModeCheckBox = new System.Windows.Forms.CheckBox();
            this.EffectsCheckListContainer = new System.Windows.Forms.GroupBox();
            this.EffectLogger = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.FileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutFEZChaosModToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageChaosMod = new System.Windows.Forms.TabPage();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.ZuSpeakEnglishCheckBox = new System.Windows.Forms.CheckBox();
            this.FirstPersonAnywhereCheckBox = new System.Windows.Forms.CheckBox();
            this.StereoModeCheckBox = new System.Windows.Forms.CheckBox();
            this.AllowRotateAnywhereCheckBox = new System.Windows.Forms.CheckBox();
            this.DebugInfoCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.LatestEffectsToDisplaySpinnerContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LatestEffectsToDisplaySpinner)).BeginInit();
            this.MaxEffectsToDisplaySpinnerContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MaxEffectsToDisplaySpinner)).BeginInit();
            this.EffectsDurationMultiplierSpinnerContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EffectsDurationMultiplierSpinner)).BeginInit();
            this.EffectsDelaySpinnerContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EffectsDelaySpinner)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageChaosMod.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.LatestEffectsToDisplaySpinnerContainer);
            this.splitContainer1.Panel1.Controls.Add(this.MaxEffectsToDisplaySpinnerContainer);
            this.splitContainer1.Panel1.Controls.Add(this.EffectsDurationMultiplierSpinnerContainer);
            this.splitContainer1.Panel1.Controls.Add(this.EffectsDelaySpinnerContainer);
            this.splitContainer1.Panel1.Controls.Add(this.ChaosModeCheckBox);
            this.splitContainer1.Panel1.Controls.Add(this.EffectsCheckListContainer);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.EffectLogger);
            this.splitContainer1.Size = new System.Drawing.Size(940, 706);
            this.splitContainer1.SplitterDistance = 352;
            this.splitContainer1.SplitterWidth = 10;
            this.splitContainer1.TabIndex = 1;
            // 
            // LatestEffectsToDisplaySpinnerContainer
            // 
            this.LatestEffectsToDisplaySpinnerContainer.Controls.Add(this.LatestEffectsCountUnit);
            this.LatestEffectsToDisplaySpinnerContainer.Controls.Add(this.LatestEffectsToDisplaySpinner);
            this.LatestEffectsToDisplaySpinnerContainer.Location = new System.Drawing.Point(5, 222);
            this.LatestEffectsToDisplaySpinnerContainer.Name = "LatestEffectsToDisplaySpinnerContainer";
            this.LatestEffectsToDisplaySpinnerContainer.Size = new System.Drawing.Size(214, 57);
            this.LatestEffectsToDisplaySpinnerContainer.TabIndex = 9;
            this.LatestEffectsToDisplaySpinnerContainer.TabStop = false;
            this.LatestEffectsToDisplaySpinnerContainer.Text = "Latest Effects to Display";
            // 
            // LatestEffectsCountUnit
            // 
            this.LatestEffectsCountUnit.AutoSize = true;
            this.LatestEffectsCountUnit.Location = new System.Drawing.Point(132, 27);
            this.LatestEffectsCountUnit.Name = "LatestEffectsCountUnit";
            this.LatestEffectsCountUnit.Size = new System.Drawing.Size(58, 20);
            this.LatestEffectsCountUnit.TabIndex = 7;
            this.LatestEffectsCountUnit.Text = "effects";
            // 
            // LatestEffectsToDisplaySpinner
            // 
            this.LatestEffectsToDisplaySpinner.Location = new System.Drawing.Point(6, 25);
            this.LatestEffectsToDisplaySpinner.Name = "LatestEffectsToDisplaySpinner";
            this.LatestEffectsToDisplaySpinner.Size = new System.Drawing.Size(120, 26);
            this.LatestEffectsToDisplaySpinner.TabIndex = 5;
            this.LatestEffectsToDisplaySpinner.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.LatestEffectsToDisplaySpinner.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
            this.LatestEffectsToDisplaySpinner.ValueChanged += new System.EventHandler(this.LatestEffectsToDisplaySpinner_ValueChanged);
            // 
            // MaxEffectsToDisplaySpinnerContainer
            // 
            this.MaxEffectsToDisplaySpinnerContainer.Controls.Add(this.MaxEffectsCountUnit);
            this.MaxEffectsToDisplaySpinnerContainer.Controls.Add(this.MaxEffectsToDisplaySpinner);
            this.MaxEffectsToDisplaySpinnerContainer.Location = new System.Drawing.Point(5, 159);
            this.MaxEffectsToDisplaySpinnerContainer.Name = "MaxEffectsToDisplaySpinnerContainer";
            this.MaxEffectsToDisplaySpinnerContainer.Size = new System.Drawing.Size(214, 57);
            this.MaxEffectsToDisplaySpinnerContainer.TabIndex = 8;
            this.MaxEffectsToDisplaySpinnerContainer.TabStop = false;
            this.MaxEffectsToDisplaySpinnerContainer.Text = "Max Effects to Display";
            // 
            // MaxEffectsCountUnit
            // 
            this.MaxEffectsCountUnit.AutoSize = true;
            this.MaxEffectsCountUnit.Location = new System.Drawing.Point(132, 27);
            this.MaxEffectsCountUnit.Name = "MaxEffectsCountUnit";
            this.MaxEffectsCountUnit.Size = new System.Drawing.Size(58, 20);
            this.MaxEffectsCountUnit.TabIndex = 7;
            this.MaxEffectsCountUnit.Text = "effects";
            // 
            // MaxEffectsToDisplaySpinner
            // 
            this.MaxEffectsToDisplaySpinner.Location = new System.Drawing.Point(6, 25);
            this.MaxEffectsToDisplaySpinner.Name = "MaxEffectsToDisplaySpinner";
            this.MaxEffectsToDisplaySpinner.Size = new System.Drawing.Size(120, 26);
            this.MaxEffectsToDisplaySpinner.TabIndex = 5;
            this.MaxEffectsToDisplaySpinner.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.MaxEffectsToDisplaySpinner.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
            this.MaxEffectsToDisplaySpinner.ValueChanged += new System.EventHandler(this.MaxEffectsToDisplaySpinner_ValueChanged);
            // 
            // EffectsDurationMultiplierSpinnerContainer
            // 
            this.EffectsDurationMultiplierSpinnerContainer.Controls.Add(this.EffectsDurationMultiplierSpinner);
            this.EffectsDurationMultiplierSpinnerContainer.Location = new System.Drawing.Point(4, 96);
            this.EffectsDurationMultiplierSpinnerContainer.Name = "EffectsDurationMultiplierSpinnerContainer";
            this.EffectsDurationMultiplierSpinnerContainer.Size = new System.Drawing.Size(214, 57);
            this.EffectsDurationMultiplierSpinnerContainer.TabIndex = 7;
            this.EffectsDurationMultiplierSpinnerContainer.TabStop = false;
            this.EffectsDurationMultiplierSpinnerContainer.Text = "Effect Duration Multiplier";
            // 
            // EffectsDurationMultiplierSpinner
            // 
            this.EffectsDurationMultiplierSpinner.DecimalPlaces = 3;
            this.EffectsDurationMultiplierSpinner.Location = new System.Drawing.Point(6, 25);
            this.EffectsDurationMultiplierSpinner.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.EffectsDurationMultiplierSpinner.Name = "EffectsDurationMultiplierSpinner";
            this.EffectsDurationMultiplierSpinner.Size = new System.Drawing.Size(120, 26);
            this.EffectsDurationMultiplierSpinner.TabIndex = 5;
            this.EffectsDurationMultiplierSpinner.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.EffectsDurationMultiplierSpinner.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
            this.EffectsDurationMultiplierSpinner.ValueChanged += new System.EventHandler(this.EffectsDurationMultiplierSpinner_ValueChanged);
            // 
            // EffectsDelaySpinnerContainer
            // 
            this.EffectsDelaySpinnerContainer.Controls.Add(this.EffectsUnit);
            this.EffectsDelaySpinnerContainer.Controls.Add(this.EffectsDelaySpinner);
            this.EffectsDelaySpinnerContainer.Location = new System.Drawing.Point(4, 33);
            this.EffectsDelaySpinnerContainer.Name = "EffectsDelaySpinnerContainer";
            this.EffectsDelaySpinnerContainer.Size = new System.Drawing.Size(214, 57);
            this.EffectsDelaySpinnerContainer.TabIndex = 6;
            this.EffectsDelaySpinnerContainer.TabStop = false;
            this.EffectsDelaySpinnerContainer.Text = "Delay Between Effects";
            // 
            // EffectsUnit
            // 
            this.EffectsUnit.AutoSize = true;
            this.EffectsUnit.Location = new System.Drawing.Point(133, 27);
            this.EffectsUnit.Name = "EffectsUnit";
            this.EffectsUnit.Size = new System.Drawing.Size(69, 20);
            this.EffectsUnit.TabIndex = 6;
            this.EffectsUnit.Text = "seconds";
            // 
            // EffectsDelaySpinner
            // 
            this.EffectsDelaySpinner.DecimalPlaces = 3;
            this.EffectsDelaySpinner.Location = new System.Drawing.Point(6, 25);
            this.EffectsDelaySpinner.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.EffectsDelaySpinner.Name = "EffectsDelaySpinner";
            this.EffectsDelaySpinner.Size = new System.Drawing.Size(120, 26);
            this.EffectsDelaySpinner.TabIndex = 5;
            this.EffectsDelaySpinner.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.EffectsDelaySpinner.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
            this.EffectsDelaySpinner.ValueChanged += new System.EventHandler(this.EffectsDelaySpinner_ValueChanged);
            // 
            // ChaosModeCheckBox
            // 
            this.ChaosModeCheckBox.AutoSize = true;
            this.ChaosModeCheckBox.Checked = true;
            this.ChaosModeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChaosModeCheckBox.Location = new System.Drawing.Point(4, 3);
            this.ChaosModeCheckBox.Name = "ChaosModeCheckBox";
            this.ChaosModeCheckBox.Size = new System.Drawing.Size(179, 24);
            this.ChaosModeCheckBox.TabIndex = 3;
            this.ChaosModeCheckBox.Text = "Chaos Mod Enabled";
            this.ChaosModeCheckBox.UseVisualStyleBackColor = true;
            this.ChaosModeCheckBox.CheckedChanged += new System.EventHandler(this.ChaosModeCheckBox_CheckedChanged);
            // 
            // EffectsCheckListContainer
            // 
            this.EffectsCheckListContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EffectsCheckListContainer.Location = new System.Drawing.Point(224, 3);
            this.EffectsCheckListContainer.Name = "EffectsCheckListContainer";
            this.EffectsCheckListContainer.Size = new System.Drawing.Size(713, 346);
            this.EffectsCheckListContainer.TabIndex = 2;
            this.EffectsCheckListContainer.TabStop = false;
            this.EffectsCheckListContainer.Text = "Effects";
            // 
            // EffectLogger
            // 
            this.EffectLogger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EffectLogger.Location = new System.Drawing.Point(0, 0);
            this.EffectLogger.Multiline = true;
            this.EffectLogger.Name = "EffectLogger";
            this.EffectLogger.ReadOnly = true;
            this.EffectLogger.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.EffectLogger.Size = new System.Drawing.Size(940, 344);
            this.EffectLogger.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileToolStripMenuItem,
            this.HelpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(954, 33);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // FileToolStripMenuItem
            // 
            this.FileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewToolStripMenuItem,
            this.OpenToolStripMenuItem,
            this.SaveToolStripMenuItem,
            this.SaveAsToolStripMenuItem});
            this.FileToolStripMenuItem.Name = "FileToolStripMenuItem";
            this.FileToolStripMenuItem.Size = new System.Drawing.Size(54, 29);
            this.FileToolStripMenuItem.Text = "&File";
            // 
            // NewToolStripMenuItem
            // 
            this.NewToolStripMenuItem.Name = "NewToolStripMenuItem";
            this.NewToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.NewToolStripMenuItem.Size = new System.Drawing.Size(271, 34);
            this.NewToolStripMenuItem.Text = "New";
            this.NewToolStripMenuItem.Click += new System.EventHandler(this.NewToolStripMenuItem_Click);
            // 
            // OpenToolStripMenuItem
            // 
            this.OpenToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.OpenToolStripMenuItem.Name = "OpenToolStripMenuItem";
            this.OpenToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.OpenToolStripMenuItem.Size = new System.Drawing.Size(271, 34);
            this.OpenToolStripMenuItem.Text = "&Open";
            this.OpenToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // SaveToolStripMenuItem
            // 
            this.SaveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SaveToolStripMenuItem.Name = "SaveToolStripMenuItem";
            this.SaveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.SaveToolStripMenuItem.Size = new System.Drawing.Size(271, 34);
            this.SaveToolStripMenuItem.Text = "&Save";
            this.SaveToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
            // 
            // SaveAsToolStripMenuItem
            // 
            this.SaveAsToolStripMenuItem.Name = "SaveAsToolStripMenuItem";
            this.SaveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.S)));
            this.SaveAsToolStripMenuItem.Size = new System.Drawing.Size(271, 34);
            this.SaveAsToolStripMenuItem.Text = "Save &As";
            this.SaveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItem_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AboutFEZChaosModToolStripMenuItem});
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(65, 29);
            this.HelpToolStripMenuItem.Text = "Help";
            // 
            // AboutFEZChaosModToolStripMenuItem
            // 
            this.AboutFEZChaosModToolStripMenuItem.Name = "AboutFEZChaosModToolStripMenuItem";
            this.AboutFEZChaosModToolStripMenuItem.Size = new System.Drawing.Size(294, 34);
            this.AboutFEZChaosModToolStripMenuItem.Text = "About FEZ Chaos Mod";
            this.AboutFEZChaosModToolStripMenuItem.Click += new System.EventHandler(this.AboutFEZChaosModToolStripMenuItem_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "ini";
            this.saveFileDialog1.FileName = "chaosmod";
            this.saveFileDialog1.Filter = "Settings file|*.ini";
            this.saveFileDialog1.Title = "Save Chaos Mod Settings File";
            this.saveFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.SaveFileDialog1_FileOk);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "ini";
            this.openFileDialog1.FileName = "chaosmod";
            this.openFileDialog1.Filter = "Settings file|*.ini";
            this.openFileDialog1.Title = "Open Chaos Mod Settings File";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.OpenFileDialog1_FileOk);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageChaosMod);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Location = new System.Drawing.Point(0, 36);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(954, 745);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPageChaosMod
            // 
            this.tabPageChaosMod.Controls.Add(this.splitContainer1);
            this.tabPageChaosMod.Location = new System.Drawing.Point(4, 29);
            this.tabPageChaosMod.Name = "tabPageChaosMod";
            this.tabPageChaosMod.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageChaosMod.Size = new System.Drawing.Size(946, 712);
            this.tabPageChaosMod.TabIndex = 0;
            this.tabPageChaosMod.Text = "Chaos Mod";
            this.tabPageChaosMod.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.ZuSpeakEnglishCheckBox);
            this.tabPage1.Controls.Add(this.FirstPersonAnywhereCheckBox);
            this.tabPage1.Controls.Add(this.StereoModeCheckBox);
            this.tabPage1.Controls.Add(this.AllowRotateAnywhereCheckBox);
            this.tabPage1.Controls.Add(this.DebugInfoCheckBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 29);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(917, 712);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "Miscellaneous";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // ZuSpeakEnglishCheckBox
            // 
            this.ZuSpeakEnglishCheckBox.AutoSize = true;
            this.ZuSpeakEnglishCheckBox.Location = new System.Drawing.Point(9, 127);
            this.ZuSpeakEnglishCheckBox.Name = "ZuSpeakEnglishCheckBox";
            this.ZuSpeakEnglishCheckBox.Size = new System.Drawing.Size(193, 24);
            this.ZuSpeakEnglishCheckBox.TabIndex = 4;
            this.ZuSpeakEnglishCheckBox.Text = "All Zu speak is English";
            this.ZuSpeakEnglishCheckBox.UseVisualStyleBackColor = true;
            this.ZuSpeakEnglishCheckBox.CheckedChanged += new System.EventHandler(this.ZuSpeakEnglishCheckBox_CheckedChanged);
            // 
            // FirstPersonAnywhereCheckBox
            // 
            this.FirstPersonAnywhereCheckBox.AutoSize = true;
            this.FirstPersonAnywhereCheckBox.Location = new System.Drawing.Point(9, 67);
            this.FirstPersonAnywhereCheckBox.Name = "FirstPersonAnywhereCheckBox";
            this.FirstPersonAnywhereCheckBox.Size = new System.Drawing.Size(235, 24);
            this.FirstPersonAnywhereCheckBox.TabIndex = 3;
            this.FirstPersonAnywhereCheckBox.Text = "Allow First Person Anywhere";
            this.FirstPersonAnywhereCheckBox.UseVisualStyleBackColor = true;
            this.FirstPersonAnywhereCheckBox.CheckedChanged += new System.EventHandler(this.FirstPersonAnywhereCheckBox_CheckedChanged);
            // 
            // StereoModeCheckBox
            // 
            this.StereoModeCheckBox.AutoSize = true;
            this.StereoModeCheckBox.Location = new System.Drawing.Point(9, 97);
            this.StereoModeCheckBox.Name = "StereoModeCheckBox";
            this.StereoModeCheckBox.Size = new System.Drawing.Size(172, 24);
            this.StereoModeCheckBox.TabIndex = 2;
            this.StereoModeCheckBox.Text = "Stereoscopic Mode";
            this.StereoModeCheckBox.UseVisualStyleBackColor = true;
            this.StereoModeCheckBox.CheckedChanged += new System.EventHandler(this.StereoModeCheckBox_CheckedChanged);
            // 
            // AllowRotateAnywhereCheckBox
            // 
            this.AllowRotateAnywhereCheckBox.AutoSize = true;
            this.AllowRotateAnywhereCheckBox.Location = new System.Drawing.Point(9, 37);
            this.AllowRotateAnywhereCheckBox.Name = "AllowRotateAnywhereCheckBox";
            this.AllowRotateAnywhereCheckBox.Size = new System.Drawing.Size(199, 24);
            this.AllowRotateAnywhereCheckBox.TabIndex = 1;
            this.AllowRotateAnywhereCheckBox.Text = "Allow Rotate Anywhere";
            this.AllowRotateAnywhereCheckBox.UseVisualStyleBackColor = true;
            this.AllowRotateAnywhereCheckBox.CheckedChanged += new System.EventHandler(this.AllowRotateAnywhereCheckBox_CheckedChanged);
            // 
            // DebugInfoCheckBox
            // 
            this.DebugInfoCheckBox.AutoSize = true;
            this.DebugInfoCheckBox.Checked = true;
            this.DebugInfoCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.DebugInfoCheckBox.Location = new System.Drawing.Point(8, 6);
            this.DebugInfoCheckBox.Name = "DebugInfoCheckBox";
            this.DebugInfoCheckBox.Size = new System.Drawing.Size(170, 24);
            this.DebugInfoCheckBox.TabIndex = 0;
            this.DebugInfoCheckBox.Text = "Display Debug Info";
            this.DebugInfoCheckBox.UseVisualStyleBackColor = true;
            this.DebugInfoCheckBox.CheckedChanged += new System.EventHandler(this.DebugInfoCheckBox_CheckedChanged);
            // 
            // ChaosModWindow
            // 
            this.ClientSize = new System.Drawing.Size(954, 781);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.tabControl1);
            this.HelpButton = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ChaosModWindow";
            this.Text = "Chaos Mod Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ChaosModWindow_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.LatestEffectsToDisplaySpinnerContainer.ResumeLayout(false);
            this.LatestEffectsToDisplaySpinnerContainer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LatestEffectsToDisplaySpinner)).EndInit();
            this.MaxEffectsToDisplaySpinnerContainer.ResumeLayout(false);
            this.MaxEffectsToDisplaySpinnerContainer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MaxEffectsToDisplaySpinner)).EndInit();
            this.EffectsDurationMultiplierSpinnerContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.EffectsDurationMultiplierSpinner)).EndInit();
            this.EffectsDelaySpinnerContainer.ResumeLayout(false);
            this.EffectsDelaySpinnerContainer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EffectsDelaySpinner)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageChaosMod.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void ChaosModeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bool chaosChecked = ChaosModeCheckBox.Checked;
            FezChaosMod.Enabled = chaosChecked;
            //EffectsDelaySpinner.Enabled = chaosChecked;
            //EffectsDurationMultiplierSpinner.Enabled = chaosChecked;
            //EffectsCheckList.Enabled = chaosChecked;
            //RandTeleCheckList.Enabled = chaosChecked;
        }

        private void EffectsDelaySpinner_ValueChanged(object sender, EventArgs e) { chaosMod.DelayBetweenEffects = Decimal.ToDouble(EffectsDelaySpinner.Value); }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e) { _ = openFileDialog1.ShowDialog(Instance); }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(ActiveSaveFile))
                SaveSettingsToFile(ActiveSaveFile);
            else
                saveFileDialog1.ShowDialog(Instance);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e) { _ = saveFileDialog1.ShowDialog(Instance); }
        private void SaveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e) { SaveSettingsToFile(saveFileDialog1.FileName); }
        private void OpenFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e) { LoadSettingsFromFile(openFileDialog1.FileName); }

        private void SaveSettingsToFile(string file)
        {
            try
            {
                ActiveSaveFile = Path.GetFullPath(file);
                ChaosModWindow.LogLineDebug("Saving to file: " + file);
                ChaosModSettingsHelper.Write(this, file);
            }
            catch (Exception e)
            {
                ChaosModWindow.LogLine("Warning: Failed to write to file: " + file);
                ChaosModWindow.LogLine(e.Message);
            }
        }
        private void LoadSettingsFromFile(string file)
        {
            try
            {
                ActiveSaveFile = Path.GetFullPath(file);
                ChaosModWindow.LogLineDebug("Loading from file: " + file);
                ChaosModSettingsHelper.Read(this, file);
            }
            catch (Exception e)
            {
                ChaosModWindow.LogLine("Warning: Failed to load from file: " + file);
                ChaosModWindow.LogLine(e.Message);
            }
        }
        private void AboutFEZChaosModToolStripMenuItem_Click(object sender, EventArgs e) { AboutForm.Show(this); }

        private void EffectsDurationMultiplierSpinner_ValueChanged(object sender, EventArgs e) { chaosMod.EffectsDurationMultiplier = Decimal.ToDouble(EffectsDurationMultiplierSpinner.Value); }

        private void ChaosModWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SetAllInputsValues(DefaultSettings);
        }

        private void DebugInfoCheckBox_CheckedChanged(object sender, EventArgs e) { chaosMod.ShowDebugInfo = DebugInfoCheckBox.Checked; }

        private void AllowRotateAnywhereCheckBox_CheckedChanged(object sender, EventArgs e) { chaosMod.AllowRotateAnywhere = AllowRotateAnywhereCheckBox.Checked; }

        private void StereoModeCheckBox_CheckedChanged(object sender, EventArgs e) { FezEngine.Tools.ServiceHelper.Get<Services.IGameStateManager>().StereoMode = StereoModeCheckBox.Checked; }

        private void FirstPersonAnywhereCheckBox_CheckedChanged(object sender, EventArgs e) { chaosMod.AllowFirstPersonAnywhere = FirstPersonAnywhereCheckBox.Checked; }

        private void MaxEffectsToDisplaySpinner_ValueChanged(object sender, EventArgs e)
        {
            decimal val = MaxEffectsToDisplaySpinner.Value;
            chaosMod.MaxActiveEffectsToDisplay = Decimal.ToInt32(val);
            LatestEffectsToDisplaySpinner.Maximum = val;
        }

        private void ZuSpeakEnglishCheckBox_CheckedChanged(object sender, EventArgs e) { chaosMod.ZuSpeakEnglish = ZuSpeakEnglishCheckBox.Checked; }

        private void LatestEffectsToDisplaySpinner_ValueChanged(object sender, EventArgs e)
        {
            chaosMod.LatestEffectsToDisplay = Decimal.ToInt32(LatestEffectsToDisplaySpinner.Value);
        }
    }
}