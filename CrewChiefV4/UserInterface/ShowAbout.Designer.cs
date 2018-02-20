namespace CrewChiefV4.UserInterface
{
    partial class ShowAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            this.MinimizeBox = false;
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Name = "textBox1";
            this.textBox1.Multiline = true;
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.TabIndex = 0;
            this.textBox1.MaxLength = 99999999;
            this.Padding = new System.Windows.Forms.Padding(10, 10, 10, 10);
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            // 
            // Readme
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(857, 508);
            this.Controls.Add(this.textBox1);
            this.Name = "About Crew Chief";
            this.Text = "About Crew Chief";
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;

    }
}