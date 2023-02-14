using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;


namespace FezGame.ChaosMod
{
    //Hacked together from some stuff I found on the internet
    public class CircularProgressBar : ProgressBar, IDisposable
    {
        public CircularProgressBar()
        {
        }


        public static readonly int Size = 50;

        private static readonly System.Drawing.Color __BackColor = BackColor.ToDrawingColor();
        private static readonly System.Drawing.Color __BarColor1 = BarColor1.ToDrawingColor();

        internal void DrawProgressCircle(double progress, string Text, Vector2 pos, float scale, bool isPaused)
        {
            if (progress > 1)
                progress = 1;
            bool isDone = progress >= 1;

            if (!isDone)
            {
                if(BackTexture == null)
                {
                    BackTexture = GenerateBackTexture();
                }
                DrawingTools.Instance.DrawTexture(BackTexture, pos, scale / PieRenderScale, SamplerState.AnisotropicClamp, null, BackColor);
                DrawingTools.Instance.DrawTexture(GetCircleTextureForProgress(progress), pos, scale / PieRenderScale, SamplerState.AnisotropicClamp, null, isPaused ? PausedColor : ProgressColor);

            }
            if (Text != null)
            {
                //draw the text centered
                var textSize = DrawingTools.Instance.MeasureString(Text);
                var shifted = new Vector2
                {
                    X = pos.X + (Size / 2 - textSize.X) * scale,
                    Y = pos.Y + (Size / 2 - textSize.Y) * scale
                };
                DrawingTools.Instance.DrawShadowedText(Text, isDone ? Microsoft.Xna.Framework.Color.Transparent : TextColor, shifted,
                                            scale);
            }
        }

        private static readonly Dictionary<int, Texture2D> CircleProgressTextureDictionary = new Dictionary<int, Texture2D>();
        private static Texture2D BackTexture = null;
        private static readonly int PieRenderScale = 2;
        private static readonly int PieOutlineSize = 2;
        private static Bitmap bitmap = null;
        private static Graphics graphics = null;
        private static Brush brush = null;
        private static Brush mBackColor = null;
        private static Texture2D GetCircleTextureForProgress(double progress)
        {
            int roundedprog = (int)(progress * 100);
            if (CircleProgressTextureDictionary.TryGetValue(roundedprog, out Texture2D texture))
                return texture;
            texture = GenerateCircleTextureForProgress(progress);
            CircleProgressTextureDictionary.Add(roundedprog, texture);
            return texture;
        }
        private static void ResetGraphics()
        {
            if (bitmap == null)
                bitmap = new Bitmap(Size * PieRenderScale, Size * PieRenderScale, PixelFormat.Format32bppArgb);
            if (graphics == null)
                graphics = Graphics.FromImage(bitmap);
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        }
        private static Texture2D GenerateBackTexture()
        {
            ResetGraphics();

            //Dibuja el circulo blanco interior:\
            if (mBackColor == null)
                mBackColor = new SolidBrush(__BackColor);

            graphics.FillEllipse(mBackColor,
                    0, 0,
                    Size * PieRenderScale,
                    Size * PieRenderScale);

            return DrawingTools.Instance.BitMapToTexture2D(bitmap);
        }
        private static Texture2D GenerateCircleTextureForProgress(double progress)
        {
            ResetGraphics();

            //Dibuja la Barra de Progreso
            if (brush == null)
                brush = new SolidBrush(__BarColor1);
            graphics.FillPie(brush,
                    PieOutlineSize * PieRenderScale, PieOutlineSize * PieRenderScale,
                    (Size - PieOutlineSize * 2) * PieRenderScale,
                    (Size - PieOutlineSize * 2) * PieRenderScale,
                    -90,
                    (int)Math.Round(360.0 * progress));

            return DrawingTools.Instance.BitMapToTexture2D(bitmap);
        }

        public void Dispose()
        {
            mBackColor?.Dispose();
            brush?.Dispose();
            graphics?.Dispose();
            bitmap?.Dispose();
        }
    }
}
