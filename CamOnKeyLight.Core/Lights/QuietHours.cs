namespace CamOnKeyLight.Core
{
    public static class QuietHours
    {
        public const string AllDays = "Sun,Mon,Tue,Wed,Thu,Fri,Sat";
        public const string NoDays = "-";

        public static readonly string[] DayTokens = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        public static string NormalizeTime(string? text, string fallback)
        {
            return TryParseTime(text, out _) ? text!.Trim() : fallback;
        }

        public static string NormalizeDays(string? text)
        {
            if (text != null && text.Trim() == NoDays)
                return NoDays;

            if (string.IsNullOrWhiteSpace(text))
                return AllDays;

            return FormatDays(ParseDays(text));
        }

        public static bool TryParseTime(string? text, out int minutes)
        {
            minutes = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string[] parts = text.Trim().Split(':');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out int hour) || !int.TryParse(parts[1], out int minute))
                return false;

            if (hour is < 0 or > 23 || minute is < 0 or > 59)
                return false;

            minutes = hour * 60 + minute;
            return true;
        }

        public static string FormatTime(int minutes)
        {
            int day = ((minutes % 1440) + 1440) % 1440;
            return (day / 60).ToString("00") + ":" + (day % 60).ToString("00");
        }

        public static bool[] ParseDays(string? text)
        {
            bool[] days = new bool[7];
            if (text != null && text.Trim() == NoDays)
                return days;

            if (string.IsNullOrWhiteSpace(text))
            {
                Array.Fill(days, true);
                return days;
            }

            bool any = false;
            foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                for (int i = 0; i < DayTokens.Length; i++)
                {
                    if (DayTokens[i].Equals(part, StringComparison.OrdinalIgnoreCase))
                    {
                        days[i] = true;
                        any = true;
                    }
                }
            }

            if (!any)
                Array.Fill(days, true);

            return days;
        }

        public static string FormatDays(bool[] days)
        {
            if (days.Length != 7 || days.All(day => !day))
                return NoDays;

            List<string> tokens = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                if (days[i])
                    tokens.Add(DayTokens[i]);
            }

            return string.Join(",", tokens);
        }

        public static bool IsQuiet(AppUserSettings settings, DateTime localNow)
        {
            if (!settings.QuietHoursEnabled)
                return false;

            if (!TryParseTime(settings.QuietStart, out int start) || !TryParseTime(settings.QuietEnd, out int end))
                return false;

            bool[] days = ParseDays(settings.QuietDays);
            int day = (int)localNow.DayOfWeek;
            if (day < 0 || day >= days.Length || !days[day])
                return false;

            int minute = localNow.Hour * 60 + localNow.Minute;
            if (start == end)
                return true;

            if (start < end)
                return minute >= start && minute < end;

            return minute >= start || minute < end;
        }

        public static string TrayTip(string title, bool quiet)
        {
            string text = quiet ? title + " — quiet hours" : title;
            if (text.Length <= 63)
                return text;

            return quiet ? "Cam On Key Light — quiet hours" : "Cam On Key Light";
        }

        public static string MenuLabel(AppUserSettings settings, DateTime localNow)
        {
            if (!settings.QuietHoursEnabled)
                return "Quiet hours off";

            if (!IsQuiet(settings, localNow))
                return "Quiet hours — not now";

            string end = NormalizeTime(settings.QuietEnd, "07:00");
            return "Quiet hours — on until " + end;
        }
    }
}
