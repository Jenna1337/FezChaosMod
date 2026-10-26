using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FezGame.ChaosMod
{
    public class IniSection : Dictionary<string, string> { }
    public class IniFile : Dictionary<string, IniSection>
    {
        /// <summary>
        /// Searches globally across all sections for the first matching key.
        /// </summary>
        public bool TryGetValue(string key, out string value, params string[] sectionsToCheckFirst)
        {
            if (sectionsToCheckFirst != null)
            {
                foreach (var sectionName in sectionsToCheckFirst)
                {
                    if (this.TryGetValue(sectionName, out IniSection section))
                    {
                        if (section.TryGetValue(key, out value))
                        {
                            return true;
                        }
                    }
                }
            }
            foreach (var section in this.Values)
            {
                if (section.TryGetValue(key, out value))
                {
                    return true;
                }
            }
            value = null;
            return false;
        }
    }
    static class ChaosModSettingsHelper
    {
        internal static readonly string MetadataSectionName = "Metadata";
        private static readonly string FezChaosModVersionName = "ChaosMod.Version";
        /// <summary>
        /// Loads chaos mod settings file
        /// </summary>
        /// <param name="settings">The ChaosModSettings into which the settings will be loaded</param>
        /// <param name="filepath">The file path of the settings file</param>
        public static void Read(ChaosModSettings settings, string filepath)
        {
            IniFile ctrlvals = ReadIniFile(filepath);

#if !DEBUG
            ChaosModWindow.ClearLog();
#endif

            bool IsCorrectChaosVerion = false;
            if (ctrlvals.TryGetValue(FezChaosModVersionName, out string FileChaosVersion))
                IsCorrectChaosVerion = FezChaosMod.Version.Equals(FileChaosVersion = FileChaosVersion.Trim());
            if (!IsCorrectChaosVerion)
                ChaosModWindow.LogLine($"Warning: The loaded file's {FezChaosModVersionName} (\"{FileChaosVersion}\") did not match the expected value \"{FezChaosMod.Version}\".");
            settings.SetAllInputsValues(ctrlvals);
        }
        /// <summary>
        /// Saves chaos mod settings file
        /// </summary>
        /// <param name="settings">The ChaosModSettings from which the settings will be saved</param>
        /// <param name="filepath">The file path to save the settings file</param>
        public static void Write(ChaosModSettings settings, string filepath)
        {
            var ctrlvals = settings.GetAllInputsValues();
            WriteIniFile(filepath, ctrlvals);
        }


        private static readonly char IniKeyValDelimiter = '=';

        //note: does not support multiline values
        private static IniFile ReadIniFile(string filepath)
        {
            var inidata = new IniFile();
            string[] lines = File.ReadAllLines(filepath, System.Text.Encoding.UTF8);
            string section = "";
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(";") || trimmed.Length <= 0)
                {
                    continue;
                }

                if (trimmed.StartsWith("["))
                {
                    section = Regex.Match(trimmed, @"\[(.*?)(?:\]|$)").Groups[1].Value.Trim();
                    if (!inidata.ContainsKey(section))
                        inidata[section] = new IniSection();
                    continue;
                }

                var kvmatch = Regex.Match(trimmed, $@"(.*?)(?:{IniKeyValDelimiter}(.*))?$");
                var readValue = kvmatch.Groups[2].Success ? kvmatch.Groups[2].Value : "";
                if (!string.IsNullOrEmpty(readValue))
                {
                    inidata[section][kvmatch.Groups[1].Value.Trim()] = readValue;
                }
            }
            return inidata;
        }
        //note: does not support multiline values
        private static void WriteIniFile(string filepath, IniFile dict)
        {
            List<string> lines = new List<string>()
            {
                "; Chaos Mod settings",
                "",
                $"[{MetadataSectionName}]",
                $"{FezChaosModVersionName}{IniKeyValDelimiter}{FezChaosMod.Version}",
                ""
            };
            foreach (var dd in dict)//.OrderBy(dd => dd.Key))
            {
                if (!string.IsNullOrEmpty(dd.Key))
                {
                    lines.Add($"[{dd.Key}]");
                }

                foreach (var d in dd.Value)//.OrderBy(d=>d.Key))
                {
                    lines.Add($"{d.Key}{IniKeyValDelimiter}{d.Value}");
                }
                lines.Add("");
            }
            File.WriteAllLines(filepath, lines, System.Text.Encoding.UTF8);
        }
    }
}
