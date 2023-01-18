#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Microsoft.Xna.Framework;
using System.Reflection;
using Common;
using FezEngine.Tools;
using FezGame.Components;
using FezEngine.Components;
using FezGame.ChaosMod;

namespace FezGame {
    class patch_Fez : Fez {

        public static FezChaosMod ChaosMod { get; internal set; }
        public static bool ChaosMode = true;
        public static bool RoomRandoMode;// = true;
        public static bool ItemRandoMode;

        public extern void orig_Update(GameTime gameTime);
        protected override void Update(GameTime gameTime) {
            orig_Update(gameTime);
        }

        public extern void orig_Draw(GameTime gameTime);
        protected override void Draw(GameTime gameTime) {
            orig_Draw(gameTime);
            //if (ChaosMod != null)
            //    ChaosMod.Draw((float)Math.Floor(base.GraphicsDevice.GetViewScale()), base.GraphicsDevice.Viewport);
        }

        public extern void orig_Initialize();
        protected override void Initialize() {
            orig_Initialize();
        }

        public static extern void orig_LoadComponents(Fez game);
        protected static void LoadComponents(Fez game) {
            if (ServiceHelper.FirstLoadDone)
                return;
            orig_LoadComponents(game);
            ServiceHelper.AddComponent(ChaosMod = new FezChaosMod(game));
            ServiceHelper.FirstLoadDone = true;
        }

    }
}
