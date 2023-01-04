using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FezGame.ChaosMod
{
    public class DrawingTools : IDisposable
    {
        private readonly SpriteFont BigFont;
        private readonly SpriteBatch Batch;
        private readonly GraphicsDevice graphicsDevice;

        [ServiceDependency]
        public IFontManager FontManager { private get; set; }

        private static DrawingTools instance = null;
        public static DrawingTools Instance { get => instance ?? (instance = new DrawingTools()); }

        private DrawingTools()
        {
            ServiceHelper.InjectServices(this);
            BigFont = FontManager.Big;
            Batch = new SpriteBatch(graphicsDevice = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice);
        }

        public void DrawTexture(Texture2D texture, Vector2 pos, float scale, SamplerState samplerState, Rectangle? sourceRectangle, Color color)
        {
            Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState, DepthStencilState.None, RasterizerState.CullNone);
            Batch.Draw(texture, pos, sourceRectangle, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            Batch.End();
        }

        public void DrawTexture(Texture2D texture, Rectangle destinationRectangle, SamplerState samplerState, Rectangle? sourceRectangle, Color color)
        {
            Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState, DepthStencilState.None, RasterizerState.CullNone);
            Batch.Draw(texture, destinationRectangle, sourceRectangle, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            Batch.End();
        }

        public void DrawShadowedText(string Text, Color color, Vector2 pos, float scale)
        {
            Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            if (!Color.Transparent.Equals(color))
                DrawText(Text, Color.Black, pos + new Vector2(scale, scale), scale);//shadow
            DrawText(Text, color, pos, scale);
            Batch.End();
        }

        private void DrawText(string text, Color color, Vector2 pos, float scale)
        {
            Batch.DrawString(BigFont, text, pos, color, 0f, Vector2.Zero, 2f * scale, SpriteEffects.None, 0f);
        }

        public void Dispose()
        {
            Batch.Dispose();
        }
        public Vector2 MeasureString(string text)
        {
            return BigFont.MeasureString(text);
        }

        public Texture2D BitMapToTexture2D(System.Drawing.Bitmap bitmap)
        {
            var colors = new Color[bitmap.Width * bitmap.Height];
            for (int x = 0; x < bitmap.Width; ++x)
                for (int y = 0; y < bitmap.Height; ++y)
                {
                    var color = bitmap.GetPixel(x, y);
                    colors[x + y * bitmap.Height] = new Color(color.R, color.G, color.B, color.A);
                }

            Texture2D texture = ColorArrayToTexture2D(colors, bitmap.Width, bitmap.Height);
            texture.SetData(colors);

            return texture;
        }
        public Texture2D ColorArrayToTexture2D(Color[] colors, int Height, int Width)
        {
            Texture2D texture = new Texture2D(graphicsDevice, Width, Height, false, SurfaceFormat.Color);
            texture.SetData(colors);

            return texture;
        }
    }
}
