namespace SyncButler
{
    partial class MiniPartnershipForm
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
            this.miniListBox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // miniListBox
            // 
            this.miniListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.miniListBox.FormattingEnabled = true;
            this.miniListBox.Location = new System.Drawing.Point(0, 0);
            this.miniListBox.Margin = new System.Windows.Forms.Padding(0);
            this.miniListBox.Name = "miniListBox";
            this.miniListBox.Size = new System.Drawing.Size(283, 264);
            this.miniListBox.TabIndex = 0;
            // 
            // MiniPartnershipForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.miniListBox);
            this.Name = "MiniPartnershipForm";
            this.Text = "MiniPartnershipForm";
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ListBox miniListBox;
    }
}