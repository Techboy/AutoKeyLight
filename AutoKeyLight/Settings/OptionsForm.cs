using AutoKeyLight.Core;

namespace AutoKeyLight
{
    internal sealed class OptionsForm : Form
    {
        private readonly AppUserSettings settings;
        private readonly TabControl tabs;
        private TabPage quietPage = null!;
        private CheckBox quietEnabled = null!;
        private TextBox quietStart = null!;
        private TextBox quietEnd = null!;
        private CheckBox[] days = null!;
        private Label quietError = null!;
        private RadioButton modeAll = null!;
        private RadioButton modeOnly = null!;
        private RadioButton modeExcept = null!;
        private CheckedListBox appList = null!;
        private TextBox appName = null!;
        private CheckBox allGroups = null!;
        private CheckedListBox groupList = null!;
        private CheckBox notifyPresence = null!;
        private CheckBox notifyUpdate = null!;

        public OptionsForm(AppUserSettings settings, IReadOnlyList<string> groups, IReadOnlyList<string> observedApps)
        {
            this.settings = settings;
            const int logicalWidth = 520;
            DialogShell.Prepare(this, "Options", logicalWidth);
            int inner = DialogShell.ContentWidth(logicalWidth);
            tabs = new TabControl
            {
                Width = inner,
                Height = 360,
                MinimumSize = new Size(inner, 360),
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(118, 28),
                DrawMode = TabDrawMode.OwnerDrawFixed
            };
            tabs.DrawItem += DrawTab;
            quietPage = QuietPage(inner);
            tabs.TabPages.Add(quietPage);
            tabs.TabPages.Add(AppsPage(inner, observedApps));
            tabs.TabPages.Add(GroupsPage(inner, groups));
            tabs.TabPages.Add(NotificationsPage(inner));
            Button ok = DialogShell.CommandButton("OK", DialogResult.None);
            Button cancel = DialogShell.CommandButton("Cancel", DialogResult.Cancel);
            ok.Click += (_, _) => Accept();
            AcceptButton = ok;
            CancelButton = cancel;
            TableLayoutPanel table = DialogShell.Content(logicalWidth);
            DialogShell.AddRow(table, tabs, new Padding(0, 0, 0, 4));
            DialogShell.AddRow(table, DialogShell.ButtonRow(logicalWidth, cancel, ok), Padding.Empty);
            Controls.Add(table);
            ResumeLayout(true);
        }

        private TabPage QuietPage(int inner)
        {
            TabPage page = new TabPage("Quiet hours") { Padding = new Padding(8) };
            quietEnabled = new CheckBox { AutoSize = true, Text = "Enable quiet hours", Location = new Point(8, 12), Checked = settings.QuietHoursEnabled };
            Label startLabel = new Label { AutoSize = true, Text = "Start", Location = new Point(8, 48) };
            quietStart = new TextBox { Location = new Point(56, 44), Width = 72, Text = settings.QuietStart, AccessibleName = "Quiet hours start" };
            Label endLabel = new Label { AutoSize = true, Text = "End", Location = new Point(160, 48) };
            quietEnd = new TextBox { Location = new Point(200, 44), Width = 72, Text = settings.QuietEnd, AccessibleName = "Quiet hours end" };
            FlowLayoutPanel dayRow = new FlowLayoutPanel { Location = new Point(8, 80), Size = new Size(inner - 24, 36), WrapContents = false };
            days = new CheckBox[QuietHours.DayTokens.Length];
            bool[] selected = QuietHours.ParseDays(settings.QuietDays);
            for (int i = 0; i < days.Length; i++)
            {
                days[i] = new CheckBox { AutoSize = true, Text = QuietHours.DayTokens[i], Checked = selected[i], Margin = new Padding(0, 4, 8, 0) };
                dayRow.Controls.Add(days[i]);
            }

            Label hint = new Label
            {
                Location = new Point(8, 128),
                MaximumSize = new Size(inner - 24, 120),
                AutoSize = true,
                Text = "During quiet hours, automatic mode does not turn lights on. On and Off from the tray still work. A range that passes midnight, such as 22:00 to 07:00, is supported. If the times match, the selected days are quiet all day."
            };
            quietError = new Label { Location = new Point(8, 250), AutoSize = true, MaximumSize = new Size(inner - 24, 40), Text = "" };
            page.Controls.Add(quietEnabled);
            page.Controls.Add(startLabel);
            page.Controls.Add(quietStart);
            page.Controls.Add(endLabel);
            page.Controls.Add(quietEnd);
            page.Controls.Add(dayRow);
            page.Controls.Add(hint);
            page.Controls.Add(quietError);
            return page;
        }

