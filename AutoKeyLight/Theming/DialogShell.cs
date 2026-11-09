namespace AutoKeyLight
{
    /// <summary>
    /// Pop-up dialogs size to their content. Button height is the main window's
    /// already DPI-scaled button height, so OK, Cancel, and Close are not clipped.
    /// </summary>
    internal static class DialogShell
    {
        public static void Prepare(Form form, string title, Font font, int buttonHeight)
        {
            form.SuspendLayout();
            form.Text = title;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.CenterParent;
            form.AutoScaleMode = AutoScaleMode.None;
            form.Font = font;
            form.AutoSize = true;
            form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            int pad = Math.Max(12, buttonHeight / 2);
            form.Padding = new Padding(pad);
            form.Tag = buttonHeight;
        }

        public static int ButtonHeight(Form form)
        {
            return form.Tag is int height && height > 0 ? height : 28;
        }

        public static TableLayoutPanel Content()
        {
            TableLayoutPanel table = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return table;
        }

        public static void AddRow(TableLayoutPanel table, Control control, Padding margin)
        {
            control.Margin = margin;
            control.Dock = DockStyle.Fill;
            int row = table.RowCount;
            table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, row);
        }

        public static Button CommandButton(string text, DialogResult result, int buttonHeight)
        {
            return new Button
            {
                Text = text,
                DialogResult = result,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(Math.Max(88, buttonHeight * 3), buttonHeight),
                Margin = new Padding(8, buttonHeight / 2, 0, 0),
                UseVisualStyleBackColor = true
            };
        }

        public static FlowLayoutPanel ButtonRow(bool alignRight, params Button[] buttons)
        {
            FlowLayoutPanel row = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = alignRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            foreach (Button button in buttons)
                row.Controls.Add(button);
            return row;
        }
    }
}
