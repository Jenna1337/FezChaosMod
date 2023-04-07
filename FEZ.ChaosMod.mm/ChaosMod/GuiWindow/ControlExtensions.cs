using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

namespace FezGame.ChaosMod
{
    /// <summary>
    /// An interface that tells the ControlExtensions that the implementing component should be treated as a group of components
    /// </summary>
    public interface IInputGroup<T> where T : Control
    {
        Dictionary<string, T> Groups { get; }
        T this[string key] { get; }
    }
    public static class ControlExtensions
    {
        public static readonly string InputGroupInputGroupSeperator = "---";
        public static bool? AsNullableBool(this CheckState checkState)
        {
            switch (checkState)
            {
            case CheckState.Checked:
                return true;
            case CheckState.Unchecked:
                return false;
            default:
                return null;
            }
        }
        public static bool IsInputGroup(this Control control)
        {
            var T = typeof(IInputGroup<>);
            return control.GetType().GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == T);
        }
        [Obsolete("This method is not recommended. Check the Count of control.GetAllInputs() instead.", false)]//TODO remove in version 0.9.6
        public static bool HasMultipleInputValues(this Control control)
        {
            return control.GetAllInputs().Count() > 1;
        }
        public static bool IsInput(this Control control)
        {
            if (control.Name != "")
            {
                if (
                control.IsInputGroup() ||
                control is CheckBox ||
                control is CheckedListBox ||
                control is ComboBox ||
                control is DateTimePicker ||
                control is ListBox ||
                control is MonthCalendar ||
                control is NumericUpDown ||
                control is RadioButton)
                {
                    return true;
                }
                else if (control is TextBoxBase)
                {
                    return !(control as TextBoxBase).ReadOnly;
                }
            }
            return false;
        }
        [Obsolete]//TODO remove in version 0.9.6
        public static TabPage GetParentTabPage(this Control control)
        {
            return control == null ? null : (control is TabPage ? control as TabPage : GetParentTabPage(control.Parent));
        }
        /// <summary>
        /// Gets all child input <see cref="Control"/> elements.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="IgnoreInInputGroup">If the children of controls implementing the <see cref="IInputGroup{T}"/> should be ignored.</param>
        /// <returns>An <see cref="IEnumerable{Control}"/></returns>
        public static IEnumerable<Control> GetAllInputs(this Control root, bool IgnoreInInputGroup = true)
        {
            var controls = root.Controls.Cast<Control>();
            var inputs = controls;

            if (IgnoreInInputGroup)
            {
                inputs = inputs.Where(c => !(c.IsInputGroup()));
            }

            inputs = inputs.SelectMany(ctrl => ctrl.GetAllInputs(IgnoreInInputGroup))
                .Concat(controls.Where(c => c.IsInput()));

            return inputs;
        }
        /// <summary>
        /// Gets all values for the designated input <see cref="Control"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="prependControlNameIfMultivalued"></param>
        /// <returns>
        /// <para>A Dictionary object containing all (if any) name(s) and corresponding value(s) of the input.<br/>
        /// <br/>
        /// For most input <see cref="Control"/> objects this will return a Dictionary with only a singular key/value pair,
        /// in which case the key is the Name of this <see cref="Control"/>, and the value is the singular value of the control.</para>
        /// </returns>
        public static Dictionary<string, object> GetValues(this Control control, bool prependControlNameIfMultivalued)
        {
            var values = new Dictionary<string, object>();

            if (control.IsInputGroup())
            {
                var vallist = control.GetAllInputsValues(true, false);
                foreach (var giv in vallist)
                {
                    foreach (var kv in giv.Value)
                    {
                        var k = kv.Key;
                        k = k != null && k.Length > 0 ? k : giv.Key;
                        values.Add(k, kv.Value);
                    }
                    ;
                }
            }

            switch (control)//use "if is's" instead of a switch so as to support derrived types?
            {
            case CheckBox C:
                values.Add(control.Name, C.Checked);//Note: not using C.CheckState.AsNullableBool() as it could return null, which is not supported by SetValues
                break;
            case ComboBox C:
                values.Add(control.Name, C.SelectedItem);
                break;
            case DateTimePicker C:
                values.Add(control.Name, C.Value);
                break;
            case CheckedListBox C:
                {
                    var items = C.Items;
                    var checkedindices = C.CheckedIndices;
                    for (int i = 0; i < items.Count; i++)
                    {
                        object item = items[i];
                        values.Add(item.ToString(), checkedindices.Contains(i));
                    }
                }
                break;
            case ListBox C:
                {
                    var items = C.Items;
                    var selectedindices = C.SelectedIndices;
                    for (int i = 0; i < items.Count; i++)
                    {
                        object item = items[i];
                        values.Add(item.ToString(), selectedindices.Contains(i));
                    }
                }
                break;
            case MonthCalendar C:
                values.Add("Start", C.SelectionStart);
                values.Add("End", C.SelectionEnd);
                break;
            case NumericUpDown C:
                values.Add(control.Name, C.Value);
                break;
            case RadioButton C:
                values.Add(control.Name, C.Checked);
                break;
            case MaskedTextBox C:
                values.Add(control.Name, C.Text);
                break;
            case RichTextBox C:
                values.Add(control.Name, C.Text);
                break;
            case TextBox C:
                values.Add(control.Name, C.Text);
                break;
            }

            if (prependControlNameIfMultivalued && values.Count > 1)
            {
                var newvalues = new Dictionary<string, object>();
                foreach (var kv in values)
                {
                    newvalues[control.Name + InputGroupInputGroupSeperator + kv.Key] = kv.Value;
                }
                values = newvalues;
            }

            return values;
        }
        public static void SetValues(this Control control, Dictionary<string, string> vals)
        {
            if (control.IsInputGroup())
            {
                var cglinputs = control.GetAllInputs(false);
                foreach (var gi in cglinputs)
                {
                    if (vals.TryGetValue(gi.Name, out string val))
                    {
                        gi.SetValues(new Dictionary<string, string>
                        {
                            { gi.Name, val }
                        });
                    }
                    else
                    {
                        switch (control)
                        //TODO none of these will ever be true, but should probably do something with it since SetValues currently doesn't support prependControlNameIfMultivalued
                        {
                        case CheckedListBox C:
                            for (int i = 0; i < C.Items.Count; i++)
                                if (vals.TryGetValue(gi.Name + InputGroupInputGroupSeperator + C.Items[i].ToString(), out val))
                                    C.SetItemChecked(i, bool.Parse(val));
                            break;
                        case ListBox C:
                            for (int i = 0; i < C.Items.Count; i++)
                                if (vals.TryGetValue(gi.Name + InputGroupInputGroupSeperator + C.Items[i].ToString(), out val))
                                    C.SetSelected(i, bool.Parse(val));
                            break;
                        case MonthCalendar C:
                            if (vals.TryGetValue(gi.Name + InputGroupInputGroupSeperator + "Start", out string start))
                                C.SelectionStart = DateTime.Parse(start);
                            if (vals.TryGetValue(gi.Name + InputGroupInputGroupSeperator + "End", out string end))
                                C.SelectionEnd = DateTime.Parse(end);
                            break;

                        }
                    }
                }
            }

            switch (control)//use "if is's" instead of a switch so as to support derrived types?
            {
            case CheckBox C:
                C.Checked = bool.Parse(vals.Single().Value);
                break;
            case ComboBox C:
                C.SelectedItem = vals.Single().Value;
                break;
            case DateTimePicker C:
                C.Value = DateTime.Parse(vals.Single().Value);
                break;
            case CheckedListBox C:
                for (int i = 0; i < C.Items.Count; i++)
                    if (vals.TryGetValue(C.Items[i].ToString(), out string val))
                        C.SetItemChecked(i, bool.Parse(val));
                break;
            case ListBox C:
                for (int i = 0; i < C.Items.Count; i++)
                    if (vals.TryGetValue(C.Items[i].ToString(), out string val))
                        C.SetSelected(i, bool.Parse(val));
                break;
            case MonthCalendar C:
                if (vals.TryGetValue("Start", out string start))
                    C.SelectionStart = DateTime.Parse(start);
                if (vals.TryGetValue("End", out string end))
                    C.SelectionEnd = DateTime.Parse(end);
                break;
            case NumericUpDown C:
                C.Value = decimal.Parse(vals.Single().Value);
                break;
            case RadioButton C:
                C.Checked = bool.Parse(vals.Single().Value);
                break;
            case MaskedTextBox C:
                C.Text = vals.Single().Value;
                break;
            case RichTextBox C:
                C.Text = vals.Single().Value;
                break;
            case TextBox C:
                C.Text = vals.Single().Value;
                break;
            }
        }
        /// <summary>
        /// Gets all the values for all <see cref="Control.Name">Named</see> input <see cref="Control"/> elements contained in this <see cref="Control"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="prependControlNameIfMultivalued"></param>
        /// <param name="IgnoreInInputGroup">If the children of controls implementing the <see cref="IInputGroup{T}"/> should be ignored.</param>
        /// <returns>
        /// A Dictionary object where the key is the Name of the input <see cref="Control"/>, and the value is the result of <see cref="GetAllInputs(Control, bool)"/>
        /// </returns>
        public static Dictionary<string, Dictionary<string, object>> GetAllInputsValues(this Control control, bool prependControlNameIfMultivalued = false, bool IgnoreInInputGroup = true)
        {
            var i = control.GetAllInputs(IgnoreInInputGroup);
            return i.ToDictionary(c => c.Name, c => c.GetValues(prependControlNameIfMultivalued));
        }
        public static void SetAllInputsValues(this Control root, Dictionary<string, Dictionary<string, object>> ctrlvals)
        {
            SetAllInputsValues(root, ctrlvals.ToDictionary(c => c.Key, c => c.Value.ToDictionary(v => v.Key, v => v.Value.ToString())));
        }
        public static void SetAllInputsValues(this Control root, Dictionary<string, Dictionary<string, string>> ctrlvals)
        {
            List<Control> controls = root.GetAllInputs(false).ToList();
            foreach (var control in controls)
                if (ctrlvals.TryGetValue(control.Name, out Dictionary<string, string> vals))
                    control.SetValues(vals);
        }
    }
}
