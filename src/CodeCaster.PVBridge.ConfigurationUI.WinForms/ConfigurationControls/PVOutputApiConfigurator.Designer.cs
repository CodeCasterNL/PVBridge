
namespace CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls
{
    partial class PVOutputApiConfigurator
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
            this.apiKeyLabel = new System.Windows.Forms.Label();
            this.apiKeyTextBox = new System.Windows.Forms.TextBox();
            this.pvOutputLinkLabel = new System.Windows.Forms.LinkLabel();
            this.readSystemInfoButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.systemLabel = new System.Windows.Forms.Label();
            this.systemComboBox = new System.Windows.Forms.ComboBox();
            this.systemIdTextBox = new System.Windows.Forms.TextBox();
            this.systemIdLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // apiKeyLabel
            // 
            this.apiKeyLabel.AutoSize = true;
            this.apiKeyLabel.Location = new System.Drawing.Point(0, 80);
            this.apiKeyLabel.Name = "apiKeyLabel";
            this.apiKeyLabel.Size = new System.Drawing.Size(49, 15);
            this.apiKeyLabel.TabIndex = 1;
            this.apiKeyLabel.Text = "&API key:";
            // 
            // apiKeyTextBox
            // 
            this.apiKeyTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.apiKeyTextBox.Location = new System.Drawing.Point(75, 77);
            this.apiKeyTextBox.Name = "apiKeyTextBox";
            this.apiKeyTextBox.PasswordChar = '*';
            this.apiKeyTextBox.Size = new System.Drawing.Size(222, 23);
            this.apiKeyTextBox.TabIndex = 2;
            this.apiKeyTextBox.TextChanged += new System.EventHandler(this.Credentials_TextChanged);
            // 
            // pvOutputLinkLabel
            // 
            this.pvOutputLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pvOutputLinkLabel.LinkArea = new System.Windows.Forms.LinkArea(11, 20);
            this.pvOutputLinkLabel.Location = new System.Drawing.Point(0, 0);
            this.pvOutputLinkLabel.Name = "pvOutputLinkLabel";
            this.pvOutputLinkLabel.Size = new System.Drawing.Size(300, 50);
            this.pvOutputLinkLabel.TabIndex = 0;
            this.pvOutputLinkLabel.TabStop = true;
            this.pvOutputLinkLabel.Text = "Enter your https://PVOutput.org API key and system number to retrieve system info" +
    "rmation. Nothing will be saved yet.";
            this.pvOutputLinkLabel.UseCompatibleTextRendering = true;
            this.pvOutputLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.PVOutputLinkLabel_LinkClicked);
            // 
            // readSystemInfoButton
            // 
            this.readSystemInfoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.readSystemInfoButton.Enabled = false;
            this.readSystemInfoButton.Location = new System.Drawing.Point(140, 134);
            this.readSystemInfoButton.Name = "readSystemInfoButton";
            this.readSystemInfoButton.Size = new System.Drawing.Size(157, 35);
            this.readSystemInfoButton.TabIndex = 5;
            this.readSystemInfoButton.Text = "Read &system info";
            this.readSystemInfoButton.UseVisualStyleBackColor = true;
            this.readSystemInfoButton.Click += new System.EventHandler(this.ReadDeviceInfoButton_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLabel.Location = new System.Drawing.Point(0, 172);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(297, 43);
            this.statusLabel.TabIndex = 6;
            this.statusLabel.Text = "StatusLabel";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // systemLabel
            // 
            this.systemLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.systemLabel.AutoSize = true;
            this.systemLabel.Location = new System.Drawing.Point(0, 221);
            this.systemLabel.Name = "systemLabel";
            this.systemLabel.Size = new System.Drawing.Size(48, 15);
            this.systemLabel.TabIndex = 6;
            this.systemLabel.Text = "S&ystem:";
            // 
            // systemComboBox
            // 
            this.systemComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.systemComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.systemComboBox.FormattingEnabled = true;
            this.systemComboBox.Location = new System.Drawing.Point(75, 218);
            this.systemComboBox.Name = "systemComboBox";
            this.systemComboBox.Size = new System.Drawing.Size(222, 23);
            this.systemComboBox.TabIndex = 7;
            this.systemComboBox.SelectedValueChanged += new System.EventHandler(this.SystemComboBox_SelectedValueChanged);
            // 
            // systemIdTextBox
            // 
            this.systemIdTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.systemIdTextBox.Location = new System.Drawing.Point(75, 105);
            this.systemIdTextBox.Name = "systemIdTextBox";
            this.systemIdTextBox.Size = new System.Drawing.Size(222, 23);
            this.systemIdTextBox.TabIndex = 4;
            this.systemIdTextBox.TextChanged += new System.EventHandler(this.Credentials_TextChanged);
            // 
            // systemIdLabel
            // 
            this.systemIdLabel.AutoSize = true;
            this.systemIdLabel.Location = new System.Drawing.Point(0, 108);
            this.systemIdLabel.Name = "systemIdLabel";
            this.systemIdLabel.Size = new System.Drawing.Size(62, 15);
            this.systemIdLabel.TabIndex = 3;
            this.systemIdLabel.Text = "&System ID:";
            // 
            // PVOutputApiConfigurator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.systemIdTextBox);
            this.Controls.Add(this.systemIdLabel);
            this.Controls.Add(this.systemComboBox);
            this.Controls.Add(this.systemLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.readSystemInfoButton);
            this.Controls.Add(this.pvOutputLinkLabel);
            this.Controls.Add(this.apiKeyTextBox);
            this.Controls.Add(this.apiKeyLabel);
            this.MinimumSize = new System.Drawing.Size(300, 250);
            this.Name = "PVOutputApiConfigurator";
            this.Size = new System.Drawing.Size(300, 250);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label apiKeyLabel;
        private System.Windows.Forms.TextBox apiKeyTextBox;
        private System.Windows.Forms.LinkLabel pvOutputLinkLabel;
        private System.Windows.Forms.Button readSystemInfoButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label systemLabel;
        private System.Windows.Forms.ComboBox systemComboBox;
        private System.Windows.Forms.TextBox systemIdTextBox;
        private System.Windows.Forms.Label systemIdLabel;
    }
}
