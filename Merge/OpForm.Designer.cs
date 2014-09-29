namespace Merge
{
    partial class OpForm
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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPerform = new System.Windows.Forms.Button();
            this.logListBox = new System.Windows.Forms.ListBox();
            this.errorLogTextBox = new System.Windows.Forms.TextBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnPerform);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(674, 30);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // btnPerform
            // 
            this.btnPerform.Location = new System.Drawing.Point(3, 3);
            this.btnPerform.Name = "btnPerform";
            this.btnPerform.Size = new System.Drawing.Size(75, 23);
            this.btnPerform.TabIndex = 0;
            this.btnPerform.Text = "Perform";
            this.btnPerform.UseVisualStyleBackColor = true;
            this.btnPerform.Click += new System.EventHandler(this.btnPerform_Click);
            // 
            // logListBox
            // 
            this.logListBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.logListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logListBox.FormattingEnabled = true;
            this.logListBox.IntegralHeight = false;
            this.logListBox.Location = new System.Drawing.Point(0, 30);
            this.logListBox.Name = "logListBox";
            this.logListBox.Size = new System.Drawing.Size(674, 204);
            this.logListBox.TabIndex = 2;
            // 
            // errorLogTextBox
            // 
            this.errorLogTextBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.errorLogTextBox.Location = new System.Drawing.Point(0, 234);
            this.errorLogTextBox.Multiline = true;
            this.errorLogTextBox.Name = "errorLogTextBox";
            this.errorLogTextBox.ReadOnly = true;
            this.errorLogTextBox.Size = new System.Drawing.Size(674, 77);
            this.errorLogTextBox.TabIndex = 3;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter1.Location = new System.Drawing.Point(0, 231);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(674, 3);
            this.splitter1.TabIndex = 4;
            this.splitter1.TabStop = false;
            // 
            // OpForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(674, 311);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.logListBox);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.errorLogTextBox);
            this.Name = "OpForm";
            this.Text = "OpForm";
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnPerform;
        private System.Windows.Forms.ListBox logListBox;
        private System.Windows.Forms.TextBox errorLogTextBox;
        private System.Windows.Forms.Splitter splitter1;
    }
}