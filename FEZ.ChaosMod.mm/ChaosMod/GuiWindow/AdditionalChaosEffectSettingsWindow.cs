using FezGame.ChaosMod;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;

namespace FezGame
{
    public class AdditionalChaosEffectSettingsWindow
    {
        private readonly FezChaosMod.AdditionalChaosEffectSettings settings;
        public bool IsOpen = true;

        public AdditionalChaosEffectSettingsWindow(FezChaosMod.AdditionalChaosEffectSettings settings)
        {
            this.settings = settings;
        }

        public void Draw()
        {
            if (!IsOpen) return;

            // Window title defaults to settings name or fallback
            string windowTitle = settings.AdditionalSettingsName ?? "Additional Chaos Effect Settings";

            // Set a default initial window size for convenience
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(380, 315), ImGuiCond.FirstUseEver);

            if (ImGui.Begin(windowTitle, ref IsOpen, ImGuiWindowFlags.NoCollapse))
            {
                // Group box equivalent (child window with a border for the checklist area)
                // Leaving room at the bottom for the OK button
                Vector2 contentRegion = ImGui.GetContentRegionAvail();
                contentRegion.Y -= 45; // Reserve space for button

                if (ImGui.BeginChild("SettingsScrollRegion", contentRegion, ImGuiChildFlags.Border, ImGuiWindowFlags.None))
                {
                    // Iterate through the settings list directly
                    // Note: If AdditionalSettingsCheckListList is a List of KeyValuePairs or custom objects, 
                    // adjust the loop variables to match your structure.
                    for (int i = 0; i < settings.AdditionalSettingsCheckListList.Count; i++)
                    {
                        var pair = settings.AdditionalSettingsCheckListList[i];

                        // Create a mutable local bool for ImGui Checkbox binding
                        bool isChecked = pair.Value;

                        if (ImGui.Checkbox(pair.Key, ref isChecked))
                        {
                            // If the user toggles the checkbox, update state and notify settings
                            settings.AdditionalSettingsCheckListList[i] = new KeyValuePair<string, bool>(pair.Key, isChecked);
                            settings.ItemCheck(pair.Key, isChecked);
                        }
                    }
                }
                ImGui.EndChild();

                ImGui.Spacing();

                // OK / Close Button docked near the bottom center/right
                // Centering or stretching the button to match WinForms look
                if (ImGui.Button("OK", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 35)))
                {
                    IsOpen = false;
                }
            }
            ImGui.End();
        }
    }
}