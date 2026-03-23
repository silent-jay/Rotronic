namespace Rotronic
{
    partial class StandardInventoryFrm
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
            this.comboBoxStandard = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.dataGridViewStandard = new System.Windows.Forms.DataGridView();
            this.ColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnSerial = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnCalibrationDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnCalibrationDueDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.buttonReverse = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStandard)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBoxStandard
            // 
            this.comboBoxStandard.FormattingEnabled = true;
            this.comboBoxStandard.Location = new System.Drawing.Point(31, 66);
            this.comboBoxStandard.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.comboBoxStandard.Name = "comboBoxStandard";
            this.comboBoxStandard.Size = new System.Drawing.Size(137, 21);
            this.comboBoxStandard.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 49);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Chamber/Mirror Data";
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Location = new System.Drawing.Point(237, 49);
            this.buttonUpdate.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(96, 34);
            this.buttonUpdate.TabIndex = 2;
            this.buttonUpdate.Text = "Update";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            this.buttonUpdate.Click += new System.EventHandler(this.buttonUpdate_Click);
            // 
            // dataGridViewStandard
            // 
            this.dataGridViewStandard.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewStandard.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnName,
            this.ColumnSerial,
            this.ColumnCalibrationDate,
            this.ColumnCalibrationDueDate});
            this.dataGridViewStandard.Location = new System.Drawing.Point(31, 96);
            this.dataGridViewStandard.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dataGridViewStandard.Name = "dataGridViewStandard";
            this.dataGridViewStandard.RowHeadersWidth = 62;
            this.dataGridViewStandard.RowTemplate.Height = 28;
            this.dataGridViewStandard.Size = new System.Drawing.Size(1227, 562);
            this.dataGridViewStandard.TabIndex = 3;
            // 
            // ColumnName
            // 
            this.ColumnName.HeaderText = "Name";
            this.ColumnName.MinimumWidth = 8;
            this.ColumnName.Name = "ColumnName";
            this.ColumnName.Width = 150;
            // 
            // ColumnSerial
            // 
            this.ColumnSerial.HeaderText = "Serial Number";
            this.ColumnSerial.MinimumWidth = 8;
            this.ColumnSerial.Name = "ColumnSerial";
            this.ColumnSerial.ReadOnly = true;
            this.ColumnSerial.Width = 150;
            // 
            // ColumnCalibrationDate
            // 
            this.ColumnCalibrationDate.HeaderText = "Calibration Date";
            this.ColumnCalibrationDate.MinimumWidth = 8;
            this.ColumnCalibrationDate.Name = "ColumnCalibrationDate";
            this.ColumnCalibrationDate.Width = 150;
            // 
            // ColumnCalibrationDueDate
            // 
            this.ColumnCalibrationDueDate.HeaderText = "Calibration Due Date";
            this.ColumnCalibrationDueDate.MinimumWidth = 8;
            this.ColumnCalibrationDueDate.Name = "ColumnCalibrationDueDate";
            this.ColumnCalibrationDueDate.Width = 150;
            // 
            // buttonReverse
            // 
            this.buttonReverse.Location = new System.Drawing.Point(338, 49);
            this.buttonReverse.Name = "buttonReverse";
            this.buttonReverse.Size = new System.Drawing.Size(121, 34);
            this.buttonReverse.TabIndex = 4;
            this.buttonReverse.Text = "Reverse Traceability";
            this.buttonReverse.UseVisualStyleBackColor = true;
            this.buttonReverse.Click += new System.EventHandler(this.buttonReverse_Click);
            // 
            // StandardInventoryFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1265, 666);
            this.Controls.Add(this.buttonReverse);
            this.Controls.Add(this.dataGridViewStandard);
            this.Controls.Add(this.buttonUpdate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxStandard);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "StandardInventoryFrm";
            this.Text = "StandardInventoryFrm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStandard)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxStandard;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonUpdate;
        private System.Windows.Forms.DataGridView dataGridViewStandard;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnSerial;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnCalibrationDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnCalibrationDueDate;
        private System.Windows.Forms.Button buttonReverse;
    }
}