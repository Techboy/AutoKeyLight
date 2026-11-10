namespace AutoKeyLight.Core
{
    public enum CameraAppFilter
    {
        All,
        Only,
        Except
    }

    public static class CameraAppFilterPreference
    {
        public static CameraAppFilter Parse(string? value)
        {
            if (value != null && value.Trim().Equals("Only", StringComparison.OrdinalIgnoreCase))
                return CameraAppFilter.Only;

            if (value != null && value.Trim().Equals("Except", StringComparison.OrdinalIgnoreCase))
                return CameraAppFilter.Except;

            return CameraAppFilter.All;
        }

        public static string Normalize(string? value)
        {
            return Parse(value).ToString();
        }
    }

    public static class CameraAppNames
    {
        public const string Unknown = "(unknown app)";

        public static string Friendly(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string text = raw.Trim();
            if (text.Equals(Unknown, StringComparison.OrdinalIgnoreCase))
                return Unknown;

            if (text.Contains('#'))
                text = text.Replace('#', '\\');

            int slash = Math.Max(text.LastIndexOf('\\'), text.LastIndexOf('/'));
            if (slash >= 0 && slash < text.Length - 1)
                text = text[(slash + 1)..];

            if (text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return text;

            int underscore = text.IndexOf('_');
            if (underscore > 0)
            {
                string family = text[..underscore];
                int dot = family.LastIndexOf('.');
                if (dot >= 0 && dot < family.Length - 1)
                    return family[(dot + 1)..];
            }

            return text;
        }

        public static List<string> Clean(IEnumerable<string>? names)
        {
            List<string> cleaned = new List<string>();
            if (names == null)
                return cleaned;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                string friendly = Friendly(name);
                if (friendly.Length == 0 || !seen.Add(friendly))
                    continue;

                cleaned.Add(friendly);
            }

            return cleaned;
        }
    }

    public static class CameraAppPolicy
    {
        public static bool CountsAsOn(CameraAppFilter filter, IEnumerable<string>? selected, IEnumerable<string>? active)
        {
            HashSet<string> chosen = new HashSet<string>(CameraAppNames.Clean(selected), StringComparer.OrdinalIgnoreCase);
            List<string> live = CameraAppNames.Clean(active);
            if (live.Count == 0)
                return false;

            return filter switch
            {
                CameraAppFilter.Only => live.Any(chosen.Contains),
                CameraAppFilter.Except => live.Any(name => !chosen.Contains(name)),
                _ => true
            };
        }
    }
}
