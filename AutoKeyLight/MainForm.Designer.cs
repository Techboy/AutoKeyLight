namespace AutoKeyLight
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tmrCameraCheck = new System.Windows.Forms.Timer(components);
            menuMain = new MenuStrip();
            menuHelp = new ToolStripMenuItem();
            helpAbout = new ToolStripMenuItem();
            helpElgato = new ToolStripMenuItem();
            lvLights = new ListView();
            colLight = new ColumnHeader();
            colDescription = new ColumnHeader();
            colStatus = new ColumnHeader();
            btnAddLight = new Button();
            btnEditLight = new Button();
            btnRemoveLight = new Button();
            btnRetry = new Button();
            lblCameraState = new Label();
            lblLightState = new Label();
            lblDetail = new Label();
            grpTiming = new GroupBox();
            lblDelay = new Label();
            numUpDown = new NumericUpDown();
            lblOffDelay = new Label();
            numUpDownOff = new NumericUpDown();
            grpMode = new GroupBox();
            rbLightAuto = new RadioButton();
            rbLightOn = new RadioButton();
            rbLightOff = new RadioButton();
            lblMode = new Label();
            grpWindows = new GroupBox();
            chkStartWithWindows = new CheckBox();
            lblSignInHint = new Label();
            lblAppearance = new Label();
            cmbTheme = new ComboBox();
            grpUpdates = new GroupBox();
            chkAutomaticUpdates = new CheckBox();
            btnCheckUpdates = new Button();
            btnInstallUpdate = new Button();
            btnShortcuts = new Button();
            lblUpdate = new Label();
            niTray = new NotifyIcon(components);
            cmsTray = new ContextMenuStrip(components);
            tsmiAuto = new ToolStripMenuItem();
            tsmiOn = new ToolStripMenuItem();
            tsmiOff = new ToolStripMenuItem();
            sepTray = new ToolStripSeparator();
            tsmiShortcuts = new ToolStripMenuItem();
            tsmiOpen = new ToolStripMenuItem();
            tsmiExit = new ToolStripMenuItem();
            menuMain.SuspendLayout();
            grpTiming.SuspendLayout();
            grpMode.SuspendLayout();
            grpWindows.SuspendLayout();
            grpUpdates.SuspendLayout();
            cmsTray.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownOff).BeginInit();
            SuspendLayout();
            // 
            // tmrCameraCheck
            // 
            tmrCameraCheck.Enabled = true;
            tmrCameraCheck.Interval = 1000;
            tmrCameraCheck.Tick += tmrCameraCheck_Tick;
            // 
            // menuMain
            // 
            menuMain.Items.AddRange(new ToolStripItem[] { menuHelp });
            menuMain.Dock = DockStyle.Top;
            menuMain.Location = new Point(0, 0);
            menuMain.Name = "menuMain";
            menuMain.Size = new Size(544, 24);
            menuMain.TabIndex = 0;
            menuMain.Text = "Main menu";
            // 
            // menuHelp
            // 
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { helpAbout, helpElgato });
            menuHelp.Name = "menuHelp";
            menuHelp.Size = new Size(44, 20);
            menuHelp.Text = "&Help";
            // 
            // helpAbout
            // 
            helpAbout.Name = "helpAbout";
            helpAbout.Size = new Size(180, 22);
            helpAbout.Text = "&About Auto Key Light";
            helpAbout.Click += helpAbout_Click;
            // 
            // helpElgato
            // 
            helpElgato.Name = "helpElgato";
            helpElgato.Size = new Size(180, 22);
            helpElgato.Text = "&Elgato website";
            helpElgato.Click += helpElgato_Click;
            // 
            // lvLights
            // 
            lvLights.Columns.AddRange(new ColumnHeader[] { colLight, colDescription, colStatus });
            lvLights.FullRowSelect = true;
            lvLights.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvLights.HideSelection = false;
            lvLights.Location = new Point(12, 32);
            lvLights.MultiSelect = false;
            lvLights.Name = "lvLights";
            lvLights.Size = new Size(520, 112);
            lvLights.TabIndex = 1;
            lvLights.UseCompatibleStateImageBehavior = false;
            lvLights.View = View.Details;
            lvLights.AccessibleName = "Lights";
            lvLights.SelectedIndexChanged += lvLights_SelectedIndexChanged;
            lvLights.DoubleClick += lvLights_DoubleClick;
            lvLights.KeyDown += lvLights_KeyDown;
            lvLights.DrawColumnHeader += lvLights_DrawColumnHeader;
            lvLights.DrawItem += lvLights_DrawItem;
            lvLights.DrawSubItem += lvLights_DrawSubItem;
            // 
            // colLight
            // 
            colLight.Text = "Light";
            colLight.Width = 150;
            // 
            // colDescription
            // 
            colDescription.Text = "Description";
            colDescription.Width = 220;
            // 
            // colStatus
            // 
            colStatus.Text = "Status";
            colStatus.Width = 140;
            // 
            // btnAddLight
            // 
            btnAddLight.Location = new Point(12, 150);
            btnAddLight.Name = "btnAddLight";
            btnAddLight.Size = new Size(96, 28);
            btnAddLight.TabIndex = 2;
            btnAddLight.Text = "&Add";
            btnAddLight.UseVisualStyleBackColor = true;
            btnAddLight.Click += btnAddLight_Click;
            // 
            // btnEditLight
            // 
            btnEditLight.Enabled = false;
            btnEditLight.Location = new Point(114, 150);
            btnEditLight.Name = "btnEditLight";
            btnEditLight.Size = new Size(96, 28);
            btnEditLight.TabIndex = 3;
            btnEditLight.Text = "&Edit";
            btnEditLight.UseVisualStyleBackColor = true;
            btnEditLight.Click += btnEditLight_Click;
            // 
            // btnRemoveLight
            // 
            btnRemoveLight.Enabled = false;
            btnRemoveLight.Location = new Point(216, 150);
            btnRemoveLight.Name = "btnRemoveLight";
            btnRemoveLight.Size = new Size(96, 28);
            btnRemoveLight.TabIndex = 4;
            btnRemoveLight.Text = "&Remove";
            btnRemoveLight.UseVisualStyleBackColor = true;
            btnRemoveLight.Click += btnRemoveLight_Click;
            // 
            // btnRetry
            // 
            btnRetry.Location = new Point(318, 150);
            btnRetry.Name = "btnRetry";
            btnRetry.Size = new Size(96, 28);
            btnRetry.TabIndex = 5;
            btnRetry.Text = "Re&try";
            btnRetry.UseVisualStyleBackColor = true;
            btnRetry.Click += btnRetry_Click;
            // 
            // lblCameraState
            // 
            lblCameraState.AutoSize = true;
            lblCameraState.Location = new Point(12, 186);
            lblCameraState.Name = "lblCameraState";
            lblCameraState.Size = new Size(93, 15);
            lblCameraState.TabIndex = 6;
            lblCameraState.Text = "Camera is OFF";
            // 
            // lblLightState
            // 
            lblLightState.AutoSize = true;
            lblLightState.Location = new Point(160, 186);
            lblLightState.Name = "lblLightState";
            lblLightState.Size = new Size(88, 15);
            lblLightState.TabIndex = 7;
            lblLightState.Text = "No lights added";
            // 
            // lblDetail
            // 
            lblDetail.AutoSize = true;
            lblDetail.Location = new Point(12, 206);
            lblDetail.MaximumSize = new Size(520, 18);
            lblDetail.Name = "lblDetail";
            lblDetail.Size = new Size(0, 15);
            lblDetail.TabIndex = 8;
            lblDetail.Visible = false;
            // 
            // grpTiming
            // 
            grpTiming.Controls.Add(lblDelay);
            grpTiming.Controls.Add(numUpDown);
            grpTiming.Controls.Add(lblOffDelay);
            grpTiming.Controls.Add(numUpDownOff);
            grpTiming.Location = new Point(12, 240);
            grpTiming.Name = "grpTiming";
            grpTiming.Size = new Size(520, 56);
            grpTiming.TabIndex = 9;
            grpTiming.TabStop = false;
            grpTiming.Text = "Timing";
            // 
            // lblDelay
            // 
            lblDelay.AutoSize = true;
            lblDelay.Location = new Point(12, 24);
            lblDelay.Name = "lblDelay";
            lblDelay.Size = new Size(82, 15);
            lblDelay.TabIndex = 0;
            lblDelay.Text = "On delay (ms)";
            // 
            // numUpDown
            // 
            numUpDown.Location = new Point(104, 20);
            numUpDown.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numUpDown.Name = "numUpDown";
            numUpDown.Size = new Size(84, 23);
            numUpDown.TabIndex = 1;
            numUpDown.AccessibleName = "On delay milliseconds";
            numUpDown.ValueChanged += numUpDown_ValueChanged;
            // 
            // lblOffDelay
            // 
            lblOffDelay.AutoSize = true;
            lblOffDelay.Location = new Point(220, 24);
            lblOffDelay.Name = "lblOffDelay";
            lblOffDelay.Size = new Size(87, 15);
            lblOffDelay.TabIndex = 2;
            lblOffDelay.Text = "Off delay (ms)";
            // 
            // numUpDownOff
            // 
            numUpDownOff.Location = new Point(316, 20);
            numUpDownOff.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            numUpDownOff.Name = "numUpDownOff";
            numUpDownOff.Size = new Size(84, 23);
            numUpDownOff.TabIndex = 3;
            numUpDownOff.AccessibleName = "Off delay milliseconds";
            numUpDownOff.ValueChanged += numUpDownOff_ValueChanged;
            // 
            // grpMode
            // 
            grpMode.Controls.Add(rbLightAuto);
            grpMode.Controls.Add(rbLightOn);
            grpMode.Controls.Add(rbLightOff);
            grpMode.Controls.Add(lblMode);
            grpMode.Location = new Point(12, 304);
            grpMode.Name = "grpMode";
            grpMode.Size = new Size(520, 72);
            grpMode.TabIndex = 10;
            grpMode.TabStop = false;
            grpMode.Text = "Light control";
            // 
            // rbLightAuto
            // 
            rbLightAuto.AutoSize = true;
            rbLightAuto.Checked = true;
            rbLightAuto.Location = new Point(12, 22);
            rbLightAuto.Name = "rbLightAuto";
            rbLightAuto.Size = new Size(51, 19);
            rbLightAuto.TabIndex = 0;
            rbLightAuto.TabStop = true;
            rbLightAuto.Text = "Auto";
            rbLightAuto.UseVisualStyleBackColor = true;
            rbLightAuto.CheckedChanged += LightMode_CheckedChanged;
            // 
            // rbLightOn
            // 
            rbLightOn.AutoSize = true;
            rbLightOn.Location = new Point(110, 22);
            rbLightOn.Name = "rbLightOn";
            rbLightOn.Size = new Size(41, 19);
            rbLightOn.TabIndex = 1;
            rbLightOn.Text = "On";
            rbLightOn.UseVisualStyleBackColor = true;
            rbLightOn.CheckedChanged += LightMode_CheckedChanged;
            // 
            // rbLightOff
            // 
            rbLightOff.AutoSize = true;
            rbLightOff.Location = new Point(190, 22);
            rbLightOff.Name = "rbLightOff";
            rbLightOff.Size = new Size(42, 19);
            rbLightOff.TabIndex = 2;
            rbLightOff.Text = "Off";
            rbLightOff.UseVisualStyleBackColor = true;
            rbLightOff.CheckedChanged += LightMode_CheckedChanged;
            // 
            // lblMode
            // 
            lblMode.AutoSize = true;
            lblMode.Location = new Point(12, 46);
            lblMode.MaximumSize = new Size(490, 0);
            lblMode.Name = "lblMode";
            lblMode.Size = new Size(184, 15);
            lblMode.TabIndex = 3;
            lblMode.Text = "Auto — lights follow the webcam";
            // 
            // grpWindows
            // 
            grpWindows.Controls.Add(chkStartWithWindows);
            grpWindows.Controls.Add(lblSignInHint);
            grpWindows.Controls.Add(lblAppearance);
            grpWindows.Controls.Add(cmbTheme);
            grpWindows.Location = new Point(12, 384);
            grpWindows.Name = "grpWindows";
            grpWindows.Size = new Size(520, 78);
            grpWindows.TabIndex = 11;
            grpWindows.TabStop = false;
            grpWindows.Text = "This PC";
            // 
            // chkStartWithWindows
            // 
            chkStartWithWindows.AutoSize = true;
            chkStartWithWindows.Location = new Point(12, 22);
            chkStartWithWindows.Name = "chkStartWithWindows";
            chkStartWithWindows.Size = new Size(154, 19);
            chkStartWithWindows.TabIndex = 0;
            chkStartWithWindows.Text = "Start when I &sign in";
            chkStartWithWindows.UseVisualStyleBackColor = true;
            chkStartWithWindows.CheckedChanged += chkStartWithWindows_CheckedChanged;
            // 
            // lblSignInHint
            // 
            lblSignInHint.AutoSize = true;
            lblSignInHint.Location = new Point(12, 46);
            lblSignInHint.Name = "lblSignInHint";
            lblSignInHint.Size = new Size(280, 15);
            lblSignInHint.TabIndex = 1;
            lblSignInHint.Text = "Runs after you sign in, not before Windows login.";
            // 
            // lblAppearance
            // 
            lblAppearance.AutoSize = true;
            lblAppearance.Location = new Point(300, 24);
            lblAppearance.Name = "lblAppearance";
            lblAppearance.Size = new Size(70, 15);
            lblAppearance.TabIndex = 2;
            lblAppearance.Text = "Appearance";
            // 
            // cmbTheme
            // 
            cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTheme.Items.AddRange(new object[] { "Light", "Dark", "Follow Windows" });
            cmbTheme.Location = new Point(380, 20);
            cmbTheme.Name = "cmbTheme";
            cmbTheme.Size = new Size(124, 23);
            cmbTheme.TabIndex = 3;
            cmbTheme.AccessibleName = "Appearance";
            cmbTheme.SelectedIndexChanged += cmbTheme_SelectedIndexChanged;
            cmbTheme.DrawItem += cmbTheme_DrawItem;
            // 
            // grpUpdates
            // 
            grpUpdates.Controls.Add(chkAutomaticUpdates);
            grpUpdates.Controls.Add(btnCheckUpdates);
            grpUpdates.Controls.Add(btnInstallUpdate);
            grpUpdates.Controls.Add(btnShortcuts);
            grpUpdates.Controls.Add(lblUpdate);
            grpUpdates.Location = new Point(12, 470);
            grpUpdates.Name = "grpUpdates";
            grpUpdates.Size = new Size(520, 124);
            grpUpdates.TabIndex = 12;
            grpUpdates.TabStop = false;
            grpUpdates.Text = "Updates";
            // 
            // chkAutomaticUpdates
            // 
            chkAutomaticUpdates.AutoSize = true;
            chkAutomaticUpdates.Location = new Point(12, 22);
            chkAutomaticUpdates.Name = "chkAutomaticUpdates";
            chkAutomaticUpdates.Size = new Size(184, 19);
            chkAutomaticUpdates.TabIndex = 0;
            chkAutomaticUpdates.Text = "Install updates automatically";
            chkAutomaticUpdates.UseVisualStyleBackColor = true;
            chkAutomaticUpdates.CheckedChanged += chkAutomaticUpdates_CheckedChanged;
            // 
            // btnCheckUpdates
            // 
            btnCheckUpdates.Location = new Point(12, 48);
            btnCheckUpdates.Name = "btnCheckUpdates";
            btnCheckUpdates.Size = new Size(140, 28);
            btnCheckUpdates.TabIndex = 1;
            btnCheckUpdates.Text = "&Check for updates";
            btnCheckUpdates.UseVisualStyleBackColor = true;
            btnCheckUpdates.Click += btnCheckUpdates_Click;
            // 
            // btnInstallUpdate
            // 
            btnInstallUpdate.Enabled = false;
            btnInstallUpdate.Location = new Point(158, 48);
            btnInstallUpdate.Name = "btnInstallUpdate";
            btnInstallUpdate.Size = new Size(140, 28);
            btnInstallUpdate.TabIndex = 2;
            btnInstallUpdate.Text = "&Install update";
            btnInstallUpdate.UseVisualStyleBackColor = true;
            btnInstallUpdate.Click += btnInstallUpdate_Click;
            // 
            // btnShortcuts
            // 
            btnShortcuts.Location = new Point(304, 48);
            btnShortcuts.Name = "btnShortcuts";
            btnShortcuts.Size = new Size(140, 28);
            btnShortcuts.TabIndex = 3;
            btnShortcuts.Text = "S&hortcuts";
            btnShortcuts.UseVisualStyleBackColor = true;
            btnShortcuts.Click += btnShortcuts_Click;
            // 
            // lblUpdate
            // 
            lblUpdate.AutoSize = true;
            lblUpdate.Location = new Point(12, 84);
            lblUpdate.MaximumSize = new Size(496, 48);
            lblUpdate.Name = "lblUpdate";
            lblUpdate.Size = new Size(0, 15);
            lblUpdate.TabIndex = 4;
            // 
            // niTray
            // 
            niTray.ContextMenuStrip = cmsTray;
            niTray.Text = "Auto Key Light";
            niTray.Visible = true;
            niTray.DoubleClick += niTray_DoubleClick;
            // 
            // cmsTray
            // 
            cmsTray.Items.AddRange(new ToolStripItem[] { tsmiAuto, tsmiOn, tsmiOff, sepTray, tsmiShortcuts, tsmiOpen, tsmiExit });
            cmsTray.Name = "cmsTray";
            cmsTray.Size = new Size(160, 142);
            cmsTray.ItemClicked += cmsTray_ItemClicked;
            // 
            // tsmiAuto
            // 
            tsmiAuto.Checked = true;
            tsmiAuto.Name = "tsmiAuto";
            tsmiAuto.Size = new Size(159, 22);
            tsmiAuto.Text = "Auto";
            // 
            // tsmiOn
            // 
            tsmiOn.Name = "tsmiOn";
            tsmiOn.Size = new Size(159, 22);
            tsmiOn.Text = "On";
            // 
            // tsmiOff
            // 
            tsmiOff.Name = "tsmiOff";
            tsmiOff.Size = new Size(159, 22);
            tsmiOff.Text = "Off";
            // 
            // sepTray
            // 
            sepTray.Name = "sepTray";
            sepTray.Size = new Size(156, 6);
            // 
            // tsmiShortcuts
            // 
            tsmiShortcuts.Name = "tsmiShortcuts";
            tsmiShortcuts.Size = new Size(159, 22);
            tsmiShortcuts.Text = "Shortcuts…";
            // 
            // tsmiOpen
            // 
            tsmiOpen.Name = "tsmiOpen";
            tsmiOpen.Size = new Size(159, 22);
            tsmiOpen.Text = "Open";
            // 
            // tsmiExit
            // 
            tsmiExit.Name = "tsmiExit";
            tsmiExit.Size = new Size(159, 22);
            tsmiExit.Text = "Exit";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(544, 606);
            Controls.Add(grpUpdates);
            Controls.Add(grpWindows);
            Controls.Add(grpMode);
            Controls.Add(grpTiming);
            Controls.Add(lblDetail);
            Controls.Add(lblLightState);
            Controls.Add(lblCameraState);
            Controls.Add(btnRetry);
            Controls.Add(btnRemoveLight);
            Controls.Add(btnEditLight);
            Controls.Add(btnAddLight);
            Controls.Add(lvLights);
            Controls.Add(menuMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MainMenuStrip = menuMain;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Auto Key Light";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            menuMain.ResumeLayout(false);
            menuMain.PerformLayout();
            grpTiming.ResumeLayout(false);
            grpTiming.PerformLayout();
            grpMode.ResumeLayout(false);
            grpMode.PerformLayout();
            grpWindows.ResumeLayout(false);
            grpWindows.PerformLayout();
            grpUpdates.ResumeLayout(false);
            grpUpdates.PerformLayout();
            cmsTray.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownOff).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Timer tmrCameraCheck;
        private MenuStrip menuMain;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem helpAbout;
        private ToolStripMenuItem helpElgato;
        private ListView lvLights;
        private ColumnHeader colLight;
        private ColumnHeader colDescription;
        private ColumnHeader colStatus;
        private Button btnAddLight;
        private Button btnEditLight;
        private Button btnRemoveLight;
        private Button btnRetry;
        private Label lblCameraState;
        private Label lblLightState;
        private Label lblDetail;
        private GroupBox grpTiming;
        private Label lblDelay;
        private NumericUpDown numUpDown;
        private Label lblOffDelay;
        private NumericUpDown numUpDownOff;
        private GroupBox grpMode;
        private RadioButton rbLightAuto;
        private RadioButton rbLightOn;
        private RadioButton rbLightOff;
        private Label lblMode;
        private GroupBox grpWindows;
        private CheckBox chkStartWithWindows;
        private Label lblSignInHint;
        private Label lblAppearance;
        private ComboBox cmbTheme;
        private GroupBox grpUpdates;
        private CheckBox chkAutomaticUpdates;
        private Button btnCheckUpdates;
        private Button btnInstallUpdate;
        private Button btnShortcuts;
        private Label lblUpdate;
        private NotifyIcon niTray;
        private ContextMenuStrip cmsTray;
        private ToolStripMenuItem tsmiAuto;
        private ToolStripMenuItem tsmiOn;
        private ToolStripMenuItem tsmiOff;
        private ToolStripSeparator sepTray;
        private ToolStripMenuItem tsmiShortcuts;
        private ToolStripMenuItem tsmiOpen;
        private ToolStripMenuItem tsmiExit;
    }
}
