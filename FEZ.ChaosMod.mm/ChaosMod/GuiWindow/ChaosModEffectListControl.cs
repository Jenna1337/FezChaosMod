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
        private static readonly bool ignoresubcategories = false;

        private static readonly int RowHeight = 26;
        private static readonly Size spinnerSize = new Size(120, RowHeight);

        private readonly ToolTip tooltip;
        private static readonly string NameSeperator = CollapsableGroupControl.NameSeperator;
        internal AdditionalChaosEffectSettingsWindow AdditionalSettingsWindow = null;

        public ChaosModEffectListControl(FezChaosMod chaosMod) : base()
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
            this.SuspendLayout();
            for (int i = 0; i < chaosMod.ChaosEffectsList.Count; ++i)
            {
                AddEffect(chaosMod.ChaosEffectsList[i]);
            }
            this.ResumeLayout();
            ResizeLabelsSoEverythingLooksNice();

            chaosMod.ChaosEffectAdded += (effect) => { AddEffect(effect); ResizeLabelsSoEverythingLooksNice(); };

            //TODO add a thing to each group to indicate how many are enabled
            //TODO indicate if/when an effect can start?; maybe change the color of the text or something; could maybe change if the "activate effect" button is enabled
            //TODO add a thing to enable/disable all the effects in a category; maybe a checkbox to the right of the collapse button? dunno if that'd be too confusing for users
        }
        private static readonly Regex subcatregex = new Regex(@"(?<=\D)\.(?=\D)");//TODO review this regex
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
            enabledCheckBox.CheckedChanged += new EventHandler((object sender, EventArgs e) => { effect.Enabled = enabledCheckBox.Checked; });

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
                Size = spinnerSize,
                //TabIndex = 5;
                TextAlign = System.Windows.Forms.HorizontalAlignment.Right,
                UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left,
                Value = (decimal)effect.Ratio
            };
            ratioSpinner.ValueChanged += new EventHandler((object sender, EventArgs e) => { effect.Ratio = Decimal.ToDouble(ratioSpinner.Value); });
            tooltip.SetToolTip(ratioSpinner, "Ratio");

            Control durationSpinner = null;
            if (effect.Duration > 0)
            {
                durationSpinner = new NumericUpDown
                {
                    DecimalPlaces = 4,
                    Maximum = new decimal(new int[] {
                    1000,
                    0,
                    0,
                    0}),
                    Name = effect.Name + NameSeperator + "DurationSpinner",
                    Size = spinnerSize,
                    TextAlign = System.Windows.Forms.HorizontalAlignment.Right,
                    UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left,
                    Value = (decimal)effect.Duration
                };
                ((NumericUpDown)durationSpinner).ValueChanged += new EventHandler((object sender, EventArgs e) => { effect.Duration = decimal.ToDouble(((NumericUpDown)durationSpinner).Value); });
                tooltip.SetToolTip(durationSpinner, "Duration");
            }
            else
            {
                durationSpinner = new Control
                {
                    Size = spinnerSize,
                    AutoSize = false,
                };
            }

            //TODO Note: clicking activateEffectButton on certain effects before starting or continuing a save can cause crashes; should probably disable these buttons until the game actually starts
            Button activateEffectButton = new Button
            {
                Text = "Start",
                Width = 57,
                Height = RowHeight,
            };
            tooltip.SetToolTip(activateEffectButton, "Forcibly activates the effect\nWarning: might break the game!");

            Button terminateEffectButton = new Button
            {
                Text = "End",
                Enabled = false,
                Width = 57,
                Height = RowHeight,
            };
            tooltip.SetToolTip(terminateEffectButton, "Forcibly terminates and removes all instances of this effect.");

            activateEffectButton.Click += new EventHandler((object sender, EventArgs e) =>
            {
                effect.Activate();
            });
            FezChaosMod.Instance.ChaosEffectActivated += (eff) => { if (eff.Name == effect.Name) { activateEffectButton.Enabled = eff.IsDone; } };
            FezChaosMod.Instance.ChaosEffectEnded += (eff) => { if (eff.Name == effect.Name) { activateEffectButton.Enabled = eff.IsDone; } };

            terminateEffectButton.Click += new EventHandler((object sender, EventArgs e) =>
            {
                effect.Terminate();
            });
            FezChaosMod.Instance.ChaosEffectActivated += (eff) => { if (eff.Name == effect.Name) { terminateEffectButton.Enabled = !eff.IsDone; } };
            FezChaosMod.Instance.ChaosEffectEnded += (eff) => { if (eff.Name == effect.Name) { terminateEffectButton.Enabled = !eff.IsDone; } };

            Control additionalSettingsButton = null;
            if (effect.AdditionalSettings != null)
            {
                additionalSettingsButton = new Button
                {
                    Text = "...",
                    Width = 35,
                    Height = RowHeight,
                };
                additionalSettingsButton.Font = new Font(additionalSettingsButton.Font.Name, additionalSettingsButton.Font.Size * 1.3f);
                tooltip.SetToolTip(additionalSettingsButton, "Additional Settings");

                AdditionalSettingsWindow = new AdditionalChaosEffectSettingsWindow(effect.AdditionalSettings)
                {
                    Text = effect.Name + " Additional Settings"
                };
                additionalSettingsButton.Click += new EventHandler((object sender, EventArgs e) => { _ = AdditionalSettingsWindow.ShowDialog(); });
            }
            else
            {
                additionalSettingsButton = new Control
                {
                    AutoSize = false,
                    Width = 35,
                    Height = RowHeight,
                };
            }

            string category = effect.Category != null && effect.Category.Length > 0 ? effect.Category : "Uncategorized";
            string displayname = category;
            string[] categories = subcatregex.Split(category);

            CollapsableGroupedListControl container = this;
            if (!ignoresubcategories && categories.Length != 1)
            {
                //adds subcategories for groups that match subcatregex

                int lastcatindex = categories.Length - 1;
                displayname = categories[lastcatindex];
                string firstcat = categories[0];

                for (int i = 0; i < lastcatindex; ++i)
                {
                    string subcatname = String.Join(NameSeperator, categories.Take(i+1));
                    CollapsableGroupControl subcat = container[subcatname];
                    subcat.Label.Text = categories[i];
                    subcat.GroupContainer.Dock = DockStyle.Fill;
                    var controls = subcat.GroupContainer.Controls;
                    if (controls.Count <= 0)
                    {
                        var newlist = new CollapsableGroupedListControl(container)
                        {
                            AutoScroll = false
                        };
                        controls.Add(newlist);
                    }
                    container = (CollapsableGroupedListControl)controls[0];
                }
            }
            container.Add(category, enabledCheckBox, ratioSpinner, durationSpinner, activateEffectButton, terminateEffectButton, additionalSettingsButton);
            container[category].Label.Text = displayname;
            this.PerformLayout();
        }
        private void ResizeLabelsSoEverythingLooksNice()
        {
            this.PerformLayout();
            int longestEffectNameWidth = 0;
            foreach (CollapsableGroupControl control in Instances.SelectMany(g => g.Groups.Values))
            {
                Label labelControl = control.Label;
                var groupAreaControls = control.GroupContainer.Controls;
                foreach (Control effcon in groupAreaControls)
                {
                    if (typeof(CollapsableGroupedListControl).IsAssignableFrom(effcon.GetType()))
                    {
                        continue;
                    }
                    var effCheckboxControl = effcon.Controls[0];
                    if (effCheckboxControl.Width > longestEffectNameWidth)
                        longestEffectNameWidth = effCheckboxControl.Width;
                }
            }
            foreach (CollapsableGroupControl control in Instances.SelectMany(g => g.Groups.Values))
            {
                Label labelControl = control.Label;
                var groupAreaControls = control.GroupContainer.Controls;
                foreach (Control effcon in groupAreaControls)
                {
                    if (typeof(CollapsableGroupedListControl).IsAssignableFrom(effcon.GetType()))
                    {
                        continue;
                    }
                    var effCheckboxControl = effcon.Controls[0];
                    effCheckboxControl.AutoSize = false;
                    effCheckboxControl.Width = longestEffectNameWidth;
                }
            }
            this.RefreshLayout();
        }
    }
}
