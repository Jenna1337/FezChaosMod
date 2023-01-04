using FezGame.ChaosMod;
using FezGame.Randomizer;
using System;
using System.Windows.Forms;

namespace FezGame
{
    class ChaosModAboutWindow : Form
    {
        private Label labelVersionRandomizer;
        private Button buttonOk;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
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
            this.labelVersionRandomizer = new System.Windows.Forms.Label();
            this.buttonOk = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
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
            // labelVersionRandomizer
            // 
            this.labelVersionRandomizer.AutoSize = true;
            this.labelVersionRandomizer.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelVersionRandomizer.Location = new System.Drawing.Point(31, 143);
            this.labelVersionRandomizer.Name = "labelVersionRandomizer";
            this.labelVersionRandomizer.Size = new System.Drawing.Size(204, 22);
            this.labelVersionRandomizer.TabIndex = 1;
            this.labelVersionRandomizer.Text = "labelVersionRandomizer";
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
            this.Controls.Add(this.labelVersionRandomizer);
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