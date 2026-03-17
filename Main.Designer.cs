namespace Rotronic
{
    partial class Main
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.calibrationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createStepListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.temperatureAdjToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.headerViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mirrorViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hygrogenDisplayOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.celsiusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.validationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hygrogenIPConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewRotProbe = new System.Windows.Forms.ListView();
            this.label1 = new System.Windows.Forms.Label();
            this.listViewMirror = new System.Windows.Forms.ListView();
            this.label2 = new System.Windows.Forms.Label();
            this.listViewChamber = new System.Windows.Forms.ListView();
            this.label3 = new System.Windows.Forms.Label();
            this.humidityAdjustToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.calibrationsToolStripMenuItem,
            this.optionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1939, 36);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // calibrationsToolStripMenuItem
            // 
            this.calibrationsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.createStepListToolStripMenuItem,
            this.temperatureAdjToolStripMenuItem,
            this.humidityAdjustToolStripMenuItem});
            this.calibrationsToolStripMenuItem.Name = "calibrationsToolStripMenuItem";
            this.calibrationsToolStripMenuItem.Size = new System.Drawing.Size(121, 29);
            this.calibrationsToolStripMenuItem.Text = "Calibrations";
            // 
            // createStepListToolStripMenuItem
            // 
            this.createStepListToolStripMenuItem.Name = "createStepListToolStripMenuItem";
            this.createStepListToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.createStepListToolStripMenuItem.Text = "AutoCal";
            this.createStepListToolStripMenuItem.Click += new System.EventHandler(this.createStepListToolStripMenuItem_Click);
            // 
            // temperatureAdjToolStripMenuItem
            // 
            this.temperatureAdjToolStripMenuItem.Name = "temperatureAdjToolStripMenuItem";
            this.temperatureAdjToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.temperatureAdjToolStripMenuItem.Text = "Temperature Adjust";
            this.temperatureAdjToolStripMenuItem.Click += new System.EventHandler(this.temperatureAdjToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.headerViewToolStripMenuItem,
            this.mirrorViewToolStripMenuItem,
            this.hygrogenDisplayOptionsToolStripMenuItem,
            this.celsiusToolStripMenuItem,
            this.validationToolStripMenuItem,
            this.hygrogenIPConfigToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(92, 29);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // headerViewToolStripMenuItem
            // 
            this.headerViewToolStripMenuItem.Name = "headerViewToolStripMenuItem";
            this.headerViewToolStripMenuItem.Size = new System.Drawing.Size(326, 34);
            this.headerViewToolStripMenuItem.Text = "Probe Display Options";
            this.headerViewToolStripMenuItem.Click += new System.EventHandler(this.headerViewToolStripMenuItem_Click);
            // 
            // mirrorViewToolStripMenuItem
            // 
            this.mirrorViewToolStripMenuItem.Name = "mirrorViewToolStripMenuItem";
            this.mirrorViewToolStripMenuItem.Size = new System.Drawing.Size(326, 34);
            this.mirrorViewToolStripMenuItem.Text = "Mirror Display Options";
            this.mirrorViewToolStripMenuItem.Click += new System.EventHandler(this.mirrorViewToolStripMenuItem_Click);
            // 
            // hygrogenDisplayOptionsToolStripMenuItem
            // 
            this.hygrogenDisplayOptionsToolStripMenuItem.Name = "hygrogenDisplayOptionsToolStripMenuItem";
            this.hygrogenDisplayOptionsToolStripMenuItem.Size = new System.Drawing.Size(326, 34);
            this.hygrogenDisplayOptionsToolStripMenuItem.Text = "Hygrogen Display Options";
            this.hygrogenDisplayOptionsToolStripMenuItem.Click += new System.EventHandler(this.hygrogenDisplayOptionsToolStripMenuItem_Click);
            // 
            // celsiusToolStripMenuItem
            // 
            this.celsiusToolStripMenuItem.Checked = true;
            this.celsiusToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.celsiusToolStripMenuItem.Name = "celsiusToolStripMenuItem";
            this.celsiusToolStripMenuItem.Size = new System.Drawing.Size(326, 34);
            this.celsiusToolStripMenuItem.Text = "Celsius Only?";
            this.celsiusToolStripMenuItem.Click += new System.EventHandler(this.celsiusToolStripMenuItem_Click);
            // 
            // validationToolStripMenuItem
            // 
            this.validationToolStripMenuItem.Name = "validationToolStripMenuItem";
            this.validationToolStripMenuItem.Size = new System.Drawing.Size(326, 34);
            this.validationToolStripMenuItem.Text = "Validation";
            this.validationToolStripMenuItem.Click += new System.EventHandler(this.validationToolStripMenuItem_Click);
            // 
            // hygrogenIPConfigToolStripMenuItem
            // 
            this.hygrogenIPConfigToolStripMenuItem.Name = "hygrogenIPConfigToolStripMenuItem";
            this.hygrogenIPConfigToolStripMenuItem.Size = new System.Drawing.Size(326, 34);
            this.hygrogenIPConfigToolStripMenuItem.Text = "Hygrogen IP Config";
            this.hygrogenIPConfigToolStripMenuItem.Click += new System.EventHandler(this.hygrogenIPConfigToolStripMenuItem_Click);
            // 
            // listViewRotProbe
            // 
            this.listViewRotProbe.AllowColumnReorder = true;
            this.listViewRotProbe.HideSelection = false;
            this.listViewRotProbe.Location = new System.Drawing.Point(51, 117);
            this.listViewRotProbe.Name = "listViewRotProbe";
            this.listViewRotProbe.Size = new System.Drawing.Size(1817, 220);
            this.listViewRotProbe.TabIndex = 2;
            this.listViewRotProbe.UseCompatibleStateImageBehavior = false;
            this.listViewRotProbe.View = System.Windows.Forms.View.Details;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(51, 91);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "Probe List";
            // 
            // listViewMirror
            // 
            this.listViewMirror.HideSelection = false;
            this.listViewMirror.Location = new System.Drawing.Point(51, 378);
            this.listViewMirror.Name = "listViewMirror";
            this.listViewMirror.Size = new System.Drawing.Size(1817, 187);
            this.listViewMirror.TabIndex = 4;
            this.listViewMirror.UseCompatibleStateImageBehavior = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(51, 355);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 20);
            this.label2.TabIndex = 5;
            this.label2.Text = "Standard List";
            // 
            // listViewChamber
            // 
            this.listViewChamber.HideSelection = false;
            this.listViewChamber.Location = new System.Drawing.Point(51, 643);
            this.listViewChamber.Name = "listViewChamber";
            this.listViewChamber.Size = new System.Drawing.Size(1817, 300);
            this.listViewChamber.TabIndex = 6;
            this.listViewChamber.UseCompatibleStateImageBehavior = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(51, 620);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(103, 20);
            this.label3.TabIndex = 7;
            this.label3.Text = "Chamber List";
            // 
            // humidityAdjustToolStripMenuItem
            // 
            this.humidityAdjustToolStripMenuItem.Name = "humidityAdjustToolStripMenuItem";
            this.humidityAdjustToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.humidityAdjustToolStripMenuItem.Text = "Humidity Adjust";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1939, 1027);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listViewChamber);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listViewMirror);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listViewRotProbe);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Main";
            this.Text = "Rotronic AutoCal V2";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem calibrationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem headerViewToolStripMenuItem;
        internal System.Windows.Forms.ListView listViewRotProbe;
        private System.Windows.Forms.Label label1;
        internal System.Windows.Forms.ListView listViewMirror;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripMenuItem mirrorViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createStepListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem temperatureAdjToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem celsiusToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem validationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hygrogenIPConfigToolStripMenuItem;
      internal System.Windows.Forms.ListView listViewChamber;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ToolStripMenuItem hygrogenDisplayOptionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem humidityAdjustToolStripMenuItem;
    }
}

