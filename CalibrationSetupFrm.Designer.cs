namespace Rotronic
{
    partial class CalibrationSetupFrm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView listViewRotProbe;
        private System.Windows.Forms.ListView listViewMirror;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;

        // New select-all checkboxes for the header column
        private System.Windows.Forms.CheckBox chkSelectAllProbes;
        private System.Windows.Forms.CheckBox chkSelectAllMirrors;

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
            this.listViewRotProbe = new System.Windows.Forms.ListView();
            this.listViewMirror = new System.Windows.Forms.ListView();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.chkSelectAllProbes = new System.Windows.Forms.CheckBox();
            this.chkSelectAllMirrors = new System.Windows.Forms.CheckBox();
            this.checkBoxManual = new System.Windows.Forms.CheckBox();
            this.buttonBegin = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listViewRotProbe
            // 
            this.listViewRotProbe.AllowColumnReorder = true;
            this.listViewRotProbe.HideSelection = false;
            this.listViewRotProbe.Location = new System.Drawing.Point(24, 56);
            this.listViewRotProbe.Name = "listViewRotProbe";
            this.listViewRotProbe.Size = new System.Drawing.Size(1880, 360);
            this.listViewRotProbe.TabIndex = 0;
            this.listViewRotProbe.UseCompatibleStateImageBehavior = false;
            this.listViewRotProbe.View = System.Windows.Forms.View.Details;
            // 
            // listViewMirror
            // 
            this.listViewMirror.HideSelection = false;
            this.listViewMirror.Location = new System.Drawing.Point(24, 446);
            this.listViewMirror.Name = "listViewMirror";
            this.listViewMirror.Size = new System.Drawing.Size(1880, 240);
            this.listViewMirror.TabIndex = 1;
            this.listViewMirror.UseCompatibleStateImageBehavior = false;
            this.listViewMirror.View = System.Windows.Forms.View.Details;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Probe Info";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 422);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Standard List";
            // 
            // chkSelectAllProbes
            // 
            this.chkSelectAllProbes.AutoSize = true;
            this.chkSelectAllProbes.Location = new System.Drawing.Point(28, 58);
            this.chkSelectAllProbes.Name = "chkSelectAllProbes";
            this.chkSelectAllProbes.Size = new System.Drawing.Size(22, 21);
            this.chkSelectAllProbes.TabIndex = 4;
            this.chkSelectAllProbes.UseVisualStyleBackColor = true;
            this.chkSelectAllProbes.CheckedChanged += new System.EventHandler(this.chkSelectAllProbes_CheckedChanged);
            // 
            // chkSelectAllMirrors
            // 
            this.chkSelectAllMirrors.AutoSize = true;
            this.chkSelectAllMirrors.Location = new System.Drawing.Point(28, 448);
            this.chkSelectAllMirrors.Name = "chkSelectAllMirrors";
            this.chkSelectAllMirrors.Size = new System.Drawing.Size(22, 21);
            this.chkSelectAllMirrors.TabIndex = 5;
            this.chkSelectAllMirrors.UseVisualStyleBackColor = true;
            this.chkSelectAllMirrors.CheckedChanged += new System.EventHandler(this.chkSelectAllMirrors_CheckedChanged);
            // 
            // checkBoxManual
            // 
            this.checkBoxManual.AutoSize = true;
            this.checkBoxManual.Checked = true;
            this.checkBoxManual.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxManual.Location = new System.Drawing.Point(28, 726);
            this.checkBoxManual.Name = "checkBoxManual";
            this.checkBoxManual.Size = new System.Drawing.Size(224, 24);
            this.checkBoxManual.TabIndex = 6;
            this.checkBoxManual.Text = "Manual Hygrogen Control?";
            this.checkBoxManual.UseVisualStyleBackColor = true;
            // 
            // buttonBegin
            // 
            this.buttonBegin.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonBegin.Location = new System.Drawing.Point(593, 726);
            this.buttonBegin.Name = "buttonBegin";
            this.buttonBegin.Size = new System.Drawing.Size(690, 66);
            this.buttonBegin.TabIndex = 7;
            this.buttonBegin.Text = "Begin Calibration!";
            this.buttonBegin.UseVisualStyleBackColor = true;
            this.buttonBegin.Click += new System.EventHandler(this.buttonBegin_Click);
            // 
            // CalibrationSetupFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2040, 833);
            this.Controls.Add(this.buttonBegin);
            this.Controls.Add(this.checkBoxManual);
            this.Controls.Add(this.chkSelectAllMirrors);
            this.Controls.Add(this.chkSelectAllProbes);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listViewMirror);
            this.Controls.Add(this.listViewRotProbe);
            this.Name = "CalibrationSetupFrm";
            this.Text = "Calibration Setup";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CalibrationSetupFrm_FormClosed);
            this.Load += new System.EventHandler(this.CalibrationSetupFrm_Load);
            this.Resize += new System.EventHandler(this.CalibrationSetupFrm_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxManual;
        private System.Windows.Forms.Button buttonBegin;
    }
}