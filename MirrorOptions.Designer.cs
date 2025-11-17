namespace Rotronic
{
    partial class MirrorOptions
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
            this.listBoxDNS = new System.Windows.Forms.ListBox();
            this.listBoxShow = new System.Windows.Forms.ListBox();
            this.buttonCan = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonDwn = new System.Windows.Forms.Button();
            this.buttonUp = new System.Windows.Forms.Button();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.buttonDel = new System.Windows.Forms.Button();
            this.labelDontShow = new System.Windows.Forms.Label();
            this.labelShow = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listBoxDNS
            // 
            this.listBoxDNS.FormattingEnabled = true;
            this.listBoxDNS.ItemHeight = 20;
            this.listBoxDNS.Location = new System.Drawing.Point(849, 122);
            this.listBoxDNS.Name = "listBoxDNS";
            this.listBoxDNS.Size = new System.Drawing.Size(428, 744);
            this.listBoxDNS.TabIndex = 17;
            // 
            // listBoxShow
            // 
            this.listBoxShow.FormattingEnabled = true;
            this.listBoxShow.ItemHeight = 20;
            this.listBoxShow.Location = new System.Drawing.Point(83, 122);
            this.listBoxShow.Name = "listBoxShow";
            this.listBoxShow.Size = new System.Drawing.Size(387, 744);
            this.listBoxShow.TabIndex = 16;
            // 
            // buttonCan
            // 
            this.buttonCan.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCan.Location = new System.Drawing.Point(849, 915);
            this.buttonCan.Name = "buttonCan";
            this.buttonCan.Size = new System.Drawing.Size(115, 50);
            this.buttonCan.TabIndex = 15;
            this.buttonCan.Text = "Cancel";
            this.buttonCan.UseVisualStyleBackColor = true;
            this.buttonCan.Click += new System.EventHandler(this.buttonCan_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(355, 915);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(115, 50);
            this.buttonOK.TabIndex = 14;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonDwn
            // 
            this.buttonDwn.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDwn.Location = new System.Drawing.Point(476, 500);
            this.buttonDwn.Name = "buttonDwn";
            this.buttonDwn.Size = new System.Drawing.Size(66, 96);
            this.buttonDwn.TabIndex = 12;
            this.buttonDwn.Text = "↓";
            this.buttonDwn.UseVisualStyleBackColor = true;
            this.buttonDwn.Click += new System.EventHandler(this.buttonDwn_Click);
            // 
            // buttonUp
            // 
            this.buttonUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonUp.Location = new System.Drawing.Point(476, 396);
            this.buttonUp.Name = "buttonUp";
            this.buttonUp.Size = new System.Drawing.Size(66, 96);
            this.buttonUp.TabIndex = 11;
            this.buttonUp.Text = "↑";
            this.buttonUp.UseVisualStyleBackColor = true;
            this.buttonUp.Click += new System.EventHandler(this.buttonUp_Click);
            // 
            // buttonAdd
            // 
            this.buttonAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAdd.Location = new System.Drawing.Point(592, 601);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(97, 53);
            this.buttonAdd.TabIndex = 9;
            this.buttonAdd.Text = "←";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
            // 
            // buttonDel
            // 
            this.buttonDel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDel.Location = new System.Drawing.Point(592, 343);
            this.buttonDel.Name = "buttonDel";
            this.buttonDel.Size = new System.Drawing.Size(97, 53);
            this.buttonDel.TabIndex = 8;
            this.buttonDel.Text = "→";
            this.buttonDel.UseVisualStyleBackColor = true;
            this.buttonDel.Click += new System.EventHandler(this.buttonDel_Click);
            // 
            // labelDontShow
            // 
            this.labelDontShow.AutoSize = true;
            this.labelDontShow.Location = new System.Drawing.Point(845, 81);
            this.labelDontShow.Name = "labelDontShow";
            this.labelDontShow.Size = new System.Drawing.Size(122, 20);
            this.labelDontShow.TabIndex = 13;
            this.labelDontShow.Text = "Available Fields:";
            // 
            // labelShow
            // 
            this.labelShow.AutoSize = true;
            this.labelShow.Location = new System.Drawing.Point(79, 81);
            this.labelShow.Name = "labelShow";
            this.labelShow.Size = new System.Drawing.Size(130, 20);
            this.labelShow.TabIndex = 10;
            this.labelShow.Text = "Display Columns:";
            // 
            // MirrorOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1404, 1112);
            this.Controls.Add(this.listBoxDNS);
            this.Controls.Add(this.listBoxShow);
            this.Controls.Add(this.buttonCan);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonDwn);
            this.Controls.Add(this.buttonUp);
            this.Controls.Add(this.buttonAdd);
            this.Controls.Add(this.buttonDel);
            this.Controls.Add(this.labelDontShow);
            this.Controls.Add(this.labelShow);
            this.Name = "MirrorOptions";
            this.Text = "MirrorOptions";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxDNS;
        private System.Windows.Forms.ListBox listBoxShow;
        private System.Windows.Forms.Button buttonCan;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonDwn;
        private System.Windows.Forms.Button buttonUp;
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.Button buttonDel;
        private System.Windows.Forms.Label labelDontShow;
        private System.Windows.Forms.Label labelShow;
    }
}