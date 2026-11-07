using System.IO.Compression;
using System.Security.Cryptography;

namespace AutoKeyLight.Core
{
    public static class Sha256Text
    {
        public static string Hex(byte[] data)
        {
            return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        }

        public static string? ParseGitHubDigest(string? digest)
        {
            if (string.IsNullOrWhiteSpace(digest))
                return null;

            string text = digest.Trim();
            if (!text.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                return null;

            return Normalize(text["sha256:".Length..]);
        }

        public static bool TryParseChecksumFile(string text, out string hex)
        {
            hex = "";
            string trimmed = text.Trim().TrimStart('\uFEFF');
            string[] lines = trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length != 1)
                return false;

            string[] parts = lines[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is < 1 or > 2)
                return false;

            string? normalized = Normalize(parts[0]);
            if (normalized == null)
                return false;

            if (parts.Length == 2)
            {
                string name = parts[1].TrimStart('*');
                if (!name.Equals(UpdateSource.PackageName, StringComparison.Ordinal))
                    return false;
            }

            hex = normalized;
            return true;
        }

        public static bool FixedEquals(string leftHex, string rightHex)
        {
            try
            {
                byte[] left = Convert.FromHexString(leftHex);
                byte[] right = Convert.FromHexString(rightHex);
                return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string? Verify(byte[] data, string? githubDigestField, string checksumFileText, long? declaredSize)
        {
            if (declaredSize is long size && size != data.LongLength)
                return "The download size does not match the release.";

            if (!string.IsNullOrWhiteSpace(githubDigestField) && ParseGitHubDigest(githubDigestField) == null)
                return "The release digest is not a SHA-256 digest.";

            if (!TryParseChecksumFile(checksumFileText, out string checksumHex))
                return "The checksum file is not valid.";

            string? github = ParseGitHubDigest(githubDigestField);
            if (github != null && !FixedEquals(github, checksumHex))
                return "The checksum file does not match GitHub's digest.";

            string actual = Hex(data);
            if (!FixedEquals(actual, checksumHex))
                return "The download does not match the published checksum.";

            if (github != null && !FixedEquals(actual, github))
                return "The download does not match GitHub's digest.";

            return null;
        }

        private static string? Normalize(string hex)
        {
            string text = hex.Trim().ToLowerInvariant();
            if (text.Length != 64)
                return null;

            foreach (char character in text)
            {
                bool digit = character is >= '0' and <= '9' or >= 'a' and <= 'f';
                if (!digit)
                    return null;
            }

            return text;
        }
    }

    public static class PackageRules
    {
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".json", ".config", ".xml", ".pdb", ".txt", ".md", ".png", ".ico", ".gif"
        };

        private static readonly HashSet<string> AllowedExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AutoKeyLight.exe", "createdump.exe"
        };

        private static readonly HashSet<string> Extensionless = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LICENSE", "NOTICE", "THIRD-PARTY-NOTICES"
        };

        public static string? Validate(IEnumerable<(string Path, long Length)> entries)
        {
            bool foundApp = false;
            int count = 0;
            long total = 0;
            foreach ((string path, long length) in entries)
            {
                count++;
                if (count > UpdateSource.MaxEntries)
                    return "The update package has too many files.";

                if (length < 0)
                    return "The update package has an invalid file size.";

                total += length;
                if (total > UpdateSource.MaxExtractedBytes)
                    return "The update package is larger than expected.";

                if (!TryRelative(path, out string relative, out bool directory))
                    return "The update package contains an unsafe path.";

                if (directory)
                    continue;

                if (relative.Equals("AutoKeyLight.exe", StringComparison.OrdinalIgnoreCase))
                    foundApp = true;

                if (!IsAllowedFile(relative))
                    return "The update package contains a file that is not part of the app: " + relative;
            }

            if (!foundApp)
                return "The update package does not contain AutoKeyLight.exe.";

            return null;
        }

        public static string ResolveEntryPath(string destinationRoot, string entryName)
        {
            if (!TryRelative(entryName, out string relative, out _))
                throw new InvalidDataException("The update package contains an unsafe path.");

            string combined = destinationRoot;
            foreach (string part in relative.Split('/'))
            {
                if (part.Length > 0)
                    combined = Path.Combine(combined, part);
            }

            string fullDestination = Path.GetFullPath(destinationRoot);
            string fullEntry = Path.GetFullPath(combined);
            if (!IsInside(fullEntry, fullDestination))
                throw new InvalidDataException("The update package contains an unsafe path.");

            return fullEntry;
        }

