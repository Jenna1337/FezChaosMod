using FezGame.ChaosMod;
using System;
using System.Collections.Generic;
using System.IO;
using FezGame.ChaosMod;
using static FezGame.ChaosMod.FezChaosMod;
using System.Linq;
using System.Reflection;

namespace FezGame
{
    internal class ChaosModSettings
    {
        public bool chaosModeEnabled = true;
        public float delayBetweenEffects = 10f;
        public float durationMultiplier = 1f;
        public int maxEffectsToDisplay = 10;
        public int latestEffectsToDisplay = 5;

        public bool allowRotateAnywhere = false;
        public bool firstPersonAnywhere = false;
        public bool stereoMode = false;
        public bool zuSpeakEnglish = false;
        public bool showDebugInfo = true;

        public List<ChaosEffectSetting> chaosEffectSettings = FezChaosMod.Instance.ChaosEffectsList.Select(
            effect => new ChaosEffectSetting(effect)).ToList();

        public class ChaosEffectSetting
        {
            private double Ratio;
            private double Duration;
            private readonly string Name;
            AdditionalChaosEffectSettings AdditionalSettings;

            public ChaosEffectSetting(ChaosEffect effect)
            {
                this.Ratio = effect._ratio;
                this.Duration = effect._duration;
                this.Name = effect.Name;
                this.AdditionalSettings = effect.AdditionalSettings;
            }
        }

        private static ChaosModSettings DefaultSettings = new ChaosModSettings();

        public ChaosModSettings()
        {
        }

        // --- Mappers for INI Settings Files ---

        public Dictionary<string, object> GetAllInputsValues(bool prependControlNameIfMultivalued = false)
        {
            Dictionary<string, object> settings = [];
            IteratePublicFields(typeof(ChaosModSettings), field =>
            {
                settings.TryAdd(field.Name, field.GetValue(this));
            });
            return settings;
        }
        public void SetAllInputsValues(Dictionary<string, string> ctrlvals)
        {
            IteratePublicFields(typeof(ChaosModSettings), field =>
            {
                field.SetValue(this, Convert.ChangeType(ctrlvals[field.Name], field.FieldType, System.Globalization.CultureInfo.InvariantCulture));
            });
        }

        internal void Reset()
        {
            //copy values from DefaultSettings into this
            DefaultSettings.CopyInto(this);
        }

        private void CopyInto(ChaosModSettings target)
        {
            IteratePublicFields(typeof(ChaosModSettings), field =>
            {
                field.SetValue(target, field.GetValue(this));
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingsName"></param>
        /// <param name="existingValue"></param>
        /// <returns>new value</returns>
        public delegate object SettingsFunction(FieldInfo settingsName, object existingValue);
        private void IterateAllSettings(SettingsFunction MappingFunc)
        {
            IteratePublicFields(typeof(ChaosModSettings), field =>
            {
                field.SetValue(this, MappingFunc(field, field.GetValue(this)));
            });
        }
        private void IteratePublicFields(Type type, Action<FieldInfo> MappingFunc)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType.IsPrimitive)
                {
                    MappingFunc(field);
                }
                //TODO handle nested field
            }
        }
    }

}