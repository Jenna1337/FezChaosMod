using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FezGame.ChaosMod
{
    public class ProgressBar
    {
        protected static Microsoft.Xna.Framework.Color BackColor { get; } = new Microsoft.Xna.Framework.Color(128, 128, 128, 200);
        protected static Microsoft.Xna.Framework.Color BarColor1 { get; } = new Microsoft.Xna.Framework.Color(255, 255, 255, 240);
        protected static Microsoft.Xna.Framework.Color ProgressColor { get; set; }
        protected static Microsoft.Xna.Framework.Color TextColor { get; set; }

        internal static void SetColors(Microsoft.Xna.Framework.Color ProgressColor, Microsoft.Xna.Framework.Color TextColor)
        {
            ProgressBar.ProgressColor = ProgressColor;
            ProgressBar.TextColor = TextColor;
        }
        internal static void SetColors(System.Drawing.Color ProgressColor, System.Drawing.Color TextColor)
        {
            ProgressBar.ProgressColor = ProgressColor.ToXnaColor();
            ProgressBar.TextColor = TextColor.ToXnaColor();
        }
    }
}
