using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FezGame.ChaosMod
{
    class NesGlitchesWrapper
    {
        private readonly object glitches;
        private static readonly Type NesGlitchesType = Type.GetType("FezGame.Components.NesGlitches");

        public NesGlitchesWrapper(object glitches)
        {
            this.glitches = glitches;

        }

        private readonly PropertyInfo ActiveGlitchesInfo = NesGlitchesType.GetProperty("ActiveGlitches");
        private readonly PropertyInfo FreezeProbabilityInfo = NesGlitchesType.GetProperty("FreezeProbability");
        private readonly PropertyInfo DisappearProbabilityInfo = NesGlitchesType.GetProperty("DisappearProbability");

        public int ActiveGlitches
        {
            get => (int)ActiveGlitchesInfo.GetValue(glitches, null);
            set => ActiveGlitchesInfo.SetValue(glitches, value, null);
        }
        public float FreezeProbability
        {
            get => (float)FreezeProbabilityInfo.GetValue(glitches, null);
            set => FreezeProbabilityInfo.SetValue(glitches, value, null);
        }
        public float DisappearProbability
        {
            get => (float)DisappearProbabilityInfo.GetValue(glitches, null);
            set => DisappearProbabilityInfo.SetValue(glitches, value, null);
        }

        public void Dispose()
        {
            FezEngine.Tools.ServiceHelper.RemoveComponent((Microsoft.Xna.Framework.IGameComponent)glitches);
        }
    }
}
