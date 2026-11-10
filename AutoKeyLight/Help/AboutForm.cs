namespace AutoKeyLight
{
    internal sealed class AboutForm : Form
    {
        private const int LogicalWidth = 460;

        public AboutForm(Version version)
        {
            DialogShell.Prepare(this, "About Auto Key Light", LogicalWidth);
            TableLayoutPanel table = DialogShell.Content(LogicalWidth);

            Label body = DialogShell.WrappingLabel(
                "Auto Key Light " + version.ToString(3) + "\r\n\r\n" +
                "Auto Key Light is an independent app. It turns Elgato lights on and off when the webcam turns on and off, and it can hold the lights on or off manually.\r\n\r\n" +
                "It is not made, sold, or endorsed by Elgato.\r\n\r\n" +
                "MIT License\r\nCopyright (c) 2022 Gabriel Garcia",
                LogicalWidth);
            LinkLabel link = new LinkLabel
            {
                AutoSize = true,
                Text = "https://www.elgato.com/",
                TabStop = true,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Margin = new Padding(0, 8, 0, 0)
            };
            link.LinkClicked += (_, _) => Program.OpenElgatoWebsite();
            Button close = DialogShell.CommandButton("Close", DialogResult.OK);
            AcceptButton = close;
            CancelButton = close;

            DialogShell.AddRow(table, body, new Padding(0, 0, 0, 8));
            DialogShell.AddRow(table, link, new Padding(0, 0, 0, 4));
            DialogShell.AddRow(table, DialogShell.ButtonRow(LogicalWidth, close), Padding.Empty);
            Controls.Add(table);
            ResumeLayout(true);
        }
    }
}
