using System.Diagnostics;
using System.Text;
using AutoKeyLight.Core;
using Microsoft.Win32;

namespace AutoKeyLight
{
    public partial class MainForm : Form
    {
        private const int WmHotkey = 0x0312;

        private readonly HttpClient httpClient = new HttpClient();
        private readonly Icon trayIconLit;
        private readonly Icon trayIconUnlit;
        private readonly LightModeController controller = new LightModeController(DateTime.UtcNow);
        private readonly LightStatusBoard board = new LightStatusBoard();
        private readonly PresenceTracker presence = new PresenceTracker();
        private readonly HashSet<string> seenCameraApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CancellationTokenSource> lightCalls = new Dictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);
        private readonly HotkeyBook hotkeys = new HotkeyBook();
        private readonly Win32HotkeyRegistrar registrar;
        private readonly bool startInBackground;
        private readonly System.Windows.Forms.Timer updateTimer;
        private CameraActivityMonitor? cameraActivityMonitor = CameraActivityMonitor.TryCreate();
        private UpdateCoordinator updateCoordinator = null!;
        private decimal cameraOnDelay;
        private decimal cameraOffDelay;
        private bool settingsReady;
        private bool applyingMode;
        private bool applyingStartup;
        private AppUserSettings userSettings = AppUserSettings.CreateDefault();
        private bool capturingShortcut;
        private bool hotkeysBound;
        private bool updateProblem;
        private ThemePalette? palette;
        private EffectiveTheme effectiveTheme = EffectiveTheme.Light;
        private int discoveryBusy;

        public MainForm(string[]? args = null)
        {
            startInBackground = args != null && args.Contains("--background");
            registrar = new Win32HotkeyRegistrar(() => IsHandleCreated ? Handle : IntPtr.Zero);
            InitializeComponent();
            trayIconLit = Icon.FromHandle(Properties.Resources.TrayIconLit.GetHicon());
            trayIconUnlit = Icon.FromHandle(Properties.Resources.TrayIconUnlit.GetHicon());
            updateTimer = new System.Windows.Forms.Timer(components);
            updateTimer.Tick += updateTimer_Tick;
            updateCoordinator = new UpdateCoordinator(
                CurrentVersion(),
                AppContext.BaseDirectory,
                IsWebcamOn,
                ShowUpdate);
            updateCoordinator.InstallLaunched += () =>
            {
                niTray.Visible = false;
                Application.Exit();
            };
            ToolTip tips = new ToolTip(components);
            tips.SetToolTip(numUpDownOff, "Automatic mode only. After the camera turns off, wait this long before turning the lights off. If the camera comes back during the wait, the lights stay on. Manual Off is immediate. New installs use 1500 ms; a value you already saved is kept.");
            tips.SetToolTip(btnRetry, "Check the lights again and show whether each one responded.");
            tips.SetToolTip(btnFindLights, "Search this network for Elgato lights and add the ones that are not already listed.");
            cmsTray.Opening += cmsTray_Opening;
        }

