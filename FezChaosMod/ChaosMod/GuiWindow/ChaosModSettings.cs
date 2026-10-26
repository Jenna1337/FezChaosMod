using FezGame.ChaosMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static FezGame.ChaosMod.FezChaosMod;

namespace FezGame
{
    internal class ChaosModSettings
    {
        [ChaosModSetting]
        public bool chaosModeEnabled = true;
        [ChaosModSetting]
        public float delayBetweenEffects = 10f;
        [ChaosModSetting]
        public float durationMultiplier = 1f;
        [ChaosModSetting]
        public int maxEffectsToDisplay = 10;
        [ChaosModSetting]
        public int latestEffectsToDisplay = 5;

        [ChaosModSetting]
        public bool allowRotateAnywhere = false;
        [ChaosModSetting]
        public bool firstPersonAnywhere = false;
        [ChaosModSetting]
        public bool stereoMode = false;
        [ChaosModSetting]
        public bool zuSpeakEnglish = false;
        [ChaosModSetting]
        public bool showDebugInfo = true;

        [ChaosModSetting]
        public List<ChaosEffectSetting> chaosEffectSettings;

        public class ChaosEffectSetting
        {
            [ChaosModSetting]
            public double Weight;
            [ChaosModSetting]
            public double Duration;
            [ChaosModSetting]
            public bool Enabled;
            [ChaosModSetting]
            public AdditionalChaosEffectSettings AdditionalSettings;

            public readonly string Name;
            private readonly ChaosEffect Effect;
            public ChaosEffectSetting(ChaosEffect effect)
            {
                Effect = effect;
                Weight = effect._weight;
                Duration = effect._duration;
                Enabled = effect.Enabled;
                Name = effect.Name;
                AdditionalSettings = effect.AdditionalSettings?.Clone();
            }
            public void ApplySettings()
            {
                Effect._weight = Weight;
                Effect._duration = Duration;
                Effect.Enabled = Enabled;
                if (AdditionalSettings != null)
                {
                    //Note: these actually point to the same object
                    //TODO verify additional settings are applied properly
                    var newList = AdditionalSettings.AdditionalSettingsCheckListList;
                    var oldList = Effect.AdditionalSettings.AdditionalSettingsCheckListList;
                    // 1. Turn the new data into a lookup dictionary for fast speed
                    var newLookup = newList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // 2. Filter oldList to remove keys that are being updated by newList
                    oldList.RemoveAll(kvp => newLookup.ContainsKey(kvp.Key));

                    // 3. Add all the items from newList (both updates and brand-new items)
                    oldList.AddRange(newList);

                }
            }
        }
        public void ApplySettings()
        {
            FezChaosMod.Enabled = chaosModeEnabled;
            FezChaosMod.Instance.DelayBetweenEffects = delayBetweenEffects;
            FezChaosMod.Instance.EffectsDurationMultiplier = durationMultiplier;
            FezChaosMod.Instance.MaxActiveEffectsToDisplay = maxEffectsToDisplay;
            FezChaosMod.Instance.LatestEffectsToDisplay = latestEffectsToDisplay;

            FezChaosMod.Instance.AllowRotateAnywhere = allowRotateAnywhere;
            FezChaosMod.Instance.AllowFirstPersonAnywhere = firstPersonAnywhere;
            FezEngine.Tools.ServiceHelper.Get<Services.IGameStateManager>().StereoMode = stereoMode;
            FezChaosMod.Instance.ZuSpeakEnglish = zuSpeakEnglish;
            FezChaosMod.Instance.ShowDebugInfo = showDebugInfo;

            foreach (var item in chaosEffectSettings)
            {
                item.ApplySettings();
            }
        }


        private static ChaosModSettings DefaultSettings = null;
        private static bool IsInit = false;
        public ChaosModSettings()
        {
            if (!IsInit)
            {
                IsInit = true;
                DefaultSettings = new ChaosModSettings();
            }
            chaosEffectSettings = FezChaosMod.Instance.ChaosEffectsList.Select(effect => new ChaosEffectSetting(effect)).ToList();


        }

