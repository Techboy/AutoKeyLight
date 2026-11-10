using AutoKeyLight.Core;
using Microsoft.Win32;

namespace AutoKeyLight
{
    internal static class WebcamConsent
    {
        private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";

        public static IReadOnlyList<string> Read(bool inUseOnly)
        {
            List<string> names = new List<string>();
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath);
                if (key == null)
                    return names;

                Collect(key, inUseOnly, names);
                using RegistryKey? nonPackaged = key.OpenSubKey("NonPackaged");
                if (nonPackaged != null)
                    Collect(nonPackaged, inUseOnly, names);
            }
            catch (System.Security.SecurityException)
            {
            }
            catch (IOException)
            {
            }

            return CameraAppNames.Clean(names);
        }

        private static void Collect(RegistryKey parent, bool inUseOnly, List<string> names)
        {
            foreach (string subName in parent.GetSubKeyNames())
            {
                if (subName.Equals("NonPackaged", StringComparison.OrdinalIgnoreCase))
                    continue;

                using RegistryKey? child = parent.OpenSubKey(subName);
                if (child == null)
                    continue;

                bool inUse = child.GetValue("LastUsedTimeStop") is long timestamp && timestamp == 0;
                if (inUseOnly && !inUse)
                    continue;

                string friendly = CameraAppNames.Friendly(subName);
                if (friendly.Length > 0)
                    names.Add(friendly);
            }
        }
    }
}
