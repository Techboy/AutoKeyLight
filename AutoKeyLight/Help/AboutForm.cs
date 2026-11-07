namespace AutoKeyLight
{
    internal sealed class AboutForm : Form
    {
        public AboutForm(Version version)
        {
            Text = "About Auto Key Light";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 280);
            Font = SystemFonts.MessageBoxFont;

            Label body = new Label
            {
                Left = 16,
                Top = 16,
                Width = 428,
                Height = 150,
                Text = "Auto Key Light " + version.ToString(3) + "\r\n\r\n" +
                    "Auto Key Light is an independent app. It turns Elgato lights on and off when the webcam turns on and off, and it can hold the lights on or off manually.\r\n\r\n" +
                    "It is not made, sold, or endorsed by Elgato.\r\n\r\n" +
                    "MIT License\r\nCopyright (c) 2022 Gabriel Garcia"
            };
            LinkLabel link = new LinkLabel
            {
                Left = 16,
                Top = 176,
                Width = 428,
                Height = 24,
                Text = "https://www.elgato.com/",
                TabStop = true
            };
            link.LinkClicked += (_, _) => Program.OpenElgatoWebsite();
            Button close = new Button
            {
                Text = "Close",
                Left = 364,
                Top = 232,
                Width = 80,
                Height = 28,
                DialogResult = DialogResult.OK
            };
            AcceptButton = close;
            CancelButton = close;
            Controls.Add(body);
            Controls.Add(link);
            Controls.Add(close);
        }
    }
}
