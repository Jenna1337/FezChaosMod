using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using FezGame.GameInfo;
using System.Text.RegularExpressions;

namespace FezGame.ChaosMod
{
    /// <summary>
    /// An interface that tells the SettingsHelper that the implementing component should be treated as a group of components
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
            switch(checkState)
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
        public static bool HasMultipleInputValues(this Control control)
        {
            return control.GetAllInputs().Count() > 1;
        }
        public static bool IsInput(this Control control)
        {
            if(control.Name!="")
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
        public static TabPage GetParentTabPage(this Control control)
        {
            return control == null ? null : (control is TabPage ? control as TabPage : GetParentTabPage(control.Parent));
        }
        public static IEnumerable<Control> GetAllInputs(this Control root, bool IgnoreInInputGroup = true)
        {
            var controls = root.Controls.Cast<Control>();
            var inputs = controls;

            if (IgnoreInInputGroup)
            {
                inputs = inputs.Where(c => !(c.IsInputGroup()));
            }

            inputs = inputs.SelectMany(ctrl => ctrl.GetAllInputs())
                .Concat(controls.Where(c => c.IsInput()));

            return inputs;
        }
        public static Dictionary<string, object> GetValues(this Control control, bool prependControlNameIfMultivalued)
        {
            var values = new Dictionary<string, object>();

            if (control.IsInputGroup())
            {
                var vallist = control.GetAllInputsValues(true);
                foreach(var giv in vallist)
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

            if(prependControlNameIfMultivalued && values.Count > 1)
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
                var cglinputs = control.GetAllInputs();
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
        public static Dictionary<string, Dictionary<string, object>> GetAllInputsValues(this Control control, bool prependControlNameIfMultivalued = false)
        {
            var i = control.GetAllInputs();
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
    static class ChaosModSettingsHelper
    {
        private static readonly string MetadataSectionName = "Metadata";
        private static readonly string FezChaosModVersionName = "ChaosMod.Version";
        /// <summary>
        /// Loads chaos mod settings file
        /// </summary>
        /// <param name="chaosModWindow">The ChaosModWindow into which the settings will be loaded</param>
        /// <param name="filepath">The file path of the settings file</param>
        public static void Read(ChaosModWindow chaosModWindow, string filepath)
        {
            Dictionary<string, Dictionary<string, string>> ctrlvals = ReadIniFile(filepath);

            ChaosModWindow.ClearLog();

            if (ctrlvals.TryGetValue(MetadataSectionName, out Dictionary<string, string> metavals))
            {
                bool IsCorrectChaosVerion = false;
                if (metavals.TryGetValue(FezChaosModVersionName, out string FileChaosVersion))
                    IsCorrectChaosVerion = FezChaosMod.Version.Equals(FileChaosVersion = FileChaosVersion.Trim());
                if (!IsCorrectChaosVerion)
                    ChaosModWindow.LogLine($"Warning: The loaded file's {FezChaosModVersionName} (\"{FileChaosVersion}\") did not match the expected value \"{FezChaosMod.Version}\".");

            }
            chaosModWindow.SetAllInputsValues(ctrlvals);
        }
        /// <summary>
        /// Saves chaos mod settings file
        /// </summary>
        /// <param name="chaosModWindow">The ChaosModWindow from which the settings will be saved</param>
        /// <param name="filepath">The file path to save the settings file</param>
        public static void Write(ChaosModWindow chaosModWindow, string filepath)
        {
            Dictionary<string, Dictionary<string, object>> ctrlvals = chaosModWindow.GetAllInputsValues(false);
            WriteIniFile(filepath, ctrlvals);
        }


        private static readonly char IniKeyValDelimiter = '=';

        /*
         * Note: Ini syntax is as follows:
         * 
         * [ControlName]
         * InputName=InputValue
         * 
         */

        //note: does not support multiline values
        private static Dictionary<string, Dictionary<string, string>> ReadIniFile(string filepath)
        {
            var inidata = new Dictionary<string, Dictionary<string, string>>();
            string[] lines = File.ReadAllLines(filepath, System.Text.Encoding.UTF8);
            string section = "";
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(";") || trimmed.Length<=0)
                    continue;
                if (trimmed.StartsWith("["))
                {
                    section = Regex.Match(trimmed, @"\[(.*?)(?:\]|$)").Groups[1].Value.Trim();
                    if (!inidata.ContainsKey(section))
                        inidata[section] = new Dictionary<string, string>();
                    continue;
                }
                if (!inidata.ContainsKey(section))
                    inidata[section] = new Dictionary<string, string>();
                var kvmatch = Regex.Match(trimmed, $@"(.*?)(?:{IniKeyValDelimiter}(.*))?$");
                inidata[section][kvmatch.Groups[1].Value.Trim()] = kvmatch.Groups[2].Success ? kvmatch.Groups[2].Value : "";
            }
            return inidata;
        }
        //note: does not support multiline values
        private static void WriteIniFile(string filepath, Dictionary<string, Dictionary<string, object>> dict)
        {
            List<string> lines = new List<string>()
            {
                "; Chaos Mod settings",
                "",
                $"[{MetadataSectionName}]",
                $"{FezChaosModVersionName}{IniKeyValDelimiter}{FezChaosMod.Version}",
                ""
            };
            foreach(var dd in dict.OrderBy(dd=>dd.Key))
            {
                lines.Add($"[{dd.Key}]");
                foreach (var d in dd.Value)//.OrderBy(d=>d.Key))
                    lines.Add($"{d.Key}{IniKeyValDelimiter}{d.Value}");
                lines.Add("");
            }
            File.WriteAllLines(filepath, lines, System.Text.Encoding.UTF8);
        }
        //TODO add XML support?
    }
}
