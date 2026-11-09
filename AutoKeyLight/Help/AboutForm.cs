namespace AutoKeyLight
{
    internal sealed class AboutForm : Form
    {
        public AboutForm(Version version, Font font, int buttonHeight)
        {
            DialogShell.Prepare(this, "About Auto Key Light", font, buttonHeight);
            int fieldWidth = Math.Max(360, buttonHeight * 12);
            TableLayoutPanel table = DialogShell.Content();

            Label body = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(fieldWidth, 0),
                Text = "Auto Key Light " + version.ToString(3) + "\r\n\r\n" +
                    "Auto Key Light is an independent app. It turns Elgato lights on and off when the webcam turns on and off, and it can hold the lights on or off manually.\r\n\r\n" +
                    "It is not made, sold, or endorsed by Elgato.\r\n\r\n" +
                    "MIT License\r\nCopyright (c) 2022 Gabriel Garcia"
            };
            LinkLabel link = new LinkLabel
            {
                AutoSize = true,
                Text = "https://www.elgato.com/",
                TabStop = true,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            link.LinkClicked += (_, _) => Program.OpenElgatoWebsite();
            Button close = DialogShell.CommandButton("Close", DialogResult.OK, buttonHeight);
            AcceptButton = close;
            CancelButton = close;

            DialogShell.AddRow(table, body, new Padding(0, 0, 0, buttonHeight / 3));
            DialogShell.AddRow(table, link, new Padding(0, 0, 0, buttonHeight / 4));
            DialogShell.AddRow(table, DialogShell.ButtonRow(true, close), Padding.Empty);
            Controls.Add(table);
            ResumeLayout(true);
        }
    }
}