        // --- Mappers for INI Settings Files ---
        private const string UidReflectionPathSeparator = ".";
        private const string GeneralSettingsSectionName = "General";
        private const string EffectsSectionName = "Effects";
        string GetSectionName(FieldInfo field, string path)
        {
            string sectionName = "";
            if (typeof(ChaosModSettings).Equals(field.DeclaringType))
            {
                sectionName = GeneralSettingsSectionName;
            }
            if (typeof(ChaosEffectSetting).Equals(field.DeclaringType))
            {
                sectionName = EffectsSectionName;
            }
            if (field.FieldType.Equals(typeof(AdditionalChaosEffectSettings)))
            {
                sectionName = path;
            }
            return sectionName;
        }
        public IniFile GetAllInputsValues(ChaosModSettings source = null)
        {
            if (source == null)
            {
                source = this;
            }
            IniFile settings = [];
            IniSection GetSection(FieldInfo field, string path)
            {
                string sectionName = GetSectionName(field, path);
                IniSection section;
                if (!settings.TryGetValue(sectionName, out section))
                {
                    section = new IniSection();
                    settings.Add(sectionName, section);
                }
                return section;
            }
            string formatValue(object value)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value);
            }
            IteratePublicFields(typeof(ChaosModSettings), source, (field, ob, path) =>
            {
                bool isAdditionalSettings = field.FieldType.Equals(typeof(AdditionalChaosEffectSettings));
                if (!string.IsNullOrEmpty(path) && !isAdditionalSettings)
                {
                    path += UidReflectionPathSeparator;
                }
                IniSection section = GetSection(field, path);
                if (!isAdditionalSettings)
                {
                    section.TryAdd(path + field.Name, formatValue(field.GetValue(ob)));
                }
                else
                {
                    AdditionalSettingsCheckListItem keyValuePair = (AdditionalSettingsCheckListItem)ob;
                    section.TryAdd(keyValuePair.Key, formatValue(keyValuePair.Value));
                }
            });
            return settings;
        }
        public void SetAllInputsValues(IniFile ctrlvals)
        {
            string[] sectionsToCheckFirst = [
                ChaosModSettingsHelper.MetadataSectionName,
            ];
            IteratePublicFields(typeof(ChaosModSettings), this, (field, ob, path) =>
            {
                bool isAdditionalSettings = field.FieldType.Equals(typeof(AdditionalChaosEffectSettings));
                if (!string.IsNullOrEmpty(path) && !isAdditionalSettings)
                {
                    path += UidReflectionPathSeparator;
                }
                string uid = path + field.Name;
                if (isAdditionalSettings)
                {
                    uid = ((AdditionalSettingsCheckListItem)ob).Key;
                }
                string expectedSection = GetSectionName(field, path);
                if (ctrlvals.TryGetValue(uid, out string val, [expectedSection, .. sectionsToCheckFirst]))
                {
                    var newType = isAdditionalSettings ? typeof(bool) : field.FieldType;
                    object newVal = Convert.ChangeType(val, newType, System.Globalization.CultureInfo.InvariantCulture);
                    if (isAdditionalSettings)
                    {
                        ((AdditionalSettingsCheckListItem)ob).Value = (bool)newVal;
                    }
                    else
                    {
                        field.SetValue(ob, newVal);
                    }
                }
                else
                {
                    //TODO warn or something 
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                }
            });
            ApplySettings();
        }

        internal void Reset()
        {
            //copy values from DefaultSettings into this
            SetAllInputsValues(GetAllInputsValues(DefaultSettings));
        }

        private void IteratePublicFields(Type type, object ob, Action<FieldInfo, object, string> MappingFunc, string path = "")
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<ChaosModSettingAttribute>();
                if (attribute == null)
                {
                    continue;
                }
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
                                    path += UidReflectionPathSeparator;
                                }
                                path += ((ChaosEffectSetting)item).Name;
                            }
                            IteratePublicFields(listItemType, item, MappingFunc, path);
                        }
                    }
                    else if (FieldType.Equals(typeof(AdditionalChaosEffectSettings)))
                    {
                        var addSetts = (AdditionalChaosEffectSettings)field.GetValue(ob);
                        if (addSetts != null)
                        {
                            addSetts.AdditionalSettingsCheckListList.ForEach(kp => MappingFunc(field, kp, addSetts.AdditionalSettingsName));
                        }
                    }
                }
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        private class ChaosModSettingAttribute : Attribute
        {
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