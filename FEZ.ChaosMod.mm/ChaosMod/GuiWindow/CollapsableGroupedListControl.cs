using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FezGame.ChaosMod
{
    //TODO probably should be a ContainerControl
    public class CollapsableGroupedListControl : FlowLayoutPanel, IInputGroup<CollapsableGroupControl>
    {
        protected static readonly List<CollapsableGroupedListControl> Instances = new List<CollapsableGroupedListControl>();
        private readonly Dictionary<string, CollapsableGroupControl> groups = new Dictionary<string, CollapsableGroupControl>();

        public Dictionary<string, CollapsableGroupControl> Groups => groups;

        public CollapsableGroupedListControl()
        {
            Instances.Add(this);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.FlowDirection = FlowDirection.TopDown;
            this.AutoScroll = true;
            this.WrapContents = false;
        }
        public void Add(string groupName, params Control[] controls)
        {
            this[groupName].AddRange(controls);
        }

        public CollapsableGroupControl this[string key]
        {
            get
            {
                CollapsableGroupControl g;
                if (groups.ContainsKey(key) && (g = groups[key]) != null)
                    return g;

                g = new CollapsableGroupControl(key, this.Name);
                this.Controls.Add(g);
                groups[key] = g;
                return g;
            }
        }
    }
    public class CollapsableGroupControl : FlowLayoutPanel
    {
        protected static readonly List<CollapsableGroupControl> Instances = new List<CollapsableGroupControl>();

        private readonly FlowLayoutPanel groupContainer;
        private readonly FlowLayoutPanel labelArea;
        private readonly CheckBox showHideButton;
        public string GroupName { get; }
        public FlowLayoutPanel GroupContainer => groupContainer;
        public FlowLayoutPanel LabelArea => labelArea;

        private static readonly ImageList ArrowImageList = new ImageList();

        static CollapsableGroupControl()
        {
            //ArrowImageList.Images.Add((System.Drawing.Image)null);//TODO make showHideButton look nicer without relying on specific characters; see https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-add-or-remove-images-with-the-windows-forms-imagelist-component?view=netframeworkdesktop-4.8
        }

        public static readonly string NameSeperator = ".";
        public CollapsableGroupControl(string groupName, string parentName)
        {
            Instances.Add(this);
            GroupName = groupName;
            this.Name = ((parentName != null && parentName.Length > 0) ? parentName + NameSeperator : "") + GroupName + NameSeperator + "CollapsableGroup";
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Anchor = AnchorStyles.Right | AnchorStyles.Left;
            this.Dock = DockStyle.Top;
            this.WrapContents = false;
            this.FlowDirection = FlowDirection.TopDown;

            //BorderStyle = BorderStyle.FixedSingle;

            labelArea = new FlowLayoutPanel
            {
                Name = Name + NameSeperator + "LabelArea",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                //BorderStyle = BorderStyle.FixedSingle
            };
            labelArea.Dock = DockStyle.Left;

            showHideButton = new System.Windows.Forms.CheckBox
            {
                Name = Name + NameSeperator + "ShowHideButton",
                Appearance = System.Windows.Forms.Appearance.Button,
                Checked = true,
                CheckState = CheckState.Checked,
                ImageList = ArrowImageList,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                AutoSize = true,
            };
            //showHideButton.Height = showHideButton.Width = LabelRowHeight;
            showHideButton.GotFocus += (object sender, EventArgs e) => this.FindForm().ActiveControl = null;
            showHideButton.CheckedChanged += ShowHideButton_CheckedChanged;
            labelArea.Controls.Add(showHideButton);

            var label = new Label
            {
                Name = Name + NameSeperator + "GroupLabel",
                Text = GroupName,
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            };
            labelArea.Controls.Add(label);

            var line = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                //Dock = DockStyle.Fill,
                Height = 4,
                //Width = 100,//this is supposed to go all the way to the right of the containing CollapsableGroupedListControl, but FlowLayoutPanel can't do that, and I don't want to figure out how to work around that. 
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
            };
            labelArea.Controls.Add(line);

            base.Controls.Add(labelArea);

            //TODO maybe add a vertical line thing to better illustrate that the items in the group are a group; dunno the best way to do that

            groupContainer = new FlowLayoutPanel
            {
                Name = Name + NameSeperator + "GroupContainer",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
            };

            //adds a left indent to the left of GroupContainer
            var p = groupContainer.Margin;
            System.Drawing.Graphics g = this.CreateGraphics();
            const float indentemmulti = 1.5f;//the number of em units to indent
            p.Left += (int)(groupContainer.Font.SizeInPoints * indentemmulti / 72 * g.DpiX);//arbitrary indent number; 
            g.Dispose();
            groupContainer.Margin = p;

            base.Controls.Add(groupContainer);

            ShowHideButton_CheckedChanged(null, null);//to set the button visuals properly
        }

        private void ShowHideButton_CheckedChanged(object sender, EventArgs e)
        {
            groupContainer.Visible = showHideButton.Checked;
            //TODO maybe change the "+" and "-" to arrows? 
            if (showHideButton.Checked)
            {
                showHideButton.ImageIndex = 1;
                showHideButton.Text = "\u2796";//"-"
            }
            else
            {
                showHideButton.ImageIndex = 0;
                showHideButton.Text = "\u2795";//"+"
            }
        }

        internal void AddRange(Control[] controls)
        {
            FlowLayoutPanel newentry = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right | AnchorStyles.Left
            };
            foreach (Control control in controls)
                if (control != null) {
                    newentry.Controls.Add(control);
                    ChaosModWindow.LogLineDebug(control.Name);
                }
            groupContainer.Controls.Add(newentry);
        }
    }
}
