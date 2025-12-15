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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dataGridViewStep = new System.Windows.Forms.DataGridView();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.newStepListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.validateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.buttonValidate = new System.Windows.Forms.Button();
            this.Step = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.SetPointRH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SoakTime = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.TempPassFail = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.TemperatureAccuracy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HumPassFail = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.HumidityAccuracy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStep)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridViewStep
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewStep.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewStep.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewStep.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Step,
            this.SetPointRH,
            this.ColumnTemp,
            this.SoakTime,
            this.TempPassFail,
            this.TemperatureAccuracy,
            this.HumPassFail,
            this.HumidityAccuracy});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewStep.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewStep.Location = new System.Drawing.Point(48, 58);
            this.dataGridViewStep.Name = "dataGridViewStep";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewStep.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridViewStep.RowHeadersWidth = 62;
            this.dataGridViewStep.RowTemplate.Height = 28;
            this.dataGridViewStep.Size = new System.Drawing.Size(2736, 1137);
            this.dataGridViewStep.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newStepListToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(2910, 33);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // newStepListToolStripMenuItem
            // 
            this.newStepListToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newListToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.loadListToolStripMenuItem,
            this.validateToolStripMenuItem});
            this.newStepListToolStripMenuItem.Name = "newStepListToolStripMenuItem";
            this.newStepListToolStripMenuItem.Size = new System.Drawing.Size(73, 29);
            this.newStepListToolStripMenuItem.Text = "Menu";
            // 
            // newListToolStripMenuItem
            // 
            this.newListToolStripMenuItem.Name = "newListToolStripMenuItem";
            this.newListToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.newListToolStripMenuItem.Text = "New List";
            this.newListToolStripMenuItem.Click += new System.EventHandler(this.newListToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // loadListToolStripMenuItem
            // 
            this.loadListToolStripMenuItem.Name = "loadListToolStripMenuItem";
            this.loadListToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.loadListToolStripMenuItem.Text = "Load List";
            this.loadListToolStripMenuItem.Click += new System.EventHandler(this.loadListToolStripMenuItem_Click);
            // 
            // validateToolStripMenuItem
            // 
            this.validateToolStripMenuItem.Name = "validateToolStripMenuItem";
            this.validateToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.validateToolStripMenuItem.Text = "Validate";
            this.validateToolStripMenuItem.Click += new System.EventHandler(this.validateToolStripMenuItem_Click);
            // 
            // buttonValidate
            // 
            this.buttonValidate.Location = new System.Drawing.Point(1204, 1238);
            this.buttonValidate.Name = "buttonValidate";
            this.buttonValidate.Size = new System.Drawing.Size(329, 66);
            this.buttonValidate.TabIndex = 5;
            this.buttonValidate.Text = "Validate";
            this.buttonValidate.UseVisualStyleBackColor = true;
            this.buttonValidate.Click += new System.EventHandler(this.buttonValidate_Click);
            // 
            // Step
            // 
            this.Step.HeaderText = "Step";
            this.Step.Items.AddRange(new object[] {
            "As-Found",
            "As-Left",
            "Final"});
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
            // ColumnTemp
            // 
            this.ColumnTemp.HeaderText = "Temperature Set Point (°C)";
            this.ColumnTemp.MinimumWidth = 8;
            this.ColumnTemp.Name = "ColumnTemp";
            this.ColumnTemp.Width = 150;
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
            // TempPassFail
            // 
            this.TempPassFail.HeaderText = "Evaluate Temperature";
            this.TempPassFail.MinimumWidth = 8;
            this.TempPassFail.Name = "TempPassFail";
            this.TempPassFail.Width = 150;
            // 
            // TemperatureAccuracy
            // 
            this.TemperatureAccuracy.HeaderText = "Temperature Accuracy";
            this.TemperatureAccuracy.MinimumWidth = 8;
            this.TemperatureAccuracy.Name = "TemperatureAccuracy";
            this.TemperatureAccuracy.Width = 150;
            // 
            // HumPassFail
            // 
            this.HumPassFail.HeaderText = "Evaluate Humdity?";
            this.HumPassFail.MinimumWidth = 8;
            this.HumPassFail.Name = "HumPassFail";
            this.HumPassFail.Width = 150;
            // 
            // HumidityAccuracy
            // 
            this.HumidityAccuracy.HeaderText = "Humidity Accuracy";
            this.HumidityAccuracy.MinimumWidth = 8;
            this.HumidityAccuracy.Name = "HumidityAccuracy";
            this.HumidityAccuracy.Width = 150;
            // 
            // StepEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2910, 1343);
            this.Controls.Add(this.buttonValidate);
            this.Controls.Add(this.dataGridViewStep);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "StepEditor";
            this.Text = "Step Editor";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStep)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewStep;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem newStepListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem validateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Button buttonValidate;
        private System.Windows.Forms.DataGridViewComboBoxColumn Step;
        private System.Windows.Forms.DataGridViewTextBoxColumn SetPointRH;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnTemp;
        private System.Windows.Forms.DataGridViewComboBoxColumn SoakTime;
        private System.Windows.Forms.DataGridViewCheckBoxColumn TempPassFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn TemperatureAccuracy;
        private System.Windows.Forms.DataGridViewCheckBoxColumn HumPassFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn HumidityAccuracy;
    }
}