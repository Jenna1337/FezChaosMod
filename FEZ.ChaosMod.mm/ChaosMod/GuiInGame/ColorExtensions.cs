


using Microsoft.Xna.Framework;

namespace FezGame.ChaosMod
{
    public static class ColorExtensions
    {
        public static Microsoft.Xna.Framework.Color ToXnaColor(this System.Drawing.Color color)
        {
            return new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
        }
        public static System.Drawing.Color ToDrawingColor(this Microsoft.Xna.Framework.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static float GetHue(this Color color)
        {
            return color.ToDrawingColor().GetHue();
        }
        public static float GetSaturation(this Color color)
        {
            return color.ToDrawingColor().GetSaturation();
        }
        public static float GetLightness(this Color color)
        {
            return color.ToDrawingColor().GetBrightness();
        }

        public static Color HueRotate(this Color color, float degrees)
        {
            return color.ToDrawingColor().HueRotate(degrees);
        }
        public static Color HueRotate(this System.Drawing.Color color, float degrees)
        {
            return color.Adjust(hueRotateDegrees: degrees);
        }

        public static Color Darken(this Color color, float percentage)
        {
            return color.ToDrawingColor().Darken(percentage);
        }
        public static Color Darken(this System.Drawing.Color color, float percentage)
        {
            return color.Adjust(lightenPercentage: -percentage);
        }

        public static Color Lighten(this Color color, float percentage)
        {
            return color.ToDrawingColor().Lighten(percentage);
        }
        public static Color Lighten(this System.Drawing.Color color, float percentage)
        {
            return color.Adjust(lightenPercentage: percentage);
        }

        public static Color Desaturate(this Color color, float percentage)
        {
            return color.ToDrawingColor().Desaturate(percentage);
        }
        public static Color Desaturate(this System.Drawing.Color color, float percentage)
        {
            return color.Adjust(saturatePercentage: -percentage);
        }

        public static Color Saturate(this Color color, float percentage)
        {
            return color.ToDrawingColor().Saturate(percentage);
        }
        public static Color Saturate(this System.Drawing.Color color, float percentage)
        {
            return color.Adjust(saturatePercentage: percentage);
        }

        /// <summary>
        /// Adjusts the color by the provided amount
        /// </summary>
        /// <param name="color"></param>
        /// <param name="hueRotateDegrees">The amount in degrees to rotate the color</param>
        /// <param name="saturatePercentage">The percentage to scale the saturation</param>
        /// <param name="lightenPercentage">The percentage to scale the lightness</param>
        /// <returns></returns>
        public static Color Adjust(this System.Drawing.Color color, float hueRotateDegrees = 0f, float saturatePercentage = 0f, float lightenPercentage = 0f)
        {
            float h = color.GetHue();
            float s = color.GetSaturation();
            float l = color.GetBrightness();

            while (hueRotateDegrees < 0)
                hueRotateDegrees += 360f;

            h = (h + hueRotateDegrees) % 360f;

            s = MathHelper.Clamp(s * (1f + saturatePercentage), 0f, 1f);

            l = MathHelper.Clamp(l * (1f + lightenPercentage), 0f, 1f);

            return FromHSLA(h / 360f, s, l, color.A / 255.0f);
        }

        /// <summary>
        /// All input values should be between 0 and 1, inclusive <br />
        /// <br />
        /// Adapted from <a href="https://gist.github.com/mjackson/5311256">https://gist.github.com/mjackson/5311256</a>
        /// </summary>
        /// <param name="h">Hue</param>
        /// <param name="s">Saturation</param>
        /// <param name="l">Lightness</param>
        /// <param name="a">Alpha</param>
        /// <returns></returns>
        private static Color FromHSLA(float h, float s, float l, float a)
        {
            float r, g, b;

            if (s == 0f)
            {
                r = g = b = l; // achromatic
            }
            else
            {
                float q = l < 0.5f ? (l * (1f + s)) : (l + s - (l * s));
                float p = (2.0f * l) - q;
                r = HueToRgb(p, q, h + (1.0f / 3.0f));
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - (1.0f / 3.0f));
            }

            //int r = (int)Math.Round(r * 255);
            //int g = (int)Math.Round(g * 255);
            //int b = (int)Math.Round(b * 255);

            return new Color(r, g, b, a);
        }
        // Adapted from https://gist.github.com/mjackson/5311256
        private static float HueToRgb(float p, float q, float t)
        {
            while (t < 0.0f) t += 1.0f;
            while (t > 1.0f) t -= 1.0f;
            if (t < 1.0f / 6.0f) return p + ((q - p) * 6f * t);
            if (t < 1.0f / 2.0f) return q;
            if (t < 2.0f / 3.0f) return p + ((q - p) * ((2.0f / 3.0f) - t) * 6f);
            return p;
        }

    }
}
