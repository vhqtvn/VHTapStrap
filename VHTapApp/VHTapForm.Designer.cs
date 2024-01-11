namespace VHTapApp
{
    partial class VHTapForm
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
            lblMode = new Label();
            SuspendLayout();
            // 
            // lblMode
            // 
            lblMode.AutoSize = true;
            lblMode.ForeColor = Color.White;
            lblMode.Location = new Point(0, 0);
            lblMode.Name = "lblMode";
            lblMode.Size = new Size(59, 25);
            lblMode.TabIndex = 0;
            lblMode.Text = "mode";
            // 
            // VHTapForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.Black;
            ClientSize = new Size(800, 450);
            ControlBox = false;
            Controls.Add(lblMode);
            ForeColor = Color.White;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "VHTapForm";
            ShowInTaskbar = false;
            Text = "VHTapForm";
            TopMost = true;
            Load += VHTapForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblMode;
    }
}
