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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.newStepListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonSetup = new System.Windows.Forms.Button();
            this.dataGridViewStep = new System.Windows.Forms.DataGridView();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.Step = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.SetPointRH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnTemp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SoakTime = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.ColumnAccuracy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnAdjust = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBoxAdvTemp = new System.Windows.Forms.CheckBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStep)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newStepListToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(2017, 33);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // newStepListToolStripMenuItem
            // 
            this.newStepListToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newListToolStripMenuItem,
            this.loadListToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.newStepListToolStripMenuItem.Name = "newStepListToolStripMenuItem";
            this.newStepListToolStripMenuItem.Size = new System.Drawing.Size(73, 29);
            this.newStepListToolStripMenuItem.Text = "Menu";
            // 
            // newListToolStripMenuItem
            // 
            this.newListToolStripMenuItem.Name = "newListToolStripMenuItem";
            this.newListToolStripMenuItem.Size = new System.Drawing.Size(184, 34);
            this.newListToolStripMenuItem.Text = "New List";
            this.newListToolStripMenuItem.Click += new System.EventHandler(this.newListToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(184, 34);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // loadListToolStripMenuItem
            // 
            this.loadListToolStripMenuItem.Name = "loadListToolStripMenuItem";
            this.loadListToolStripMenuItem.Size = new System.Drawing.Size(184, 34);
            this.loadListToolStripMenuItem.Text = "Load List";
            this.loadListToolStripMenuItem.Click += new System.EventHandler(this.loadListToolStripMenuItem_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(225, 176);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(1035, 20);
            this.label5.TabIndex = 22;
            this.label5.Text = "Rotronic recommends setting temperature based on the conditions the probe will be" +
    " in normally. At least 3-4 points are recommended for humidity.";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(225, 156);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(1344, 20);
            this.label4.TabIndex = 21;
            this.label4.Text = "Clicking the checkbox for \"Adjustment Point?\" will save an adjustment point in th" +
    "e probes memory. Any number of points may be chosen for humidity. Only one may b" +
    "e chosen for Temperature";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(225, 136);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(486, 20);
            this.label3.TabIndex = 20;
            this.label3.Text = "Accuracy specification determines pass/fail criteria for the test point.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(225, 116);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(437, 20);
            this.label2.TabIndex = 19;
            this.label2.Text = "Temperature Step will collect data for temperature calibration";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(225, 96);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(381, 20);
            this.label1.TabIndex = 18;
            this.label1.Text = "Humidity Step will collect data for Humidity calibration";
            // 
            // buttonSetup
            // 
            this.buttonSetup.Location = new System.Drawing.Point(739, 782);
            this.buttonSetup.Name = "buttonSetup";
            this.buttonSetup.Size = new System.Drawing.Size(329, 66);
            this.buttonSetup.TabIndex = 16;
            this.buttonSetup.Text = "Calibration Setup";
            this.buttonSetup.UseVisualStyleBackColor = true;
            this.buttonSetup.Click += new System.EventHandler(this.buttonSetup_Click);
            // 
            // dataGridViewStep
            // 
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewStep.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewStep.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewStep.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Step,
            this.SetPointRH,
            this.ColumnTemp,
            this.SoakTime,
            this.ColumnAccuracy,
            this.ColumnAdjust});
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewStep.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridViewStep.Location = new System.Drawing.Point(229, 347);
            this.dataGridViewStep.Name = "dataGridViewStep";
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewStep.RowHeadersDefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridViewStep.RowHeadersWidth = 62;
            this.dataGridViewStep.RowTemplate.Height = 28;
            this.dataGridViewStep.Size = new System.Drawing.Size(1559, 335);
            this.dataGridViewStep.TabIndex = 15;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(225, 196);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(856, 20);
            this.label6.TabIndex = 23;
            this.label6.Text = "If the multi-point temperature adjustment is to be used, at least 4 test points a" +
    "re required, with points near 0 °C and 50 °C";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(225, 216);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(930, 20);
            this.label7.TabIndex = 26;
            this.label7.Text = "Best practice is to adjust temperature first before adjusting humidity, and to co" +
    "llect as-found data before performing any adjustment";
            // 
            // Step
            // 
            this.Step.HeaderText = "Step";
            this.Step.Items.AddRange(new object[] {
            "Humidity",
            "Temperature",
            "Adjust"});
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
            // ColumnAccuracy
            // 
            this.ColumnAccuracy.HeaderText = "Accuracy Specification";
            this.ColumnAccuracy.MinimumWidth = 8;
            this.ColumnAccuracy.Name = "ColumnAccuracy";
            this.ColumnAccuracy.Width = 150;
            // 
            // ColumnAdjust
            // 
            this.ColumnAdjust.HeaderText = "Adjustment Point?";
            this.ColumnAdjust.MinimumWidth = 8;
            this.ColumnAdjust.Name = "ColumnAdjust";
            this.ColumnAdjust.Width = 150;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(225, 76);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(1301, 20);
            this.label8.TabIndex = 27;
            this.label8.Text = "Adjust will send command to the probe to save adjustment points. This is recommen" +
    "ded after performing a single point temperature adjustment, or after a series of" +
    " humidity adjustments";
            // 
            // checkBoxAdvTemp
            // 
            this.checkBoxAdvTemp.AutoSize = true;
            this.checkBoxAdvTemp.Location = new System.Drawing.Point(229, 711);
            this.checkBoxAdvTemp.Name = "checkBoxAdvTemp";
            this.checkBoxAdvTemp.Size = new System.Drawing.Size(286, 24);
            this.checkBoxAdvTemp.TabIndex = 28;
            this.checkBoxAdvTemp.Text = "Advanced Temperature Adjustment";
            this.checkBoxAdvTemp.UseVisualStyleBackColor = true;
            // 
            // StepEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2017, 902);
            this.Controls.Add(this.checkBoxAdvTemp);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonSetup);
            this.Controls.Add(this.dataGridViewStep);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "StepEditor";
            this.Text = "AutoCal";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStep)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem newStepListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonSetup;
        private System.Windows.Forms.DataGridView dataGridViewStep;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DataGridViewComboBoxColumn Step;
        private System.Windows.Forms.DataGridViewTextBoxColumn SetPointRH;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnTemp;
        private System.Windows.Forms.DataGridViewComboBoxColumn SoakTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnAccuracy;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ColumnAdjust;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox checkBoxAdvTemp;
    }
}