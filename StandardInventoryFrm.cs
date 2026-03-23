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

namespace Rotronic
{
    public partial class StandardInventoryFrm : Form
    {
        private const string ShowAllText = "Show All";
        private MaskedTextBox _dateMask;
        private int _dateRowIndex = -1;
        private int _dateColIndex = -1;

        public StandardInventoryFrm()
        {
            InitializeComponent();

            dataGridViewStandard.AllowUserToAddRows = false;
            dataGridViewStandard.AllowUserToDeleteRows = false;
            dataGridViewStandard.AllowUserToResizeRows = false;
            dataGridViewStandard.RowHeadersVisible = false;

            this.Load += StandardInventoryFrm_Load;
            comboBoxStandard.SelectedIndexChanged += comboBoxStandard_SelectedIndexChanged;

            dataGridViewStandard.EditingControlShowing += dataGridViewStandard_EditingControlShowing;
            dataGridViewStandard.CellBeginEdit += dataGridViewStandard_CellBeginEdit;
            dataGridViewStandard.CellEndEdit += dataGridViewStandard_CellEndEdit;

            dataGridViewStandard.CellDoubleClick += dataGridViewStandard_CellDoubleClick;
            dataGridViewStandard.KeyDown += dataGridViewStandard_KeyDown;
        }

        private void dataGridViewStandard_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            buttonReverse_Click(buttonReverse, EventArgs.Empty);
        }

        private void dataGridViewStandard_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            if (dataGridViewStandard.CurrentRow == null)
                return;

