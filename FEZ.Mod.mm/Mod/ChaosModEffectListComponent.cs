﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static FezGame.ChaosMod.FezChaosMod;

namespace FezGame.ChaosMod
{
    class ChaosModEffectListControl : CollapsableGroupedListControl
    {
        private static readonly bool ignoresubcategories = true;

        private readonly ToolTip tooltip;
        private const string NameSeperator = CollapsableGroupControl.NameSeperator;
        public ChaosModEffectListControl(FezChaosMod chaosMod)
        {
            tooltip = new ToolTip
            {
                ShowAlways = true,
                AutomaticDelay = 1,
                ReshowDelay = 1,
                InitialDelay = 1,
                AutoPopDelay = 10000,
                UseFading = false,
                UseAnimation = false
            };

            for (int i = 0; i < chaosMod.ChaosEffectsList.Count; ++i)
            {
                AddEffect(chaosMod.ChaosEffectsList[i]);
            }

            ResizeLabelsSoEverythingLooksNice();

            System.Diagnostics.Debug.WriteLine($"Effect categories: {{{String.Join(", ", Groups.Keys)}}}");

            //TODO make it load faster when switching tabs in ChaosModWindow
            //TODO add a thing to each group to indicate how many are enabled
            //TODO indicate if/when an effect can start?; maybe change the color of the text or something
            //TODO add a button to manually and immediately start an effect
            //TODO add a thing to enable/disable all the effects in a category
        }
        private Regex subcatregex = new Regex(@"(?<=\D)\.(?=\D)");
        private void AddEffect(ChaosEffect effect)
        {
            //CheckBox, NumericUpDown, NumericUpDown
            CheckBox enabledCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                CheckState = System.Windows.Forms.CheckState.Checked,
                //enabledCheckBox.Location = new System.Drawing.Point(4, 3);
                Name = effect.Name,
                //enabledCheckBox.Size = new System.Drawing.Size(179, 24);
                //enabledCheckBox.TabIndex = 3;
                Text = effect.Name,
                UseVisualStyleBackColor = true
            };
            enabledCheckBox.CheckedChanged += new System.EventHandler((object sender, EventArgs e) => { effect.Enabled = enabledCheckBox.Checked; });

            NumericUpDown ratioSpinner = new NumericUpDown
            {
                DecimalPlaces = 4,
                //Location = new System.Drawing.Point(6, 25);
                Maximum = new decimal(new int[] {
                1000,
                0,
                0,
                0}),
                Name = effect.Name + NameSeperator + "RatioSpinner",
                Size = new System.Drawing.Size(120, 26),
                //TabIndex = 5;
                TextAlign = System.Windows.Forms.HorizontalAlignment.Right,
                UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left,
                Value = (decimal)effect.Ratio
            };
            ratioSpinner.ValueChanged += new System.EventHandler((object sender, EventArgs e) => { effect.Ratio = Decimal.ToDouble(ratioSpinner.Value); });
            tooltip.SetToolTip(ratioSpinner, "Ratio");

            NumericUpDown durationSpinner = null;
            if (effect.Duration > 0)
            {
                durationSpinner = new NumericUpDown
                {
                    DecimalPlaces = 4,
                    //Location = new System.Drawing.Point(6, 25);
                    Maximum = new decimal(new int[] {
                    1000,
                    0,
                    0,
                    0}),
                    Name = effect.Name + NameSeperator + "DurationSpinner",
                    Size = new System.Drawing.Size(120, 26),
                    //TabIndex = 5;
                    TextAlign = System.Windows.Forms.HorizontalAlignment.Right,
                    UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left,
                    Value = (decimal)effect.Duration
                };
                durationSpinner.ValueChanged += new System.EventHandler((object sender, EventArgs e) => { effect.Duration = Decimal.ToDouble(durationSpinner.Value); });
                tooltip.SetToolTip(durationSpinner, "Duration");
            }

            string category = effect.Category != null && effect.Category.Length > 0 ? effect.Category : "Uncategorized";
            string[] categories = subcatregex.Split(category);

            if (ignoresubcategories || categories.Length == 1)
            {
                this.Add(category, enabledCheckBox, ratioSpinner, durationSpinner);
            }
            else
            {
                //TODO add subcategories for groups that match subcatregex
                int lastcatindex = categories.Length - 1;
                string lastcat = categories[lastcatindex];

                string firstcat = categories[0];

                CollapsableGroupedListControl container = this;
                for (int i = 0; i < lastcatindex; ++i)
                {
                    string subcatname = categories[i];
                    System.Diagnostics.Debug.WriteLine(subcatname);
                    var subcat = container[subcatname];
                    subcat.GroupContainer.Dock = DockStyle.Fill;
                    var controls = subcat.GroupContainer.Controls;
                    if(controls.Count <= 0)
                    {
                        var newlist = new CollapsableGroupedListControl();
                        controls.Add(newlist);
                    }
                    container = (CollapsableGroupedListControl)controls[0];
                    System.Diagnostics.Debug.WriteLine(String.Join(", ", controls));
                    //.Controls.Add()
                    ;
                }
                container.Add(lastcat, enabledCheckBox, ratioSpinner, durationSpinner);
                this.PerformLayout();
            }
        }
        private void ResizeLabelsSoEverythingLooksNice()
        {
            this.PerformLayout();
            int longestEffectNameWidth = 0;
            foreach (Control control in Controls)
            {
                var labelControl = control.Controls[0].Controls[1];
                var groupAreaControls = control.Controls[1].Controls;
                foreach (Control effcon in groupAreaControls)
                {
                    var effCheckboxControl = effcon.Controls[0];
                    if (effCheckboxControl.Width > longestEffectNameWidth)
                        longestEffectNameWidth = effCheckboxControl.Width;
                }
            }
            foreach (Control control in Controls)
            {
                var lineControl = control.Controls[0].Controls[2];
                var labelControl = control.Controls[0].Controls[1];
                var groupAreaControls = control.Controls[1].Controls;
                foreach(Control effcon in groupAreaControls)
                {
                    var effCheckboxControl = effcon.Controls[0];
                    effCheckboxControl.AutoSize = false;
                    effCheckboxControl.Width = longestEffectNameWidth;
                }
                //System.Diagnostics.Debug.WriteLine(control.ClientSize.Width);//should write the same number every time
                int availwidth = control.ClientSize.Width;
                Control c = lineControl;
                while (c!=this)
                {//get usable available width (i.e., the maximum width that can be used without resizing the parent Control)
                    availwidth -= (c.Margin.Left + c.Margin.Right);//Note: does not account for padding.
                    c.Padding = Padding.Empty;//removes padding so we don't have to account for that.
                    c = c.Parent;
                }
                lineControl.Width = availwidth - (control.Controls[0].Padding.Right + labelControl.Bounds.X + labelControl.Bounds.Width + labelControl.Margin.Right);
            }
            this.PerformLayout();
        }
    }
}