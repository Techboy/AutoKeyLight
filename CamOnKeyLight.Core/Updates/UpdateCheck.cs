namespace CamOnKeyLight.Core
{
    public static class UpdateSource
    {
        public const string Owner = "Techboy";
        public const string Repository = "CamOnKeyLight";
        public const string PackageName = "CamOnKeyLight-win-x64.zip";
        public const string ChecksumName = "CamOnKeyLight-win-x64.zip.sha256";
        public static readonly Uri LatestReleaseUri = new Uri("https://api.github.com/repos/Techboy/CamOnKeyLight/releases/latest");
        public const long MaxPackageBytes = 250L * 1024 * 1024;
        public const long MaxChecksumBytes = 4096;
        public const long MaxExtractedBytes = 500L * 1024 * 1024;
        public const int MaxEntries = 2000;
        public static readonly TimeSpan AutomaticInstallQuietPeriod = TimeSpan.FromSeconds(15);

        public static string PackagePath(string tag)
        {
            return "/Techboy/CamOnKeyLight/releases/download/" + tag + "/" + PackageName;
        }

        public static string ChecksumPath(string tag)
        {
            return "/Techboy/CamOnKeyLight/releases/download/" + tag + "/" + ChecksumName;
        }
    }

    public static class ReleaseVersion
    {
        public static bool TryParseStable(string? tag, out Version version)
        {
            version = new Version(0, 0, 0);
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            string text = tag.Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                text = text[1..];

            string[] parts = text.Split('.');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor) || !int.TryParse(parts[2], out int patch))
                return false;

            if (major < 0 || minor < 0 || patch < 0)
                return false;

            version = new Version(major, minor, patch);
            return true;
        }

        public static int Compare(Version left, Version right)
        {
            int major = left.Major.CompareTo(right.Major);
            if (major != 0)
                return major;

            int minor = left.Minor.CompareTo(right.Minor);
            if (minor != 0)
                return minor;

            return Part(left.Build).CompareTo(Part(right.Build));
        }

        public static string Format(Version version)
        {
            return version.Major + "." + version.Minor + "." + Part(version.Build);
        }

        public static bool FileVersionMatches(string? fileVersion, Version release)
        {
            if (string.IsNullOrWhiteSpace(fileVersion))
                return false;

            string numeric = fileVersion.Split(' ')[0];
            if (!Version.TryParse(numeric, out Version? parsed))
                return false;

            return Compare(parsed, release) == 0;
        }

        private static int Part(int value)
        {
            return value < 0 ? 0 : value;
        }
    }

    public sealed record UpdatePackage(Version Version, string Tag, Uri PackageUri, long? PackageSize, string? GitHubSha256, Uri? ChecksumUri);

    public abstract record UpdateCheck
    {
        public sealed record UpToDate(Version Current) : UpdateCheck;
        public sealed record Available(UpdatePackage Package) : UpdateCheck;
        public sealed record Unavailable(string Reason) : UpdateCheck;
    }

    public static class ReleaseParser
    {
        public static UpdateCheck Parse(string json, Version current)
        {
            System.Text.Json.Nodes.JsonNode? root;
            try
            {
                root = System.Text.Json.Nodes.JsonNode.Parse(json);
            }
            catch (System.Text.Json.JsonException)
            {
                return new UpdateCheck.Unavailable("Couldn't read update information.");
            }

            if (root == null)
                return new UpdateCheck.Unavailable("Couldn't read update information.");

            try
            {
                return ParseRoot(root, current);
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException or System.Text.Json.JsonException)
            {
                return new UpdateCheck.Unavailable("Couldn't read update information.");
            }
        }

        private static UpdateCheck ParseRoot(System.Text.Json.Nodes.JsonNode root, Version current)
        {
            if (root["draft"]?.GetValue<bool>() == true || root["prerelease"]?.GetValue<bool>() == true)
                return new UpdateCheck.Unavailable("The latest published entry is not a stable release.");

            string? tag = root["tag_name"]?.GetValue<string>();
            if (!ReleaseVersion.TryParseStable(tag, out Version version) || tag == null)
                return new UpdateCheck.Unavailable("The latest release tag is not a stable version.");

            if (ReleaseVersion.Compare(version, current) <= 0)
                return new UpdateCheck.UpToDate(current);

            System.Text.Json.Nodes.JsonArray? assets = root["assets"]?.AsArray();
            if (assets == null)
                return new UpdateCheck.Unavailable("The latest release does not have a Windows package yet.");

            System.Text.Json.Nodes.JsonNode? package = FindAsset(assets, UpdateSource.PackageName);
            System.Text.Json.Nodes.JsonNode? checksum = FindAsset(assets, UpdateSource.ChecksumName);
            if (package == null || checksum == null)
                return new UpdateCheck.Unavailable("The latest release does not have a Windows package yet.");

            if (!TryAssetUri(package, tag, UpdateSource.PackageName, out Uri? packageUri) || packageUri == null)
                return new UpdateCheck.Unavailable("The download URL is not a Techboy/CamOnKeyLight release asset.");

            if (!TryAssetUri(checksum, tag, UpdateSource.ChecksumName, out Uri? checksumUri) || checksumUri == null)
                return new UpdateCheck.Unavailable("The checksum URL is not a Techboy/CamOnKeyLight release asset.");

            string? digestField = package["digest"]?.GetValue<string>();
            string? digest = null;
            if (!string.IsNullOrWhiteSpace(digestField))
            {
                digest = Sha256Text.ParseGitHubDigest(digestField);
                if (digest == null)
                    return new UpdateCheck.Unavailable("The release digest is not a SHA-256 digest.");
            }

            long? size = package["size"]?.GetValue<long>();
            return new UpdateCheck.Available(new UpdatePackage(version, tag, packageUri, size, digest, checksumUri));
        }

        private static System.Text.Json.Nodes.JsonNode? FindAsset(System.Text.Json.Nodes.JsonArray assets, string name)
        {
            foreach (System.Text.Json.Nodes.JsonNode? asset in assets)
            {
                if (asset != null && string.Equals(asset["name"]?.GetValue<string>(), name, StringComparison.Ordinal))
                    return asset;
            }

            return null;
        }

        private static bool TryAssetUri(System.Text.Json.Nodes.JsonNode asset, string tag, string fileName, out Uri? uri)
        {
            uri = null;
            string? text = asset["browser_download_url"]?.GetValue<string>();
            if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? parsed))
                return false;

            if (!UrlPolicy.IsReleaseAsset(parsed, tag, fileName))
                return false;

            uri = parsed;
            return true;
        }
    }

    public static class UrlPolicy
    {
        private static readonly string[] DownloadHosts =
        {
            "github.com",
            "objects.githubusercontent.com",
            "release-assets.githubusercontent.com",
            "github-releases.githubusercontent.com",
            "api.github.com"
        };

        public static bool IsLatestReleaseApi(Uri uri)
        {
            return IsHttps(uri)
                && uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.Equals("/repos/Techboy/CamOnKeyLight/releases/latest", StringComparison.Ordinal)
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment);
        }

        public static bool IsReleaseAsset(Uri uri, string tag, string fileName)
        {
            if (!IsHttps(uri) || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                return false;

            string expected = "/Techboy/CamOnKeyLight/releases/download/" + tag + "/" + fileName;
            return uri.AbsolutePath.Equals(expected, StringComparison.Ordinal)
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment);
        }

        public static bool IsAllowedTransport(Uri uri)
        {
            if (!IsHttps(uri) || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
                return false;

            if (uri.AbsolutePath.Contains("..", StringComparison.Ordinal))
                return false;

            if (!DownloadHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase))
                return false;

            bool contentDelivery = uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("github-releases.githubusercontent.com", StringComparison.OrdinalIgnoreCase);
            if (!contentDelivery && !string.IsNullOrEmpty(uri.Query))
                return false;

            if (uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
                return IsLatestReleaseApi(uri);

            if (contentDelivery)
                return true;

            return uri.AbsolutePath.StartsWith("/Techboy/CamOnKeyLight/releases/download/v", StringComparison.Ordinal)
                && (uri.AbsolutePath.EndsWith("/" + UpdateSource.PackageName, StringComparison.Ordinal)
                    || uri.AbsolutePath.EndsWith("/" + UpdateSource.ChecksumName, StringComparison.Ordinal));
        }

        private static bool IsHttps(Uri uri)
        {
            return uri.IsAbsoluteUri
                && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(uri.UserInfo)
                && (uri.IsDefaultPort || uri.Port == 443);
        }
    }

    public static class UpdateDecision
    {
        public static bool CanInstall(
            bool packageReady,
            bool cameraStreaming,
            bool automaticEnabled,
            bool userRequested,
            TimeSpan cameraOffFor,
            TimeSpan quietPeriod)
        {
            if (!packageReady || cameraStreaming)
                return false;

            if (userRequested)
                return true;

            if (!automaticEnabled)
                return false;

            return cameraOffFor >= quietPeriod;
        }
    }

    public static class UpdateMessages
    {
        public const string Offline = "Couldn't check for updates. You appear to be offline or GitHub is unreachable.";
        public const string RateLimited = "GitHub update check is rate-limited. Try again later.";
        public const string NoReleases = "No stable release is published yet.";
        public const string Rejected = "The update package failed the integrity checks and was not installed.";
        public const string CameraBusy = "The update is ready. It will install after the camera has been off for a moment.";
        public const string AutomaticOff = "Automatic updates are off.";

        public static string UpToDate(Version version)
        {
            return "Up to date (" + ReleaseVersion.Format(version) + ").";
        }

        public static string Ready(Version version)
        {
            return "Update " + ReleaseVersion.Format(version) + " is ready.";
        }
    }
}
