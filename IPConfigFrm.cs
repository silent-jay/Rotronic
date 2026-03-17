using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Rotronic
{
    public partial class IPConfigFrm : Form
    {
        public IPConfigFrm()
        {
            InitializeComponent();
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            var ip = textBoxIP.Text;

            if (ValidateIP(ip))
            {
                SaveIpAddress(ip);
                textBoxIP.Clear();
            }
        }

        internal static string GetIpStorePath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Rotronic");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ips.txt");
        }

        public static void RemoveSavedIpAddress(string ip)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ip))
                    return;

                ip = ip.Trim();
                var path = GetIpStorePath();
                if (!File.Exists(path))
                    return;

                var remaining = File.ReadAllLines(path)
                    .Select(l => (l ?? string.Empty).Trim())
                    .Where(l => l.Length > 0)
                    .Where(l => !string.Equals(l, ip, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                File.WriteAllLines(path, remaining);
            }
            catch
            {
                // ignore file IO errors
            }
        }

        private void SaveIpAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;

            ip = ip.Trim();
            string path = GetIpStorePath();

            // De-dupe (case-insensitive) and append
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    var s = (line ?? string.Empty).Trim();
                    if (s.Length > 0) existing.Add(s);
                }
            }

            if (existing.Add(ip))
            {
                File.AppendAllText(path, ip + Environment.NewLine);
            }
        }

        public static List<string> LoadSavedIpAddresses()
        {
            string path = GetIpStorePath();
            var result = new List<string>();

            if (!File.Exists(path))
                return result;

            foreach (var line in File.ReadAllLines(path))
            {
                var s = (line ?? string.Empty).Trim();
                if (s.Length == 0) continue;
                if (!result.Contains(s, StringComparer.OrdinalIgnoreCase))
                    result.Add(s);
            }

            return result;
        }

        /* 
         PSEUDOCODE / PLAN (detailed)
         - Validate basic IP string (null/whitespace)
         - Trim input
         - Parse to IPAddress using IPAddress.TryParse
         - Ensure AddressFamily is InterNetwork (IPv4)
         - Extract octets and apply the same disallowed checks:
             - 0.0.0.0, 255.255.255.255
             - first octet 0, 127
             - multicast 224-239
             - reserved/experimental 240-254
         - If all the above checks pass, attempt a RAW TCP connection to the device:
             - Use port 6341 (default)
             - Use TcpClient.ConnectAsync and wait with a small timeout (e.g., 1500 ms)
             - If the connection fails or times out, consider it no response -> return false
             - If connected:
                 - Get NetworkStream, set ReadTimeout (e.g., 2000 ms)
                 - Send ASCII bytes for the exact command "Name?" (no additional CR/LF)
                 - Try to read from the stream (single blocking read up to a buffer size)
                 - If read returns > 0 bytes, we treat it as a response -> return true
                 - Any IOException, timeout, no bytes, or other failure -> return false
             - Ensure stream and client are properly closed/disposed in finally
         - Show MessageBox for failures where appropriate and return boolean
        */

        private bool ValidateIP(string ip)
        {
            if (ip == "fake")
            {
                Program.AddFakeChamber();
                return true;
            }
            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("IP address cannot be empty.");
                return false;
            }

            ip = ip.Trim();

            if (!IPAddress.TryParse(ip, out IPAddress address))
            {
                MessageBox.Show("Incorrect IP address format.");
                return false;
            }

            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                MessageBox.Show("Only IPv4 addresses are supported.");
                return false;
            }

            byte[] octets = address.GetAddressBytes(); // IPv4: 4 bytes, network order

            // Specific invalid addresses / ranges
            // 0.0.0.0 (unspecified)
            if (octets[0] == 0 && octets[1] == 0 && octets[2] == 0 && octets[3] == 0)
            {
                MessageBox.Show("The unspecified address (0.0.0.0) is not allowed.");
                return false;
            }

            // 255.255.255.255 (limited broadcast)
            if (octets[0] == 255 && octets[1] == 255 && octets[2] == 255 && octets[3] == 255)
            {
                MessageBox.Show("The broadcast address (255.255.255.255) is not allowed.");
                return false;
            }

            // First octet rules
            if (octets[0] == 0)
            {
                MessageBox.Show("IP address cannot start with 0.");
                return false;
            }

            if (octets[0] == 127)
            {
                MessageBox.Show("Loopback addresses (127.x.x.x) are not allowed.");
                return false;
            }

            // Multicast: 224.0.0.0 - 239.255.255.255
            if (octets[0] >= 224 && octets[0] <= 239)
            {
                MessageBox.Show("Multicast addresses (224.0.0.0/4) are not allowed.");
                return false;
            }

            // Reserved / Experimental: 240.0.0.0 - 255.255.255.254 (except handled broadcast)
            if (octets[0] >= 240 && octets[0] <= 254)
            {
                MessageBox.Show("Reserved or experimental address ranges are not allowed.");
                return false;
            }

            // Attempt raw TCP connection to the device on port 6341 and send "Name?"
            const int port = 6341;
            bool reachable = HG2(address, port);
            if (!reachable)
            {
                MessageBox.Show("No response from device at " + ip + ":" + port);
                return false;
            }

            // Passed all checks and device responded
            return true;
        }

        private bool HG2(IPAddress address, int port, int connectTimeoutMs = 1500, int readTimeoutMs = 2000)
        {
            /* PSEUDOCODE / PLAN (detailed)
             - Create TcpClient and start ConnectAsync to the given address and port.
             - Wait up to connectTimeoutMs for the connection to succeed.
             - If not connected, return false.
             - Get NetworkStream and set ReadTimeout/WriteTimeout.
             - Send ASCII bytes for "Name?".
             - Perform a single blocking read into a buffer (catch IOException for timeout).
             - If bytesRead <= 0, treat as no response and return false.
             - Convert the received bytes to an ASCII string (trim CR/LF).
             - Show a MessageBox with the device response (include address:port for context).
             - Return true on success.
             - Ensure stream and client are closed/disposed in finally.
            */

            try
            {
                var chamber = new Chamber { IPAddress = address.ToString() };
                var response = ChamberCommands.SendResponse(chamber, "Name?", port, connectTimeoutMs, readTimeoutMs);
                if (string.IsNullOrWhiteSpace(response))
                    return false;

                try
                {
                    MessageBox.Show(response, $"Response from {address}:{port}", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