        private TabPage AppsPage(int inner, IReadOnlyList<string> observedApps)
        {
            TabPage page = new TabPage("Camera apps") { Padding = new Padding(8) };
            CameraAppFilter filter = CameraAppFilterPreference.Parse(settings.CameraAppMode);
            modeAll = new RadioButton { AutoSize = true, Text = "All apps", Location = new Point(8, 8), Checked = filter == CameraAppFilter.All };
            modeOnly = new RadioButton { AutoSize = true, Text = "Only these apps", Location = new Point(110, 8), Checked = filter == CameraAppFilter.Only };
            modeExcept = new RadioButton { AutoSize = true, Text = "All except these apps", Location = new Point(250, 8), Checked = filter == CameraAppFilter.Except };
            appList = new CheckedListBox
            {
                Location = new Point(8, 36),
                Size = new Size(inner - 24, 160),
                CheckOnClick = true,
                IntegralHeight = false,
                AccessibleName = "Camera apps"
            };
            foreach (string name in CameraAppNames.Clean(settings.KnownCameraApps.Concat(settings.SelectedCameraApps).Concat(observedApps)))
            {
                bool selected = settings.SelectedCameraApps.Any(app => app.Equals(name, StringComparison.OrdinalIgnoreCase));
                appList.Items.Add(name, selected);
            }

            appName = new TextBox { Location = new Point(8, 206), Width = 220, AccessibleName = "App name" };
            Button add = new Button { Text = "Add", Location = new Point(236, 204), Size = new Size(DialogShell.ButtonWidth, DialogShell.ButtonHeight) };
            Button remove = new Button { Text = "Remove", Location = new Point(332, 204), Size = new Size(DialogShell.ButtonWidth, DialogShell.ButtonHeight) };
            add.Click += (_, _) => AddApp();
            remove.Click += (_, _) =>
            {
                if (appList.SelectedIndex >= 0)
                    appList.Items.RemoveAt(appList.SelectedIndex);
            };
            Label hint = new Label
            {
                Location = new Point(8, 244),
                MaximumSize = new Size(inner - 24, 80),
                AutoSize = true,
                Text = "All apps is the current behavior. Checked apps are the only ones that count, or the ones that are ignored, depending on the choice above. Names appear after an app uses the camera, or you can type an exe name."
            };
            page.Controls.Add(modeAll);
            page.Controls.Add(modeOnly);
            page.Controls.Add(modeExcept);
            page.Controls.Add(appList);
            page.Controls.Add(appName);
            page.Controls.Add(add);
            page.Controls.Add(remove);
            page.Controls.Add(hint);
            return page;
        }

        private TabPage GroupsPage(int inner, IReadOnlyList<string> groups)
        {
            TabPage page = new TabPage("Groups") { Padding = new Padding(8) };
            allGroups = new CheckBox
            {
                AutoSize = true,
                Text = "All groups follow the camera",
                Location = new Point(8, 12),
                Checked = settings.AllGroupsFollowCamera
            };
            groupList = new CheckedListBox
            {
                Location = new Point(8, 40),
                Size = new Size(inner - 24, 150),
                CheckOnClick = true,
                IntegralHeight = false,
                Enabled = !settings.AllGroupsFollowCamera,
                AccessibleName = "Groups that follow the camera"
            };
            foreach (string name in LightGrouping.Clean(groups.Concat(settings.CameraGroups)))
            {
                bool selected = settings.CameraGroups.Any(group => group.Equals(name, StringComparison.OrdinalIgnoreCase));
                groupList.Items.Add(name, selected);
            }

            allGroups.CheckedChanged += (_, _) => groupList.Enabled = !allGroups.Checked;
            Label hint = new Label
            {
                Location = new Point(8, 204),
                MaximumSize = new Size(inner - 24, 100),
                AutoSize = true,
                Text = "The tray menu can turn a group on or off, or set its brightness. Automatic switching stays per light. Uncheck the box to choose which groups follow the camera. Lights with no group follow the camera only when every group does."
            };
            page.Controls.Add(allGroups);
            page.Controls.Add(groupList);
            page.Controls.Add(hint);
            return page;
        }

