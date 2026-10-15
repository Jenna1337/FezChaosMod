using FezGame.ChaosMod;
using ImGuiNET;
using System;
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
        internal static ChaosModSettings Settings = null;

        internal string activeSaveFile = "chaosmod.ini";
        private static string loggerText = "";
        private const int MaxLogLines = 1000;

        public ChaosModWindow(FezChaosMod chaosMod)
        {
            if (Instance != null)
                Instance = null;
            Instance = this;

            this.chaosMod = chaosMod;
            this.effectListUI = new ChaosModEffectListControl(chaosMod);

            if (Settings == null)
            {
                Settings = new ChaosModSettings();
            }
            // Load initial values from mod instance
            Settings.showDebugInfo = chaosMod.ShowDebugInfo;
            Settings.allowRotateAnywhere = chaosMod.AllowRotateAnywhere;
            Settings.delayBetweenEffects = (float)chaosMod.DelayBetweenEffects;
            Settings.durationMultiplier = (float)chaosMod.EffectsDurationMultiplier;
            Settings.maxEffectsToDisplay = chaosMod.MaxActiveEffectsToDisplay;
            Settings.latestEffectsToDisplay = chaosMod.LatestEffectsToDisplay;

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
                if (gameState != null) Settings.stereoMode = gameState.StereoMode;
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
                            Settings.Reset();
                        }
                        if (ImGui.MenuItem("Open...", "Ctrl+O"))
                        {
                            if (File.Exists(activeSaveFile))
                                LoadSettingsFromFile(activeSaveFile);
                        }
                        if (ImGui.MenuItem("Save", "Ctrl+S"))
                        {
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

                            if (ImGui.Checkbox("Chaos Mod Enabled", ref Settings.chaosModeEnabled))
                            {
                                FezChaosMod.Enabled = Settings.chaosModeEnabled;
                            }

                            ImGui.Spacing();

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputFloat("Delay Between Effects (seconds)", ref Settings.delayBetweenEffects, 0.1f, 1f, "%.3f"))
                            {
                                chaosMod.DelayBetweenEffects = Settings.delayBetweenEffects;
                            }

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputFloat("Effect Duration Multiplier", ref Settings.durationMultiplier, 0.1f, 1f, "%.3f"))
                            {
                                chaosMod.EffectsDurationMultiplier = Settings.durationMultiplier;
                            }

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputInt("Max Effects to Display", ref Settings.maxEffectsToDisplay))
                            {
                                chaosMod.MaxActiveEffectsToDisplay = Settings.maxEffectsToDisplay;
                                if (Settings.latestEffectsToDisplay > Settings.maxEffectsToDisplay) Settings.latestEffectsToDisplay = Settings.maxEffectsToDisplay;
                            }

                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputInt("Latest Effects to Display", ref Settings.latestEffectsToDisplay))
                            {
                                if (Settings.latestEffectsToDisplay > Settings.maxEffectsToDisplay) Settings.latestEffectsToDisplay = Settings.maxEffectsToDisplay;
                                chaosMod.LatestEffectsToDisplay = Settings.latestEffectsToDisplay;
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

                        if (ImGui.Checkbox("Display Debug Info", ref Settings.showDebugInfo))
                        {
                            chaosMod.ShowDebugInfo = Settings.showDebugInfo;
                        }

                        if (ImGui.Checkbox("Allow Rotate Anywhere", ref Settings.allowRotateAnywhere))
                        {
                            chaosMod.AllowRotateAnywhere = Settings.allowRotateAnywhere;
                        }

                        if (ImGui.Checkbox("Allow First Person Anywhere", ref Settings.firstPersonAnywhere))
                        {
                            chaosMod.AllowFirstPersonAnywhere = Settings.firstPersonAnywhere;
                        }

                        if (ImGui.Checkbox("Stereoscopic Mode", ref Settings.stereoMode))
                        {
                            try
                            {
                                FezEngine.Tools.ServiceHelper.Get<Services.IGameStateManager>().StereoMode = Settings.stereoMode;
                            }
                            catch { }
                        }

                        if (ImGui.Checkbox("All Zu speak is English", ref Settings.zuSpeakEnglish))
                        {
                            chaosMod.ZuSpeakEnglish = Settings.zuSpeakEnglish;
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


        internal void SaveSettingsToFile(string file)
        {
            try
            {
                this.activeSaveFile = Path.GetFullPath(file);
                LogLineDebug("Saving to file: " + file);
                ChaosModSettingsHelper.Write(Settings, file);
            }
            catch (Exception e)
            {
                LogLine("Warning: Failed to write to file: " + file);
                LogLine(e.Message);
            }
        }

        internal void LoadSettingsFromFile(string file)
        {
            try
            {
                activeSaveFile = Path.GetFullPath(file);
                LogLineDebug("Loading from file: " + file);
                ChaosModSettingsHelper.Read(Settings, file);
            }
            catch (Exception e)
            {
                LogLine("Warning: Failed to load from file: " + file);
                LogLine(e.Message);
            }
        }
    }
}