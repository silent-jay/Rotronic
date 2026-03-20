using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Text;

namespace Rotronic
{
    public partial class CalibrationSetupFrm : Form
    {
        private Main _main;

        // Checkbox state is persisted on the underlying device objects via their `Selected` property.

        // suppression flags to avoid recursion when programmatically changing checks
        private bool _suspendItemCheckedHandler;

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
                _main.ChambersRefreshed += Main_ChambersRefreshed;

                // wire events to keep header checkbox positioned and to track user item checks
                listViewRotProbe.ColumnWidthChanged += ListViewRotProbe_ColumnWidthChanged;
                listViewRotProbe.ItemChecked += ListViewRotProbe_ItemChecked;
                listViewMirror.ColumnWidthChanged += ListViewMirror_ColumnWidthChanged;
                listViewMirror.ItemChecked += ListViewMirror_ItemChecked;

                if (listViewChamber != null)
                {
                    try
                    {
                        // Main's chamber list is OwnerDraw with custom rendering.
                        // In Setup we want standard rendering so ForeColor (InUse) works.
                        listViewChamber.OwnerDraw = false;
                    }
                    catch { }
                    listViewChamber.ItemChecked += ListViewChamber_ItemChecked;
                }

                // initial copy to mirror current state
                CopyProbesFromMain(_main);
                CopyMirrorsFromMain(_main);
                CopyChambersFromMain(_main);
            }
        }

        private void CalibrationSetupFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_main != null)
            {
                _main.ProbesRefreshed -= Main_ProbesRefreshed;
                _main.MirrorsRefreshed -= Main_MirrorsRefreshed;
                _main.ChambersRefreshed -= Main_ChambersRefreshed;
                _main = null;
            }
        }

        private void CalibrationSetupFrm_Resize(object sender, EventArgs e)
        {
            // Select-all header checkboxes were removed from the UI.
        }

        private void Main_ProbesRefreshed(object sender, EventArgs e)
        {
            CopyProbesFromMain(sender as Main);
        }

        private void Main_MirrorsRefreshed(object sender, EventArgs e)
        {
            CopyMirrorsFromMain(sender as Main);
        }

        private void Main_ChambersRefreshed(object sender, EventArgs e)
        {
            CopyChambersFromMain(sender as Main);
        }

        private static string GetChamberIpFromSetupItem(ListViewItem item)
        {
            if (item == null) return string.Empty;
            try
            {
                if (item.Tag is Chamber c && !string.IsNullOrWhiteSpace(c.IPAddress))
                    return c.IPAddress;
            }
            catch { }

            return string.Empty;
        }

        private static string GetChamberIpFromMainItem(ListView sourceListView, ListViewItem item)
        {
            if (sourceListView == null || item == null) return string.Empty;
            try
            {
                if (item.Tag is Chamber c && !string.IsNullOrWhiteSpace(c.IPAddress))
                    return c.IPAddress;
            }
            catch { }

            // Fallback: find by column name
            for (int ci = 0; ci < sourceListView.Columns.Count; ci++)
            {
                if (string.Equals(sourceListView.Columns[ci].Name, "IPAddress", StringComparison.OrdinalIgnoreCase))
                {
                    if (ci >= 0 && ci < item.SubItems.Count)
                        return item.SubItems[ci].Text ?? string.Empty;
                    break;
                }
            }
            return string.Empty;
        }

        private void CopyChambersFromMain(Main main)
        {
            if (main == null || main.listViewChamber == null || listViewChamber == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => CopyChambersFromMain(main)));
                return;
            }

            listViewChamber.BeginUpdate();
            try
            {
                // Persist UI checked state into underlying objects
                foreach (ListViewItem existing in listViewChamber.Items)
                {
                    if (existing.Tag is Chamber c)
                        c.Selected = existing.Checked;
                }

                // Diagnostics: check for empty/duplicate IPs in Main
                try
                {
                    var ips = new List<string>();
                    foreach (ListViewItem src in main.listViewChamber.Items)
                    {
                        var ip = GetChamberIpFromMainItem(main.listViewChamber, src);
                        ips.Add(ip ?? string.Empty);
                    }
                    var emptyCount = ips.Count(s => string.IsNullOrWhiteSpace(s));
                    var dupCount = ips.Where(s => !string.IsNullOrWhiteSpace(s))
                                      .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                                      .Count(g => g.Count() > 1);
                    var sample = string.Join(",", ips.Where(s => !string.IsNullOrWhiteSpace(s)).Take(5));
                    Debug.WriteLine($"[CalibrationSetupFrm] Chamber refresh: total={ips.Count}, empty IPs={emptyCount}, duplicate IP groups={dupCount}, sample=[{sample}]");
                }
                catch { }

                bool columnsMatch = listViewChamber.Columns.Count == main.listViewChamber.Columns.Count + 1;
                if (columnsMatch)
                {
                    for (int i = 0; i < main.listViewChamber.Columns.Count; i++)
                    {
                        var mainCol = main.listViewChamber.Columns[i];
                        var thisCol = listViewChamber.Columns[i + 1];
                        if (!string.Equals(mainCol.Name, thisCol.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            columnsMatch = false;
                            break;
                        }
                    }
                }

                if (columnsMatch)
                {
                    var existingMap = new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);
                    foreach (ListViewItem it in listViewChamber.Items)
                    {
                        string ip = GetSubItemTextByColumnName(listViewChamber, it, "IPAddress", hasTestColumn: true);
                        if (string.IsNullOrWhiteSpace(ip))
                            ip = GetChamberIpFromSetupItem(it);
                        if (!string.IsNullOrWhiteSpace(ip) && !existingMap.ContainsKey(ip))
                            existingMap[ip] = it;
                    }

                    var processed = new HashSet<ListViewItem>();
                    foreach (ListViewItem srcItem in main.listViewChamber.Items)
                    {
                        var chamber = srcItem.Tag as Chamber;
                        string ipKey = GetChamberIpFromMainItem(main.listViewChamber, srcItem);

                        if (!string.IsNullOrWhiteSpace(ipKey) && existingMap.TryGetValue(ipKey, out var target))
                        {
                            for (int si = 0; si < srcItem.SubItems.Count; si++)
                            {
                                int destIndex = si + 1;
                                string txt = srcItem.SubItems[si].Text ?? string.Empty;
                                if (destIndex < target.SubItems.Count)
                                    target.SubItems[destIndex].Text = txt;
                                else
                                    target.SubItems.Add(txt);
                            }

                            target.Tag = chamber ?? srcItem.Tag;
                            if (target.Tag is Chamber tc)
                            {
                                target.ForeColor = tc.InUse ? Color.Red : SystemColors.WindowText;
                                target.Checked = tc.Selected;
                            }

                            processed.Add(target);
                        }
                        else
                        {
                            var lvi = new ListViewItem(string.Empty) { Checked = false };
                            foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                                lvi.SubItems.Add(sub.Text);

                            lvi.Tag = chamber ?? srcItem.Tag;
                            if (lvi.Tag is Chamber nc)
                            {
                                lvi.ForeColor = nc.InUse ? Color.Red : SystemColors.WindowText;
                                lvi.Checked = nc.Selected;
                            }
                            listViewChamber.Items.Add(lvi);
                            processed.Add(lvi);
                        }
                    }

                    var toRemove = listViewChamber.Items.Cast<ListViewItem>().Where(it => !processed.Contains(it)).ToList();
                    foreach (var it in toRemove)
                        listViewChamber.Items.Remove(it);

                    return;
                }

                // Full rebuild
                listViewChamber.Items.Clear();
                listViewChamber.Columns.Clear();

                var testCol = new ColumnHeader { Name = "Test", Text = "Test", Width = 60, TextAlign = HorizontalAlignment.Left };
                listViewChamber.Columns.Add(testCol);

                foreach (ColumnHeader srcCol in main.listViewChamber.Columns)
                {
                    var col = new ColumnHeader
                    {
                        Name = srcCol.Name,
                        Text = srcCol.Text,
                        Width = srcCol.Width,
                        TextAlign = srcCol.TextAlign
                    };
                    listViewChamber.Columns.Add(col);
                }

                listViewChamber.View = View.Details;
                listViewChamber.FullRowSelect = true;
                listViewChamber.CheckBoxes = true;

                foreach (ListViewItem srcItem in main.listViewChamber.Items)
                {
                    var lvi = new ListViewItem(string.Empty) { Checked = false };
                    foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                        lvi.SubItems.Add(sub.Text);

                    lvi.Tag = srcItem.Tag;
                    if (lvi.Tag is Chamber c)
                    {
                        lvi.ForeColor = c.InUse ? Color.Red : SystemColors.WindowText;
                        lvi.Checked = c.Selected;
                    }
                    listViewChamber.Items.Add(lvi);
                }
            }
            finally
            {
                listViewChamber.EndUpdate();
            }
        }

        private void ListViewChamber_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendItemCheckedHandler) return;

            if (e.Item.Tag is Chamber c)
                c.Selected = e.Item.Checked;
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
                // Diagnostics: check for empty/duplicate ComPorts in Main
                try
                {
                    var coms = new List<string>();
                    foreach (ListViewItem src in main.listViewRotProbe.Items)
                    {
                        var rp = src.Tag as RotProbe;
                        var com = rp?.ComPort ?? GetSubItemTextByColumnName(main.listViewRotProbe, src, "ComPort", hasTestColumn: false);
                        coms.Add(com ?? string.Empty);
                    }
                    var emptyCount = coms.Count(s => string.IsNullOrWhiteSpace(s));
                    var dupCount = coms.Where(s => !string.IsNullOrWhiteSpace(s))
                                       .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                                       .Count(g => g.Count() > 1);
                    var sample = string.Join(",", coms.Where(s => !string.IsNullOrWhiteSpace(s)).Take(5));
                    Debug.WriteLine($"[CalibrationSetupFrm] Probe refresh: total={coms.Count}, empty ComPorts={emptyCount}, duplicate ComPort groups={dupCount}, sample=[{sample}]");
                }
                catch { }

                // Persist current UI checked state into the underlying objects before refreshing.
                foreach (ListViewItem existing in listViewRotProbe.Items)
                {
                    if (existing.Tag is RotProbe rp)
                        rp.Selected = existing.Checked;
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
                            if (target.Tag is RotProbe trp)
                            {
                                target.ForeColor = trp.InUse ? Color.Red : SystemColors.WindowText;
                                target.Checked = trp.Selected;
                            }
                            processed.Add(target);
                        }
                        else
                        {
                            // new item -> add preserving checked fallback
                            var lvi = new ListViewItem(string.Empty) { Checked = false };
                            foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                                lvi.SubItems.Add(sub.Text);

                            lvi.Tag = rp ?? srcItem.Tag;
                            if (lvi.Tag is RotProbe lrp)
                            {
                                lvi.ForeColor = lrp.InUse ? Color.Red : SystemColors.WindowText;
                                lvi.Checked = lrp.Selected;
                            }
                            listViewRotProbe.Items.Add(lvi);
                            processed.Add(lvi);
                        }
                    }

                    // Remove any items not processed (they no longer exist in main)
                    var toRemove = listViewRotProbe.Items.Cast<ListViewItem>().Where(it => !processed.Contains(it)).ToList();
                    foreach (var it in toRemove)
                    {
                        // Keep those with no tag/comport? They are stale -> remove
                        listViewRotProbe.Items.Remove(it);
                    }

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

                    lvi.Tag = srcItem.Tag;
                    if (lvi.Tag is RotProbe rp2)
                    {
                        lvi.ForeColor = rp2.InUse ? Color.Red : SystemColors.WindowText;
                        lvi.Checked = rp2.Selected;
                    }
                    listViewRotProbe.Items.Add(lvi);
                }
            }
            finally
            {
                listViewRotProbe.EndUpdate();
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
                // Persist UI checked state into underlying mirrors
                foreach (ListViewItem existing in listViewMirror.Items)
                {
                    if (existing.Tag is Mirror m)
                        m.Selected = existing.Checked;
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
                            if (target.Tag is Mirror tm)
                            {
                                target.ForeColor = tm.InUse ? Color.Red : SystemColors.WindowText;
                                target.Checked = tm.Selected;
                            }
                            processed.Add(target);
                        }
                        else
                        {
                            var lvi = new ListViewItem(string.Empty) { Checked = false };
                            foreach (ListViewItem.ListViewSubItem sub in srcItem.SubItems)
                                lvi.SubItems.Add(sub.Text);
                            lvi.Tag = srcItem.Tag;
                            if (lvi.Tag is Mirror lm)
                            {
                                lvi.ForeColor = lm.InUse ? Color.Red : SystemColors.WindowText;
                                lvi.Checked = lm.Selected;
                            }
                            listViewMirror.Items.Add(lvi);
                            processed.Add(lvi);
                        }
                    }

                    var toRemove = listViewMirror.Items.Cast<ListViewItem>().Where(it => !processed.Contains(it)).ToList();
                    foreach (var it in toRemove)
                        listViewMirror.Items.Remove(it);

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

                    lvi.Tag = srcItem.Tag;
                    if (lvi.Tag is Mirror m2)
                    {
                        lvi.ForeColor = m2.InUse ? Color.Red : SystemColors.WindowText;
                        lvi.Checked = m2.Selected;
                    }
                    listViewMirror.Items.Add(lvi);
                }
            }
            finally
            {
                listViewMirror.EndUpdate();
            }
        }

        // Called when individual probe item is (un)checked by the user
        private void ListViewRotProbe_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendItemCheckedHandler) return;

            if (e.Item.Tag is RotProbe rp)
                rp.Selected = e.Item.Checked;
        }

        // Called when individual mirror item is (un)checked by the user
        private void ListViewMirror_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendItemCheckedHandler) return;

            // Enforce single-selection for mirrors
            if (e.Item.Checked)
            {
                _suspendItemCheckedHandler = true;
                try
                {
                    foreach (ListViewItem item in listViewMirror.Items)
                    {
                        if (!ReferenceEquals(item, e.Item) && item.Checked)
                            item.Checked = false;
                    }
                }
                finally
                {
                    _suspendItemCheckedHandler = false;
                }
            }

            if (e.Item.Tag is Mirror m)
                m.Selected = e.Item.Checked;
        }

        private void ListViewRotProbe_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
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

        public void buttonBegin_Click(object sender, EventArgs e)
        {
            // Collect selected probes (domain objects)
            var selectedProbeItems = listViewRotProbe.Items.Cast<ListViewItem>().Where(i => i.Checked).ToList();
            var selectedProbes = selectedProbeItems
                .Select(i => i?.Tag as RotProbe)
                .Where(p => p != null)
                .ToList();

            // Collect selected mirror (domain object, exactly one required)
            var selectedMirrorItem = listViewMirror.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Checked);
            var mirrorObj = selectedMirrorItem?.Tag as Mirror;
            int mirrorCount = listViewMirror.Items.Cast<ListViewItem>().Count(i => i.Checked);

            // Collect selected chamber (domain object, exactly one required)
            ListViewItem selectedChamberItem = null;
            Chamber chamberObj = null;
            int chamberCount =0;
            if (listViewChamber != null)
            {
                selectedChamberItem = listViewChamber.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Checked);
                chamberObj = selectedChamberItem?.Tag as Chamber;
                chamberCount = listViewChamber.Items.Cast<ListViewItem>().Count(i => i.Checked);
            }

            // Validate selections
            if (selectedProbes.Count ==0)
            {
                MessageBox.Show("Please select at least one probe for calibration.", "No Probes Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (mirrorCount ==0 || mirrorObj == null)
            {
                MessageBox.Show("Please select one mirror for calibration.", "No Mirror Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (mirrorCount >1)
            {
                MessageBox.Show("Please select only one mirror for calibration.", "Multiple Mirrors Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (listViewChamber != null)
            {
                if (chamberCount ==0 || chamberObj == null)
                {
                    MessageBox.Show("Please select one chamber for calibration.", "No Chamber Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (chamberCount >1)
                {
                    MessageBox.Show("Please select only one chamber for calibration.", "Multiple Chambers Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Block starting a calibration with items already in use
            var inUseProbes = selectedProbes.Where(p => p.InUse).ToList();
            if (inUseProbes.Count >0)
            {
                var probeNames = string.Join(", ", inUseProbes.Select(p => !string.IsNullOrWhiteSpace(p.DeviceName) ? p.DeviceName :
 !string.IsNullOrWhiteSpace(p.SerialNumber) ? p.SerialNumber :
 !string.IsNullOrWhiteSpace(p.ComPort) ? p.ComPort :
 p.ProbeType));
                MessageBox.Show($"One or more selected probes are already in use: {probeNames}", "Probe Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (chamberObj != null && chamberObj.InUse)
            {
                var chamberName = !string.IsNullOrWhiteSpace(chamberObj.IPAddress) ? chamberObj.IPAddress :
 !string.IsNullOrWhiteSpace(chamberObj.Name) ? chamberObj.Name :
 selectedChamberItem?.Text;
                MessageBox.Show($"The selected chamber is already in use: {chamberName}", "Chamber Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (mirrorObj.InUse)
            {
                var mirrorName = !string.IsNullOrWhiteSpace(mirrorObj.SerialNumber) ? mirrorObj.SerialNumber :
 !string.IsNullOrWhiteSpace(mirrorObj.ComPort) ? mirrorObj.ComPort :
 !string.IsNullOrWhiteSpace(mirrorObj.ID) ? mirrorObj.ID :
 selectedMirrorItem?.Text;
                MessageBox.Show($"The selected mirror is already in use: {mirrorName}", "Mirror Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Mark selected items as in-use until the calibration form closes
            foreach (var p in selectedProbes)
                p.InUse = true;
            mirrorObj.InUse = true;
            if (chamberObj != null)
                chamberObj.InUse = true;

            var calibrationProcess = new CalProgressFrm(selectedProbes, mirrorObj, chamberObj, _steps, checkBoxManual.Checked);
            calibrationProcess.FormClosed += (s, args) =>
            {
                foreach (var p in selectedProbes)
                    p.InUse = false;

                mirrorObj.InUse = false;

                if (chamberObj != null)
                    chamberObj.InUse = false;

                try
                {
                    if (listViewChamber != null)
                        CopyChambersFromMain(_main);
                }
                catch { }
            };
            calibrationProcess.Show();
        }
    }
}
