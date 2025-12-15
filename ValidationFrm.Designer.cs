namespace Rotronic
{
    partial class ValidationFrm
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
            this.buttonAddMirror = new System.Windows.Forms.Button();
            this.buttonAddProbe = new System.Windows.Forms.Button();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.textBoxMirrorTemp = new System.Windows.Forms.TextBox();
            this.textBoxMirrorHumdity = new System.Windows.Forms.TextBox();
            this.textBoxMirrorDewPoint = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonAddMirror
            // 
            this.buttonAddMirror.Location = new System.Drawing.Point(64, 331);
            this.buttonAddMirror.Name = "buttonAddMirror";
            this.buttonAddMirror.Size = new System.Drawing.Size(133, 57);
            this.buttonAddMirror.TabIndex = 0;
            this.buttonAddMirror.Text = "Add Mirror";
            this.buttonAddMirror.UseVisualStyleBackColor = true;
            this.buttonAddMirror.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonAddProbe
            // 
            this.buttonAddProbe.Location = new System.Drawing.Point(304, 331);
            this.buttonAddProbe.Name = "buttonAddProbe";
            this.buttonAddProbe.Size = new System.Drawing.Size(133, 57);
            this.buttonAddProbe.TabIndex = 1;
            this.buttonAddProbe.Text = "Add Probe";
            this.buttonAddProbe.UseVisualStyleBackColor = true;
            this.buttonAddProbe.Click += new System.EventHandler(this.buttonAddProbe_Click);
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Location = new System.Drawing.Point(555, 331);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(133, 57);
            this.buttonUpdate.TabIndex = 2;
            this.buttonUpdate.Text = "Update Values";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            this.buttonUpdate.Click += new System.EventHandler(this.buttonUpdate_Click);
            // 
            // textBoxMirrorTemp
            // 
            this.textBoxMirrorTemp.Location = new System.Drawing.Point(64, 76);
            this.textBoxMirrorTemp.Name = "textBoxMirrorTemp";
            this.textBoxMirrorTemp.Size = new System.Drawing.Size(114, 26);
            this.textBoxMirrorTemp.TabIndex = 3;
            // 
            // textBoxMirrorHumdity
            // 
            this.textBoxMirrorHumdity.Location = new System.Drawing.Point(64, 136);
            this.textBoxMirrorHumdity.Name = "textBoxMirrorHumdity";
            this.textBoxMirrorHumdity.Size = new System.Drawing.Size(114, 26);
            this.textBoxMirrorHumdity.TabIndex = 4;
            // 
            // textBoxMirrorDewPoint
            // 
            this.textBoxMirrorDewPoint.Location = new System.Drawing.Point(64, 197);
            this.textBoxMirrorDewPoint.Name = "textBoxMirrorDewPoint";
            this.textBoxMirrorDewPoint.Size = new System.Drawing.Size(114, 26);
            this.textBoxMirrorDewPoint.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(60, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Mirror Temp";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(60, 113);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Mirror Humdity";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(67, 174);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "Mirror Dewpoint";
            // 
            // ValidationFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxMirrorDewPoint);
            this.Controls.Add(this.textBoxMirrorHumdity);
            this.Controls.Add(this.textBoxMirrorTemp);
            this.Controls.Add(this.buttonUpdate);
            this.Controls.Add(this.buttonAddProbe);
            this.Controls.Add(this.buttonAddMirror);
            this.Name = "ValidationFrm";
            this.Text = "ValidationFrm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonAddMirror;
        private System.Windows.Forms.Button buttonAddProbe;
        private System.Windows.Forms.Button buttonUpdate;
        private System.Windows.Forms.TextBox textBoxMirrorTemp;
        private System.Windows.Forms.TextBox textBoxMirrorHumdity;
        private System.Windows.Forms.TextBox textBoxMirrorDewPoint;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}