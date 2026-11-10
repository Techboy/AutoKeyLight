namespace CamOnKeyLight.Core
{
    public static class PresenceTiming
    {
        public static readonly TimeSpan Debounce = TimeSpan.FromSeconds(20);
    }

    public readonly record struct PresenceNotice(string Address, bool Online);

    public static class PresenceText
    {
        public static string Title(bool online)
        {
            return online ? "Light online" : "Light offline";
        }

        public static string Body(string label, bool online)
        {
            string name = string.IsNullOrWhiteSpace(label) ? "A light" : label.Trim();
            return online ? name + " is responding again." : name + " did not respond.";
        }
    }

    /// <summary>
    /// Announces a light going offline or coming back only after the new state
    /// holds still. The first sample sets a baseline and does not notify.
    /// </summary>
    public sealed class PresenceTracker
    {
        private sealed class Slot
        {
            public bool Reachable;
            public bool? Pending;
            public DateTime PendingSince;
        }

        private readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan debounce;

        public PresenceTracker(TimeSpan? debounce = null)
        {
            this.debounce = debounce ?? PresenceTiming.Debounce;
        }

        public PresenceNotice? Observe(string address, bool reachable, DateTime utcNow)
        {
            string key = address.Trim();
            if (!slots.TryGetValue(key, out Slot? slot))
            {
                slots[key] = new Slot { Reachable = reachable };
                return null;
            }

            if (slot.Pending.HasValue && slot.Pending.Value == reachable)
            {
                if (utcNow - slot.PendingSince < debounce)
                    return null;

                slot.Reachable = reachable;
                slot.Pending = null;
                return new PresenceNotice(key, reachable);
            }

            if (reachable == slot.Reachable)
            {
                slot.Pending = null;
                return null;
            }

            slot.Pending = reachable;
            slot.PendingSince = utcNow;
            if (debounce <= TimeSpan.Zero)
            {
                slot.Reachable = reachable;
                slot.Pending = null;
                return new PresenceNotice(key, reachable);
            }

            return null;
        }
    }
}
