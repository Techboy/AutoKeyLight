using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal sealed class FindLightsForm : Form
    {
        private readonly IReadOnlyList<string> existing;
        private readonly bool single;
        private readonly ListView list;
        private readonly Label status;
        private readonly Button use;
        private CancellationTokenSource search = new CancellationTokenSource();
        private int searchGeneration;
        private bool closed;

        public FindLightsForm(IReadOnlyList<string> existingAddresses, bool single)
        {
            existing = existingAddresses;
            this.single = single;
            const int logicalWidth = 480;
            DialogShell.Prepare(this, "Find lights", logicalWidth);
            int inner = DialogShell.ContentWidth(logicalWidth);
            list = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = !single,
                HideSelection = false,
                Width = inner,
                Height = 180,
                MinimumSize = new Size(inner, 180),
                AccessibleName = "Discovered lights"
            };
            list.Columns.Add("Name", 200);
            list.Columns.Add("Address", 140);
            list.Columns.Add("Port", 70);
            status = DialogShell.WrappingLabel("Searching the network…", logicalWidth);
            use = DialogShell.CommandButton(single ? "Use" : "Add", DialogResult.None);
            Button again = DialogShell.CommandButton("Search again", DialogResult.None);
            Button cancel = DialogShell.CommandButton("Cancel", DialogResult.Cancel);
            use.Click += (_, _) => Accept();
            again.Click += async (_, _) => await SearchAsync();
            AcceptButton = use;
            CancelButton = cancel;
            TableLayoutPanel table = DialogShell.Content(logicalWidth);
            DialogShell.AddRow(table, list, new Padding(0, 0, 0, 8));
            DialogShell.AddRow(table, status, new Padding(0, 0, 0, 4));
            DialogShell.AddRow(table, DialogShell.ButtonRow(logicalWidth, cancel, again, use), Padding.Empty);
            Controls.Add(table);
            ResumeLayout(true);
        }

        public IReadOnlyList<DiscoveredLight> Chosen { get; private set; } = Array.Empty<DiscoveredLight>();

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                await SearchAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                try
                {
                    if (closed || IsDisposed || !IsHandleCreated)
                        return;

                    status.Text = "Couldn't search the network. " + ex.Message;
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            closed = true;
            try
            {
                search.Cancel();
                search.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }

            base.OnFormClosed(e);
        }

        private async Task SearchAsync()
        {
            int generation = ++searchGeneration;
            CancellationTokenSource previous = search;
            search = new CancellationTokenSource();
            CancellationToken token = search.Token;
            try
            {
                previous.Cancel();
                previous.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }

            list.Items.Clear();
            status.Text = "Searching the network…";
            use.Enabled = false;
            try
            {
                DiscoveryResult result = await ElgatoBrowser.SearchAsync(TimeSpan.FromSeconds(3), token);
                if (closed || IsDisposed || generation != searchGeneration)
                    return;

                IReadOnlyList<DiscoveredLight> fresh = ElgatoDiscovery.ExceptAdded(result.Lights, existing);
                foreach (DiscoveredLight light in fresh)
                {
                    ListViewItem item = new ListViewItem(light.Name);
                    item.SubItems.Add(light.Address);
                    item.SubItems.Add(light.Port.ToString());
                    item.Tag = light;
                    list.Items.Add(item);
                }

                int skipped = result.Lights.Count - fresh.Count;
                if (result.Error != null && fresh.Count == 0)
                    status.Text = result.Error;
                else if (fresh.Count == 0 && skipped == 0)
                    status.Text = "No Elgato lights answered. They need to be on this network.";
                else if (fresh.Count == 0)
                    status.Text = "No new lights. " + skipped + " already added.";
                else
                    status.Text = fresh.Count + (fresh.Count == 1 ? " light." : " lights.") + (skipped == 0 ? "" : " " + skipped + " already added.");

                use.Enabled = fresh.Count > 0;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void Accept()
        {
            List<DiscoveredLight> chosen = new List<DiscoveredLight>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                if (item.Tag is DiscoveredLight light)
                    chosen.Add(light);
            }

            if (chosen.Count == 0)
            {
                status.Text = "Select a light first.";
                return;
            }

            Chosen = chosen;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
