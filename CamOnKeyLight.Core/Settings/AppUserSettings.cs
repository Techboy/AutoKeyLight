using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace CamOnKeyLight.Core
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
        public const string DisplayName = "Cam On Key Light";
        public const string LegacyDisplayName = "Auto Key Light";
        public const string SettingsFileName = "user-settings.json";
        public const string DataFolderName = "CamOnKeyLight";
        public const string LegacyDataFolderName = "AutoKeyLight";
        public const string InstallFolder = "Programs";
        public const string ExeName = "CamOnKeyLight.exe";
        public const string LegacyExeName = "AutoKeyLight.exe";
        public const string AppHostPayloadName = "CamOnKeyLight.apphost.dll";
        public const string SetupFileName = "CamOnKeyLight-Setup.exe";
        public const string StartupValueName = "CamOnKeyLight";
        public const string PreviousStartupValueName = "AutoKeyLight";
        public const string LegacyStartupValueName = "MyApp";
        public const string SingleInstanceMutex = "Techboy.CamOnKeyLight";
        public const string LegacySettingsSection = "AutoKeyLight.Properties.Settings";
        public const string SettingsSection = "CamOnKeyLight.Properties.Settings";

        public static string DataDirectory(string localAppData)
        {
            return Path.Combine(localAppData, DataFolderName);
        }

        public static string LegacyDataDirectory(string localAppData)
        {
            return Path.Combine(localAppData, LegacyDataFolderName);
        }

        public static string SettingsFile(string localAppData)
        {
            return Path.Combine(DataDirectory(localAppData), SettingsFileName);
        }

        public static string LegacySettingsFile(string localAppData)
        {
            return Path.Combine(LegacyDataDirectory(localAppData), SettingsFileName);
        }

        public static string InstalledAppDirectory(string localAppData)
        {
            return Path.Combine(localAppData, InstallFolder, DataFolderName);
        }

        public static string LegacyInstalledAppDirectory(string localAppData)
        {
            return Path.Combine(localAppData, InstallFolder, LegacyDataFolderName);
        }

        public static string FileNameOf(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            int slash = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
            return slash >= 0 ? path[(slash + 1)..] : path;
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

            XElement? section = document.Descendants().FirstOrDefault(element =>
                element.Name.LocalName == SettingsLocations.SettingsSection
                || element.Name.LocalName == SettingsLocations.LegacySettingsSection);
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
            string file = SettingsLocations.FileNameOf(path);
            return file.Equals(SettingsLocations.ExeName, StringComparison.OrdinalIgnoreCase)
                || file.Equals(SettingsLocations.LegacyExeName, StringComparison.OrdinalIgnoreCase);
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

            RemoveOwnedLegacy(values, edits);
            return edits;
        }

        public static IReadOnlyList<StartupEdit> Disable(IReadOnlyDictionary<string, string> values)
        {
            List<StartupEdit> edits = new List<StartupEdit>();
            foreach (string name in OwnedNames())
            {
                if (values.TryGetValue(name, out string? raw) && IsOurExecutable(TargetPath(raw)))
                    edits.Add(new StartupEdit(name, null));
            }

            return edits;
        }

        private static void RemoveOwnedLegacy(IReadOnlyDictionary<string, string> values, List<StartupEdit> edits)
        {
            foreach (string name in new[] { SettingsLocations.PreviousStartupValueName, SettingsLocations.LegacyStartupValueName })
            {
                if (values.TryGetValue(name, out string? raw) && IsOurExecutable(TargetPath(raw)))
                    edits.Add(new StartupEdit(name, null));
            }
        }

        private static IEnumerable<string> OwnedNames()
        {
            yield return SettingsLocations.StartupValueName;
            yield return SettingsLocations.PreviousStartupValueName;
            yield return SettingsLocations.LegacyStartupValueName;
        }

        private static bool IsOwnedName(string name)
        {
            foreach (string owned in OwnedNames())
            {
                if (name.Equals(owned, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    public static class SettingsHandoff
    {
        public static bool CopyLegacyFile(string localAppData)
        {
            string current = SettingsLocations.SettingsFile(localAppData);
            string legacy = SettingsLocations.LegacySettingsFile(localAppData);
            if (File.Exists(current) || !File.Exists(legacy))
                return false;

            Directory.CreateDirectory(SettingsLocations.DataDirectory(localAppData));
            File.Copy(legacy, current, overwrite: false);
            return File.Exists(current) && File.Exists(legacy);
        }
    }

    public readonly record struct RetirementRequest(int ProcessId, string Directory);

    public static class InstallHandoff
    {
        public static bool IsLegacyProcess(string? exePath)
        {
            return SettingsLocations.FileNameOf(exePath).Equals(SettingsLocations.LegacyExeName, StringComparison.OrdinalIgnoreCase);
        }

        public static string DestinationDirectory(string currentExe, string localAppData)
        {
            string? directory = Path.GetDirectoryName(currentExe);
            if (string.IsNullOrEmpty(directory))
                return SettingsLocations.InstalledAppDirectory(localAppData);

            string current = Path.GetFullPath(directory);
            string legacyInstall = Path.GetFullPath(SettingsLocations.LegacyInstalledAppDirectory(localAppData));
            if (string.Equals(current, legacyInstall, StringComparison.OrdinalIgnoreCase))
                return SettingsLocations.InstalledAppDirectory(localAppData);

            return directory;
        }

        public static bool ShouldSkipInstallFile(string fileName)
        {
            return fileName.StartsWith("unins000", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals(SettingsLocations.LegacyExeName, StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("AutoKeyLight.dll", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("AutoKeyLight.deps.json", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("AutoKeyLight.runtimeconfig.json", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRetiredInstallDirectory(string localAppData, string? directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return false;

            string expected = Path.GetFullPath(SettingsLocations.LegacyInstalledAppDirectory(localAppData));
            string candidate;
            try
            {
                candidate = Path.GetFullPath(directory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }

            if (!string.Equals(expected, candidate, StringComparison.OrdinalIgnoreCase))
                return false;

            DirectoryInfo? current = new DirectoryInfo(candidate);
            while (current != null)
            {
                if (current.Exists && current.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    return false;

                current = current.Parent;
            }

            return true;
        }

        public static bool TryParseRetirement(string[] args, out RetirementRequest request)
        {
            request = new RetirementRequest(0, "");
            int pid = 0;
            string? directory = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--retired-pid", StringComparison.Ordinal) && i + 1 < args.Length)
                    int.TryParse(args[++i], out pid);
                else if (args[i].Equals("--retired-install", StringComparison.Ordinal) && i + 1 < args.Length)
                    directory = args[++i];
            }

            if (pid <= 0 || string.IsNullOrWhiteSpace(directory))
                return false;

            request = new RetirementRequest(pid, directory);
            return true;
        }
    }

    /// <summary>
    /// v0.6.0 accepts a zip only when AutoKeyLight.exe is present and no other
    /// program exe is. The legacy package therefore keeps that file name and
    /// carries the CamOnKeyLight host under a .dll name the old rules allow.
    /// </summary>
    public static class LegacyPackageRules
    {
        public static string? Validate(IEnumerable<(string Path, long Length)> entries)
        {
            bool foundLegacyExe = false;
            bool foundHost = false;
            bool foundDll = false;
            bool foundDeps = false;
            bool foundRuntime = false;
            foreach ((string path, long length) in entries)
            {
                if (length < 0)
                    return "The legacy package has an invalid file size.";

                string name = path.Replace('\\', '/');
                if (name.EndsWith("/", StringComparison.Ordinal))
                    continue;

                int slash = name.LastIndexOf('/');
                string file = slash >= 0 ? name[(slash + 1)..] : name;
                if (file.Equals(SettingsLocations.ExeName, StringComparison.OrdinalIgnoreCase))
                    return "The legacy package must not contain CamOnKeyLight.exe. v0.6.0 would reject it.";

                if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    && !file.Equals(SettingsLocations.LegacyExeName, StringComparison.OrdinalIgnoreCase)
                    && !file.Equals("createdump.exe", StringComparison.OrdinalIgnoreCase))
                    return "The legacy package contains an exe v0.6.0 would reject: " + file;

                if (file.Equals(SettingsLocations.LegacyExeName, StringComparison.OrdinalIgnoreCase))
                    foundLegacyExe = true;
                else if (file.Equals(SettingsLocations.AppHostPayloadName, StringComparison.OrdinalIgnoreCase))
                    foundHost = true;
                else if (file.Equals("CamOnKeyLight.dll", StringComparison.OrdinalIgnoreCase))
                    foundDll = true;
                else if (file.Equals("CamOnKeyLight.deps.json", StringComparison.OrdinalIgnoreCase))
                    foundDeps = true;
                else if (file.Equals("CamOnKeyLight.runtimeconfig.json", StringComparison.OrdinalIgnoreCase))
                    foundRuntime = true;
            }

            if (!foundLegacyExe)
                return "The legacy package does not contain AutoKeyLight.exe.";

            if (!foundHost || !foundDll || !foundDeps || !foundRuntime)
                return "The legacy package is missing the CamOnKeyLight host files.";

            return null;
        }
    }
}
