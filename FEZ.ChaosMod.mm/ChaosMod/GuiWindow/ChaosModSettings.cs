using FezEngine.Components;
using FezGame.ChaosMod;
using FezGame.ChaosMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using static FezGame.ChaosMod.FezChaosMod;

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

        public List<ChaosEffectSetting> chaosEffectSettings;

        public class ChaosEffectSetting
        {
            public double Ratio;
            public double Duration;
            public bool Enabled;
            public readonly string Name;
            public AdditionalChaosEffectSettings AdditionalSettings;

            public ChaosEffectSetting(ChaosEffect effect)
            {
                this.Ratio = effect._ratio;
                this.Duration = effect._duration;
                this.Enabled = effect.Enabled;
                this.Name = effect.Name;
                this.AdditionalSettings = effect.AdditionalSettings;
            }
        }

        private static ChaosModSettings DefaultSettings = new ChaosModSettings();

        public ChaosModSettings()
        {
            Waiters.Wait(() => FezChaosMod.DidInit, () =>
            {
                chaosEffectSettings = FezChaosMod.Instance.ChaosEffectsList.Select(
                effect => new ChaosEffectSetting(effect)).ToList();
            });
        }

        // --- Mappers for INI Settings Files ---

        public Dictionary<string, object> GetAllInputsValues(bool prependControlNameIfMultivalued = false)
        {
            Dictionary<string, object> settings = [];
            IteratePublicFields(typeof(ChaosModSettings), this, (field, ob, path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    path += ".";
                }
                settings.TryAdd(path + field.Name, field.GetValue(ob));
            });
            return settings;
        }
        public void SetAllInputsValues(Dictionary<string, string> ctrlvals)
        {
            IteratePublicFields(typeof(ChaosModSettings), this, (field, ob, path) =>
            {
                field.SetValue(ob, Convert.ChangeType(ctrlvals[field.Name], field.FieldType, System.Globalization.CultureInfo.InvariantCulture));
            });
        }

        internal void Reset()
        {
            //copy values from DefaultSettings into this
            DefaultSettings.CopyInto(this);
        }

        private void CopyInto(ChaosModSettings target)
        {
            IteratePublicFields(typeof(ChaosModSettings), this, (field, ob, _) =>
            {
                field.SetValue(target, field.GetValue(ob));
            });
        }

        private void IteratePublicFields(Type type, object ob, Action<FieldInfo, object, string> MappingFunc, string path = "")
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var FieldType = field.FieldType;
                if (FieldType.IsPrimitive)
                {
                    MappingFunc(field, ob, path);
                }
                else
                {
                    if (FieldType.IsListType())
                    {
                        Type listItemType = FieldType.GetGenericArguments()[0];
                        System.Collections.IList list = (System.Collections.IList)field.GetValue(ob);
                        var basepath = path;
                        foreach (var item in list)
                        {
                            path = basepath;
                            if (typeof(ChaosEffectSetting).Equals(listItemType))
                            {
                                if (!string.IsNullOrEmpty(path))
                                {
                                    path += ".";
                                }
                                path += ((ChaosEffectSetting)item).Name;
                            }
                            IteratePublicFields(listItemType, item, MappingFunc, path);
                        }
                    }
                    else if (FieldType.Equals(typeof(AdditionalChaosEffectSettings)))
                    {
                        var addSetts = (AdditionalChaosEffectSettings)field.GetValue(ob);
                        //TODO
                        //addSetts.AdditionalSettingsCheckListList.ForEach(MappingFunc(Field));
                    }
                }
            }
        }
    }
    internal static class TypeExtensions
    {
        public static bool IsListType(this Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }
    }

}