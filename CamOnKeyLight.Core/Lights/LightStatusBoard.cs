namespace CamOnKeyLight.Core
{
    public enum LightLinkState
    {
        Unknown,
        Checking,
        On,
        Off,
        Unreachable,
        UnexpectedReply,
        InvalidAddress
    }

    public enum LightSummaryKind
    {
        Empty,
        Checking,
        AllOn,
        AllOff,
        Mixed,
        Problems
    }

    public readonly record struct LightRow(string Address, LightLinkState State, string StatusText, string Detail);

    public static class LightDiagnostics
    {
        public static string Unreachable(string address)
        {
            return "Can't reach " + address + ". The light did not respond.";
        }

        public static string Invalid(string address)
        {
            return address + " is not a valid light address. Use an IP address or host name, and an optional port.";
        }

        public static string Unexpected(string address)
        {
            return address + " replied, but the reply was not a light status.";
        }

        public static string HttpFailure(string address, int statusCode)
        {
            return address + " returned HTTP " + statusCode + ".";
        }

        public static string OnDetail(string address)
        {
            return address + " is on.";
        }

        public static string OffDetail(string address)
        {
            return address + " is off.";
        }

        public static string StatusText(LightLinkState state)
        {
            return state switch
            {
                LightLinkState.On => "On",
                LightLinkState.Off => "Off",
                LightLinkState.Unreachable => "Unreachable",
                LightLinkState.UnexpectedReply => "Unexpected reply",
                LightLinkState.InvalidAddress => "Invalid address",
                LightLinkState.Checking => "Checking…",
                _ => "Not checked"
            };
        }
    }

    /// <summary>
    /// Remembers the last completed check for each light. An older request cannot
    /// overwrite a newer one, and a failed check never stays displayed as on.
    /// </summary>
    public sealed class LightStatusBoard
    {
        private sealed class Entry
        {
            public int Attempt;
            public LightLinkState State;
            public string Detail = "";
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> order = new List<string>();

        public void SyncAddresses(IEnumerable<string> addresses)
        {
            HashSet<string> keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            order.Clear();
            foreach (string address in addresses)
            {
                if (string.IsNullOrWhiteSpace(address))
                    continue;

                string trimmed = address.Trim();
                if (!keep.Add(trimmed))
                    continue;

                order.Add(trimmed);
                if (!entries.ContainsKey(trimmed))
                    entries[trimmed] = new Entry();
            }

            List<string> stale = entries.Keys.Where(address => !keep.Contains(address)).ToList();
            foreach (string address in stale)
                entries.Remove(address);
        }

        public int BeginUserRetry(string address)
        {
            Entry entry = Require(address);
            entry.Attempt++;
            entry.State = LightLinkState.Checking;
            entry.Detail = "Checking " + address + "…";
            return entry.Attempt;
        }

        public int BeginBackgroundRefresh(string address)
        {
            Entry entry = Require(address);
            entry.Attempt++;
            if (entry.State == LightLinkState.Unknown)
            {
                entry.State = LightLinkState.Checking;
                entry.Detail = "Checking " + address + "…";
            }

            return entry.Attempt;
        }

        public void Report(string address, LightLinkState state, string detail)
        {
            if (!entries.TryGetValue(address, out Entry? entry))
                return;

            entry.Attempt++;
            entry.State = state;
            entry.Detail = detail;
        }

        public bool TryComplete(string address, int attempt, LightLinkState state, string detail)
        {
            if (!entries.TryGetValue(address, out Entry? entry))
                return false;

            if (entry.Attempt != attempt)
                return false;

            if (state == LightLinkState.Unknown)
                return false;

            entry.State = state;
            entry.Detail = detail;
            return true;
        }

        public IReadOnlyList<LightRow> Rows
        {
            get
            {
                List<LightRow> rows = new List<LightRow>(order.Count);
                foreach (string address in order)
                {
                    Entry entry = entries[address];
                    rows.Add(new LightRow(address, entry.State, LightDiagnostics.StatusText(entry.State), entry.Detail));
                }

                return rows;
            }
        }

        public string? FirstProblem
        {
            get
            {
                foreach (LightRow row in Rows)
                {
                    if (row.State is LightLinkState.Unreachable or LightLinkState.UnexpectedReply or LightLinkState.InvalidAddress)
                        return row.Detail;
                }

                return null;
            }
        }

        public LightSummaryKind Kind
        {
            get
            {
                IReadOnlyList<LightRow> rows = Rows;
                if (rows.Count == 0)
                    return LightSummaryKind.Empty;

                int on = 0;
                int off = 0;
                int bad = 0;
                int pending = 0;
                foreach (LightRow row in rows)
                {
                    switch (row.State)
                    {
                        case LightLinkState.On:
                            on++;
                            break;
                        case LightLinkState.Off:
                            off++;
                            break;
                        case LightLinkState.Unreachable:
                        case LightLinkState.UnexpectedReply:
                        case LightLinkState.InvalidAddress:
                            bad++;
                            break;
                        default:
                            pending++;
                            break;
                    }
                }

                if (bad > 0)
                    return LightSummaryKind.Problems;
                if (on == rows.Count)
                    return LightSummaryKind.AllOn;
                if (off == rows.Count)
                    return LightSummaryKind.AllOff;
                if (pending == rows.Count)
                    return LightSummaryKind.Checking;
                if (on > 0 && off > 0)
                    return LightSummaryKind.Mixed;

                return LightSummaryKind.Checking;
            }
        }

        public string Summary
        {
            get
            {
                IReadOnlyList<LightRow> rows = Rows;
                int on = rows.Count(row => row.State == LightLinkState.On);
                int off = rows.Count(row => row.State == LightLinkState.Off);
                int bad = rows.Count(row => row.State is LightLinkState.Unreachable or LightLinkState.UnexpectedReply or LightLinkState.InvalidAddress);

                return Kind switch
                {
                    LightSummaryKind.Empty => "No lights added",
                    LightSummaryKind.AllOn => "Lights are ON",
                    LightSummaryKind.AllOff => "Lights are OFF",
                    LightSummaryKind.Problems when on == 0 && off == 0 => "Lights are unreachable",
                    LightSummaryKind.Problems => on + " on, " + off + " off, " + bad + " unreachable",
                    LightSummaryKind.Mixed => on + " on, " + off + " off",
                    _ => "Checking lights…"
                };
            }
        }

        private Entry Require(string address)
        {
            if (!entries.TryGetValue(address, out Entry? entry))
                throw new ArgumentException("That light is not in the list.", nameof(address));

            return entry;
        }
    }
}
