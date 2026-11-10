namespace CamOnKeyLight.Core
{
    public static class LightGrouping
    {
        public const string UngroupedLabel = "Ungrouped";

        public static List<string> Clean(IEnumerable<string>? names)
        {
            List<string> cleaned = new List<string>();
            if (names == null)
                return cleaned;

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                string trimmed = (name ?? "").Trim();
                if (trimmed.Length == 0 || !seen.Add(trimmed))
                    continue;

                cleaned.Add(trimmed);
            }

            return cleaned;
        }

        public static IReadOnlyList<string> Names(IEnumerable<LightRecord> lights)
        {
            return Clean(lights.Select(light => light.Group));
        }

        public static IReadOnlyList<LightRecord> Members(IEnumerable<LightRecord> lights, string group)
        {
            List<LightRecord> members = new List<LightRecord>();
            foreach (LightRecord light in lights)
            {
                if (string.Equals(light.Group.Trim(), group.Trim(), StringComparison.OrdinalIgnoreCase))
                    members.Add(light);
            }

            return members;
        }

        public static IReadOnlyList<string> AutomaticAddresses(IEnumerable<LightRecord> lights, bool allGroups, IEnumerable<string>? selectedGroups)
        {
            HashSet<string> chosen = new HashSet<string>(Clean(selectedGroups), StringComparer.OrdinalIgnoreCase);
            List<string> addresses = new List<string>();
            foreach (LightRecord light in lights)
            {
                if (string.IsNullOrWhiteSpace(light.Address))
                    continue;

                string group = light.Group.Trim();
                bool include = allGroups || (group.Length > 0 && chosen.Contains(group));
                if (include)
                    addresses.Add(light.Address.Trim());
            }

            return addresses;
        }
    }
}
