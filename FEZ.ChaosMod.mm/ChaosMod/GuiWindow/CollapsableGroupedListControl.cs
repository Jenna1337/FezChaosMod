using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FezGame.ChaosMod
{
    //TODO probably should be a ContainerControl, but then would have to figure out how to layout all the stuff myself
    public class CollapsableGroupedListControl : FlowLayoutPanel, IInputGroup<CollapsableGroupControl>
    {
        protected static readonly List<CollapsableGroupedListControl> Instances = new List<CollapsableGroupedListControl>();

        public Dictionary<string, CollapsableGroupControl> Groups { get; } = new Dictionary<string, CollapsableGroupControl>();
        public readonly CollapsableGroupedListControl ParentList = null;
        public CollapsableGroupedListControl GetTopParentList()
        {
            var p = ParentList;
            if (p != null)
            {
                while (p.ParentList != null)
                {
                    p = p.ParentList;
                }
            }
            return ParentList;
        }

        public CollapsableGroupedListControl()
        {
            Instances.Add(this);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.FlowDirection = FlowDirection.TopDown;
            this.AutoScroll = true;
            this.WrapContents = false;
        }
        public CollapsableGroupedListControl(CollapsableGroupedListControl parentList) : this()
        {
            this.ParentList = parentList;
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
                if (Groups.ContainsKey(key) && (g = Groups[key]) != null)
                    return g;

                g = new CollapsableGroupControl(key, this.Name);
                this.Controls.Add(g);
                Groups[key] = g;
                return g;
            }
        }

        public void RefreshLayout()
        {
            System.Diagnostics.Debugger.Break();
            var tp = GetTopParentList();
            if (tp != null)
            {
                tp.RefreshLayout();
                return;
            }
            else
            {
                this.ResumeLayout(false);
                this.PerformLayout();
                this.SuspendLayout();
                return;
            }
        }
    }
    //TODO probably should be a ContainerControl, but then would have to figure out how to layout all the stuff myself
    public class CollapsableGroupControl : FlowLayoutPanel
    {
        public string GroupName { get; }
        public FlowLayoutPanel GroupContainer { get; }
        public FlowLayoutPanel LabelArea { get; }
        public CheckBox ShowHideButton { get; }
        public Label Label { get; }

        private static readonly ImageList ArrowImageList = new ImageList();

        static CollapsableGroupControl()
        {
            //ArrowImageList.Images.Add((System.Drawing.Image)null);//TODO make showHideButton look nicer without relying on specific characters; see https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-add-or-remove-images-with-the-windows-forms-imagelist-component?view=netframeworkdesktop-4.8
        }

        public static readonly string NameSeperator = ".";
        public CollapsableGroupControl(string groupName, string parentName)
        {
            GroupName = groupName;
            this.Name = ((parentName != null && parentName.Length > 0) ? parentName + NameSeperator : "") + GroupName + NameSeperator + "CollapsableGroup";
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Anchor = AnchorStyles.Right | AnchorStyles.Left;
            this.Dock = DockStyle.Top;
            this.WrapContents = false;
            this.FlowDirection = FlowDirection.TopDown;

            //BorderStyle = BorderStyle.FixedSingle;

            LabelArea = new FlowLayoutPanel
            {
                Name = Name + NameSeperator + "LabelArea",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                //BorderStyle = BorderStyle.FixedSingle
            };
            LabelArea.Dock = DockStyle.Left;

            ShowHideButton = new System.Windows.Forms.CheckBox
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
            ShowHideButton.GotFocus += (object sender, EventArgs e) => this.FindForm().ActiveControl = null;
            ShowHideButton.CheckedChanged += ShowHideButton_CheckedChanged;
            LabelArea.Controls.Add(ShowHideButton);

            Label = new Label
            {
                Name = Name + NameSeperator + "GroupLabel",
                Text = GroupName,
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            };
            LabelArea.Controls.Add(Label);

            this.Paint += CollapsableGroupControl_Paint;

            base.Controls.Add(LabelArea);

            GroupContainer = new FlowLayoutPanel
            {
                Name = Name + NameSeperator + "GroupContainer",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
            };

            //adds a left indent to the left of GroupContainer
            var p = GroupContainer.Margin;
            System.Drawing.Graphics g = this.CreateGraphics();
            const float indentemmulti = 1.5f;//the number of em units to indent
            p.Left += (int)(GroupContainer.Font.SizeInPoints * indentemmulti / 72 * g.DpiX);//indents by indentemmulti em units
            g.Dispose();
            GroupContainer.Margin = p;

            base.Controls.Add(GroupContainer);

            ShowHideButton_CheckedChanged(null, null);//to set the button visuals properly
        }

        private static Brush linePen = new Pen(Color.DarkGray).Brush;
        private void CollapsableGroupControl_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            const float thickness = 2;//the thickness of the group lines

            //Horizontal line
            //TODO this is supposed to go all the way to the right of the TopParentList CollapsableGroupedListControl; seems FlowLayoutPanel does not allow for that.
            float hx = Label.Location.X + Label.Width;
            float hy = ShowHideButton.Location.Y + ShowHideButton.Height / 2f - thickness / 2f;
            g.FillRectangle(linePen, hx, hy, this.Width - hx, thickness);

            //Vertical line
            float vx = ShowHideButton.Location.X + ShowHideButton.Width / 2f - thickness / 2f;
            float vy = ShowHideButton.Location.Y + ShowHideButton.Height;
            g.FillRectangle(linePen, vx, vy, thickness, this.Width - vy);
        }

        private void ShowHideButton_CheckedChanged(object sender, EventArgs e)
        {
            GroupContainer.Visible = ShowHideButton.Checked;
            //TODO maybe change the "+" and "-" to arrows? 
            if (ShowHideButton.Checked)
            {
                ShowHideButton.ImageIndex = 1;
                ShowHideButton.Text = "\u2796";//"-"
            }
            else
            {
                ShowHideButton.ImageIndex = 0;
                ShowHideButton.Text = "\u2795";//"+"
            }
            if (Parent is CollapsableGroupedListControl cglc)
            {
                cglc.RefreshLayout();
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
            GroupContainer.Controls.Add(newentry);
        }
    }
}
