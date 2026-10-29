using ImGuiNET;
using System.Numerics;

namespace IMGUI
{
    public enum CheckState
    {
        Unchecked = 0,
        Checked = 1,
        Indeterminate = 2
    }
    public static class ImGuiExtensions
    {
        // C# wrapper using ImGuiNET's internal item flags
        public static bool CheckboxTristate(string id, ref CheckState state)
        {
            bool changed = false;
            bool isChecked = state == CheckState.Checked;

            bool dummyChecked = state == CheckState.Indeterminate ? false : isChecked;

            if (ImGui.Checkbox("##" + id, ref dummyChecked))
            {
                bool targetState = (state != CheckState.Unchecked) ? false : true;
                changed = true;
            }

            // If Indeterminate, manually overlay the indeterminate dash inside the checkbox square
            if (state == CheckState.Indeterminate)
            {
                // Get the exact screen coordinates of the checkbox widget we just drew
                Vector2 min = ImGui.GetItemRectMin();
                Vector2 max = ImGui.GetItemRectMax();

                // Calculate the padding dynamically based on your current theme style
                float padding = 4.0f;

                Vector2 boxMin = new Vector2(min.X + padding, min.Y + padding);
                Vector2 boxMax = new Vector2(max.X - padding, max.Y - padding);

                ImGui.GetWindowDrawList().AddRectFilled(boxMin, boxMax, ImGui.GetColorU32(ImGuiCol.CheckMark));
            }
            return changed;
        }
    }
}