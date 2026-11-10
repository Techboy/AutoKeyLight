namespace CamOnKeyLight.Core
{
    public enum ThemePreference
    {
        Light,
        Dark,
        FollowWindows
    }

    public enum EffectiveTheme
    {
        Light,
        Dark,
        /// <summary>
        /// Windows high contrast is active, so the app keeps system colors.
        /// </summary>
        System
    }

    public readonly record struct ThemeColor(byte R, byte G, byte B)
    {
        public static ThemeColor FromHex(uint hex)
        {
            return new ThemeColor((byte)((hex >> 16) & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)(hex & 0xFF));
        }
    }

    public readonly record struct ThemePalette(
        ThemeColor Background,
        ThemeColor Foreground,
        ThemeColor InputBackground,
        ThemeColor InputForeground,
        ThemeColor ButtonBackground,
        ThemeColor ButtonForeground,
        ThemeColor StatusOn,
        ThemeColor StatusOff,
        ThemeColor Error,
        ThemeColor Muted,
        ThemeColor Border,
        ThemeColor MenuBackground,
        ThemeColor MenuForeground,
        ThemeColor MenuHighlight,
        ThemeColor MenuHighlightText,
        ThemeColor Link)
    {
        public static ThemePalette Light { get; } = new(
            Background: ThemeColor.FromHex(0xF3F3F3),
            Foreground: ThemeColor.FromHex(0x202020),
            InputBackground: ThemeColor.FromHex(0xFFFFFF),
            InputForeground: ThemeColor.FromHex(0x202020),
            ButtonBackground: ThemeColor.FromHex(0xE4E4E4),
            ButtonForeground: ThemeColor.FromHex(0x202020),
            StatusOn: ThemeColor.FromHex(0x0E6A32),
            StatusOff: ThemeColor.FromHex(0x9F1D1D),
            Error: ThemeColor.FromHex(0x9F1D1D),
            Muted: ThemeColor.FromHex(0x3E3E3E),
            Border: ThemeColor.FromHex(0x6E6E6E),
            MenuBackground: ThemeColor.FromHex(0xFFFFFF),
            MenuForeground: ThemeColor.FromHex(0x202020),
            MenuHighlight: ThemeColor.FromHex(0xD6E4F5),
            MenuHighlightText: ThemeColor.FromHex(0x202020),
            Link: ThemeColor.FromHex(0x0B4F9C));

        public static ThemePalette Dark { get; } = new(
            Background: ThemeColor.FromHex(0x1F1F1F),
            Foreground: ThemeColor.FromHex(0xF2F2F2),
            InputBackground: ThemeColor.FromHex(0x2C2C2C),
            InputForeground: ThemeColor.FromHex(0xF2F2F2),
            ButtonBackground: ThemeColor.FromHex(0x3C3C3C),
            ButtonForeground: ThemeColor.FromHex(0xF2F2F2),
            StatusOn: ThemeColor.FromHex(0x7DFFB3),
            StatusOff: ThemeColor.FromHex(0xFFB3B0),
            Error: ThemeColor.FromHex(0xFFB3B0),
            Muted: ThemeColor.FromHex(0xD0D0D0),
            Border: ThemeColor.FromHex(0x8A8A8A),
            MenuBackground: ThemeColor.FromHex(0x2C2C2C),
            MenuForeground: ThemeColor.FromHex(0xF2F2F2),
            MenuHighlight: ThemeColor.FromHex(0x355F8A),
            MenuHighlightText: ThemeColor.FromHex(0xF2F2F2),
            Link: ThemeColor.FromHex(0x9ECAFF));

        public IEnumerable<(string Name, ThemeColor Text, ThemeColor Behind)> TextPairs()
        {
            yield return ("text", Foreground, Background);
            yield return ("input", InputForeground, InputBackground);
            yield return ("button", ButtonForeground, ButtonBackground);
            yield return ("status on", StatusOn, Background);
            yield return ("status off", StatusOff, Background);
            yield return ("error", Error, Background);
            yield return ("muted", Muted, Background);
            yield return ("menu", MenuForeground, MenuBackground);
            yield return ("menu selection", MenuHighlightText, MenuHighlight);
            yield return ("link", Link, Background);
        }
    }

    public static class ThemePreferenceParser
    {
        public static ThemePreference Parse(string? value)
        {
            if (value != null
                && Enum.TryParse(value.Trim(), ignoreCase: true, out ThemePreference preference)
                && Enum.IsDefined(preference)
                && string.Equals(Enum.GetName(preference), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return preference;
            }

            return ThemePreference.FollowWindows;
        }
    }

    public static class ThemeResolver
    {
        public static EffectiveTheme Resolve(ThemePreference preference, bool? appsUseLightTheme, bool highContrast)
        {
            if (highContrast)
                return EffectiveTheme.System;

            if (preference == ThemePreference.Light)
                return EffectiveTheme.Light;

            if (preference == ThemePreference.Dark)
                return EffectiveTheme.Dark;

            return appsUseLightTheme == false ? EffectiveTheme.Dark : EffectiveTheme.Light;
        }

        public static ThemePalette PaletteFor(EffectiveTheme theme)
        {
            return theme == EffectiveTheme.Dark ? ThemePalette.Dark : ThemePalette.Light;
        }
    }

    public static class Contrast
    {
        public static double Ratio(ThemeColor a, ThemeColor b)
        {
            double lighter = Math.Max(Luminance(a), Luminance(b));
            double darker = Math.Min(Luminance(a), Luminance(b));
            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double Luminance(ThemeColor color)
        {
            double r = Channel(color.R);
            double g = Channel(color.G);
            double b = Channel(color.B);
            return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
        }

        private static double Channel(byte value)
        {
            double c = value / 255d;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }
}
