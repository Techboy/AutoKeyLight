using System.Text;
using AutoKeyLight.Core;

namespace AutoKeyLight.Core.Tests
{
    public class FeatureTests
    {
        private static readonly DateTime Start = new DateTime(2026, 3, 2, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void OldSettingsJsonKeepsDefaultsForNewFields()
        {
            string json = """
                {
                  "lights": [ { "address": "192.168.1.5", "description": "Left" } ],
                  "cameraOffDelayMs": 0,
                  "theme": "Dark",
                  "automaticUpdates": false,
                  "lightMode": "On"
                }
                """;

            AppUserSettings? settings = SettingsJson.Deserialize(json);
            Assert.NotNull(settings);
            LightRecord light = Assert.Single(settings!.Lights);
            Assert.Equal("192.168.1.5", light.Address);
            Assert.Equal("Left", light.Description);
            Assert.Equal("", light.Group);
            Assert.Equal("", light.ProductName);
            Assert.Equal("", light.Serial);
            Assert.False(settings.QuietHoursEnabled);
            Assert.Equal("22:00", settings.QuietStart);
            Assert.Equal("07:00", settings.QuietEnd);
            Assert.Equal(QuietHours.AllDays, settings.QuietDays);
            Assert.Equal("All", settings.CameraAppMode);
            Assert.Empty(settings.KnownCameraApps);
            Assert.Empty(settings.SelectedCameraApps);
            Assert.True(settings.AllGroupsFollowCamera);
            Assert.Empty(settings.CameraGroups);
            Assert.True(settings.NotifyLightPresence);
            Assert.False(settings.NotifyOnUpdate);
            Assert.Equal(0, settings.CameraOffDelayMs);
            Assert.Equal("Dark", settings.Theme);
        }

        [Fact]
        public void EmptyObjectStillLoads()
        {
            AppUserSettings? settings = SettingsJson.Deserialize("{}");
            Assert.NotNull(settings);
            Assert.Empty(settings!.Lights);
            Assert.Equal("All", settings.CameraAppMode);
            Assert.True(settings.NotifyLightPresence);
            Assert.Equal(LightTiming.DefaultOffDelayMilliseconds, settings.CameraOffDelayMs);
        }

        [Fact]
        public void NewSettingsRoundTrip()
        {
            AppUserSettings settings = new AppUserSettings
            {
                QuietHoursEnabled = true,
                QuietStart = "21:30",
                QuietEnd = "06:15",
                QuietDays = "Mon,Tue",
                CameraAppMode = "Except",
                AllGroupsFollowCamera = false,
                NotifyLightPresence = false,
                NotifyOnUpdate = true
            };
            settings.Lights.Add(new LightRecord("10.0.0.4", "Desk", "Left", "Elgato Key Light", "ABC123"));
            settings.KnownCameraApps.Add("Zoom.exe");
            settings.SelectedCameraApps.Add("Teams.exe");
            settings.CameraGroups.Add("Left");

            AppUserSettings? again = SettingsJson.Deserialize(SettingsJson.Serialize(settings));
            Assert.NotNull(again);
            Assert.True(again!.QuietHoursEnabled);
            Assert.Equal("21:30", again.QuietStart);
            Assert.Equal("06:15", again.QuietEnd);
            Assert.Equal("Mon,Tue", again.QuietDays);
            Assert.Equal("Except", again.CameraAppMode);
            Assert.Equal(new[] { "Zoom.exe" }, again.KnownCameraApps);
            Assert.Equal(new[] { "Teams.exe" }, again.SelectedCameraApps);
            Assert.False(again.AllGroupsFollowCamera);
            Assert.Equal(new[] { "Left" }, again.CameraGroups);
            Assert.False(again.NotifyLightPresence);
            Assert.True(again.NotifyOnUpdate);
            LightRecord light = Assert.Single(again.Lights);
            Assert.Equal("Left", light.Group);
            Assert.Equal("Elgato Key Light", light.ProductName);
            Assert.Equal("ABC123", light.Serial);
        }

        [Fact]
        public void QuietHoursCoverOvernightSpansAndSkipOtherDays()
        {
            AppUserSettings settings = new AppUserSettings
            {
                QuietHoursEnabled = true,
                QuietStart = "22:00",
                QuietEnd = "07:00",
                QuietDays = "Mon,Tue"
            };

            Assert.True(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 2, 23, 0, 0)));
            Assert.True(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 3, 6, 59, 0)));
            Assert.False(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 3, 7, 0, 0)));
            Assert.False(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 2, 21, 59, 0)));
            Assert.False(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 4, 23, 0, 0)));

            settings.QuietHoursEnabled = false;
            Assert.False(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 2, 23, 0, 0)));
        }

        [Fact]
        public void EqualQuietTimesCoverTheSelectedDays()
        {
            AppUserSettings settings = new AppUserSettings
            {
                QuietHoursEnabled = true,
                QuietStart = "08:00",
                QuietEnd = "08:00",
                QuietDays = "Sun"
            };
            Assert.True(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 1, 8, 0, 0)));
            Assert.False(QuietHours.IsQuiet(settings, new DateTime(2026, 3, 2, 8, 0, 0)));
            Assert.Equal("-", QuietHours.FormatDays(new bool[7]));
            Assert.DoesNotContain(QuietHours.ParseDays("-"), day => day);
        }

        [Fact]
        public void QuietHoursDoNotTurnLightsOnButManualOnStillWorks()
        {
            LightModeController controller = new LightModeController(Start);
            Assert.Null(controller.OnCameraSample(true, Start.AddSeconds(2), TimeSpan.Zero, TimeSpan.Zero, blockAutomaticOn: true));
            Assert.Equal(LightPower.Off, controller.OnCameraSample(false, Start.AddSeconds(3), TimeSpan.Zero, TimeSpan.Zero, blockAutomaticOn: true)?.Power);
            Assert.Equal(LightPower.On, controller.SetMode(LightMode.On, cameraOn: false, Start.AddSeconds(4))?.Power);
            Assert.Equal(LightPower.On, controller.OnCameraSample(false, Start.AddSeconds(5), TimeSpan.Zero, TimeSpan.Zero, blockAutomaticOn: true)?.Power);
        }

        [Fact]
        public void FriendlyCameraNamesComeFromPathsAndPackageKeys()
        {
            Assert.Equal("Zoom.exe", CameraAppNames.Friendly(@"C:#Program Files#Zoom#bin#Zoom.exe"));
            Assert.Equal("Zoom.exe", CameraAppNames.Friendly(@"C:\Program Files\Zoom\bin\Zoom.exe"));
            Assert.Equal("chrome.exe", CameraAppNames.Friendly("chrome.exe"));
            Assert.Equal("WindowsCamera", CameraAppNames.Friendly("Microsoft.WindowsCamera_8wekyb3d8bbwe"));
            Assert.Equal(CameraAppNames.Unknown, CameraAppNames.Friendly(CameraAppNames.Unknown));
        }

        [Fact]
        public void CameraRulesDefaultToEveryAppAndCanIncludeOrSkip()
        {
            string[] active = { "Zoom.exe", "Teams.exe" };
            Assert.True(CameraAppPolicy.CountsAsOn(CameraAppFilter.All, Array.Empty<string>(), active));
            Assert.False(CameraAppPolicy.CountsAsOn(CameraAppFilter.All, Array.Empty<string>(), Array.Empty<string>()));
            Assert.True(CameraAppPolicy.CountsAsOn(CameraAppFilter.Only, new[] { "zoom.exe" }, active));
            Assert.False(CameraAppPolicy.CountsAsOn(CameraAppFilter.Only, new[] { "chrome.exe" }, active));
            Assert.False(CameraAppPolicy.CountsAsOn(CameraAppFilter.Except, new[] { "Zoom.exe", "Teams.exe" }, active));
            Assert.True(CameraAppPolicy.CountsAsOn(CameraAppFilter.Except, new[] { "Zoom.exe" }, active));
            Assert.True(CameraAppPolicy.CountsAsOn(CameraAppFilter.Except, Array.Empty<string>(), new[] { CameraAppNames.Unknown }));
            Assert.False(CameraAppPolicy.CountsAsOn(CameraAppFilter.Only, Array.Empty<string>(), new[] { CameraAppNames.Unknown }));
        }

        [Fact]
        public void AutomaticSwitchingCanBeLimitedToGroups()
        {
            LightRecord[] lights =
            {
                new LightRecord("10.0.0.1", "Left", "Desk"),
                new LightRecord("10.0.0.2", "Right", "Background"),
                new LightRecord("10.0.0.3", "Spare", "")
            };

            Assert.Equal(new[] { "10.0.0.1", "10.0.0.2", "10.0.0.3" }, LightGrouping.AutomaticAddresses(lights, true, Array.Empty<string>()));
            Assert.Equal(new[] { "10.0.0.1" }, LightGrouping.AutomaticAddresses(lights, false, new[] { "desk" }));
            Assert.Equal(new[] { "Desk", "Background" }, LightGrouping.Names(lights));
        }

        [Fact]
        public void BrightnessCommandTurnsTheLightOnAtThatLevel()
        {
            Assert.Equal("{\"numberOfLights\": 1, \"lights\": [{\"on\": 1, \"brightness\": 25}]}", LightEndpoint.BrightnessBody(25));
            Assert.Equal("{\"numberOfLights\": 1, \"lights\": [{\"on\": 1, \"brightness\": 0}]}", LightEndpoint.BrightnessBody(-4));
            Assert.Equal("{\"numberOfLights\": 1, \"lights\": [{\"on\": 1, \"brightness\": 100}]}", LightEndpoint.BrightnessBody(140));
        }

        [Fact]
        public void DiscoveryQueryNamesTheElgatoService()
        {
            byte[] query = MdnsCodec.CreateQuery(0, unicastResponse: true);
            string text = Encoding.ASCII.GetString(query);
            Assert.Contains("_elg", text);
            Assert.Contains("_tcp", text);
            Assert.Contains("local", text);
            Assert.Equal(0x80, query[^2]);
        }

        [Fact]
        public void DiscoveryPacketBecomesALightAndSkipsOnesAlreadyAdded()
        {
            byte[] packet = SamplePacket(compressed: false);
            DiscoveredLight found = Assert.Single(ElgatoDiscovery.FromPackets(new[] { packet }));
            Assert.Equal("Elgato Key Light", found.Name);
            Assert.Equal("Elgato Key Light", found.ProductName);
            Assert.Equal("192.168.1.50", found.Address);
            Assert.Equal(9123, found.Port);
            Assert.Equal("ABC123", found.Serial);

            Assert.Empty(ElgatoDiscovery.ExceptAdded(new[] { found }, new[] { "192.168.1.50" }));
            Assert.Empty(ElgatoDiscovery.ExceptAdded(new[] { found }, new[] { "192.168.1.50:9123" }));
            Assert.Single(ElgatoDiscovery.ExceptAdded(new[] { found }, new[] { "192.168.1.9" }));
        }

        [Fact]
        public void CompressedDiscoveryNamesStillResolve()
        {
            DiscoveredLight found = Assert.Single(ElgatoDiscovery.FromPackets(new[] { SamplePacket(compressed: true) }));
            Assert.Equal("192.168.1.50", found.Address);
            Assert.Equal("ABC123", found.Serial);
        }

        [Fact]
        public void RediscoveryUpdatesASerialMatchAndLeavesAmbiguousNames()
        {
            LightRecord saved = new LightRecord("192.168.1.9", "Left", "Desk", "Elgato Key Light", "ABC123");
            DiscoveredLight moved = new DiscoveredLight("Elgato Key Light", "Elgato Key Light", "192.168.1.50", 9123, "ABC123");
            Assert.Equal("192.168.1.50", ElgatoDiscovery.ReplacementAddress(saved, new[] { saved }, new[] { moved }));
            Assert.Null(ElgatoDiscovery.ReplacementAddress(saved, new[] { saved }, new[] { moved with { Address = "192.168.1.9" } }));

            LightRecord other = new LightRecord("192.168.1.50", "Taken");
            Assert.Null(ElgatoDiscovery.ReplacementAddress(saved, new[] { saved, other }, new[] { moved }));

            LightRecord named = new LightRecord("10.0.0.2", "Desk", "", "Elgato Key Light", "");
            DiscoveredLight[] twins =
            {
                new DiscoveredLight("Elgato Key Light", "Elgato Key Light", "10.0.0.3", 9123, ""),
                new DiscoveredLight("Elgato Key Light", "Elgato Key Light", "10.0.0.4", 9123, "")
            };
            Assert.Null(ElgatoDiscovery.Match(named, twins));
            Assert.Equal("10.0.0.8", ElgatoDiscovery.Match(named, new[] { twins[0] with { Address = "10.0.0.8" } })!.Address);
        }

        [Fact]
        public void PresenceNoticesWaitAndIgnoreFlapping()
        {
            PresenceTracker tracker = new PresenceTracker(TimeSpan.FromSeconds(20));
            DateTime t = Start;
            Assert.Null(tracker.Observe("192.168.1.5", true, t));
            Assert.Null(tracker.Observe("192.168.1.5", false, t.AddSeconds(1)));
            Assert.Null(tracker.Observe("192.168.1.5", true, t.AddSeconds(5)));
            Assert.Null(tracker.Observe("192.168.1.5", false, t.AddSeconds(6)));
            PresenceNotice? offline = tracker.Observe("192.168.1.5", false, t.AddSeconds(26));
            Assert.NotNull(offline);
            Assert.False(offline.Value.Online);
            Assert.Null(tracker.Observe("192.168.1.5", false, t.AddSeconds(40)));
            Assert.Null(tracker.Observe("192.168.1.5", true, t.AddSeconds(41)));
            Assert.Null(tracker.Observe("192.168.1.5", false, t.AddSeconds(50)));
            PresenceNotice? online = tracker.Observe("192.168.1.5", true, t.AddSeconds(70));
            Assert.Null(online);
            online = tracker.Observe("192.168.1.5", true, t.AddSeconds(90));
            Assert.NotNull(online);
            Assert.True(online.Value.Online);
            Assert.Equal("Light offline", PresenceText.Title(false));
            Assert.Equal("Left light did not respond.", PresenceText.Body("Left light", false));
        }

        [Fact]
        public void TrayTipMentionsQuietHoursAndStaysShort()
        {
            string tip = QuietHours.TrayTip("Auto Key Light — Auto", true);
            Assert.Contains("quiet hours", tip);
            Assert.True(tip.Length <= 63);
            AppUserSettings settings = new AppUserSettings
            {
                QuietHoursEnabled = true,
                QuietStart = "22:00",
                QuietEnd = "07:00",
                QuietDays = QuietHours.AllDays
            };
            Assert.Equal("Quiet hours — on until 07:00", QuietHours.MenuLabel(settings, new DateTime(2026, 3, 2, 23, 30, 0)));
            Assert.Equal("Quiet hours off", QuietHours.MenuLabel(new AppUserSettings(), new DateTime(2026, 3, 2, 23, 30, 0)));
        }

        private static byte[] SamplePacket(bool compressed)
        {
            Packet packet = new Packet();
            packet.U16(0);
            packet.U16(0x8400);
            packet.U16(1);
            packet.U16(3);
            packet.U16(0);
            packet.U16(0);
            int host = packet.WriteName("elgato.local");
            packet.U16(MdnsCodec.TypeA);
            packet.U16(1);

            packet.WriteName("Elgato Key Light._elg._tcp.local");
            packet.U16(MdnsCodec.TypeSrv);
            packet.U16(1);
            packet.U32(120);
            int srvData = packet.Reserve(2);
            int srvStart = packet.Position;
            packet.U16(0);
            packet.U16(0);
            packet.U16(9123);
            if (compressed)
                packet.Pointer(host);
            else
                packet.WriteName("elgato.local");
            packet.PatchU16(srvData, packet.Position - srvStart);

            packet.WriteName("Elgato Key Light._elg._tcp.local");
            packet.U16(MdnsCodec.TypeTxt);
            packet.U16(1);
            packet.U32(120);
            byte[] txt = Encoding.UTF8.GetBytes("md=Elgato Key Light");
            byte[] serial = Encoding.UTF8.GetBytes("id=ABC123");
            packet.U16(txt.Length + 1 + serial.Length + 1);
            packet.Byte((byte)txt.Length);
            packet.Bytes(txt);
            packet.Byte((byte)serial.Length);
            packet.Bytes(serial);

            packet.Pointer(host);
            packet.U16(MdnsCodec.TypeA);
            packet.U16(1);
            packet.U32(120);
            packet.U16(4);
            packet.Bytes(new byte[] { 192, 168, 1, 50 });
            return packet.ToArray();
        }

        private sealed class Packet
        {
            private readonly MemoryStream stream = new MemoryStream();

            public int Position => (int)stream.Length;

            public void Byte(byte value) => stream.WriteByte(value);

            public void Bytes(byte[] value) => stream.Write(value, 0, value.Length);

            public void U16(int value)
            {
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)value);
            }

            public void U32(int value)
            {
                U16((value >> 16) & 0xFFFF);
                U16(value & 0xFFFF);
            }

            public int Reserve(int bytes)
            {
                int start = Position;
                for (int i = 0; i < bytes; i++)
                    stream.WriteByte(0);
                return start;
            }

            public void PatchU16(int offset, int value)
            {
                long here = stream.Position;
                stream.Position = offset;
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)value);
                stream.Position = here;
            }

            public int WriteName(string dotted)
            {
                int start = Position;
                foreach (string label in dotted.Split('.'))
                {
                    Byte((byte)label.Length);
                    Bytes(Encoding.ASCII.GetBytes(label));
                }

                Byte(0);
                return start;
            }

            public void Pointer(int offset) => U16(0xC000 | offset);

            public byte[] ToArray() => stream.ToArray();
        }
    }
}
