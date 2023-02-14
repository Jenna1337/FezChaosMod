using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FezGame.ChaosMod
{
    public class ChaosModEffectText : IDisposable
    {
        private readonly CircularProgressBar circularProgressBar;
        private readonly GraphicsDevice GraphicsDevice;

        public ChaosModEffectText()
        {
            GraphicsDevice = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice;
            circularProgressBar = new CircularProgressBar();
        }

        private static readonly int ProgressCirclePadding = 4;
        internal void Draw(string text, double progress, int offset, string Text, Color color, bool isPaused)
        {
            float scale = (float)Math.Floor(GraphicsDevice.GetViewScale());
            Vector2 vector = new Vector2(15, GraphicsDevice.Viewport.Height * .75f - (CircularProgressBar.Size + ProgressCirclePadding) * offset * scale);
            circularProgressBar.DrawProgressCircle(progress, Text, vector, scale, isPaused);
            DrawingTools.Instance.DrawShadowedText(text, color, vector + new Vector2((CircularProgressBar.Size + ProgressCirclePadding) * scale, 0), scale);
        }

        public void Dispose()
        {
            circularProgressBar.Dispose();
        }
    }
}
