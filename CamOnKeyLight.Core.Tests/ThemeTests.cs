using CamOnKeyLight.Core;

namespace CamOnKeyLight.Core.Tests
{
    public class ThemeTests
    {
        [Theory]
        [InlineData(ThemePreference.Light, true, false, EffectiveTheme.Light)]
        [InlineData(ThemePreference.Dark, true, false, EffectiveTheme.Dark)]
        [InlineData(ThemePreference.FollowWindows, true, false, EffectiveTheme.Light)]
        [InlineData(ThemePreference.FollowWindows, false, false, EffectiveTheme.Dark)]
        [InlineData(ThemePreference.FollowWindows, null, false, EffectiveTheme.Light)]
        [InlineData(ThemePreference.Dark, false, true, EffectiveTheme.System)]
        public void ResolverHonorsTheChoiceAndHighContrast(ThemePreference preference, bool? appsUseLight, bool highContrast, EffectiveTheme expected)
        {
            Assert.Equal(expected, ThemeResolver.Resolve(preference, appsUseLight, highContrast));
        }

        [Theory]
        [InlineData("dark", ThemePreference.Dark)]
        [InlineData("Light", ThemePreference.Light)]
        [InlineData("FollowWindows", ThemePreference.FollowWindows)]
        [InlineData("nope", ThemePreference.FollowWindows)]
        [InlineData(null, ThemePreference.FollowWindows)]
        public void UnknownThemeFallsBackToFollowWindows(string? value, ThemePreference expected)
        {
            Assert.Equal(expected, ThemePreferenceParser.Parse(value));
        }

        [Fact]
        public void BothPalettesStayReadable()
        {
            foreach (ThemePalette palette in new[] { ThemePalette.Light, ThemePalette.Dark })
            {
                foreach ((string name, ThemeColor text, ThemeColor behind) in palette.TextPairs())
                    Assert.True(Contrast.Ratio(text, behind) >= 4.5, name + " contrast was " + Contrast.Ratio(text, behind));
            }
        }
    }
}
