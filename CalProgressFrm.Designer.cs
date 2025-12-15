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
            this.maskedTextBoxTime = new System.Windows.Forms.MaskedTextBox();
            this.buttonSkip = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.ProbeName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MirrorName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StepNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProbeTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MirrorTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TemperatureSetpoint = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TempError = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TemperaturePass = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProbeHumidity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MirrorHumdity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HumiditySetpoint = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HumdityError = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HumdityPass = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // maskedTextBoxTime
            // 
            this.maskedTextBoxTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.maskedTextBoxTime.Location = new System.Drawing.Point(88, 1081);
            this.maskedTextBoxTime.Name = "maskedTextBoxTime";
            this.maskedTextBoxTime.ReadOnly = true;
            this.maskedTextBoxTime.Size = new System.Drawing.Size(665, 170);
            this.maskedTextBoxTime.TabIndex = 0;
            // 
            // buttonSkip
            // 
            this.buttonSkip.Location = new System.Drawing.Point(88, 1279);
            this.buttonSkip.Name = "buttonSkip";
            this.buttonSkip.Size = new System.Drawing.Size(363, 60);
            this.buttonSkip.TabIndex = 1;
            this.buttonSkip.Text = "Skip Soak Time";
            this.buttonSkip.UseVisualStyleBackColor = true;
            this.buttonSkip.Click += new System.EventHandler(this.buttonSkip_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(88, 1035);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Soak Timer";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProbeName,
            this.MirrorName,
            this.StepNumber,
            this.ProbeTemp,
            this.MirrorTemp,
            this.TemperatureSetpoint,
            this.TempError,
            this.TemperaturePass,
            this.ProbeHumidity,
            this.MirrorHumdity,
            this.HumiditySetpoint,
            this.HumdityError,
            this.HumdityPass});
            this.dataGridView1.Location = new System.Drawing.Point(88, 76);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersWidth = 62;
            this.dataGridView1.RowTemplate.Height = 28;
            this.dataGridView1.Size = new System.Drawing.Size(2709, 801);
            this.dataGridView1.TabIndex = 3;
            // 
            // ProbeName
            // 
            this.ProbeName.HeaderText = "Probe Name";
            this.ProbeName.MinimumWidth = 8;
            this.ProbeName.Name = "ProbeName";
            this.ProbeName.ReadOnly = true;
            this.ProbeName.Width = 150;
            // 
            // MirrorName
            // 
            this.MirrorName.HeaderText = "Mirror Name";
            this.MirrorName.MinimumWidth = 8;
            this.MirrorName.Name = "MirrorName";
            this.MirrorName.ReadOnly = true;
            this.MirrorName.Width = 150;
            // 
            // StepNumber
            // 
            this.StepNumber.HeaderText = "Step";
            this.StepNumber.MinimumWidth = 8;
            this.StepNumber.Name = "StepNumber";
            this.StepNumber.ReadOnly = true;
            this.StepNumber.Width = 80;
            // 
            // ProbeTemp
            // 
            this.ProbeTemp.HeaderText = "Probe Temperature";
            this.ProbeTemp.MinimumWidth = 8;
            this.ProbeTemp.Name = "ProbeTemp";
            this.ProbeTemp.ReadOnly = true;
            this.ProbeTemp.Width = 150;
            // 
            // MirrorTemp
            // 
            this.MirrorTemp.HeaderText = "Mirror Temperature";
            this.MirrorTemp.MinimumWidth = 8;
            this.MirrorTemp.Name = "MirrorTemp";
            this.MirrorTemp.ReadOnly = true;
            this.MirrorTemp.Width = 150;
            // 
            // TemperatureSetpoint
            // 
            this.TemperatureSetpoint.HeaderText = "Temp Setpoint";
            this.TemperatureSetpoint.MinimumWidth = 8;
            this.TemperatureSetpoint.Name = "TemperatureSetpoint";
            this.TemperatureSetpoint.ReadOnly = true;
            this.TemperatureSetpoint.Width = 120;
            // 
            // TempError
            // 
            this.TempError.HeaderText = "Temperature Error";
            this.TempError.MinimumWidth = 8;
            this.TempError.Name = "TempError";
            this.TempError.ReadOnly = true;
            this.TempError.Width = 150;
            // 
            // TemperaturePass
            // 
            this.TemperaturePass.HeaderText = "Temp Pass?";
            this.TemperaturePass.MinimumWidth = 8;
            this.TemperaturePass.Name = "TemperaturePass";
            this.TemperaturePass.ReadOnly = true;
            this.TemperaturePass.Width = 150;
            // 
            // ProbeHumidity
            // 
            this.ProbeHumidity.HeaderText = "Probe Humidity";
            this.ProbeHumidity.MinimumWidth = 8;
            this.ProbeHumidity.Name = "ProbeHumidity";
            this.ProbeHumidity.ReadOnly = true;
            this.ProbeHumidity.Width = 150;
            // 
            // MirrorHumdity
            // 
            this.MirrorHumdity.HeaderText = "Mirror Humidity";
            this.MirrorHumdity.MinimumWidth = 8;
            this.MirrorHumdity.Name = "MirrorHumdity";
            this.MirrorHumdity.ReadOnly = true;
            this.MirrorHumdity.Width = 150;
            // 
            // HumiditySetpoint
            // 
            this.HumiditySetpoint.HeaderText = "RH Setpoint";
            this.HumiditySetpoint.MinimumWidth = 8;
            this.HumiditySetpoint.Name = "HumiditySetpoint";
            this.HumiditySetpoint.ReadOnly = true;
            this.HumiditySetpoint.Width = 120;
            // 
            // HumdityError
            // 
            this.HumdityError.HeaderText = "HumdityError";
            this.HumdityError.MinimumWidth = 8;
            this.HumdityError.Name = "HumdityError";
            this.HumdityError.ReadOnly = true;
            this.HumdityError.Width = 150;
            // 
            // HumdityPass
            // 
            this.HumdityPass.HeaderText = "Humdity Pass?";
            this.HumdityPass.MinimumWidth = 8;
            this.HumdityPass.Name = "HumdityPass";
            this.HumdityPass.ReadOnly = true;
            this.HumdityPass.Width = 150;
            // 
            // CalProgressFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2862, 1385);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonSkip);
            this.Controls.Add(this.maskedTextBoxTime);
            this.Name = "CalProgressFrm";
            this.Text = "Calibration Progress";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MaskedTextBox maskedTextBoxTime;
        private System.Windows.Forms.Button buttonSkip;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProbeName;
        private System.Windows.Forms.DataGridViewTextBoxColumn MirrorName;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProbeTemp;
        private System.Windows.Forms.DataGridViewTextBoxColumn MirrorTemp;
        private System.Windows.Forms.DataGridViewTextBoxColumn TemperatureSetpoint;
        private System.Windows.Forms.DataGridViewTextBoxColumn TempError;
        private System.Windows.Forms.DataGridViewTextBoxColumn TemperaturePass;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProbeHumidity;
        private System.Windows.Forms.DataGridViewTextBoxColumn MirrorHumdity;
        private System.Windows.Forms.DataGridViewTextBoxColumn HumiditySetpoint;
        private System.Windows.Forms.DataGridViewTextBoxColumn HumdityError;
        private System.Windows.Forms.DataGridViewTextBoxColumn HumdityPass;
    }
}