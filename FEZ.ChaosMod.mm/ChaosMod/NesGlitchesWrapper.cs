﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FezGame.ChaosMod
{
    class NesGlitchesWrapper
    {
        private readonly object glitches;
        private static Type NesGlitchesType;
        private readonly PropertyInfo ActiveGlitchesInfo;
        private readonly PropertyInfo FreezeProbabilityInfo;
        private readonly PropertyInfo DisappearProbabilityInfo;

        public NesGlitchesWrapper(object glitches, Type type)
        {
            this.glitches = glitches;
            NesGlitchesType = type;
            ActiveGlitchesInfo = NesGlitchesType.GetProperty("ActiveGlitches");
            FreezeProbabilityInfo = NesGlitchesType.GetProperty("FreezeProbability");
            DisappearProbabilityInfo = NesGlitchesType.GetProperty("DisappearProbability");
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

        public void Dispose()
        {
            FezEngine.Tools.ServiceHelper.RemoveComponent((Microsoft.Xna.Framework.IGameComponent)glitches);
        }
    }
}
