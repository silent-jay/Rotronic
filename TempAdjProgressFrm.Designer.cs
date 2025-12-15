namespace Rotronic
{
    partial class TempAdjProgressFrm
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
            this.label1 = new System.Windows.Forms.Label();
            this.buttonSkip = new System.Windows.Forms.Button();
            this.maskedTextBoxTime = new System.Windows.Forms.MaskedTextBox();
            this.dataGridViewAdjData = new System.Windows.Forms.DataGridView();
            this.ProbeName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MirrorTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProbeTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProbeHumidity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MirrorHumidity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CoeffA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CoeffB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Resistance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Offset = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.dataGridViewStepList = new System.Windows.Forms.DataGridView();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAdjData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStepList)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(70, 1021);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 20);
            this.label1.TabIndex = 5;
            this.label1.Text = "Soak Timer";
            // 
            // buttonSkip
            // 
            this.buttonSkip.Location = new System.Drawing.Point(437, 1267);
            this.buttonSkip.Name = "buttonSkip";
            this.buttonSkip.Size = new System.Drawing.Size(298, 60);
            this.buttonSkip.TabIndex = 4;
            this.buttonSkip.Text = "Skip Soak Time";
            this.buttonSkip.UseVisualStyleBackColor = true;
            this.buttonSkip.Click += new System.EventHandler(this.buttonSkip_Click);
            // 
            // maskedTextBoxTime
            // 
            this.maskedTextBoxTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.maskedTextBoxTime.Location = new System.Drawing.Point(70, 1067);
            this.maskedTextBoxTime.Name = "maskedTextBoxTime";
            this.maskedTextBoxTime.ReadOnly = true;
            this.maskedTextBoxTime.Size = new System.Drawing.Size(665, 116);
            this.maskedTextBoxTime.TabIndex = 3;
            // 
            // dataGridViewAdjData
            // 
            this.dataGridViewAdjData.AllowUserToAddRows = false;
            this.dataGridViewAdjData.AllowUserToDeleteRows = false;
            this.dataGridViewAdjData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewAdjData.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProbeName,
            this.MirrorTemp,
            this.ProbeTemp,
            this.ProbeHumidity,
            this.MirrorHumidity,
            this.CoeffA,
            this.CoeffB,
            this.Resistance,
            this.Offset});
            this.dataGridViewAdjData.Location = new System.Drawing.Point(118, 93);
            this.dataGridViewAdjData.Name = "dataGridViewAdjData";
            this.dataGridViewAdjData.ReadOnly = true;
            this.dataGridViewAdjData.RowHeadersWidth = 62;
            this.dataGridViewAdjData.RowTemplate.Height = 28;
            this.dataGridViewAdjData.Size = new System.Drawing.Size(2116, 855);
            this.dataGridViewAdjData.TabIndex = 6;
            // 
            // ProbeName
            // 
            this.ProbeName.HeaderText = "Probe Name";
            this.ProbeName.MinimumWidth = 8;
            this.ProbeName.Name = "ProbeName";
            this.ProbeName.ReadOnly = true;
            this.ProbeName.Width = 150;
            // 
            // MirrorTemp
            // 
            this.MirrorTemp.HeaderText = "Mirror Temp (°C)";
            this.MirrorTemp.MinimumWidth = 8;
            this.MirrorTemp.Name = "MirrorTemp";
            this.MirrorTemp.ReadOnly = true;
            this.MirrorTemp.Width = 150;
            // 
            // ProbeTemp
            // 
            this.ProbeTemp.HeaderText = "Probe Temp (°C)";
            this.ProbeTemp.MinimumWidth = 8;
            this.ProbeTemp.Name = "ProbeTemp";
            this.ProbeTemp.ReadOnly = true;
            this.ProbeTemp.Width = 150;
            // 
            // ProbeHumidity
            // 
            this.ProbeHumidity.HeaderText = "ProbeHumidity";
            this.ProbeHumidity.MinimumWidth = 8;
            this.ProbeHumidity.Name = "ProbeHumidity";
            this.ProbeHumidity.ReadOnly = true;
            this.ProbeHumidity.Width = 150;
            // 
            // MirrorHumidity
            // 
            this.MirrorHumidity.HeaderText = "MirrorHumidity";
            this.MirrorHumidity.MinimumWidth = 8;
            this.MirrorHumidity.Name = "MirrorHumidity";
            this.MirrorHumidity.ReadOnly = true;
            this.MirrorHumidity.Width = 150;
            // 
            // CoeffA
            // 
            this.CoeffA.HeaderText = "CoeffA";
            this.CoeffA.MinimumWidth = 8;
            this.CoeffA.Name = "CoeffA";
            this.CoeffA.ReadOnly = true;
            this.CoeffA.Width = 150;
            // 
            // CoeffB
            // 
            this.CoeffB.HeaderText = "CoeffB";
            this.CoeffB.MinimumWidth = 8;
            this.CoeffB.Name = "CoeffB";
            this.CoeffB.ReadOnly = true;
            this.CoeffB.Width = 150;
            // 
            // Resistance
            // 
            this.Resistance.HeaderText = "Resistance";
            this.Resistance.MinimumWidth = 8;
            this.Resistance.Name = "Resistance";
            this.Resistance.ReadOnly = true;
            this.Resistance.Width = 150;
            // 
            // Offset
            // 
            this.Offset.HeaderText = "Offset";
            this.Offset.MinimumWidth = 8;
            this.Offset.Name = "Offset";
            this.Offset.ReadOnly = true;
            this.Offset.Width = 150;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 20;
            this.listBox1.Location = new System.Drawing.Point(784, 1067);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(817, 244);
            this.listBox1.TabIndex = 7;
            // 
            // dataGridViewStepList
            // 
            this.dataGridViewStepList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewStepList.Location = new System.Drawing.Point(1640, 1067);
            this.dataGridViewStepList.Name = "dataGridViewStepList";
            this.dataGridViewStepList.RowHeadersWidth = 62;
            this.dataGridViewStepList.RowTemplate.Height = 28;
            this.dataGridViewStepList.Size = new System.Drawing.Size(578, 244);
            this.dataGridViewStepList.TabIndex = 8;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(74, 1267);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(275, 59);
            this.button1.TabIndex = 9;
            this.button1.Text = "Start Sequence";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // TempAdjProgressFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2309, 1379);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dataGridViewStepList);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.dataGridViewAdjData);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonSkip);
            this.Controls.Add(this.maskedTextBoxTime);
            this.Name = "TempAdjProgressFrm";
            this.Text = "TempAdjProgressFrm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAdjData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStepList)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonSkip;
        private System.Windows.Forms.MaskedTextBox maskedTextBoxTime;
        private System.Windows.Forms.DataGridView dataGridViewAdjData;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProbeName;
        private System.Windows.Forms.DataGridViewTextBoxColumn MirrorTemp;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProbeTemp;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProbeHumidity;
        private System.Windows.Forms.DataGridViewTextBoxColumn MirrorHumidity;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoeffA;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoeffB;
        private System.Windows.Forms.DataGridViewTextBoxColumn Resistance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Offset;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.DataGridView dataGridViewStepList;
        private System.Windows.Forms.Button button1;
    }
}