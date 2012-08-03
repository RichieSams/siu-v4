namespace SkinInstaller
{
    partial class FileLocForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileLocForm));
            this.origLoc = new System.Windows.Forms.Label();
            this.fileName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.text1 = new System.Windows.Forms.Label();
            this.text2 = new System.Windows.Forms.Label();
            this.possibleLocs = new System.Windows.Forms.ComboBox();
            this.ok = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // origLoc
            // 
            this.origLoc.AutoSize = true;
            this.origLoc.Location = new System.Drawing.Point(8, 54);
            this.origLoc.Name = "origLoc";
            this.origLoc.Size = new System.Drawing.Size(27, 13);
            this.origLoc.TabIndex = 11;
            this.origLoc.Text = "asdf";
            // 
            // fileName
            // 
            this.fileName.AutoSize = true;
            this.fileName.Location = new System.Drawing.Point(8, 22);
            this.fileName.Name = "fileName";
            this.fileName.Size = new System.Drawing.Size(27, 13);
            this.fileName.TabIndex = 7;
            this.fileName.Text = "asdf";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Original location was:";
            // 
            // text1
            // 
            this.text1.AutoSize = true;
            this.text1.Location = new System.Drawing.Point(8, 9);
            this.text1.Name = "text1";
            this.text1.Size = new System.Drawing.Size(224, 13);
            this.text1.TabIndex = 8;
            this.text1.Text = "The following file was found in multiple folders:";
            // 
            // text2
            // 
            this.text2.AutoSize = true;
            this.text2.Location = new System.Drawing.Point(8, 80);
            this.text2.Name = "text2";
            this.text2.Size = new System.Drawing.Size(222, 13);
            this.text2.TabIndex = 9;
            this.text2.Text = "Please specify the correct location for this file:";
            // 
            // possibleLocs
            // 
            this.possibleLocs.FormattingEnabled = true;
            this.possibleLocs.Location = new System.Drawing.Point(11, 96);
            this.possibleLocs.Name = "possibleLocs";
            this.possibleLocs.Size = new System.Drawing.Size(567, 21);
            this.possibleLocs.TabIndex = 12;
            // 
            // ok
            // 
            this.ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ok.Location = new System.Drawing.Point(257, 129);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(75, 23);
            this.ok.TabIndex = 13;
            this.ok.Text = "Ok";
            this.ok.UseVisualStyleBackColor = true;
            // 
            // FileLocForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(588, 162);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.possibleLocs);
            this.Controls.Add(this.origLoc);
            this.Controls.Add(this.fileName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.text1);
            this.Controls.Add(this.text2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FileLocForm";
            this.Text = "File Location";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label origLoc;
        private System.Windows.Forms.Label fileName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label text1;
        private System.Windows.Forms.Label text2;
        public System.Windows.Forms.ComboBox possibleLocs;
        private System.Windows.Forms.Button ok;
    }
}