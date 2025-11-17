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
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.headerViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewRotProbe = new System.Windows.Forms.ListView();
            this.label1 = new System.Windows.Forms.Label();
            this.listViewMirror = new System.Windows.Forms.ListView();
            this.label2 = new System.Windows.Forms.Label();
            this.mirrorViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.menuStrip1.Size = new System.Drawing.Size(2638, 33);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // calibrationsToolStripMenuItem
            // 
            this.calibrationsToolStripMenuItem.Name = "calibrationsToolStripMenuItem";
            this.calibrationsToolStripMenuItem.Size = new System.Drawing.Size(121, 29);
            this.calibrationsToolStripMenuItem.Text = "Calibrations";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.headerViewToolStripMenuItem,
            this.mirrorViewToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(92, 29);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // headerViewToolStripMenuItem
            // 
            this.headerViewToolStripMenuItem.Name = "headerViewToolStripMenuItem";
            this.headerViewToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.headerViewToolStripMenuItem.Text = "Probe View";
            this.headerViewToolStripMenuItem.Click += new System.EventHandler(this.headerViewToolStripMenuItem_Click);
            // 
            // listViewRotProbe
            // 
            this.listViewRotProbe.AllowColumnReorder = true;
            this.listViewRotProbe.HideSelection = false;
            this.listViewRotProbe.Location = new System.Drawing.Point(51, 117);
            this.listViewRotProbe.Name = "listViewRotProbe";
            this.listViewRotProbe.Size = new System.Drawing.Size(1817, 357);
            this.listViewRotProbe.TabIndex = 2;
            this.listViewRotProbe.UseCompatibleStateImageBehavior = false;
            this.listViewRotProbe.View = System.Windows.Forms.View.Details;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(51, 91);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "Probe Info";
            // 
            // listViewMirror
            // 
            this.listViewMirror.HideSelection = false;
            this.listViewMirror.Location = new System.Drawing.Point(63, 554);
            this.listViewMirror.Name = "listViewMirror";
            this.listViewMirror.Size = new System.Drawing.Size(1804, 187);
            this.listViewMirror.TabIndex = 4;
            this.listViewMirror.UseCompatibleStateImageBehavior = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(63, 528);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 20);
            this.label2.TabIndex = 5;
            this.label2.Text = "Standard List";
            // 
            // mirrorViewToolStripMenuItem
            // 
            this.mirrorViewToolStripMenuItem.Name = "mirrorViewToolStripMenuItem";
            this.mirrorViewToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.mirrorViewToolStripMenuItem.Text = "Mirror View";
            this.mirrorViewToolStripMenuItem.Click += new System.EventHandler(this.mirrorViewToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2638, 1104);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listViewMirror);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listViewRotProbe);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Main";
            this.Text = "Form1";
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
        private System.Windows.Forms.ListView listViewRotProbe;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listViewMirror;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripMenuItem mirrorViewToolStripMenuItem;
    }
}

