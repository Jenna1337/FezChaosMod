using FezGame.ChaosMod;
using FezGame.GameInfo;
using System;
using System.Windows.Forms;

namespace FezGame
{
    class AdditionalChaosEffectSettingsWindow : Form
    {
        private GroupBox GroupBox1;
        private Button ButtonOk;

        private CheckedListBox CheckList { get; }
        private CheckedListBox ShadowCheckList { get; }

        private readonly FezChaosMod.AdditionalChaosEffectSettings settings;

        public AdditionalChaosEffectSettingsWindow(FezChaosMod.AdditionalChaosEffectSettings settings)
        {
            InitializeComponent();
            this.settings = settings;

            this.Name = settings.AdditionalSettingsName;

            CheckList = new CheckedListBox
            {
                Name = settings.AdditionalSettingsName,
                Dock = DockStyle.Fill,
                FormattingEnabled = true,
                IntegralHeight = false,
            };
            foreach (var pair in settings.AdditionalSettingsCheckListList)
            {
                _ = CheckList.Items.Add(pair.Key, pair.Value);
            }

            //Create a syncronized invisible clone to get the list to appear in ChaosModSettingHelper
            ShadowCheckList = new CheckedListBox
            {
                Name = settings.AdditionalSettingsName,
                Height = 0,
                Width = 0,
                Visible = false,
            };
            foreach (var pair in settings.AdditionalSettingsCheckListList)
            {
                _ = ShadowCheckList.Items.Add(pair.Key, pair.Value);
            }

            ChaosModWindow.Instance.Controls.Add(ShadowCheckList);

            ShadowCheckList.ItemCheck += HandleCheck;
            CheckList.ItemCheck += HandleCheck;
            GroupBox1.Controls.Add(CheckList);

            GroupBox1.Text = settings.AdditionalSettingsName;

            ButtonOk.Focus();
        }

        private void InitializeComponent()
        {
            this.ButtonOk = new System.Windows.Forms.Button();
            this.GroupBox1 = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // buttonOk
            // 
            this.ButtonOk.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ButtonOk.Location = new System.Drawing.Point(0, 276);
            this.ButtonOk.Name = "ButtonOk";
            this.ButtonOk.Size = new System.Drawing.Size(379, 37);
            this.ButtonOk.TabIndex = 2;
            this.ButtonOk.Text = "OK";
            this.ButtonOk.UseVisualStyleBackColor = true;
            this.ButtonOk.Click += new System.EventHandler(this.Button1_Click);
            // 
            // groupBox1
            // 
            this.GroupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GroupBox1.Location = new System.Drawing.Point(0, 0);
            this.GroupBox1.Name = "GroupBox1";
            this.GroupBox1.Size = new System.Drawing.Size(379, 276);
            this.GroupBox1.TabIndex = 3;
            this.GroupBox1.TabStop = false;
            this.GroupBox1.Text = "GroupBox1";
            // 
            // AdditionalChaosEffectSettingsWindow
            // 
            this.ClientSize = new System.Drawing.Size(379, 313);
            this.Controls.Add(this.GroupBox1);
            this.Controls.Add(this.ButtonOk);
            this.Name = "AdditionalChaosEffectSettingsWindow";
            this.ShowIcon = false;
            this.Text = "Additional Chaos Effect Settings";
            this.ResumeLayout(false);

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
        private bool doingcheck = false;
        private void HandleCheck(object o, ItemCheckEventArgs a)
        {
            if (doingcheck)
            {
                return;
            }

            doingcheck = true;
            settings.ItemCheck(CheckList.Items[a.Index].ToString(), a.NewValue == CheckState.Checked);
            
            //make sure both lists are updated
            CheckList.SetItemCheckState(a.Index, a.NewValue);
            ShadowCheckList.SetItemCheckState(a.Index, a.NewValue);

            doingcheck = false;
        }
    }
}