        public static void Extract(Stream zipStream, string destinationDirectory)
        {
            using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            string? error = Validate(archive.Entries.Select(entry => (entry.FullName, entry.Length)));
            if (error != null)
                throw new InvalidDataException(error);

            Directory.CreateDirectory(destinationDirectory);
            long written = 0;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string target = ResolveEntryPath(destinationDirectory, entry.FullName);
                if (string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith('/'))
                {
                    Directory.CreateDirectory(target);
                    continue;
                }

                string? parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                    Directory.CreateDirectory(parent);

                using Stream input = entry.Open();
                using FileStream output = File.Create(target);
                byte[] buffer = new byte[81920];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    written += read;
                    if (written > UpdateSource.MaxExtractedBytes)
                        throw new InvalidDataException("The update package is larger than expected.");

                    output.Write(buffer, 0, read);
                }
            }
        }

        private static bool IsAllowedFile(string relative)
        {
            string name = relative.Contains('/') ? relative[(relative.LastIndexOf('/') + 1)..] : relative;
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return !relative.Contains('/') && AllowedExecutables.Contains(name);

            string extension = Path.GetExtension(name);
            if (extension.Length == 0)
                return Extensionless.Contains(name);

            return AllowedExtensions.Contains(extension);
        }

        private static bool TryRelative(string path, out string relative, out bool directory)
        {
            relative = "";
            directory = false;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string normalized = path.Replace('\\', '/');
            directory = normalized.EndsWith('/');
            if (normalized.StartsWith('/') || normalized.Contains(':'))
                return false;

            string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            foreach (string part in parts)
            {
                if (part is "." or "..")
                    return false;
                if (part.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-' or '+')))
                    return false;
            }

            relative = string.Join('/', parts);
            return true;
        }

        public static bool IsInside(string path, string root)
        {
            string fullPath = Path.GetFullPath(path);
            string fullRoot = Path.GetFullPath(root);
            if (string.Equals(fullPath, fullRoot, StringComparison.OrdinalIgnoreCase))
                return true;

            string prefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class SafeUpdatePaths
    {
        public static string? Validate(string source, string destination, string updatesRoot, string windowsRoot)
        {
            string fullSource = Path.GetFullPath(source);
            string fullDestination = Path.GetFullPath(destination);
            string fullUpdates = Path.GetFullPath(updatesRoot);
            string fullWindows = Path.GetFullPath(windowsRoot);

            if (string.Equals(fullSource, fullDestination, StringComparison.OrdinalIgnoreCase))
                return "The update source and the install folder are the same.";

            if (!PackageRules.IsInside(fullSource, Path.Combine(fullUpdates, "staging")))
                return "The update files are not in the staging folder.";

            if (PackageRules.IsInside(fullDestination, fullUpdates))
                return "The install folder cannot be inside the update folder.";

            if (PackageRules.IsInside(fullDestination, fullWindows))
                return "The install folder is inside the Windows directory.";

            string? root = Path.GetPathRoot(fullDestination);
            if (root != null && string.Equals(Path.GetFullPath(root), fullDestination, StringComparison.OrdinalIgnoreCase))
                return "The install folder cannot be the root of a drive.";

            if (PackageRules.IsInside(fullSource, fullDestination) || PackageRules.IsInside(fullDestination, fullSource))
                return "The update folders overlap.";

            return null;
        }
    }

    public static class FolderCopy
    {
        public static void CopyContents(string source, string destination)
        {
            DirectoryInfo root = new DirectoryInfo(source);
            CopyDirectory(root, Path.GetFullPath(destination));
        }

        private static void CopyDirectory(DirectoryInfo source, string destination)
        {
            if (source.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException("Refusing to copy a link.");

            Directory.CreateDirectory(destination);
            foreach (FileInfo file in source.EnumerateFiles())
            {
                if (file.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    throw new InvalidOperationException("Refusing to copy a link.");

                string target = Path.Combine(destination, file.Name);
                if (!PackageRules.IsInside(target, destination))
                    throw new InvalidOperationException("Refusing to copy outside the destination.");

                file.CopyTo(target, overwrite: true);
            }

            foreach (DirectoryInfo child in source.EnumerateDirectories())
                CopyDirectory(child, Path.Combine(destination, child.Name));
        }
    }

    public static class DirectoryReplace
    {
        public static void Replace(string source, string destination, string backup)
        {
            if (Directory.Exists(backup))
                Directory.Delete(backup, recursive: true);

            if (Directory.Exists(destination))
                FolderCopy.CopyContents(destination, backup);

            try
            {
                FolderCopy.CopyContents(source, destination);
            }
            catch
            {
                if (Directory.Exists(backup))
                    FolderCopy.CopyContents(backup, destination);
                throw;
            }
        }
    }

    public readonly record struct ApplyRequest(int ProcessId, string Source, string Destination, Version ExpectedVersion);

    public static class ApplyArguments
    {
        public static bool TryParse(string[] args, out ApplyRequest request, out string? error)
        {
            request = default;
            error = null;
            if (args.Length == 0 || !args[0].Equals("--apply-update", StringComparison.Ordinal))
            {
                error = "Not an update request.";
                return false;
            }

            int? pid = null;
            string? source = null;
            string? destination = null;
            Version? version = null;
            for (int i = 1; i < args.Length; i++)
            {
                string name = args[i];
                if (i + 1 >= args.Length)
                {
                    error = "The update request is incomplete.";
                    return false;
                }

                string value = args[++i];
                switch (name)
                {
                    case "--pid":
                        if (!int.TryParse(value, out int parsed) || parsed <= 0)
                        {
                            error = "The update request has a bad process id.";
                            return false;
                        }

                        pid = parsed;
                        break;
                    case "--source":
                        source = value;
                        break;
                    case "--dest":
                        destination = value;
                        break;
                    case "--expected-version":
                        if (!ReleaseVersion.TryParseStable(value, out Version parsedVersion))
                        {
                            error = "The update request has a bad version.";
                            return false;
                        }

                        version = parsedVersion;
                        break;
                    default:
                        error = "The update request has an unknown option.";
                        return false;
                }
            }

            if (pid == null || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination) || version == null)
            {
                error = "The update request is incomplete.";
                return false;
            }

            request = new ApplyRequest(pid.Value, source, destination, version);
            return true;
        }
    }
}
