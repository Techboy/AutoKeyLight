namespace CamOnKeyLight.Core
{
    public sealed class HotkeyChord : IEquatable<HotkeyChord>
    {
        public HotkeyChord(bool control, bool alt, bool shift, string key)
        {
            Control = control;
            Alt = alt;
            Shift = shift;
            Key = key.Trim().ToUpperInvariant();
        }

        public bool Control { get; }
        public bool Alt { get; }
        public bool Shift { get; }
        public string Key { get; }

        public bool Equals(HotkeyChord? other)
        {
            return other != null
                && Control == other.Control
                && Alt == other.Alt
                && Shift == other.Shift
                && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as HotkeyChord);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Control, Alt, Shift, Key.ToUpperInvariant());
        }
    }

    public sealed record HotkeyAssignment(bool Applied, LightMode Mode, HotkeyChord? ActiveChord, string? Error)
    {
        public static HotkeyAssignment Succeeded(LightMode mode, HotkeyChord? chord)
        {
            return new HotkeyAssignment(true, mode, chord, null);
        }

        public static HotkeyAssignment Failed(LightMode mode, string error, HotkeyChord? stillActive)
        {
            return new HotkeyAssignment(false, mode, stillActive, error);
        }
    }

    public interface IHotkeyRegistrar
    {
        bool TryRegister(int id, HotkeyChord chord);
        void Unregister(int id);
    }

    public static class HotkeyIds
    {
        public static int For(LightMode mode)
        {
            return mode switch
            {
                LightMode.Auto => 1,
                LightMode.On => 2,
                LightMode.Off => 3,
                _ => 0
            };
        }

        public static bool TryMode(int id, out LightMode mode)
        {
            switch (id)
            {
                case 1:
                    mode = LightMode.Auto;
                    return true;
                case 2:
                    mode = LightMode.On;
                    return true;
                case 3:
                    mode = LightMode.Off;
                    return true;
                default:
                    mode = LightMode.Auto;
                    return false;
            }
        }
    }

    public static class HotkeyGrammar
    {
        private static readonly HashSet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ctrl+A", "Ctrl+C", "Ctrl+F", "Ctrl+N", "Ctrl+O", "Ctrl+P", "Ctrl+S", "Ctrl+T",
            "Ctrl+V", "Ctrl+W", "Ctrl+X", "Ctrl+Y", "Ctrl+Z", "Alt+F4", "Alt+Tab", "Ctrl+Esc",
            "Ctrl+Shift+Esc"
        };

        public static string Format(HotkeyChord chord)
        {
            List<string> parts = new List<string>();
            if (chord.Control)
                parts.Add("Ctrl");
            if (chord.Alt)
                parts.Add("Alt");
            if (chord.Shift)
                parts.Add("Shift");
            parts.Add(chord.Key);
            return string.Join("+", parts);
        }

        public static bool TryParse(string? text, out HotkeyChord? chord, out string? error)
        {
            chord = null;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
                return true;

            string[] parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool control = false;
            bool alt = false;
            bool shift = false;
            string? key = null;
            foreach (string part in parts)
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    control = true;
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    alt = true;
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    shift = true;
                else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Windows key shortcuts are not used, so the app does not clash with system shortcuts.";
                    return false;
                }
                else if (key != null)
                {
                    error = "Use one key with Ctrl, Alt, or Shift.";
                    return false;
                }
                else
                {
                    key = part;
                }
            }

            if (key == null)
            {
                error = "Add a letter, number, or function key as well as Ctrl, Alt, or Shift.";
                return false;
            }

            return TryCreate(control, alt, shift, key, out chord, out error);
        }

        public static bool TryCreate(bool control, bool alt, bool shift, string key, out HotkeyChord? chord, out string? error)
        {
            chord = null;
            error = null;
            if (!control && !alt && !shift)
            {
                error = "Add Ctrl, Alt, or Shift so a normal key still types.";
                return false;
            }

            if (!IsAllowedKey(key))
            {
                error = "Use a letter, number, or function key (F1 through F24).";
                return false;
            }

            HotkeyChord candidate = new HotkeyChord(control, alt, shift, key);
            string formatted = Format(candidate);
            if (Reserved.Contains(formatted))
            {
                error = formatted + " is used by Windows or by text editing. Choose another, such as Ctrl+Alt+Shift and a letter.";
                return false;
            }

            chord = candidate;
            return true;
        }

        public static bool TryVirtualKey(HotkeyChord chord, out uint modifiers, out uint virtualKey)
        {
            modifiers = 0;
            if (chord.Control)
                modifiers |= 0x0002;
            if (chord.Alt)
                modifiers |= 0x0001;
            if (chord.Shift)
                modifiers |= 0x0004;
            modifiers |= 0x4000;

            if (chord.Key.Length == 1 && chord.Key[0] is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                virtualKey = chord.Key[0];
                return true;
            }

            if (chord.Key.Length is >= 2 and <= 3
                && chord.Key[0] == 'F'
                && int.TryParse(chord.Key.AsSpan(1), out int function)
                && function is >= 1 and <= 24)
            {
                virtualKey = (uint)(0x70 + function - 1);
                return true;
            }

            virtualKey = 0;
            return false;
        }

        private static bool IsAllowedKey(string key)
        {
            if (key.Length == 1 && key[0] is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                return true;

            if (key.Length is < 2 or > 3 || (key[0] != 'F' && key[0] != 'f'))
                return false;

            return int.TryParse(key.AsSpan(1), out int function) && function is >= 1 and <= 24;
        }
    }

    public sealed class HotkeyBook
    {
        private readonly Dictionary<LightMode, HotkeyChord> active = new Dictionary<LightMode, HotkeyChord>();

        public HotkeyChord? Current(LightMode mode)
        {
            return active.TryGetValue(mode, out HotkeyChord? chord) ? chord : null;
        }

        public string Display(LightMode mode)
        {
            HotkeyChord? chord = Current(mode);
            return chord == null ? "None" : HotkeyGrammar.Format(chord);
        }

        public HotkeyAssignment Assign(LightMode mode, HotkeyChord? requested, IHotkeyRegistrar registrar)
        {
            if (requested == null)
            {
                registrar.Unregister(HotkeyIds.For(mode));
                active.Remove(mode);
                return HotkeyAssignment.Succeeded(mode, null);
            }

            foreach (KeyValuePair<LightMode, HotkeyChord> pair in active)
            {
                if (pair.Key != mode && pair.Value.Equals(requested))
                {
                    return HotkeyAssignment.Failed(
                        mode,
                        HotkeyGrammar.Format(requested) + " is already used for " + pair.Key + ".",
                        Current(mode));
                }
            }

            if (active.TryGetValue(mode, out HotkeyChord? current) && current.Equals(requested))
                return HotkeyAssignment.Succeeded(mode, requested);

            int id = HotkeyIds.For(mode);
            registrar.Unregister(id);
            if (!registrar.TryRegister(id, requested))
            {
                string error = HotkeyGrammar.Format(requested) + " is already used by another program. The shortcut was not changed.";
                if (current != null && !registrar.TryRegister(id, current))
                {
                    active.Remove(mode);
                    return HotkeyAssignment.Failed(mode, error + " The previous shortcut could not be restored.", null);
                }

                return HotkeyAssignment.Failed(mode, error, current);
            }

            active[mode] = requested;
            return HotkeyAssignment.Succeeded(mode, requested);
        }

        public IReadOnlyList<string> BindSaved(string? auto, string? on, string? off, IHotkeyRegistrar registrar)
        {
            List<string> errors = new List<string>();
            BindOne(LightMode.Auto, auto, registrar, errors);
            BindOne(LightMode.On, on, registrar, errors);
            BindOne(LightMode.Off, off, registrar, errors);
            return errors;
        }

        public void ReleaseAll(IHotkeyRegistrar registrar)
        {
            registrar.Unregister(HotkeyIds.For(LightMode.Auto));
            registrar.Unregister(HotkeyIds.For(LightMode.On));
            registrar.Unregister(HotkeyIds.For(LightMode.Off));
            active.Clear();
        }

        private void BindOne(LightMode mode, string? text, IHotkeyRegistrar registrar, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!HotkeyGrammar.TryParse(text, out HotkeyChord? chord, out string? error) || chord == null)
            {
                errors.Add("Saved shortcut for " + mode + " was not registered. " + error);
                return;
            }

            HotkeyAssignment result = Assign(mode, chord, registrar);
            if (!result.Applied && result.Error != null)
                errors.Add(result.Error);
        }
    }
}
