using CamOnKeyLight.Core;

namespace CamOnKeyLight.Core.Tests
{
    public class LightStatusTests
    {
        [Fact]
        public void NewerFailureClearsAPreviousOnStatus()
        {
            LightStatusBoard board = Board("10.0.0.8");
            int first = board.BeginBackgroundRefresh("10.0.0.8");
            Assert.True(board.TryComplete(firstAddress(board), first, LightLinkState.On, LightDiagnostics.OnDetail("10.0.0.8")));
            int second = board.BeginBackgroundRefresh("10.0.0.8");
            Assert.True(board.TryComplete("10.0.0.8", second, LightLinkState.Unreachable, LightDiagnostics.Unreachable("10.0.0.8")));

            LightRow row = Assert.Single(board.Rows);
            Assert.Equal(LightLinkState.Unreachable, row.State);
            Assert.Equal("Unreachable", row.StatusText);
            Assert.NotEqual(LightSummaryKind.AllOn, board.Kind);
            Assert.DoesNotContain("ON", board.Summary);
            Assert.Equal(LightDiagnostics.Unreachable("10.0.0.8"), board.FirstProblem);
        }

        [Fact]
        public void StaleFailureDoesNotOverwriteANewerSuccess()
        {
            LightStatusBoard board = Board("10.0.0.8");
            int first = board.BeginUserRetry("10.0.0.8");
            int second = board.BeginUserRetry("10.0.0.8");
            Assert.True(board.TryComplete("10.0.0.8", second, LightLinkState.On, LightDiagnostics.OnDetail("10.0.0.8")));
            Assert.False(board.TryComplete("10.0.0.8", first, LightLinkState.Unreachable, LightDiagnostics.Unreachable("10.0.0.8")));
            Assert.Equal(LightLinkState.On, Assert.Single(board.Rows).State);
            Assert.Equal(LightSummaryKind.AllOn, board.Kind);
        }

        [Fact]
        public void StaleSuccessDoesNotHideANewerFailure()
        {
            LightStatusBoard board = Board("10.0.0.8");
            int first = board.BeginBackgroundRefresh("10.0.0.8");
            int second = board.BeginBackgroundRefresh("10.0.0.8");
            Assert.True(board.TryComplete("10.0.0.8", second, LightLinkState.Unreachable, LightDiagnostics.Unreachable("10.0.0.8")));
            Assert.False(board.TryComplete("10.0.0.8", first, LightLinkState.On, LightDiagnostics.OnDetail("10.0.0.8")));
            Assert.Equal(LightLinkState.Unreachable, Assert.Single(board.Rows).State);
            Assert.Equal("Lights are unreachable", board.Summary);
        }

        [Fact]
        public void UserRetryShowsCheckingUntilTheNewCallFinishes()
        {
            LightStatusBoard board = Board("10.0.0.8");
            board.TryComplete("10.0.0.8", board.BeginBackgroundRefresh("10.0.0.8"), LightLinkState.On, "on");
            board.BeginUserRetry("10.0.0.8");
            Assert.Equal(LightLinkState.Checking, Assert.Single(board.Rows).State);
            Assert.Equal("Checking…", Assert.Single(board.Rows).StatusText);
        }

        [Fact]
        public void BackgroundRefreshDoesNotFlashCheckingOverAKnownStatus()
        {
            LightStatusBoard board = Board("10.0.0.8");
            board.TryComplete("10.0.0.8", board.BeginBackgroundRefresh("10.0.0.8"), LightLinkState.Off, "off");
            board.BeginBackgroundRefresh("10.0.0.8");
            Assert.Equal(LightLinkState.Off, Assert.Single(board.Rows).State);
        }

        [Fact]
        public void MixedAndProblemSummariesStaySpecific()
        {
            LightStatusBoard board = new LightStatusBoard();
            board.SyncAddresses(new[] { "10.0.0.2", "10.0.0.3", "10.0.0.4" });
            board.TryComplete("10.0.0.2", board.BeginBackgroundRefresh("10.0.0.2"), LightLinkState.On, "on");
            board.TryComplete("10.0.0.3", board.BeginBackgroundRefresh("10.0.0.3"), LightLinkState.Off, "off");
            Assert.Equal("1 on, 1 off", board.Summary);
            board.TryComplete("10.0.0.4", board.BeginBackgroundRefresh("10.0.0.4"), LightLinkState.UnexpectedReply, LightDiagnostics.Unexpected("10.0.0.4"));
            Assert.Equal(LightSummaryKind.Problems, board.Kind);
            Assert.Contains("unreachable", board.Summary);
            Assert.DoesNotContain("Lights are ON", board.Summary);
        }

        [Fact]
        public void DiagnosticsDoNotGiveNetworkAdvice()
        {
            string[] messages =
            {
                LightDiagnostics.Unreachable("10.0.0.5"),
                LightDiagnostics.Unexpected("10.0.0.5"),
                LightDiagnostics.HttpFailure("10.0.0.5", 500),
                LightDiagnostics.OnDetail("10.0.0.5"),
                LightDiagnostics.OffDetail("10.0.0.5")
            };

            foreach (string message in messages)
            {
                Assert.DoesNotContain("VPN", message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("route", message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("firewall", message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("proxy", message, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static LightStatusBoard Board(string address)
        {
            LightStatusBoard board = new LightStatusBoard();
            board.SyncAddresses(new[] { address });
            return board;
        }

        private static string firstAddress(LightStatusBoard board)
        {
            return board.Rows[0].Address;
        }
    }
}
