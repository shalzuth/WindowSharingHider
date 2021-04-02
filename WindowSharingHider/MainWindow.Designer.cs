
namespace WindowSharingHider
{
    partial class MainWindow
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
            this.windowListCheckBox = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            // 
            // windowListCheckBox
            // 
            this.windowListCheckBox.CheckOnClick = true;
            this.windowListCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.windowListCheckBox.FormattingEnabled = true;
            this.windowListCheckBox.Location = new System.Drawing.Point(0, 0);
            this.windowListCheckBox.Name = "windowListCheckBox";
            this.windowListCheckBox.Size = new System.Drawing.Size(328, 384);
            this.windowListCheckBox.TabIndex = 0;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(328, 384);
            this.Controls.Add(this.windowListCheckBox);
            this.Name = "MainWindow";
            this.Text = "Window Sharing Hider";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox windowListCheckBox;
    }
}

