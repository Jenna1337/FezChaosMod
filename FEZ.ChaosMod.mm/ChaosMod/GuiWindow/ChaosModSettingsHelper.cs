using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using FezGame.GameInfo;
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
        /// <param name="chaosModWindow">The ChaosModWindow into which the settings will be loaded</param>
        /// <param name="filepath">The file path of the settings file</param>
        public static void Read(ChaosModWindow chaosModWindow, string filepath)
        {
            Dictionary<string, Dictionary<string, string>> ctrlvals = ReadIniFile(filepath);

#if !DEBUG
            ChaosModWindow.ClearLog();
#endif

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
                if (trimmed.StartsWith(";") || trimmed.Length <= 0)
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
            foreach (var dd in dict.OrderBy(dd => dd.Key))
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
