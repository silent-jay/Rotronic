using System;
using System.Drawing;
using System.Windows.Forms;

namespace Rotronic
{
    internal sealed class SafeShutdownDialog : Form
    {
        private readonly Button btnCloseNow;
        private readonly TextBox txtStatus;

        public event EventHandler CloseNowRequested;

        public SafeShutdownDialog()
        {
            Text = "Safe Shutdown";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Width = 560;
            Height = 320;

            var lbl = new Label
            {
                AutoSize = false,
                Text = "Performing safe shutdown. Waiting for all chambers to reach safe conditions.",
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(10, 10, 10, 0)
            };

            txtStatus = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font(FontFamily.GenericMonospace, 9f),
            };

            btnCloseNow = new Button
            {
                Text = "Close Now",
                Dock = DockStyle.Bottom,
                Height = 40
            };

            btnCloseNow.Click += (s, e) =>
            {
                try { CloseNowRequested?.Invoke(this, EventArgs.Empty); } catch { }
                btnCloseNow.Enabled = false;
                btnCloseNow.Text = "Closing...";
            };

            Controls.Add(txtStatus);
            Controls.Add(btnCloseNow);
            Controls.Add(lbl);
        }

        public void SetStatus(string message)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                try { BeginInvoke((Action)(() => SetStatus(message))); } catch { }
                return;
            }

            txtStatus.Text = message ?? string.Empty;
        }

        public void SafeClose()
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                try { BeginInvoke((Action)(SafeClose)); } catch { }
                return;
            }

            try { Close(); } catch { }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SafeShutdownDialog
            // 
            this.ClientSize = new System.Drawing.Size(330, 257);
            this.Name = "SafeShutdownDialog";
            this.ResumeLayout(false);

        }
    }
}
