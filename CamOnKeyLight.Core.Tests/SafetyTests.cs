using CamOnKeyLight.Core;

namespace CamOnKeyLight.Core.Tests
{
    public class SafetyTests
    {
        [Theory]
        [InlineData("192.168.1.5", "http://192.168.1.5:9123/elgato/lights")]
        [InlineData("192.168.1.5:80", "http://192.168.1.5:80/elgato/lights")]
        [InlineData("light.local", "http://light.local:9123/elgato/lights")]
        [InlineData("[::1]", "http://[::1]:9123/elgato/lights")]
        [InlineData("[::1]:9000", "http://[::1]:9000/elgato/lights")]
        public void LightAddressesAcceptHostsAndOptionalPorts(string address, string url)
        {
            Assert.True(LightAddressRules.TryCreateUrl(address, out string created, out string? error));
            Assert.Null(error);
            Assert.Equal(url, created);
        }

        [Theory]
        [InlineData("http://192.168.1.5")]
        [InlineData("192.168.1.5/elgato/lights")]
        [InlineData("user@192.168.1.5")]
        [InlineData("192.168.1.5?on=1")]
        [InlineData("192.168.1.5#lamp")]
        [InlineData("192.168.1.5:0")]
        [InlineData("192.168.1.5:65536")]
        [InlineData("")]
        public void LightAddressesRejectWebAddressesAndBadPorts(string address)
        {
            Assert.False(LightAddressRules.TryCreateUrl(address, out _, out string? error));
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void UpdateRemovesStaleFilesAndKeepsLogs()
        {
            string root = NewRoot();
            string source = Path.Combine(root, "source");
            string destination = Path.Combine(root, "app");
            string backup = Path.Combine(root, "backup");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(Path.Combine(destination, "logs"));
            File.WriteAllText(Path.Combine(destination, "keep.txt"), "old");
            File.WriteAllText(Path.Combine(destination, "stale.txt"), "gone");
            File.WriteAllText(Path.Combine(destination, "logs", "light.log"), "keep-me");
            File.WriteAllText(Path.Combine(source, "keep.txt"), "new");
            File.WriteAllText(Path.Combine(source, "fresh.txt"), "fresh");
            try
            {
                DirectoryReplace.Replace(source, destination, backup);
                Assert.Equal("new", File.ReadAllText(Path.Combine(destination, "keep.txt")));
                Assert.Equal("fresh", File.ReadAllText(Path.Combine(destination, "fresh.txt")));
                Assert.False(File.Exists(Path.Combine(destination, "stale.txt")));
                Assert.Equal("keep-me", File.ReadAllText(Path.Combine(destination, "logs", "light.log")));
                Assert.Equal("gone", File.ReadAllText(Path.Combine(backup, "stale.txt")));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void AFailedUpdateRestoresRemovedFiles()
        {
            string root = NewRoot();
            string source = Path.Combine(root, "source");
            string destination = Path.Combine(root, "app");
            string backup = Path.Combine(root, "backup");
            string outside = Path.Combine(root, "outside");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(destination);
            Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(destination, "old.txt"), "old");
            File.WriteAllText(Path.Combine(source, "new.txt"), "new");
            File.WriteAllText(Path.Combine(outside, "safe.txt"), "safe");
            Directory.CreateSymbolicLink(Path.Combine(source, "linked"), outside);
            try
            {
                Assert.Throws<InvalidOperationException>(() => DirectoryReplace.Replace(source, destination, backup));
                Assert.Equal("old", File.ReadAllText(Path.Combine(destination, "old.txt")));
                Assert.False(File.Exists(Path.Combine(destination, "new.txt")));
                Assert.Equal("safe", File.ReadAllText(Path.Combine(outside, "safe.txt")));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void RetiredInstallOnlyAcceptsTheOldProgramFolder()
        {
            string root = NewRoot();
            string expected = SettingsLocations.LegacyInstalledAppDirectory(root);
            Directory.CreateDirectory(expected);
            string elsewhere = Path.Combine(root, "elsewhere");
            Directory.CreateDirectory(elsewhere);
            try
            {
                Assert.True(InstallHandoff.IsRetiredInstallDirectory(root, expected));
                Assert.True(InstallHandoff.IsRetiredInstallDirectory(root, Path.Combine(expected, "..", "AutoKeyLight")));
                Assert.False(InstallHandoff.IsRetiredInstallDirectory(root, elsewhere));
                Assert.False(InstallHandoff.IsRetiredInstallDirectory(root, Path.Combine(expected, "unins000.exe")));
                string linkParent = Path.Combine(root, "Programs");
                string link = Path.Combine(linkParent, "AutoKeyLight");
                Directory.Delete(expected);
                Directory.CreateSymbolicLink(link, elsewhere);
                Assert.False(InstallHandoff.IsRetiredInstallDirectory(root, link));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private static string NewRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "camonkeylight-safety-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
