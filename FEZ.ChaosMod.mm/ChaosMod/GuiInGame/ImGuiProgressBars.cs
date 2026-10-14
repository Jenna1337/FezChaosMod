#if NET10_0_OR_GREATER
using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using NVector2 = System.Numerics.Vector2;

namespace FezGame.ChaosMod
{
    internal static class ImGuiProgressDrawing
    {
        internal static uint Pack(Color color)
        {
            return ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(
                color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
        }

        internal static double ClampProgress(double progress)
        {
            return double.IsNaN(progress) ? 0d : Math.Clamp(progress, 0d, 1d);
        }

        internal static void DrawCenteredText(ImDrawListPtr drawList, string text,
            NVector2 center, float scale, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            var position = center - ImGui.CalcTextSize(text) * scale / 2f;
            float fontSize = ImGui.GetFontSize() * scale;
            drawList.AddText(ImGui.GetFont(), fontSize, position + new NVector2(scale), Pack(Color.Black), text);
            drawList.AddText(ImGui.GetFont(), fontSize, position, Pack(color), text);
        }
    }

    public class CircularProgressBar : ProgressBar, IDisposable
    {
        public static readonly int Size = 50;
        private const int PieOutlineSize = 2;

        internal void DrawProgressCircle(double progress, string text, Vector2 pos, float scale, bool isPaused)
        {
            progress = ImGuiProgressDrawing.ClampProgress(progress);
            if (progress >= 1d) return;

            var drawList = ImGui.GetBackgroundDrawList();
            var center = new NVector2(pos.X, pos.Y) + new NVector2(Size * scale / 2f);
            drawList.AddCircleFilled(center, Size * scale / 2f, ImGuiProgressDrawing.Pack(BackColor));

            float radius = (Size / 2f - PieOutlineSize) * scale;
            float startAngle = -MathF.PI / 2f;
            float sweep = (float)(Math.PI * 2d * progress);
            uint color = ImGuiProgressDrawing.Pack(isPaused ? PausedColor : ProgressColor);
            // A pie larger than half a circle is concave. Split it into convex sectors.
            while (sweep > 0f)
            {
                float angle = Math.Min(sweep, MathF.PI);
                drawList.PathLineTo(center);
                drawList.PathArcTo(center, radius, startAngle, startAngle + angle);
                drawList.PathFillConvex(color);
                startAngle += angle;
                sweep -= angle;
            }
            var textSize = ImGui.CalcTextSize(text);
            float textSizeMax = Math.Max(textSize.X, textSize.Y);
            float maxTextSquareSideLength = Size / (float)Math.Sqrt(2);
            float fitScale = maxTextSquareSideLength / textSizeMax;
            float scaleAdjustedForTextSize = scale * fitScale;
            ImGuiProgressDrawing.DrawCenteredText(drawList, text, center, scaleAdjustedForTextSize, TextColor);
        }

        public void Dispose() { }
    }

    public class LinearProgressBar : ProgressBar, IDisposable
    {
        internal void DrawProgressBar(double progress, string text, Rectangle dest, float scale, bool isPaused)
        {
            progress = ImGuiProgressDrawing.ClampProgress(progress);
            var drawList = ImGui.GetBackgroundDrawList();
            var min = new NVector2(dest.X, dest.Y);
            var max = new NVector2(dest.Right, dest.Bottom);
            drawList.AddRectFilled(min, max, ImGuiProgressDrawing.Pack(BackColor));
            drawList.AddRectFilled(min, new NVector2(dest.X + (float)(dest.Width * progress), dest.Bottom),
                ImGuiProgressDrawing.Pack(isPaused ? PausedColor : ProgressColor));
            ImGuiProgressDrawing.DrawCenteredText(drawList, text, (min + max) / 2f, scale, TextColor);
        }

        public void Dispose() { }
    }
}
#endif