        private TabPage NotificationsPage(int inner)
        {
            TabPage page = new TabPage("Notifications") { Padding = new Padding(8) };
            notifyPresence = new CheckBox
            {
                AutoSize = true,
                Text = "Notify when a light goes offline or comes back",
                Location = new Point(8, 16),
                Checked = settings.NotifyLightPresence
            };
            notifyUpdate = new CheckBox
            {
                AutoSize = true,
                Text = "Notify when an update is installed",
                Location = new Point(8, 44),
                Checked = settings.NotifyOnUpdate
            };
            Label hint = new Label
            {
                Location = new Point(8, 84),
                MaximumSize = new Size(inner - 24, 80),
                AutoSize = true,
                Text = "Offline and online notices wait about 20 seconds, so a light that flickers does not send a burst of messages. The first check after startup does not notify."
            };
            page.Controls.Add(notifyPresence);
            page.Controls.Add(notifyUpdate);
            page.Controls.Add(hint);
            return page;
        }

        private void DrawTab(object? sender, DrawItemEventArgs e)
        {
            bool selected = tabs.SelectedIndex == e.Index;
            Color back = selected ? SystemColors.Highlight : tabs.Parent?.BackColor ?? SystemColors.Control;
            Color fore = selected ? SystemColors.HighlightText : tabs.Parent?.ForeColor ?? SystemColors.ControlText;
            using SolidBrush brush = new SolidBrush(back);
            e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, e.Font, e.Bounds, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void AddApp()
        {
            string name = CameraAppNames.Friendly(appName.Text);
            if (name.Length == 0)
                return;

            for (int i = 0; i < appList.Items.Count; i++)
            {
                if (string.Equals(appList.Items[i]?.ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    appList.SetItemChecked(i, true);
                    appName.Clear();
                    return;
                }
            }

            appList.Items.Add(name, true);
            appName.Clear();
        }

        private void Accept()
        {
            if (!QuietHours.TryParseTime(quietStart.Text, out int start) || !QuietHours.TryParseTime(quietEnd.Text, out int end))
            {
                quietError.Text = "Use 24-hour times such as 22:00 and 07:00.";
                tabs.SelectedTab = quietPage;
                quietStart.Focus();
                return;
            }

            settings.QuietHoursEnabled = quietEnabled.Checked;
            settings.QuietStart = QuietHours.FormatTime(start);
            settings.QuietEnd = QuietHours.FormatTime(end);
            bool[] selectedDays = new bool[days.Length];
            for (int i = 0; i < days.Length; i++)
                selectedDays[i] = days[i].Checked;
            settings.QuietDays = QuietHours.FormatDays(selectedDays);
            settings.CameraAppMode = modeOnly.Checked ? CameraAppFilter.Only.ToString() : modeExcept.Checked ? CameraAppFilter.Except.ToString() : CameraAppFilter.All.ToString();
            settings.KnownCameraApps = appList.Items.Cast<object>().Select(item => item.ToString() ?? "").Where(name => name.Length > 0).ToList();
            settings.SelectedCameraApps = appList.CheckedItems.Cast<object>().Select(item => item.ToString() ?? "").Where(name => name.Length > 0).ToList();
            settings.AllGroupsFollowCamera = allGroups.Checked;
            settings.CameraGroups = groupList.CheckedItems.Cast<object>().Select(item => item.ToString() ?? "").Where(name => name.Length > 0).ToList();
            settings.NotifyLightPresence = notifyPresence.Checked;
            settings.NotifyOnUpdate = notifyUpdate.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
