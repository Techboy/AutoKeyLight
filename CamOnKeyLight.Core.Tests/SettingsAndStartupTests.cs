using CamOnKeyLight.Core;

namespace CamOnKeyLight.Core.Tests
{
    public class SettingsAndStartupTests
    {
        [Fact]
        public void SettingsFileStaysOutsideTheInstallFolder()
        {
            string local = Path.Combine("C:", "Users", "Paul", "AppData", "Local");
            Assert.True(SettingsLocations.SettingsAreOutsideInstall(local));
            Assert.EndsWith(Path.Combine("CamOnKeyLight", "user-settings.json"), SettingsLocations.SettingsFile(local));
            Assert.EndsWith(Path.Combine("Programs", "CamOnKeyLight"), SettingsLocations.InstalledAppDirectory(local));
            Assert.NotEqual(UpdateSource.PackageName, SettingsLocations.SetupFileName);
        }

        [Fact]
        public void SetupExeIsNotAnUpdatePayload()
        {
            string? error = PackageRules.Validate(new (string, long)[]
            {
                ("CamOnKeyLight.exe", 10),
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
                    <CamOnKeyLight.Properties.Settings>
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
                    </CamOnKeyLight.Properties.Settings>
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
            string current = Path.Combine("C:", "Users", "Paul Smith", "AppData", "Local", "Programs", "CamOnKeyLight", "CamOnKeyLight.exe");
            string stale = Path.Combine("C:", "Users", "Paul Smith", "Downloads", "CamOnKeyLight", "CamOnKeyLight.exe");
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["MyApp"] = stale,
                ["SomethingElse"] = "C:\\Windows\\notepad.exe"
            };

            Assert.True(StartupRegistration.HadStartupEnabled(values));
            IReadOnlyList<StartupEdit> edits = StartupRegistration.Enable(current, values);
            Assert.Contains(edits, edit => edit.Name == "CamOnKeyLight" && edit.Value == "\"" + current + "\"");
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
            string current = "C:\\Apps\\CamOnKeyLight.exe";
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["CamOnKeyLight"] = "\"" + current + "\"",
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
            string current = "C:\\Apps\\CamOnKeyLight.exe";
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["CamOnKeyLight"] = "\"" + current + "\""
            };
            Assert.Empty(StartupRegistration.Enable(current, values));
        }

        [Fact]
        public void OldSettingsFileIsCopiedAndTheOriginalIsKept()
        {
            string root = Path.Combine(Path.GetTempPath(), "camonkeylight-settings-" + Guid.NewGuid().ToString("N"));
            string legacy = SettingsLocations.LegacySettingsFile(root);
            Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
            File.WriteAllText(legacy, "{\"lightMode\":\"On\"}");
            try
            {
                Assert.True(SettingsHandoff.CopyLegacyFile(root));
                Assert.Equal("{\"lightMode\":\"On\"}", File.ReadAllText(SettingsLocations.SettingsFile(root)));
                Assert.True(File.Exists(legacy));
                Assert.False(SettingsHandoff.CopyLegacyFile(root));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void OldUserConfigSectionStillLoads()
        {
            string xml = """
                <configuration>
                  <userSettings>
                    <AutoKeyLight.Properties.Settings>
                      <setting name="IPs" serializeAs="Xml">
                        <value><ArrayOfString><string>192.168.1.6</string></ArrayOfString></value>
                      </setting>
                      <setting name="LightMode" serializeAs="String"><value>Off</value></setting>
                    </AutoKeyLight.Properties.Settings>
                  </userSettings>
                </configuration>
                """;
            AppUserSettings? parsed = UserConfigXml.Parse(xml);
            Assert.NotNull(parsed);
            Assert.Equal("192.168.1.6", Assert.Single(parsed!.Lights).Address);
            Assert.Equal("Off", parsed.LightMode);
        }

        [Fact]
        public void OldRunKeyMovesToTheNewNameWithoutTurningStartupOn()
        {
            string oldExe = Path.Combine("C:", "Users", "Paul", "AppData", "Local", "Programs", "AutoKeyLight", "AutoKeyLight.exe");
            string newExe = Path.Combine("C:", "Users", "Paul", "AppData", "Local", "Programs", "CamOnKeyLight", "CamOnKeyLight.exe");
            Dictionary<string, string> enabled = new Dictionary<string, string>
            {
                ["AutoKeyLight"] = "\"" + oldExe + "\""
            };
            Assert.True(StartupRegistration.HadStartupEnabled(enabled));
            IReadOnlyList<StartupEdit> edits = StartupRegistration.Enable(newExe, enabled);
            Assert.Contains(edits, edit => edit.Name == "CamOnKeyLight" && edit.Value == "\"" + newExe + "\"");
            Assert.Contains(edits, edit => edit.Name == "AutoKeyLight" && edit.Value == null);

            Assert.False(StartupRegistration.HadStartupEnabled(new Dictionary<string, string>()));
            Assert.False(StartupRegistration.IsOurExecutable("C:\\Windows\\notepad.exe"));
            Assert.True(StartupRegistration.IsOurExecutable(oldExe));
        }

        [Fact]
        public void InstalledLegacyFolderMovesAndAPortableFolderStays()
        {
            string local = Path.Combine("C:", "Users", "Paul", "AppData", "Local");
            string installed = Path.Combine(local, "Programs", "AutoKeyLight", "AutoKeyLight.exe");
            string portable = Path.Combine(local, "Apps", "AutoKeyLight.exe");
            Assert.Equal(Path.Combine(local, "Programs", "CamOnKeyLight"), InstallHandoff.DestinationDirectory(installed, local));
            Assert.Equal(Path.Combine(local, "Apps"), InstallHandoff.DestinationDirectory(portable, local));
            Assert.True(InstallHandoff.IsLegacyProcess(installed));
            Assert.False(InstallHandoff.IsLegacyProcess(Path.Combine(local, "Programs", "CamOnKeyLight", "CamOnKeyLight.exe")));
            Assert.True(InstallHandoff.ShouldSkipInstallFile("unins000.exe"));
            Assert.True(InstallHandoff.TryParseRetirement(new[] { "--background", "--retired-install", portable, "--retired-pid", "42" }, out RetirementRequest request));
            Assert.Equal(42, request.ProcessId);
            Assert.Equal(portable, request.Directory);
        }

        [Fact]
        public void LegacyUpdateZipKeepsTheOldExeName()
        {
            Assert.Null(LegacyPackageRules.Validate(new (string, long)[]
            {
                ("AutoKeyLight.exe", 10),
                ("AutoKeyLight.dll", 10),
                ("createdump.exe", 10),
                ("CamOnKeyLight.dll", 10),
                ("CamOnKeyLight.deps.json", 10),
                ("CamOnKeyLight.runtimeconfig.json", 10),
                ("CamOnKeyLight.apphost.dll", 10)
            }));
            Assert.NotNull(LegacyPackageRules.Validate(new (string, long)[]
            {
                ("AutoKeyLight.exe", 10),
                ("CamOnKeyLight.exe", 10),
                ("CamOnKeyLight.dll", 10),
                ("CamOnKeyLight.deps.json", 10),
                ("CamOnKeyLight.runtimeconfig.json", 10),
                ("CamOnKeyLight.apphost.dll", 10)
            }));
        }
    }
}
