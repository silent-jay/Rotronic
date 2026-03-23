namespace Rotronic
{
    partial class ProbeInventoryCalHistoryFrm
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
            this.dataGridViewCalibrationHistory = new System.Windows.Forms.DataGridView();
            this.richTextBoxData = new System.Windows.Forms.RichTextBox();
            this.buttonCalEvent = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewCalibrationHistory)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewCalibrationHistory
            // 
            this.dataGridViewCalibrationHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewCalibrationHistory.Location = new System.Drawing.Point(12, 125);
            this.dataGridViewCalibrationHistory.Name = "dataGridViewCalibrationHistory";
            this.dataGridViewCalibrationHistory.RowHeadersWidth = 62;
            this.dataGridViewCalibrationHistory.RowTemplate.Height = 28;
            this.dataGridViewCalibrationHistory.Size = new System.Drawing.Size(1874, 530);
            this.dataGridViewCalibrationHistory.TabIndex = 0;
            // 
            // richTextBoxData
            // 
            this.richTextBoxData.Location = new System.Drawing.Point(31, 661);
            this.richTextBoxData.Name = "richTextBoxData";
            this.richTextBoxData.Size = new System.Drawing.Size(713, 321);
            this.richTextBoxData.TabIndex = 1;
            this.richTextBoxData.Text = "";
            // 
            // buttonCalEvent
            // 
            this.buttonCalEvent.Location = new System.Drawing.Point(21, 35);
            this.buttonCalEvent.Name = "buttonCalEvent";
            this.buttonCalEvent.Size = new System.Drawing.Size(267, 55);
            this.buttonCalEvent.TabIndex = 2;
            this.buttonCalEvent.Text = "View Calibration Data";
            this.buttonCalEvent.UseVisualStyleBackColor = true;
            this.buttonCalEvent.Click += new System.EventHandler(this.buttonCalEvent_Click);
            // 
            // ProbeInventoryCalHistoryFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1898, 1024);
            this.Controls.Add(this.buttonCalEvent);
            this.Controls.Add(this.richTextBoxData);
            this.Controls.Add(this.dataGridViewCalibrationHistory);
            this.Name = "ProbeInventoryCalHistoryFrm";
            this.Text = "ProbeInventoryCalHistoryFrm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewCalibrationHistory)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewCalibrationHistory;
        private System.Windows.Forms.RichTextBox richTextBoxData;
        private System.Windows.Forms.Button buttonCalEvent;
    }
}