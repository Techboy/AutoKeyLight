using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal static class AppLog
    {
        private const int MaxBytes = 1_000_000;
        private const int KeptFiles = 3;
        private static readonly object Gate = new object();

        public static void Write(string address, string operation, string message)
        {
            try
            {
                string directory = Path.Combine(
                    SettingsLocations.DataDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                    "logs");
                Directory.CreateDirectory(directory);
                string line = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    + " " + OneLine(operation) + " " + OneLine(address) + " " + OneLine(message) + Environment.NewLine;
                lock (Gate)
                {
                    string path = Path.Combine(directory, "light.log");
                    Rotate(directory, path, line.Length);
                    File.AppendAllText(path, line);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        private static void Rotate(string directory, string path, int incoming)
        {
            if (!File.Exists(path))
                return;

            if (new FileInfo(path).Length + incoming < MaxBytes)
                return;

            string oldest = Path.Combine(directory, "light." + (KeptFiles - 1) + ".log");
            if (File.Exists(oldest))
                File.Delete(oldest);

            for (int index = KeptFiles - 2; index >= 1; index--)
            {
                string from = Path.Combine(directory, "light." + index + ".log");
                string to = Path.Combine(directory, "light." + (index + 1) + ".log");
                if (File.Exists(from))
                    File.Move(from, to, overwrite: true);
            }

            File.Move(path, Path.Combine(directory, "light.1.log"), overwrite: true);
        }

        private static string OneLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            string flat = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return flat.Length <= 300 ? flat : flat[..300];
        }
    }
}