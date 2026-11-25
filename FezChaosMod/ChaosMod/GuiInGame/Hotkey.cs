using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FezGame
{
    internal class Hotkey
    {
        private Keys MainKey { get; set; }
        private KeyModifiers ModifierKeys { get; set; }
        public bool IsPressed => GetIsPressed();

        public override string ToString()
        {
            if (ModifierKeys == KeyModifiers.None)
                return MainKey.ToString(); // Or string.Empty if no trigger key

            return $"{ModifierKeys.ToString().Replace(", ", "+")}+{MainKey}";
        }

        private static readonly Dictionary<string, KeyModifiers> AliasMap = BuildAliasMap();
        [Flags]
        private enum KeyModifiers
        {
            [KeyModifier]
            None = 0,
            [KeyModifier("Control")]
            Ctrl = 1,
            [KeyModifier("Option")]
            Alt = 2,
            [KeyModifier]
            Shift = 4,
            [KeyModifier("Command", "Win", "Windows")]
            Cmd = 8,
        }

        public Hotkey(Keys key) : this(key, KeyModifiers.None)
        {
        }
        private Hotkey(Keys key, KeyModifiers modifiers)
        {
            MainKey = key;
            ModifierKeys = modifiers;
        }
        public Hotkey(string keyCombo)
        {
            MainKey = Keys.None;
            ModifierKeys = KeyModifiers.None;

            if (string.IsNullOrWhiteSpace(keyCombo))
                return;

            foreach (string rawToken in keyCombo.Split('+'))
            {
                string token = rawToken.Trim();
                if (string.IsNullOrEmpty(token))
                    continue;

                if (AliasMap.TryGetValue(token, out KeyModifiers mod))
                {
                    ModifierKeys |= mod;
                }
                else if (!int.TryParse(token, out _) && Enum.TryParse<Keys>(token, ignoreCase: true, out Keys key))
                {
                    MainKey = key;
                }
            }
        }
        private bool GetIsPressed()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (!keyboardState.IsKeyDown(MainKey))
            {
                return false;
            }
            return GetActiveModifiers(keyboardState) == ModifierKeys;
        }
        private static KeyModifiers GetActiveModifiers(KeyboardState state)
        {
            KeyModifiers active = KeyModifiers.None;

            if (state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl))
                active |= KeyModifiers.Ctrl;
            if (state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt))
                active |= KeyModifiers.Alt;
            if (state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift))
                active |= KeyModifiers.Shift;
            if (state.IsKeyDown(Keys.LeftWindows) || state.IsKeyDown(Keys.RightWindows))
                active |= KeyModifiers.Cmd;

            return active;
        }
        private static readonly HashSet<Keys> PressedNonModifiers = [];
        public static bool GetPressedCombo(out Hotkey hotkey)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            PressedNonModifiers.Clear();
            foreach (Keys key in keyboardState.GetPressedKeys())
            {
                if (!key.IsModifier())
                {
                    PressedNonModifiers.Add(key);
                }
            }
            // Require exactly one non-modifier key so combos register cleanly
            if (PressedNonModifiers.Count == 1)
            {
                foreach (Keys mainKey in PressedNonModifiers)
                {
                    hotkey = new Hotkey(mainKey, GetActiveModifiers(keyboardState));
                    return true;
                }
            }
            hotkey = null;
            return false;
        }

        private static Dictionary<string, KeyModifiers> BuildAliasMap()
        {
            var map = new Dictionary<string, KeyModifiers>(StringComparer.OrdinalIgnoreCase);

            foreach (FieldInfo field in typeof(KeyModifiers).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<KeyModifierAttribute>();
                if (attr == null)
                    continue;

                var value = (KeyModifiers)field.GetValue(null);
                if (value == KeyModifiers.None)
                    continue;

                // Register the canonical enum name (e.g., "Ctrl")
                map[field.Name] = value;

                // Register all aliases from [KeyModifier(...)]
                foreach (string alias in attr.Aliases)
                {
                    map[alias] = value;
                }
            }

            return map;
        }
        private class KeyModifierAttribute : Attribute
        {
            public KeyModifierAttribute(params string[] aliases)
            {
                Aliases = aliases;
            }

            public string[] Aliases { get; }
        }

    }

    public static class KeysExtensions
    {
        public static bool IsModifier(this Keys key)
        {
            switch (key)
            {
            case Keys.LeftShift:
            case Keys.RightShift:
            case Keys.LeftControl:
            case Keys.RightControl:
            case Keys.LeftAlt:
            case Keys.RightAlt:
            case Keys.LeftWindows:
            case Keys.RightWindows:
                return true;
            default:
                return false;
            }
        }
    }
}
