using FezGame.ChaosMod;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;

namespace FezGame
{
    public class ChaosModWindow
    {
        public static ChaosModWindow Instance { get; private set; }

        // Non-auto property for ImGui boolean ref passing (prevents CS0206)
        public bool IsOpen = false;

        private readonly FezChaosMod chaosMod;
        private readonly ChaosModEffectListControl effectListUI;
        private readonly ChaosModAboutWindow aboutWindow = new ChaosModAboutWindow();

        // UI state fields mapping to old control names for INI compatibility
        public bool chaosModeEnabled = true;
        public float delayBetweenEffects = 0f;
        public float durationMultiplier = 1f;
        public int maxEffectsToDisplay = 10;
        public int latestEffectsToDisplay = 5;

        public bool allowRotateAnywhere = false;
        public bool firstPersonAnywhere = false;
        public bool stereoMode = false;
        public bool zuSpeakEnglish = false;
        public bool showDebugInfo = true;

        private string activeSaveFile = null;
        private static string loggerText = "";
        private const int MaxLogLines = 1000;

        public ChaosModWindow(FezChaosMod chaosMod)
        {
            if (Instance != null)
                Instance = null;
            Instance = this;

            this.chaosMod = chaosMod;
            this.effectListUI = new ChaosModEffectListControl(chaosMod);

            // Load initial values from mod instance
            showDebugInfo = chaosMod.ShowDebugInfo;
            allowRotateAnywhere = chaosMod.AllowRotateAnywhere;
            delayBetweenEffects = (float)chaosMod.DelayBetweenEffects;
            durationMultiplier = (float)chaosMod.EffectsDurationMultiplier;
            maxEffectsToDisplay = chaosMod.MaxActiveEffectsToDisplay;
            latestEffectsToDisplay = chaosMod.LatestEffectsToDisplay;

            LogLine("Initialization complete.");
            LogLine($"Welcome to Chaos Mod version {FezChaosMod.Version}");
        }

        public static void LogLine(string text)
        {
            Common.Logger.Log("ChaosMod", text);
            System.Diagnostics.Debug.WriteLine(text);

            if (Instance == null) return;

            string[] lines = loggerText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= MaxLogLines)
            {
                loggerText = string.Join("\n", lines, 1, lines.Length - 1) + "\n" + text + "\n";
            }
            else
            {
                loggerText += text + "\n";
            }
        }

        public static void LogLineDebug(string text)
        {
            if (Instance == null || !Instance.chaosMod.ShowDebugInfo)
            {
                if (Instance == null) Common.Logger.Log("ChaosMod", text);
                return;
            }
            LogLine("Debug: " + text);
        }

        public static void ClearLog()
        {
            loggerText = string.Empty;
        }

        public void Draw()
        {
            if (!IsOpen) return;

            // Sync stereoscopic mode live from engine service if active
            try
            {
                var gameState = FezEngine.Tools.ServiceHelper.Get<Services.IGameStateManager>();
                if (gameState != null) stereoMode = gameState.StereoMode;
            }
            catch { }

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(950, 781), ImGuiCond.FirstUseEver);

