namespace Rotronic
{
    partial class StepEditor
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            this.buttonSetup = new System.Windows.Forms.Button();
            this.dataGridViewStep = new System.Windows.Forms.DataGridView();
            this.Step = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.SetPointRH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SetPointTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SoakTime = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Accuracy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Adjust = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.richTextBoxDescription = new System.Windows.Forms.RichTextBox();
            this.comboBoxStepList = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStep)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonSetup
            // 
            this.buttonSetup.Location = new System.Drawing.Point(752, 934);
            this.buttonSetup.Name = "buttonSetup";
            this.buttonSetup.Size = new System.Drawing.Size(329, 66);
            this.buttonSetup.TabIndex = 16;
            this.buttonSetup.Text = "Calibration Setup";
            this.buttonSetup.UseVisualStyleBackColor = true;
            this.buttonSetup.Click += new System.EventHandler(this.buttonSetup_Click);
            // 
            // dataGridViewStep
            // 
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle13.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle13.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle13.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle13.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle13.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewStep.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle13;
            this.dataGridViewStep.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewStep.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Step,
            this.SetPointRH,
            this.SetPointTemp,
            this.SoakTime,
            this.Accuracy,
            this.Adjust});
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle14.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle14.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle14.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle14.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewStep.DefaultCellStyle = dataGridViewCellStyle14;
            this.dataGridViewStep.Location = new System.Drawing.Point(34, 152);
            this.dataGridViewStep.Name = "dataGridViewStep";
            dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle15.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle15.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle15.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle15.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle15.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle15.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewStep.RowHeadersDefaultCellStyle = dataGridViewCellStyle15;
            this.dataGridViewStep.RowHeadersWidth = 62;
            this.dataGridViewStep.RowTemplate.Height = 28;
            this.dataGridViewStep.Size = new System.Drawing.Size(1832, 604);
            this.dataGridViewStep.TabIndex = 15;
            // 
            // Step
            // 
            this.Step.HeaderText = "Step";
            this.Step.Items.AddRange(new object[] {
            "Humidity",
            "Temperature",
            "Adjust",
            "AdvancedTempStart",
            "AdvancedTempEnd",
            "Factory",
            "As-FoundStart",
            "As-FoundEnd",
            "As-LeftStart",
            "As-LeftEnd"});
            this.Step.MinimumWidth = 8;
            this.Step.Name = "Step";
            this.Step.Width = 250;
            // 
            // SetPointRH
            // 
            this.SetPointRH.HeaderText = "Humidity Set Point(%rh)";
            this.SetPointRH.MinimumWidth = 8;
            this.SetPointRH.Name = "SetPointRH";
            this.SetPointRH.Width = 150;
            // 
            // SetPointTemp
            // 
            this.SetPointTemp.HeaderText = "Temperature Set Point (°C)";
            this.SetPointTemp.MinimumWidth = 8;
            this.SetPointTemp.Name = "SetPointTemp";
            this.SetPointTemp.Width = 150;
            // 
            // SoakTime
            // 
            this.SoakTime.HeaderText = "Soak Time";
            this.SoakTime.Items.AddRange(new object[] {
            "00:15",
            "00:30",
            "00:45",
            "01:00",
            "01:15",
            "01:30",
            "01:45",
            "02:00"});
            this.SoakTime.MinimumWidth = 8;
            this.SoakTime.Name = "SoakTime";
            this.SoakTime.Width = 150;
            // 
            // Accuracy
            // 
            this.Accuracy.HeaderText = "Accuracy Specification";
            this.Accuracy.MinimumWidth = 8;
            this.Accuracy.Name = "Accuracy";
            this.Accuracy.Width = 150;
            // 
            // Adjust
            // 
            this.Adjust.HeaderText = "Adjustment Point?";
            this.Adjust.MinimumWidth = 8;
            this.Adjust.Name = "Adjust";
            this.Adjust.Width = 150;
            // 
            // richTextBoxDescription
            // 
            this.richTextBoxDescription.Location = new System.Drawing.Point(34, 781);
            this.richTextBoxDescription.Name = "richTextBoxDescription";
            this.richTextBoxDescription.Size = new System.Drawing.Size(908, 109);
            this.richTextBoxDescription.TabIndex = 17;
            this.richTextBoxDescription.Text = "Enter procedure description here";
            // 
            // comboBoxStepList
            // 
            this.comboBoxStepList.FormattingEnabled = true;
            this.comboBoxStepList.Location = new System.Drawing.Point(34, 100);
            this.comboBoxStepList.Name = "comboBoxStepList";
            this.comboBoxStepList.Size = new System.Drawing.Size(844, 28);
            this.comboBoxStepList.TabIndex = 18;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 77);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 20);
            this.label1.TabIndex = 19;
            this.label1.Text = "Procedure List";
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(955, 77);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(126, 50);
            this.buttonSave.TabIndex = 20;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // StepEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1898, 1024);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxStepList);
            this.Controls.Add(this.richTextBoxDescription);
            this.Controls.Add(this.buttonSetup);
            this.Controls.Add(this.dataGridViewStep);
            this.Name = "StepEditor";
            this.Text = "AutoCal";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStep)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonSetup;
        private System.Windows.Forms.DataGridView dataGridViewStep;
        private System.Windows.Forms.DataGridViewComboBoxColumn Step;
        private System.Windows.Forms.DataGridViewTextBoxColumn SetPointRH;
        private System.Windows.Forms.DataGridViewTextBoxColumn SetPointTemp;
        private System.Windows.Forms.DataGridViewComboBoxColumn SoakTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn Accuracy;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Adjust;
        private System.Windows.Forms.RichTextBox richTextBoxDescription;
        private System.Windows.Forms.ComboBox comboBoxStepList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonSave;
    }
}