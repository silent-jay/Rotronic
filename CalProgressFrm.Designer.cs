namespace Rotronic
{
    partial class CalProgressFrm
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
            this.button1 = new System.Windows.Forms.Button();
            this.comboBoxRotProbe = new System.Windows.Forms.ComboBox();
            this.textBoxTemp = new System.Windows.Forms.TextBox();
            this.textBoxHum = new System.Windows.Forms.TextBox();
            this.textBoxTempSP = new System.Windows.Forms.TextBox();
            this.textBoxHumSP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxSoak = new System.Windows.Forms.TextBox();
            this.dataGridViewCalProgress = new System.Windows.Forms.DataGridView();
            this.labelChamber = new System.Windows.Forms.Label();
            this.labelMirror = new System.Windows.Forms.Label();
            this.panelChamberStable = new System.Windows.Forms.Panel();
            this.panelMirrorStable = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewCalProgress)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(49, 711);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(193, 55);
            this.button1.TabIndex = 0;
            this.button1.Text = "Start Calibration";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // comboBoxRotProbe
            // 
            this.comboBoxRotProbe.FormattingEnabled = true;
            this.comboBoxRotProbe.Location = new System.Drawing.Point(49, 61);
            this.comboBoxRotProbe.Name = "comboBoxRotProbe";
            this.comboBoxRotProbe.Size = new System.Drawing.Size(389, 28);
            this.comboBoxRotProbe.TabIndex = 1;
            // 
            // textBoxTemp
            // 
            this.textBoxTemp.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxTemp.Location = new System.Drawing.Point(330, 591);
            this.textBoxTemp.Name = "textBoxTemp";
            this.textBoxTemp.Size = new System.Drawing.Size(197, 62);
            this.textBoxTemp.TabIndex = 2;
            // 
            // textBoxHum
            // 
            this.textBoxHum.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxHum.Location = new System.Drawing.Point(569, 592);
            this.textBoxHum.Name = "textBoxHum";
            this.textBoxHum.Size = new System.Drawing.Size(197, 62);
            this.textBoxHum.TabIndex = 3;
            // 
            // textBoxTempSP
            // 
            this.textBoxTempSP.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxTempSP.Location = new System.Drawing.Point(330, 703);
            this.textBoxTempSP.Name = "textBoxTempSP";
            this.textBoxTempSP.Size = new System.Drawing.Size(197, 62);
            this.textBoxTempSP.TabIndex = 4;
            // 
            // textBoxHumSP
            // 
            this.textBoxHumSP.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxHumSP.Location = new System.Drawing.Point(569, 704);
            this.textBoxHumSP.Name = "textBoxHumSP";
            this.textBoxHumSP.Size = new System.Drawing.Size(197, 62);
            this.textBoxHumSP.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(330, 565);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Temperature";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(330, 680);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(164, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Temperature Setpoint";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(565, 569);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "Humidity";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(565, 681);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(134, 20);
            this.label4.TabIndex = 9;
            this.label4.Text = "Humidity Setpoint";
            // 
            // textBoxSoak
            // 
            this.textBoxSoak.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxSoak.Location = new System.Drawing.Point(49, 643);
            this.textBoxSoak.Name = "textBoxSoak";
            this.textBoxSoak.Size = new System.Drawing.Size(193, 62);
            this.textBoxSoak.TabIndex = 10;
            // 
            // dataGridViewCalProgress
            // 
            this.dataGridViewCalProgress.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewCalProgress.Location = new System.Drawing.Point(49, 108);
            this.dataGridViewCalProgress.Name = "dataGridViewCalProgress";
            this.dataGridViewCalProgress.RowHeadersWidth = 62;
            this.dataGridViewCalProgress.RowTemplate.Height = 28;
            this.dataGridViewCalProgress.Size = new System.Drawing.Size(1641, 433);
            this.dataGridViewCalProgress.TabIndex = 11;
            // 
            // labelChamber
            // 
            this.labelChamber.AutoSize = true;
            this.labelChamber.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.labelChamber.Location = new System.Drawing.Point(878, 599);
            this.labelChamber.Name = "labelChamber";
            this.labelChamber.Size = new System.Drawing.Size(402, 55);
            this.labelChamber.TabIndex = 12;
            this.labelChamber.Text = "Chamber Stability";
            // 
            // labelMirror
            // 
            this.labelMirror.AutoSize = true;
            this.labelMirror.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.labelMirror.Location = new System.Drawing.Point(878, 710);
            this.labelMirror.Name = "labelMirror";
            this.labelMirror.Size = new System.Drawing.Size(328, 55);
            this.labelMirror.TabIndex = 13;
            this.labelMirror.Text = "Mirror Stability";
            // 
            // panelChamberStable
            // 
            this.panelChamberStable.BackColor = System.Drawing.Color.Red;
            this.panelChamberStable.Location = new System.Drawing.Point(841, 612);
            this.panelChamberStable.Name = "panelChamberStable";
            this.panelChamberStable.Size = new System.Drawing.Size(28, 28);
            this.panelChamberStable.TabIndex = 14;
            this.panelChamberStable.Paint += new System.Windows.Forms.PaintEventHandler(this.panelChamberStable_Paint);
            // 
            // panelMirrorStable
            // 
            this.panelMirrorStable.BackColor = System.Drawing.Color.Red;
            this.panelMirrorStable.Location = new System.Drawing.Point(841, 723);
            this.panelMirrorStable.Name = "panelMirrorStable";
            this.panelMirrorStable.Size = new System.Drawing.Size(28, 28);
            this.panelMirrorStable.TabIndex = 15;
            this.panelMirrorStable.Paint += new System.Windows.Forms.PaintEventHandler(this.panelMirrorStable_Paint);
            // 
            // CalProgressFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1736, 822);
            this.Controls.Add(this.panelMirrorStable);
            this.Controls.Add(this.panelChamberStable);
            this.Controls.Add(this.labelMirror);
            this.Controls.Add(this.labelChamber);
            this.Controls.Add(this.dataGridViewCalProgress);
            this.Controls.Add(this.textBoxSoak);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxHumSP);
            this.Controls.Add(this.textBoxTempSP);
            this.Controls.Add(this.textBoxHum);
            this.Controls.Add(this.textBoxTemp);
            this.Controls.Add(this.comboBoxRotProbe);
            this.Controls.Add(this.button1);
            this.Name = "CalProgressFrm";
            this.Text = "CalProgressFrm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewCalProgress)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox comboBoxRotProbe;
        private System.Windows.Forms.TextBox textBoxTemp;
        private System.Windows.Forms.TextBox textBoxHum;
        private System.Windows.Forms.TextBox textBoxTempSP;
        private System.Windows.Forms.TextBox textBoxHumSP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxSoak;
        private System.Windows.Forms.DataGridView dataGridViewCalProgress;
        private System.Windows.Forms.Label labelChamber;
        private System.Windows.Forms.Label labelMirror;
        private System.Windows.Forms.Panel panelChamberStable;
        private System.Windows.Forms.Panel panelMirrorStable;
    }
}