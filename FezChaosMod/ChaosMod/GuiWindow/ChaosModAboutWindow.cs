using FezGame.ChaosMod;
using ImGuiNET;

namespace FezGame
{
    public class ChaosModAboutWindow
    {
        public bool IsOpen = true;

        public void Draw()
        {
            if (!IsOpen) return;

            // Set a default initial window size matching your original form dimensions (379x313)
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(379, 313), ImGuiCond.FirstUseEver);

            // Open window with fixed or standard flags
            if (ImGui.Begin("About FEZ Chaos Mod", ref IsOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
            {
                // Add some vertical spacing to match original padding layout
                ImGui.Dummy(new System.Numerics.Vector2(0, 20));

                // Author label
                ImGui.Text("FEZ Chaos Mod made by Jenna Sloan");

                ImGui.Spacing();

                // Version label dynamically pulling from FezChaosMod.Version
                ImGui.Text($"Chaos Mod Version: {FezChaosMod.Version}");

                // Position the OK button near the bottom center/right matching original coordinates
                // We calculate available height to push it down
                float availableHeight = ImGui.GetContentRegionAvail().Y;
                ImGui.Dummy(new System.Numerics.Vector2(0, availableHeight - 70));

                // Center the button horizontally roughly where it was
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 108) * 0.5f);

                if (ImGui.Button("OK", new System.Numerics.Vector2(108, 37)))
                {
                    IsOpen = false;
                }
            }
            ImGui.End();
        }
    }
}