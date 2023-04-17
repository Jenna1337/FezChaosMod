using FezGame.ChaosMod;
using FezGame.GameInfo;
using System;
using System.Windows.Forms;

namespace FezGame
{
    class AdditionalChaosEffectSettingsWindow : Form
    {
        private GroupBox groupBox1;
        private Button buttonOk;

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
            groupBox1.Controls.Add(CheckList);

            groupBox1.Text = settings.AdditionalSettingsName;

            buttonOk.Focus();
        }

        private void InitializeComponent()
        {
            this.buttonOk = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // buttonOk
            // 
            this.buttonOk.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonOk.Location = new System.Drawing.Point(0, 276);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(379, 37);
            this.buttonOk.TabIndex = 2;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.button1_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(379, 276);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // AdditionalChaosEffectSettingsWindow
            // 
            this.ClientSize = new System.Drawing.Size(379, 313);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonOk);
            this.Name = "AdditionalChaosEffectSettingsWindow";
            this.ShowIcon = false;
            this.Text = "Additional Chaos Effect Settings";
            this.ResumeLayout(false);

        }

        private void button1_Click(object sender, EventArgs e)
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