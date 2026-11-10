using CamOnKeyLight.Core;
using Microsoft.Win32;

namespace CamOnKeyLight
{
    internal static class StartupRegistry
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool ApplyForCurrentProcess(bool enable)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
                return false;

            Dictionary<string, string> values = Read(key);
            string exe = Application.ExecutablePath;
            IReadOnlyList<StartupEdit> edits = enable
                ? StartupRegistration.Enable(exe, values)
                : StartupRegistration.Disable(values);
            Apply(key, edits);
            return enable
                ? StartupRegistration.HadStartupEnabled(Read(key))
                : false;
        }

        public static bool RepairCurrentProcess()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
                return false;

            Dictionary<string, string> values = Read(key);
            if (!StartupRegistration.HadStartupEnabled(values))
                return false;

            Apply(key, StartupRegistration.Enable(Application.ExecutablePath, values));
            return true;
        }

        private static Dictionary<string, string> Read(RegistryKey key)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in new[] { SettingsLocations.StartupValueName, SettingsLocations.PreviousStartupValueName, SettingsLocations.LegacyStartupValueName })
            {
                if (key.GetValue(name) is string text)
                    values[name] = text;
            }

            return values;
        }

        private static void Apply(RegistryKey key, IReadOnlyList<StartupEdit> edits)
        {
            foreach (StartupEdit edit in edits)
            {
                if (edit.Value == null)
                    key.DeleteValue(edit.Name, throwOnMissingValue: false);
                else
                    key.SetValue(edit.Name, edit.Value);
            }
        }
    }
}
