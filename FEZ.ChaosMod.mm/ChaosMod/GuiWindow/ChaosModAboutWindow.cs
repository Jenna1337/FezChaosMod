using FezGame.ChaosMod;
using FezGame.GameInfo;
using System;
using System.Windows.Forms;

namespace FezGame
{
    class ChaosModAboutWindow : Form
    {
        private Button buttonOk;
        private Label labelMadeBy;
        private Label labelVersionChaosMod;

        public ChaosModAboutWindow()
        {
            InitializeComponent();
            labelVersionChaosMod.Text = $"Chaos Mod Version: {FezChaosMod.Version}";
            buttonOk.Focus();
        }

        private void InitializeComponent()
        {
            this.labelVersionChaosMod = new System.Windows.Forms.Label();
            this.buttonOk = new System.Windows.Forms.Button();
            this.labelMadeBy = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelVersionChaosMod
            // 
            this.labelVersionChaosMod.AutoSize = true;
            this.labelVersionChaosMod.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelVersionChaosMod.Location = new System.Drawing.Point(31, 94);
            this.labelVersionChaosMod.Name = "labelVersionChaosMod";
            this.labelVersionChaosMod.Size = new System.Drawing.Size(195, 22);
            this.labelVersionChaosMod.TabIndex = 0;
            this.labelVersionChaosMod.Text = "labelVersionChaosMod";
            // 
            // buttonOk
            // 
            this.buttonOk.Location = new System.Drawing.Point(141, 204);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(108, 37);
            this.buttonOk.TabIndex = 2;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.button1_Click);
            // 
            // labelMadeBy
            // 
            this.labelMadeBy.AutoSize = true;
            this.labelMadeBy.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMadeBy.Location = new System.Drawing.Point(31, 47);
            this.labelMadeBy.Name = "labelMadeBy";
            this.labelMadeBy.Size = new System.Drawing.Size(318, 22);
            this.labelMadeBy.TabIndex = 3;
            this.labelMadeBy.Text = "FEZ Chaos Mod made by Jenna Sloan";
            // 
            // ChaosModAboutWindow
            // 
            this.ClientSize = new System.Drawing.Size(379, 313);
            this.Controls.Add(this.labelMadeBy);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.labelVersionChaosMod);
            this.Name = "ChaosModAboutWindow";
            this.ShowIcon = false;
            this.Text = "About FEZ Chaos Mod";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}