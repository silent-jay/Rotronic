using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Data.SQLite;

namespace Rotronic
{
    public partial class ProbeInventoryFrm : Form
    {
        public ProbeInventoryFrm()
        {
            InitializeComponent();

            this.Load += ProbeInventoryFrm_Load;

            dataGridViewProbe.CellDoubleClick += dataGridViewProbe_CellDoubleClick;
            dataGridViewProbe.KeyDown += dataGridViewProbe_KeyDown;
        }

        private void dataGridViewProbe_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            buttonCal_Click(buttonCal, EventArgs.Empty);
        }

        private void dataGridViewProbe_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            if (dataGridViewProbe.CurrentRow == null)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            buttonCal_Click(buttonCal, EventArgs.Empty);
        }

        private void ProbeInventoryFrm_Load(object sender, EventArgs e)
        {
            try
            {
                Data.InitializeDatabase();
                dataGridViewProbe.Rows.Clear();

                ApplyTwoDecimalFormatting(dataGridViewProbe);

                using (var conn = new SQLiteConnection(GetConnectionStringForProbeInventory()))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT SerialNumber, DeviceName, DeviceModel, FirmwareVersion, DeviceType, HumidityFactoryCorrection, HumidityUserCorrection, HumidityDriftCorrection, PT100CoeffA, PT100CoeffB, PT100CoeffC, TempOffset, TempConversion, LastCalibrationUtc, NextDueUtc FROM Probe ORDER BY COALESCE(DeviceName, ''), SerialNumber;";
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var idx = dataGridViewProbe.Rows.Add();
                                var row = dataGridViewProbe.Rows[idx];

                                row.Cells["DeviceName"].Value = r.IsDBNull(1) ? null : r.GetString(1);
                                row.Cells["SerialNumber"].Value = r.IsDBNull(0) ? null : r.GetString(0);
                                row.Cells["DeviceModel"].Value = r.IsDBNull(2) ? null : r.GetString(2);
                                row.Cells["FirmwareVersion"].Value = r.IsDBNull(3) ? null : r.GetString(3);
                                row.Cells["DeviceType"].Value = r.IsDBNull(4) ? null : r.GetString(4);
                                row.Cells["HumidityFactoryCorrection"].Value = r.IsDBNull(5) ? (object)null : r.GetDouble(5);
                                row.Cells["HumidityUserCorrection"].Value = r.IsDBNull(6) ? (object)null : r.GetDouble(6);
                                row.Cells["HumidityDriftCorrection"].Value = r.IsDBNull(7) ? (object)null : r.GetDouble(7);
                                row.Cells["PT100CoeffA"].Value = r.IsDBNull(8) ? (object)null : r.GetDouble(8);
                                row.Cells["PT100CoeffB"].Value = r.IsDBNull(9) ? (object)null : r.GetDouble(9);
                                row.Cells["PT100CoeffC"].Value = r.IsDBNull(10) ? (object)null : r.GetDouble(10);
                                row.Cells["TempOffset"].Value = r.IsDBNull(11) ? (object)null : r.GetDouble(11);
                                row.Cells["TempConversion"].Value = r.IsDBNull(12) ? (object)null : r.GetDouble(12);
                                row.Cells["LastCalibrationUtc"].Value = FormatDbUtcToDdMmmYyyyHmOrEmpty(r.IsDBNull(13) ? null : r.GetString(13));
                                row.Cells["NextDueUtc"].Value = FormatDbUtcToDdMmmYyyyHmOrEmpty(r.IsDBNull(14) ? null : r.GetString(14));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load probes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ApplyTwoDecimalFormatting(DataGridView grid)
        {
            if (grid == null)
                return;

            try
            {
                foreach (DataGridViewColumn c in grid.Columns)
                {
                    if (c == null)
                        continue;

                    var name = c.Name ?? string.Empty;

                    if (name.IndexOf("Accuracy", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        c.DefaultCellStyle.Format = "0.00";
                        continue;
                    }

                    if (name.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    if (name.IndexOf("Temp", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Temperature", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Hum", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Humidity", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        c.DefaultCellStyle.Format = "0.00";
                    }
                }
            }
            catch { }
        }

        private static string FormatDbUtcToDdMmmYyyyHmOrEmpty(string dbUtc)
        {
            if (string.IsNullOrWhiteSpace(dbUtc))
                return string.Empty;

            var t = dbUtc.Trim();
            if (DateTime.TryParseExact(t, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            {
                var utc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
                return utc.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture).ToUpperInvariant();
            }

            // Fallback: try common saved display format(s)
            if (DateTime.TryParseExact(t, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dd))
                return dd.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture).ToUpperInvariant();

            return t;
        }

        private static string GetConnectionStringForProbeInventory()
        {
            var path = Data.GetDatabasePath();
            return string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", path);
        }

        private void buttonCal_Click(object sender, EventArgs e)
        {
            var row = dataGridViewProbe.CurrentRow;
            if (row == null)
                return;

            var serial = (row.Cells["SerialNumber"].Value ?? string.Empty).ToString().Trim();
            if (string.IsNullOrWhiteSpace(serial))
                return;

            using (var frm = new ProbeInventoryCalHistoryFrm(serial))
            {
                frm.ShowDialog(this);
            }
        }
    }
}
