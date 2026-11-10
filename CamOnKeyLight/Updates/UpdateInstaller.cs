using System.Diagnostics;
using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal static class UpdateInstaller
    {
        public static bool IsApplyRequest(string[] args)
        {
            return args.Length > 0 && args[0].Equals("--apply-update", StringComparison.Ordinal);
        }

        public static void Run(string[] args)
        {
            if (!ApplyArguments.TryParse(args, out ApplyRequest request, out string? error))
            {
                WriteError(error ?? "The update request is incomplete.");
                return;
            }

            string? pathError = SafeUpdatePaths.Validate(request.Source, request.Destination, UpdatesRoot(), WindowsRoot());
            if (pathError != null)
            {
                WriteError(pathError);
                return;
            }

            try
            {
                using Process running = Process.GetProcessById(request.ProcessId);
                if (!running.WaitForExit(60000))
                {
                    WriteError("The app did not exit, so the update was not applied.");
                    return;
                }
            }
            catch (ArgumentException)
            {
            }

            string backup = Path.Combine(UpdatesRoot(), "backup");
            try
            {
                DirectoryReplace.Replace(request.Source, request.Destination, backup);
                string? exe = LaunchExe(request.Destination);
                if (exe == null)
                    throw new InvalidOperationException("The update did not contain the program.");

                string? fileVersion = FileVersionInfo.GetVersionInfo(exe).FileVersion;
                if (!ReleaseVersion.FileVersionMatches(fileVersion, request.ExpectedVersion))
                {
                    if (Directory.Exists(backup))
                        FolderCopy.CopyContents(backup, request.Destination);

                    WriteError("The downloaded app version did not match the release, so the previous files were restored.");
                    return;
                }

                try
                {
                    File.WriteAllText(Path.Combine(UpdatesRoot(), "just-installed.txt"), ReleaseVersion.Format(request.ExpectedVersion));
                }
                catch (IOException)
                {
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = request.Destination,
                    UseShellExecute = false,
                    ArgumentList = { "--background" }
                });
            }
            catch (Exception ex)
            {
                WriteError("The update could not be applied. " + ex.Message);
            }
        }

        public static void StartHelper(string staging, string installDirectory, int processId, Version version)
        {
            string? pathError = SafeUpdatePaths.Validate(staging, installDirectory, UpdatesRoot(), WindowsRoot());
            if (pathError != null)
                throw new InvalidOperationException(pathError);

            string? stagedExe = LaunchExe(staging);
            if (stagedExe == null)
                throw new InvalidOperationException("The staged update is missing CamOnKeyLight.exe.");

            Process? started = Process.Start(new ProcessStartInfo
            {
                FileName = stagedExe,
                WorkingDirectory = staging,
                UseShellExecute = false,
                ArgumentList =
                {
                    "--apply-update",
                    "--pid",
                    processId.ToString(),
                    "--source",
                    staging,
                    "--dest",
                    installDirectory,
                    "--expected-version",
                    ReleaseVersion.Format(version)
                }
            });

            if (started == null)
                throw new InvalidOperationException("Windows did not start the update.");
        }

        private static string? LaunchExe(string directory)
        {
            foreach (string name in new[] { SettingsLocations.ExeName, SettingsLocations.LegacyExeName })
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        public static string UpdatesRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CamOnKeyLight", "updates");
        }

        public static string LastErrorPath()
        {
            return Path.Combine(UpdatesRoot(), "last-error.txt");
        }

        private static string WindowsRoot()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        }

        private static void WriteError(string message)
        {
            try
            {
                Directory.CreateDirectory(UpdatesRoot());
                File.WriteAllText(LastErrorPath(), message);
            }
            catch
            {
            }
        }
    }
}
