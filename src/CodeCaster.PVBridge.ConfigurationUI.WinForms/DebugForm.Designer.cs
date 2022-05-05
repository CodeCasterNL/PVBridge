
namespace CodeCaster.PVBridge.ConfigurationUI.WinForms
{
    partial class DebugForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.serviceStatusTimer = new System.Windows.Forms.Timer(this.components);
            this.tabControl = new System.Windows.Forms.TabControl();
            this.statusTabPage = new System.Windows.Forms.TabPage();
            this.serviceStatusGroupBox = new System.Windows.Forms.GroupBox();
            this.forceSyncButton = new System.Windows.Forms.Button();
            this.grpcStatusLabel = new System.Windows.Forms.Label();
            this.grpcStatusInfoLabel = new System.Windows.Forms.Label();
            this.controlLabel = new System.Windows.Forms.Label();
            this.controlServiceButton = new System.Windows.Forms.Button();
            this.isServiceRunningLabel = new System.Windows.Forms.Label();
            this.serviceRunningLabel = new System.Windows.Forms.Label();
            this.configurationTabPage = new System.Windows.Forms.TabPage();
            this.logTabPage = new System.Windows.Forms.TabPage();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.tabControl.SuspendLayout();
            this.statusTabPage.SuspendLayout();
            this.serviceStatusGroupBox.SuspendLayout();
            this.logTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // serviceStatusTimer
            // 
            this.serviceStatusTimer.Enabled = true;
            this.serviceStatusTimer.Interval = 5000;
            this.serviceStatusTimer.Tick += new System.EventHandler(this.ServiceStatusTimer_Tick);
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.statusTabPage);
            this.tabControl.Controls.Add(this.configurationTabPage);
            this.tabControl.Controls.Add(this.logTabPage);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1043, 598);
            this.tabControl.TabIndex = 0;
            // 
            // statusTabPage
            // 
            this.statusTabPage.Controls.Add(this.serviceStatusGroupBox);
            this.statusTabPage.Location = new System.Drawing.Point(4, 24);
            this.statusTabPage.Name = "statusTabPage";
            this.statusTabPage.Size = new System.Drawing.Size(1035, 570);
            this.statusTabPage.TabIndex = 2;
            this.statusTabPage.Text = "Status";
            this.statusTabPage.UseVisualStyleBackColor = true;
            // 
            // serviceStatusGroupBox
            // 
            this.serviceStatusGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.serviceStatusGroupBox.Controls.Add(this.forceSyncButton);
            this.serviceStatusGroupBox.Controls.Add(this.grpcStatusLabel);
            this.serviceStatusGroupBox.Controls.Add(this.grpcStatusInfoLabel);
            this.serviceStatusGroupBox.Controls.Add(this.controlLabel);
            this.serviceStatusGroupBox.Controls.Add(this.controlServiceButton);
            this.serviceStatusGroupBox.Controls.Add(this.isServiceRunningLabel);
            this.serviceStatusGroupBox.Controls.Add(this.serviceRunningLabel);
            this.serviceStatusGroupBox.Location = new System.Drawing.Point(3, 3);
            this.serviceStatusGroupBox.Name = "serviceStatusGroupBox";
            this.serviceStatusGroupBox.Size = new System.Drawing.Size(1029, 126);
            this.serviceStatusGroupBox.TabIndex = 1;
            this.serviceStatusGroupBox.TabStop = false;
            this.serviceStatusGroupBox.Text = "Service status";
            // 
            // forceSyncButton
            // 
            this.forceSyncButton.Location = new System.Drawing.Point(164, 41);
            this.forceSyncButton.Name = "forceSyncButton";
            this.forceSyncButton.Size = new System.Drawing.Size(75, 23);
            this.forceSyncButton.TabIndex = 6;
            this.forceSyncButton.Tag = "start";
            this.forceSyncButton.Text = "&Force sync";
            this.forceSyncButton.UseVisualStyleBackColor = true;
            this.forceSyncButton.Click += new System.EventHandler(this.ForceSyncButton_Click);
            // 
            // grpcStatusLabel
            // 
            this.grpcStatusLabel.AutoSize = true;
            this.grpcStatusLabel.Location = new System.Drawing.Point(244, 23);
            this.grpcStatusLabel.Name = "grpcStatusLabel";
            this.grpcStatusLabel.Size = new System.Drawing.Size(65, 15);
            this.grpcStatusLabel.TabIndex = 5;
            this.grpcStatusLabel.Text = "(unknown)";
            // 
            // grpcStatusInfoLabel
            // 
            this.grpcStatusInfoLabel.AutoSize = true;
            this.grpcStatusInfoLabel.Location = new System.Drawing.Point(164, 23);
            this.grpcStatusInfoLabel.Name = "grpcStatusInfoLabel";
            this.grpcStatusInfoLabel.Size = new System.Drawing.Size(74, 15);
            this.grpcStatusInfoLabel.TabIndex = 4;
            this.grpcStatusInfoLabel.Text = "gRPC Status:";
            // 
            // controlLabel
            // 
            this.controlLabel.AutoSize = true;
            this.controlLabel.Location = new System.Drawing.Point(7, 45);
            this.controlLabel.Name = "controlLabel";
            this.controlLabel.Size = new System.Drawing.Size(50, 15);
            this.controlLabel.TabIndex = 3;
            this.controlLabel.Text = "Control:";
            // 
            // controlServiceButton
            // 
            this.controlServiceButton.Location = new System.Drawing.Point(68, 41);
            this.controlServiceButton.Name = "controlServiceButton";
            this.controlServiceButton.Size = new System.Drawing.Size(75, 23);
            this.controlServiceButton.TabIndex = 2;
            this.controlServiceButton.Tag = "start";
            this.controlServiceButton.Text = "&Start";
            this.controlServiceButton.UseVisualStyleBackColor = true;
            this.controlServiceButton.Click += new System.EventHandler(this.ControlServiceButton_Click);
            // 
            // isServiceRunningLabel
            // 
            this.isServiceRunningLabel.AutoSize = true;
            this.isServiceRunningLabel.Location = new System.Drawing.Point(68, 23);
            this.isServiceRunningLabel.Name = "isServiceRunningLabel";
            this.isServiceRunningLabel.Size = new System.Drawing.Size(65, 15);
            this.isServiceRunningLabel.TabIndex = 1;
            this.isServiceRunningLabel.Text = "(unknown)";
            // 
            // serviceRunningLabel
            // 
            this.serviceRunningLabel.AutoSize = true;
            this.serviceRunningLabel.Location = new System.Drawing.Point(7, 23);
            this.serviceRunningLabel.Name = "serviceRunningLabel";
            this.serviceRunningLabel.Size = new System.Drawing.Size(55, 15);
            this.serviceRunningLabel.TabIndex = 0;
            this.serviceRunningLabel.Text = "Running:";
            // 
            // configurationTabPage
            // 
            this.configurationTabPage.Location = new System.Drawing.Point(4, 24);
            this.configurationTabPage.Name = "configurationTabPage";
            this.configurationTabPage.Size = new System.Drawing.Size(1035, 570);
            this.configurationTabPage.TabIndex = 3;
            this.configurationTabPage.Text = "Configuration";
            this.configurationTabPage.UseVisualStyleBackColor = true;
            // 
            // logTabPage
            // 
            this.logTabPage.Controls.Add(this.logTextBox);
            this.logTabPage.Location = new System.Drawing.Point(4, 24);
            this.logTabPage.Name = "logTabPage";
            this.logTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.logTabPage.Size = new System.Drawing.Size(1035, 570);
            this.logTabPage.TabIndex = 1;
            this.logTabPage.Text = "Log";
            this.logTabPage.UseVisualStyleBackColor = true;
            // 
            // logTextBox
            // 
            this.logTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logTextBox.Location = new System.Drawing.Point(6, 6);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.logTextBox.Size = new System.Drawing.Size(1023, 510);
            this.logTextBox.TabIndex = 0;
            // 
            // DebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1067, 622);
            this.Controls.Add(this.tabControl);
            this.Name = "DebugForm";
            this.Text = "PVBridge Service Status";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabControl.ResumeLayout(false);
            this.statusTabPage.ResumeLayout(false);
            this.serviceStatusGroupBox.ResumeLayout(false);
            this.serviceStatusGroupBox.PerformLayout();
            this.logTabPage.ResumeLayout(false);
            this.logTabPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer serviceStatusTimer;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage logTabPage;
        private System.Windows.Forms.TextBox logTextBox;
        private System.Windows.Forms.TabPage statusTabPage;
        private System.Windows.Forms.GroupBox serviceStatusGroupBox;
        private System.Windows.Forms.Button forceSyncButton;
        private System.Windows.Forms.Label grpcStatusLabel;
        private System.Windows.Forms.Label grpcStatusInfoLabel;
        private System.Windows.Forms.Label controlLabel;
        private System.Windows.Forms.Button controlServiceButton;
        private System.Windows.Forms.Label isServiceRunningLabel;
        private System.Windows.Forms.Label serviceRunningLabel;
        private System.Windows.Forms.TabPage configurationTabPage;
    }
}

