
namespace CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls
{
    partial class GoodWeApiConfigurator
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.emailLabel = new System.Windows.Forms.Label();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.emailTextBox = new System.Windows.Forms.TextBox();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.semsPortalLinkLabel = new System.Windows.Forms.LinkLabel();
            this.inverterIdLabel = new System.Windows.Forms.Label();
            this.readDeviceInfoButton = new System.Windows.Forms.Button();
            this.plantLabel = new System.Windows.Forms.Label();
            this.plantComboBox = new System.Windows.Forms.ComboBox();
            this.inverterComboBox = new System.Windows.Forms.ComboBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // emailLabel
            // 
            this.emailLabel.AutoSize = true;
            this.emailLabel.Location = new System.Drawing.Point(-2, 79);
            this.emailLabel.Name = "emailLabel";
            this.emailLabel.Size = new System.Drawing.Size(39, 15);
            this.emailLabel.TabIndex = 1;
            this.emailLabel.Text = "&Email:";
            // 
            // passwordLabel
            // 
            this.passwordLabel.AutoSize = true;
            this.passwordLabel.Location = new System.Drawing.Point(0, 108);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(60, 15);
            this.passwordLabel.TabIndex = 3;
            this.passwordLabel.Text = "&Password:";
            // 
            // emailTextBox
            // 
            this.emailTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.emailTextBox.Location = new System.Drawing.Point(72, 76);
            this.emailTextBox.Name = "emailTextBox";
            this.emailTextBox.Size = new System.Drawing.Size(225, 23);
            this.emailTextBox.TabIndex = 2;
            this.emailTextBox.TextChanged += new System.EventHandler(this.Credentials_TextChanged);
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.passwordTextBox.Location = new System.Drawing.Point(72, 105);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.PasswordChar = '*';
            this.passwordTextBox.Size = new System.Drawing.Size(225, 23);
            this.passwordTextBox.TabIndex = 4;
            this.passwordTextBox.TextChanged += new System.EventHandler(this.Credentials_TextChanged);
            // 
            // semsPortalLinkLabel
            // 
            this.semsPortalLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.semsPortalLinkLabel.LinkArea = new System.Windows.Forms.LinkArea(58, 26);
            this.semsPortalLinkLabel.Location = new System.Drawing.Point(0, 0);
            this.semsPortalLinkLabel.Name = "semsPortalLinkLabel";
            this.semsPortalLinkLabel.Size = new System.Drawing.Size(300, 50);
            this.semsPortalLinkLabel.TabIndex = 0;
            this.semsPortalLinkLabel.TabStop = true;
            this.semsPortalLinkLabel.Text = "Enter your credentials for your GoodWe inverter\'s portal, https://www.semsportal." +
    "com, to retrieve your device information. Nothing will be saved yet.";
            this.semsPortalLinkLabel.UseCompatibleTextRendering = true;
            this.semsPortalLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SemsPortalLinkLabel_LinkClicked);
            // 
            // inverterIdLabel
            // 
            this.inverterIdLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.inverterIdLabel.AutoSize = true;
            this.inverterIdLabel.Location = new System.Drawing.Point(-3, 257);
            this.inverterIdLabel.Name = "inverterIdLabel";
            this.inverterIdLabel.Size = new System.Drawing.Size(50, 15);
            this.inverterIdLabel.TabIndex = 9;
            this.inverterIdLabel.Text = "&Inverter:";
            // 
            // readDeviceInfoButton
            // 
            this.readDeviceInfoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.readDeviceInfoButton.Enabled = false;
            this.readDeviceInfoButton.Location = new System.Drawing.Point(138, 134);
            this.readDeviceInfoButton.Name = "readDeviceInfoButton";
            this.readDeviceInfoButton.Size = new System.Drawing.Size(159, 35);
            this.readDeviceInfoButton.TabIndex = 5;
            this.readDeviceInfoButton.Text = "Read &device info";
            this.readDeviceInfoButton.UseVisualStyleBackColor = true;
            this.readDeviceInfoButton.Click += new System.EventHandler(this.ReadDeviceInfoButton_Click);
            // 
            // plantLabel
            // 
            this.plantLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.plantLabel.AutoSize = true;
            this.plantLabel.Location = new System.Drawing.Point(-3, 228);
            this.plantLabel.Name = "plantLabel";
            this.plantLabel.Size = new System.Drawing.Size(37, 15);
            this.plantLabel.TabIndex = 7;
            this.plantLabel.Text = "P&lant:";
            // 
            // plantComboBox
            // 
            this.plantComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.plantComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.plantComboBox.Enabled = false;
            this.plantComboBox.FormattingEnabled = true;
            this.plantComboBox.Location = new System.Drawing.Point(73, 225);
            this.plantComboBox.Name = "plantComboBox";
            this.plantComboBox.Size = new System.Drawing.Size(224, 23);
            this.plantComboBox.TabIndex = 8;
            this.plantComboBox.SelectedValueChanged += new System.EventHandler(this.PlantComboBox_SelectedValueChanged);
            // 
            // inverterComboBox
            // 
            this.inverterComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.inverterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.inverterComboBox.Enabled = false;
            this.inverterComboBox.FormattingEnabled = true;
            this.inverterComboBox.Location = new System.Drawing.Point(72, 254);
            this.inverterComboBox.Name = "inverterComboBox";
            this.inverterComboBox.Size = new System.Drawing.Size(225, 23);
            this.inverterComboBox.TabIndex = 10;
            this.inverterComboBox.SelectedValueChanged += new System.EventHandler(this.InverterComboBox_SelectedValueChanged);
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLabel.Location = new System.Drawing.Point(-3, 172);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(300, 42);
            this.statusLabel.TabIndex = 11;
            this.statusLabel.Text = "StatusLabel";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // GoodWeApiConfigurator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.inverterComboBox);
            this.Controls.Add(this.plantComboBox);
            this.Controls.Add(this.plantLabel);
            this.Controls.Add(this.readDeviceInfoButton);
            this.Controls.Add(this.inverterIdLabel);
            this.Controls.Add(this.semsPortalLinkLabel);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.emailTextBox);
            this.Controls.Add(this.passwordLabel);
            this.Controls.Add(this.emailLabel);
            this.MinimumSize = new System.Drawing.Size(300, 280);
            this.Name = "GoodWeApiConfigurator";
            this.Size = new System.Drawing.Size(300, 280);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label emailLabel;
        private System.Windows.Forms.Label passwordLabel;
        private System.Windows.Forms.TextBox emailTextBox;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.LinkLabel semsPortalLinkLabel;
        private System.Windows.Forms.Label inverterIdLabel;
        private System.Windows.Forms.Button readDeviceInfoButton;
        private System.Windows.Forms.Label plantLabel;
        private System.Windows.Forms.ComboBox plantComboBox;
        private System.Windows.Forms.ComboBox inverterComboBox;
        private System.Windows.Forms.Label statusLabel;
    }
}