            // Avoid triggering while editing a cell.
            if (dataGridViewStandard.IsCurrentCellInEditMode)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            buttonReverse_Click(buttonReverse, EventArgs.Empty);
        }

        private void StandardInventoryFrm_Load(object sender, EventArgs e)
        {
            try
            {
                Data.InitializeDatabase();

                comboBoxStandard.BeginUpdate();
                var priorSelection = comboBoxStandard.SelectedItem as string;

                comboBoxStandard.Items.Clear();

                comboBoxStandard.Items.Insert(0, ShowAllText);

                foreach (var m in Data.GetMirrors())
                {
                    string name = string.IsNullOrWhiteSpace(m.Name) ? "No Name" : m.Name.Trim();
                    comboBoxStandard.Items.Add(string.Format("Mirror: {0}, {1}", name, m.SerialNumber ?? string.Empty));
                }

                foreach (var c in Data.GetChambers())
                {
                    string name = string.IsNullOrWhiteSpace(c.Name) ? "No Name" : c.Name.Trim();
                    comboBoxStandard.Items.Add(string.Format("Chamber: {0}, {1}", name, c.HC2SerialNumber ?? string.Empty));
                }

                if (!string.IsNullOrWhiteSpace(priorSelection))
                    comboBoxStandard.SelectedItem = priorSelection;

                if (comboBoxStandard.SelectedIndex < 0)
                    comboBoxStandard.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load standards list: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                comboBoxStandard.EndUpdate();
            }

        }

        public void SelectStandardInComboBox(string type, string name, string serial)
        {
            if (comboBoxStandard == null)
                return;

            string displayName = string.IsNullOrWhiteSpace(name) ? "No Name" : name.Trim();
            string displaySerial = serial ?? string.Empty;
            string formatted;

            if (string.Equals(type, "Mirror", StringComparison.OrdinalIgnoreCase))
                formatted = string.Format("Mirror: {0}, {1}", displayName, displaySerial);
            else if (string.Equals(type, "Chamber", StringComparison.OrdinalIgnoreCase))
                formatted = string.Format("Chamber: {0}, {1}", displayName, displaySerial);
            else
                return;

            // Prefer exact match
            for (int i = 0; i < comboBoxStandard.Items.Count; i++)
            {
                if (string.Equals(comboBoxStandard.Items[i]?.ToString(), formatted, StringComparison.Ordinal))
                {
                    comboBoxStandard.SelectedIndex = i;
                    return;
                }
            }

            // Fallback: match by serial substring
            for (int i = 0; i < comboBoxStandard.Items.Count; i++)
            {
                var text = comboBoxStandard.Items[i]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(displaySerial) && text.IndexOf(displaySerial, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    comboBoxStandard.SelectedIndex = i;
                    return;
                }
            }
        }

        private bool IsDateColumn(int colIndex)
        {
            if (colIndex < 0 || colIndex >= dataGridViewStandard.Columns.Count)
                return false;

            var name = dataGridViewStandard.Columns[colIndex].Name ?? string.Empty;
            return string.Equals(name, "ColumnCalibrationDate", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "ColumnCalibrationDueDate", StringComparison.OrdinalIgnoreCase);
        }

        private void dataGridViewStandard_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (!IsDateColumn(e.ColumnIndex))
                return;

            _dateRowIndex = e.RowIndex;
            _dateColIndex = e.ColumnIndex;

            var cell = dataGridViewStandard.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var current = (cell.Value ?? string.Empty).ToString().Trim();
            if (string.IsNullOrWhiteSpace(current) || current.Equals("DD-MMM-YYYY", StringComparison.OrdinalIgnoreCase))
                cell.Value = "__-___-____";

            // Ensure the editing system transitions into EditingControlShowing so our overlay is positioned.
            try { dataGridViewStandard.BeginInvoke((Action)(() => { try { dataGridViewStandard.BeginEdit(true); } catch { } })); } catch { }
        }

        private void dataGridViewStandard_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridViewStandard.CurrentCell == null)
                return;

            int col = dataGridViewStandard.CurrentCell.ColumnIndex;
            int row = dataGridViewStandard.CurrentCell.RowIndex;

            if (!IsDateColumn(col) || row < 0)
                return;

            // Hide the default editing control for date columns; we will overlay our own.
            try { e.Control.Visible = false; } catch { }

            // Replace default TextBox editor with an overlayed MaskedTextBox.
            if (_dateMask == null)
            {
                _dateMask = new MaskedTextBox();
                _dateMask.Mask = "00-LLL-0000";
                _dateMask.CutCopyMaskFormat = MaskFormat.ExcludePromptAndLiterals;
                _dateMask.TextMaskFormat = MaskFormat.IncludePromptAndLiterals;
                _dateMask.PromptChar = '_';
                _dateMask.HidePromptOnLeave = false;
                _dateMask.BorderStyle = BorderStyle.FixedSingle;
                _dateMask.Validating += DateMask_Validating;
                _dateMask.Validated += DateMask_Validated;
                _dateMask.KeyDown += DateMask_KeyDown;
                _dateMask.TextChanged += DateMask_TextChanged;
                _dateMask.Leave += (s, ev) => { try { _dateMask.Hide(); } catch { } };
            }

            var rect = dataGridViewStandard.GetCellDisplayRectangle(col, row, true);
            _dateMask.Bounds = rect;

            var cell = dataGridViewStandard.Rows[row].Cells[col];
            var current = (cell.Value ?? string.Empty).ToString().Trim();
            if (string.IsNullOrWhiteSpace(current) || current == "DD-MMM-YYYY")
                current = "__-___-____";

            // Ensure literals match expected '-' even if underlying text held different characters.
            current = current.Replace(' ', '_');
            _dateMask.Text = current;
            try { _dateMask.SkipLiterals = true; } catch { }
            try { _dateMask.ResetOnPrompt = false; } catch { }
            try { _dateMask.ResetOnSpace = false; } catch { }
            try { _dateMask.MaskCompleted.ToString(); } catch { }

            if (!_dateMask.Visible)
                dataGridViewStandard.Controls.Add(_dateMask);

            _dateMask.BringToFront();
            _dateMask.Show();
            _dateMask.Focus();

            // Start at first editable position
            try { _dateMask.Select(0, 0); } catch { }
        }

        private void dataGridViewStandard_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (!IsDateColumn(e.ColumnIndex))
                return;

            try { if (_dateMask != null) _dateMask.Hide(); } catch { }
        }

        private void DateMask_TextChanged(object sender, EventArgs e)
        {
            // MaskedTextBox shows literals, but caret movement can be awkward.
            // Nudge caret forward when user completes a segment so typing feels like it auto-adds '-'.
            if (_dateMask == null)
                return;

            try
            {
                // If caret lands on a dash literal, move to next prompt.
                int pos = _dateMask.SelectionStart;
                if (pos >= 0 && pos < _dateMask.Text.Length && _dateMask.Text[pos] == '-')
                    _dateMask.SelectionStart = pos + 1;
            }
            catch { }
        }

        private void DateMask_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                try { dataGridViewStandard.CancelEdit(); } catch { }
                try { _dateMask.Hide(); } catch { }
            }
        }

        private void DateMask_Validating(object sender, CancelEventArgs e)
        {
            if (_dateMask == null)
                return;

            string text = (_dateMask.Text ?? string.Empty).Trim().ToUpperInvariant();

            // If user left it blank-ish, store as empty.
            if (text == "__-___-____" || string.IsNullOrWhiteSpace(text) || text.Replace("_", string.Empty).Length == 0)
            {
                CommitDateToCell(string.Empty);
                try { _dateMask.Hide(); } catch { }
                return;
            }

            if (!IsValidDdMmmYyyy(text))
            {
                e.Cancel = true;
                _dateMask.BackColor = Color.MistyRose;
                return;
            }

            _dateMask.BackColor = SystemColors.Window;
            CommitDateToCell(text);

            try { _dateMask.Hide(); } catch { }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Ensure overlay editor is removed when it loses focus and apply due-date auto-fill in a consistent place.
            if (_dateMask != null)
            {
                try { _dateMask.Validated -= DateMask_Validated; } catch { }
                try { _dateMask.Validated += DateMask_Validated; } catch { }
            }
        }

        private void DateMask_Validated(object sender, EventArgs e)
        {
            if (_dateMask == null)
                return;

            string text = (_dateMask.Text ?? string.Empty).Trim().ToUpperInvariant();
            // QOL: if Calibration Date was entered and Due Date is blank/placeholder, auto-fill +1 year.
            try
            {
                if (_dateColIndex >= 0 && _dateRowIndex >= 0 && _dateRowIndex < dataGridViewStandard.Rows.Count)
                {
                    if (dataGridViewStandard.Columns[_dateColIndex].Name == "ColumnCalibrationDate" && IsValidDdMmmYyyy(text))
                    {
                        var dueColIdx = dataGridViewStandard.Columns["ColumnCalibrationDueDate"]?.Index ?? -1;
                        if (dueColIdx >= 0)
                        {
                            var dueCell = dataGridViewStandard.Rows[_dateRowIndex].Cells[dueColIdx];
                            var dueText = (dueCell.Value ?? string.Empty).ToString().Trim();
                            if (string.IsNullOrWhiteSpace(dueText) || dueText.Equals("DD-MMM-YYYY", StringComparison.OrdinalIgnoreCase) || dueText == "__-___-____")
                            {
                                if (DateTime.TryParseExact(text, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var calDate))
                                {
                                    var due = calDate.AddYears(1);
                                    dueCell.Value = due.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void CommitDateToCell(string value)
        {
            if (_dateRowIndex < 0 || _dateColIndex < 0)
                return;
            if (_dateRowIndex >= dataGridViewStandard.Rows.Count)
                return;
            if (_dateColIndex >= dataGridViewStandard.Columns.Count)
                return;

            try
            {
                dataGridViewStandard.Rows[_dateRowIndex].Cells[_dateColIndex].Value = value;
            }
            catch { }
        }

        private static bool IsValidDdMmmYyyy(string text)
        {
            // Strictly DD-MMM-YYYY with MMM as English month abbreviation.
            // Example: 05-JAN-2026
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (text.Length != 11)
                return false;

            if (text[2] != '-' || text[6] != '-')
                return false;

            string dd = text.Substring(0, 2);
            string mmm = text.Substring(3, 3).ToUpperInvariant();
            string yyyy = text.Substring(7, 4);

            if (!int.TryParse(dd, out int day) || day < 1 || day > 31)
                return false;
            if (!int.TryParse(yyyy, out int year) || year < 1900 || year > 9999)
                return false;

            string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
            if (Array.IndexOf(months, mmm) < 0)
                return false;

            // Basic range validation per month (ignore leap day correctness beyond Feb 29)
            int maxDay = 31;
            if (mmm == "APR" || mmm == "JUN" || mmm == "SEP" || mmm == "NOV")
                maxDay = 30;
            else if (mmm == "FEB")
                maxDay = 29;
            if (day > maxDay)
                return false;

            return true;
        }

        private void comboBoxStandard_SelectedIndexChanged(object sender, EventArgs e)
        {
            var text = comboBoxStandard.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (string.Equals(text.Trim(), ShowAllText, StringComparison.OrdinalIgnoreCase))
            {
                try { buttonUpdate.Enabled = false; } catch { }
                try
                {
                    Data.InitializeDatabase();
                    dataGridViewStandard.Rows.Clear();

                    foreach (var m in Data.GetMirrors())
                    {
                        int idx = dataGridViewStandard.Rows.Add();
                        var row = dataGridViewStandard.Rows[idx];
                        row.Cells["ColumnName"].Value = string.IsNullOrWhiteSpace(m.Name) ? "No Name" : m.Name;
                        row.Cells["ColumnSerial"].Value = m.SerialNumber ?? string.Empty;
                        row.Cells["ColumnCalibrationDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(m.LastCalibrationUtc);
                        row.Cells["ColumnCalibrationDueDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(m.NextDueUtc);
                        row.Cells["ColumnName"].ReadOnly = false;
                    }

                    foreach (var c in Data.GetChambers())
                    {
                        int idx = dataGridViewStandard.Rows.Add();
                        var row = dataGridViewStandard.Rows[idx];
                        row.Cells["ColumnName"].Value = string.IsNullOrWhiteSpace(c.Name) ? "No Name" : c.Name;
                        row.Cells["ColumnSerial"].Value = c.HC2SerialNumber ?? string.Empty;
                        row.Cells["ColumnCalibrationDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(c.ControlProbeCalibrationUtc);
                        row.Cells["ColumnCalibrationDueDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(c.ControlProbeNextDueUtc);
                        row.Cells["ColumnName"].ReadOnly = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed to load standards: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            try { buttonUpdate.Enabled = true; } catch { }

            try
            {
                Data.InitializeDatabase();

                // Expected formats:
                // "Mirror: {Name}, {SerialNumber}"
                // "Chamber: {Name}, {HC2SerialNumber}"
                bool isMirror = text.StartsWith("Mirror:", StringComparison.OrdinalIgnoreCase);
                bool isChamber = text.StartsWith("Chamber:", StringComparison.OrdinalIgnoreCase);

                string serial = string.Empty;
                int comma = text.LastIndexOf(',');
                if (comma >= 0 && comma + 1 < text.Length)
                    serial = text.Substring(comma + 1).Trim();

                dataGridViewStandard.Rows.Clear();

                if (isMirror)
                {
                    var m = Data.GetMirrorBySerial(serial);
                    if (m == null)
                        return;

                    int idx = dataGridViewStandard.Rows.Add();
                    var row = dataGridViewStandard.Rows[idx];
                    row.Cells["ColumnName"].Value = string.IsNullOrWhiteSpace(m.Name) ? "No Name" : m.Name;
                    row.Cells["ColumnSerial"].Value = m.SerialNumber ?? string.Empty;
                    row.Cells["ColumnCalibrationDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(m.LastCalibrationUtc);
                    row.Cells["ColumnCalibrationDueDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(m.NextDueUtc);

                    row.Cells["ColumnName"].ReadOnly = false;
                }
                else if (isChamber)
                {
                    var c = Data.GetChamberByHC2Serial(serial);
                    if (c == null)
                        return;

                    int idx = dataGridViewStandard.Rows.Add();
                    var row = dataGridViewStandard.Rows[idx];
                    row.Cells["ColumnName"].Value = string.IsNullOrWhiteSpace(c.Name) ? "No Name" : c.Name;
                    row.Cells["ColumnSerial"].Value = c.HC2SerialNumber ?? string.Empty;
                    row.Cells["ColumnCalibrationDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(c.ControlProbeCalibrationUtc);
                    row.Cells["ColumnCalibrationDueDate"].Value = FormatDbUtcToDdMmmYyyyOrPlaceholder(c.ControlProbeNextDueUtc);

                    row.Cells["ColumnName"].ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load standard details: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string FormatDbUtcToDdMmmYyyyOrPlaceholder(string dbUtc)
        {
            if (string.IsNullOrWhiteSpace(dbUtc))
                return "DD-MMM-YYYY";

            var t = dbUtc.Trim();
            if (DateTime.TryParseExact(t, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            {
                var utc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
                return utc.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
            }

            // If older data was stored in display format, keep it but normalize casing.
            if (DateTime.TryParseExact(t, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dd))
                return dd.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

            return t;
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {

            if (comboBoxStandard.SelectedItem == null)
                return;
            if (dataGridViewStandard.Rows.Count == 0)
                return;

            var selectedText = comboBoxStandard.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedText))
                return;

            bool isMirror = selectedText.StartsWith("Mirror:", StringComparison.OrdinalIgnoreCase);
            bool isChamber = selectedText.StartsWith("Chamber:", StringComparison.OrdinalIgnoreCase);

            var gridRow = dataGridViewStandard.Rows[0];

            string name = (gridRow.Cells["ColumnName"].Value ?? string.Empty).ToString().Trim();
            string serial = (gridRow.Cells["ColumnSerial"].Value ?? string.Empty).ToString().Trim();
            string cal = (gridRow.Cells["ColumnCalibrationDate"].Value ?? string.Empty).ToString().Trim().ToUpperInvariant();
            string due = (gridRow.Cells["ColumnCalibrationDueDate"].Value ?? string.Empty).ToString().Trim().ToUpperInvariant();

            // Respect the UI placeholders.
            if (string.Equals(cal, "DD-MMM-YYYY", StringComparison.OrdinalIgnoreCase)) cal = string.Empty;
            if (string.Equals(due, "DD-MMM-YYYY", StringComparison.OrdinalIgnoreCase)) due = string.Empty;

            if (!string.IsNullOrWhiteSpace(cal) && !IsValidDdMmmYyyy(cal))
            {
                MessageBox.Show(this, "Calibration Date must be in DD-MMM-YYYY format.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(due) && !IsValidDdMmmYyyy(due))
            {
                MessageBox.Show(this, "Calibration Due Date must be in DD-MMM-YYYY format.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Data.InitializeDatabase();

                if (isMirror)
                {
                    Data.UpdateMirrorInventory(serial, name, cal, due);
                }
                else if (isChamber)
                {
                    // Name is not editable for chamber rows.
                    Data.UpdateChamberInventory(serial, cal, due);
                }

                // Refresh UI from DB and keep selection.
                var prior = selectedText;
                StandardInventoryFrm_Load(this, EventArgs.Empty);
                comboBoxStandard.SelectedItem = prior;

                MessageBox.Show(this, "Updated.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to update: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonReverse_Click(object sender, EventArgs e)
        {
            try
            {
                Data.InitializeDatabase();

                string selectedText = (comboBoxStandard.SelectedItem as string) ?? string.Empty;
                bool isShowAll = string.Equals(selectedText.Trim(), ShowAllText, StringComparison.OrdinalIgnoreCase);

                bool isMirror = false;
                bool isChamber = false;
                string serial = string.Empty;

                if (!isShowAll)
                {
                    if (string.IsNullOrWhiteSpace(selectedText))
                        return;

                    isMirror = selectedText.StartsWith("Mirror:", StringComparison.OrdinalIgnoreCase);
                    isChamber = selectedText.StartsWith("Chamber:", StringComparison.OrdinalIgnoreCase);

                    int comma = selectedText.LastIndexOf(',');
                    if (comma >= 0 && comma + 1 < selectedText.Length)
                        serial = selectedText.Substring(comma + 1).Trim();
                }
                else
                {
                    // Infer type/serial from the selected grid row.
                    var row = dataGridViewStandard.CurrentRow;
                    if (row == null)
                        return;

                    serial = (row.Cells["ColumnSerial"].Value ?? string.Empty).ToString().Trim();
                    if (string.IsNullOrWhiteSpace(serial))
                        return;

                    // In Show All mode: mirror rows have editable name, chamber rows do not.
                    bool nameReadOnly = true;
                    try { nameReadOnly = row.Cells["ColumnName"].ReadOnly; } catch { }

                    isMirror = !nameReadOnly;
                    isChamber = nameReadOnly;
                }

                if (string.IsNullOrWhiteSpace(serial))
                    return;

                if (isMirror)
                {
                    using (var frm = new ProbeInventoryCalHistoryFrm(serial, "MirrorSerialNumber"))
                        frm.ShowDialog(this);
                }
                else if (isChamber)
                {
                    // UI shows HC2 serial; for reverse traceability we need ControllerSerialNumber.
                    var controllerSerial = Data.GetChamberControllerSerialByHC2Serial(serial) ?? serial;
                    using (var frm = new ProbeInventoryCalHistoryFrm(controllerSerial, "ChamberControllerSerialNumber"))
                        frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to open reverse traceability history: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
