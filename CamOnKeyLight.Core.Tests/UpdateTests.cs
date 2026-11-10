using System.IO.Compression;
using System.Net;
using CamOnKeyLight.Core;

namespace CamOnKeyLight.Core.Tests
{
    public class UpdateTests
    {
        private static readonly Version Current = new Version(1, 0, 0);

        [Fact]
        public void UpdateSourceIsThisForkOnly()
        {
            Assert.Equal("Techboy", UpdateSource.Owner);
            Assert.Equal("CamOnKeyLight", UpdateSource.Repository);
            Assert.Equal("https://api.github.com/repos/Techboy/CamOnKeyLight/releases/latest", UpdateSource.LatestReleaseUri.AbsoluteUri);
            Assert.DoesNotContain("astutejoe", UpdateSource.LatestReleaseUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("astutejoe", UpdateMessages.Offline, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StableReleaseIsOfferedWhenNewer()
        {
            UpdateCheck result = ReleaseParser.Parse(Release("v1.2.3", prerelease: false, "https://github.com/Techboy/CamOnKeyLight/releases/download/v1.2.3/"), Current);
            UpdateCheck.Available available = Assert.IsType<UpdateCheck.Available>(result);
            Assert.Equal(new Version(1, 2, 3), available.Package.Version);
            Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", available.Package.GitHubSha256);
        }

        [Fact]
        public void AReleaseWithoutADigestIsRefused()
        {
            string json = Release("v1.2.3", prerelease: false, "https://github.com/Techboy/CamOnKeyLight/releases/download/v1.2.3/")
                .Replace("\"digest\": \"sha256:" + new string('a', 64) + "\"", "\"digest\": \"\"", StringComparison.Ordinal);
            UpdateCheck result = ReleaseParser.Parse(json, Current);
            UpdateCheck.Unavailable unavailable = Assert.IsType<UpdateCheck.Unavailable>(result);
            Assert.Contains("digest", unavailable.Reason, StringComparison.OrdinalIgnoreCase);
            byte[] data = "package"u8.ToArray();
            Assert.NotNull(Sha256Text.Verify(data, null, new string('a', 64) + "  " + UpdateSource.PackageName, data.Length));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void DraftsAndPrereleasesAreIgnored(bool draft, bool prerelease)
        {
            string json = Release("v2.0.0", prerelease, "https://github.com/Techboy/CamOnKeyLight/releases/download/v2.0.0/", draft);
            UpdateCheck result = ReleaseParser.Parse(json, Current);
            Assert.IsType<UpdateCheck.Unavailable>(result);
        }

        [Fact]
        public void OlderAndEqualReleasesDoNotDowngrade()
        {
            Assert.IsType<UpdateCheck.UpToDate>(ReleaseParser.Parse(Release("v1.0.0", false, "https://github.com/Techboy/CamOnKeyLight/releases/download/v1.0.0/"), new Version(1, 0, 0, 0)));
            Assert.IsType<UpdateCheck.UpToDate>(ReleaseParser.Parse(Release("v0.9.0", false, "https://github.com/Techboy/CamOnKeyLight/releases/download/v0.9.0/"), Current));
        }

        [Fact]
        public void UpstreamAndInsecureUrlsAreRejected()
        {
            string upstream = Release("v2.0.0", false, "https://github.com/astutejoe/AutoKeyLight/releases/download/v2.0.0/");
            Assert.IsType<UpdateCheck.Unavailable>(ReleaseParser.Parse(upstream, Current));
            Assert.False(UrlPolicy.IsReleaseAsset(new Uri("http://github.com/Techboy/CamOnKeyLight/releases/download/v1.2.3/CamOnKeyLight-win-x64.zip"), "v1.2.3", UpdateSource.PackageName));
            Assert.False(UrlPolicy.IsReleaseAsset(new Uri("https://user:token@github.com/Techboy/CamOnKeyLight/releases/download/v1.2.3/CamOnKeyLight-win-x64.zip"), "v1.2.3", UpdateSource.PackageName));
            Assert.False(UrlPolicy.IsAllowedTransport(new Uri("https://example.com/Techboy/CamOnKeyLight/releases/download/v1.2.3/CamOnKeyLight-win-x64.zip")));
        }

        [Fact]
        public void ContentDeliveryRedirectsMayCarryAQuery()
        {
            Assert.True(UrlPolicy.IsAllowedTransport(new Uri("https://release-assets.githubusercontent.com/asset?name=CamOnKeyLight-win-x64.zip")));
            Assert.False(UrlPolicy.IsAllowedTransport(new Uri("https://github.com/Techboy/CamOnKeyLight/releases/download/v1.2.3/CamOnKeyLight-win-x64.zip?token=1")));
        }

        [Fact]
        public void ChecksumMustMatchTheBytesAndGitHubDigest()
        {
            byte[] data = "package"u8.ToArray();
            string hash = Sha256Text.Hex(data);
            string file = hash + "  " + UpdateSource.PackageName + "\n";
            Assert.Null(Sha256Text.Verify(data, "sha256:" + hash, file, data.Length));
            Assert.NotNull(Sha256Text.Verify(data, "sha256:" + new string('b', 64), file, data.Length));
            Assert.NotNull(Sha256Text.Verify(data, "sha256:" + hash, "deadbeef  " + UpdateSource.PackageName, data.Length));
            Assert.NotNull(Sha256Text.Verify(data, "sha256:" + hash, file, data.Length + 1));
        }

        [Fact]
        public void ZipSlipAndScriptsAreRejected()
        {
            Assert.NotNull(PackageRules.Validate(new[] { ("../CamOnKeyLight.exe", 1L) }));
            Assert.NotNull(PackageRules.Validate(new[] { ("CamOnKeyLight.exe", 1L), ("install.ps1", 1L) }));
            Assert.NotNull(PackageRules.Validate(new[] { ("CamOnKeyLight.exe", 1L), ("other.exe", 1L) }));
            Assert.Null(PackageRules.Validate(new (string, long)[]
            {
                ("CamOnKeyLight.exe", 10),
                ("CamOnKeyLight.dll", 10),
                ("CamOnKeyLight.deps.json", 10),
                ("CamOnKeyLight.runtimeconfig.json", 10),
                ("CamOnKeyLight.dll.config", 10),
                ("createdump.exe", 10),
                ("LICENSE", 10),
                ("README.md", 10)
            }));
        }

        [Fact]
        public void ExtractRejectsPathsOutsideTheDestination()
        {
            using MemoryStream zip = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = archive.CreateEntry("../escape.txt");
                using Stream writer = entry.Open();
                writer.Write("nope"u8);
            }

            zip.Position = 0;
            string destination = Path.Combine(Path.GetTempPath(), "autokeylight-zip-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(destination);
            try
            {
                Assert.Throws<InvalidDataException>(() => PackageRules.Extract(zip, destination));
                Assert.False(File.Exists(Path.Combine(Directory.GetParent(destination)!.FullName, "escape.txt")));
            }
            finally
            {
                Directory.Delete(destination, recursive: true);
            }
        }

        [Fact]
        public void RedirectToAnotherHostIsRejected()
        {
            RecordingHandler inner = new RecordingHandler(_ => Response(HttpStatusCode.Redirect, "https://evil.example/payload"));
            RedirectGuardHandler guard = new RedirectGuardHandler(inner);
            using HttpClient client = new HttpClient(guard);
            Assert.Throws<InvalidOperationException>(() => client.GetAsync(UpdateSource.LatestReleaseUri).GetAwaiter().GetResult());
            Assert.DoesNotContain(inner.Urls, url => url.Host.Equals("evil.example", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void UpdateClientHasNoCredentials()
        {
            using HttpClient client = UpdateClientFactory.Create();
            Assert.DoesNotContain(client.DefaultRequestHeaders, header => header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(client.DefaultRequestHeaders.UserAgent, product => product.Product != null && product.Product.Name == "CamOnKeyLight");
        }

        [Fact]
        public void OversizedDownloadIsRejected()
        {
            RecordingHandler inner = new RecordingHandler(_ =>
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(new byte[20]);
                return response;
            });
            using HttpClient client = new HttpClient(inner);
            Assert.Throws<InvalidDataException>(() => LimitedDownloader.ReadAsync(client, UpdateSource.LatestReleaseUri, 10, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Fact]
        public void InstallWaitsUntilTheCameraIsQuietUnlessTheUserAsked()
        {
            TimeSpan quiet = UpdateSource.AutomaticInstallQuietPeriod;
            Assert.False(UpdateDecision.CanInstall(true, cameraStreaming: true, automaticEnabled: true, userRequested: true, TimeSpan.Zero, quiet));
            Assert.False(UpdateDecision.CanInstall(true, false, true, false, TimeSpan.FromSeconds(5), quiet));
            Assert.True(UpdateDecision.CanInstall(true, false, true, false, quiet, quiet));
            Assert.True(UpdateDecision.CanInstall(true, false, false, true, TimeSpan.Zero, quiet));
            Assert.False(UpdateDecision.CanInstall(true, false, false, false, quiet, quiet));
        }

        [Fact]
        public void ApplyArgumentsRejectIncompleteRequests()
        {
            Assert.False(ApplyArguments.TryParse(new[] { "--apply-update" }, out _, out string? error));
            Assert.False(string.IsNullOrEmpty(error));
            Assert.True(ApplyArguments.TryParse(
                new[] { "--apply-update", "--pid", "15", "--source", "C:\\stage", "--dest", "C:\\app", "--expected-version", "1.2.3" },
                out ApplyRequest request,
                out _));
            Assert.Equal(15, request.ProcessId);
            Assert.Equal(new Version(1, 2, 3), request.ExpectedVersion);
        }

        [Fact]
        public void ReplaceRestoresThePreviousFilesWhenCopyFails()
        {
            string root = Path.Combine(Path.GetTempPath(), "autokeylight-replace-" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, "source");
            string destination = Path.Combine(root, "dest");
            string backup = Path.Combine(root, "backup");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(destination, "marker.txt"), "old");
            File.WriteAllText(Path.Combine(source, "marker.txt"), "new");
            File.CreateSymbolicLink(Path.Combine(source, "link.txt"), Path.Combine(root, "outside.txt"));
            try
            {
                Assert.Throws<InvalidOperationException>(() => DirectoryReplace.Replace(source, destination, backup));
                Assert.Equal("old", File.ReadAllText(Path.Combine(destination, "marker.txt")));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void SafePathsRejectWindowsAndTheUpdateFolder()
        {
            string root = Path.Combine(Path.GetTempPath(), "autokeylight-paths-" + Guid.NewGuid().ToString("N"));
            string updates = Path.Combine(root, "updates");
            string staging = Path.Combine(updates, "staging", "1.2.3");
            string windows = Path.Combine(root, "windows");
            Directory.CreateDirectory(staging);
            Directory.CreateDirectory(windows);
            string install = Path.Combine(root, "install");
            Directory.CreateDirectory(install);
            try
            {
                Assert.Null(SafeUpdatePaths.Validate(staging, install, updates, windows));
                Assert.NotNull(SafeUpdatePaths.Validate(staging, windows, updates, windows));
                Assert.NotNull(SafeUpdatePaths.Validate(staging, Path.Combine(updates, "staging"), updates, windows));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void FileVersionMatchesTheReleaseTag()
        {
            Assert.True(ReleaseVersion.FileVersionMatches("1.2.3.0", new Version(1, 2, 3)));
            Assert.False(ReleaseVersion.FileVersionMatches("1.2.4.0", new Version(1, 2, 3)));
        }

        [Fact]
        public void PublishedOutputMatchesPackageRules()
        {
            string? directory = Environment.GetEnvironmentVariable("AUTOKEYLIGHT_PUBLISH_DIR");
            if (string.IsNullOrEmpty(directory))
                return;

            List<(string Path, long Length)> entries = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(file => (Path.GetRelativePath(directory, file), new FileInfo(file).Length))
                .ToList();
            Assert.Null(PackageRules.Validate(entries));
            Assert.DoesNotContain(entries, entry => IsUnusedWpfAssembly(entry.Path));
        }

        [Fact]
        public void LegacyPublishedOutputMatchesPackageRules()
        {
            string? directory = Environment.GetEnvironmentVariable("AUTOKEYLIGHT_LEGACY_PUBLISH_DIR");
            if (string.IsNullOrEmpty(directory))
                return;

            List<(string Path, long Length)> entries = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(file => (Path.GetRelativePath(directory, file), new FileInfo(file).Length))
                .ToList();
            Assert.Null(LegacyPackageRules.Validate(entries));
            Assert.DoesNotContain(entries, entry => IsUnusedWpfAssembly(entry.Path));
        }

        private static bool IsUnusedWpfAssembly(string path)
        {
            string name = Path.GetFileName(path);
            return name.StartsWith("Presentation", StringComparison.OrdinalIgnoreCase)
                || name.Equals("ReachFramework.dll", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SavedDefaultsMatchTheDocumentedBehavior()
        {
            string? directory = AppContext.BaseDirectory;
            string? settings = null;
            while (directory != null)
            {
                string candidate = Path.Combine(directory, "CamOnKeyLight", "Properties", "Settings.settings");
                if (File.Exists(candidate))
                {
                    settings = File.ReadAllText(candidate);
                    break;
                }

                directory = Path.GetDirectoryName(directory);
            }

            Assert.NotNull(settings);
            Assert.Contains("<Setting Name=\"CameraOffDelay\"", settings, StringComparison.Ordinal);
            Assert.Contains(">1500<", settings, StringComparison.Ordinal);
            Assert.Contains("<Setting Name=\"LightMode\"", settings, StringComparison.Ordinal);
            Assert.Contains(">Auto<", settings, StringComparison.Ordinal);
            Assert.Contains("<Setting Name=\"Theme\"", settings, StringComparison.Ordinal);
            Assert.Contains(">FollowWindows<", settings, StringComparison.Ordinal);
            Assert.Contains("<Setting Name=\"AutomaticUpdates\"", settings, StringComparison.Ordinal);
            Assert.Contains(">True<", settings, StringComparison.Ordinal);
            Assert.Contains("<Setting Name=\"HotkeyAuto\"", settings, StringComparison.Ordinal);
        }

        private static string Release(string tag, bool prerelease, string downloadPrefix, bool draft = false)
        {
            string hash = new string('a', 64);
            return $$"""
            {
              "tag_name": "{{tag}}",
              "draft": {{(draft ? "true" : "false")}},
              "prerelease": {{(prerelease ? "true" : "false")}},
              "assets": [
                {
                  "name": "CamOnKeyLight-win-x64.zip",
                  "size": 12,
                  "browser_download_url": "{{downloadPrefix}}CamOnKeyLight-win-x64.zip",
                  "digest": "sha256:{{hash}}"
                },
                {
                  "name": "CamOnKeyLight-win-x64.zip.sha256",
                  "size": 80,
                  "browser_download_url": "{{downloadPrefix}}CamOnKeyLight-win-x64.zip.sha256"
                }
              ]
            }
            """;
        }

        private static HttpResponseMessage Response(HttpStatusCode status, string location)
        {
            HttpResponseMessage response = new HttpResponseMessage(status);
            response.Headers.Location = new Uri(location);
            return response;
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;
            public List<Uri> Urls { get; } = new List<Uri>();

            public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                this.responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri != null)
                    Urls.Add(request.RequestUri);
                return Task.FromResult(responder(request));
            }
        }
    }
}
