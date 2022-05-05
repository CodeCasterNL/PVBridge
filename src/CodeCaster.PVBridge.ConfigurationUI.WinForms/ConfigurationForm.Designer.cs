using CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms
{
    partial class ConfigurationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.introLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.goodWeLabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.pvOutputLabel = new System.Windows.Forms.Label();
            this.goodWeApiConfigurator = new CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls.GoodWeApiConfigurator();
            this.pvOutputApiConfigurator = new CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls.PVOutputApiConfigurator();
            this.reloadButton = new System.Windows.Forms.Button();
            this.fileStatusLabel = new System.Windows.Forms.Label();
            this.syncStartdatePicker = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.serviceStatusTimer = new System.Windows.Forms.Timer(this.components);
            this.serviceStatusStaticLabel = new System.Windows.Forms.Label();
            this.serviceStatusTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.controlServiceButton = new System.Windows.Forms.Button();
            this.versionLinkLabel = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // introLabel
            // 
            this.introLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.introLabel.Location = new System.Drawing.Point(12, 34);
            this.introLabel.Name = "introLabel";
            this.introLabel.Size = new System.Drawing.Size(672, 33);
            this.introLabel.TabIndex = 1;
            this.introLabel.Text = "Configure the background service which will sync your inverter\'s live and histori" +
    "c data with aggregators. Currently only works from GoodWe to PVOutput.org.";
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(91, 25);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "PVBridge";
            // 
            // goodWeLabel
            // 
            this.goodWeLabel.AutoSize = true;
            this.goodWeLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.goodWeLabel.Location = new System.Drawing.Point(12, 78);
            this.goodWeLabel.Name = "goodWeLabel";
            this.goodWeLabel.Size = new System.Drawing.Size(86, 25);
            this.goodWeLabel.TabIndex = 2;
            this.goodWeLabel.Text = "GoodWe";
            // 
            // saveButton
            // 
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Enabled = false;
            this.saveButton.Location = new System.Drawing.Point(524, 480);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(160, 35);
            this.saveButton.TabIndex = 12;
            this.saveButton.Text = "Save Configuration";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // pvOutputLabel
            // 
            this.pvOutputLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pvOutputLabel.AutoSize = true;
            this.pvOutputLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.pvOutputLabel.Location = new System.Drawing.Point(383, 78);
            this.pvOutputLabel.Name = "pvOutputLabel";
            this.pvOutputLabel.Size = new System.Drawing.Size(130, 25);
            this.pvOutputLabel.TabIndex = 4;
            this.pvOutputLabel.Text = "PVOutput.org";
            // 
            // goodWeApiConfigurator
            // 
            this.goodWeApiConfigurator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.goodWeApiConfigurator.Location = new System.Drawing.Point(12, 106);
            this.goodWeApiConfigurator.MinimumSize = new System.Drawing.Size(300, 250);
            this.goodWeApiConfigurator.Name = "goodWeApiConfigurator";
            this.goodWeApiConfigurator.Size = new System.Drawing.Size(335, 280);
            this.goodWeApiConfigurator.TabIndex = 3;
            // 
            // pvOutputApiConfigurator
            // 
            this.pvOutputApiConfigurator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pvOutputApiConfigurator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pvOutputApiConfigurator.Location = new System.Drawing.Point(349, 106);
            this.pvOutputApiConfigurator.MinimumSize = new System.Drawing.Size(300, 165);
            this.pvOutputApiConfigurator.Name = "pvOutputApiConfigurator";
            this.pvOutputApiConfigurator.Size = new System.Drawing.Size(335, 280);
            this.pvOutputApiConfigurator.TabIndex = 5;
            // 
            // reloadButton
            // 
            this.reloadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.reloadButton.Location = new System.Drawing.Point(358, 480);
            this.reloadButton.Name = "reloadButton";
            this.reloadButton.Size = new System.Drawing.Size(160, 35);
            this.reloadButton.TabIndex = 11;
            this.reloadButton.Text = "&Reload Configuration";
            this.reloadButton.UseVisualStyleBackColor = true;
            this.reloadButton.Click += new System.EventHandler(this.ReloadButton_Click);
            // 
            // fileStatusLabel
            // 
            this.fileStatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.fileStatusLabel.AutoSize = true;
            this.fileStatusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.fileStatusLabel.Location = new System.Drawing.Point(11, 430);
            this.fileStatusLabel.Name = "fileStatusLabel";
            this.fileStatusLabel.Size = new System.Drawing.Size(87, 15);
            this.fileStatusLabel.TabIndex = 8;
            this.fileStatusLabel.Text = "File status label";
            this.fileStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // syncStartdatePicker
            // 
            this.syncStartdatePicker.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.syncStartdatePicker.Location = new System.Drawing.Point(483, 400);
            this.syncStartdatePicker.MinDate = new System.DateTime(1839, 1, 1, 0, 0, 0, 0);
            this.syncStartdatePicker.Name = "syncStartdatePicker";
            this.syncStartdatePicker.Size = new System.Drawing.Size(200, 23);
            this.syncStartdatePicker.TabIndex = 7;
            this.syncStartdatePicker.Visible = false;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(295, 404);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(182, 15);
            this.label1.TabIndex = 6;
            this.label1.Text = "Since when do you want to sync?";
            this.label1.Visible = false;
            // 
            // serviceStatusTimer
            // 
            this.serviceStatusTimer.Enabled = true;
            this.serviceStatusTimer.Interval = 5000;
            this.serviceStatusTimer.Tick += new System.EventHandler(this.ServiceStatusTimer_Tick);
            // 
            // serviceStatusStaticLabel
            // 
            this.serviceStatusStaticLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.serviceStatusStaticLabel.AutoSize = true;
            this.serviceStatusStaticLabel.Location = new System.Drawing.Point(11, 458);
            this.serviceStatusStaticLabel.Name = "serviceStatusStaticLabel";
            this.serviceStatusStaticLabel.Size = new System.Drawing.Size(81, 15);
            this.serviceStatusStaticLabel.TabIndex = 9;
            this.serviceStatusStaticLabel.Text = "Service status:";
            // 
            // serviceStatusTextBox
            // 
            this.serviceStatusTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.serviceStatusTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.serviceStatusTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.serviceStatusTextBox.Location = new System.Drawing.Point(89, 458);
            this.serviceStatusTextBox.Multiline = true;
            this.serviceStatusTextBox.Name = "serviceStatusTextBox";
            this.serviceStatusTextBox.Size = new System.Drawing.Size(594, 16);
            this.serviceStatusTextBox.TabIndex = 10;
            this.serviceStatusTextBox.TabStop = false;
            this.serviceStatusTextBox.Text = "awaiting service status... (readonly status textbox)";
            this.serviceStatusTextBox.TextChanged += new System.EventHandler(this.ServiceStatusTextBox_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(332, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 25);
            this.label2.TabIndex = 13;
            this.label2.Text = "➞";
            // 
            // controlServiceButton
            // 
            this.controlServiceButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.controlServiceButton.Location = new System.Drawing.Point(192, 480);
            this.controlServiceButton.Name = "controlServiceButton";
            this.controlServiceButton.Size = new System.Drawing.Size(160, 35);
            this.controlServiceButton.TabIndex = 14;
            this.controlServiceButton.Tag = "start";
            this.controlServiceButton.Text = "Start Servi&ce";
            this.controlServiceButton.UseVisualStyleBackColor = true;
            this.controlServiceButton.Click += new System.EventHandler(this.ControlServiceButton_Click);
            // 
            // versionLinkLabel
            // 
            this.versionLinkLabel.AutoSize = true;
            this.versionLinkLabel.LinkArea = new System.Windows.Forms.LinkArea(23, 999);
            this.versionLinkLabel.Location = new System.Drawing.Point(12, 403);
            this.versionLinkLabel.Name = "versionLinkLabel";
            this.versionLinkLabel.Size = new System.Drawing.Size(249, 21);
            this.versionLinkLabel.TabIndex = 15;
            this.versionLinkLabel.TabStop = true;
            this.versionLinkLabel.Text = "There\'s a new version! Download v0.0.0 here.";
            this.versionLinkLabel.UseCompatibleTextRendering = true;
            this.versionLinkLabel.Visible = false;
            this.versionLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.VersionLinkLabel_LinkClicked);
            // 
            // ConfigurationForm
            // 
            this.AcceptButton = this.saveButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(696, 523);
            this.Controls.Add(this.versionLinkLabel);
            this.Controls.Add(this.controlServiceButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.serviceStatusTextBox);
            this.Controls.Add(this.serviceStatusStaticLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.syncStartdatePicker);
            this.Controls.Add(this.fileStatusLabel);
            this.Controls.Add(this.reloadButton);
            this.Controls.Add(this.pvOutputApiConfigurator);
            this.Controls.Add(this.goodWeApiConfigurator);
            this.Controls.Add(this.pvOutputLabel);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.goodWeLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.introLabel);
            this.MinimumSize = new System.Drawing.Size(712, 562);
            this.Name = "ConfigurationForm";
            this.Text = "PVBridge - Configuration";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfigurationForm_FormClosing);
            this.Load += new System.EventHandler(this.ConfigurationForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label introLabel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label goodWeLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Label pvOutputLabel;
        private GoodWeApiConfigurator goodWeApiConfigurator;
        private PVOutputApiConfigurator pvOutputApiConfigurator;
        private System.Windows.Forms.Button reloadButton;
        private System.Windows.Forms.Label fileStatusLabel;
        private System.Windows.Forms.DateTimePicker syncStartdatePicker;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer serviceStatusTimer;
        private System.Windows.Forms.Label serviceStatusStaticLabel;
        private System.Windows.Forms.TextBox serviceStatusTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button controlServiceButton;
        private System.Windows.Forms.LinkLabel versionLinkLabel;
    }
}