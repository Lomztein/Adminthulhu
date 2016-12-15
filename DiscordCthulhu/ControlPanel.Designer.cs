namespace DiscordCthulhu {
    partial class ControlPanel {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose ( bool disposing ) {
            if (disposing && (components != null)) {
                components.Dispose ();
            }
            base.Dispose (disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent () {
            this.sendButton = new System.Windows.Forms.Button();
            this.messageText = new System.Windows.Forms.TextBox();
            this.channelName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // sendButton
            // 
            this.sendButton.Location = new System.Drawing.Point(697, 12);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(75, 23);
            this.sendButton.TabIndex = 0;
            this.sendButton.Text = "SEND";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // messageText
            // 
            this.messageText.Location = new System.Drawing.Point(96, 14);
            this.messageText.Name = "messageText";
            this.messageText.Size = new System.Drawing.Size(595, 20);
            this.messageText.TabIndex = 1;
            this.messageText.TextChanged += new System.EventHandler(this.messageText_TextChanged);
            // 
            // channelName
            // 
            this.channelName.Location = new System.Drawing.Point(12, 14);
            this.channelName.Name = "channelName";
            this.channelName.Size = new System.Drawing.Size(78, 20);
            this.channelName.TabIndex = 3;
            this.channelName.TextChanged += new System.EventHandler(this.channelName_TextChanged);
            // 
            // ControlPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 50);
            this.Controls.Add(this.channelName);
            this.Controls.Add(this.messageText);
            this.Controls.Add(this.sendButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "ControlPanel";
            this.Text = "Discord Bot";
            this.Load += new System.EventHandler(this.ControlPanel_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button sendButton;
        private System.Windows.Forms.TextBox messageText;
        private System.Windows.Forms.TextBox channelName;
    }
}