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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Rotronic
{
    public partial class ProbeInventoryCalibrationData : Form
    {
        private readonly string _calibrationId;

        /* DataGridView display requirements:
         *      - Take calibrationID and return Step and Sample tables associated with that
         *      calibration ID. Display is complicated by multiple samples per step and 
         *      multiple steps per calibration ID. Display should be easy to read.
         *      
         *          StepId INTEGER PRIMARY KEY AUTOINCREMENT,
    CalibrationId TEXT NOT NULL,
    StepNumber INTEGER NOT NULL,
    StepName TEXT NULL,
    HumiditySetpoint REAL NULL,
    TemperatureSetpointC REAL NULL,
    Accuracy REAL NULL,
    Adjustment INTEGER NULL,
    RampStartUtc TEXT NULL,
    SoakStartUtc TEXT NULL,
    SoakEndUtc TEXT NULL,
    FirstSampleUtc TEXT NULL,
    LastSampleUtc TEXT NULL,
    FOREIGN KEY(CalibrationId) REFERENCES Calibration(CalibrationId) ON DELETE CASCADE
         * 
         *     StepId INTEGER PRIMARY KEY AUTOINCREMENT,
    CalibrationId TEXT NOT NULL,
    StepNumber INTEGER NOT NULL,
    StepName TEXT NULL,
    HumiditySetpoint REAL NULL,
    TemperatureSetpointC REAL NULL,
    Accuracy REAL NULL,
    Adjustment INTEGER NULL,
    RampStartUtc TEXT NULL,
    SoakStartUtc TEXT NULL,
    SoakEndUtc TEXT NULL,
    FirstSampleUtc TEXT NULL,
    LastSampleUtc TEXT NULL,
    FOREIGN KEY(CalibrationId) REFERENCES Calibration(CalibrationId) ON DELETE CASCADE
         * 
         */

        public ProbeInventoryCalibrationData(string calibrationId)
        {
            InitializeComponent();

            _calibrationId = (calibrationId ?? string.Empty).Trim();

            this.Load += ProbeInventoryCalibrationData_Load;
            dataGridViewSteps.CellClick += dataGridViewSteps_CellClick;
        }

        public ProbeInventoryCalibrationData() : this(null)
        {
        }

        private void ProbeInventoryCalibrationData_Load(object sender, EventArgs e)
        {
            try
            {
                Data.InitializeDatabase();
                LoadSteps();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load calibration data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSteps()
        {
            dataGridViewSteps.DataSource = null;
            dataGridViewSamples.DataSource = null;

            if (string.IsNullOrWhiteSpace(_calibrationId))
                return;

            using (var conn = new SQLiteConnection(GetConnectionString()))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"SELECT *
FROM Step
WHERE CalibrationId = @CalibrationId
ORDER BY StepNumber ASC;";
                cmd.Parameters.AddWithValue("@CalibrationId", _calibrationId);

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    NormalizeUtcColumn(dt, "RampStartUtc");
                    NormalizeUtcColumn(dt, "SoakStartUtc");
                    NormalizeUtcColumn(dt, "SoakEndUtc");
                    NormalizeUtcColumn(dt, "FirstSampleUtc");
                    NormalizeUtcColumn(dt, "LastSampleUtc");

                    AddStepDerivedColumns(dt);
                    PopulateStepDerivedColumns(dt);
                    AddStepPassFailColumn(dt);

                    dataGridViewSteps.DataSource = dt;
                }
            }

            TryHideColumn(dataGridViewSteps, "CalibrationId");
            TryHideColumn(dataGridViewSteps, "StepId");

            TryMoveColumn(dataGridViewSteps, "ReferenceHumidity", 3);
            TryMoveColumn(dataGridViewSteps, "ProbeHumidity", 4);
            TryMoveColumn(dataGridViewSteps, "HumidityError", 5);
            TryMoveColumn(dataGridViewSteps, "ReferenceTemperature", 6);
            TryMoveColumn(dataGridViewSteps, "ProbeTemperature", 7);
            TryMoveColumn(dataGridViewSteps, "TemperatureError", 8);
            TryMoveColumn(dataGridViewSteps, "PassFail", dataGridViewSteps.Columns.Count - 1);

            ApplyTwoDecimalFormatting(dataGridViewSteps);

            TryTogglePassFailColumnVisibility();

            try
            {
                dataGridViewSteps.CellFormatting -= dataGridViewSteps_CellFormatting;
                dataGridViewSteps.CellFormatting += dataGridViewSteps_CellFormatting;
            }
            catch { }

            // Auto-load samples for the first step (if any)
            try
            {
                if (dataGridViewSteps.Rows.Count > 0)
                {
                    var first = dataGridViewSteps.Rows[0];
                    if (first != null)
                    {
                        dataGridViewSteps.ClearSelection();
                        first.Selected = true;
                        LoadSamplesForSelectedStep();
                    }
                }
            }
            catch { }
        }

        private void dataGridViewSteps_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            LoadSamplesForSelectedStep();
        }

        private void LoadSamplesForSelectedStep()
        {
            dataGridViewSamples.DataSource = null;

            if (dataGridViewSteps.CurrentRow == null)
                return;

            object rawStepId = null;
            try { rawStepId = dataGridViewSteps.CurrentRow.Cells["StepId"].Value; } catch { }
            if (rawStepId == null || rawStepId == DBNull.Value)
                return;

            var stepId = Convert.ToInt32(rawStepId, CultureInfo.InvariantCulture);

            using (var conn = new SQLiteConnection(GetConnectionString()))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"SELECT *
FROM Sample
WHERE StepId = @StepId
ORDER BY SampleUtc ASC;";
                cmd.Parameters.AddWithValue("@StepId", stepId);

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    NormalizeUtcColumn(dt, "SampleUtc");

                    AddErrorColumns(dt);

                    AddAverageRow(dt);

                    AddSamplePassFailColumn(dt);

                    dataGridViewSamples.DataSource = dt;
                }
            }

            TryHideColumn(dataGridViewSamples, "CalibrationId");
            TryHideColumn(dataGridViewSamples, "StepId");
            TryHideColumn(dataGridViewSamples, "SampleId");

            // Reorder key columns to the front
            TryMoveColumn(dataGridViewSamples, "SampleUtc", 0);
            TryRenameHeader(dataGridViewSamples, "SampleUtc", "SampleTime");

            TryMoveColumn(dataGridViewSamples, "MirrorHumidity", 1);
            TryRenameHeader(dataGridViewSamples, "MirrorHumidity", "Reference Humidity");

            TryMoveColumn(dataGridViewSamples, "ProbeHumidity", 2);

            TryMoveColumn(dataGridViewSamples, "MirrorTemperatureC", 3);
            TryRenameHeader(dataGridViewSamples, "MirrorTemperatureC", "Reference Temperature");

            TryMoveColumn(dataGridViewSamples, "ProbeTemperatureC", 4);

            // Place derived error columns after the primary values.
            TryMoveColumn(dataGridViewSamples, "HumidityError", 3);
            TryMoveColumn(dataGridViewSamples, "TemperatureError", 6);

            TryMoveColumn(dataGridViewSamples, "PassFail", dataGridViewSamples.Columns.Count - 1);

            ApplyTwoDecimalFormatting(dataGridViewSamples);

            TryTogglePassFailColumnVisibility();
        }

        private void TryTogglePassFailColumnVisibility()
        {
            try
            {
                var accuracy = GetCurrentStepAccuracy();
                bool show = accuracy.HasValue && accuracy.Value != 0;

                if (dataGridViewSteps != null && dataGridViewSteps.Columns.Contains("PassFail"))
                    dataGridViewSteps.Columns["PassFail"].Visible = show;
                if (dataGridViewSamples != null && dataGridViewSamples.Columns.Contains("PassFail"))
                    dataGridViewSamples.Columns["PassFail"].Visible = show;
            }
            catch { }
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

        private void AddStepDerivedColumns(DataTable dt)
        {
            if (dt == null)
                return;

            EnsureColumn(dt, "ReferenceHumidity", typeof(double));
            EnsureColumn(dt, "ProbeHumidity", typeof(double));
            EnsureColumn(dt, "HumidityError", typeof(double));
            EnsureColumn(dt, "ReferenceTemperature", typeof(double));
            EnsureColumn(dt, "ProbeTemperature", typeof(double));
            EnsureColumn(dt, "TemperatureError", typeof(double));
        }

        private void AddStepPassFailColumn(DataTable dt)
        {
            if (dt == null)
                return;

            EnsureColumn(dt, "PassFail", typeof(string));

            foreach (DataRow r in dt.Rows)
            {
                if (r == null)
                    continue;

                var stepName = dt.Columns.Contains("StepName") ? (r["StepName"] as string) : null;
                var accuracy = TryGetNullableDouble(dt.Columns.Contains("Accuracy") ? r["Accuracy"] : null);

                var humErr = TryGetNullableDouble(dt.Columns.Contains("HumidityError") ? r["HumidityError"] : null);
                var tempErr = TryGetNullableDouble(dt.Columns.Contains("TemperatureError") ? r["TemperatureError"] : null);

                r["PassFail"] = EvaluatePassFail(stepName, accuracy, humErr, tempErr) ?? (object)DBNull.Value;
            }
        }

        private void AddSamplePassFailColumn(DataTable dt)
        {
            if (dt == null)
                return;

            EnsureColumn(dt, "PassFail", typeof(string));

            var stepName = GetCurrentStepName();
            var accuracy = GetCurrentStepAccuracy();

            foreach (DataRow r in dt.Rows)
            {
                if (r == null)
                    continue;

                // Average row uses a string in SampleUtc and may have computed errors; still evaluate if possible.
                var humErr = TryGetNullableDouble(dt.Columns.Contains("HumidityError") ? r["HumidityError"] : null);
                var tempErr = TryGetNullableDouble(dt.Columns.Contains("TemperatureError") ? r["TemperatureError"] : null);

                r["PassFail"] = EvaluatePassFail(stepName, accuracy, humErr, tempErr) ?? (object)DBNull.Value;
            }
        }

        private string GetCurrentStepName()
        {
            try
            {
                if (dataGridViewSteps.CurrentRow == null)
                    return null;
                if (!dataGridViewSteps.Columns.Contains("StepName"))
                    return null;
                var v = dataGridViewSteps.CurrentRow.Cells["StepName"].Value;
                return v == null || v == DBNull.Value ? null : v.ToString();
            }
            catch { return null; }
        }

        private double? GetCurrentStepAccuracy()
        {
            try
            {
                if (dataGridViewSteps.CurrentRow == null)
                    return null;
                if (!dataGridViewSteps.Columns.Contains("Accuracy"))
                    return null;
                var v = dataGridViewSteps.CurrentRow.Cells["Accuracy"].Value;
                return TryGetNullableDouble(v);
            }
            catch { return null; }
        }

        private static string EvaluatePassFail(string stepName, double? accuracy, double? humidityError, double? temperatureError)
        {
            if (!accuracy.HasValue)
                return null;

            bool isTemp = !string.IsNullOrWhiteSpace(stepName)
                && stepName.IndexOf("TEMP", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isHum = !string.IsNullOrWhiteSpace(stepName)
                && stepName.IndexOf("HUM", StringComparison.OrdinalIgnoreCase) >= 0;

            double? err = null;
            if (isTemp)
                err = temperatureError;
            else if (isHum)
                err = humidityError;
            else
                err = temperatureError ?? humidityError;

            if (!err.HasValue)
                return null;

            return Math.Abs(err.Value) > accuracy.Value ? "Fail" : "Pass";
        }

        private void PopulateStepDerivedColumns(DataTable stepTable)
        {
            if (stepTable == null || stepTable.Rows.Count == 0)
                return;

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();

                foreach (DataRow stepRow in stepTable.Rows)
                {
                    if (stepRow == null)
                        continue;

                    object rawStepId = stepRow.Table.Columns.Contains("StepId") ? stepRow["StepId"] : null;
                    if (rawStepId == null || rawStepId == DBNull.Value)
                        continue;

                    int stepId;
                    try { stepId = Convert.ToInt32(rawStepId, CultureInfo.InvariantCulture); }
                    catch { continue; }

                    var stepName = stepRow.Table.Columns.Contains("StepName") ? (stepRow["StepName"] as string) : null;

                    var summary = GetWorstErrorSampleForStep(conn, stepId, stepName);
                    if (summary == null)
                        continue;

                    stepRow["ReferenceHumidity"] = summary.ReferenceHumidity.HasValue ? (object)summary.ReferenceHumidity.Value : DBNull.Value;
                    stepRow["ProbeHumidity"] = summary.ProbeHumidity.HasValue ? (object)summary.ProbeHumidity.Value : DBNull.Value;
                    stepRow["HumidityError"] = summary.HumidityError.HasValue ? (object)summary.HumidityError.Value : DBNull.Value;
                    stepRow["ReferenceTemperature"] = summary.ReferenceTemperature.HasValue ? (object)summary.ReferenceTemperature.Value : DBNull.Value;
                    stepRow["ProbeTemperature"] = summary.ProbeTemperature.HasValue ? (object)summary.ProbeTemperature.Value : DBNull.Value;
                    stepRow["TemperatureError"] = summary.TemperatureError.HasValue ? (object)summary.TemperatureError.Value : DBNull.Value;
                }
            }
        }

        private sealed class StepSampleSummary
        {
            public double? ReferenceHumidity { get; set; }
            public double? ProbeHumidity { get; set; }
            public double? HumidityError { get; set; }
            public double? ReferenceTemperature { get; set; }
            public double? ProbeTemperature { get; set; }
            public double? TemperatureError { get; set; }
        }

        private static StepSampleSummary GetWorstErrorSampleForStep(SQLiteConnection conn, int stepId, string stepName)
        {
            if (conn == null)
                return null;

            bool isTemp = !string.IsNullOrWhiteSpace(stepName)
                && stepName.IndexOf("TEMP", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isHum = !string.IsNullOrWhiteSpace(stepName)
                && stepName.IndexOf("HUM", StringComparison.OrdinalIgnoreCase) >= 0;

            const string sql = @"SELECT
    ProbeHumidity,
    MirrorHumidity,
    ProbeTemperatureC,
    MirrorTemperatureC
FROM Sample
WHERE StepId = @StepId
ORDER BY SampleUtc ASC;";

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@StepId", stepId);

                using (var r = cmd.ExecuteReader())
                {
                    StepSampleSummary best = null;
                    double bestAbs = double.MinValue;

                    while (r.Read())
                    {
                        double? probeHum = r.IsDBNull(0) ? (double?)null : r.GetDouble(0);
                        double? refHum = r.IsDBNull(1) ? (double?)null : r.GetDouble(1);
                        double? probeTemp = r.IsDBNull(2) ? (double?)null : r.GetDouble(2);
                        double? refTemp = r.IsDBNull(3) ? (double?)null : r.GetDouble(3);

                        double? humErr = (probeHum.HasValue && refHum.HasValue) ? (double?)(probeHum.Value - refHum.Value) : null;
                        double? tempErr = (probeTemp.HasValue && refTemp.HasValue) ? (double?)(probeTemp.Value - refTemp.Value) : null;

                        double abs;
                        if (isTemp)
                        {
                            if (!tempErr.HasValue)
                                continue;
                            abs = Math.Abs(tempErr.Value);
                        }
                        else if (isHum)
                        {
                            if (!humErr.HasValue)
                                continue;
                            abs = Math.Abs(humErr.Value);
                        }
                        else
                        {
                            // Unknown step type: prefer temperature if present, else humidity.
                            if (tempErr.HasValue)
                                abs = Math.Abs(tempErr.Value);
                            else if (humErr.HasValue)
                                abs = Math.Abs(humErr.Value);
                            else
                                continue;
                        }

                        if (best == null || abs > bestAbs)
                        {
                            bestAbs = abs;
                            best = new StepSampleSummary
                            {
                                ReferenceHumidity = refHum,
                                ProbeHumidity = probeHum,
                                HumidityError = humErr,
                                ReferenceTemperature = refTemp,
                                ProbeTemperature = probeTemp,
                                TemperatureError = tempErr
                            };
                        }
                    }

                    return best;
                }
            }
        }

        private static void EnsureColumn(DataTable dt, string columnName, Type type)
        {
            if (dt == null || string.IsNullOrWhiteSpace(columnName) || type == null)
                return;

            if (dt.Columns.Contains(columnName))
                return;

            dt.Columns.Add(new DataColumn(columnName, type));
        }

        private static void AddAverageRow(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
                return;

            var avgRow = dt.NewRow();

            if (dt.Columns.Contains("SampleUtc"))
                avgRow["SampleUtc"] = "AVERAGE";

            foreach (DataColumn col in dt.Columns)
            {
                if (col == null)
                    continue;

                if (string.Equals(col.ColumnName, "SampleUtc", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(col.ColumnName, "SampleId", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (col.DataType != typeof(double) && col.DataType != typeof(float) && col.DataType != typeof(decimal) && col.DataType != typeof(int) && col.DataType != typeof(long))
                    continue;

                double sum = 0;
                int count = 0;

                foreach (DataRow r in dt.Rows)
                {
                    if (r == null)
                        continue;

                    var v = TryGetNullableDouble(r[col]);
                    if (!v.HasValue)
                        continue;

                    sum += v.Value;
                    count++;
                }

                if (count == 0)
                {
                    avgRow[col] = DBNull.Value;
                    continue;
                }

                avgRow[col] = sum / count;
            }

            dt.Rows.Add(avgRow);
        }

        private static void AddErrorColumns(DataTable dt)
        {
            if (dt == null)
                return;

            AddErrorColumn(dt, "HumidityError", "ProbeHumidity", "MirrorHumidity");
            AddErrorColumn(dt, "TemperatureError", "ProbeTemperatureC", "MirrorTemperatureC");
        }

        private static void AddErrorColumn(DataTable dt, string newColumnName, string probeColumnName, string referenceColumnName)
        {
            if (dt.Columns.Contains(newColumnName))
                return;

            if (!dt.Columns.Contains(probeColumnName) || !dt.Columns.Contains(referenceColumnName))
                return;

            var col = new DataColumn(newColumnName, typeof(double));
            dt.Columns.Add(col);

            foreach (DataRow row in dt.Rows)
            {
                var probe = TryGetNullableDouble(row[probeColumnName]);
                var reference = TryGetNullableDouble(row[referenceColumnName]);

                if (!probe.HasValue || !reference.HasValue)
                {
                    row[newColumnName] = DBNull.Value;
                    continue;
                }

                row[newColumnName] = probe.Value - reference.Value;
            }
        }

        private static double? TryGetNullableDouble(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private void dataGridViewSteps_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                var grid = (DataGridView)sender;
                var col = grid.Columns[e.ColumnIndex];
                if (col == null)
                    return;

                if (!string.Equals(col.Name, "Adjustment", StringComparison.OrdinalIgnoreCase))
                    return;

                if (e.Value == null || e.Value == DBNull.Value)
                    return;

                var v = Convert.ToInt32(e.Value, CultureInfo.InvariantCulture);
                e.Value = (v != 0) ? "True" : "False";
                e.FormattingApplied = true;
            }
            catch { }
        }

        private static string GetConnectionString()
        {
            var path = Data.GetDatabasePath();
            return string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", path);
        }

        private static void NormalizeUtcColumn(DataTable dt, string columnName)
        {
            if (dt == null || string.IsNullOrWhiteSpace(columnName) || !dt.Columns.Contains(columnName))
                return;

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
                    row[columnName] = utc.ToString("dd-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToUpperInvariant();
                }
            }
        }

        private static void TryHideColumn(DataGridView grid, string columnName)
        {
            if (grid == null || string.IsNullOrWhiteSpace(columnName))
                return;

            try
            {
                if (grid.Columns.Contains(columnName))
                    grid.Columns[columnName].Visible = false;
            }
            catch { }
        }

        private static void TryMoveColumn(DataGridView grid, string columnName, int displayIndex)
        {
            if (grid == null || string.IsNullOrWhiteSpace(columnName))
                return;

            try
            {
                if (!grid.Columns.Contains(columnName))
                    return;

                var col = grid.Columns[columnName];
                if (col == null)
                    return;

                col.DisplayIndex = Math.Max(0, displayIndex);
            }
            catch { }
        }

        private static void TryRenameHeader(DataGridView grid, string columnName, string headerText)
        {
            if (grid == null || string.IsNullOrWhiteSpace(columnName))
                return;

            try
            {
                if (grid.Columns.Contains(columnName))
                    grid.Columns[columnName].HeaderText = headerText ?? string.Empty;
            }
            catch { }
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "Export Calibration Data";
                    sfd.Filter = "Excel Workbook (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                    sfd.DefaultExt = "xlsx";
                    sfd.FileName = string.IsNullOrWhiteSpace(_calibrationId) ? "CalibrationExport.xlsx" : ("Calibration_" + _calibrationId + ".xlsx");

                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        return;

                    ExportToXlsx(sfd.FileName);
                }

                MessageBox.Show(this, "Exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to export: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToXlsx(string filePath)
        {
            var steps = dataGridViewSteps.DataSource as DataTable;
            if (steps == null)
                throw new InvalidOperationException("Steps data is not available.");

            using (var doc = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                var wbPart = doc.AddWorkbookPart();
                wbPart.Workbook = new Workbook();

                var wsPart = wbPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                wsPart.Worksheet = new Worksheet(sheetData);

                var sheets = wbPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = wbPart.GetIdOfPart(wsPart),
                    SheetId = 1,
                    Name = "Calibration"
                });

                WriteCalibrationLayout(sheetData, steps);

                wbPart.Workbook.Save();
            }
        }

        private void WriteCalibrationLayout(SheetData sheetData, DataTable steps)
        {
            var stepCols = GetVisibleColumnsInDisplayOrder(dataGridViewSteps);
            var sampleCols = GetVisibleColumnsInDisplayOrder(dataGridViewSamples);

            // Samples grid may not be loaded for each step; drive sample columns from data table schema if available.
            if (sampleCols.Count == 0)
            {
                // Fallback to common columns including derived.
                sampleCols = new List<string> { "SampleTime", "Reference Humidity", "ProbeHumidity", "HumidityError", "Reference Temperature", "ProbeTemperatureC", "TemperatureError", "PassFail" };
            }

            // Row 1: title
            sheetData.AppendChild(NewRow(NewTextCell("CalibrationId"), NewTextCell(_calibrationId ?? string.Empty)));
            sheetData.AppendChild(NewRow());

            foreach (DataRow stepRow in steps.Rows)
            {
                // Step header
                var stepHeader = new List<Cell>();
                stepHeader.Add(NewTextCell("Step"));
                if (steps.Columns.Contains("StepNumber"))
                    stepHeader.Add(NewTextCell(Convert.ToString(stepRow["StepNumber"], CultureInfo.InvariantCulture)));
                if (steps.Columns.Contains("StepName"))
                    stepHeader.Add(NewTextCell(Convert.ToString(stepRow["StepName"], CultureInfo.InvariantCulture)));
                sheetData.AppendChild(new Row(stepHeader));

                // Step details row (all visible step columns)
                sheetData.AppendChild(NewHeaderRow(stepCols));
                sheetData.AppendChild(NewDataRow(stepCols, GetStepRowDictionary(stepRow)));

                // Samples for step
                var stepIdObj = steps.Columns.Contains("StepId") ? stepRow["StepId"] : null;
                int stepId;
                if (stepIdObj == null || stepIdObj == DBNull.Value || !int.TryParse(Convert.ToString(stepIdObj, CultureInfo.InvariantCulture), out stepId))
                {
                    sheetData.AppendChild(NewRow());
                    continue;
                }

                var samples = LoadSamplesForStep(stepId);
                sheetData.AppendChild(NewHeaderRow(sampleCols));
                foreach (DataRow s in samples.Rows)
                    sheetData.AppendChild(NewDataRow(sampleCols, GetSampleRowDictionary(s)));

                sheetData.AppendChild(NewRow());
            }
        }

        private DataTable LoadSamplesForStep(int stepId)
        {
            using (var conn = new SQLiteConnection(GetConnectionString()))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"SELECT * FROM Sample WHERE StepId = @StepId ORDER BY SampleUtc ASC;";
                cmd.Parameters.AddWithValue("@StepId", stepId);
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    NormalizeUtcColumn(dt, "SampleUtc");
                    AddErrorColumns(dt);
                    AddAverageRow(dt);
                    AddSamplePassFailColumn(dt);

                    // Harmonize names with UI-renamed ones
                    if (dt.Columns.Contains("SampleUtc") && !dt.Columns.Contains("SampleTime"))
                    {
                        dt.Columns["SampleUtc"].ColumnName = "SampleTime";
                    }
                    if (dt.Columns.Contains("MirrorHumidity") && !dt.Columns.Contains("Reference Humidity"))
                        dt.Columns["MirrorHumidity"].ColumnName = "Reference Humidity";
                    if (dt.Columns.Contains("MirrorTemperatureC") && !dt.Columns.Contains("Reference Temperature"))
                        dt.Columns["MirrorTemperatureC"].ColumnName = "Reference Temperature";

                    return dt;
                }
            }
        }

        private static List<string> GetVisibleColumnsInDisplayOrder(DataGridView grid)
        {
            var cols = new List<DataGridViewColumn>();
            if (grid != null)
            {
                foreach (DataGridViewColumn c in grid.Columns)
                {
                    if (c != null && c.Visible)
                        cols.Add(c);
                }
            }
            cols.Sort((a, b) => a.DisplayIndex.CompareTo(b.DisplayIndex));
            return cols.Select(c => c.HeaderText ?? c.Name).ToList();
        }

        private static Dictionary<string, object> GetStepRowDictionary(DataRow r)
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn c in r.Table.Columns)
                d[c.ColumnName] = r[c];
            return d;
        }

        private static Dictionary<string, object> GetSampleRowDictionary(DataRow r)
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn c in r.Table.Columns)
                d[c.ColumnName] = r[c];
            return d;
        }

        private static Row NewHeaderRow(IEnumerable<string> headers)
        {
            var cells = new List<Cell>();
            foreach (var h in headers)
                cells.Add(NewTextCell(h));
            return new Row(cells);
        }

        private static Row NewDataRow(IList<string> headers, IDictionary<string, object> row)
        {
            var cells = new List<Cell>();
            foreach (var h in headers)
            {
                object v;
                row.TryGetValue(h, out v);
                if (v == null || v == DBNull.Value)
                    cells.Add(NewTextCell(string.Empty));
                else
                    cells.Add(NewTextCell(FormatExportValue(h, v)));
            }
            return new Row(cells);
        }

        private static string FormatExportValue(string header, object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            var t = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(t))
                return string.Empty;

            // Force 2 decimals for any temperature/humidity related fields.
            // This is display formatting (scientific reporting); underlying DB values remain full precision.
            if (IsTempOrHumidityHeader(header))
            {
                double d;
                if (double.TryParse(t, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out d))
                    return d.ToString("0.00", CultureInfo.InvariantCulture);
            }

            return t;
        }

        private static bool IsTempOrHumidityHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return false;

            var h = header.Trim();
            return h.IndexOf("Temp", StringComparison.OrdinalIgnoreCase) >= 0
                || h.IndexOf("Temperature", StringComparison.OrdinalIgnoreCase) >= 0
                || h.IndexOf("Hum", StringComparison.OrdinalIgnoreCase) >= 0
                || h.IndexOf("Humidity", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Row NewRow(params Cell[] cells)
        {
            var r = new Row();
            if (cells != null)
            {
                foreach (var c in cells)
                    if (c != null) r.Append(c);
            }
            return r;
        }

        private static Cell NewTextCell(string text)
        {
            return new Cell(new CellValue(text ?? string.Empty)) { DataType = CellValues.String };
        }

        private void buttonCalCert_Click(object sender, EventArgs e)
        {
            
        }
    }
}
