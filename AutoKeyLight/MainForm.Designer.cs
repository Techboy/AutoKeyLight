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
            lvLights = new ListView();
            colLight = new ColumnHeader();
            colStatus = new ColumnHeader();
            btnAddLight = new Button();
            btnRemoveLight = new Button();
            btnRetry = new Button();
            lblCameraState = new Label();
            lblLightState = new Label();
            lblDetail = new Label();
            lblDelay = new Label();
            numUpDown = new NumericUpDown();
            lblOffDelay = new Label();
            numUpDownOff = new NumericUpDown();
            grpMode = new GroupBox();
            rbLightAuto = new RadioButton();
            rbLightOn = new RadioButton();
            rbLightOff = new RadioButton();
            lblMode = new Label();
            chkStartWithWindows = new CheckBox();
            lblAppearance = new Label();
            cmbTheme = new ComboBox();
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
            cmsTray.SuspendLayout();
            grpMode.SuspendLayout();
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
            // lvLights
            // 
            lvLights.Columns.AddRange(new ColumnHeader[] { colLight, colStatus });
            lvLights.FullRowSelect = true;
            lvLights.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvLights.Location = new Point(12, 12);
            lvLights.MultiSelect = false;
            lvLights.Name = "lvLights";
            lvLights.Size = new Size(456, 112);
            lvLights.TabIndex = 0;
            lvLights.UseCompatibleStateImageBehavior = false;
            lvLights.View = View.Details;
            lvLights.SelectedIndexChanged += lvLights_SelectedIndexChanged;
            // 
            // colLight
            // 
            colLight.Text = "Light";
            colLight.Width = 210;
            // 
            // colStatus
            // 
            colStatus.Text = "Status";
            colStatus.Width = 220;
            // 
            // btnAddLight
            // 
            btnAddLight.Location = new Point(12, 130);
            btnAddLight.Name = "btnAddLight";
            btnAddLight.Size = new Size(88, 28);
            btnAddLight.TabIndex = 1;
            btnAddLight.Text = "Add";
            btnAddLight.UseVisualStyleBackColor = true;
            btnAddLight.Click += btnAddLight_Click;
            // 
            // btnRemoveLight
            // 
            btnRemoveLight.Enabled = false;
            btnRemoveLight.Location = new Point(106, 130);
            btnRemoveLight.Name = "btnRemoveLight";
            btnRemoveLight.Size = new Size(88, 28);
            btnRemoveLight.TabIndex = 2;
            btnRemoveLight.Text = "Remove";
            btnRemoveLight.UseVisualStyleBackColor = true;
            btnRemoveLight.Click += btnRemoveLight_Click;
            // 
            // btnRetry
            // 
            btnRetry.Location = new Point(200, 130);
            btnRetry.Name = "btnRetry";
            btnRetry.Size = new Size(88, 28);
            btnRetry.TabIndex = 3;
            btnRetry.Text = "Retry";
            btnRetry.UseVisualStyleBackColor = true;
            btnRetry.Click += btnRetry_Click;
            // 
            // lblCameraState
            // 
            lblCameraState.AutoSize = true;
            lblCameraState.Location = new Point(12, 168);
            lblCameraState.Name = "lblCameraState";
            lblCameraState.Size = new Size(93, 15);
            lblCameraState.TabIndex = 4;
            lblCameraState.Text = "Camera is OFF";
            // 
            // lblLightState
            // 
            lblLightState.AutoSize = true;
            lblLightState.Location = new Point(12, 188);
            lblLightState.Name = "lblLightState";
            lblLightState.Size = new Size(88, 15);
            lblLightState.TabIndex = 5;
            lblLightState.Text = "No lights added";
            // 
            // lblDetail
            // 
            lblDetail.AutoSize = true;
            lblDetail.Location = new Point(12, 208);
            lblDetail.MaximumSize = new Size(456, 36);
            lblDetail.Name = "lblDetail";
            lblDetail.Size = new Size(0, 15);
            lblDetail.TabIndex = 6;
            lblDetail.Visible = false;
            // 
            // lblDelay
            // 
            lblDelay.AutoSize = true;
            lblDelay.Location = new Point(12, 250);
            lblDelay.Name = "lblDelay";
            lblDelay.Size = new Size(82, 15);
            lblDelay.TabIndex = 7;
            lblDelay.Text = "On delay (ms)";
            // 
            // numUpDown
            // 
            numUpDown.Location = new Point(148, 246);
            numUpDown.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numUpDown.Name = "numUpDown";
            numUpDown.Size = new Size(90, 23);
            numUpDown.TabIndex = 8;
            numUpDown.ValueChanged += numUpDown_ValueChanged;
            // 
            // lblOffDelay
            // 
            lblOffDelay.AutoSize = true;
            lblOffDelay.Location = new Point(12, 280);
            lblOffDelay.Name = "lblOffDelay";
            lblOffDelay.Size = new Size(87, 15);
            lblOffDelay.TabIndex = 9;
            lblOffDelay.Text = "Off delay (ms)";
            // 
            // numUpDownOff
            // 
            numUpDownOff.Location = new Point(148, 276);
            numUpDownOff.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            numUpDownOff.Name = "numUpDownOff";
            numUpDownOff.Size = new Size(90, 23);
            numUpDownOff.TabIndex = 10;
            numUpDownOff.ValueChanged += numUpDownOff_ValueChanged;
            // 
            // grpMode
            // 
            grpMode.Controls.Add(rbLightAuto);
            grpMode.Controls.Add(rbLightOn);
            grpMode.Controls.Add(rbLightOff);
            grpMode.Controls.Add(lblMode);
            grpMode.Location = new Point(12, 310);
            grpMode.Name = "grpMode";
            grpMode.Size = new Size(456, 86);
            grpMode.TabIndex = 11;
            grpMode.TabStop = false;
            grpMode.Text = "Light control";
            // 
            // rbLightAuto
            // 
            rbLightAuto.AutoSize = true;
            rbLightAuto.Checked = true;
            rbLightAuto.Location = new Point(16, 24);
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
            rbLightOn.Location = new Point(130, 24);
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
            rbLightOff.Location = new Point(230, 24);
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
            lblMode.Location = new Point(16, 54);
            lblMode.MaximumSize = new Size(424, 0);
            lblMode.Name = "lblMode";
            lblMode.Size = new Size(184, 15);
            lblMode.TabIndex = 3;
            lblMode.Text = "Auto — lights follow the webcam";
            // 
            // chkStartWithWindows
            // 
            chkStartWithWindows.AutoSize = true;
            chkStartWithWindows.Location = new Point(12, 408);
            chkStartWithWindows.Name = "chkStartWithWindows";
            chkStartWithWindows.Size = new Size(128, 19);
            chkStartWithWindows.TabIndex = 12;
            chkStartWithWindows.Text = "Start with Windows";
            chkStartWithWindows.UseVisualStyleBackColor = true;
            chkStartWithWindows.CheckedChanged += chkStartWithWindows_CheckedChanged;
            // 
            // lblAppearance
            // 
            lblAppearance.AutoSize = true;
            lblAppearance.Location = new Point(12, 440);
            lblAppearance.Name = "lblAppearance";
            lblAppearance.Size = new Size(70, 15);
            lblAppearance.TabIndex = 13;
            lblAppearance.Text = "Appearance";
            // 
            // cmbTheme
            // 
            cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTheme.Items.AddRange(new object[] { "Light", "Dark", "Follow Windows" });
            cmbTheme.Location = new Point(148, 436);
            cmbTheme.Name = "cmbTheme";
            cmbTheme.Size = new Size(180, 23);
            cmbTheme.TabIndex = 14;
            cmbTheme.SelectedIndexChanged += cmbTheme_SelectedIndexChanged;
            // 
            // chkAutomaticUpdates
            // 
            chkAutomaticUpdates.AutoSize = true;
            chkAutomaticUpdates.Location = new Point(12, 472);
            chkAutomaticUpdates.Name = "chkAutomaticUpdates";
            chkAutomaticUpdates.Size = new Size(184, 19);
            chkAutomaticUpdates.TabIndex = 15;
            chkAutomaticUpdates.Text = "Install updates automatically";
            chkAutomaticUpdates.UseVisualStyleBackColor = true;
            chkAutomaticUpdates.CheckedChanged += chkAutomaticUpdates_CheckedChanged;
            // 
            // btnCheckUpdates
            // 
            btnCheckUpdates.Location = new Point(12, 500);
            btnCheckUpdates.Name = "btnCheckUpdates";
            btnCheckUpdates.Size = new Size(140, 28);
            btnCheckUpdates.TabIndex = 16;
            btnCheckUpdates.Text = "Check for updates";
            btnCheckUpdates.UseVisualStyleBackColor = true;
            btnCheckUpdates.Click += btnCheckUpdates_Click;
            // 
            // btnInstallUpdate
            // 
            btnInstallUpdate.Enabled = false;
            btnInstallUpdate.Location = new Point(160, 500);
            btnInstallUpdate.Name = "btnInstallUpdate";
            btnInstallUpdate.Size = new Size(140, 28);
            btnInstallUpdate.TabIndex = 17;
            btnInstallUpdate.Text = "Install update";
            btnInstallUpdate.UseVisualStyleBackColor = true;
            btnInstallUpdate.Click += btnInstallUpdate_Click;
            // 
            // btnShortcuts
            // 
            btnShortcuts.Location = new Point(308, 500);
            btnShortcuts.Name = "btnShortcuts";
            btnShortcuts.Size = new Size(160, 28);
            btnShortcuts.TabIndex = 18;
            btnShortcuts.Text = "Shortcuts";
            btnShortcuts.UseVisualStyleBackColor = true;
            btnShortcuts.Click += btnShortcuts_Click;
            // 
            // lblUpdate
            // 
            lblUpdate.AutoSize = true;
            lblUpdate.Location = new Point(12, 538);
            lblUpdate.MaximumSize = new Size(456, 80);
            lblUpdate.Name = "lblUpdate";
            lblUpdate.Size = new Size(0, 15);
            lblUpdate.TabIndex = 19;
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
            cmsTray.ImageScalingSize = new Size(24, 24);
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
            ClientSize = new Size(480, 630);
            Controls.Add(lblUpdate);
            Controls.Add(btnShortcuts);
            Controls.Add(btnInstallUpdate);
            Controls.Add(btnCheckUpdates);
            Controls.Add(chkAutomaticUpdates);
            Controls.Add(cmbTheme);
            Controls.Add(lblAppearance);
            Controls.Add(chkStartWithWindows);
            Controls.Add(grpMode);
            Controls.Add(numUpDownOff);
            Controls.Add(lblOffDelay);
            Controls.Add(numUpDown);
            Controls.Add(lblDelay);
            Controls.Add(lblDetail);
            Controls.Add(lblLightState);
            Controls.Add(lblCameraState);
            Controls.Add(btnRetry);
            Controls.Add(btnRemoveLight);
            Controls.Add(btnAddLight);
            Controls.Add(lvLights);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Auto Key Light";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            cmsTray.ResumeLayout(false);
            grpMode.ResumeLayout(false);
            grpMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownOff).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Timer tmrCameraCheck;
        private ListView lvLights;
        private ColumnHeader colLight;
        private ColumnHeader colStatus;
        private Button btnAddLight;
        private Button btnRemoveLight;
        private Button btnRetry;
        private Label lblCameraState;
        private Label lblLightState;
        private Label lblDetail;
        private Label lblDelay;
        private NumericUpDown numUpDown;
        private Label lblOffDelay;
        private NumericUpDown numUpDownOff;
        private GroupBox grpMode;
        private RadioButton rbLightAuto;
        private RadioButton rbLightOn;
        private RadioButton rbLightOff;
        private Label lblMode;
        private CheckBox chkStartWithWindows;
        private Label lblAppearance;
        private ComboBox cmbTheme;
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
