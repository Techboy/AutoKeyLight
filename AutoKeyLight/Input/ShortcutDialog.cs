using AutoKeyLight.Core;

namespace AutoKeyLight
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
            Text = "Keyboard shortcuts";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(470, 250);
            KeyPreview = true;

            Label intro = new Label
            {
                Left = 12,
                Top = 12,
                Width = 446,
                Height = 52,
                Text = "These shortcuts work while this window is closed. Nothing is reserved at first, so other programs keep their keys. Use Ctrl, Alt, or Shift with a letter, number, or function key."
            };
            Controls.Add(intro);

            int top = 72;
            foreach (LightMode mode in new[] { LightMode.Auto, LightMode.On, LightMode.Off })
            {
                LightMode captured = mode;
                Label name = new Label { Left = 12, Top = top + 4, Width = 48, Text = mode.ToString() };
                Label value = new Label { Left = 68, Top = top + 4, Width = 180, Text = current(mode) };
                Button set = new Button { Left = 258, Top = top, Width = 88, Height = 26, Text = "Set" };
                Button clear = new Button { Left = 354, Top = top, Width = 88, Height = 26, Text = "Clear" };
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
                Controls.Add(name);
                Controls.Add(value);
                Controls.Add(set);
                Controls.Add(clear);
                top += 34;
            }

            status = new Label { Left = 12, Top = 184, Width = 446, Height = 52 };
            Controls.Add(status);
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
