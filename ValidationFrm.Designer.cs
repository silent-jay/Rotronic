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
            this.buttonChamber = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxProbeTemp = new System.Windows.Forms.TextBox();
            this.textBoxProbeHumidity = new System.Windows.Forms.TextBox();
            this.textBoxProbeTempCount = new System.Windows.Forms.TextBox();
            this.textBoxProbeRes = new System.Windows.Forms.TextBox();
            this.textBoxChTemp = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.textBoxChTempSP = new System.Windows.Forms.TextBox();
            this.textBoxChHum = new System.Windows.Forms.TextBox();
            this.textBoxChHumSP = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonAddMirror
            // 
            this.buttonAddMirror.Location = new System.Drawing.Point(202, 331);
            this.buttonAddMirror.Name = "buttonAddMirror";
            this.buttonAddMirror.Size = new System.Drawing.Size(133, 57);
            this.buttonAddMirror.TabIndex = 0;
            this.buttonAddMirror.Text = "Add Mirror";
            this.buttonAddMirror.UseVisualStyleBackColor = true;
            this.buttonAddMirror.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonAddProbe
            // 
            this.buttonAddProbe.Location = new System.Drawing.Point(45, 331);
            this.buttonAddProbe.Name = "buttonAddProbe";
            this.buttonAddProbe.Size = new System.Drawing.Size(133, 57);
            this.buttonAddProbe.TabIndex = 1;
            this.buttonAddProbe.Text = "Add Probe";
            this.buttonAddProbe.UseVisualStyleBackColor = true;
            this.buttonAddProbe.Click += new System.EventHandler(this.buttonAddProbe_Click);
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Location = new System.Drawing.Point(567, 331);
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
            this.textBoxMirrorHumdity.Location = new System.Drawing.Point(64, 128);
            this.textBoxMirrorHumdity.Name = "textBoxMirrorHumdity";
            this.textBoxMirrorHumdity.Size = new System.Drawing.Size(114, 26);
            this.textBoxMirrorHumdity.TabIndex = 4;
            // 
            // textBoxMirrorDewPoint
            // 
            this.textBoxMirrorDewPoint.Location = new System.Drawing.Point(64, 181);
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
            this.label2.Location = new System.Drawing.Point(60, 105);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Mirror Humdity";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(60, 157);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "Mirror Dewpoint";
            // 
            // buttonChamber
            // 
            this.buttonChamber.Location = new System.Drawing.Point(388, 331);
            this.buttonChamber.Name = "buttonChamber";
            this.buttonChamber.Size = new System.Drawing.Size(133, 57);
            this.buttonChamber.TabIndex = 9;
            this.buttonChamber.Text = "Add Chamber";
            this.buttonChamber.UseVisualStyleBackColor = true;
            this.buttonChamber.Click += new System.EventHandler(this.buttonChamber_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(291, 53);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 20);
            this.label4.TabIndex = 10;
            this.label4.Text = "Probe Temp";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(291, 105);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(116, 20);
            this.label5.TabIndex = 11;
            this.label5.Text = "Probe Humidity";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(291, 157);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(193, 20);
            this.label6.TabIndex = 12;
            this.label6.Text = "Probe Temperature Count";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(291, 214);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(135, 20);
            this.label7.TabIndex = 13;
            this.label7.Text = "Probe Resistance";
            // 
            // textBoxProbeTemp
            // 
            this.textBoxProbeTemp.Location = new System.Drawing.Point(295, 76);
            this.textBoxProbeTemp.Name = "textBoxProbeTemp";
            this.textBoxProbeTemp.Size = new System.Drawing.Size(100, 26);
            this.textBoxProbeTemp.TabIndex = 14;
            // 
            // textBoxProbeHumidity
            // 
            this.textBoxProbeHumidity.Location = new System.Drawing.Point(295, 128);
            this.textBoxProbeHumidity.Name = "textBoxProbeHumidity";
            this.textBoxProbeHumidity.Size = new System.Drawing.Size(100, 26);
            this.textBoxProbeHumidity.TabIndex = 15;
            // 
            // textBoxProbeTempCount
            // 
            this.textBoxProbeTempCount.Location = new System.Drawing.Point(295, 180);
            this.textBoxProbeTempCount.Name = "textBoxProbeTempCount";
            this.textBoxProbeTempCount.Size = new System.Drawing.Size(100, 26);
            this.textBoxProbeTempCount.TabIndex = 16;
            // 
            // textBoxProbeRes
            // 
            this.textBoxProbeRes.Location = new System.Drawing.Point(295, 237);
            this.textBoxProbeRes.Name = "textBoxProbeRes";
            this.textBoxProbeRes.Size = new System.Drawing.Size(100, 26);
            this.textBoxProbeRes.TabIndex = 17;
            // 
            // textBoxChTemp
            // 
            this.textBoxChTemp.Location = new System.Drawing.Point(519, 76);
            this.textBoxChTemp.Name = "textBoxChTemp";
            this.textBoxChTemp.Size = new System.Drawing.Size(100, 26);
            this.textBoxChTemp.TabIndex = 18;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(515, 53);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(118, 20);
            this.label8.TabIndex = 19;
            this.label8.Text = "Chamber Temp";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(515, 105);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(143, 20);
            this.label9.TabIndex = 20;
            this.label9.Text = "Chamber Temp SP";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(515, 158);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(139, 20);
            this.label10.TabIndex = 21;
            this.label10.Text = "Chamber Humidity";
            // 
            // textBoxChTempSP
            // 
            this.textBoxChTempSP.Location = new System.Drawing.Point(519, 129);
            this.textBoxChTempSP.Name = "textBoxChTempSP";
            this.textBoxChTempSP.Size = new System.Drawing.Size(100, 26);
            this.textBoxChTempSP.TabIndex = 22;
            // 
            // textBoxChHum
            // 
            this.textBoxChHum.Location = new System.Drawing.Point(519, 181);
            this.textBoxChHum.Name = "textBoxChHum";
            this.textBoxChHum.Size = new System.Drawing.Size(100, 26);
            this.textBoxChHum.TabIndex = 23;
            // 
            // textBoxChHumSP
            // 
            this.textBoxChHumSP.Location = new System.Drawing.Point(519, 237);
            this.textBoxChHumSP.Name = "textBoxChHumSP";
            this.textBoxChHumSP.Size = new System.Drawing.Size(100, 26);
            this.textBoxChHumSP.TabIndex = 24;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(519, 214);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(164, 20);
            this.label11.TabIndex = 25;
            this.label11.Text = "Chamber Humidity SP";
            // 
            // ValidationFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(781, 450);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.textBoxChHumSP);
            this.Controls.Add(this.textBoxChHum);
            this.Controls.Add(this.textBoxChTempSP);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textBoxChTemp);
            this.Controls.Add(this.textBoxProbeRes);
            this.Controls.Add(this.textBoxProbeTempCount);
            this.Controls.Add(this.textBoxProbeHumidity);
            this.Controls.Add(this.textBoxProbeTemp);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonChamber);
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
        private System.Windows.Forms.Button buttonChamber;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBoxProbeTemp;
        private System.Windows.Forms.TextBox textBoxProbeHumidity;
        private System.Windows.Forms.TextBox textBoxProbeTempCount;
        private System.Windows.Forms.TextBox textBoxProbeRes;
        private System.Windows.Forms.TextBox textBoxChTemp;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxChTempSP;
        private System.Windows.Forms.TextBox textBoxChHum;
        private System.Windows.Forms.TextBox textBoxChHumSP;
        private System.Windows.Forms.Label label11;
    }
}