namespace FezGame.ChaosMod
{
    public class ProgressBar
    {
        protected static Microsoft.Xna.Framework.Color BackColor { get; } = new Microsoft.Xna.Framework.Color(128, 128, 128, 200);
        protected static Microsoft.Xna.Framework.Color BarColor1 { get; } = new Microsoft.Xna.Framework.Color(255, 255, 255, 240);
        protected static Microsoft.Xna.Framework.Color ProgressColor { get; set; }
        protected static Microsoft.Xna.Framework.Color TextColor { get; set; }
        protected static Microsoft.Xna.Framework.Color PausedColor { get; set; }

        internal static void SetColors(Microsoft.Xna.Framework.Color ProgressColor, Microsoft.Xna.Framework.Color TextColor, Microsoft.Xna.Framework.Color PausedColor)
        {
            ProgressBar.ProgressColor = ProgressColor;
            ProgressBar.TextColor = TextColor;
            ProgressBar.PausedColor = PausedColor;
        }
    }
}
