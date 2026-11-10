using CamOnKeyLight.Core;

namespace CamOnKeyLight.Core.Tests
{
    public class LightAutomationTests
    {
        private static readonly DateTime Start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void DefaultOffDelayIsShortEnoughToCoverAnAppSwitch()
        {
            Assert.Equal(1500m, LightTiming.DefaultOffDelayMilliseconds);
            Assert.InRange(LightTiming.DefaultOffDelay.TotalMilliseconds, 800, 2500);
            Assert.InRange(LightTiming.LightRequestTimeout.TotalSeconds, 2, 10);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("nope")]
        [InlineData("1")]
        public void InvalidModeFallsBackToAuto(string? value)
        {
            Assert.Equal(LightMode.Auto, LightModePreference.Parse(value));
        }

        [Theory]
        [InlineData("on", LightMode.On)]
        [InlineData("Off", LightMode.Off)]
        [InlineData("Auto", LightMode.Auto)]
        public void ModeNamesRoundTrip(string value, LightMode expected)
        {
            Assert.Equal(expected, LightModePreference.Parse(value));
        }

        [Fact]
        public void StartupAutoKeepsTheExistingDelay()
        {
            LightModeController controller = new LightModeController(Start);
            Assert.Null(controller.ApplyStartupMode(LightMode.Auto, cameraOn: false, Start));
            Assert.Null(controller.OnCameraSample(false, Start.AddMilliseconds(200), TimeSpan.Zero, LightTiming.DefaultOffDelay));
            Assert.Null(controller.OnCameraSample(false, Start.AddSeconds(1), TimeSpan.Zero, LightTiming.DefaultOffDelay));
            LightCommand? command = controller.OnCameraSample(false, Start.AddMilliseconds(1500), TimeSpan.Zero, LightTiming.DefaultOffDelay);
            Assert.Equal(LightPower.Off, command?.Power);
        }

        [Fact]
        public void StartupManualModesApplyImmediately()
        {
            LightModeController on = new LightModeController(Start);
            Assert.Equal(LightPower.On, on.ApplyStartupMode(LightMode.On, cameraOn: false, Start)?.Power);

            LightModeController off = new LightModeController(Start);
            Assert.Equal(LightPower.Off, off.ApplyStartupMode(LightMode.Off, cameraOn: true, Start)?.Power);
            Assert.Equal(LightPower.Off, off.OnCameraSample(true, Start.AddMilliseconds(10), TimeSpan.Zero, TimeSpan.FromSeconds(5))?.Power);
            Assert.Equal(LightMode.Off, off.Mode);
        }

        [Fact]
        public void SwitchingCameraAppsWithinOffDelayDoesNotTurnLightsOff()
        {
            LightModeController controller = new LightModeController(Start);
            DateTime on = Start.AddSeconds(2);
            Assert.Equal(LightPower.On, controller.OnCameraSample(true, on, TimeSpan.Zero, TimeSpan.FromMilliseconds(1500))?.Power);

            DateTime dropped = on.AddSeconds(1);
            Assert.Null(controller.OnCameraSample(false, dropped, TimeSpan.Zero, TimeSpan.FromMilliseconds(1500)));
            Assert.Null(controller.OnCameraSample(false, dropped.AddMilliseconds(400), TimeSpan.Zero, TimeSpan.FromMilliseconds(1500)));

            LightCommand? resumed = controller.OnCameraSample(true, dropped.AddMilliseconds(500), TimeSpan.Zero, TimeSpan.FromMilliseconds(1500));
            Assert.NotEqual(LightPower.Off, resumed?.Power);
            Assert.Equal(LightPower.On, resumed?.Power);
        }

        [Fact]
        public void HoldingCameraOffDoesNotResetTheOffTimer()
        {
            LightModeController controller = new LightModeController(Start);
            DateTime edge = Start.AddSeconds(2);
            controller.OnCameraSample(true, edge, TimeSpan.Zero, TimeSpan.FromSeconds(2));
            Assert.Null(controller.OnCameraSample(false, edge.AddSeconds(1), TimeSpan.Zero, TimeSpan.FromSeconds(2)));
            Assert.Null(controller.OnCameraSample(false, edge.AddSeconds(2), TimeSpan.Zero, TimeSpan.FromSeconds(2)));
            Assert.Equal(LightPower.Off, controller.OnCameraSample(false, edge.AddSeconds(3), TimeSpan.Zero, TimeSpan.FromSeconds(2))?.Power);
        }

        [Fact]
        public void CameraReturningResetsTheNextOffTimer()
        {
            LightModeController controller = new LightModeController(Start);
            TimeSpan delay = TimeSpan.FromMilliseconds(1500);
            DateTime t0 = Start.AddSeconds(5);
            controller.OnCameraSample(true, t0, TimeSpan.Zero, delay);
            Assert.Null(controller.OnCameraSample(false, t0.AddSeconds(1), TimeSpan.Zero, delay));
            controller.OnCameraSample(true, t0.AddSeconds(1.4), TimeSpan.Zero, delay);
            DateTime secondDrop = t0.AddSeconds(2);
            Assert.Null(controller.OnCameraSample(false, secondDrop, TimeSpan.Zero, delay));
            Assert.Null(controller.OnCameraSample(false, secondDrop.AddSeconds(1), TimeSpan.Zero, delay));
            Assert.Equal(LightPower.Off, controller.OnCameraSample(false, secondDrop.AddMilliseconds(1500), TimeSpan.Zero, delay)?.Power);
        }

