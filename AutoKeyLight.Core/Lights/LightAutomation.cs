namespace AutoKeyLight.Core
{
    public enum LightMode
    {
        Auto,
        On,
        Off
    }

    public enum LightPower
    {
        On,
        Off
    }

    public readonly record struct LightCommand(long Sequence, LightPower Power)
    {
        public bool TurnOn => Power == LightPower.On;
    }

    public readonly record struct LightTarget(string Address, string Host, LightPower Power);

    /// <summary>
    /// Decides when the lights should change. Automatic mode keeps the existing
    /// webcam delays. Manual On and Off ignore the webcam, and Off never waits.
    /// </summary>
    public sealed class LightModeController
    {
        private bool cameraLatchedOn;
        private DateTime cameraOnSinceUtc;
        private DateTime cameraOffSinceUtc;
        private readonly DateTime autoCommandsEnabledAtUtc;
        private long sequence;

        public LightModeController(DateTime startedUtc)
        {
            cameraOnSinceUtc = startedUtc;
            cameraOffSinceUtc = startedUtc;
            // The previous build skipped the first automatic command for about a second.
            autoCommandsEnabledAtUtc = startedUtc.AddSeconds(1);
        }

        public LightMode Mode { get; private set; } = LightMode.Auto;

        public long Sequence => sequence;

        public static bool AppliesWithoutWebcam(LightMode mode)
        {
            return mode != LightMode.Auto;
        }

        public LightCommand? ApplyStartupMode(LightMode persisted, bool cameraOn, DateTime utcNow)
        {
            if (persisted == LightMode.Auto)
                return null;

            return SetMode(persisted, cameraOn, utcNow);
        }

        public LightCommand? SetMode(LightMode next, bool cameraOn, DateTime utcNow)
        {
            LightMode previous = Mode;
            Mode = next;

            if (next == LightMode.On)
                return Command(LightPower.On);

            // Manual Off is immediate. It does not wait for the anti-flicker delay.
            if (next == LightMode.Off)
                return Command(LightPower.Off);

            if (previous != LightMode.Auto)
            {
                LatchCamera(cameraOn, utcNow, forceTimestamp: true);
                return Command(cameraOn ? LightPower.On : LightPower.Off);
            }

            return null;
        }

        public LightCommand? OnCameraSample(bool cameraOn, DateTime utcNow, TimeSpan onDelay, TimeSpan offDelay)
        {
            if (onDelay < TimeSpan.Zero)
                onDelay = TimeSpan.Zero;
            if (offDelay < TimeSpan.Zero)
                offDelay = TimeSpan.Zero;

            if (Mode == LightMode.On)
                return Command(LightPower.On);

            if (Mode == LightMode.Off)
                return Command(LightPower.Off);

            LatchCamera(cameraOn, utcNow, forceTimestamp: false);

            if (utcNow < autoCommandsEnabledAtUtc)
                return null;

            if (cameraOn)
            {
                // A camera that is active again cancels a pending automatic off.
                if (utcNow < cameraOnSinceUtc.Add(onDelay))
                    return null;

                return Command(LightPower.On);
            }

            if (utcNow < cameraOffSinceUtc.Add(offDelay))
                return null;

            return Command(LightPower.Off);
        }

        public bool IsCurrent(long commandSequence)
        {
            return commandSequence == sequence;
        }

        public static LightControlSnapshot Describe(LightMode mode)
        {
            return mode switch
            {
                LightMode.On => new LightControlSnapshot(
                    mode,
                    "Auto Key Light — On",
                    "On — lights stay on, webcam is ignored",
                    false,
                    true,
                    false),
                LightMode.Off => new LightControlSnapshot(
                    mode,
                    "Auto Key Light — Off",
                    "Off — lights stay off, webcam is ignored",
                    false,
                    false,
                    true),
                _ => new LightControlSnapshot(
                    LightMode.Auto,
                    "Auto Key Light — Auto",
                    "Auto — lights follow the webcam",
                    true,
                    false,
                    false)
            };
        }

        private LightCommand Command(LightPower power)
        {
            sequence++;
            return new LightCommand(sequence, power);
        }

        private void LatchCamera(bool cameraOn, DateTime utcNow, bool forceTimestamp)
        {
            if (cameraOn)
            {
                if (!cameraLatchedOn || forceTimestamp)
                    cameraOnSinceUtc = utcNow;
                cameraLatchedOn = true;
            }
            else if (cameraLatchedOn || forceTimestamp)
            {
                cameraOffSinceUtc = utcNow;
                cameraLatchedOn = false;
            }
            else
            {
                cameraLatchedOn = false;
            }
        }
    }

    public readonly record struct LightControlSnapshot(
        LightMode Mode,
        string Title,
        string Detail,
        bool AutoChecked,
        bool OnChecked,
        bool OffChecked);

    public static class LightModePreference
    {
        public const string DefaultValue = "Auto";

        public static LightMode Parse(string? value)
        {
            if (value != null
                && Enum.TryParse(value.Trim(), ignoreCase: true, out LightMode mode)
                && Enum.IsDefined(mode)
                && string.Equals(Enum.GetName(mode), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return mode;
            }

            return LightMode.Auto;
        }
    }

    public static class LightTiming
    {
        /// <summary>
        /// Long enough to cover a brief gap while Windows switches camera apps,
        /// short enough that the lights still turn off promptly.
        /// </summary>
        public const decimal DefaultOffDelayMilliseconds = 1500;

        public static readonly TimeSpan DefaultOffDelay = TimeSpan.FromMilliseconds(1500);

        public static readonly TimeSpan LightRequestTimeout = TimeSpan.FromSeconds(5);
    }

    public static class LightEndpoint
    {
        public static string Host(string address)
        {
            if (address.Contains(':', StringComparison.Ordinal))
                return address;

            return address + ":9123";
        }

        public static string LightsUrl(string address)
        {
            return "http://" + Host(address) + "/elgato/lights";
        }

        public static string PutBody(bool turnOn)
        {
            int onState = turnOn ? 1 : 0;
            return "{\"numberOfLights\": 1, \"lights\": [{\"on\": " + onState + "}]}";
        }

        public static IReadOnlyList<LightTarget> ForEachLight(IEnumerable<string> addresses, LightPower power)
        {
            List<LightTarget> targets = new List<LightTarget>();
            foreach (string address in addresses)
            {
                if (string.IsNullOrWhiteSpace(address))
                    continue;

                string trimmed = address.Trim();
                targets.Add(new LightTarget(trimmed, Host(trimmed), power));
            }

            return targets;
        }
    }

    public static class LightResponse
    {
        public static bool TryReadPower(string json, out bool on)
        {
            on = false;
            try
            {
                System.Text.Json.Nodes.JsonNode? root = System.Text.Json.Nodes.JsonNode.Parse(json);
                System.Text.Json.Nodes.JsonArray? lights = root?["lights"]?.AsArray();
                if (lights == null || lights.Count == 0)
                    return false;

                System.Text.Json.Nodes.JsonNode? flag = lights[0]?["on"];
                if (flag == null)
                    return false;

                int value = flag.GetValue<int>();
                if (value != 0 && value != 1)
                    return false;

                on = value == 1;
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }
    }
}
