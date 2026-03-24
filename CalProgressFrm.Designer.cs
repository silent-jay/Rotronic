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
            this.label5 = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxUser = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.buttonManage = new System.Windows.Forms.Button();
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
            this.textBoxTemp.Location = new System.Drawing.Point(271, 590);
            this.textBoxTemp.Name = "textBoxTemp";
            this.textBoxTemp.Size = new System.Drawing.Size(197, 62);
            this.textBoxTemp.TabIndex = 2;
            // 
            // textBoxHum
            // 
            this.textBoxHum.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxHum.Location = new System.Drawing.Point(510, 591);
            this.textBoxHum.Name = "textBoxHum";
            this.textBoxHum.Size = new System.Drawing.Size(266, 62);
            this.textBoxHum.TabIndex = 3;
            // 
            // textBoxTempSP
            // 
            this.textBoxTempSP.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxTempSP.Location = new System.Drawing.Point(271, 702);
            this.textBoxTempSP.Name = "textBoxTempSP";
            this.textBoxTempSP.Size = new System.Drawing.Size(197, 62);
            this.textBoxTempSP.TabIndex = 4;
            // 
            // textBoxHumSP
            // 
            this.textBoxHumSP.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxHumSP.Location = new System.Drawing.Point(510, 703);
            this.textBoxHumSP.Name = "textBoxHumSP";
            this.textBoxHumSP.Size = new System.Drawing.Size(266, 62);
            this.textBoxHumSP.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(271, 564);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Temperature";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(271, 679);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(164, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Temperature Setpoint";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(506, 568);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "Humidity";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(506, 680);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(134, 20);
            this.label4.TabIndex = 9;
            this.label4.Text = "Humidity Setpoint";
            // 
            // textBoxSoak
            // 
            this.textBoxSoak.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBoxSoak.Location = new System.Drawing.Point(49, 591);
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
            this.labelChamber.Location = new System.Drawing.Point(838, 590);
            this.labelChamber.Name = "labelChamber";
            this.labelChamber.Size = new System.Drawing.Size(402, 55);
            this.labelChamber.TabIndex = 12;
            this.labelChamber.Text = "Chamber Stability";
            // 
            // labelMirror
            // 
            this.labelMirror.AutoSize = true;
            this.labelMirror.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.labelMirror.Location = new System.Drawing.Point(838, 699);
            this.labelMirror.Name = "labelMirror";
            this.labelMirror.Size = new System.Drawing.Size(328, 55);
            this.labelMirror.TabIndex = 13;
            this.labelMirror.Text = "Mirror Stability";
            // 
            // panelChamberStable
            // 
            this.panelChamberStable.BackColor = System.Drawing.Color.Red;
            this.panelChamberStable.Location = new System.Drawing.Point(801, 603);
            this.panelChamberStable.Name = "panelChamberStable";
            this.panelChamberStable.Size = new System.Drawing.Size(28, 28);
            this.panelChamberStable.TabIndex = 14;
            this.panelChamberStable.Paint += new System.Windows.Forms.PaintEventHandler(this.panelChamberStable_Paint);
            // 
            // panelMirrorStable
            // 
            this.panelMirrorStable.BackColor = System.Drawing.Color.Red;
            this.panelMirrorStable.Location = new System.Drawing.Point(801, 712);
            this.panelMirrorStable.Name = "panelMirrorStable";
            this.panelMirrorStable.Size = new System.Drawing.Size(28, 28);
            this.panelMirrorStable.TabIndex = 15;
            this.panelMirrorStable.Paint += new System.Windows.Forms.PaintEventHandler(this.panelMirrorStable_Paint);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(45, 564);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(147, 20);
            this.label5.TabIndex = 16;
            this.label5.Text = "Soak/Sample Timer";
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(1246, 591);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(433, 87);
            this.richTextBox1.TabIndex = 17;
            this.richTextBox1.Text = "";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(1246, 568);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(51, 20);
            this.label6.TabIndex = 18;
            this.label6.Text = "Notes";
            // 
            // comboBoxUser
            // 
            this.comboBoxUser.FormattingEnabled = true;
            this.comboBoxUser.Items.AddRange(new object[] {
            "Jeremy Martin",
            "Laura Sweeton"});
            this.comboBoxUser.Location = new System.Drawing.Point(1246, 737);
            this.comboBoxUser.Name = "comboBoxUser";
            this.comboBoxUser.Size = new System.Drawing.Size(231, 28);
            this.comboBoxUser.TabIndex = 19;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(1242, 714);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(72, 20);
            this.label7.TabIndex = 20;
            this.label7.Text = "Operator";
            // 
            // buttonManage
            // 
            this.buttonManage.Location = new System.Drawing.Point(1506, 711);
            this.buttonManage.Name = "buttonManage";
            this.buttonManage.Size = new System.Drawing.Size(125, 54);
            this.buttonManage.TabIndex = 21;
            this.buttonManage.Text = "Manage";
            this.buttonManage.UseVisualStyleBackColor = true;
            this.buttonManage.Click += new System.EventHandler(this.buttonManage_Click);
            // 
            // CalProgressFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1736, 822);
            this.Controls.Add(this.buttonManage);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.comboBoxUser);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.label5);
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
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxUser;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button buttonManage;
    }
}