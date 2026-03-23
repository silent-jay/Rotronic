namespace Rotronic
{
    partial class ProbeInventoryFrm
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
            this.dataGridViewProbe = new System.Windows.Forms.DataGridView();
            this.buttonCal = new System.Windows.Forms.Button();
            this.DeviceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SerialNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DeviceModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FirmwareVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DeviceType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HumidityFactoryCorrection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HumidityUserCorrection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HumidityDriftCorrection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PT100CoeffA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PT100CoeffB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PT100CoeffC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TempOffset = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TempConversion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastCalibrationUtc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NextDueUtc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewProbe)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewProbe
            // 
            this.dataGridViewProbe.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewProbe.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DeviceName,
            this.SerialNumber,
            this.DeviceModel,
            this.FirmwareVersion,
            this.DeviceType,
            this.HumidityFactoryCorrection,
            this.HumidityUserCorrection,
            this.HumidityDriftCorrection,
            this.PT100CoeffA,
            this.PT100CoeffB,
            this.PT100CoeffC,
            this.TempOffset,
            this.TempConversion,
            this.LastCalibrationUtc,
            this.NextDueUtc});
            this.dataGridViewProbe.Location = new System.Drawing.Point(19, 100);
            this.dataGridViewProbe.Name = "dataGridViewProbe";
            this.dataGridViewProbe.RowHeadersWidth = 62;
            this.dataGridViewProbe.RowTemplate.Height = 28;
            this.dataGridViewProbe.Size = new System.Drawing.Size(1867, 912);
            this.dataGridViewProbe.TabIndex = 0;
            // 
            // buttonCal
            // 
            this.buttonCal.Location = new System.Drawing.Point(19, 37);
            this.buttonCal.Name = "buttonCal";
            this.buttonCal.Size = new System.Drawing.Size(238, 45);
            this.buttonCal.TabIndex = 1;
            this.buttonCal.Text = "View Calibration History";
            this.buttonCal.UseVisualStyleBackColor = true;
            this.buttonCal.Click += new System.EventHandler(this.buttonCal_Click);
            // 
            // DeviceName
            // 
            this.DeviceName.HeaderText = "Device Name";
            this.DeviceName.MinimumWidth = 8;
            this.DeviceName.Name = "DeviceName";
            this.DeviceName.ReadOnly = true;
            this.DeviceName.Width = 150;
            // 
            // SerialNumber
            // 
            this.SerialNumber.HeaderText = "Serial Number";
            this.SerialNumber.MinimumWidth = 8;
            this.SerialNumber.Name = "SerialNumber";
            this.SerialNumber.ReadOnly = true;
            this.SerialNumber.Width = 150;
            // 
            // DeviceModel
            // 
            this.DeviceModel.HeaderText = "Device Model";
            this.DeviceModel.MinimumWidth = 8;
            this.DeviceModel.Name = "DeviceModel";
            this.DeviceModel.ReadOnly = true;
            this.DeviceModel.Width = 150;
            // 
            // FirmwareVersion
            // 
            this.FirmwareVersion.HeaderText = "Firmware Version";
            this.FirmwareVersion.MinimumWidth = 8;
            this.FirmwareVersion.Name = "FirmwareVersion";
            this.FirmwareVersion.ReadOnly = true;
            this.FirmwareVersion.Width = 150;
            // 
            // DeviceType
            // 
            this.DeviceType.HeaderText = "Device Type";
            this.DeviceType.MinimumWidth = 8;
            this.DeviceType.Name = "DeviceType";
            this.DeviceType.ReadOnly = true;
            this.DeviceType.Width = 150;
            // 
            // HumidityFactoryCorrection
            // 
            this.HumidityFactoryCorrection.HeaderText = "Factory Humidity Correction";
            this.HumidityFactoryCorrection.MinimumWidth = 8;
            this.HumidityFactoryCorrection.Name = "HumidityFactoryCorrection";
            this.HumidityFactoryCorrection.ReadOnly = true;
            this.HumidityFactoryCorrection.Width = 150;
            // 
            // HumidityUserCorrection
            // 
            this.HumidityUserCorrection.HeaderText = "User Humidity Correction";
            this.HumidityUserCorrection.MinimumWidth = 8;
            this.HumidityUserCorrection.Name = "HumidityUserCorrection";
            this.HumidityUserCorrection.ReadOnly = true;
            this.HumidityUserCorrection.Width = 150;
            // 
            // HumidityDriftCorrection
            // 
            this.HumidityDriftCorrection.HeaderText = "Humidity Drift Correction";
            this.HumidityDriftCorrection.MinimumWidth = 8;
            this.HumidityDriftCorrection.Name = "HumidityDriftCorrection";
            this.HumidityDriftCorrection.ReadOnly = true;
            this.HumidityDriftCorrection.Width = 150;
            // 
            // PT100CoeffA
            // 
            this.PT100CoeffA.HeaderText = "CoeffA";
            this.PT100CoeffA.MinimumWidth = 8;
            this.PT100CoeffA.Name = "PT100CoeffA";
            this.PT100CoeffA.ReadOnly = true;
            this.PT100CoeffA.Width = 150;
            // 
            // PT100CoeffB
            // 
            this.PT100CoeffB.HeaderText = "CoeffB";
            this.PT100CoeffB.MinimumWidth = 8;
            this.PT100CoeffB.Name = "PT100CoeffB";
            this.PT100CoeffB.ReadOnly = true;
            this.PT100CoeffB.Width = 150;
            // 
            // PT100CoeffC
            // 
            this.PT100CoeffC.HeaderText = "CoeffC";
            this.PT100CoeffC.MinimumWidth = 8;
            this.PT100CoeffC.Name = "PT100CoeffC";
            this.PT100CoeffC.ReadOnly = true;
            this.PT100CoeffC.Width = 150;
            // 
            // TempOffset
            // 
            this.TempOffset.HeaderText = "Temperature Offset";
            this.TempOffset.MinimumWidth = 8;
            this.TempOffset.Name = "TempOffset";
            this.TempOffset.ReadOnly = true;
            this.TempOffset.Width = 150;
            // 
            // TempConversion
            // 
            this.TempConversion.HeaderText = "Temperature Conversion";
            this.TempConversion.MinimumWidth = 8;
            this.TempConversion.Name = "TempConversion";
            this.TempConversion.ReadOnly = true;
            this.TempConversion.Width = 150;
            // 
            // LastCalibrationUtc
            // 
            this.LastCalibrationUtc.HeaderText = "Last Calibration";
            this.LastCalibrationUtc.MinimumWidth = 8;
            this.LastCalibrationUtc.Name = "LastCalibrationUtc";
            this.LastCalibrationUtc.ReadOnly = true;
            this.LastCalibrationUtc.Width = 150;
            // 
            // NextDueUtc
            // 
            this.NextDueUtc.HeaderText = "Next Calibration Due";
            this.NextDueUtc.MinimumWidth = 8;
            this.NextDueUtc.Name = "NextDueUtc";
            this.NextDueUtc.ReadOnly = true;
            this.NextDueUtc.Width = 150;
            // 
            // ProbeInventoryFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1898, 1024);
            this.Controls.Add(this.buttonCal);
            this.Controls.Add(this.dataGridViewProbe);
            this.Name = "ProbeInventoryFrm";
            this.Text = "ProbeInventoryFrm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewProbe)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewProbe;
        private System.Windows.Forms.Button buttonCal;
        private System.Windows.Forms.DataGridViewTextBoxColumn DeviceName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SerialNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn DeviceModel;
        private System.Windows.Forms.DataGridViewTextBoxColumn FirmwareVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn DeviceType;
        private System.Windows.Forms.DataGridViewTextBoxColumn HumidityFactoryCorrection;
        private System.Windows.Forms.DataGridViewTextBoxColumn HumidityUserCorrection;
        private System.Windows.Forms.DataGridViewTextBoxColumn HumidityDriftCorrection;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoeffA;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoeffB;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoeffC;
        private System.Windows.Forms.DataGridViewTextBoxColumn TempOffset;
        private System.Windows.Forms.DataGridViewTextBoxColumn TempConversion;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastCalibrationUtc;
        private System.Windows.Forms.DataGridViewTextBoxColumn NextDueUtc;
        private System.Windows.Forms.DataGridViewTextBoxColumn PT100CoeffA;
        private System.Windows.Forms.DataGridViewTextBoxColumn PT100CoeffB;
        private System.Windows.Forms.DataGridViewTextBoxColumn PT100CoeffC;
    }
}