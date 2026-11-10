using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal sealed class ShortcutDialog : Form
    {
        private readonly Func<LightMode, HotkeyChord?, HotkeyAssignment> assign;
        private readonly Func<LightMode, string> current;
        private readonly Dictionary<LightMode, Label> displays = new Dictionary<LightMode, Label>();
        private readonly Label status;
        private LightMode? capturing;

        public ShortcutDialog(Func<LightMode, HotkeyChord?, HotkeyAssignment> assign, Func<LightMode, string> current)
        {
            this.assign = assign;
            this.current = current;
            const int logicalWidth = 480;
            DialogShell.Prepare(this, "Keyboard shortcuts", logicalWidth);
            KeyPreview = true;

            TableLayoutPanel table = DialogShell.Content(logicalWidth);
            table.ColumnCount = 4;
            table.ColumnStyles.Clear();
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label intro = DialogShell.WrappingLabel(
                "These shortcuts work while this window is closed. Nothing is reserved at first, so other programs keep their keys. Use Ctrl, Alt, or Shift with a letter, number, or function key.",
                logicalWidth);
            table.Controls.Add(intro, 0, 0);
            table.SetColumnSpan(intro, 4);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowCount = 1;

            int row = 1;
            foreach (LightMode mode in new[] { LightMode.Auto, LightMode.On, LightMode.Off })
            {
                LightMode captured = mode;
                Label name = new Label { AutoSize = true, Text = mode.ToString(), Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 12, 0) };
                Label value = new Label { AutoSize = true, Text = current(mode), Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 12, 0) };
                Button set = DialogShell.CommandButton("Set", DialogResult.None);
                Button clear = DialogShell.CommandButton("Clear", DialogResult.None);
                set.DialogResult = DialogResult.None;
                clear.DialogResult = DialogResult.None;
                set.Margin = new Padding(0, 4, 8, 0);
                clear.Margin = new Padding(0, 4, 0, 0);
                set.Click += (_, _) =>
                {
                    capturing = captured;
                    status.Text = "Press Ctrl, Alt, or Shift plus a key for " + captured + ". Esc cancels.";
                };
                clear.Click += (_, _) =>
                {
                    capturing = null;
                    HotkeyAssignment result = assign(captured, null);
                    status.Text = result.Applied ? captured + " shortcut cleared." : result.Error ?? "";
                    RefreshDisplays();
                };
                displays[mode] = value;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.RowCount = row + 1;
                table.Controls.Add(name, 0, row);
                table.Controls.Add(value, 1, row);
                table.Controls.Add(set, 2, row);
                table.Controls.Add(clear, 3, row);
                row++;
            }

            status = DialogShell.WrappingLabel("", logicalWidth);
            status.Margin = new Padding(0, 12, 0, 0);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowCount = row + 1;
            table.Controls.Add(status, 0, row);
            table.SetColumnSpan(status, 4);
            Controls.Add(table);
            ResumeLayout(true);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (capturing == null)
            {
                base.OnKeyDown(e);
                return;
            }

            e.SuppressKeyPress = true;
            e.Handled = true;
            if (e.KeyCode == Keys.Escape)
            {
                capturing = null;
                status.Text = "Cancelled.";
                return;
            }

            if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin)
                return;

            string key = KeyName(e.KeyCode);
            if (!HotkeyGrammar.TryCreate(e.Control, e.Alt, e.Shift, key, out HotkeyChord? chord, out string? error) || chord == null)
            {
                status.Text = error ?? "That shortcut was not saved.";
                capturing = null;
                return;
            }

            HotkeyAssignment result = assign(capturing.Value, chord);
            status.Text = result.Applied ? "Saved " + HotkeyGrammar.Format(chord) + "." : result.Error ?? "That shortcut was not saved.";
            capturing = null;
            RefreshDisplays();
        }

        private void RefreshDisplays()
        {
            foreach (KeyValuePair<LightMode, Label> pair in displays)
                pair.Value.Text = current(pair.Key);
        }

        private static string KeyName(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
                return ((char)('0' + (key - Keys.D0))).ToString();

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return ((char)('0' + (key - Keys.NumPad0))).ToString();

            return key.ToString();
        }
    }
}
