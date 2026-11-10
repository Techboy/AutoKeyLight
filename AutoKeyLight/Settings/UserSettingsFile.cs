using AutoKeyLight.Core;

namespace AutoKeyLight
{
    internal static class UserSettingsFile
    {
        public static AppUserSettings Load()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string path = SettingsLocations.SettingsFile(local);
            if (File.Exists(path))
            {
                try
                {
                    AppUserSettings? saved = SettingsJson.Deserialize(File.ReadAllText(path));
                    if (saved != null)
                        return saved;
                }
                catch (System.Text.Json.JsonException)
                {
                }
                catch (IOException)
                {
                }
            }

            List<SettingsMigration.Candidate> candidates = new List<SettingsMigration.Candidate>();
            try
            {
                candidates.Add(new SettingsMigration.Candidate(DateTime.UtcNow, FromProperties()));
            }
            catch (System.Configuration.ConfigurationErrorsException)
            {
            }

            string data = SettingsLocations.DataDirectory(local);
            if (Directory.Exists(data))
            {
                foreach (string file in Directory.EnumerateFiles(data, "user.config", SearchOption.AllDirectories))
                {
                    try
                    {
                        AppUserSettings? parsed = UserConfigXml.Parse(File.ReadAllText(file));
                        if (parsed != null)
                            candidates.Add(new SettingsMigration.Candidate(File.GetLastWriteTimeUtc(file), parsed));
                    }
                    catch (IOException)
                    {
                    }
                    catch (System.Xml.XmlException)
                    {
                    }
                }
            }

            AppUserSettings chosen = SettingsMigration.Choose(null, candidates);
            Save(chosen);
            return chosen;
        }

        public static void Save(AppUserSettings settings)
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directory = SettingsLocations.DataDirectory(local);
            Directory.CreateDirectory(directory);
            string path = SettingsLocations.SettingsFile(local);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, SettingsJson.Serialize(settings));
            File.Move(temporary, path, overwrite: true);
        }

        private static AppUserSettings FromProperties()
        {
            AppUserSettings settings = AppUserSettings.CreateDefault();
            settings.CameraOnDelayMs = Properties.Settings.Default.CameraDelay;
            settings.CameraOffDelayMs = Properties.Settings.Default.CameraOffDelay;
            settings.Theme = Properties.Settings.Default.Theme ?? "FollowWindows";
            settings.AutomaticUpdates = Properties.Settings.Default.AutomaticUpdates;
            settings.LightMode = Properties.Settings.Default.LightMode ?? "Auto";
            settings.HotkeyAuto = Properties.Settings.Default.HotkeyAuto ?? "";
            settings.HotkeyOn = Properties.Settings.Default.HotkeyOn ?? "";
            settings.HotkeyOff = Properties.Settings.Default.HotkeyOff ?? "";
            if (Properties.Settings.Default.IPs != null)
            {
                foreach (string? address in Properties.Settings.Default.IPs)
                {
                    if (!string.IsNullOrWhiteSpace(address))
                        settings.Lights.Add(new LightRecord(address, ""));
                }
            }

            return settings;
        }
    }
}
