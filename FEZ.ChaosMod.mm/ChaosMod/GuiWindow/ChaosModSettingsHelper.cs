using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace FezGame.ChaosMod
{
    static class ChaosModSettingsHelper
    {
        private static readonly string MetadataSectionName = "Metadata";
        private static readonly string FezChaosModVersionName = "ChaosMod.Version";
        /// <summary>
        /// Loads chaos mod settings file
        /// </summary>
        /// <param name="settings">The ChaosModSettings into which the settings will be loaded</param>
        /// <param name="filepath">The file path of the settings file</param>
        public static void Read(ChaosModSettings settings, string filepath)
        {
            Dictionary<string, string> ctrlvals = ReadIniFile(filepath);

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
            Dictionary<string, object> ctrlvals = settings.GetAllInputsValues(false);
            WriteIniFile(filepath, ctrlvals);
        }


        private static readonly char IniKeyValDelimiter = '=';

        //note: does not support multiline values
        private static Dictionary<string, string> ReadIniFile(string filepath)
        {
            var inidata = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(filepath, System.Text.Encoding.UTF8);
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(";") || trimmed.Length <= 0)
                    continue;
                var kvmatch = Regex.Match(trimmed, $@"(.*?)(?:{IniKeyValDelimiter}(.*))?$");
                var readValue = kvmatch.Groups[2].Success ? kvmatch.Groups[2].Value : "";
                if (!string.IsNullOrEmpty(readValue))
                {
                    inidata[kvmatch.Groups[1].Value.Trim()] = readValue;
                }
            }
            return inidata;
        }
        //note: does not support multiline values
        private static void WriteIniFile(string filepath, Dictionary<string, object> dict)
        {
            List<string> lines = new List<string>()
            {
                "; Chaos Mod settings",
                "",
                $"[{MetadataSectionName}]",
                $"{FezChaosModVersionName}{IniKeyValDelimiter}{FezChaosMod.Version}",
                ""
            };
            foreach (var d in dict)//.OrderBy(dd => dd.Key))
            {
                string formattedValue = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", d.Value);
                lines.Add($"{d.Key}{IniKeyValDelimiter}{d.Value.ToString()}");
            }
            File.WriteAllLines(filepath, lines, System.Text.Encoding.UTF8);
        }
    }
}
