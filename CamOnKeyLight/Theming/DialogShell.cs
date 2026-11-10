namespace CamOnKeyLight
{
    /// <summary>
    /// Dialog sizes are 96 DPI logical pixels. AutoScaleMode.Dpi scales them
    /// with the display, the width stays at the logical minimum, and the height
    /// grows to fit the content.
    /// </summary>
    internal static class DialogShell
    {
        public const int ButtonWidth = 88;
        public const int ButtonHeight = 28;

        public static void Prepare(Form form, string title, int logicalWidth)
        {
            form.SuspendLayout();
            form.Text = title;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.CenterParent;
            form.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            form.AutoScaleDimensions = new SizeF(96F, 96F);
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.AutoSize = true;
            form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            form.Padding = new Padding(16);
            form.MinimumSize = new Size(logicalWidth, 1);
        }

        public static int ContentWidth(int logicalWidth)
        {
            return logicalWidth - 32;
        }

        public static TableLayoutPanel Content(int logicalWidth)
        {
            int inner = ContentWidth(logicalWidth);
            TableLayoutPanel table = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Width = inner,
                MinimumSize = new Size(inner, 0),
                MaximumSize = new Size(inner, 8000),
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, inner));
            return table;
        }

        public static void AddRow(TableLayoutPanel table, Control control, Padding margin)
        {
            control.Margin = margin;
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            int row = table.RowCount;
            table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, row);
        }

        public static Label WrappingLabel(string text, int logicalWidth)
        {
            return new Label
            {
                AutoSize = true,
                MaximumSize = new Size(ContentWidth(logicalWidth), 8000),
                Text = text
            };
        }

        public static TextBox Field(string text, string accessibleName, int logicalWidth)
        {
            return new TextBox
            {
                Text = text,
                AccessibleName = accessibleName,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                MinimumSize = new Size(ContentWidth(logicalWidth), 0),
                Width = ContentWidth(logicalWidth)
            };
        }

        public static Button CommandButton(string text, DialogResult result)
        {
            return new Button
            {
                Text = text,
                DialogResult = result,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ButtonWidth, ButtonHeight),
                Size = new Size(ButtonWidth, ButtonHeight),
                Margin = new Padding(8, 12, 0, 0),
                UseVisualStyleBackColor = true
            };
        }

        public static FlowLayoutPanel ButtonRow(int logicalWidth, params Button[] rightToLeft)
        {
            FlowLayoutPanel row = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Width = ContentWidth(logicalWidth),
                MinimumSize = new Size(ContentWidth(logicalWidth), ButtonHeight),
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            foreach (Button button in rightToLeft)
                row.Controls.Add(button);
            return row;
        }
    }
}
