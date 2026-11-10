using AutoKeyLight.Core;

namespace AutoKeyLight.Core.Tests
{
    public class SettingsAndStartupTests
    {
        [Fact]
        public void SettingsFileStaysOutsideTheInstallFolder()
        {
            string local = Path.Combine("C:", "Users", "Paul", "AppData", "Local");
            Assert.True(SettingsLocations.SettingsAreOutsideInstall(local));
            Assert.EndsWith(Path.Combine("AutoKeyLight", "user-settings.json"), SettingsLocations.SettingsFile(local));
            Assert.EndsWith(Path.Combine("Programs", "AutoKeyLight"), SettingsLocations.InstalledAppDirectory(local));
            Assert.NotEqual(UpdateSource.PackageName, SettingsLocations.SetupFileName);
        }

        [Fact]
        public void SetupExeIsNotAnUpdatePayload()
        {
            string? error = PackageRules.Validate(new (string, long)[]
            {
                ("AutoKeyLight.exe", 10),
                (SettingsLocations.SetupFileName, 10)
            });
            Assert.NotNull(error);
        }

        [Fact]
        public void ExistingUserConfigKeepsAddressesAndDoesNotInventDescriptions()
        {
            string xml = """
                <configuration>
                  <userSettings>
                    <AutoKeyLight.Properties.Settings>
                      <setting name="IPs" serializeAs="Xml">
                        <value>
                          <ArrayOfString>
                            <string>192.168.1.6</string>
                            <string>192.168.1.5</string>
                          </ArrayOfString>
                        </value>
                      </setting>
                      <setting name="CameraDelay" serializeAs="String"><value>250</value></setting>
                      <setting name="CameraOffDelay" serializeAs="String"><value>0</value></setting>
                      <setting name="Theme" serializeAs="String"><value>Dark</value></setting>
                      <setting name="AutomaticUpdates" serializeAs="String"><value>False</value></setting>
                      <setting name="LightMode" serializeAs="String"><value>On</value></setting>
                      <setting name="HotkeyOn" serializeAs="String"><value>Ctrl+Alt+Shift+O</value></setting>
                    </AutoKeyLight.Properties.Settings>
                  </userSettings>
                </configuration>
                """;

            AppUserSettings? parsed = UserConfigXml.Parse(xml);
            Assert.NotNull(parsed);
            Assert.Equal(new[] { "192.168.1.6", "192.168.1.5" }, parsed!.Lights.Select(light => light.Address).ToArray());
            Assert.All(parsed.Lights, light => Assert.Equal("", light.Description));
            Assert.Equal(250, parsed.CameraOnDelayMs);
            Assert.Equal(0, parsed.CameraOffDelayMs);
            Assert.Equal("Dark", parsed.Theme);
            Assert.False(parsed.AutomaticUpdates);
            Assert.Equal("On", parsed.LightMode);
            Assert.Equal("Ctrl+Alt+Shift+O", parsed.HotkeyOn);
        }

        [Fact]
        public void SavedJsonWinsAndEmptyMigrationDoesNotEraseLights()
        {
            AppUserSettings saved = new AppUserSettings();
            saved.Lights.Add(new LightRecord("10.0.0.8", "Desk"));
            AppUserSettings older = new AppUserSettings();
            older.Lights.Add(new LightRecord("10.0.0.1", "Old"));
            AppUserSettings chosen = SettingsMigration.Choose(saved, new[]
            {
                new SettingsMigration.Candidate(DateTime.UtcNow, older)
            });
            Assert.Equal("Desk", Assert.Single(chosen.Lights).Description);
        }

        [Fact]
        public void MissingJsonImportsTheConfigThatStillHasLights()
        {
            AppUserSettings empty = AppUserSettings.CreateDefault();
            AppUserSettings legacy = new AppUserSettings();
            legacy.Lights.Add(new LightRecord("192.168.1.6", ""));
            legacy.CameraOffDelayMs = 0;
            AppUserSettings chosen = SettingsMigration.Choose(null, new[]
            {
                new SettingsMigration.Candidate(DateTime.UtcNow, empty),
                new SettingsMigration.Candidate(DateTime.UtcNow.AddDays(-1), legacy)
            });
            Assert.Equal("192.168.1.6", Assert.Single(chosen.Lights).Address);
            Assert.Equal(0, chosen.CameraOffDelayMs);
        }

        [Fact]
        public void JsonRoundTripKeepsDescriptions()
        {
            AppUserSettings settings = new AppUserSettings();
            settings.Lights.Add(new LightRecord("192.168.1.6", "Light on the left"));
            AppUserSettings? again = SettingsJson.Deserialize(SettingsJson.Serialize(settings));
            Assert.NotNull(again);
            Assert.Equal("Light on the left", Assert.Single(again!.Lights).Description);
        }

        [Fact]
        public void LegacyUnquotedStartupIsRepairedAndQuoted()
        {
            string current = Path.Combine("C:", "Users", "Paul Smith", "AppData", "Local", "Programs", "AutoKeyLight", "AutoKeyLight.exe");
            string stale = Path.Combine("C:", "Users", "Paul Smith", "Downloads", "AutoKeyLight", "AutoKeyLight.exe");
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["MyApp"] = stale,
                ["SomethingElse"] = "C:\\Windows\\notepad.exe"
            };

            Assert.True(StartupRegistration.HadStartupEnabled(values));
            IReadOnlyList<StartupEdit> edits = StartupRegistration.Enable(current, values);
            Assert.Contains(edits, edit => edit.Name == "AutoKeyLight" && edit.Value == "\"" + current + "\"");
            Assert.Contains(edits, edit => edit.Name == "MyApp" && edit.Value == null);
            Assert.DoesNotContain(edits, edit => edit.Name == "SomethingElse");
        }

        [Fact]
        public void UnrelatedMyAppValueIsLeftAlone()
        {
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["MyApp"] = "C:\\Tools\\Other.exe"
            };
            Assert.False(StartupRegistration.HadStartupEnabled(values));
            Assert.Empty(StartupRegistration.Disable(values));
        }

        [Fact]
        public void DisableRemovesOnlyThisAppsEntries()
        {
            string current = "C:\\Apps\\AutoKeyLight.exe";
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["AutoKeyLight"] = "\"" + current + "\"",
                ["MyApp"] = current,
                ["Keep"] = "C:\\Keep.exe"
            };
            IReadOnlyList<StartupEdit> edits = StartupRegistration.Disable(values);
            Assert.Equal(2, edits.Count);
            Assert.All(edits, edit => Assert.Null(edit.Value));
            Assert.DoesNotContain(edits, edit => edit.Name == "Keep");
        }

        [Fact]
        public void AlreadyQuotedCurrentPathNeedsNoStartupWrite()
        {
            string current = "C:\\Apps\\AutoKeyLight.exe";
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["AutoKeyLight"] = "\"" + current + "\""
            };
            Assert.Empty(StartupRegistration.Enable(current, values));
        }
    }
}
