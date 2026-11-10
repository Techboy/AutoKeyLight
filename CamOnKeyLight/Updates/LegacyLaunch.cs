using System.Diagnostics;
using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal static class LegacyLaunch
    {
        public static bool TryHandOff(string[] args)
        {
            string? exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe) || !InstallHandoff.IsLegacyProcess(exe))
                return false;

            try
            {
                string? sourceDir = Path.GetDirectoryName(exe);
                if (string.IsNullOrEmpty(sourceDir))
                    return false;

                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string destination = InstallHandoff.DestinationDirectory(exe, local);
                bool sameFolder = string.Equals(Path.GetFullPath(sourceDir), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase);
                if (!sameFolder)
                    CopyInstall(sourceDir, destination);

                string payload = Path.Combine(destination, SettingsLocations.AppHostPayloadName);
                string newExe = Path.Combine(destination, SettingsLocations.ExeName);
                if (File.Exists(payload))
                    File.Copy(payload, newExe, overwrite: true);
                else if (!File.Exists(newExe))
                    return false;

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = newExe,
                    WorkingDirectory = destination,
                    UseShellExecute = false
                };
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("--retired-install", StringComparison.Ordinal) || args[i].Equals("--retired-pid", StringComparison.Ordinal))
                    {
                        i++;
                        continue;
                    }

                    start.ArgumentList.Add(args[i]);
                }

                if (!sameFolder)
                {
                    start.ArgumentList.Add("--retired-install");
                    start.ArgumentList.Add(sourceDir);
                    start.ArgumentList.Add("--retired-pid");
                    start.ArgumentList.Add(Environment.ProcessId.ToString());
                }

                return Process.Start(start) != null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                return false;
            }
        }

        public static void CompleteRetirement(RetirementRequest retirement)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    try
                    {
                        using Process process = Process.GetProcessById(retirement.ProcessId);
                        process.WaitForExit(30000);
                    }
                    catch (ArgumentException)
                    {
                    }

                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    if (!InstallHandoff.IsRetiredInstallDirectory(local, retirement.Directory))
                        return;

                    string uninstaller = Path.Combine(Path.GetFullPath(retirement.Directory), "unins000.exe");
                    FileInfo uninstallerInfo = new FileInfo(uninstaller);
                    if (uninstallerInfo.Exists && !uninstallerInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        using Process? removed = Process.Start(new ProcessStartInfo
                        {
                            FileName = uninstaller,
                            Arguments = "/VERYSILENT /NORESTART",
                            UseShellExecute = false
                        });
                        removed?.WaitForExit(60000);
                    }
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                }

                try
                {
                    WriteStartMenuShortcut(Environment.ProcessPath);
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                }
            });
        }

        private static void CopyInstall(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.EnumerateFiles(source))
            {
                string name = Path.GetFileName(file);
                if (InstallHandoff.ShouldSkipInstallFile(name))
                    continue;

                string target = Path.Combine(destination, name);
                if (!PackageRules.IsInside(target, destination))
                    continue;

                File.Copy(file, target, overwrite: true);
            }
        }

        private static void WriteStartMenuShortcut(string? exe)
        {
            if (string.IsNullOrEmpty(exe))
                return;

            string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            if (string.IsNullOrEmpty(programs))
                return;

            Directory.CreateDirectory(programs);
            string link = Path.Combine(programs, SettingsLocations.DisplayName + ".lnk");
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                return;

            object shell = Activator.CreateInstance(shellType) ?? throw new InvalidOperationException("Windows Script Host is unavailable.");
            object shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { link })
                ?? throw new InvalidOperationException("The Start menu shortcut could not be created.");
            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { exe });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(exe) ?? "" });
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "Turn Elgato lights on and off with the webcam" });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
            string oldLink = Path.Combine(programs, SettingsLocations.LegacyDisplayName + ".lnk");
            if (File.Exists(oldLink) && !string.Equals(oldLink, link, StringComparison.OrdinalIgnoreCase))
                File.Delete(oldLink);
        }
    }
}