            // Pass local field to avoid CS0206 property ref error
            bool isOpenLocal = IsOpen;
            if (ImGui.Begin("Chaos Mod Settings", ref isOpenLocal, ImGuiWindowFlags.MenuBar))
            {
                IsOpen = isOpenLocal;

                // --- Menu Bar ---
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        if (ImGui.MenuItem("New", "Ctrl+N"))
                        {
                            // Reset defaults
                            delayBetweenEffects = 0f;
                            durationMultiplier = 1f;
                            chaosModeEnabled = true;
                        }
                        if (ImGui.MenuItem("Open...", "Ctrl+O"))
                        {
                            if (File.Exists(activeSaveFile))
                                LoadSettingsFromFile(activeSaveFile);
                        }
                        if (ImGui.MenuItem("Save", "Ctrl+S"))
                        {
                            if (!string.IsNullOrEmpty(activeSaveFile) && File.Exists(activeSaveFile))
                                SaveSettingsToFile(activeSaveFile);
                        }
                        if (ImGui.MenuItem("Save As..."))
                        {
                            if (!string.IsNullOrEmpty(activeSaveFile))
                                SaveSettingsToFile(activeSaveFile);
                        }
                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Help"))
                    {
                        if (ImGui.MenuItem("About FEZ Chaos Mod"))
                        {
                            aboutWindow.IsOpen = true;
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }

                // --- Tabs ---
                if (ImGui.BeginTabBar("ChaosModTabs"))
                {
                    // TAB 1: Chaos Mod Main View
                    if (ImGui.BeginTabItem("Chaos Mod"))
                    {
                        System.Numerics.Vector2 contentAvail = ImGui.GetContentRegionAvail();
                        float topSplitHeight = contentAvail.Y * 0.5f;

                        // Top Container Panel
                        if (ImGui.BeginChild("TopSplitPanel", new System.Numerics.Vector2(contentAvail.X, topSplitHeight), ImGuiChildFlags.Border))
                        {
                            // Left Control Settings Group
                            ImGui.BeginGroup();

                            if (ImGui.Checkbox("Chaos Mod Enabled", ref chaosModeEnabled))
                            {
                                FezChaosMod.Enabled = chaosModeEnabled;
                            }

                            ImGui.Spacing();

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputFloat("Delay Between Effects (seconds)", ref delayBetweenEffects, 0.1f, 1f, "%.3f"))
                            {
                                chaosMod.DelayBetweenEffects = delayBetweenEffects;
                            }

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputFloat("Effect Duration Multiplier", ref durationMultiplier, 0.1f, 1f, "%.3f"))
                            {
                                chaosMod.EffectsDurationMultiplier = durationMultiplier;
                            }

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputInt("Max Effects to Display", ref maxEffectsToDisplay))
                            {
                                chaosMod.MaxActiveEffectsToDisplay = maxEffectsToDisplay;
                                if (latestEffectsToDisplay > maxEffectsToDisplay) latestEffectsToDisplay = maxEffectsToDisplay;
                            }

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputInt("Latest Effects to Display", ref latestEffectsToDisplay))
                            {
                                if (latestEffectsToDisplay > maxEffectsToDisplay) latestEffectsToDisplay = maxEffectsToDisplay;
                                chaosMod.LatestEffectsToDisplay = latestEffectsToDisplay;
                            }

                            ImGui.EndGroup();

                            ImGui.SameLine();

                            // Right Effect Checklist Container Box
                            System.Numerics.Vector2 listRegionSize = ImGui.GetContentRegionAvail();
                            if (ImGui.BeginChild("EffectsCheckListContainer", listRegionSize, ImGuiChildFlags.Border))
                            {
                                ImGui.Text("Effects");
                                ImGui.Separator();
                                effectListUI.Draw();
                            }
                            ImGui.EndChild();
                        }
                        ImGui.EndChild();

                        ImGui.Spacing();

                        // Bottom Logger Panel Box
                        if (ImGui.BeginChild("BottomLoggerPanel", ImGui.GetContentRegionAvail(), ImGuiChildFlags.Border))
                        {
                            ImGui.Text("Event Logger");
                            ImGui.Separator();
                            ImGui.InputTextMultiline("##EffectLoggerBox", ref loggerText, 100000, ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.ReadOnly);
                        }
                        ImGui.EndChild();

