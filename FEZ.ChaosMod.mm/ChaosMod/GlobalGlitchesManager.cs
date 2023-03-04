using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FezGame.ChaosMod
{
    internal class GlobalGlitchesManager : DrawableGameComponent
    {
        private readonly DrawableGameComponent glitches;
        private static Type NesGlitchesType;
        private readonly PropertyInfo ActiveGlitchesInfo;
        private readonly PropertyInfo FreezeProbabilityInfo;
        private readonly PropertyInfo DisappearProbabilityInfo;

        public GlobalGlitchesManager(Game game) : base(game)
        {
            System.Diagnostics.Debugger.Break();
            IContentManagerProvider CMProvider = ServiceHelper.Get<IContentManagerProvider>();

            NesGlitchesType = typeof(FezGame.Fez).Assembly.GetType("FezGame.Components.NesGlitches");
            glitches = (DrawableGameComponent)NesGlitchesType.GetConstructor(new[] { typeof(Game) }).Invoke(new[] { game });
            ServiceHelper.AddComponent(glitches);

            this.DrawOrder = glitches.DrawOrder-1;

            (NesGlitchesType.GetField("GlitchMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(glitches) as Mesh)
                .Texture = CMProvider.Global.Load<Texture2D>("Other Textures/glitches/glitch_atlas");
            NesGlitchesType.GetField("sGlitches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(glitches,
                (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Glitches")
                 select CMProvider.Global.Load<SoundEffect>(x)).ToArray());
            NesGlitchesType.GetField("sTimestretches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(glitches,
                (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Timestretches")
                 select CMProvider.Global.Load<SoundEffect>(x)).ToArray());

            ActiveGlitchesInfo = NesGlitchesType.GetProperty("ActiveGlitches");
            FreezeProbabilityInfo = NesGlitchesType.GetProperty("FreezeProbability");
            DisappearProbabilityInfo = NesGlitchesType.GetProperty("DisappearProbability");
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public int ActiveGlitches
        {
            get => (int)ActiveGlitchesInfo.GetValue(glitches, null);
            set => ActiveGlitchesInfo.SetValue(glitches, value, null);
        }
        public float FreezeProbability
        {
            //TODO implement my own version of NesGlitches's screen freezing effect
            get => (float)FreezeProbabilityInfo.GetValue(glitches, null);
            set => FreezeProbabilityInfo.SetValue(glitches, value, null);
        }
        public float DisappearProbability
        {
            get => (float)DisappearProbabilityInfo.GetValue(glitches, null);
            set => DisappearProbabilityInfo.SetValue(glitches, value, null);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ServiceHelper.RemoveComponent(glitches);
        }

        public override void Update(GameTime gameTime)
        {
        }

        public override void Draw(GameTime gameTime)
        {
        }
    }
}
