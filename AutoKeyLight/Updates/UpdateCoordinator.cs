using System.Net;
using System.Text;
using AutoKeyLight.Core;

namespace AutoKeyLight
{
    internal readonly record struct UpdateUi(string Status, bool CheckEnabled, bool InstallEnabled, bool Problem);

    internal sealed class UpdateCoordinator : IDisposable
    {
        private readonly HttpClient http = UpdateClientFactory.Create();
        private readonly Version current;
        private readonly string installDirectory;
        private readonly string updatesRoot;
        private readonly Func<bool> cameraIsOn;
        private readonly Action<UpdateUi> onUi;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private string? staging;
        private Version? stagedVersion;
        private bool userRequested;
        private bool automatic;
        private DateTime? cameraOffSince;
        private bool installing;

        public UpdateCoordinator(Version current, string installDirectory, Func<bool> cameraIsOn, Action<UpdateUi> onUi)
        {
            this.current = current;
            this.installDirectory = installDirectory;
            this.cameraIsOn = cameraIsOn;
            this.onUi = onUi;
            updatesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoKeyLight", "updates");
            Publish("Version " + ReleaseVersion.Format(current), true, false, false);
        }

        public event Action? InstallLaunched;

        public void CheckNow()
        {
            _ = CheckAsync();
        }

        public void RequestInstall()
        {
            userRequested = true;
            MaybeInstall();
        }

        public void NotifyCamera(bool cameraOn, bool automaticEnabled)
        {
            automatic = automaticEnabled;
            if (cameraOn)
                cameraOffSince = null;
            else
                cameraOffSince ??= DateTime.UtcNow;

            MaybeInstall();
        }

        public void Dispose()
        {
            lifetime.Cancel();
            http.Dispose();
        }

        private async Task CheckAsync()
        {
            if (!await gate.WaitAsync(0))
                return;

            try
            {
                Publish("Checking for updates…", false, false, false);
                using HttpResponseMessage response = await http.GetAsync(UpdateSource.LatestReleaseUri, lifetime.Token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Publish(UpdateMessages.NoReleases, true, false, false);
                    return;
                }

                if (response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
                {
                    Publish(UpdateMessages.RateLimited, true, false, true);
                    return;
                }

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync(lifetime.Token);
                UpdateCheck check = ReleaseParser.Parse(json, current);
                if (check is UpdateCheck.UpToDate)
                {
                    ClearStaging();
                    Publish(UpdateMessages.UpToDate(current), true, false, false);
                    return;
                }

                if (check is not UpdateCheck.Available available || available.Package.ChecksumUri == null)
                {
                    string reason = check is UpdateCheck.Unavailable unavailable ? unavailable.Reason : UpdateMessages.Rejected;
                    Publish(reason, true, false, true);
                    return;
                }

                Publish("Downloading update " + ReleaseVersion.Format(available.Package.Version) + "…", false, false, false);
                byte[] package = await LimitedDownloader.ReadAsync(http, available.Package.PackageUri, UpdateSource.MaxPackageBytes, lifetime.Token);
                byte[] checksumBytes = await LimitedDownloader.ReadAsync(http, available.Package.ChecksumUri, UpdateSource.MaxChecksumBytes, lifetime.Token);
                string digestField = available.Package.GitHubSha256 == null ? "" : "sha256:" + available.Package.GitHubSha256;
                string? problem = Sha256Text.Verify(package, string.IsNullOrEmpty(digestField) ? null : digestField, Encoding.UTF8.GetString(checksumBytes), available.Package.PackageSize);
                if (problem != null)
                {
                    Publish(UpdateMessages.Rejected + " " + problem, true, false, true);
                    return;
                }

                string stagingDir = Path.Combine(updatesRoot, "staging", ReleaseVersion.Format(available.Package.Version));
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);

                Directory.CreateDirectory(stagingDir);
                using MemoryStream stream = new MemoryStream(package, writable: false);
                PackageRules.Extract(stream, stagingDir);
                staging = stagingDir;
                stagedVersion = available.Package.Version;
                userRequested = false;
                Publish(UpdateMessages.Ready(available.Package.Version), true, true, false);
                MaybeInstall();
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                Publish(UpdateMessages.Offline, true, staging != null, true);
            }
            catch (Exception)
            {
                Publish(UpdateMessages.Rejected, true, staging != null, true);
            }
            finally
            {
                try
                {
                    gate.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private void MaybeInstall()
        {
            if (installing || lifetime.IsCancellationRequested)
                return;

            string? staged = staging;
            Version? version = stagedVersion;
            if (staged == null || version == null)
                return;

            bool cameraOn = cameraOffSince == null || cameraIsOn();
            TimeSpan offFor = cameraOffSince == null ? TimeSpan.Zero : DateTime.UtcNow - cameraOffSince.Value;
            if (!UpdateDecision.CanInstall(true, cameraOn, automatic, userRequested, offFor, UpdateSource.AutomaticInstallQuietPeriod))
            {
                if (cameraOn)
                    Publish(UpdateMessages.CameraBusy, true, true, false);
                else if (!automatic && !userRequested)
                    Publish(UpdateMessages.Ready(version) + " Automatic install is off.", true, true, false);
                return;
            }

            if (cameraIsOn())
            {
                cameraOffSince = null;
                Publish(UpdateMessages.CameraBusy, true, true, false);
                return;
            }

            installing = true;
            try
            {
                UpdateInstaller.StartHelper(staged, installDirectory, Environment.ProcessId, version);
            }
            catch (Exception ex)
            {
                installing = false;
                userRequested = false;
                Publish("Couldn't start the update. " + ex.Message, true, true, true);
                return;
            }

            Publish("Installing update " + ReleaseVersion.Format(version) + "…", false, false, false);
            InstallLaunched?.Invoke();
        }

        private void ClearStaging()
        {
            staging = null;
            stagedVersion = null;
            userRequested = false;
        }

        private void Publish(string status, bool checkEnabled, bool installEnabled, bool problem)
        {
            onUi(new UpdateUi(status, checkEnabled, installEnabled, problem));
        }
    }
}
