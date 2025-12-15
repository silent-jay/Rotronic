using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{
    public partial class CalibrationStepFrm : Form
    {
        public CalibrationStepFrm()
        {
            InitializeComponent();
        }

        private void loadListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Determine application exe folder and ensure "Step Files" subfolder exists
            string exeFolder = Application.StartupPath;
            string stepFilesFolder = Path.Combine(exeFolder, "Step Files");
            try
            {
                if (!Directory.Exists(stepFilesFolder))
                    Directory.CreateDirectory(stepFilesFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to access folder for loading files: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dlg = new OpenFileDialog())
            {
                dlg.InitialDirectory = stepFilesFolder;
                dlg.Filter = "Excel Workbook|*.xlsx";
                dlg.DefaultExt = "xlsx";
                dlg.Title = "Load Steps from Excel Workbook";
                dlg.Multiselect = false;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                string filePath = dlg.FileName;
                try
                {
                    using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false))
                    {
                        WorkbookPart workbookPart = document.WorkbookPart;
                        if (workbookPart == null)
                        {
                            MessageBox.Show(this, "The selected file is not a valid Excel workbook.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        Sheet firstSheet = workbookPart.Workbook.Sheets.Elements<Sheet>().FirstOrDefault();
                        if (firstSheet == null)
                        {
                            MessageBox.Show(this, "The workbook contains no sheets.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheet.Id);
                        SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                        var rows = sheetData?.Elements<Row>().ToList() ?? new List<Row>();

                        if (rows.Count == 0)
                        {
                            MessageBox.Show(this, "The workbook contains no data to load.", "Empty File", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        // Local helper to read cell text, handling shared strings
                        string ReadCell(Cell cell)
                        {
                            if (cell == null)
                                return string.Empty;

                            string value = cell.InnerText;
                            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                            {
                                if (int.TryParse(value, out int ssIndex))
                                {
                                    var sst = workbookPart.SharedStringTablePart?.SharedStringTable;
                                    if (sst != null)
                                    {
                                        var ssi = sst.Elements<SharedStringItem>().ElementAtOrDefault(ssIndex);
                                        if (ssi != null)
                                            return ssi.InnerText ?? string.Empty;
                                    }
                                }
                                return string.Empty;
                            }

                            // For booleans, numbers or inline strings just return the inner text
                            return value ?? string.Empty;
                        }

                        // Build list of visible column indexes and expected headers (order matters)
                        List<int> visibleColumnIndexes = new List<int>();
                        List<string> expectedHeaders = new List<string>();
                        foreach (DataGridViewColumn col in dataGridViewStep.Columns)
                        {
                            if (col.Visible)
                            {
                                visibleColumnIndexes.Add(col.Index);
                                string headerText = col.HeaderText ?? col.Name ?? string.Empty;
                                expectedHeaders.Add(headerText.Trim());
                            }
                        }

                        // Read header row (first row)
                        Row headerRow = rows[0];
                        var headerCells = headerRow.Elements<Cell>().ToList();
                        List<string> fileHeaders = headerCells.Select(c => ReadCell(c).Trim()).ToList();

                        // Validate header count matches visible columns
                        if (fileHeaders.Count != expectedHeaders.Count)
                        {
                            MessageBox.Show(this, "The Excel file format does not match the expected column layout (column count mismatch).", "Format Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Validate header texts (case-insensitive, trimmed)
                        for (int i = 0; i < expectedHeaders.Count; i++)
                        {
                            string a = expectedHeaders[i] ?? string.Empty;
                            string b = fileHeaders[i] ?? string.Empty;
                            if (!string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                MessageBox.Show(this, $"The Excel header \"{b}\" does not match the expected column \"{a}\".\nLoad aborted.", "Format Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        // Validation passed - prepare to load rows
                        // If bound to a DataTable, get reference
                        DataTable targetTable = null;
                        if (dataGridViewStep.DataSource is DataTable dt)
                        {
                            targetTable = dt;
                        }
                        else if (dataGridViewStep.DataSource is BindingSource bs && bs.DataSource is DataTable bsTable)
                        {
                            targetTable = bsTable;
                        }

                        // If bound to DataTable, verify mapping from visible DataGridView columns to DataColumn exists
                        Dictionary<int, string> columnIndexToDataColumnName = new Dictionary<int, string>();
                        if (targetTable != null)
                        {
                            foreach (int colIndex in visibleColumnIndexes)
                            {
                                var dgvCol = dataGridViewStep.Columns[colIndex];
                                string tryName = dgvCol.Name ?? dgvCol.HeaderText ?? string.Empty;
                                if (targetTable.Columns.Contains(tryName))
                                {
                                    columnIndexToDataColumnName[colIndex] = tryName;
                                    continue;
                                }

                                // Try header text as column name
                                string header = dgvCol.HeaderText ?? string.Empty;
                                if (targetTable.Columns.Contains(header))
                                {
                                    columnIndexToDataColumnName[colIndex] = header;
                                    continue;
                                }

                                // No match found -> incompatible
                                MessageBox.Show(this, $"The DataTable bound to the grid does not contain a matching column for \"{dgvCol.HeaderText}\".\nLoad aborted.", "Format Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        // Helper to parse bool-like strings
                        Func<string, bool> parseBool = s =>
                        {
                            if (string.IsNullOrWhiteSpace(s))
                                return false;
                            var t = s.Trim().ToLowerInvariant();
                            return t == "true" || t == "1" || t == "yes" || t == "y" || t == "x" || t == "checked";
                        };

                        // Load rows from Excel into the grid or DataTable
                        foreach (var dataRow in rows.Skip(1))
                        {
                            var cells = dataRow.Elements<Cell>().ToList();
                            // Build array of cell text values corresponding to expectedHeaders order
                            string[] values = new string[expectedHeaders.Count];
                            for (int i = 0; i < expectedHeaders.Count; i++)
                            {
                                // If the cell at position i exists, read it, otherwise empty string
                                if (i < cells.Count)
                                    values[i] = ReadCell(cells[i]);
                                else
                                    values[i] = string.Empty;
                            }

                            if (targetTable == null)
                            {
                                // Unbound grid: create an object[] matching visible columns and convert checkbox columns to bool
                                object[] rowValues = new object[visibleColumnIndexes.Count];
                                for (int idx = 0; idx < visibleColumnIndexes.Count; idx++)
                                {
                                    int dgvColIndex = visibleColumnIndexes[idx];
                                    var dgvCol = dataGridViewStep.Columns[dgvColIndex];

                                    string raw = values[idx] ?? string.Empty;

                                    if (dgvCol is DataGridViewCheckBoxColumn)
                                    {
                                        rowValues[idx] = parseBool(raw);
                                    }
                                    else
                                    {
                                        rowValues[idx] = raw;
                                    }
                                }

                                dataGridViewStep.Rows.Add(rowValues);
                            }
                            else
                            {
                                // Bound to DataTable: create DataRow and set mapped columns, respecting boolean DataColumns
                                DataRow newRow = targetTable.NewRow();
                                for (int idx = 0; idx < visibleColumnIndexes.Count; idx++)
                                {
                                    int dgvColIndex = visibleColumnIndexes[idx];
                                    string dataColName = columnIndexToDataColumnName[dgvColIndex];
                                    var dgvCol = dataGridViewStep.Columns[dgvColIndex];

                                    string raw = values[idx] ?? string.Empty;

                                    var targetCol = targetTable.Columns[dataColName];
                                    if (targetCol != null && targetCol.DataType == typeof(bool))
                                    {
                                        // assign parsed boolean (empty => false)
                                        newRow[dataColName] = parseBool(raw);
                                    }
                                    else
                                    {
                                        if (string.IsNullOrEmpty(raw))
                                            newRow[dataColName] = DBNull.Value;
                                        else
                                            newRow[dataColName] = raw;
                                    }
                                }
                                targetTable.Rows.Add(newRow);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed to load file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void buttonSetup_Click(object sender, EventArgs e)
        {
            bool isValid = DataValidation();
            if (!isValid)
            {
                // Validation failed; do not proceed with calibration setup
                return;
            }
            if (!GridHasData())
            {
                MessageBox.Show(this, "No step file is loaded", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build list of StepClass objects from the grid and pass to CalibrationSetupFrm
            var steps = BuildStepListFromGrid();
            var calForm = new CalibrationSetupFrm(steps);
            calForm.Show(this);
        }

        private bool DataValidation()
        {

            /* Plan (detailed pseudocode):
             1. Change signature to return bool: true when validation passes (no errors), false when errors found.
             2. Keep existing validation logic intact.
             3. When errors are found:
                - Focus the first invalid cell if possible.
                - Show combined message box.
                - Return false.
             4. When no errors:
                - Clear lingering ErrorText values.
                - Return true.
             5. Ensure no other behavioral changes.
            */

            // Local helpers
            int FindColumnIndexContaining(params string[] tokens)
            {
                if (tokens == null || tokens.Length == 0)
                    return -1;

                for (int i = 0; i < dataGridViewStep.Columns.Count; i++)
                {
                    var c = dataGridViewStep.Columns[i];
                    string combined = ((c?.Name ?? "") + "|" + (c?.HeaderText ?? "")).ToLowerInvariant();
                    bool all = true;
                    foreach (var t in tokens)
                    {
                        if (string.IsNullOrWhiteSpace(t))
                            continue;
                        if (!combined.Contains(t.ToLowerInvariant().Trim()))
                        {
                            all = false;
                            break;
                        }
                    }
                    if (all)
                        return i;
                }
                return -1;
            }

            bool TryParseTwoDecimal(string raw, out double result)
            {
                result = 0;
                if (string.IsNullOrWhiteSpace(raw))
                    return false;

                string s = raw.Trim();
                // Allow comma as decimal separator by normalizing to dot
                s = s.Replace(',', '.');

                // Reject if there are multiple dots
                int dotCount = s.Count(ch => ch == '.');
                if (dotCount > 1)
                    return false;

                // Remove leading + sign
                if (s.StartsWith("+"))
                    s = s.Substring(1);

                // Use invariant culture for parsing after normalization
                if (!double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result))
                    return false;

                // Check decimal places by inspecting the normalized string (avoid floating rounding issues)
                int idx = s.IndexOf('.');
                if (idx < 0)
                {
                    // integer -> 0 decimals -> allowed
                    return true;
                }

                string frac = s.Substring(idx + 1);
                // If frac contains exponent part (e.g. "1.23e3"), split at 'e' or 'E'
                int eidx = frac.IndexOfAny(new char[] { 'e', 'E' });
                if (eidx >= 0)
                    frac = frac.Substring(0, eidx);

                // Disallow more than 2 decimal digits
                if (frac.Length > 2)
                    return false;

                return true;
            }

            string FormatTwoDecimal(double value)
            {
                // Use current culture formatting to present to the user, but ensure two decimals
                return value.ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
            }

            // Identify columns (best-effort)
            int colStep = FindColumnIndexContaining("step");
            int colTempSet = FindColumnIndexContaining("temp", "set");
            int colHumSet = FindColumnIndexContaining("hum", "set");
            int colEvalTemp = FindColumnIndexContaining("eval", "temp");
            if (colEvalTemp < 0) colEvalTemp = FindColumnIndexContaining("evaluate", "temp");
            int colEvalHum = FindColumnIndexContaining("eval", "hum");
            if (colEvalHum < 0) colEvalHum = FindColumnIndexContaining("evaluate", "hum");

            // Accuracy columns (new behaviour)
            int colTempAccuracy = -1;
            int colHumAccuracy = -1;
            for (int i = 0; i < dataGridViewStep.Columns.Count; i++)
            {
                var c = dataGridViewStep.Columns[i];
                string combined = ((c?.Name ?? "") + "|" + (c?.HeaderText ?? "")).ToLowerInvariant();
                if (combined.Contains("accuracy") && (combined.Contains("temp") || combined.Contains("temperature")))
                    colTempAccuracy = i;
                if (combined.Contains("accuracy") && (combined.Contains("hum") || combined.Contains("humidity")))
                    colHumAccuracy = i;
            }

            var errors = new List<string>();
            DataGridViewCell firstInvalidCell = null;

            for (int rowIndex = 0; rowIndex < dataGridViewStep.Rows.Count; rowIndex++)
            {
                var row = dataGridViewStep.Rows[rowIndex];
                if (row.IsNewRow)
                    continue;

                // Clear any previous error texts on this row's cells (we'll set if needed)
                foreach (DataGridViewCell c in row.Cells)
                    c.ErrorText = string.Empty;

                // 1) Step column selected
                if (colStep >= 0)
                {
                    var stepCell = row.Cells[colStep];
                    var val = stepCell?.Value;
                    if (val == null || string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        string msg = $"Row {rowIndex + 1}: Step not selected.";
                        errors.Add(msg);
                        if (firstInvalidCell == null) firstInvalidCell = stepCell;
                        if (stepCell != null) stepCell.ErrorText = "Select a step";
                    }
                }

                // 2) Temperature setpoint must be valid double with up to 2 decimals; format to 2 decimals
                if (colTempSet >= 0)
                {
                    var cell = row.Cells[colTempSet];
                    var raw = cell?.Value?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        string msg = $"Row {rowIndex + 1}: Temperature set point is empty or invalid.";
                        errors.Add(msg);
                        if (firstInvalidCell == null) firstInvalidCell = cell;
                        if (cell != null) cell.ErrorText = "Enter temperature set point";
                    }
                    else
                    {
                        if (!TryParseTwoDecimal(raw, out double parsed))
                        {
                            string msg = $"Row {rowIndex + 1}: Temperature set point must be a number with at most 2 decimals.";
                            errors.Add(msg);
                            if (firstInvalidCell == null) firstInvalidCell = cell;
                            if (cell != null) cell.ErrorText = "Invalid format (max 2 decimals)";
                        }
                        else
                        {
                            // Normalize/formatted display to 2 decimals
                            try
                            {
                                cell.Value = FormatTwoDecimal(parsed);
                            }
                            catch { /* ignore formatting failure */ }
                        }
                    }
                }

                // 3) Humidity setpoint must be valid double with up to 2 decimals; format to 2 decimals
                if (colHumSet >= 0)
                {
                    var cell = row.Cells[colHumSet];
                    var raw = cell?.Value?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        string msg = $"Row {rowIndex + 1}: Humidity set point is empty or invalid.";
                        errors.Add(msg);
                        if (firstInvalidCell == null) firstInvalidCell = cell;
                        if (cell != null) cell.ErrorText = "Enter humidity set point";
                    }
                    else
                    {
                        if (!TryParseTwoDecimal(raw, out double parsed))
                        {
                            string msg = $"Row {rowIndex + 1}: Humidity set point must be a number with at most 2 decimals.";
                            errors.Add(msg);
                            if (firstInvalidCell == null) firstInvalidCell = cell;
                            if (cell != null) cell.ErrorText = "Invalid format (max 2 decimals)";
                        }
                        else
                        {
                            try
                            {
                                cell.Value = FormatTwoDecimal(parsed);
                            }
                            catch { /* ignore formatting failure */ }
                        }
                    }
                }

                // 4) Evaluate Temperature checkbox -> if checked validate temperature accuracy column, else it must be blank
                if (colEvalTemp >= 0)
                {
                    bool evalTemp = GetCellBoolValue(rowIndex, colEvalTemp);

                    if (evalTemp)
                    {
                        if (colTempAccuracy >= 0)
                        {
                            var cAcc = row.Cells[colTempAccuracy];
                            var raw = cAcc?.Value?.ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(raw))
                            {
                                string msg = $"Row {rowIndex + 1}: Temperature accuracy is required when Evaluate Temperature is checked.";
                                errors.Add(msg);
                                if (firstInvalidCell == null) firstInvalidCell = cAcc;
                                if (cAcc != null) cAcc.ErrorText = "Required";
                            }
                            else if (!TryParseTwoDecimal(raw, out double pacc))
                            {
                                string msg = $"Row {rowIndex + 1}: Temperature accuracy must be a number with at most 2 decimals.";
                                errors.Add(msg);
                                if (firstInvalidCell == null) firstInvalidCell = cAcc;
                                if (cAcc != null) cAcc.ErrorText = "Invalid format";
                            }
                            else
                            {
                                try { cAcc.Value = FormatTwoDecimal(pacc); } catch { }
                            }
                        }
                        else
                        {
                            // If evaluation requires temperature accuracy but no such column exists, mark as error
                            string msg = $"Row {rowIndex + 1}: Evaluate Temperature is checked but no Temperature Accuracy column is present.";
                            errors.Add(msg);
                            if (firstInvalidCell == null) firstInvalidCell = dataGridViewStep.Rows[rowIndex].Cells[Math.Max(0, colEvalTemp)];
                        }
                    }
                    else
                    {
                        // If eval unchecked, accuracy should be blank (if column exists)
                        if (colTempAccuracy >= 0)
                        {
                            var cAcc = row.Cells[colTempAccuracy];
                            if (cAcc?.Value != null && !string.IsNullOrWhiteSpace(cAcc.Value.ToString()))
                            {
                                string msg = $"Row {rowIndex + 1}: Temperature accuracy must be blank when Evaluate Temperature is not checked.";
                                errors.Add(msg);
                                if (firstInvalidCell == null) firstInvalidCell = cAcc;
                                if (cAcc != null) cAcc.ErrorText = "Must be blank";
                            }
                        }
                    }
                }

                // 5) Evaluate Humidity checkbox -> if checked validate humidity accuracy column, else it must be blank
                if (colEvalHum >= 0)
                {
                    bool evalHum = GetCellBoolValue(rowIndex, colEvalHum);

                    if (evalHum)
                    {
                        if (colHumAccuracy >= 0)
                        {
                            var cAcc = row.Cells[colHumAccuracy];
                            var raw = cAcc?.Value?.ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(raw))
                            {
                                string msg = $"Row {rowIndex + 1}: Humidity accuracy is required when Evaluate Humidity is checked.";
                                errors.Add(msg);
                                if (firstInvalidCell == null) firstInvalidCell = cAcc;
                                if (cAcc != null) cAcc.ErrorText = "Required";
                            }
                            else if (!TryParseTwoDecimal(raw, out double pacc))
                            {
                                string msg = $"Row {rowIndex + 1}: Humidity accuracy must be a number with at most 2 decimals.";
                                errors.Add(msg);
                                if (firstInvalidCell == null) firstInvalidCell = cAcc;
                                if (cAcc != null) cAcc.ErrorText = "Invalid format";
                            }
                            else
                            {
                                try { cAcc.Value = FormatTwoDecimal(pacc); } catch { }
                            }
                        }
                        else
                        {
                            string msg = $"Row {rowIndex + 1}: Evaluate Humidity is checked but no Humidity Accuracy column is present.";
                            errors.Add(msg);
                            if (firstInvalidCell == null) firstInvalidCell = dataGridViewStep.Rows[rowIndex].Cells[Math.Max(0, colEvalHum)];
                        }
                    }
                    else
                    {
                        if (colHumAccuracy >= 0)
                        {
                            var cAcc = row.Cells[colHumAccuracy];
                            if (cAcc?.Value != null && !string.IsNullOrWhiteSpace(cAcc.Value.ToString()))
                            {
                                string msg = $"Row {rowIndex + 1}: Humidity accuracy must be blank when Evaluate Humidity is not checked.";
                                errors.Add(msg);
                                if (firstInvalidCell == null) firstInvalidCell = cAcc;
                                if (cAcc != null) cAcc.ErrorText = "Must be blank";
                            }
                        }
                    }
                }
            } // end rows loop

            if (errors.Count > 0)
            {
                try
                {
                    // Focus first invalid cell if available
                    if (firstInvalidCell != null)
                    {
                        dataGridViewStep.CurrentCell = firstInvalidCell;
                        dataGridViewStep.FirstDisplayedScrollingRowIndex = Math.Max(0, firstInvalidCell.RowIndex - 2);
                    }
                }
                catch { /* ignore focus errors */ }

                string msg = "Validation failed:\n" + string.Join("\n", errors.Distinct());
                MessageBox.Show(this, msg, "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            else
            {
                // Clear any lingering error texts
                foreach (DataGridViewRow row in dataGridViewStep.Rows)
                {
                    foreach (DataGridViewCell c in row.Cells)
                        c.ErrorText = string.Empty;
                }
                return true;
            }
        }
        private bool GridHasData()
        {
            // Check DataSource scenarios first for an accurate count
            if (dataGridViewStep.DataSource is DataTable dt)
            {
                return dt.Rows.Count > 0;
            }

            if (dataGridViewStep.DataSource is BindingSource bs)
            {
                // If BindingSource wraps a DataTable, use its row count
                if (bs.DataSource is DataTable bsDt)
                    return bsDt.Rows.Count > 0;

                // BindingSource exposes Count; use it if available
                try
                {
                    if (bs.Count > 0)
                        return true;
                }
                catch
                {
                    // Swallow and fallback to row inspection
                }
            }

            // Fallback for unbound or other binding types:
            // Inspect rows ignoring the NewRow placeholder. Consider a row with any non-empty cell as data.
            foreach (DataGridViewRow row in dataGridViewStep.Rows)
            {
                if (row.IsNewRow)
                    continue;

                foreach (DataGridViewCell cell in row.Cells)
                {
                    var v = cell?.Value;
                    if (v != null && !string.IsNullOrWhiteSpace(v.ToString()))
                        return true;
                }
            }

            return false;
        }
        private List<StepClass> BuildStepListFromGrid()
        {
            var result = new List<StepClass>();

            if (dataGridViewStep == null || dataGridViewStep.Columns.Count == 0)
                return result;

            // Helper to find column by tokens (name or header contains all tokens)
            int FindColumnIndexContaining(params string[] tokens)
            {
                if (tokens == null || tokens.Length == 0)
                    return -1;

                for (int i = 0; i < dataGridViewStep.Columns.Count; i++)
                {
                    var c = dataGridViewStep.Columns[i];
                    string combined = ((c?.Name ?? "") + "|" + (c?.HeaderText ?? "")).ToLowerInvariant();
                    bool all = true;
                    foreach (var t in tokens)
                    {
                        if (string.IsNullOrWhiteSpace(t))
                            continue;
                        if (!combined.Contains(t.ToLowerInvariant().Trim()))
                        {
                            all = false;
                            break;
                        }
                    }
                    if (all)
                        return i;
                }
                return -1;
            }

            // Identify columns (best-effort)
            int colStep = FindColumnIndexContaining("step");
            int colTempSet = FindColumnIndexContaining("temp", "set");
            int colHumSet = FindColumnIndexContaining("hum", "set");
            int colSoak = FindColumnIndexContaining("soak", "time");
            int colEvalTemp = FindColumnIndexContaining("eval", "temp");
            if (colEvalTemp < 0) colEvalTemp = FindColumnIndexContaining("evaluate", "temp");
            int colEvalHum = FindColumnIndexContaining("eval", "hum");
            if (colEvalHum < 0) colEvalHum = FindColumnIndexContaining("evaluate", "hum");

            int colTempAccuracy = FindTemperatureAccuracyColumnIndex();
            int colHumAccuracy = FindHumidityAccuracyColumnIndex();

            // Local helpers for parsing
            string GetCellText(DataGridViewRow row, int colIndex)
            {
                if (colIndex < 0 || colIndex >= dataGridViewStep.Columns.Count)
                    return string.Empty;
                var cell = row.Cells[colIndex];
                if (cell?.Value == null)
                    return string.Empty;
                return cell.Value.ToString().Trim();
            }

            double ParseDoubleOrNaN(string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return double.NaN;

                string t = s.Trim().Replace(',', '.');
                if (double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    return v;
                return double.NaN;
            }

            foreach (DataGridViewRow row in dataGridViewStep.Rows)
            {
                if (row == null || row.IsNewRow)
                    continue;

                var step = new StepClass();

                // Steps (string)
                if (colStep >= 0)
                    step.Steps = GetCellText(row, colStep);
                else
                    step.Steps = string.Empty;

                // Setpoints
                if (colHumSet >= 0)
                    step.HumiditySetPoint = ParseDoubleOrNaN(GetCellText(row, colHumSet));
                else
                    step.HumiditySetPoint = double.NaN;

                if (colTempSet >= 0)
                    step.TemperatureSetPoint = ParseDoubleOrNaN(GetCellText(row, colTempSet));
                else
                    step.TemperatureSetPoint = double.NaN;

                // Soak time (string)
                if (colSoak >= 0)
                    step.SoakTime = GetCellText(row, colSoak);
                else
                    step.SoakTime = string.Empty;

                // Evaluate flags
                if (colEvalTemp >= 0)
                    step.EvalTemp = GetCellBoolValue(row.Index, colEvalTemp);
                else
                    step.EvalTemp = false;

                if (colEvalHum >= 0)
                    step.EvalHumidity = GetCellBoolValue(row.Index, colEvalHum);
                else
                    step.EvalHumidity = false;

                // Accuracy values: map into StepClass numeric slots (MinTemperature/MinHumidity)
                // Min/Max were removed; use Min* fields to carry the single accuracy value, Max* set to NaN.
                step.MinTemperature = (colTempAccuracy >= 0) ? ParseDoubleOrNaN(GetCellText(row, colTempAccuracy)) : double.NaN;
                step.MaxTemperature = double.NaN;

                step.MinHumidity = (colHumAccuracy >= 0) ? ParseDoubleOrNaN(GetCellText(row, colHumAccuracy)) : double.NaN;
                step.MaxHumidity = double.NaN;

                result.Add(step);
            }

            return result;
        }
        private bool GetCellBoolValue(int rowIndex, int colIndex)
        {
            try
            {
                var r = dataGridViewStep.Rows[rowIndex];
                var cell = r.Cells[colIndex];
                if (cell == null)
                    return false;

                var val = cell.Value;
                if (val == null)
                    return false;

                if (val is bool b)
                    return b;

                // Sometimes CheckBox column can be boolean-like strings or ints
                string s = val.ToString();
                if (bool.TryParse(s, out bool parsedBool))
                    return parsedBool;
                if (int.TryParse(s, out int intVal))
                    return intVal != 0;

                return false;
            }
            catch
            {
                return false;
            }
        }
        private int FindTemperatureAccuracyColumnIndex()
        {
            for (int i = 0; i < dataGridViewStep.Columns.Count; i++)
            {
                var c = dataGridViewStep.Columns[i];
                string combined = ((c?.Name ?? "") + "|" + (c?.HeaderText ?? "")).ToLowerInvariant();
                if (combined.Contains("accuracy") && (combined.Contains("temp") || combined.Contains("temperature")))
                    return i;
            }
            return -1;
        }
        private int FindHumidityAccuracyColumnIndex()
        {
            for (int i = 0; i < dataGridViewStep.Columns.Count; i++)
            {
                var c = dataGridViewStep.Columns[i];
                string combined = ((c?.Name ?? "") + "|" + (c?.HeaderText ?? "")).ToLowerInvariant();
                if (combined.Contains("accuracy") && (combined.Contains("hum") || combined.Contains("humidity")))
                    return i;
            }
            return -1;
        }
    }
}