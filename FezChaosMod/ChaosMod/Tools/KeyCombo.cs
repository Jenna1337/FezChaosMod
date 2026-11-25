using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FezGame
{
    internal class KeyCombo : IEquatable<KeyCombo>
    {
        private Keys MainKey { get; }
        private KeyModifiers ModifierKeys { get; }
        private bool wasPressed = false;
        public bool IsPressed => GetIsPressed();

        public override string ToString()
        {
            if (ModifierKeys == KeyModifiers.None)
            {
                return MainKey.ToString();
            }

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
            [KeyModifier("Cmd", "Command", "Win", "Windows", "OSKey", "Super")]
            Meta = 8,
        }

        public KeyCombo(Keys key) : this(key, KeyModifiers.None)
        {
        }
        private KeyCombo(Keys key, KeyModifiers modifiers)
        {
            MainKey = key;
            ModifierKeys = modifiers;
        }
        public KeyCombo(string keyCombo)
        {
            MainKey = Keys.None;
            ModifierKeys = KeyModifiers.None;

            if (string.IsNullOrWhiteSpace(keyCombo))
            {
                return;
            }

            foreach (string rawToken in keyCombo.Split('+'))
            {
                string token = rawToken.Trim();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

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
            if (MainKey == Keys.None)
            {
                return false;
            }

            KeyboardState keyboardState = Keyboard.GetState();
            bool isDown = keyboardState.IsKeyDown(MainKey) && GetActiveModifiers(keyboardState) == ModifierKeys;
            bool justPressed = isDown && !wasPressed;
            wasPressed = isDown;

            return justPressed;
        }
        private static KeyModifiers GetActiveModifiers(KeyboardState state)
        {
            KeyModifiers active = KeyModifiers.None;

            if (state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl))
            {
                active |= KeyModifiers.Ctrl;
            }

            if (state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt))
            {
                active |= KeyModifiers.Alt;
            }

            if (state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift))
            {
                active |= KeyModifiers.Shift;
            }

            if (state.IsKeyDown(Keys.LeftWindows) || state.IsKeyDown(Keys.RightWindows))
            {
                active |= KeyModifiers.Meta;
            }

            return active;
        }
        private static readonly HashSet<Keys> PressedNonModifiers = [];
        public static bool GetPressedCombo(out KeyCombo hotkey)
        {
            //TODO maybe use this method to add a button to the GUI to change the keybind? 
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
                    hotkey = new KeyCombo(mainKey, GetActiveModifiers(keyboardState));
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
                {
                    continue;
                }

                var value = (KeyModifiers)field.GetValue(null);
                if (value == KeyModifiers.None)
                {
                    continue;
                }

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

        public bool Equals(KeyCombo other)
        {
            if (other is null)
            {
                return false;
            }

            return MainKey == other.MainKey && ModifierKeys == other.ModifierKeys;
        }

        public override bool Equals(object obj) => Equals(obj as KeyCombo);

        public override int GetHashCode() => HashCode.Combine(MainKey, ModifierKeys);

        public static bool operator ==(KeyCombo left, KeyCombo right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(KeyCombo left, KeyCombo right) => !(left == right);
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