                        ImGui.EndTabItem();
                    }

                    // TAB 2: Miscellaneous Options Tab
                    if (ImGui.BeginTabItem("Miscellaneous"))
                    {
                        ImGui.Spacing();

                        if (ImGui.Checkbox("Display Debug Info", ref showDebugInfo))
                        {
                            chaosMod.ShowDebugInfo = showDebugInfo;
                        }

                        if (ImGui.Checkbox("Allow Rotate Anywhere", ref allowRotateAnywhere))
                        {
                            chaosMod.AllowRotateAnywhere = allowRotateAnywhere;
                        }

                        if (ImGui.Checkbox("Allow First Person Anywhere", ref firstPersonAnywhere))
                        {
                            chaosMod.AllowFirstPersonAnywhere = firstPersonAnywhere;
                        }

                        if (ImGui.Checkbox("Stereoscopic Mode", ref stereoMode))
                        {
                            try
                            {
                                FezEngine.Tools.ServiceHelper.Get<Services.IGameStateManager>().StereoMode = stereoMode;
                            }
                            catch { }
                        }

                        if (ImGui.Checkbox("All Zu speak is English", ref zuSpeakEnglish))
                        {
                            chaosMod.ZuSpeakEnglish = zuSpeakEnglish;
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            else
            {
                IsOpen = isOpenLocal;
            }
            ImGui.End();

            // Render About sub-window if open
            if (aboutWindow.IsOpen)
            {
                aboutWindow.Draw();
            }
        }

        // --- Mappers for INI Settings Files ---

        public Dictionary<string, Dictionary<string, object>> GetAllInputsValues(bool prependControlNameIfMultivalued = false)
        {
            return new Dictionary<string, Dictionary<string, object>>
            {
                ["ChaosModeCheckBox"] = new Dictionary<string, object>
                {
                    ["ChaosModeCheckBox"] = chaosModeEnabled
                },
                ["EffectsDelaySpinner"] = new Dictionary<string, object>
                {
                    ["EffectsDelaySpinner"] = (decimal)delayBetweenEffects
                },
                ["EffectsDurationMultiplierSpinner"] = new Dictionary<string, object>
                {
                    ["EffectsDurationMultiplierSpinner"] = (decimal)durationMultiplier
                },
                ["MaxEffectsToDisplaySpinner"] = new Dictionary<string, object>
                {
                    ["MaxEffectsToDisplaySpinner"] = (decimal)maxEffectsToDisplay
                },
                ["LatestEffectsToDisplaySpinner"] = new Dictionary<string, object>
                {
                    ["LatestEffectsToDisplaySpinner"] = (decimal)latestEffectsToDisplay
                },
                ["AllowRotateAnywhereCheckBox"] = new Dictionary<string, object>
                {
                    ["AllowRotateAnywhereCheckBox"] = allowRotateAnywhere
                },
                ["FirstPersonAnywhereCheckBox"] = new Dictionary<string, object>
                {
                    ["FirstPersonAnywhereCheckBox"] = firstPersonAnywhere
                },
                ["StereoModeCheckBox"] = new Dictionary<string, object>
                {
                    ["StereoModeCheckBox"] = stereoMode
                },
                ["ZuSpeakEnglishCheckBox"] = new Dictionary<string, object>
                {
                    ["ZuSpeakEnglishCheckBox"] = zuSpeakEnglish
                },
                ["DebugInfoCheckBox"] = new Dictionary<string, object>
                {
                    ["DebugInfoCheckBox"] = showDebugInfo
                }
            };
        }
        public void SetAllInputsValues(Dictionary<string, Dictionary<string, string>> ctrlvals)
        {
            foreach (var section in ctrlvals.Values)
            {
                foreach (var kvp in section)
                {
                    string key = kvp.Key;
                    string val = kvp.Value;

                    switch (key)
                    {
                    case "ChaosModeCheckBox":
                        if (bool.TryParse(val, out bool cMode)) chaosModeEnabled = cMode;
                        break;
                    case "EffectsDelaySpinner":
                        if (float.TryParse(val, out float delay)) delayBetweenEffects = delay;
                        break;
                    case "EffectsDurationMultiplierSpinner":
                        if (float.TryParse(val, out float mult)) durationMultiplier = mult;
                        break;
                    case "MaxEffectsToDisplaySpinner":
                        if (int.TryParse(val, out int max)) maxEffectsToDisplay = max;
                        break;
                    case "LatestEffectsToDisplaySpinner":
                        if (int.TryParse(val, out int latest)) latestEffectsToDisplay = latest;
                        break;
                    case "AllowRotateAnywhereCheckBox":
                        if (bool.TryParse(val, out bool rot)) allowRotateAnywhere = rot;
                        break;
                    case "FirstPersonAnywhereCheckBox":
                        if (bool.TryParse(val, out bool fp)) firstPersonAnywhere = fp;
                        break;
                    case "StereoModeCheckBox":
                        if (bool.TryParse(val, out bool stereo)) stereoMode = stereo;
                        break;
                    case "ZuSpeakEnglishCheckBox":
                        if (bool.TryParse(val, out bool zu)) zuSpeakEnglish = zu;
                        break;
                    case "DebugInfoCheckBox":
                        if (bool.TryParse(val, out bool dbg)) showDebugInfo = dbg;
                        break;
                    }
                }
            }
        }

        private void SaveSettingsToFile(string file)
        {
            try
            {
                activeSaveFile = Path.GetFullPath(file);
                LogLineDebug("Saving to file: " + file);
                ChaosModSettingsHelper.Write(this, file);
            }
            catch (Exception e)
            {
                LogLine("Warning: Failed to write to file: " + file);
                LogLine(e.Message);
            }
        }

        private void LoadSettingsFromFile(string file)
        {
            try
            {
                activeSaveFile = Path.GetFullPath(file);
                LogLineDebug("Loading from file: " + file);
                ChaosModSettingsHelper.Read(this, file);
            }
            catch (Exception e)
            {
                LogLine("Warning: Failed to load from file: " + file);
                LogLine(e.Message);
            }
        }
    }
}