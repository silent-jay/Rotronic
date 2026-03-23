namespace Rotronic
{
    partial class ProbeInventoryCalibrationData
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
            this.buttonExport = new System.Windows.Forms.Button();
            this.buttonCalCert = new System.Windows.Forms.Button();
            this.dataGridViewSteps = new System.Windows.Forms.DataGridView();
            this.dataGridViewSamples = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSteps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSamples)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonExport
            // 
            this.buttonExport.Location = new System.Drawing.Point(21, 19);
            this.buttonExport.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(80, 29);
            this.buttonExport.TabIndex = 0;
            this.buttonExport.Text = "Export Data";
            this.buttonExport.UseVisualStyleBackColor = true;
            this.buttonExport.Click += new System.EventHandler(this.buttonExport_Click);
            // 
            // buttonCalCert
            // 
            this.buttonCalCert.Location = new System.Drawing.Point(151, 19);
            this.buttonCalCert.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonCalCert.Name = "buttonCalCert";
            this.buttonCalCert.Size = new System.Drawing.Size(190, 29);
            this.buttonCalCert.TabIndex = 1;
            this.buttonCalCert.Text = "Generate Calibration Certificate";
            this.buttonCalCert.UseVisualStyleBackColor = true;
            this.buttonCalCert.Click += new System.EventHandler(this.buttonCalCert_Click);
            // 
            // dataGridViewSteps
            // 
            this.dataGridViewSteps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewSteps.Location = new System.Drawing.Point(8, 63);
            this.dataGridViewSteps.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dataGridViewSteps.Name = "dataGridViewSteps";
            this.dataGridViewSteps.ReadOnly = true;
            this.dataGridViewSteps.RowHeadersWidth = 62;
            this.dataGridViewSteps.RowTemplate.Height = 28;
            this.dataGridViewSteps.Size = new System.Drawing.Size(1249, 292);
            this.dataGridViewSteps.TabIndex = 2;
            // 
            // dataGridViewSamples
            // 
            this.dataGridViewSamples.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewSamples.Location = new System.Drawing.Point(8, 359);
            this.dataGridViewSamples.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dataGridViewSamples.Name = "dataGridViewSamples";
            this.dataGridViewSamples.ReadOnly = true;
            this.dataGridViewSamples.RowHeadersWidth = 62;
            this.dataGridViewSamples.RowTemplate.Height = 28;
            this.dataGridViewSamples.Size = new System.Drawing.Size(1249, 298);
            this.dataGridViewSamples.TabIndex = 3;
            // 
            // ProbeInventoryCalibrationData
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1265, 666);
            this.Controls.Add(this.dataGridViewSamples);
            this.Controls.Add(this.dataGridViewSteps);
            this.Controls.Add(this.buttonCalCert);
            this.Controls.Add(this.buttonExport);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "ProbeInventoryCalibrationData";
            this.Text = "ProbeInventoryCalibrationData";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSteps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSamples)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.Button buttonCalCert;
        private System.Windows.Forms.DataGridView dataGridViewSteps;
        private System.Windows.Forms.DataGridView dataGridViewSamples;
    }
}