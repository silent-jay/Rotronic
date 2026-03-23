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
using System.IO;

namespace Rotronic
{
    public partial class ProbeInventoryCalHistoryFrm : Form
    {
        private readonly string _serialNumber;
        private readonly string _serialColumnName;

        private static string NormalizeSerialColumnName(string serialColumnName)
        {
            if (string.IsNullOrWhiteSpace(serialColumnName))
                return "ProbeSerialNumber";

            var t = serialColumnName.Trim();
            if (string.Equals(t, "ProbeSerialNumber", StringComparison.OrdinalIgnoreCase))
                return "ProbeSerialNumber";
            if (string.Equals(t, "MirrorSerialNumber", StringComparison.OrdinalIgnoreCase))
                return "MirrorSerialNumber";
            if (string.Equals(t, "ChamberControllerSerialNumber", StringComparison.OrdinalIgnoreCase))
                return "ChamberControllerSerialNumber";

            return "ProbeSerialNumber";
        }

        public ProbeInventoryCalHistoryFrm(string probeSerialNumber)
        {
            InitializeComponent();

            _serialNumber = (probeSerialNumber ?? string.Empty).Trim();
            _serialColumnName = "ProbeSerialNumber";
            this.Load += ProbeInventoryCalHistoryFrm_Load;

            dataGridViewCalibrationHistory.ReadOnly = true;
            dataGridViewCalibrationHistory.AllowUserToAddRows = false;
            dataGridViewCalibrationHistory.AllowUserToDeleteRows = false;

            dataGridViewCalibrationHistory.CellClick += dataGridViewCalibrationHistory_CellClick;
            dataGridViewCalibrationHistory.CellDoubleClick += dataGridViewCalibrationHistory_CellDoubleClick;
            dataGridViewCalibrationHistory.KeyDown += dataGridViewCalibrationHistory_KeyDown;
        }

        public ProbeInventoryCalHistoryFrm(string serialNumber, string serialColumnName)
        {
            InitializeComponent();

            _serialNumber = (serialNumber ?? string.Empty).Trim();
            _serialColumnName = NormalizeSerialColumnName(serialColumnName);
            this.Load += ProbeInventoryCalHistoryFrm_Load;

            dataGridViewCalibrationHistory.ReadOnly = true;
            dataGridViewCalibrationHistory.AllowUserToAddRows = false;
            dataGridViewCalibrationHistory.AllowUserToDeleteRows = false;

            dataGridViewCalibrationHistory.CellClick += dataGridViewCalibrationHistory_CellClick;
            dataGridViewCalibrationHistory.CellDoubleClick += dataGridViewCalibrationHistory_CellDoubleClick;
            dataGridViewCalibrationHistory.KeyDown += dataGridViewCalibrationHistory_KeyDown;
        }

        private void dataGridViewCalibrationHistory_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            buttonCalEvent_Click(buttonCalEvent, EventArgs.Empty);
        }

        private void dataGridViewCalibrationHistory_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            if (dataGridViewCalibrationHistory.CurrentRow == null)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            buttonCalEvent_Click(buttonCalEvent, EventArgs.Empty);
        }

        private void dataGridViewCalibrationHistory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var cell = dataGridViewCalibrationHistory.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var raw = cell?.Value == null || cell.Value == DBNull.Value ? string.Empty : cell.Value.ToString();
            var columnName = dataGridViewCalibrationHistory.Columns[e.ColumnIndex]?.Name ?? string.Empty;

            richTextBoxData.Text = FormatForDisplay(columnName, raw);
        }

        private void ProbeInventoryCalHistoryFrm_Load(object sender, EventArgs e)
        {
            try
            {
                Data.InitializeDatabase();
                using (var conn = new SQLiteConnection(GetConnectionString()))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"SELECT *
FROM Calibration
 WHERE " + _serialColumnName + @" = @SerialNumber
ORDER BY StartedUtc DESC;";
                    cmd.Parameters.AddWithValue("@SerialNumber", _serialNumber);

                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);

                        if (dt.Columns.Contains("StartedUtc"))
                            NormalizeUtcColumn(dt, "StartedUtc");
                        if (dt.Columns.Contains("EndedUtc"))
                            NormalizeUtcColumn(dt, "EndedUtc");

                        dataGridViewCalibrationHistory.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load calibration history: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void NormalizeUtcColumn(DataTable dt, string columnName)
        {
            foreach (DataRow row in dt.Rows)
            {
                if (row.IsNull(columnName))
                    continue;

                var raw = row[columnName] as string;
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var t = raw.Trim();
                if (DateTime.TryParseExact(t, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    var utc = parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
                    row[columnName] = utc.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture).ToUpperInvariant();
                }
            }
        }

        private static string GetConnectionString()
        {
            var path = Data.GetDatabasePath();
            return string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", path);
        }

        private static string FormatForDisplay(string columnName, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            // Pretty-print common "snapshot" JSON fields.
            if (columnName.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var trimmed = raw.Trim();
                if (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
                {
                    try
                    {
                        return PrettyPrintJsonText(trimmed);
                    }
                    catch
                    {
                        // Fall back to raw
                    }
                }
            }

            return raw;
        }

        private static string PrettyPrintJsonText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            var sb = new StringBuilder(json.Length * 2);
            int indent = 0;
            bool inString = false;
            bool escape = false;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];

                if (inString)
                {
                    sb.Append(ch);

                    if (escape)
                    {
                        escape = false;
                    }
                    else
                    {
                        if (ch == '\\')
                            escape = true;
                        else if (ch == '"')
                            inString = false;
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inString = true;
                        sb.Append(ch);
                        break;
                    case '{':
                    case '[':
                        sb.Append(ch);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case '}':
                    case ']':
                        sb.AppendLine();
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                        break;
                    case ',':
                        sb.Append(ch);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        if (!char.IsWhiteSpace(ch))
                            sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        private void buttonCalEvent_Click(object sender, EventArgs e)
        {
            var row = dataGridViewCalibrationHistory.CurrentRow;
            if (row == null)
                return;

            object raw = null;
            try { raw = row.Cells["CalibrationId"].Value; } catch { }
            var calibrationId = raw == null || raw == DBNull.Value ? string.Empty : raw.ToString().Trim();
            if (string.IsNullOrWhiteSpace(calibrationId))
                return;

            using (var calEventFrm = new ProbeInventoryCalibrationData(calibrationId))
            {
                calEventFrm.ShowDialog(this);
            }
        }
    }
}