        [Fact]
        public void CameraOnExactlyAtTheOffDeadlineDoesNotTurnLightsOff()
        {
            LightModeController controller = new LightModeController(Start);
            TimeSpan delay = TimeSpan.FromSeconds(2);
            DateTime edge = Start.AddSeconds(5);
            controller.OnCameraSample(true, edge, TimeSpan.Zero, delay);
            DateTime offAt = edge.AddSeconds(1);
            Assert.Null(controller.OnCameraSample(false, offAt, TimeSpan.Zero, delay));
            LightCommand? command = controller.OnCameraSample(true, offAt.AddSeconds(2), TimeSpan.Zero, delay);
            Assert.NotEqual(LightPower.Off, command?.Power);
        }

        [Fact]
        public void ManualOffIsImmediateAndBackgroundSamplesCannotUndoIt()
        {
            LightModeController controller = new LightModeController(Start);
            DateTime t = Start.AddSeconds(3);
            controller.SetMode(LightMode.Auto, true, t);
            controller.OnCameraSample(true, t, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            Assert.Null(controller.OnCameraSample(false, t.AddSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)));

            LightCommand? manual = controller.SetMode(LightMode.Off, cameraOn: true, t.AddSeconds(1.2));
            Assert.NotNull(manual);
            Assert.Equal(LightPower.Off, manual.Value.Power);
            Assert.True(controller.IsCurrent(manual.Value.Sequence));

            LightCommand? sample = controller.OnCameraSample(true, t.AddSeconds(1.3), TimeSpan.Zero, TimeSpan.FromSeconds(5));
            Assert.NotNull(sample);
            Assert.Equal(LightPower.Off, sample.Value.Power);
            Assert.False(controller.IsCurrent(manual.Value.Sequence));
            Assert.Equal(LightMode.Off, controller.Mode);
        }

        [Fact]
        public void ManualOnStaysOnWhileTheCameraTurnsOff()
        {
            LightModeController controller = new LightModeController(Start);
            Assert.Equal(LightPower.On, controller.SetMode(LightMode.On, cameraOn: false, Start)?.Power);
            Assert.Equal(LightPower.On, controller.OnCameraSample(false, Start.AddSeconds(2), TimeSpan.Zero, TimeSpan.Zero)?.Power);
            Assert.Equal(LightPower.On, controller.OnCameraSample(true, Start.AddSeconds(3), TimeSpan.Zero, TimeSpan.Zero)?.Power);
            Assert.Equal(LightMode.On, controller.Mode);
        }

        [Fact]
        public void ReturningToAutoMatchesTheCurrentCameraImmediately()
        {
            LightModeController controller = new LightModeController(Start);
            controller.SetMode(LightMode.Off, cameraOn: true, Start);
            Assert.Equal(LightPower.On, controller.SetMode(LightMode.Auto, cameraOn: true, Start.AddSeconds(4))?.Power);

            Assert.Null(controller.OnCameraSample(true, Start.AddSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)));
            Assert.Null(controller.OnCameraSample(false, Start.AddSeconds(6), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)));
            Assert.Equal(LightPower.Off, controller.OnCameraSample(false, Start.AddSeconds(11), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))?.Power);
        }

        [Fact]
        public void ReturningToAutoWhileTheCameraIsOffTurnsLightsOffImmediately()
        {
            LightModeController controller = new LightModeController(Start);
            controller.SetMode(LightMode.On, cameraOn: false, Start);
            Assert.Equal(LightPower.Off, controller.SetMode(LightMode.Auto, cameraOn: false, Start.AddSeconds(2))?.Power);
        }

        [Fact]
        public void StayingOnAutoDoesNotForceACommand()
        {
            LightModeController controller = new LightModeController(Start);
            Assert.Null(controller.SetMode(LightMode.Auto, cameraOn: true, Start.AddSeconds(2)));
        }

        [Fact]
        public void SnapshotMarksExactlyOneMode()
        {
            foreach (LightMode mode in new[] { LightMode.Auto, LightMode.On, LightMode.Off })
            {
                LightControlSnapshot snapshot = LightModeController.Describe(mode);
                int checks = (snapshot.AutoChecked ? 1 : 0) + (snapshot.OnChecked ? 1 : 0) + (snapshot.OffChecked ? 1 : 0);
                Assert.Equal(1, checks);
                Assert.Equal(mode, snapshot.Mode);
                Assert.Contains(mode.ToString(), snapshot.Title);
            }
        }

        [Fact]
        public void EveryLightGetsTheSamePower()
        {
            IReadOnlyList<LightTarget> targets = LightEndpoint.ForEachLight(new[] { "10.0.0.2", "10.0.0.3:9000", " " }, LightPower.Off);
            Assert.Equal(2, targets.Count);
            Assert.All(targets, target => Assert.Equal(LightPower.Off, target.Power));
            Assert.Equal("10.0.0.2:9123", targets[0].Host);
            Assert.Equal("10.0.0.3:9000", targets[1].Host);
            Assert.Equal("http://10.0.0.2:9123/elgato/lights", LightEndpoint.LightsUrl("10.0.0.2"));
            Assert.Equal("{\"numberOfLights\": 1, \"lights\": [{\"on\": 0}]}", LightEndpoint.PutBody(false));
            Assert.Equal("{\"numberOfLights\": 1, \"lights\": [{\"on\": 1}]}", LightEndpoint.PutBody(true));
        }

        [Theory]
        [InlineData("{\"numberOfLights\":1,\"lights\":[{\"on\":1}]}", true, true)]
        [InlineData("{\"lights\":[{\"on\":0}]}", true, false)]
        [InlineData("{\"lights\":[]}", false, false)]
        [InlineData("{\"lights\":[{\"on\":2}]}", false, false)]
        [InlineData("not json", false, false)]
        public void LightReplyIsOnlyOnOrOff(string json, bool parsed, bool on)
        {
            Assert.Equal(parsed, LightResponse.TryReadPower(json, out bool value));
            Assert.Equal(on, value);
        }
    }
}
