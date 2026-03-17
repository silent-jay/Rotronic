namespace Rotronic
{
    partial class ChamberControllerFrm
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxHum = new System.Windows.Forms.TextBox();
            this.textBoxTemp = new System.Windows.Forms.TextBox();
            this.checkBoxHumControl = new System.Windows.Forms.CheckBox();
            this.checkBoxTempControl = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(68, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(138, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Humidity Setpoint:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(501, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(164, 20);
            this.label2.TabIndex = 1;
            this.label2.Text = "Temperature Setpoint";
            // 
            // textBoxHum
            // 
            this.textBoxHum.Font = new System.Drawing.Font("Microsoft Sans Serif", 32F);
            this.textBoxHum.Location = new System.Drawing.Point(77, 114);
            this.textBoxHum.MaxLength = 6;
            this.textBoxHum.Name = "textBoxHum";
            this.textBoxHum.Size = new System.Drawing.Size(218, 80);
            this.textBoxHum.TabIndex = 2;
            // 
            // textBoxTemp
            // 
            this.textBoxTemp.Font = new System.Drawing.Font("Microsoft Sans Serif", 32F);
            this.textBoxTemp.Location = new System.Drawing.Point(505, 114);
            this.textBoxTemp.MaxLength = 5;
            this.textBoxTemp.Name = "textBoxTemp";
            this.textBoxTemp.Size = new System.Drawing.Size(218, 80);
            this.textBoxTemp.TabIndex = 3;
            // 
            // checkBoxHumControl
            // 
            this.checkBoxHumControl.AutoSize = true;
            this.checkBoxHumControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F);
            this.checkBoxHumControl.Location = new System.Drawing.Point(72, 200);
            this.checkBoxHumControl.Name = "checkBoxHumControl";
            this.checkBoxHumControl.Size = new System.Drawing.Size(332, 41);
            this.checkBoxHumControl.TabIndex = 4;
            this.checkBoxHumControl.Text = "Humidity Control On";
            this.checkBoxHumControl.UseVisualStyleBackColor = true;
            // 
            // checkBoxTempControl
            // 
            this.checkBoxTempControl.AutoSize = true;
            this.checkBoxTempControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F);
            this.checkBoxTempControl.Location = new System.Drawing.Point(505, 200);
            this.checkBoxTempControl.Name = "checkBoxTempControl";
            this.checkBoxTempControl.Size = new System.Drawing.Size(391, 41);
            this.checkBoxTempControl.TabIndex = 5;
            this.checkBoxTempControl.Text = "Temperature Control On";
            this.checkBoxTempControl.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(72, 288);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(144, 62);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(505, 288);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(144, 62);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // ChamberController
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(987, 447);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkBoxTempControl);
            this.Controls.Add(this.checkBoxHumControl);
            this.Controls.Add(this.textBoxTemp);
            this.Controls.Add(this.textBoxHum);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "ChamberController";
            this.Text = "Chamber Controller";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxHum;
        private System.Windows.Forms.TextBox textBoxTemp;
        private System.Windows.Forms.CheckBox checkBoxHumControl;
        private System.Windows.Forms.CheckBox checkBoxTempControl;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}