        public static Version CurrentVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            TitleBarTheme.Apply(Handle, effectiveTheme == EffectiveTheme.Dark);
            if (settingsReady)
                BindHotkeys();
        }

        private void BindHotkeys()
        {
            if (hotkeysBound || !IsHandleCreated)
                return;

            hotkeysBound = true;
            IReadOnlyList<string> errors = hotkeys.BindSaved(
                userSettings.HotkeyAuto,
                userSettings.HotkeyOn,
                userSettings.HotkeyOff,
                registrar);
            RefreshShortcutLabels();
            if (errors.Count > 0)
                ShowUpdate(new UpdateUi(string.Join(" ", errors), true, btnInstallUpdate.Enabled, true));
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && !capturingShortcut && HotkeyIds.TryMode(m.WParam.ToInt32(), out LightMode mode))
            {
                SetLightMode(mode, true);
                return;
            }

            base.WndProc(ref m);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            userSettings = UserSettingsFile.Load();
            foreach (string name in userSettings.KnownCameraApps)
                seenCameraApps.Add(name);
            ReloadLights();
            ImportElgatoLights();
            board.SyncAddresses(Addresses());
            ShowLightRows();
            SizeLightColumns();

            numUpDown.Value = cameraOnDelay = ClampDelay(userSettings.CameraOnDelayMs, numUpDown.Maximum);
            numUpDownOff.Value = cameraOffDelay = ClampDelay(userSettings.CameraOffDelayMs, numUpDownOff.Maximum);

            cmbTheme.SelectedIndex = ThemeIndex(ThemePreferenceParser.Parse(userSettings.Theme));
            chkAutomaticUpdates.Checked = userSettings.AutomaticUpdates;
            applyingStartup = true;
            chkStartWithWindows.Checked = StartupRegistry.RepairCurrentProcess();
            applyingStartup = false;
            LightMode persisted = LightModePreference.Parse(userSettings.LightMode);
            rbLightAuto.Checked = persisted == LightMode.Auto;
            rbLightOn.Checked = persisted == LightMode.On;
            rbLightOff.Checked = persisted == LightMode.Off;
            settingsReady = true;

            ApplyTheme(ThemePreferenceParser.Parse(userSettings.Theme));
            LightCommand? startup = controller.ApplyStartupMode(persisted, IsWebcamOn(), DateTime.UtcNow);
            if (startup != null)
                Dispatch(startup.Value, automatic: false);
            AnnounceInstalledUpdate();

            ShowCamera(IsWebcamOn());
            UpdateModeVisuals();
            RefreshAll(userInitiated: true);
            Icon = Properties.Resources.Icon;
            ShowPendingUpdateError();
            CleanupStaging();

            if (chkAutomaticUpdates.Checked)
            {
                updateTimer.Interval = 5000;
                updateTimer.Start();
            }

            SystemEvents.UserPreferenceChanged += SystemThemeChanged;
            BindHotkeys();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (chkStartWithWindows.Checked || startInBackground)
                Hide();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                niTray.Visible = true;
                Hide();
                e.Cancel = true;
                return;
            }

            SystemEvents.UserPreferenceChanged -= SystemThemeChanged;
            cameraActivityMonitor?.Dispose();
            cameraActivityMonitor = null;
            hotkeys.ReleaseAll(registrar);
            foreach (CancellationTokenSource call in lightCalls.Values)
                call.Cancel();
            updateCoordinator.Dispose();
        }

        private void tmrCameraCheck_Tick(object sender, EventArgs e)
        {
            bool cameraOn = IsWebcamOn();
            ShowCamera(cameraOn);
            LightCommand? command = controller.OnCameraSample(
                cameraOn,
                DateTime.UtcNow,
                TimeSpan.FromMilliseconds((double)cameraOnDelay),
                TimeSpan.FromMilliseconds((double)cameraOffDelay),
                blockAutomaticOn: QuietHours.IsQuiet(userSettings, DateTime.Now));
            if (command != null)
                Dispatch(command.Value, automatic: true);
            UpdateQuietIndicator();

            RefreshAll(userInitiated: false);
            updateCoordinator.NotifyCamera(cameraOn, chkAutomaticUpdates.Checked);
        }

        private void updateTimer_Tick(object? sender, EventArgs e)
        {
            updateTimer.Interval = 6 * 60 * 60 * 1000;
            if (chkAutomaticUpdates.Checked)
                updateCoordinator.CheckNow();
        }

        private bool IsWebcamOn()
        {
            IReadOnlyList<string> active = ActiveCameraApps();
            RememberApps(active);
            return CameraAppPolicy.CountsAsOn(
                CameraAppFilterPreference.Parse(userSettings.CameraAppMode),
                userSettings.SelectedCameraApps,
                active);
        }

        private IReadOnlyList<string> ActiveCameraApps()
        {
            if (cameraActivityMonitor != null)
            {
                if (!cameraActivityMonitor.IsCameraStreaming)
                    return Array.Empty<string>();

                List<string> names = new List<string>();
                foreach (int processId in cameraActivityMonitor.CopyStreamingProcessIds())
                {
                    string? file = ProcessFileName(processId);
                    if (file != null)
                        names.Add(file);
                }

                List<string> cleaned = CameraAppNames.Clean(names);
                return cleaned.Count == 0 ? new[] { CameraAppNames.Unknown } : cleaned;
            }

            return WebcamConsent.Read(inUseOnly: true);
        }

        private static string? ProcessFileName(int processId)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                if (string.IsNullOrWhiteSpace(process.ProcessName))
                    return null;

                string name = process.ProcessName;
                return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return null;
            }
        }

        private void RememberApps(IEnumerable<string> active)
        {
            bool added = false;
            foreach (string name in CameraAppNames.Clean(active))
            {
                if (name.Equals(CameraAppNames.Unknown, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!seenCameraApps.Add(name))
                    continue;

                if (userSettings.KnownCameraApps.Any(existing => existing.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                userSettings.KnownCameraApps.Add(name);
                added = true;
            }

            if (added && settingsReady)
                SaveUserSettings();
        }

        private void ShowCamera(bool cameraOn)
        {
            lblCameraState.Text = cameraOn ? "Camera is ON" : "Camera is OFF";
            lblCameraState.ForeColor = cameraOn ? StatusOnColor() : StatusOffColor();
            niTray.Icon = cameraOn ? trayIconLit : trayIconUnlit;
        }

        private void SetLightMode(LightMode mode, bool persist)
        {
            if (applyingMode)
                return;

            applyingMode = true;
            try
            {
                LightCommand? command = controller.SetMode(mode, IsWebcamOn(), DateTime.UtcNow);
                if (persist && settingsReady)
                {
                    userSettings.LightMode = mode.ToString();
                    SaveUserSettings();
                }

                rbLightAuto.Checked = mode == LightMode.Auto;
                rbLightOn.Checked = mode == LightMode.On;
                rbLightOff.Checked = mode == LightMode.Off;
                tsmiAuto.Checked = mode == LightMode.Auto;
                tsmiOn.Checked = mode == LightMode.On;
                tsmiOff.Checked = mode == LightMode.Off;
                UpdateModeVisuals();
                if (command != null)
                    Dispatch(command.Value, automatic: mode == LightMode.Auto);
            }
            finally
            {
                applyingMode = false;
            }
        }

        private void UpdateModeVisuals()
        {
            LightControlSnapshot snapshot = LightModeController.Describe(controller.Mode);
            lblMode.Text = snapshot.Detail;
            Text = snapshot.Title;
            UpdateQuietIndicator();
            lblMode.ForeColor = controller.Mode switch
            {
                LightMode.On => StatusOnColor(),
                LightMode.Off => StatusOffColor(),
                _ => ForegroundColor()
            };
            tsmiAuto.Checked = snapshot.AutoChecked;
            tsmiOn.Checked = snapshot.OnChecked;
            tsmiOff.Checked = snapshot.OffChecked;
        }

        private void LightMode_CheckedChanged(object sender, EventArgs e)
        {
            if (!settingsReady || applyingMode || sender is not RadioButton radio || !radio.Checked)
                return;

            LightMode mode = rbLightOn.Checked ? LightMode.On : rbLightOff.Checked ? LightMode.Off : LightMode.Auto;
            SetLightMode(mode, true);
        }

        private void Dispatch(LightCommand command, bool automatic)
        {
            if (!controller.IsCurrent(command.Sequence))
                return;

            if (automatic && command.TurnOn && QuietHours.IsQuiet(userSettings, DateTime.Now))
                return;

            SendPower(command.TurnOn, automatic);
        }

        private void SendPower(bool turnOn, bool automatic)
        {
            IEnumerable<string> addresses = automatic
                ? LightGrouping.AutomaticAddresses(userSettings.Lights, userSettings.AllGroupsFollowCamera, userSettings.CameraGroups)
                : userSettings.Lights.Select(light => light.Address);
            SendBody(addresses, LightEndpoint.PutBody(turnOn));
        }

        private void SendBody(IEnumerable<string> addresses, string body)
        {
            foreach (LightTarget target in LightEndpoint.ForEachLight(addresses, LightPower.On))
                _ = PutBodyAsync(target.Address, body);
        }

        private async Task PutBodyAsync(string address, string body)
        {
            try
            {
                using StringContent content = new StringContent(body, Encoding.UTF8, "application/json");
                await httpClient.PutAsync(LightEndpoint.LightsUrl(address), content);
            }
            catch
            {
            }
        }

        private void RefreshAll(bool userInitiated)
        {
            board.SyncAddresses(Addresses());
            foreach (string address in Addresses())
                RefreshOne(address, userInitiated);
        }

        private void RefreshOne(string address, bool userInitiated)
        {
            if (!userInitiated && lightCalls.ContainsKey(address))
                return;

            if (userInitiated && lightCalls.TryGetValue(address, out CancellationTokenSource? previous))
            {
                lightCalls.Remove(address);
                previous.Cancel();
                previous.Dispose();
            }

            int attempt = userInitiated ? board.BeginUserRetry(address) : board.BeginBackgroundRefresh(address);
            ShowLightRows();
            CancellationTokenSource cts = new CancellationTokenSource(LightTiming.LightRequestTimeout);
            lightCalls[address] = cts;
            _ = RefreshOneAsync(address, attempt, cts);
        }

        private async Task RefreshOneAsync(string address, int attempt, CancellationTokenSource cts)
        {
            LightLinkState state = LightLinkState.Unreachable;
            string detail = LightDiagnostics.Unreachable(address);
            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(LightEndpoint.LightsUrl(address), cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    detail = LightDiagnostics.HttpFailure(address, (int)response.StatusCode);
                }
                else
                {
                    string body = await response.Content.ReadAsStringAsync(cts.Token);
                    if (!LightResponse.TryReadPower(body, out bool on))
                    {
                        state = LightLinkState.UnexpectedReply;
                        detail = LightDiagnostics.Unexpected(address);
                    }
                    else
                    {
                        state = on ? LightLinkState.On : LightLinkState.Off;
                        detail = on ? LightDiagnostics.OnDetail(address) : LightDiagnostics.OffDetail(address);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
            }

            void Finish()
            {
                if (IsDisposed)
                    return;

                board.TryComplete(address, attempt, state, detail);
                NotePresence(address, state);
                if (lightCalls.TryGetValue(address, out CancellationTokenSource? current) && current == cts)
                {
                    lightCalls.Remove(address);
                    cts.Dispose();
                }

                ShowLightRows();
            }

            if (IsDisposed)
                return;

            if (InvokeRequired)
                BeginInvoke(Finish);
            else
                Finish();
        }

        private void ShowLightRows()
        {
            IReadOnlyList<LightRow> rows = board.Rows;
            bool same = lvLights.Items.Count == rows.Count;
            for (int i = 0; same && i < rows.Count; i++)
                same = string.Equals(lvLights.Items[i].Name, rows[i].Address, StringComparison.OrdinalIgnoreCase);

            if (!same)
            {
                string? selected = SelectedAddress();
                lvLights.BeginUpdate();
                lvLights.Items.Clear();
                foreach (LightRow row in rows)
                {
                    ListViewItem item = new ListViewItem(row.Address);
                    item.Name = row.Address;
                    item.SubItems.Add(DescriptionFor(row.Address));
                    item.SubItems.Add(GroupFor(row.Address));
                    item.SubItems.Add(row.StatusText);
                    item.Selected = string.Equals(row.Address, selected, StringComparison.OrdinalIgnoreCase);
                    lvLights.Items.Add(item);
                }

                lvLights.EndUpdate();
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    EnsureSubItem(lvLights.Items[i], 1, DescriptionFor(rows[i].Address));
                    EnsureSubItem(lvLights.Items[i], 2, GroupFor(rows[i].Address));
                    EnsureSubItem(lvLights.Items[i], 3, rows[i].StatusText);
                }
            }

            lblLightState.Text = board.Summary;
            lblLightState.ForeColor = board.Kind switch
            {
                LightSummaryKind.AllOn => StatusOnColor(),
                LightSummaryKind.AllOff => StatusOffColor(),
                LightSummaryKind.Problems => ErrorColor(),
                _ => ForegroundColor()
            };
            lblDetail.Text = board.FirstProblem ?? "";
            lblDetail.Visible = board.FirstProblem != null;
            lblDetail.ForeColor = ErrorColor();
            btnRetry.Enabled = rows.Count > 0;
            bool selectedRow = lvLights.SelectedItems.Count > 0;
            btnRemoveLight.Enabled = selectedRow;
            btnEditLight.Enabled = selectedRow;
        }

        private static void EnsureSubItem(ListViewItem item, int index, string text)
        {
            while (item.SubItems.Count <= index)
                item.SubItems.Add("");

            if (item.SubItems[index].Text != text)
                item.SubItems[index].Text = text;
        }

        private IEnumerable<string> Addresses()
        {
            foreach (ListViewItem item in lvLights.Items)
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                    yield return item.Name;
            }
        }

        private string? SelectedAddress()
        {
            return lvLights.SelectedItems.Count == 0 ? null : lvLights.SelectedItems[0].Name;
        }

        private void ReloadLights()
        {
            lvLights.Items.Clear();
            foreach (LightRecord light in userSettings.Lights)
            {
                if (!string.IsNullOrWhiteSpace(light.Address))
                    AddAddress(light.Address, light.Description);
            }
        }

        private void AddAddress(string address, string description)
        {
            if (lvLights.Items.ContainsKey(address))
                return;

            ListViewItem item = new ListViewItem(address);
            item.Name = address;
            item.SubItems.Add(description);
            item.SubItems.Add(GroupFor(address));
            item.SubItems.Add("Not checked");
            lvLights.Items.Add(item);
        }

        private string DescriptionFor(string address)
        {
            foreach (LightRecord light in userSettings.Lights)
            {
                if (string.Equals(light.Address, address, StringComparison.OrdinalIgnoreCase))
                    return light.Description;
            }

            return "";
        }

        private string GroupFor(string address)
        {
            return RecordFor(address)?.Group ?? "";
        }

        private LightRecord? RecordFor(string address)
        {
            foreach (LightRecord light in userSettings.Lights)
            {
                if (string.Equals(light.Address, address, StringComparison.OrdinalIgnoreCase))
                    return light;
            }

            return null;
        }

        private void ImportElgatoLights()
        {
            string elgatoSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Elgato", "ControlCenter", "Settings.xml");
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(elgatoSettingsFile);
                foreach (System.Xml.XmlNode outer in doc.ChildNodes)
                {
                    if (outer.Name != "AppSettings")
                        continue;

                    foreach (System.Xml.XmlNode node in outer.ChildNodes)
                    {
                        if (node.Name != "Application")
                            continue;

                        foreach (System.Xml.XmlNode inner in node.ChildNodes)
                        {
                            if (inner.Name != "Accessories")
                                continue;

                            foreach (System.Xml.XmlNode accessory in inner.ChildNodes)
                            {
                                foreach (System.Xml.XmlNode attribute in accessory.ChildNodes)
                                {
                                    if (attribute.Name == "IpAddress")
                                        AddAddress(attribute.InnerText.Trim(), DescriptionFor(attribute.InnerText.Trim()));
                                }
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private void btnAddLight_Click(object sender, EventArgs e)
        {
            if (PromptLight(null) is not LightRecord light)
                return;

            userSettings.Lights.RemoveAll(existing => string.Equals(existing.Address, light.Address, StringComparison.OrdinalIgnoreCase));
            userSettings.Lights.Add(light);
            SaveUserSettings();
            ReloadLights();
            ImportElgatoLights();
            board.SyncAddresses(Addresses());
            ShowLightRows();
            RefreshAll(true);
        }

        private void btnEditLight_Click(object sender, EventArgs e)
        {
            EditSelectedLight();
        }

        private void lvLights_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                btnRemoveLight_Click(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode is Keys.Enter or Keys.F2)
            {
                EditSelectedLight();
                e.Handled = true;
            }
        }

        private void lvLights_DoubleClick(object sender, EventArgs e)
        {
            EditSelectedLight();
        }

        private void EditSelectedLight()
        {
            if (SelectedAddress() is not string address)
                return;

            if (PromptLight(RecordFor(address) ?? new LightRecord(address, DescriptionFor(address))) is not LightRecord edited)
                return;

            if (!string.Equals(address, edited.Address, StringComparison.OrdinalIgnoreCase))
                userSettings.Lights.RemoveAll(existing => string.Equals(existing.Address, address, StringComparison.OrdinalIgnoreCase));

            userSettings.Lights.RemoveAll(existing => string.Equals(existing.Address, edited.Address, StringComparison.OrdinalIgnoreCase));
            userSettings.Lights.Add(edited);
            SaveUserSettings();
            ReloadLights();
            ImportElgatoLights();
            board.SyncAddresses(Addresses());
            ShowLightRows();
        }

        private void btnRemoveLight_Click(object sender, EventArgs e)
        {
            if (SelectedAddress() is not string address)
                return;

            userSettings.Lights.RemoveAll(existing => string.Equals(existing.Address, address, StringComparison.OrdinalIgnoreCase));
            SaveUserSettings();
            ReloadLights();
            board.SyncAddresses(Addresses());
            ShowLightRows();
        }

        private void btnRetry_Click(object sender, EventArgs e)
        {
            board.SyncAddresses(Addresses());
            RefreshAll(userInitiated: true);
        }

        private void btnFindLights_Click(object sender, EventArgs e)
        {
            AddFoundLights(false);
        }

        private void menuOptions_Click(object sender, EventArgs e)
        {
            ShowOptions();
        }

        private void lvLights_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool selected = lvLights.SelectedItems.Count > 0;
            btnRemoveLight.Enabled = selected;
            btnEditLight.Enabled = selected;
        }

        private LightRecord? PromptLight(LightRecord? existing)
        {
            using Form prompt = new Form();
            const int logicalWidth = 420;
            DialogShell.Prepare(prompt, existing == null ? "Add light" : "Edit light", logicalWidth);
            TableLayoutPanel table = DialogShell.Content(logicalWidth);
            Label addressLabel = new Label { AutoSize = true, Text = "&Address" };
            TextBox addressBox = DialogShell.Field(existing?.Address ?? "", "Address", logicalWidth);
            Label descriptionLabel = new Label { AutoSize = true, Text = "&Description" };
            TextBox descriptionBox = DialogShell.Field(existing?.Description ?? "", "Description", logicalWidth);
            Label groupLabel = new Label { AutoSize = true, Text = "&Group" };
            TextBox groupBox = DialogShell.Field(existing?.Group ?? "", "Group", logicalWidth);
            string serial = existing?.Serial ?? "";
            string product = existing?.ProductName ?? "";
            Button find = DialogShell.CommandButton("Find lights", DialogResult.None);
            find.Click += (_, _) =>
            {
                using FindLightsForm found = new FindLightsForm(userSettings.Lights.Select(light => light.Address).ToList(), single: true);
                ApplyDialogTheme(found);
                if (found.ShowDialog(prompt) != DialogResult.OK || found.Chosen.Count == 0)
                    return;

                DiscoveredLight pick = found.Chosen[0];
                addressBox.Text = pick.Address;
                if (string.IsNullOrWhiteSpace(descriptionBox.Text))
                    descriptionBox.Text = pick.Name;
                serial = pick.Serial;
                product = string.IsNullOrWhiteSpace(pick.ProductName) ? pick.Name : pick.ProductName;
            };
            Button confirmation = DialogShell.CommandButton("OK", DialogResult.OK);
            Button cancel = DialogShell.CommandButton("Cancel", DialogResult.Cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;
            DialogShell.AddRow(table, addressLabel, new Padding(0, 0, 0, 4));
            DialogShell.AddRow(table, addressBox, new Padding(0, 0, 0, 8));
            DialogShell.AddRow(table, descriptionLabel, new Padding(0, 0, 0, 4));
            DialogShell.AddRow(table, descriptionBox, new Padding(0, 0, 0, 8));
            DialogShell.AddRow(table, groupLabel, new Padding(0, 0, 0, 4));
            DialogShell.AddRow(table, groupBox, new Padding(0, 0, 0, 8));
            DialogShell.AddRow(table, find, new Padding(0, 0, 0, 4));
            DialogShell.AddRow(table, DialogShell.ButtonRow(logicalWidth, cancel, confirmation), Padding.Empty);
            prompt.Controls.Add(table);
            prompt.ResumeLayout(true);
            ApplyDialogTheme(prompt);

            if (prompt.ShowDialog(Visible ? this : null) != DialogResult.OK || string.IsNullOrWhiteSpace(addressBox.Text))
                return null;

            return new LightRecord(addressBox.Text, descriptionBox.Text, groupBox.Text, product, serial);
        }

        private void helpAbout_Click(object sender, EventArgs e)
        {
            using AboutForm about = new AboutForm(CurrentVersion());
            if (palette != null)
            {
                ThemeApplier.Apply(about, palette.Value);
                about.HandleCreated += (_, _) => TitleBarTheme.Apply(about.Handle, effectiveTheme == EffectiveTheme.Dark);
            }

            about.ShowDialog(this);
        }

        private void helpElgato_Click(object sender, EventArgs e)
        {
            Program.OpenElgatoWebsite();
        }

        private void SizeLightColumns()
        {
            int width = Math.Max(180, lvLights.ClientSize.Width - 4);
            colLight.Width = (int)(width * 0.28);
            colDescription.Width = (int)(width * 0.32);
            colGroup.Width = (int)(width * 0.18);
            colStatus.Width = Math.Max(80, width - colLight.Width - colDescription.Width - colGroup.Width);
        }

        private void StyleChrome()
        {
            lvLights.OwnerDraw = true;
            lvLights.Invalidate();
            cmbTheme.FlatStyle = FlatStyle.Flat;
            cmbTheme.DrawMode = DrawMode.OwnerDrawFixed;
            if (palette == null)
            {
                menuMain.Renderer = new ToolStripProfessionalRenderer();
                menuMain.BackColor = SystemColors.MenuBar;
                menuMain.ForeColor = SystemColors.MenuText;
                return;
            }

            ThemeApplier.ApplyMenu(menuMain, palette.Value);
        }

        private void lvLights_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            Color back = palette?.ButtonBackground.ToColor() ?? SystemColors.Control;
            Color fore = palette?.Foreground.ToColor() ?? SystemColors.ControlText;
            Color line = palette?.Border.ToColor() ?? SystemColors.ControlDark;
            using SolidBrush brush = new SolidBrush(back);
            e.Graphics.FillRectangle(brush, e.Bounds);
            Rectangle text = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", Font, text, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            using Pen pen = new Pen(line);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void lvLights_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
        }

        private void lvLights_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item.Selected;
            Color back = selected
                ? palette?.MenuHighlight.ToColor() ?? SystemColors.Highlight
                : palette?.InputBackground.ToColor() ?? SystemColors.Window;
            Color fore = selected
                ? palette?.MenuHighlightText.ToColor() ?? SystemColors.HighlightText
                : palette?.InputForeground.ToColor() ?? SystemColors.WindowText;
            using SolidBrush brush = new SolidBrush(back);
            e.Graphics.FillRectangle(brush, e.Bounds);
            Rectangle text = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 8), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", Font, text, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            if (selected && e.ColumnIndex == 0 && lvLights.Focused)
                e.DrawFocusRectangle(e.Bounds);
        }

        private void cmbTheme_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color back = selected
                ? palette?.MenuHighlight.ToColor() ?? SystemColors.Highlight
                : palette?.InputBackground.ToColor() ?? SystemColors.Window;
            Color fore = selected
                ? palette?.MenuHighlightText.ToColor() ?? SystemColors.HighlightText
                : palette?.InputForeground.ToColor() ?? SystemColors.WindowText;
            using SolidBrush brush = new SolidBrush(back);
            e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, cmbTheme.Items[e.Index]?.ToString() ?? "", Font, e.Bounds, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            if ((e.State & DrawItemState.Focus) != 0)
                e.DrawFocusRectangle();
        }

        private void chkStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (!settingsReady || applyingStartup)
                return;

            applyingStartup = true;
            chkStartWithWindows.Checked = StartupRegistry.ApplyForCurrentProcess(chkStartWithWindows.Checked);
            applyingStartup = false;
        }

        private void numUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (!settingsReady)
                return;

            cameraOnDelay = numUpDown.Value;
            userSettings.CameraOnDelayMs = cameraOnDelay;
            SaveUserSettings();
        }

        private void numUpDownOff_ValueChanged(object sender, EventArgs e)
        {
            if (!settingsReady)
                return;

            cameraOffDelay = numUpDownOff.Value;
            userSettings.CameraOffDelayMs = cameraOffDelay;
            SaveUserSettings();
        }

        private static decimal ClampDelay(decimal value, decimal maximum)
        {
            if (value < 0)
                return 0;
            if (value > maximum)
                return maximum;
            return value;
        }

        private void SaveUserSettings()
        {
            UserSettingsFile.Save(userSettings);
        }

        private void niTray_DoubleClick(object sender, EventArgs e)
        {
            Show();
            Activate();
        }

        private void cmsTray_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == null)
                return;

            switch (e.ClickedItem.Name)
            {
                case "tsmiExit":
                    Application.Exit();
                    break;
                case "tsmiOpen":
                    Show();
                    Activate();
                    break;
                case "tsmiAuto":
                    SetLightMode(LightMode.Auto, true);
                    break;
                case "tsmiOn":
                    SetLightMode(LightMode.On, true);
                    break;
                case "tsmiOff":
                    SetLightMode(LightMode.Off, true);
                    break;
                case "tsmiShortcuts":
                    ShowShortcuts();
                    break;
                case "tsmiFindLights":
                    AddFoundLights(true);
                    break;
                case "tsmiOptions":
                    ShowOptions();
                    break;
            }
        }

        private void btnShortcuts_Click(object sender, EventArgs e)
        {
            ShowShortcuts();
        }

        private void ShowShortcuts()
        {
            capturingShortcut = true;
            try
            {
                using ShortcutDialog dialog = new ShortcutDialog(AssignShortcut, hotkeys.Display);
                if (palette != null)
                {
                    ThemeApplier.Apply(dialog, palette.Value);
                    dialog.HandleCreated += (_, _) => TitleBarTheme.Apply(dialog.Handle, effectiveTheme == EffectiveTheme.Dark);
                }
                else
                {
                    ThemeApplier.ApplySystem(dialog);
                }

                if (Visible)
                    dialog.ShowDialog(this);
                else
                    dialog.ShowDialog();
            }
            finally
            {
                capturingShortcut = false;
                RefreshShortcutLabels();
            }
        }

        private HotkeyAssignment AssignShortcut(LightMode mode, HotkeyChord? chord)
        {
            HotkeyAssignment result = hotkeys.Assign(mode, chord, registrar);
            string text = result.ActiveChord == null ? "" : HotkeyGrammar.Format(result.ActiveChord);
            switch (mode)
            {
                case LightMode.On:
                    userSettings.HotkeyOn = text;
                    break;
                case LightMode.Off:
                    userSettings.HotkeyOff = text;
                    break;
                default:
                    userSettings.HotkeyAuto = text;
                    break;
            }

            SaveUserSettings();
            RefreshShortcutLabels();
            if (!result.Applied && result.Error != null)
                ShowUpdate(new UpdateUi(result.Error, btnCheckUpdates.Enabled, btnInstallUpdate.Enabled, true));

            return result;
        }

        private void RefreshShortcutLabels()
        {
            tsmiAuto.ShortcutKeyDisplayString = hotkeys.Current(LightMode.Auto) == null ? "" : hotkeys.Display(LightMode.Auto);
            tsmiOn.ShortcutKeyDisplayString = hotkeys.Current(LightMode.On) == null ? "" : hotkeys.Display(LightMode.On);
            tsmiOff.ShortcutKeyDisplayString = hotkeys.Current(LightMode.Off) == null ? "" : hotkeys.Display(LightMode.Off);
            bool problem = lblUpdate.Text.Contains("another program", StringComparison.OrdinalIgnoreCase)
                || lblUpdate.Text.Contains("was not registered", StringComparison.OrdinalIgnoreCase);
            btnShortcuts.Text = problem ? "Shortcuts…" : "Shortcuts";
        }

        private void cmbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!settingsReady)
                return;

            ThemePreference preference = SelectedTheme();
            userSettings.Theme = preference.ToString();
            SaveUserSettings();
            ApplyTheme(preference);
        }

        private ThemePreference SelectedTheme()
        {
            return cmbTheme.SelectedIndex switch
            {
                0 => ThemePreference.Light,
                1 => ThemePreference.Dark,
                _ => ThemePreference.FollowWindows
            };
        }

        private static int ThemeIndex(ThemePreference preference)
        {
            return preference switch
            {
                ThemePreference.Light => 0,
                ThemePreference.Dark => 1,
                _ => 2
            };
        }

        private void ApplyTheme(ThemePreference preference)
        {
            effectiveTheme = ThemeResolver.Resolve(preference, WindowsTheme.AppsUseLightTheme(), SystemInformation.HighContrast);
            palette = effectiveTheme == EffectiveTheme.System ? null : ThemeResolver.PaletteFor(effectiveTheme);
            if (palette == null)
                ThemeApplier.ApplySystem(this, cmsTray);
            else
            {
                ThemeApplier.Apply(this, palette.Value);
                ThemeApplier.ApplyMenu(cmsTray, palette.Value);
            }

            TitleBarTheme.Apply(Handle, effectiveTheme == EffectiveTheme.Dark);
            StyleChrome();
            ShowCamera(IsWebcamOn());
            UpdateModeVisuals();
            ShowLightRows();
            lblUpdate.ForeColor = updateProblem ? ErrorColor() : MutedColor();
        }

        private void SystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility))
                return;

            if (IsDisposed || SelectedTheme() != ThemePreference.FollowWindows)
                return;

            BeginInvoke(() => ApplyTheme(ThemePreference.FollowWindows));
        }

        private void chkAutomaticUpdates_CheckedChanged(object sender, EventArgs e)
        {
            if (!settingsReady)
                return;

            userSettings.AutomaticUpdates = chkAutomaticUpdates.Checked;
            SaveUserSettings();
            if (chkAutomaticUpdates.Checked)
            {
                updateTimer.Interval = 6 * 60 * 60 * 1000;
                updateTimer.Start();
                updateCoordinator.CheckNow();
            }
            else
            {
                updateTimer.Stop();
                ShowUpdate(new UpdateUi(UpdateMessages.AutomaticOff + " Version " + ReleaseVersion.Format(CurrentVersion()) + ".", true, btnInstallUpdate.Enabled, false));
            }
        }

        private void btnCheckUpdates_Click(object sender, EventArgs e)
        {
            updateCoordinator.CheckNow();
        }

        private void btnInstallUpdate_Click(object sender, EventArgs e)
        {
            updateCoordinator.RequestInstall();
        }

        private void ShowUpdate(UpdateUi state)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => ShowUpdate(state));
                return;
            }

            updateProblem = state.Problem;
            lblUpdate.Text = state.Status;
            lblUpdate.ForeColor = state.Problem ? ErrorColor() : MutedColor();
            btnCheckUpdates.Enabled = state.CheckEnabled;
            btnInstallUpdate.Enabled = state.InstallEnabled;
            RefreshShortcutLabels();
        }

        private void ShowPendingUpdateError()
        {
            try
            {
                string path = UpdateInstaller.LastErrorPath();
                if (!File.Exists(path))
                    return;

                string message = File.ReadAllText(path).Trim();
                File.Delete(path);
                if (message.Length > 0)
                    ShowUpdate(new UpdateUi(message, true, false, true));
            }
            catch
            {
            }
        }

        private static void CleanupStaging()
        {
            try
            {
                string staging = Path.Combine(UpdateInstaller.UpdatesRoot(), "staging");
                if (Directory.Exists(staging) && !PackageRules.IsInside(AppContext.BaseDirectory, staging))
                    Directory.Delete(staging, true);
            }
            catch
            {
            }
        }

        private void UpdateQuietIndicator()
        {
            string title = LightModeController.Describe(controller.Mode).Title;
            bool quiet = QuietHours.IsQuiet(userSettings, DateTime.Now);
            niTray.Text = QuietHours.TrayTip(title, quiet);
            tsmiQuiet.Text = QuietHours.MenuLabel(userSettings, DateTime.Now);
        }

        private void NotePresence(string address, LightLinkState state)
        {
            if (state is not (LightLinkState.On or LightLinkState.Off or LightLinkState.Unreachable or LightLinkState.UnexpectedReply))
                return;

            bool reachable = state is LightLinkState.On or LightLinkState.Off;
            PresenceNotice? notice = presence.Observe(address, reachable, DateTime.UtcNow);
            if (notice == null)
                return;

            if (userSettings.NotifyLightPresence)
            {
                string label = DescriptionFor(notice.Value.Address);
                niTray.ShowBalloonTip(
                    4000,
                    PresenceText.Title(notice.Value.Online),
                    PresenceText.Body(string.IsNullOrWhiteSpace(label) ? notice.Value.Address : label, notice.Value.Online),
                    notice.Value.Online ? ToolTipIcon.Info : ToolTipIcon.Warning);
            }

            if (!notice.Value.Online)
                _ = RediscoverOfflineAsync();
        }

        private async Task RediscoverOfflineAsync()
        {
            if (Interlocked.CompareExchange(ref discoveryBusy, 1, 0) != 0)
                return;

            try
            {
                DiscoveryResult result = await ElgatoBrowser.SearchAsync(TimeSpan.FromSeconds(3), CancellationToken.None);
                if (IsDisposed || result.Lights.Count == 0)
                    return;

                void Apply()
                {
                    if (IsDisposed)
                        return;

                    bool changed = false;
                    foreach (LightRecord light in userSettings.Lights.ToList())
                    {
                        string? replacement = ElgatoDiscovery.ReplacementAddress(light, userSettings.Lights, result.Lights);
                        if (replacement == null)
                            continue;

                        light.Address = replacement;
                        changed = true;
                    }

                    if (!changed)
                        return;

                    SaveUserSettings();
                    ReloadLights();
                    board.SyncAddresses(Addresses());
                    ShowLightRows();
                    RefreshAll(true);
                }

                if (InvokeRequired)
                    BeginInvoke(Apply);
                else
                    Apply();
            }
            catch (OperationCanceledException)
            {
            }
            catch (System.Net.Sockets.SocketException)
            {
            }
            finally
            {
                Interlocked.Exchange(ref discoveryBusy, 0);
            }
        }

        private void AnnounceInstalledUpdate()
        {
            try
            {
                string path = Path.Combine(UpdateInstaller.UpdatesRoot(), "just-installed.txt");
                if (!File.Exists(path))
                    return;

                string version = File.ReadAllText(path).Trim();
                File.Delete(path);
                if (userSettings.NotifyOnUpdate && version.Length > 0)
                    niTray.ShowBalloonTip(5000, "Update installed", "Auto Key Light " + version + " is now running.", ToolTipIcon.Info);
            }
            catch (IOException)
            {
            }
        }

        private void ApplyDialogTheme(Form dialog)
        {
            if (palette != null)
            {
                ThemeApplier.Apply(dialog, palette.Value);
                dialog.HandleCreated += (_, _) => TitleBarTheme.Apply(dialog.Handle, effectiveTheme == EffectiveTheme.Dark);
            }
            else
            {
                ThemeApplier.ApplySystem(dialog);
            }
        }

        private void AddFoundLights(bool fromTray)
        {
            using FindLightsForm found = new FindLightsForm(userSettings.Lights.Select(light => light.Address).ToList(), single: false);
            ApplyDialogTheme(found);
            IWin32Window? owner = Visible && !fromTray ? this : null;
            if (found.ShowDialog(owner) != DialogResult.OK)
                return;

            bool changed = false;
            foreach (DiscoveredLight light in found.Chosen)
            {
                bool exists = userSettings.Lights.Any(saved => LightEndpoint.Host(saved.Address).Equals(LightEndpoint.Host(light.Address), StringComparison.OrdinalIgnoreCase));
                if (exists)
                    continue;

                string product = string.IsNullOrWhiteSpace(light.ProductName) ? light.Name : light.ProductName;
                userSettings.Lights.Add(new LightRecord(light.Address, light.Name, "", product, light.Serial));
                changed = true;
            }

            if (!changed)
                return;

            SaveUserSettings();
            ReloadLights();
            board.SyncAddresses(Addresses());
            ShowLightRows();
            RefreshAll(true);
        }

        private void ShowOptions()
        {
            AppUserSettings edited = userSettings.Clone();
            List<string> observed = seenCameraApps.Concat(WebcamConsent.Read(inUseOnly: false)).ToList();
            using OptionsForm dialog = new OptionsForm(edited, LightGrouping.Names(userSettings.Lights), observed);
            ApplyDialogTheme(dialog);
            if (dialog.ShowDialog(Visible ? this : null) != DialogResult.OK)
                return;

            userSettings = edited;
            seenCameraApps.Clear();
            foreach (string name in userSettings.KnownCameraApps)
                seenCameraApps.Add(name);
            SaveUserSettings();
            UpdateQuietIndicator();
        }

        private void cmsTray_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            RebuildTrayGroups();
            UpdateQuietIndicator();
            if (palette != null)
                ThemeApplier.ApplyMenu(cmsTray, palette.Value);
            else
                ThemeApplier.ApplySystemMenu(cmsTray);
        }

        private void RebuildTrayGroups()
        {
            tsmiGroups.DropDownItems.Clear();
            IReadOnlyList<string> names = LightGrouping.Names(userSettings.Lights);
            bool ungrouped = userSettings.Lights.Any(light => string.IsNullOrWhiteSpace(light.Group));
            tsmiGroups.Visible = names.Count > 0;
            foreach (string name in names)
                tsmiGroups.DropDownItems.Add(GroupMenu(name, name));

            if (names.Count > 0 && ungrouped)
                tsmiGroups.DropDownItems.Add(GroupMenu("", LightGrouping.UngroupedLabel));
        }

        private ToolStripMenuItem GroupMenu(string group, string label)
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(label);
            List<string> addresses = LightGrouping.Members(userSettings.Lights, group).Select(light => light.Address).ToList();
            menu.DropDownItems.Add(GroupCommand("On", addresses, LightEndpoint.PutBody(true)));
            menu.DropDownItems.Add(GroupCommand("Off", addresses, LightEndpoint.PutBody(false)));
            foreach (int level in new[] { 25, 50, 75, 100 })
            {
                int brightness = level;
                menu.DropDownItems.Add(GroupCommand("Brightness " + brightness + "%", addresses, LightEndpoint.BrightnessBody(brightness)));
            }

            return menu;
        }

        private ToolStripMenuItem GroupCommand(string text, IReadOnlyList<string> addresses, string body)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += (_, _) => SendBody(addresses, body);
            return item;
        }

        private Color StatusOnColor()
        {
            return palette?.StatusOn.ToColor() ?? SystemColors.ControlText;
        }

        private Color StatusOffColor()
        {
            return palette?.StatusOff.ToColor() ?? SystemColors.ControlText;
        }

        private Color ErrorColor()
        {
            return palette?.Error.ToColor() ?? SystemColors.ControlText;
        }

        private Color MutedColor()
        {
            return palette?.Muted.ToColor() ?? SystemColors.ControlText;
        }

        private Color ForegroundColor()
        {
            return palette?.Foreground.ToColor() ?? SystemColors.ControlText;
        }
    }
}
