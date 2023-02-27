using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace FezGame.ChaosMod
{
    public class LinearProgressBar : ProgressBar, IDisposable
    {
        public LinearProgressBar()
        {
        }

        private static Texture2D backTexture = null;
        private static Texture2D barTexture = null;

        internal void DrawProgressBar(double progress, string Text, Rectangle dest, float scale, bool isPaused)
        {
            if (progress > 1)
            {
                progress = 1;
            }
            
            if (backTexture == null)
            {
                backTexture = DrawingTools.Instance.ColorArrayToTexture2D(new Color[1] { BackColor }, 1, 1);
            }
            if (barTexture == null)
            {
                barTexture = DrawingTools.Instance.ColorArrayToTexture2D(new Color[1] { BarColor1 }, 1, 1);
            }

            DrawingTools.Instance.DrawTexture(backTexture, dest, SamplerState.AnisotropicClamp, null, BackColor);
            int progWidth = (int)Math.Round(dest.Width * progress);
            Rectangle progdest = new Rectangle(dest.X, dest.Y, progWidth, dest.Height);
            DrawingTools.Instance.DrawTexture(barTexture, progdest, SamplerState.AnisotropicClamp, null, isPaused ? PausedColor : ProgressColor);

            //draw the text centered
            var textSize = DrawingTools.Instance.MeasureString(Text);
            var shifted = new Vector2
            {
                X = dest.Center.X - textSize.X * scale,
                Y = dest.Center.Y - textSize.Y * scale
            };
            DrawingTools.Instance.DrawShadowedText(Text, TextColor, shifted, scale);
        }

        public void Dispose()
        {
            backTexture.Dispose();
            barTexture.Dispose();
        }
    }
}
