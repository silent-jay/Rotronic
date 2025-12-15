using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Rotronic
{
    public partial class CalibrationSetupFrm : Form
    {
        private Main _main;

        // Persist checked state between refreshes using a stable row key (concatenate main columns)
        private readonly Dictionary<string, bool> _probeCheckedStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _mirrorCheckedStates = new Dictionary<string, bool>();

        // Fallback maps by ComPort (most stable single identifier)
        private readonly Dictionary<string, bool> _probeCheckedByComPort = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _mirrorCheckedByComPort = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // suppression flags to avoid recursion when programmatically changing checks
        private bool _suspendItemCheckedHandler;
        private bool _suspendSelectAllHandler;

        // Store steps passed from StepEditor
        private List<StepClass> _steps = new List<StepClass>();

        public CalibrationSetupFrm()
        {
            InitializeComponent();
        }

        // New constructor to accept steps
        public CalibrationSetupFrm(List<StepClass> steps) : this()
        {
            _steps = steps ?? new List<StepClass>();
            // Optionally reflect step count in the form title
            try
            {
                this.Text = $"Calibration Setup ({_steps.Count} steps)";
            }
            catch { /* ignore UI errors */ }
        }

        // Expose read-only access if other code needs the steps
        public IReadOnlyList<StepClass> Steps => _steps.AsReadOnly();

        private void CalibrationSetupFrm_Load(object sender, EventArgs e)
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

        private void CalibrationSetupFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_main != null)
            {
                _main.ProbesRefreshed -= Main_ProbesRefreshed;
                _main.MirrorsRefreshed -= Main_MirrorsRefreshed;
                _main = null;
            }
        }

        private void CalibrationSetupFrm_Resize(object sender, EventArgs e)
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

        // Build a stable key from a few stable column names (ComPort, SerialNumber, DeviceName, ProbeAddress, ProbeType)
        // Falls back to concatenating all subitems (excluding Test) if none of those columns exist.
        private static string BuildRowKeyFromSubItems(ListView lv, ListViewItem item, bool hasTestColumn)
        {
            if (item == null || lv == null) return string.Empty;

            // Preferred stable column names for probes
            var preferred = new[] { "ComPort", "SerialNumber", "DeviceName", "ProbeAddress", "ProbeType" };
            var available = new List<string>();

            foreach (var name in preferred)
            {
                for (int ci =0; ci < lv.Columns.Count; ci++)
                {
                    var col = lv.Columns[ci];
                    if (string.Equals(col.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        available.Add(name);
                        break;
                    }
                }
            }

            // If we found at least one stable column, use those columns (in order) to build key
            if (available.Count >0)
            {
                var parts = new List<string>();
                foreach (var name in available)
                {
                    // find column index in lv.Columns
                    int colIndex = -1;
                    for (int ci =0; ci < lv.Columns.Count; ci++)
                    {
                        if (string.Equals(lv.Columns[ci].Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            colIndex = ci;
                            break;
                        }
                    }

                    if (colIndex >=0)
                    {
                        // account for Test column shifting in setup forms
                        int subIndex = hasTestColumn ? colIndex +1 : colIndex;
                        if (subIndex >=0 && subIndex < item.SubItems.Count)
                            parts.Add(item.SubItems[subIndex].Text ?? string.Empty);
                        else
                            parts.Add(string.Empty);
                    }
                }

                return string.Join("\u001F", parts);
            }

            // Fallback: concat all subitems excluding Test column
            int startIndex = hasTestColumn ?1 :0;
            var allParts = new List<string>();
            for (int i = startIndex; i < item.SubItems.Count; i++)
            {
                allParts.Add(item.SubItems[i].Text ?? string.Empty);
            }
            return string.Join("\u001F", allParts);
        }

        // Helper: read subitem text by column name from a source ListView's columns (handles Test shift)
        private static string GetSubItemTextByColumnName(ListView sourceListView, ListViewItem item, string columnName, bool hasTestColumn)
        {
            if (sourceListView == null || item == null || string.IsNullOrEmpty(columnName))
                return string.Empty;

            for (int ci =0; ci < sourceListView.Columns.Count; ci++)
            {
                if (string.Equals(sourceListView.Columns[ci].Name, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    int subIndex = hasTestColumn ? ci +1 : ci;
                    if (subIndex >=0 && subIndex < item.SubItems.Count)
                        return item.SubItems[subIndex].Text ?? string.Empty;
                    return string.Empty;
                }
            }
            return string.Empty;
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
                // Capture current checked states before touching the list
                _probeCheckedStates.Clear();
                _probeCheckedByComPort.Clear();
                foreach (ListViewItem existing in listViewRotProbe.Items)
                {
                    string key = BuildRowKeyFromSubItems(listViewRotProbe, existing, hasTestColumn: true);
                    _probeCheckedStates[key] = existing.Checked;

                    // also try to capture ComPort if present in this setup list
                    string com = GetSubItemTextByColumnName(listViewRotProbe, existing, "ComPort", hasTestColumn: true);
                    if (!string.IsNullOrEmpty(com) && !_probeCheckedByComPort.ContainsKey(com))
                        _probeCheckedByComPort[com] = existing.Checked;
                }

                // If columns already match (Test + main columns) and simple update is possible, do in-place update
                bool columnsMatch = false;
                if (main != null && main.listViewRotProbe != null)
                {
                    if (listViewRotProbe.Columns.Count == main.listViewRotProbe.Columns.Count +1)
                    {
                        columnsMatch = true;
                        for (int i =0; i < main.listViewRotProbe.Columns.Count; i++)
                        {
                            var mainCol = main.listViewRotProbe.Columns[i];
                            var thisCol = listViewRotProbe.Columns[i +1];
                            if (!string.Equals(mainCol.Name, thisCol.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                columnsMatch = false;
                                break;
                            }
                        }
                    }
                }

                if (columnsMatch)
                {
                    // Update items in-place using main items to preserve Checked
                    // Build existing map by ComPort (case-insensitive)
                    var existingMap = new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);
                    foreach (ListViewItem it in listViewRotProbe.Items)
                    {
                        string com = GetSubItemTextByColumnName(listViewRotProbe, it, "ComPort", hasTestColumn: true);
                        if (string.IsNullOrEmpty(com) && it.Tag is RotProbe rp)
                            com = rp.ComPort;
                        if (!string.IsNullOrEmpty(com) && !existingMap.ContainsKey(com))
                            existingMap[com] = it;
                    }

                    var processed = new HashSet<ListViewItem>();

                    // Iterate main items and update or add
                    foreach (ListViewItem srcItem in main.listViewRotProbe.Items)
                    {
                        RotProbe rp = srcItem.Tag as RotProbe;
                        string comKey = rp?.ComPort ?? GetSubItemTextByColumnName(main.listViewRotProbe, srcItem, "ComPort", hasTestColumn: false);

                        ListViewItem target = null;
                        if (!string.IsNullOrEmpty(comKey) && existingMap.TryGetValue(comKey, out target))
                        {
                            // Update subitems (shifted by1 because of Test column)
                            for (int si =0; si < srcItem.SubItems.Count; si++)
                            {
                                int destIndex = si +1;
                                string txt = srcItem.SubItems[si].Text ?? string.Empty;
                                if (destIndex < target.SubItems.Count)
                                    target.SubItems[destIndex].Text = txt;
                                else
                                    target.SubItems.Add(txt);
                            }

                            // update tag
                            target.Tag = rp ?? srcItem.Tag;
                            processed.Add(target);
                        }
                        else
                        {
                            // new item -> add preserving checked fallback
                            var lvi = new ListViewItem(string.Empty) { Checked = false };
                            foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                                lvi.SubItems.Add(sub.Text);

                            // Restore checked by previous maps
                            bool restored = false;
                            string mainKey = BuildRowKeyFromSubItems(main.listViewRotProbe, srcItem, hasTestColumn: false);
                            if (_probeCheckedStates.TryGetValue(mainKey, out bool wasChecked))
                            {
                                lvi.Checked = wasChecked;
                                restored = true;
                            }
                            else if (!string.IsNullOrEmpty(comKey) && _probeCheckedByComPort.TryGetValue(comKey, out bool byCom))
                            {
                                lvi.Checked = byCom;
                                restored = true;
                            }

                            lvi.Tag = rp ?? srcItem.Tag;
                            listViewRotProbe.Items.Add(lvi);
                        }
                    }

                    // Remove any items not processed (they no longer exist in main)
                    var toRemove = listViewRotProbe.Items.Cast<ListViewItem>().Where(it => !processed.Contains(it)).ToList();
                    foreach (var it in toRemove)
                    {
                        // Keep those with no tag/comport? They are stale -> remove
                        listViewRotProbe.Items.Remove(it);
                    }

                    // After populating, update the select-all checkbox state
                    UpdateSelectAllCheckboxState(chkSelectAllProbes, listViewRotProbe);

                    return;
                }

                // Fallback: full rebuild (columns changed or first load)
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

                    // Compute key using main's columns (no Test column in main)
                    string mainKey = BuildRowKeyFromSubItems(main.listViewRotProbe, srcItem, hasTestColumn: false);

                    // Prefer Tag-based identification: underlying RotProbe.ComPort when available
                    bool restored = false;
                    try
                    {
                        if (srcItem.Tag is RotProbe rp && !string.IsNullOrEmpty(rp.ComPort))
                        {
                            if (_probeCheckedByComPort.TryGetValue(rp.ComPort, out bool byCom))
                            {
                                lvi.Checked = byCom;
                                restored = true;
                            }
                        }
                    }
                    catch { }

                    // Try to restore previous checked state by mainKey first (if not restored by com)
                    if (!restored && _probeCheckedStates.TryGetValue(mainKey, out bool wasChecked))
                    {
                        lvi.Checked = wasChecked;
                        restored = true;
                    }

                    // fallback by ComPort read from main item if still not restored
                    if (!restored)
                    {
                        string com = GetSubItemTextByColumnName(main.listViewRotProbe, srcItem, "ComPort", hasTestColumn: false);
                        if (!string.IsNullOrEmpty(com) && _probeCheckedByComPort.TryGetValue(com, out bool byCom2))
                        {
                            lvi.Checked = byCom2;
                            restored = true;
                        }
                    }

                    if (!restored)
                        lvi.Checked = false;

                    lvi.Tag = srcItem.Tag;
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
                // Capture current checked states before touching the list
                _mirrorCheckedStates.Clear();
                _mirrorCheckedByComPort.Clear();
                foreach (ListViewItem existing in listViewMirror.Items)
                {
                    string key = BuildRowKeyFromSubItems(listViewMirror, existing, hasTestColumn: true);
                    _mirrorCheckedStates[key] = existing.Checked;

                    string com = GetSubItemTextByColumnName(listViewMirror, existing, "ComPort", hasTestColumn: true);
                    if (!string.IsNullOrEmpty(com) && !_mirrorCheckedByComPort.ContainsKey(com))
                        _mirrorCheckedByComPort[com] = existing.Checked;
                }

                // If columns already match (Test + main columns) and simple update is possible, do in-place update
                bool columnsMatch = false;
                if (main != null && main.listViewMirror != null)
                {
                    if (listViewMirror.Columns.Count == main.listViewMirror.Columns.Count +1)
                    {
                        columnsMatch = true;
                        for (int i =0; i < main.listViewMirror.Columns.Count; i++)
                        {
                            var mainCol = main.listViewMirror.Columns[i];
                            var thisCol = listViewMirror.Columns[i +1];
                            if (!string.Equals(mainCol.Name, thisCol.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                columnsMatch = false;
                                break;
                            }
                        }
                    }
                }

                if (columnsMatch)
                {
                    var existingMap = new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);
                    foreach (ListViewItem it in listViewMirror.Items)
                    {
                        string com = GetSubItemTextByColumnName(listViewMirror, it, "ComPort", hasTestColumn: true);
                        if (string.IsNullOrEmpty(com) && it.Tag is Mirror mr)
                            com = mr.ComPort;
                        if (!string.IsNullOrEmpty(com) && !existingMap.ContainsKey(com))
                            existingMap[com] = it;
                    }

                    var processed = new HashSet<ListViewItem>();

                    foreach (ListViewItem srcItem in main.listViewMirror.Items)
                    {
                        Mirror m = srcItem.Tag as Mirror;
                        string comKey = m?.ComPort ?? GetSubItemTextByColumnName(main.listViewMirror, srcItem, "ComPort", hasTestColumn: false);

                        ListViewItem target = null;
                        if (!string.IsNullOrEmpty(comKey) && existingMap.TryGetValue(comKey, out target))
                        {
                            // Update subitems (shifted by1 because of Test column)
                            for (int si =0; si < srcItem.SubItems.Count; si++)
                            {
                                int destIndex = si +1;
                                string txt = srcItem.SubItems[si].Text ?? string.Empty;
                                if (destIndex < target.SubItems.Count)
                                    target.SubItems[destIndex].Text = txt;
                                else
                                    target.SubItems.Add(txt);
                            }

                            target.Tag = m ?? srcItem.Tag;
                            processed.Add(target);
                        }
                        else
                        {
                            var lvi = new ListViewItem(string.Empty) { Checked = false };
                            foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                                lvi.SubItems.Add(sub.Text);

                            bool restored = false;
                            string mainKey = BuildRowKeyFromSubItems(main.listViewMirror, srcItem, hasTestColumn: false);
                            if (_mirrorCheckedStates.TryGetValue(mainKey, out bool wasChecked))
                            {
                                lvi.Checked = wasChecked;
                                restored = true;
                            }
                            else if (!string.IsNullOrEmpty(comKey) && _mirrorCheckedByComPort.TryGetValue(comKey, out bool byCom))
                            {
                                lvi.Checked = byCom;
                                restored = true;
                            }

                            lvi.Tag = srcItem.Tag;
                            listViewMirror.Items.Add(lvi);
                        }
                    }

                    var toRemove = listViewMirror.Items.Cast<ListViewItem>().Where(it => !processed.Contains(it)).ToList();
                    foreach (var it in toRemove)
                        listViewMirror.Items.Remove(it);

                    UpdateSelectAllCheckboxState(chkSelectAllMirrors, listViewMirror);
                    return;
                }

                // Fallback: full rebuild
                listViewMirror.Items.Clear();
                listViewMirror.Columns.Clear();

                var testCol = new ColumnHeader { Name = "Test", Text = "Test", Width =60, TextAlign = HorizontalAlignment.Left };
                listViewMirror.Columns.Add(testCol);

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

                foreach (ListViewItem srcItem in main.listViewMirror.Items)
                {
                    var lvi = new ListViewItem(string.Empty) { Checked = false };
                    foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                    {
                        lvi.SubItems.Add(sub.Text);
                    }

                    string mainKey = BuildRowKeyFromSubItems(main.listViewMirror, srcItem, hasTestColumn: false);

                    bool restored = false;
                    try
                    {
                        if (srcItem.Tag is Mirror m && !string.IsNullOrEmpty(m.ComPort))
                        {
                            if (_mirrorCheckedByComPort.TryGetValue(m.ComPort, out bool byCom))
                            {
                                lvi.Checked = byCom;
                                restored = true;
                            }
                        }
                    }
                    catch { }

                    if (!restored && _mirrorCheckedStates.TryGetValue(mainKey, out bool wasChecked))
                    {
                        lvi.Checked = wasChecked;
                        restored = true;
                    }

                    if (!restored)
                    {
                        string com = GetSubItemTextByColumnName(main.listViewMirror, srcItem, "ComPort", hasTestColumn: false);
                        if (!string.IsNullOrEmpty(com) && _mirrorCheckedByComPort.TryGetValue(com, out bool byCom2))
                        {
                            lvi.Checked = byCom2;
                            restored = true;
                        }
                    }

                    if (!restored)
                        lvi.Checked = false;

                    lvi.Tag = srcItem.Tag;
                    listViewMirror.Items.Add(lvi);
                }

                UpdateSelectAllCheckboxState(chkSelectAllMirrors, listViewMirror);
            }
            finally
            {
                listViewMirror.EndUpdate();
                PositionSelectAllCheckboxes();
            }
        }

        // Called when individual probe item is (un)checked by the user
        private void ListViewRotProbe_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendItemCheckedHandler) return;

            string key = BuildRowKeyFromSubItems(listViewRotProbe, e.Item, hasTestColumn: true);
            _probeCheckedStates[key] = e.Item.Checked;

            // also capture ComPort if available
            string com = GetSubItemTextByColumnName(listViewRotProbe, e.Item, "ComPort", hasTestColumn: true);
            if (!string.IsNullOrEmpty(com))
                _probeCheckedByComPort[com] = e.Item.Checked;

            UpdateSelectAllCheckboxState(chkSelectAllProbes, listViewRotProbe);
        }

        // Called when individual mirror item is (un)checked by the user
        private void ListViewMirror_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendItemCheckedHandler) return;

            string key = BuildRowKeyFromSubItems(listViewMirror, e.Item, hasTestColumn: true);
            _mirrorCheckedStates[key] = e.Item.Checked;

            string com = GetSubItemTextByColumnName(listViewMirror, e.Item, "ComPort", hasTestColumn: true);
            if (!string.IsNullOrEmpty(com))
                _mirrorCheckedByComPort[com] = e.Item.Checked;

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
                    string key = BuildRowKeyFromSubItems(listViewRotProbe, item, hasTestColumn: true);
                    _probeCheckedStates[key] = check;

                    string com = GetSubItemTextByColumnName(listViewRotProbe, item, "ComPort", hasTestColumn: true);
                    if (!string.IsNullOrEmpty(com))
                        _probeCheckedByComPort[com] = check;
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
                    string key = BuildRowKeyFromSubItems(listViewMirror, item, hasTestColumn: true);
                    _mirrorCheckedStates[key] = check;

                    string com = GetSubItemTextByColumnName(listViewMirror, item, "ComPort", hasTestColumn: true);
                    if (!string.IsNullOrEmpty(com))
                        _mirrorCheckedByComPort[com] = check;
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
                MessageBox.Show("Please select at least one probe for calibration.", "No Probes Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (mirrorCount ==0)
            {
                MessageBox.Show("Please select one mirror for calibration.", "No Mirror Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (mirrorCount >1)
            {
                MessageBox.Show("Please select only one mirror for calibration.", "Multiple Mirrors Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var calibrationProcess = new CalProgressFrm(selectedProbes, selectedMirror, _steps, checkBoxManual.Checked);
            calibrationProcess.Show();
        }
    }
}
