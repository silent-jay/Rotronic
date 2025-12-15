using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Rotronic
{
    public partial class TempAdjustSetupFrm : Form
    {
        private Main _main;

        // Persist checked state between refreshes using a stable row key (concatenate main columns)
        private readonly Dictionary<string, bool> _probeCheckedStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _mirrorCheckedStates = new Dictionary<string, bool>();

        // suppression flags to avoid recursion when programmatically changing checks
        private bool _suspendItemCheckedHandler;
        private bool _suspendSelectAllHandler;

        public TempAdjustSetupFrm()
        {
            InitializeComponent();
        }

        private void TempAdjustSetupFrm_Load(object sender, EventArgs e)
        {
            // Find the running Main form instance
            _main = Application.OpenForms.OfType<Main>().FirstOrDefault();
            if (_main != null)
            {
                _main.ProbesRefreshed += Main_ProbesRefreshed;
                _main.MirrorsRefreshed += Main_MirrorsRefreshed;

                // wire events to keep header checkbox positioned and to track user item checks
                listViewRotProbe.ColumnWidthChanged += ListViewRotProbe_ColumnWidthChanged;
                listViewRotProbe.ItemChecked += ListViewRotProbe_ItemChecked;
                listViewMirror.ColumnWidthChanged += ListViewMirror_ColumnWidthChanged;
                listViewMirror.ItemChecked += ListViewMirror_ItemChecked;

                // initial copy to mirror current state
                CopyProbesFromMain(_main);
                CopyMirrorsFromMain(_main);

                // Make sure select-all checkboxes are placed correctly
                PositionSelectAllCheckboxes();
            }
        }

        private void TempAdjustSetupFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_main != null)
            {
                _main.ProbesRefreshed -= Main_ProbesRefreshed;
                _main.MirrorsRefreshed -= Main_MirrorsRefreshed;
                _main = null;
            }
        }

        private void TempAdjustSetupFrm_Resize(object sender, EventArgs e)
        {
            PositionSelectAllCheckboxes();
        }

        private void Main_ProbesRefreshed(object sender, EventArgs e)
        {
            CopyProbesFromMain(sender as Main);
        }

        private void Main_MirrorsRefreshed(object sender, EventArgs e)
        {
            CopyMirrorsFromMain(sender as Main);
        }

        // Build a stable key from the main data columns (skip the "Test" column in this form)
        private static string BuildRowKeyFromSubItems(ListViewItem item, bool hasTestColumn)
        {
            // If the item is null, return empty key
            if (item == null) return string.Empty;

            int startIndex = hasTestColumn ?1 :0;
            var parts = new List<string>();
            for (int i = startIndex; i < item.SubItems.Count; i++)
            {
                parts.Add(item.SubItems[i].Text ?? string.Empty);
            }
            // Use a non-printable separator to avoid accidental collisions
            return string.Join("\u001F", parts);
        }

        private void CopyProbesFromMain(Main main)
        {
            if (main == null || listViewRotProbe == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => CopyProbesFromMain(main)));
                return;
            }

            listViewRotProbe.BeginUpdate();
            try
            {
                // Capture current checked states before clearing
                _probeCheckedStates.Clear();
                foreach (ListViewItem existing in listViewRotProbe.Items)
                {
                    string key = BuildRowKeyFromSubItems(existing, hasTestColumn: true);
                    _probeCheckedStates[key] = existing.Checked;
                }

                listViewRotProbe.Items.Clear();
                listViewRotProbe.Columns.Clear();

                // Add first "Test" column (checkbox column)
                var testCol = new ColumnHeader { Name = "Test", Text = "Test", Width =60, TextAlign = HorizontalAlignment.Left };
                listViewRotProbe.Columns.Add(testCol);

                // Copy columns from main (they become shifted to the right)
                foreach (ColumnHeader srcCol in main.listViewRotProbe.Columns)
                {
                    var col = new ColumnHeader
                    {
                        Name = srcCol.Name,
                        Text = srcCol.Text,
                        Width = srcCol.Width,
                        TextAlign = srcCol.TextAlign
                    };
                    listViewRotProbe.Columns.Add(col);
                }

                listViewRotProbe.View = View.Details;
                listViewRotProbe.FullRowSelect = true;
                listViewRotProbe.CheckBoxes = true;

                // Copy items: insert original columns as subitems after the Test column.
                foreach (ListViewItem srcItem in main.listViewRotProbe.Items)
                {
                    // First cell (Test column) left blank; checkbox will appear there.
                    var lvi = new ListViewItem(string.Empty) { Checked = false };

                    // Copy all original subitems (including the original first column)
                    foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                    {
                        lvi.SubItems.Add(sub.Text);
                    }

                    // Restore checked state if we have a previous value
                    string key = BuildRowKeyFromSubItems(lvi, hasTestColumn: true);
                    if (_probeCheckedStates.TryGetValue(key, out bool wasChecked))
                        lvi.Checked = wasChecked;
                    else
                        lvi.Checked = false;

                    listViewRotProbe.Items.Add(lvi);
                }

                // After populating, update the select-all checkbox state
                UpdateSelectAllCheckboxState(chkSelectAllProbes, listViewRotProbe);
            }
            finally
            {
                listViewRotProbe.EndUpdate();
                // Ensure header checkbox positioned after columns are created
                PositionSelectAllCheckboxes();
            }
        }

        private void CopyMirrorsFromMain(Main main)
        {
            if (main == null || listViewMirror == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => CopyMirrorsFromMain(main)));
                return;
            }

            listViewMirror.BeginUpdate();
            try
            {
                // Capture current checked states before clearing
                _mirrorCheckedStates.Clear();
                foreach (ListViewItem existing in listViewMirror.Items)
                {
                    string key = BuildRowKeyFromSubItems(existing, hasTestColumn: true);
                    _mirrorCheckedStates[key] = existing.Checked;
                }

                listViewMirror.Items.Clear();
                listViewMirror.Columns.Clear();

                // Add first "Test" column (checkbox column)
                var testCol = new ColumnHeader { Name = "Test", Text = "Test", Width =60, TextAlign = HorizontalAlignment.Left };
                listViewMirror.Columns.Add(testCol);

                // Copy columns from main (they become shifted to the right)
                foreach (ColumnHeader srcCol in main.listViewMirror.Columns)
                {
                    var col = new ColumnHeader
                    {
                        Name = srcCol.Name,
                        Text = srcCol.Text,
                        Width = srcCol.Width,
                        TextAlign = srcCol.TextAlign
                    };
                    listViewMirror.Columns.Add(col);
                }

                listViewMirror.View = View.Details;
                listViewMirror.FullRowSelect = true;
                listViewMirror.CheckBoxes = true;

                // Copy items: insert original columns as subitems after the Test column.
                foreach (ListViewItem srcItem in main.listViewMirror.Items)
                {
                    var lvi = new ListViewItem(string.Empty) { Checked = false };
                    foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                    {
                        lvi.SubItems.Add(sub.Text);
                    }

                    // Restore checked state if we have a previous value
                    string key = BuildRowKeyFromSubItems(lvi, hasTestColumn: true);
                    if (_mirrorCheckedStates.TryGetValue(key, out bool wasChecked))
                        lvi.Checked = wasChecked;
                    else
                        lvi.Checked = false;

                    listViewMirror.Items.Add(lvi);
                }

                // After populating, update the select-all checkbox state
                UpdateSelectAllCheckboxState(chkSelectAllMirrors, listViewMirror);
            }
            finally
            {
                listViewMirror.EndUpdate();
                // Ensure header checkbox positioned after columns are created
                PositionSelectAllCheckboxes();
            }
        }

        // Called when individual probe item is (un)checked by the user
        private void ListViewRotProbe_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendItemCheckedHandler) return;

            string key = BuildRowKeyFromSubItems(e.Item, hasTestColumn: true);
            _probeCheckedStates[key] = e.Item.Checked;

            UpdateSelectAllCheckboxState(chkSelectAllProbes, listViewRotProbe);
        }

        // Called when individual mirror item is (un)checked by the user
        private void ListViewMirror_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendItemCheckedHandler) return;

            string key = BuildRowKeyFromSubItems(e.Item, hasTestColumn: true);
            _mirrorCheckedStates[key] = e.Item.Checked;

            UpdateSelectAllCheckboxState(chkSelectAllMirrors, listViewMirror);
        }

        private void chkSelectAllProbes_CheckedChanged(object sender, EventArgs e)
        {
            if (_suspendSelectAllHandler) return;
            if (chkSelectAllProbes.CheckState == CheckState.Indeterminate) return;

            bool check = chkSelectAllProbes.Checked;
            _suspendItemCheckedHandler = true;
            try
            {
                foreach (ListViewItem item in listViewRotProbe.Items)
                {
                    item.Checked = check;
                    string key = BuildRowKeyFromSubItems(item, hasTestColumn: true);
                    _probeCheckedStates[key] = check;
                }
            }
            finally
            {
                _suspendItemCheckedHandler = false;
            }
        }

        private void chkSelectAllMirrors_CheckedChanged(object sender, EventArgs e)
        {
            if (_suspendSelectAllHandler) return;
            if (chkSelectAllMirrors.CheckState == CheckState.Indeterminate) return;

            bool check = chkSelectAllMirrors.Checked;
            _suspendItemCheckedHandler = true;
            try
            {
                foreach (ListViewItem item in listViewMirror.Items)
                {
                    item.Checked = check;
                    string key = BuildRowKeyFromSubItems(item, hasTestColumn: true);
                    _mirrorCheckedStates[key] = check;
                }
            }
            finally
            {
                _suspendItemCheckedHandler = false;
            }
        }

        // Keep header checkbox visually aligned with the first column; reposition when columns change or form resizes
        private void PositionSelectAllCheckboxes()
        {
            PositionSelectAllCheckbox(listViewRotProbe, chkSelectAllProbes);
            PositionSelectAllCheckbox(listViewMirror, chkSelectAllMirrors);
        }

        private void PositionSelectAllCheckbox(ListView lv, CheckBox chk)
        {
            if (lv == null || chk == null) return;

            // Put the checkbox roughly over the first header cell.
            // We position it relative to the listview's client area, with a small padding.
            var lvScreen = lv.PointToScreen(Point.Empty);
            var formClient = this.PointToClient(lvScreen);

            // X: left of listView + small offset
            int x = formClient.X +6;
            // Y: top of listView + small offset to align with header row
            int y = formClient.Y +6;

            // Ensure checkbox stays within form bounds
            if (x <0) x =2;
            if (y <0) y =2;

            chk.Location = new Point(x, y);
            // Make sure it's visible and ThreeState so we can show Indeterminate when some checked
            chk.Visible = true;
            chk.ThreeState = true;
        }

        private void ListViewRotProbe_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            PositionSelectAllCheckbox(listViewRotProbe, chkSelectAllProbes);

            // Persist widths to header options so main refresh does not overwrite user resized widths
            try
            {
                var opts = HeaderOptions.Load() ?? new HeaderOptions();
                foreach (ColumnHeader col in listViewRotProbe.Columns)
                {
                    if (string.IsNullOrWhiteSpace(col.Name))
                        continue;

                    // Skip the 'Test' column introduced in this form
                    if (col.Name.Equals("Test", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (Enum.TryParse<HeaderOptions.Field>(col.Name, true, out var field))
                    {
                        if (opts.Options.ContainsKey(field))
                        {
                            opts.Options[field].Width = col.Width;
                        }
                    }
                }
                opts.Save();
            }
            catch
            {
                // ignore persistence failures
            }
        }

        private void ListViewMirror_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            PositionSelectAllCheckbox(listViewMirror, chkSelectAllMirrors);

            // Persist widths to mirror display options so main refresh does not overwrite user resized widths
            try
            {
                var opts = MirrorDisplayOptions.Load() ?? new MirrorDisplayOptions();
                foreach (ColumnHeader col in listViewMirror.Columns)
                {
                    if (string.IsNullOrWhiteSpace(col.Name))
                        continue;

                    if (Enum.TryParse<MirrorDisplayOptions.Field>(col.Name, true, out var field))
                    {
                        if (opts.Options.ContainsKey(field))
                        {
                            opts.Options[field].Width = col.Width;
                        }
                    }
                }
                opts.Save();
            }
            catch
            {
                // ignore persistence failures
            }
        }

        // Set select-all checkbox state based on current items: Checked if all checked, Unchecked if none, Indeterminate otherwise
        private void UpdateSelectAllCheckboxState(CheckBox chk, ListView lv)
        {
            if (chk == null || lv == null) return;

            _suspendSelectAllHandler = true;
            try
            {
                int total = lv.Items.Count;
                if (total ==0)
                {
                    chk.CheckState = CheckState.Unchecked;
                    return;
                }

                int checkedCount = lv.Items.Cast<ListViewItem>().Count(i => i.Checked);
                if (checkedCount ==0)
                    chk.CheckState = CheckState.Unchecked;
                else if (checkedCount == total)
                    chk.CheckState = CheckState.Checked;
                else
                    chk.CheckState = CheckState.Indeterminate;
            }
            finally
            {
                _suspendSelectAllHandler = false;
            }
        }

        public void buttonBegin_Click(object sender, EventArgs e)
        {
            // Collect selected probes
            var selectedProbes = new List<ListViewItem>();
            foreach (ListViewItem item in listViewRotProbe.Items)
            {
                if (item.Checked)
                {
                    selectedProbes.Add(item);
                }
            }
            // Collect selected mirror (exactly one required)
            ListViewItem selectedMirror = null;
            int mirrorCount =0;
            foreach (ListViewItem item in listViewMirror.Items)
            {
                if (item.Checked)
                {
                    mirrorCount++;
                    if (mirrorCount ==1)
                        selectedMirror = item;
                }
            }

            // Validate selections
            if (selectedProbes.Count ==0)
            {
                MessageBox.Show("Please select at least one probe for adjustment.", "No Probes Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (mirrorCount ==0)
            {
                MessageBox.Show("Please select one mirror for adjustment.", "No Mirror Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (mirrorCount >1)
            {
                MessageBox.Show("Please select only one mirror for adjustment.", "Multiple Mirrors Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var tempAdjustProcess = new TempAdjProgressFrm(selectedProbes, selectedMirror, checkBoxManual.Checked);
            tempAdjustProcess.Show();
        }
    }
}