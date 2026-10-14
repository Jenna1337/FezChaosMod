using FezGame.ChaosMod;
using ImGuiNET;
using System.Linq;
using static FezGame.ChaosMod.FezChaosMod;

namespace FezGame
{
    public class ChaosModEffectListControl
    {
        private readonly FezChaosMod chaosMod;
        // Tracks state for any active additional settings window
        public AdditionalChaosEffectSettingsWindow ActiveAdditionalSettingsWindow { get; private set; }

        public ChaosModEffectListControl(FezChaosMod chaosMod)
        {
            this.chaosMod = chaosMod;
        }

        public void Draw()
        {
            // Group effects by category (defaulting to "Uncategorized" if null/empty)
            var categorizedEffects = chaosMod.ChaosEffectsList
                .GroupBy(e => string.IsNullOrEmpty(e.Category) ? "Uncategorized" : e.Category)
                .OrderBy(g => g.Key);

            foreach (var categoryGroup in categorizedEffects)
            {
                // Render category as a collapsible header
                if (ImGui.CollapsingHeader(categoryGroup.Key, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Indent(10.0f);

                    foreach (var effect in categoryGroup)
                    {
                        DrawEffectRow(effect);
                    }

                    ImGui.Unindent(10.0f);
                    ImGui.Spacing();
                }
            }

            // Render active sub-window if opened via "..." button
            if (ActiveAdditionalSettingsWindow != null && ActiveAdditionalSettingsWindow.IsOpen)
            {
                ActiveAdditionalSettingsWindow.Draw();
            }
        }

        private void DrawEffectRow(ChaosEffect effect)
        {
            string idPrefix = "##" + effect.Name;

            // 1. Enabled Checkbox
            bool isEnabled = effect.Enabled;
            if (ImGui.Checkbox(effect.Name + idPrefix + "Check", ref isEnabled))
            {
                effect.Enabled = isEnabled;
                ChaosModWindow.Settings.chaosEffectSettings.First(ef => ef.Name == effect.Name).Enabled = isEnabled;
            }

            ImGui.SameLine();

            // 2. Weight Spinner / Input
            float weight = (float)effect._weight;
            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputFloat(idPrefix + "Weight", ref weight, 0.01f, 0.1f, "%.4f"))
            {
                effect._weight = weight;
                ChaosModWindow.Settings.chaosEffectSettings.First(ef => ef.Name == effect.Name).Weight = weight;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Weight");

            // 3. Duration Spinner / Input (if applicable)
            if (effect.Duration > 0)
            {
                ImGui.SameLine();
                float duration = (float)effect._duration;
                ImGui.SetNextItemWidth(100f);
                if (ImGui.InputFloat(idPrefix + "Duration", ref duration, 0.1f, 1f, "%.4f"))
                {
                    effect._duration = duration;
                    ChaosModWindow.Settings.chaosEffectSettings.First(ef => ef.Name == effect.Name).Duration = duration;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Duration");
            }
            else
            {
                ImGui.SameLine();
                ImGui.Dummy(new System.Numerics.Vector2(104f, 1f));
            }

            ImGui.SameLine();

            // Check if this specific effect is currently running/active in the mod
            bool isRunning = chaosMod.IsEffectActive(effect); // Or check your active collection

            // 4. Start / Activate Button (disabled if already running)
            if (isRunning) ImGui.BeginDisabled();
            if (ImGui.Button("Start" + idPrefix))
            {
                effect.Activate();
            }
            if (isRunning) ImGui.EndDisabled();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Forcibly activates the effect\nWarning: might break the game!");

            ImGui.SameLine();

            // 5. End / Terminate Button (disabled if NOT running)
            if (!isRunning) ImGui.BeginDisabled();
            if (ImGui.Button("End" + idPrefix))
            {
                effect.Terminate();
            }
            if (!isRunning) ImGui.EndDisabled();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Forcibly terminates and removes all instances of this effect.");

            ImGui.SameLine();

            // 6. Additional Settings Button ("...")
            if (effect.AdditionalSettings != null)
            {
                if (ImGui.Button("..." + idPrefix))
                {
                    ActiveAdditionalSettingsWindow = new AdditionalChaosEffectSettingsWindow(effect.AdditionalSettings)
                    {
                        IsOpen = true
                    };
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Additional Settings");
            }

            ImGui.Spacing();
        }
    }
}