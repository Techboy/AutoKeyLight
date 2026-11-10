using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace AutoKeyLight.Core
{
    public sealed class LightRecord
    {
        public LightRecord()
        {
            Address = "";
            Description = "";
            Group = "";
            ProductName = "";
            Serial = "";
        }

        public LightRecord(string address, string description, string? group = null, string? productName = null, string? serial = null)
        {
            Address = (address ?? "").Trim();
            Description = (description ?? "").Trim();
            Group = (group ?? "").Trim();
            ProductName = (productName ?? "").Trim();
            Serial = (serial ?? "").Trim();
        }

        public string Address { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public string ProductName { get; set; }
        public string Serial { get; set; }
    }

    public sealed class AppUserSettings
    {
        public List<LightRecord> Lights { get; set; } = new List<LightRecord>();
        public decimal CameraOnDelayMs { get; set; }
        public decimal CameraOffDelayMs { get; set; } = LightTiming.DefaultOffDelayMilliseconds;
        public string Theme { get; set; } = "FollowWindows";
        public bool AutomaticUpdates { get; set; } = true;
        public string LightMode { get; set; } = "Auto";
        public string HotkeyAuto { get; set; } = "";
        public string HotkeyOn { get; set; } = "";
        public string HotkeyOff { get; set; } = "";
        public bool QuietHoursEnabled { get; set; }
        public string QuietStart { get; set; } = "22:00";
        public string QuietEnd { get; set; } = "07:00";
        public string QuietDays { get; set; } = QuietHours.AllDays;
        public string CameraAppMode { get; set; } = "All";
        public List<string> KnownCameraApps { get; set; } = new List<string>();
        public List<string> SelectedCameraApps { get; set; } = new List<string>();
        public bool AllGroupsFollowCamera { get; set; } = true;
        public List<string> CameraGroups { get; set; } = new List<string>();
        public bool NotifyLightPresence { get; set; } = true;
        public bool NotifyOnUpdate { get; set; }

        public static AppUserSettings CreateDefault()
        {
            return new AppUserSettings();
        }

        public AppUserSettings Clone()
        {
            AppUserSettings copy = new AppUserSettings
            {
                CameraOnDelayMs = CameraOnDelayMs,
                CameraOffDelayMs = CameraOffDelayMs,
                Theme = Theme,
                AutomaticUpdates = AutomaticUpdates,
                LightMode = LightMode,
                HotkeyAuto = HotkeyAuto,
                HotkeyOn = HotkeyOn,
                HotkeyOff = HotkeyOff,
                QuietHoursEnabled = QuietHoursEnabled,
                QuietStart = QuietStart,
                QuietEnd = QuietEnd,
                QuietDays = QuietDays,
                CameraAppMode = CameraAppMode,
                AllGroupsFollowCamera = AllGroupsFollowCamera,
                NotifyLightPresence = NotifyLightPresence,
                NotifyOnUpdate = NotifyOnUpdate
            };
            copy.KnownCameraApps.AddRange(KnownCameraApps);
            copy.SelectedCameraApps.AddRange(SelectedCameraApps);
            copy.CameraGroups.AddRange(CameraGroups);
            foreach (LightRecord light in Lights)
                copy.Lights.Add(new LightRecord(light.Address, light.Description, light.Group, light.ProductName, light.Serial));
            return copy;
        }
    }

    public static class SettingsLocations
    {
        public const string SettingsFileName = "user-settings.json";
        public const string DataFolderName = "AutoKeyLight";
        public const string InstallFolder = "Programs";
        public const string ExeName = "AutoKeyLight.exe";
        public const string SetupFileName = "AutoKeyLight-Setup.exe";
        public const string StartupValueName = "AutoKeyLight";
        public const string LegacyStartupValueName = "MyApp";
        public const string SingleInstanceMutex = "Techboy.AutoKeyLight";

        public static string DataDirectory(string localAppData)
        {
            return Path.Combine(localAppData, DataFolderName);
        }

        public static string SettingsFile(string localAppData)
        {
            return Path.Combine(DataDirectory(localAppData), SettingsFileName);
        }

        public static string InstalledAppDirectory(string localAppData)
        {
            return Path.Combine(localAppData, InstallFolder, DataFolderName);
        }

        public static bool SettingsAreOutsideInstall(string localAppData)
        {
            string settings = Path.GetFullPath(SettingsFile(localAppData));
            string install = Path.GetFullPath(InstalledAppDirectory(localAppData));
            return !settings.StartsWith(install + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(settings, install, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class SettingsJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public static string Serialize(AppUserSettings settings)
        {
            return JsonSerializer.Serialize(settings, Options);
        }

        public static AppUserSettings? Deserialize(string json)
        {
            AppUserSettings? settings = JsonSerializer.Deserialize<AppUserSettings>(json, Options);
            if (settings == null)
                return null;

            settings.Lights ??= new List<LightRecord>();
            for (int i = settings.Lights.Count - 1; i >= 0; i--)
            {
                if (settings.Lights[i] == null || string.IsNullOrWhiteSpace(settings.Lights[i].Address))
                    settings.Lights.RemoveAt(i);
                else
                {
                    settings.Lights[i].Address = settings.Lights[i].Address.Trim();
                    settings.Lights[i].Description = (settings.Lights[i].Description ?? "").Trim();
                    settings.Lights[i].Group = (settings.Lights[i].Group ?? "").Trim();
                    settings.Lights[i].ProductName = (settings.Lights[i].ProductName ?? "").Trim();
                    settings.Lights[i].Serial = (settings.Lights[i].Serial ?? "").Trim();
                }
            }

            settings.Theme ??= "FollowWindows";
            settings.LightMode ??= "Auto";
            settings.HotkeyAuto ??= "";
            settings.HotkeyOn ??= "";
            settings.HotkeyOff ??= "";
            settings.QuietStart = QuietHours.NormalizeTime(settings.QuietStart, "22:00");
            settings.QuietEnd = QuietHours.NormalizeTime(settings.QuietEnd, "07:00");
            settings.QuietDays = QuietHours.NormalizeDays(settings.QuietDays);
            settings.CameraAppMode = CameraAppFilterPreference.Normalize(settings.CameraAppMode);
            settings.KnownCameraApps = CameraAppNames.Clean(settings.KnownCameraApps);
            settings.SelectedCameraApps = CameraAppNames.Clean(settings.SelectedCameraApps);
            settings.CameraGroups = LightGrouping.Clean(settings.CameraGroups);
            return settings;
        }
    }

    public static class UserConfigXml
    {
        public static AppUserSettings? Parse(string xml)
        {
            XDocument document;
            try
            {
                document = XDocument.Parse(xml);
            }
            catch (System.Xml.XmlException)
            {
                return null;
            }

            XElement? section = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "AutoKeyLight.Properties.Settings");
            if (section == null)
                return null;

            AppUserSettings settings = AppUserSettings.CreateDefault();
            bool sawSetting = false;
            foreach (XElement setting in section.Elements().Where(element => element.Name.LocalName == "setting"))
            {
                string? name = (string?)setting.Attribute("name");
                if (string.IsNullOrEmpty(name))
                    continue;

                sawSetting = true;
                XElement? value = setting.Elements().FirstOrDefault(element => element.Name.LocalName == "value");
                if (name == "IPs")
                {
                    settings.Lights = ReadAddresses(value).Select(address => new LightRecord(address, "")).ToList();
                    continue;
                }

                string text = value?.Value ?? "";
                switch (name)
                {
                    case "CameraDelay":
                        if (decimal.TryParse(text, out decimal onDelay))
                            settings.CameraOnDelayMs = onDelay;
                        break;
                    case "CameraOffDelay":
                        if (decimal.TryParse(text, out decimal offDelay))
                            settings.CameraOffDelayMs = offDelay;
                        break;
                    case "Theme":
                        settings.Theme = text;
                        break;
                    case "AutomaticUpdates":
                        if (bool.TryParse(text, out bool automatic))
                            settings.AutomaticUpdates = automatic;
                        break;
                    case "LightMode":
                        settings.LightMode = text;
                        break;
                    case "HotkeyAuto":
                        settings.HotkeyAuto = text;
                        break;
                    case "HotkeyOn":
                        settings.HotkeyOn = text;
                        break;
                    case "HotkeyOff":
                        settings.HotkeyOff = text;
                        break;
                }
            }

            return sawSetting ? settings : null;
        }

        private static IEnumerable<string> ReadAddresses(XElement? value)
        {
            if (value == null)
                yield break;

            foreach (XElement address in value.Descendants().Where(element => element.Name.LocalName == "string"))
            {
                if (!string.IsNullOrWhiteSpace(address.Value))
                    yield return address.Value.Trim();
            }
        }
    }

    public static class SettingsMigration
    {
        public readonly record struct Candidate(DateTime WrittenUtc, AppUserSettings Settings);

        public static AppUserSettings Choose(AppUserSettings? savedFile, IReadOnlyList<Candidate> olderConfigs)
        {
            if (savedFile != null)
                return savedFile.Clone();

            Candidate? withLights = null;
            foreach (Candidate candidate in olderConfigs.OrderByDescending(candidate => candidate.WrittenUtc))
            {
                if (candidate.Settings.Lights.Count > 0)
                {
                    withLights = candidate;
                    break;
                }
            }

            if (withLights != null)
                return withLights.Value.Settings.Clone();

            Candidate? newest = null;
            foreach (Candidate candidate in olderConfigs.OrderByDescending(candidate => candidate.WrittenUtc))
            {
                newest = candidate;
                break;
            }

            return newest?.Settings.Clone() ?? AppUserSettings.CreateDefault();
        }
    }

    public readonly record struct StartupEdit(string Name, string? Value);

    public static class StartupRegistration
    {
        public static string Quote(string exePath)
        {
            return "\"" + exePath.Trim().Trim('"') + "\"";
        }

        public static string? TargetPath(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string text = raw.Trim();
            if (text.StartsWith('"'))
            {
                int end = text.IndexOf('"', 1);
                if (end > 1)
                    return text[1..end];
            }

            int exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exe >= 0)
                return text[..(exe + 4)].Trim().Trim('"');

            return text;
        }

        public static bool IsOurExecutable(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            int slash = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
            string file = slash >= 0 ? path[(slash + 1)..] : path;
            return file.Equals(SettingsLocations.ExeName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HadStartupEnabled(IReadOnlyDictionary<string, string> values)
        {
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (IsOwnedName(pair.Key) && IsOurExecutable(TargetPath(pair.Value)))
                    return true;
            }

            return false;
        }

        public static IReadOnlyList<StartupEdit> Enable(string currentExe, IReadOnlyDictionary<string, string> values)
        {
            List<StartupEdit> edits = new List<StartupEdit>();
            string desired = Quote(currentExe);
            if (!values.TryGetValue(SettingsLocations.StartupValueName, out string? current)
                || !string.Equals(current.Trim(), desired, StringComparison.OrdinalIgnoreCase))
            {
                edits.Add(new StartupEdit(SettingsLocations.StartupValueName, desired));
            }

            if (values.TryGetValue(SettingsLocations.LegacyStartupValueName, out string? legacy) && IsOurExecutable(TargetPath(legacy)))
                edits.Add(new StartupEdit(SettingsLocations.LegacyStartupValueName, null));

            return edits;
        }

        public static IReadOnlyList<StartupEdit> Disable(IReadOnlyDictionary<string, string> values)
        {
            List<StartupEdit> edits = new List<StartupEdit>();
            foreach (string name in new[] { SettingsLocations.StartupValueName, SettingsLocations.LegacyStartupValueName })
            {
                if (values.TryGetValue(name, out string? raw) && IsOurExecutable(TargetPath(raw)))
                    edits.Add(new StartupEdit(name, null));
            }

            return edits;
        }

        private static bool IsOwnedName(string name)
        {
            return name.Equals(SettingsLocations.StartupValueName, StringComparison.OrdinalIgnoreCase)
                || name.Equals(SettingsLocations.LegacyStartupValueName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
