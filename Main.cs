using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{
    public partial class Main : Form
    {
        private static bool IsCountFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            var f = fieldName.Trim();
            return f.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsTempOrHumidityFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            var f = fieldName.Trim();

            // Temperature/humidity fields only; exclude counts.
            if (IsCountFieldName(f))
                return false;

            return f.IndexOf("Temp", StringComparison.OrdinalIgnoreCase) >= 0
                || f.IndexOf("Temperature", StringComparison.OrdinalIgnoreCase) >= 0
                || f.IndexOf("Hum", StringComparison.OrdinalIgnoreCase) >= 0
                || f.IndexOf("Humidity", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatTempHumidityValueIfNeeded(string fieldName, object value)
        {
            if (value == null)
                return string.Empty;

            // If we know the field is Temp/Humidity (non-count), force two decimals.
            if (IsTempOrHumidityFieldName(fieldName))
            {
                try
                {
                    // Handle numeric and numeric-like strings.
                    var d = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    return d.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    // Fallback to raw text
                }
            }

            return value.ToString();
        }
        private readonly HashSet<string> _promptedNewMirrors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _promptedNewChambers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Timer to refresh listViewRotProbe periodically
        private System.Windows.Forms.Timer _refreshTimer;

        // Events raised after the lists are refreshed so other forms can mirror them
        public event EventHandler ProbesRefreshed;
        public event EventHandler MirrorsRefreshed;

        public event EventHandler ChambersRefreshed;

        // Last-known summary state to avoid unnecessary UI rebuilds
        private List<string> _lastProbeKeys;
        private List<string> _lastProbeColumnNames;
        private List<string> _lastMirrorKeys;
        private List<string> _lastMirrorColumnNames;

        private List<string> _lastChamberKeys;
        private List<string> _lastChamberColumnNames;

        private int _colIdxChamberTempControl = -1;
        private int _colIdxChamberHumControl = -1;
        private int _colIdxChamberTempSp = -1;
        private int _colIdxChamberHumSp = -1;
        private int _colIdxChamberTempStable = -1;
        private int _colIdxChamberHumStable = -1;
        private int _colIdxChamberExtRefStable = -1;
        private bool _chamberListViewClickWired;

        private TextBox _chamberEditBox;
        private ListViewItem _chamberEditItem;
        private int _chamberEditSubIndex = -1;
        private Rectangle _chamberEditBounds;

        private readonly Dictionary<string, ChamberControllerFrm> _openChamberControllers =
 new Dictionary<string, ChamberControllerFrm>(StringComparer.OrdinalIgnoreCase);

        public Main()
        {
            InitializeComponent();
            // Initialize timer
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 2_000; // 2 seconds
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Attach handlers to save column widths when user resizes columns
            try
            {
                this.listViewRotProbe.ColumnWidthChanged -= ListViewRotProbe_ColumnWidthChanged;
                this.listViewRotProbe.ColumnWidthChanged += ListViewRotProbe_ColumnWidthChanged;
            }
            catch { }
            try
            {
                this.listViewMirror.ColumnWidthChanged -= ListViewMirror_ColumnWidthChanged;
                this.listViewMirror.ColumnWidthChanged += ListViewMirror_ColumnWidthChanged;
            }
            catch { }

            // Attach handlers to allow copying (Ctrl+C) and context-menu copy
            try
            {
                this.listViewRotProbe.KeyDown -= ListViewRotProbe_KeyDown;
                this.listViewRotProbe.KeyDown += ListViewRotProbe_KeyDown;
            }
            catch { }

            try
            {
                // Create a simple context menu with Copy and Copy All
                var probeContextMenu = new ContextMenuStrip();
                var copyItem = new ToolStripMenuItem("Copy\tCtrl+C");
                copyItem.Click += CopySelectedMenu_Click;
                var copyAllItem = new ToolStripMenuItem("Copy All");
                copyAllItem.Click += CopyAllMenu_Click;
                probeContextMenu.Items.Add(copyItem);
                probeContextMenu.Items.Add(copyAllItem);
                this.listViewRotProbe.ContextMenuStrip = probeContextMenu;
            }
            catch { }

            // Initial population
            RefreshConnectedProbes();
            RefreshConnectedMirrors();

            WireChamberListViewControls();

        }

        private void WireChamberListViewControls()
        {
            if (listViewChamber == null || _chamberListViewClickWired)
                return;

            _chamberListViewClickWired = true;

            listViewChamber.OwnerDraw = true;
            listViewChamber.DrawColumnHeader -= ListViewChamber_DrawColumnHeader;
            listViewChamber.DrawColumnHeader += ListViewChamber_DrawColumnHeader;
            listViewChamber.DrawSubItem -= ListViewChamber_DrawSubItem;
            listViewChamber.DrawSubItem += ListViewChamber_DrawSubItem;
            listViewChamber.MouseUp -= ListViewChamber_MouseUp;
            listViewChamber.MouseUp += ListViewChamber_MouseUp;
            listViewChamber.Scrollable = true;
            listViewChamber.KeyDown -= ListViewChamber_KeyDown;
            listViewChamber.KeyDown += ListViewChamber_KeyDown;

            listViewChamber.DoubleClick -= ListViewChamber_DoubleClick;
            listViewChamber.DoubleClick += ListViewChamber_DoubleClick;
        }

        private void UpdateChamberControlColumnIndexes()
        {
            _colIdxChamberTempControl = -1;
            _colIdxChamberHumControl = -1;
            _colIdxChamberTempSp = -1;
            _colIdxChamberHumSp = -1;
            _colIdxChamberTempStable = -1;
            _colIdxChamberHumStable = -1;
            _colIdxChamberExtRefStable = -1;

            if (listViewChamber == null || listViewChamber.Columns == null)
                return;

            for (int i = 0; i < listViewChamber.Columns.Count; i++)
            {
                var name = listViewChamber.Columns[i]?.Name ?? string.Empty;
                if (_colIdxChamberTempControl < 0 && string.Equals(name, ChamberDisplayOptions.Field.TempControl.ToString(), StringComparison.OrdinalIgnoreCase))
                    _colIdxChamberTempControl = i;
                if (_colIdxChamberHumControl < 0 && string.Equals(name, ChamberDisplayOptions.Field.HumControl.ToString(), StringComparison.OrdinalIgnoreCase))
                    _colIdxChamberHumControl = i;
                if (_colIdxChamberTempSp < 0 && string.Equals(name, ChamberDisplayOptions.Field.TemperatureSP.ToString(), StringComparison.OrdinalIgnoreCase))
                    _colIdxChamberTempSp = i;
                if (_colIdxChamberHumSp < 0 && string.Equals(name, ChamberDisplayOptions.Field.HumiditySP.ToString(), StringComparison.OrdinalIgnoreCase))
                    _colIdxChamberHumSp = i;
                if (_colIdxChamberTempStable < 0 && string.Equals(name, ChamberDisplayOptions.Field.TempStable.ToString(), StringComparison.OrdinalIgnoreCase))
                    _colIdxChamberTempStable = i;
                if (_colIdxChamberHumStable < 0 && string.Equals(name, ChamberDisplayOptions.Field.HumStable.ToString(), StringComparison.OrdinalIgnoreCase))
                    _colIdxChamberHumStable = i;
                if (_colIdxChamberExtRefStable < 0 && string.Equals(name, ChamberDisplayOptions.Field.ExtRefStable.ToString(), StringComparison.OrdinalIgnoreCase))
                    _colIdxChamberExtRefStable = i;
            }
        }

        private static bool TryParseBoolish(string text, out bool value)
        {
            value = false;
            if (text == null) return false;
            var t = text.Trim();
            if (t.Length == 0) return false;

            if (string.Equals(t, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "on", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            if (string.Equals(t, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "off", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return bool.TryParse(t, out value);
        }

        private static Rectangle GetCheckboxBounds(Rectangle cellBounds)
        {
            var cb = System.Windows.Forms.CheckBoxRenderer.GetGlyphSize(System.Drawing.Graphics.FromHwnd(IntPtr.Zero), System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
            int x = cellBounds.Left + 6;
            int y = cellBounds.Top + (cellBounds.Height - cb.Height) / 2;
            return new Rectangle(x, y, cb.Width, cb.Height);
        }

        private bool IsChamberControlColumn(int colIndex)
        {
            return colIndex >= 0 && (colIndex == _colIdxChamberTempControl || colIndex == _colIdxChamberHumControl);
        }

        private bool IsChamberEditableSetpointColumn(int colIndex)
        {
            return colIndex >= 0 && (colIndex == _colIdxChamberTempSp || colIndex == _colIdxChamberHumSp);
        }

        private bool IsChamberStableColumn(int colIndex)
        {
            return colIndex >= 0 && (colIndex == _colIdxChamberTempStable || colIndex == _colIdxChamberHumStable || colIndex == _colIdxChamberExtRefStable);
        }

        private void EndChamberCellEdit(bool commit)
        {
            if (_chamberEditBox == null)
                return;

            var editBox = _chamberEditBox;
            var item = _chamberEditItem;
            var subIndex = _chamberEditSubIndex;
            var bounds = _chamberEditBounds;

            _chamberEditBox = null;
            _chamberEditItem = null;
            _chamberEditSubIndex = -1;
            _chamberEditBounds = Rectangle.Empty;

            try { listViewChamber.Controls.Remove(editBox); } catch { }
            try { editBox.Hide(); } catch { }
            try { editBox.Dispose(); } catch { }

            if (!commit)
            {
                try { listViewChamber.Invalidate(bounds); } catch { }
                return;
            }

            if (item == null || subIndex < 0 || subIndex >= item.SubItems.Count)
                return;

            var chamber = item.Tag as Chamber;
            if (chamber == null)
                return;

            var text = (editBox.Text ?? string.Empty).Trim();
            if (!double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                if (!double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out value))
                    return;
            }

            bool ok = false;
            if (subIndex == _colIdxChamberTempSp)
                ok = ChamberCommands.SetTempSP(chamber, value);
            else if (subIndex == _colIdxChamberHumSp)
                ok = ChamberCommands.SetRHSP(chamber, value);

            if (ok)
            {
                item.SubItems[subIndex].Text = value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                try { listViewChamber.Invalidate(bounds); } catch { }
            }
        }

        private void BeginChamberCellEdit(ListViewItem item, ListViewItem.ListViewSubItem subItem, int subIndex)
        {
            if (listViewChamber == null || item == null || subItem == null)
                return;
            if (!IsChamberEditableSetpointColumn(subIndex))
                return;

            EndChamberCellEdit(commit: false);

            _chamberEditItem = item;
            _chamberEditSubIndex = subIndex;
            _chamberEditBounds = subItem.Bounds;

            var tb = new TextBox();
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Text = subItem.Text;
            tb.Bounds = new Rectangle(subItem.Bounds.Left + 2, subItem.Bounds.Top + 2, Math.Max(20, subItem.Bounds.Width - 4), Math.Max(18, subItem.Bounds.Height - 4));
            tb.Visible = true;

            tb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    EndChamberCellEdit(commit: true);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    EndChamberCellEdit(commit: false);
                }
            };
            tb.LostFocus += (s, e) => EndChamberCellEdit(commit: true);

            _chamberEditBox = tb;
            listViewChamber.Controls.Add(tb);
            tb.BringToFront();
            tb.Focus();
            tb.SelectAll();
        }

        private void ListViewChamber_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void ListViewChamber_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (!IsChamberControlColumn(e.ColumnIndex))
            {
                if (!IsChamberStableColumn(e.ColumnIndex))
                {
                    e.DrawDefault = true;
                    return;
                }
            }

            bool value = false;
            TryParseBoolish(e.SubItem?.Text, out value);

            var selected = (e.ItemState & ListViewItemStates.Selected) != 0;
            e.Graphics.FillRectangle(selected ? SystemBrushes.Highlight : SystemBrushes.Window, e.Bounds);

            if (IsChamberControlColumn(e.ColumnIndex))
            {
                var state = value ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal;
                var cbBounds = GetCheckboxBounds(e.Bounds);
                System.Windows.Forms.CheckBoxRenderer.DrawCheckBox(e.Graphics, cbBounds.Location, state);
            }
            else
            {
                // Stable indicator: green circle for true, red circle for false
                var diameter = Math.Min(e.Bounds.Height - 6, 14);
                if (diameter < 6) diameter = Math.Min(e.Bounds.Height - 2, e.Bounds.Width - 2);
                if (diameter < 4)
                {
                    e.DrawDefault = true;
                    return;
                }

                int x = e.Bounds.Left + 6;
                int y = e.Bounds.Top + (e.Bounds.Height - diameter) / 2;
                var circle = new Rectangle(x, y, diameter, diameter);
                using (var brush = new SolidBrush(value ? Color.LimeGreen : Color.Red))
                    e.Graphics.FillEllipse(brush, circle);
                using (var pen = new Pen(Color.DimGray))
                    e.Graphics.DrawEllipse(pen, circle);
            }

            if (selected)
            {
                using (var p = new Pen(SystemColors.Highlight))
                    e.Graphics.DrawRectangle(p, new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 1, e.Bounds.Height - 1));
            }
        }

        private void ListViewChamber_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (listViewChamber == null)
                return;

            var hit = listViewChamber.HitTest(e.Location);
            if (hit == null || hit.Item == null || hit.SubItem == null)
                return;

            int subIndex = hit.Item.SubItems.IndexOf(hit.SubItem);

            if (IsChamberEditableSetpointColumn(subIndex))
            {
                BeginChamberCellEdit(hit.Item, hit.SubItem, subIndex);
                return;
            }

            if (!IsChamberControlColumn(subIndex))
                return;

            var cellBounds = hit.SubItem.Bounds;
            var cbBounds = GetCheckboxBounds(cellBounds);
            if (!cbBounds.Contains(e.Location))
                return;

            bool current = false;
            TryParseBoolish(hit.SubItem.Text, out current);
            bool next = !current;

            var chamber = hit.Item.Tag as Chamber;
            if (chamber == null)
                return;

            bool ok = false;
            if (subIndex == _colIdxChamberTempControl)
                ok = ChamberCommands.SetTempControl(chamber, next);
            else if (subIndex == _colIdxChamberHumControl)
                ok = ChamberCommands.SetRHControl(chamber, next);

            if (ok)
            {
                hit.SubItem.Text = next ? "True" : "False";
                listViewChamber.Invalidate(cellBounds);
            }
        }

        private void ListViewChamber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && _chamberEditBox != null)
            {
                e.Handled = true;
                EndChamberCellEdit(commit: false);
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                OpenSelectedChamberController();
                return;
            }
        }

        private void ListViewChamber_DoubleClick(object sender, EventArgs e)
        {
            OpenSelectedChamberController();
        }

        private void OpenSelectedChamberController()
        {
            if (listViewChamber == null)
                return;

            // Don't open when inline-editing a setpoint cell.
            if (_chamberEditBox != null)
                return;

            if (listViewChamber.SelectedItems == null || listViewChamber.SelectedItems.Count ==0)
                return;

            var item = listViewChamber.SelectedItems[0];
            var chamber = item?.Tag as Chamber;
            if (chamber == null)
                return;

            var key = chamber.IPAddress ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
                key = chamber.Name ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(key)
                && _openChamberControllers.TryGetValue(key, out var existing)
                && existing != null
                && !existing.IsDisposed)
            {
                try
                {
                    if (existing.WindowState == FormWindowState.Minimized)
                        existing.WindowState = FormWindowState.Normal;
                    existing.Activate();
                    existing.BringToFront();
                }
                catch { }
                return;
            }

            var controller = new ChamberControllerFrm(chamber);

            if (!string.IsNullOrWhiteSpace(key))
            {
                _openChamberControllers[key] = controller;
                controller.FormClosed += (s, e) =>
                {
                    try { _openChamberControllers.Remove(key); } catch { }
                };
            }

            controller.Show(this);
            controller.BringToFront();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshConnectedProbes();
            RefreshConnectedMirrors();
            RefreshConnectedChambers();
        }

        private void OnChambersRefreshed(EventArgs e)
        {
            try { ChambersRefreshed?.Invoke(this, e); } catch { }
        }

        private void RefreshConnectedChambers()
        {
            if (listViewChamber == null)
                return;

            try
            {
                WireChamberListViewControls();
                EndChamberCellEdit(commit: true);
                listViewChamber.BeginUpdate();

                var chambers = Program.GetConnectedChambersSnapshot();

                if (chambers == null)
                {
                    if (listViewChamber.Items.Count > 0 || listViewChamber.Columns.Count > 0)
                    {
                        listViewChamber.Items.Clear();
                        listViewChamber.Columns.Clear();
                        _lastChamberKeys = null;
                        _lastChamberColumnNames = null;
                        OnChambersRefreshed(EventArgs.Empty);
                    }
                    return;
                }

                var displayOptions = ChamberDisplayOptions.Load() ?? new ChamberDisplayOptions();
                var visibleFields = displayOptions.GetVisibleOrdered().ToList();
                if (visibleFields.Count == 0)
                {
                    if (listViewChamber.Items.Count > 0 || listViewChamber.Columns.Count > 0)
                    {
                        listViewChamber.Items.Clear();
                        listViewChamber.Columns.Clear();
                        _lastChamberKeys = null;
                        _lastChamberColumnNames = null;
                        OnChambersRefreshed(EventArgs.Empty);
                    }
                    return;
                }

                var currentColumnNames = visibleFields.Select(kv => kv.Key.ToString()).ToList();
                var currentKeys = chambers.Select(c => (c?.IPAddress ?? string.Empty)).OrderBy(s => s).ToList();

                bool columnsChanged = _lastChamberColumnNames == null || !_lastChamberColumnNames.SequenceEqual(currentColumnNames);
                bool itemsChanged = _lastChamberKeys == null || !_lastChamberKeys.SequenceEqual(currentKeys);

                if (!columnsChanged && !itemsChanged && listViewChamber.Items.Count > 0 && listViewChamber.Columns.Count > 0)
                {
                    UpdateChamberItemValuesInPlace(chambers, visibleFields);
                    listViewChamber.EndUpdate();
                    OnChambersRefreshed(EventArgs.Empty);
                    return;
                }

                listViewChamber.Items.Clear();
                listViewChamber.Columns.Clear();

                listViewChamber.View = View.Details;
                listViewChamber.FullRowSelect = true;

                foreach (var kv in visibleFields)
                {
                    var field = kv.Key;
                    var option = kv.Value;
                    var headerText = option.HeaderText ?? field.ToString();

                    var column = new ColumnHeader
                    {
                        Name = field.ToString(),
                        Text = headerText,
                        Width = (option != null && option.Width > 0) ? option.Width : 120,
                        TextAlign = HorizontalAlignment.Left
                    };

                    listViewChamber.Columns.Add(column);
                }

                UpdateChamberControlColumnIndexes();

                var elementType = typeof(Chamber);
                var props = elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                var propCache = new Dictionary<object, PropertyInfo>();
                foreach (var kv in visibleFields)
                {
                    var fieldName = kv.Key.ToString();
                    var p = elementType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (p == null)
                    {
                        string normalizedField = new string(fieldName.Where(c => c != '_' && c != ' ').ToArray());
                        p = props.FirstOrDefault(c => string.Equals(new string(c.Name.Where(ch => ch != '_' && ch != ' ').ToArray()), normalizedField, StringComparison.OrdinalIgnoreCase));
                    }
                    propCache[kv.Key] = p;
                }

                foreach (var c in chambers)
                {
                    if (c == null)
                        continue;

                    TryAutoAddStandardAndPrompt(c);

                    var values = new List<string>(visibleFields.Count);
                    foreach (var kv in visibleFields)
                    {
                        var prop = propCache[kv.Key];
                        if (prop != null)
                        {
                            try { var v = prop.GetValue(c); values.Add(v?.ToString() ?? string.Empty); } catch { values.Add(string.Empty); }
                        }
                        else
                        {
                            values.Add(string.Empty);
                        }
                    }

                    var lvi = new ListViewItem(values.Count > 0 ? values[0] : string.Empty);
                    for (int i = 1; i < values.Count; i++)
                        lvi.SubItems.Add(values[i]);

                    lvi.Tag = c;
                    lvi.ForeColor = c.InUse ? Color.Red : SystemColors.WindowText;
                    listViewChamber.Items.Add(lvi);
                }

                const int MinColumnWidth = 40;
                for (int i = 0; i < listViewChamber.Columns.Count; i++)
                {
                    var col = listViewChamber.Columns[i];
                    if (col.Width < MinColumnWidth)
                        col.Width = 120;
                }

                _lastChamberColumnNames = currentColumnNames;
                _lastChamberKeys = currentKeys;
                OnChambersRefreshed(EventArgs.Empty);
            }
            finally
            {
                listViewChamber.EndUpdate();
            }
        }

        private void TryAutoAddStandardAndPrompt(Chamber chamber)
        {
            if (chamber == null)
                return;

            var controllerSerial = (chamber.ControllerSerial ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(controllerSerial))
                return;

            if (_promptedNewChambers.Contains(controllerSerial))
                return;

            try
            {
                Data.InitializeDatabase();
                if (Data.ChamberExists(controllerSerial))
                {
                    _promptedNewChambers.Add(controllerSerial);
                    return;
                }

                Data.UpsertChamber(controllerSerial, chamber.HC2Serial, chamber.Name);
                _promptedNewChambers.Add(controllerSerial);

                MessageBox.Show(this, "This is the first time connecting this Standard. Please complete the setup.", "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var frm = new StandardInventoryFrm();
                frm.Shown += (s, e) =>
                {
                    try
                    {
                        frm.SelectStandardInComboBox("Chamber", chamber.Name, chamber.HC2Serial);
                    }
                    catch { }
                };
                frm.Show(this);
                try { frm.BringToFront(); } catch { }
            }
            catch
            {
                // ignore auto-add failures
            }
        }

        private void UpdateChamberItemValuesInPlace(List<Chamber> chambers, List<KeyValuePair<ChamberDisplayOptions.Field, ChamberDisplayOptions.ColumnOption>> visibleFields)
        {
            if (chambers == null || visibleFields == null) return;

            var map = chambers
                .Where(c => c != null)
                .GroupBy(c => (c.IPAddress ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var elementType = typeof(Chamber);
            var props = elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var propCache = new Dictionary<object, PropertyInfo>();
            foreach (var kv in visibleFields)
            {
                var fieldName = kv.Key.ToString();
                var p = elementType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null)
                {
                    string normalizedField = new string(fieldName.Where(c => c != '_' && c != ' ').ToArray());
                    p = props.FirstOrDefault(c => string.Equals(new string(c.Name.Where(ch => ch != '_' && ch != ' ').ToArray()), normalizedField, StringComparison.OrdinalIgnoreCase));
                }
                propCache[kv.Key] = p;
            }

            for (int i = listViewChamber.Items.Count - 1; i >= 0; i--)
            {
                var item = listViewChamber.Items[i];
                string ip = string.Empty;
                Chamber underlying = null;
                try { underlying = item.Tag as Chamber; } catch { underlying = null; }
                if (underlying != null && !string.IsNullOrEmpty(underlying.IPAddress))
                    ip = underlying.IPAddress;

                if (string.IsNullOrEmpty(ip))
                {
                    for (int ci = 0; ci < listViewChamber.Columns.Count; ci++)
                    {
                        if (string.Equals(listViewChamber.Columns[ci].Name, "IPAddress", StringComparison.OrdinalIgnoreCase))
                        {
                            int subIndex = ci;
                            if (subIndex >= 0 && subIndex < item.SubItems.Count)
                                ip = item.SubItems[subIndex].Text;
                            break;
                        }
                    }
                }

                Chamber cNew = null;
                if (!string.IsNullOrEmpty(ip) && map.TryGetValue(ip, out cNew))
                {
                    for (int colIdx = 0; colIdx < visibleFields.Count; colIdx++)
                    {
                        var kv = visibleFields[colIdx];
                        var prop = propCache[kv.Key];
                        string txt = string.Empty;
                        if (prop != null)
                        {
                            try { var v = prop.GetValue(cNew); txt = v == null ? string.Empty : FormatTempHumidityValueIfNeeded(kv.Key.ToString(), v); } catch { txt = string.Empty; }
                        }

                        if (colIdx < item.SubItems.Count)
                            item.SubItems[colIdx].Text = txt;
                        else
                            item.SubItems.Add(txt);
                    }

                    item.Tag = cNew;
                    item.ForeColor = cNew.InUse ? Color.Red : SystemColors.WindowText;
                }
                else
                {
                    // For fake chambers used in UX/UI testing, they may not appear in the monitored snapshot.
                    // Keep them visible rather than removing the row.
                    var existing = item.Tag as Chamber;
                    if (existing != null && string.Equals(existing.Name ?? string.Empty, "Fake Chamber", StringComparison.OrdinalIgnoreCase))
                    {
                        item.ForeColor = existing.InUse ? Color.Red : SystemColors.WindowText;
                    }
                    else
                    {
                        listViewChamber.Items.RemoveAt(i);
                    }
                }
            }

            var existingIps = listViewChamber.Items.Cast<ListViewItem>().Select(it => (it.Tag as Chamber)?.IPAddress ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var c in chambers)
            {
                var ipKey = c?.IPAddress ?? string.Empty;
                if (!existingIps.Contains(ipKey))
                {
                    var values = new List<string>(visibleFields.Count);
                    foreach (var kv in visibleFields)
                    {
                        var prop = propCache[kv.Key];
                        try { var v = prop?.GetValue(c); values.Add(v == null ? string.Empty : FormatTempHumidityValueIfNeeded(kv.Key.ToString(), v)); } catch { values.Add(string.Empty); }
                    }
                    var lvi = new ListViewItem(values.Count > 0 ? values[0] : string.Empty);
                    for (int i = 1; i < values.Count; i++) lvi.SubItems.Add(values[i]);
                    lvi.Tag = c;
                    lvi.ForeColor = c.InUse ? Color.Red : SystemColors.WindowText;
                    listViewChamber.Items.Add(lvi);
                }
            }
        }



        private void RefreshConnectedMirrors()
        {
            if (listViewMirror == null)
                return;

            try
            {
                listViewMirror.BeginUpdate();

                // Take a thread-safe snapshot of mirrors before touching the UI to avoid races and flicker
                var mirrors = Program.GetConnectedMirrorsSnapshot();

                // First-time detection should not depend on ListView rebuild logic.
                try
                {
                    if (mirrors != null)
                    {
                        foreach (var m in mirrors)
                            TryAutoAddStandardAndPrompt(m);
                    }
                }
                catch { }

                if (mirrors == null)
                {
                    // Only clear columns/items if they exist
                    if (listViewMirror.Items.Count > 0 || listViewMirror.Columns.Count > 0)
                    {
                        listViewMirror.Items.Clear();
                        listViewMirror.Columns.Clear();
                        _lastMirrorKeys = null;
                        _lastMirrorColumnNames = null;
                        OnMirrorsRefreshed(EventArgs.Empty);
                    }
                    return;
                }

                // Load display options
                var displayOptions = MirrorDisplayOptions.Load() ?? new MirrorDisplayOptions();
                var visibleFields = displayOptions.GetVisibleOrdered().ToList();
                if (visibleFields.Count == 0)
                {
                    if (listViewMirror.Items.Count > 0 || listViewMirror.Columns.Count > 0)
                    {
                        listViewMirror.Items.Clear();
                        listViewMirror.Columns.Clear();
                        _lastMirrorKeys = null;
                        _lastMirrorColumnNames = null;
                        OnMirrorsRefreshed(EventArgs.Empty);
                    }
                    return;
                }

                var currentColumnNames = visibleFields.Select(kv => kv.Key.ToString()).ToList();
                var currentKeys = mirrors.Select(m => (m?.ComPort ?? string.Empty)).OrderBy(s => s).ToList();

                bool columnsChanged = _lastMirrorColumnNames == null || !_lastMirrorColumnNames.SequenceEqual(currentColumnNames);
                bool itemsChanged = _lastMirrorKeys == null || !_lastMirrorKeys.SequenceEqual(currentKeys);

                if (!columnsChanged && !itemsChanged && listViewMirror.Items.Count > 0 && listViewMirror.Columns.Count > 0)
                {
                    // Only values changed: update subitems in-place to avoid flicker and avoid raising event
                    UpdateMirrorItemValuesInPlace(mirrors, visibleFields);
                    listViewMirror.EndUpdate();
                    OnMirrorsRefreshed(EventArgs.Empty);
                    return;
                }

                // Otherwise rebuild columns/items
                listViewMirror.Items.Clear();
                listViewMirror.Columns.Clear();

                // Determine element type
                Type elementType = null;
                var mirrorsType = mirrors.GetType();
                var enumInterface = mirrorsType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumInterface != null)
                {
                    elementType = enumInterface.GetGenericArguments()[0];
                }

                object firstElement = null;
                var enumer = mirrors as System.Collections.IEnumerable;
                if (enumer != null)
                {
                    foreach (var obj in enumer)
                    {
                        firstElement = obj;
                        break;
                    }
                }

                if (elementType == null && firstElement != null)
                    elementType = firstElement.GetType();

                listViewMirror.View = View.Details;
                listViewMirror.FullRowSelect = true;

                // Create columns according to display options
                foreach (var kv in visibleFields)
                {
                    var field = kv.Key;
                    var option = kv.Value;
                    var headerText = option?.HeaderText ?? field.ToString();

                    var column = new ColumnHeader
                    {
                        Name = field.ToString(),
                        Text = headerText,
                        Width = (option != null && option.Width > 0) ? option.Width : 120,
                        TextAlign = HorizontalAlignment.Left
                    };

                    listViewMirror.Columns.Add(column);
                }

                if (elementType == null)
                {
                    // No type info; just list ToString() values
                    if (enumer != null)
                    {
                        foreach (var itemObj in enumer)
                        {
                            var text = itemObj?.ToString() ?? string.Empty;
                            var lvi = new ListViewItem(text);
                            lvi.Tag = itemObj;
                            listViewMirror.Items.Add(lvi);
                        }
                    }
                }
                else
                {
                    // Cache properties of elementType
                    var propCache = new Dictionary<object, PropertyInfo>();
                    var props = elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                    Func<string, PropertyInfo> findProp = (fieldName) =>
                    {
                        if (string.IsNullOrWhiteSpace(fieldName))
                            return null;

                        // exact match (case-insensitive)
                        var p = elementType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (p != null) return p;

                        // normalize: remove underscores and spaces
                        string normalizedField = new string(fieldName.Where(c => c != '_' && c != ' ').ToArray());
                        foreach (var candidate in props)
                        {
                            var normalizedCandidate = new string(candidate.Name.Where(c => c != '_' && c != ' ').ToArray());
                            if (string.Equals(normalizedCandidate, normalizedField, StringComparison.OrdinalIgnoreCase))
                                return candidate;
                        }

                        // contains match
                        foreach (var candidate in props)
                        {
                            if (candidate.Name.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0)
                                return candidate;
                        }

                        return null;
                    };

                    // Prepopulate cache using the visible field keys (unknown enum type, treat key as object)
                    foreach (var kv in visibleFields)
                    {
                        var fieldObj = kv.Key;
                        var fieldName = fieldObj.ToString();
                        PropertyInfo matched = findProp(fieldName);

                        if (matched == null)
                        {
                            switch (fieldName.ToLowerInvariant())
                            {
                                case "comport":
                                case "com_port":
                                    matched = findProp("Port") ?? findProp("ComPort");
                                    break;
                                case "serialnumber":
                                    matched = findProp("Serial") ?? findProp("SerialNo") ?? findProp("SerialNumber");
                                    break;
                                case "devicename":
                                    matched = findProp("Name") ?? findProp("DeviceName");
                                    break;
                                case "firmwareversion":
                                    matched = findProp("Firmware") ?? findProp("FirmwareVersion") ?? findProp("FwVersion");
                                    break;
                                case "devicemodel":
                                    matched = findProp("Model") ?? findProp("DeviceModel");
                                    break;
                                // add additional heuristics as required
                            }
                        }

                        // Handle "Humidity" vs "Humdity" misspelling
                        if (matched == null)
                        {
                            try
                            {
                                if (fieldName.IndexOf("Humidity", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var alt = fieldName.Replace("Humidity", "Humdity");
                                    matched = findProp(alt);
                                }
                                else if (fieldName.IndexOf("Humdity", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var alt = fieldName.Replace("Humdity", "Humidity");
                                    matched = findProp(alt);
                                }
                            }
                            catch
                            {
                                // ignore replacement issues
                            }
                        }

                        propCache[fieldObj] = matched;
                    }

                    // Populate rows
                    if (enumer != null)
                    {
                        foreach (var itemObj in enumer)
                        {
                            if (itemObj == null)
                                continue;

                            var values = new List<string>(visibleFields.Count);
                            foreach (var kv in visibleFields)
                            {
                                var fieldObj = kv.Key;
                                var prop = propCache.ContainsKey(fieldObj) ? propCache[fieldObj] : null;
                                if (prop != null)
                                {
                                    try
                                    {
                                        var v = prop.GetValue(itemObj);
                                        values.Add(v == null ? string.Empty : FormatTempHumidityValueIfNeeded(fieldObj.ToString(), v));
                                    }
                                    catch
                                    {
                                        values.Add(string.Empty);
                                    }
                                }
                                else
                                {
                                    values.Add(string.Empty);
                                }
                            }

                            var lvi = new ListViewItem(values.Count > 0 ? values[0] : string.Empty);
                            for (int i = 1; i < values.Count; i++)
                                lvi.SubItems.Add(values[i]);

                            lvi.Tag = itemObj; // persist underlying object for stable identification
                            if (itemObj is Mirror m)
                                lvi.ForeColor = m.InUse ? Color.Red : SystemColors.WindowText;
                            listViewMirror.Items.Add(lvi);
                        }
                    }
                }

                // Ensure reasonable minimum column widths
                const int MinColumnWidth = 40;
                for (int i = 0; i < listViewMirror.Columns.Count; i++)
                {
                    var col = listViewMirror.Columns[i];
                    if (col.Width < MinColumnWidth)
                        col.Width = 120;
                }

                // record last-known state
                _lastMirrorColumnNames = currentColumnNames;
                _lastMirrorKeys = currentKeys;

                // Notify listeners that mirrors were refreshed
                OnMirrorsRefreshed(EventArgs.Empty);
            }
            finally
            {
                listViewMirror.EndUpdate();
            }
        }

        private void TryAutoAddStandardAndPrompt(Mirror mirror)
        {
            if (mirror == null)
                return;

            var sn = (mirror.SerialNumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sn))
                return;

            if (_promptedNewMirrors.Contains(sn))
                return;

            try
            {
                Data.InitializeDatabase();
                if (Data.MirrorExists(sn))
                {
                    _promptedNewMirrors.Add(sn);
                    return;
                }

                Data.UpsertMirror(sn, null);
                _promptedNewMirrors.Add(sn);

                MessageBox.Show(this, "This is the first time connecting this Standard. Please complete the setup.", "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var frm = new StandardInventoryFrm();
                frm.Shown += (s, e) =>
                {
                    try
                    {
                        frm.SelectStandardInComboBox("Mirror", null, sn);
                    }
                    catch { }
                };
                frm.Show(this);
                try { frm.BringToFront(); } catch { }
            }
            catch
            {
                // ignore auto-add failures
            }
        }

        // Helper: update mirror item values in-place (no clearing)
        private void UpdateMirrorItemValuesInPlace(List<Mirror> mirrors, List<KeyValuePair<MirrorDisplayOptions.Field, MirrorDisplayOptions.ColumnOption>> visibleFields)
        {
            if (mirrors == null || visibleFields == null) return;

            // Map mirrors by ComPort (case-insensitive)
            var map = mirrors
                .Where(m => m != null)
                .GroupBy(m => (m.ComPort ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Build property cache from Mirror type
            var elementType = typeof(Mirror);
            var props = elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var propCache = new Dictionary<object, PropertyInfo>();
            foreach (var kv in visibleFields)
            {
                var fieldName = kv.Key.ToString();
                var p = elementType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null)
                {
                    string normalizedField = new string(fieldName.Where(c => c != '_' && c != ' ').ToArray());
                    p = props.FirstOrDefault(c => string.Equals(new string(c.Name.Where(ch => ch != '_' && ch != ' ').ToArray()), normalizedField, StringComparison.OrdinalIgnoreCase));
                }
                propCache[kv.Key] = p;
            }

            // For each existing ListViewItem update subitems if matching mirror found (by Tag or ComPort)
            for (int i = listViewMirror.Items.Count - 1; i >= 0; i--)
            {
                var item = listViewMirror.Items[i];
                string com = string.Empty;
                Mirror underlying = null;
                try { underlying = item.Tag as Mirror; } catch { underlying = null; }
                if (underlying != null && !string.IsNullOrEmpty(underlying.ComPort))
                    com = underlying.ComPort;

                if (string.IsNullOrEmpty(com))
                {
                    // fallback: try to read from first column by name 'ComPort' if present
                    for (int ci = 0; ci < listViewMirror.Columns.Count; ci++)
                    {
                        if (string.Equals(listViewMirror.Columns[ci].Name, "ComPort", StringComparison.OrdinalIgnoreCase))
                        {
                            int subIndex = ci; // main view has no Test column
                            if (subIndex >= 0 && subIndex < item.SubItems.Count)
                                com = item.SubItems[subIndex].Text;
                            break;
                        }
                    }
                }

                Mirror mNew = null;
                if (!string.IsNullOrEmpty(com) && map.TryGetValue(com, out mNew))
                {
                    // update subitems according to visibleFields
                    for (int colIdx = 0; colIdx < visibleFields.Count; colIdx++)
                    {
                        var kv = visibleFields[colIdx];
                        var prop = propCache[kv.Key];
                        string txt = string.Empty;
                        if (prop != null)
                        {
                            try { var v = prop.GetValue(mNew); txt = v == null ? string.Empty : FormatTempHumidityValueIfNeeded(kv.Key.ToString(), v); } catch { txt = string.Empty; }
                        }

                        if (colIdx < item.SubItems.Count)
                            item.SubItems[colIdx].Text = txt;
                        else
                            item.SubItems.Add(txt);
                    }

                    item.Tag = mNew;
                    item.ForeColor = mNew.InUse ? Color.Red : SystemColors.WindowText;
                }
                else
                {
                    // mirror removed -> remove item
                    listViewMirror.Items.RemoveAt(i);
                }
            }

            // Add any new mirrors not present in the list
            var existingComs = listViewMirror.Items.Cast<ListViewItem>().Select(it => (it.Tag as Mirror)?.ComPort ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var m in mirrors)
            {
                var comKey = m?.ComPort ?? string.Empty;
                if (!existingComs.Contains(comKey))
                {
                    var values = new List<string>(visibleFields.Count);
                    foreach (var kv in visibleFields)
                    {
                        var prop = propCache[kv.Key];
                        try { var v = prop?.GetValue(m); values.Add(v == null ? string.Empty : FormatTempHumidityValueIfNeeded(kv.Key.ToString(), v)); } catch { values.Add(string.Empty); }
                    }
                    var lvi = new ListViewItem(values.Count > 0 ? values[0] : string.Empty);
                    for (int i = 1; i < values.Count; i++) lvi.SubItems.Add(values[i]);
                    lvi.Tag = m;
                    lvi.ForeColor = m.InUse ? Color.Red : SystemColors.WindowText;
                    listViewMirror.Items.Add(lvi);
                }
            }
        }

        // Refreshes listViewRotProbe with contents of Program.ConnectedProbes, mapping HeaderOptions.Field -> RotProbe property names
        private void RefreshConnectedProbes()
        {
            if (listViewRotProbe == null)
                return;

            try
            {
                listViewRotProbe.BeginUpdate();

                // Take a snapshot first to avoid race conditions
                var probes = Program.GetConnectedProbesSnapshot();

                if (probes == null)
                {
                    if (listViewRotProbe.Items.Count > 0 || listViewRotProbe.Columns.Count > 0)
                    {
                        listViewRotProbe.Items.Clear();
                        listViewRotProbe.Columns.Clear();
                        _lastProbeKeys = null;
                        _lastProbeColumnNames = null;
                        OnProbesRefreshed(EventArgs.Empty);
                    }
                    return;
                }

                // Load display options to determine which logical fields/columns to show
                var displayOptions = HeaderOptions.Load() ?? new HeaderOptions();
                var visibleFields = displayOptions.GetVisibleOrdered().ToList();
                if (visibleFields.Count == 0)
                {
                    if (listViewRotProbe.Items.Count > 0 || listViewRotProbe.Columns.Count > 0)
                    {
                        listViewRotProbe.Items.Clear();
                        listViewRotProbe.Columns.Clear();
                        _lastProbeKeys = null;
                        _lastProbeColumnNames = null;
                        OnProbesRefreshed(EventArgs.Empty);
                    }
                    return;
                }

                var currentColumnNames = visibleFields.Select(kv => kv.Key.ToString()).ToList();
                var currentKeys = probes.Select(p => (p?.ComPort ?? string.Empty)).OrderBy(s => s).ToList();

                bool columnsChanged = _lastProbeColumnNames == null || !_lastProbeColumnNames.SequenceEqual(currentColumnNames);
                bool itemsChanged = _lastProbeKeys == null || !_lastProbeKeys.SequenceEqual(currentKeys);

                if (!columnsChanged && !itemsChanged && listViewRotProbe.Items.Count > 0 && listViewRotProbe.Columns.Count > 0)
                {
                    // Only values changed: update subitems in-place and do not raise event
                    UpdateProbeItemValuesInPlace(probes, visibleFields);
                    listViewRotProbe.EndUpdate();
                    OnProbesRefreshed(EventArgs.Empty);
                    return;
                }

                // Otherwise rebuild
                listViewRotProbe.Items.Clear();
                listViewRotProbe.Columns.Clear();

                // Determine element type
                Type elementType = null;
                var probesType = probes.GetType();
                var enumInterface = probesType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumInterface != null)
                {
                    elementType = enumInterface.GetGenericArguments()[0];
                }

                object firstElement = null;
                var enumer = probes as System.Collections.IEnumerable;
                if (enumer != null)
                {
                    foreach (var obj in enumer)
                    {
                        firstElement = obj;
                        break;
                    }
                }

                if (elementType == null && firstElement != null)
                    elementType = firstElement.GetType();

                listViewRotProbe.View = View.Details;
                listViewRotProbe.FullRowSelect = true;

                // Create columns based on display options; create ColumnHeader objects explicitly
                foreach (var kv in visibleFields)
                {
                    var field = kv.Key;
                    var option = kv.Value;
                    var headerText = option.HeaderText ?? field.ToString();

                    var column = new ColumnHeader
                    {
                        Name = field.ToString(),
                        Text = headerText,
                        Width = (option != null && option.Width > 0) ? option.Width : 120,
                        TextAlign = HorizontalAlignment.Left
                    };

                    listViewRotProbe.Columns.Add(column);
                }

                if (elementType == null)
                {
                    // No probe type info: list ToString() for each item in single column
                    if (enumer != null)
                    {
                        foreach (var itemObj in enumer)
                        {
                            var text = itemObj?.ToString() ?? string.Empty;
                            var lvi = new ListViewItem(text);
                            lvi.Tag = itemObj;
                            listViewRotProbe.Items.Add(lvi);
                        }
                    }
                }
                else
                {
                    // Build a property cache mapping Field -> PropertyInfo (or null if not found)
                    var propCache = new Dictionary<HeaderOptions.Field, PropertyInfo>();
                    var props = elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                    Func<string, PropertyInfo> findProp = (fieldName) =>
                    {
                        if (string.IsNullOrWhiteSpace(fieldName))
                            return null;

                        // 1) exact match (case-insensitive)
                        var p = elementType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (p != null) return p;

                        // Normalize name: remove underscores/spaces
                        string normalizedField = new string(fieldName.Where(c => c != '_' && c != ' ').ToArray());
                        foreach (var candidate in props)
                        {
                            var normalizedCandidate = new string(candidate.Name.Where(c => c != '_' && c != ' ').ToArray());
                            if (string.Equals(normalizedCandidate, normalizedField, StringComparison.OrdinalIgnoreCase))
                                return candidate;
                        }

                        // 2) contains
                        foreach (var candidate in props)
                        {
                            if (candidate.Name.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0)
                                return candidate;
                        }

                        // 3) not found
                        return null;
                    };

                    // Prepopulate cache
                    foreach (var kv in visibleFields)
                    {
                        var field = kv.Key;
                        var fieldName = field.ToString();
                        PropertyInfo matched = findProp(fieldName);

                        // Try some common alternate names for known fields (best-effort)
                        if (matched == null)
                        {
                            switch (fieldName.ToLowerInvariant())
                            {
                                case "comport":
                                case "com_port":
                                    matched = findProp("Port") ?? findProp("ComPort");
                                    break;
                                case "serialnumber":
                                    matched = findProp("Serial") ?? findProp("SerialNo") ?? findProp("SerialNumber");
                                    break;
                                case "devicename":
                                    matched = findProp("Name") ?? findProp("DeviceName");
                                    break;
                                case "firmwareversion":
                                    matched = findProp("Firmware") ?? findProp("FirmwareVersion") ?? findProp("FwVersion");
                                    matched = findProp("Firmware") ?? findProp("FirmwareVersion") ?? findProp("FwVersion");
                                    break;
                                case "devicemodel":
                                    matched = findProp("Model") ?? findProp("DeviceModel");
                                    break;
                                // add other heuristics here as needed
                            }
                        }

                        // Handle the consistent misspelling in RotProbe: "Humdity" vs the enum "Humidity"
                        // Try swapping "Humidity" <-> "Humdity" so fields like HumidityUnit map to HumidityUnit property
                        if (matched == null)
                        {
                            try
                            {
                                if (fieldName.IndexOf("Humidity", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var alt = fieldName.Replace("Humidity", "Humdity");
                                    matched = findProp(alt);
                                }
                                else if (fieldName.IndexOf("Humdity", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var alt = fieldName.Replace("Humdity", "Humidity");
                                    matched = findProp(alt);
                                }
                            }
                            catch
                            {
                                // ignore any unexpected replacement errors
                            }
                        }

                        propCache[field] = matched;
                    }

                    // Populate rows
                    if (enumer != null)
                    {
                        foreach (var itemObj in enumer)
                        {
                            if (itemObj == null)
                                continue;

                            var values = new List<string>(visibleFields.Count);
                            foreach (var kv in visibleFields)
                            {
                                var field = kv.Key;
                                var prop = propCache[field];
                                if (prop != null)
                                {
                                    try
                                    {
                                        var v = prop.GetValue(itemObj);
                                        values.Add(v == null ? string.Empty : FormatTempHumidityValueIfNeeded(field.ToString(), v));
                                    }
                                    catch
                                    {
                                        values.Add(string.Empty);
                                    }
                                }
                                else
                                {
                                    // No matching property: fallback to empty string
                                    values.Add(string.Empty);
                                }
                            }

                            // Build ListViewItem: first value as text, others as subitems
                            var lvi = new ListViewItem(values.Count > 0 ? values[0] : string.Empty);
                            for (int i = 1; i < values.Count; i++)
                                lvi.SubItems.Add(values[i]);

                            lvi.Tag = itemObj; // persist underlying object for stable identification
                            if (itemObj is RotProbe rp)
                                lvi.ForeColor = rp.InUse ? Color.Red : SystemColors.WindowText;
                            listViewRotProbe.Items.Add(lvi);
                        }
                    }
                }

                // Preserve user-resized widths: only ensure that columns have a reasonable minimum width.
                const int MinColumnWidth = 40;
                for (int i = 0; i < listViewRotProbe.Columns.Count; i++)
                {
                    var col = listViewRotProbe.Columns[i];
                    if (col.Width < MinColumnWidth)
                        col.Width = 120; // reset to default if somehow extremely small
                }

                // record last-known state
                _lastProbeColumnNames = currentColumnNames;
                _lastProbeKeys = currentKeys;

                // Notify listeners that probes were refreshed
                OnProbesRefreshed(EventArgs.Empty);
            }
            finally
            {
                listViewRotProbe.EndUpdate();
            }
        }

        // Helper: update probe item values in-place (no clearing)
        private void UpdateProbeItemValuesInPlace(List<RotProbe> probes, List<KeyValuePair<HeaderOptions.Field, HeaderOptions.ColumnOption>> visibleFields)
        {
            if (probes == null || visibleFields == null) return;

            // Map probes by ComPort (case-insensitive)
            var map = probes.Where(p => p != null).ToDictionary(p => (p.ComPort ?? string.Empty), StringComparer.OrdinalIgnoreCase);

            // Build property cache from RotProbe type
            var elementType = typeof(RotProbe);
            var props = elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var propCache = new Dictionary<HeaderOptions.Field, PropertyInfo>();
            foreach (var kv in visibleFields)
            {
                var fieldName = kv.Key.ToString();
                var p = elementType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null)
                {
                    string normalizedField = new string(fieldName.Where(c => c != '_' && c != ' ').ToArray());
                    p = props.FirstOrDefault(c => string.Equals(new string(c.Name.Where(ch => ch != '_' && ch != ' ').ToArray()), normalizedField, StringComparison.OrdinalIgnoreCase));
                }
                propCache[kv.Key] = p;
            }

            // For each existing ListViewItem update subitems if matching probe found (by Tag or ComPort)
            for (int i = listViewRotProbe.Items.Count - 1; i >= 0; i--)
            {
                var item = listViewRotProbe.Items[i];
                string com = string.Empty;
                RotProbe underlying = null;
                try { underlying = item.Tag as RotProbe; } catch { underlying = null; }
                if (underlying != null && !string.IsNullOrEmpty(underlying.ComPort))
                    com = underlying.ComPort;

                if (string.IsNullOrEmpty(com))
                {
                    for (int ci = 0; ci < listViewRotProbe.Columns.Count; ci++)
                    {
                        if (string.Equals(listViewRotProbe.Columns[ci].Name, "ComPort", StringComparison.OrdinalIgnoreCase))
                        {
                            int subIndex = ci; // main view has no Test column
                            if (subIndex >= 0 && subIndex < item.SubItems.Count)
                                com = item.SubItems[subIndex].Text;
                            break;
                        }
                    }
                }

                RotProbe pNew = null;
                if (!string.IsNullOrEmpty(com) && map.TryGetValue(com, out pNew))
                {
                    // update subitems according to visibleFields
                    for (int colIdx = 0; colIdx < visibleFields.Count; colIdx++)
                    {
                        var kv = visibleFields[colIdx];
                        var prop = propCache[kv.Key];
                        string txt = string.Empty;
                        if (prop != null)
                        {
                            try { var v = prop.GetValue(pNew); txt = v == null ? string.Empty : FormatTempHumidityValueIfNeeded(kv.Key.ToString(), v); } catch { txt = string.Empty; }
                        }

                        if (colIdx < item.SubItems.Count)
                            item.SubItems[colIdx].Text = txt;
                        else
                            item.SubItems.Add(txt);
                    }

                    item.Tag = pNew;
                    item.ForeColor = pNew.InUse ? Color.Red : SystemColors.WindowText;
                }
                else
                {
                    // probe removed -> remove item
                    // Keep fake probes used for UX/UI testing even if they don't appear in the latest snapshot.
                    // If the fake probe is not visible, it will be re-added in the next full refresh.
                    var existing = item.Tag as RotProbe;
                    if (existing != null && string.Equals(existing.DeviceModel ?? string.Empty, "Fake", StringComparison.OrdinalIgnoreCase))
                    {
                        item.ForeColor = existing.InUse ? Color.Red : SystemColors.WindowText;
                    }
                    else
                    {
                        listViewRotProbe.Items.RemoveAt(i);
                    }
                }
            }

            // Add any new probes not present in the list
            var existingComs = listViewRotProbe.Items.Cast<ListViewItem>().Select(it => (it.Tag as RotProbe)?.ComPort ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var p in probes)
            {
                var comKey = p?.ComPort ?? string.Empty;
                if (!existingComs.Contains(comKey))
                {
                    var values = new List<string>(visibleFields.Count);
                    foreach (var kv in visibleFields)
                    {
                        var prop = propCache[kv.Key];
                        try { var v = prop?.GetValue(p); values.Add(v == null ? string.Empty : FormatTempHumidityValueIfNeeded(kv.Key.ToString(), v)); } catch { values.Add(string.Empty); }
                    }
                    var lvi = new ListViewItem(values.Count > 0 ? values[0] : string.Empty);
                    for (int i = 1; i < values.Count; i++) lvi.SubItems.Add(values[i]);
                    lvi.Tag = p;
                    lvi.ForeColor = p.InUse ? Color.Red : SystemColors.WindowText;
                    listViewRotProbe.Items.Add(lvi);
                }
            }
        }

        protected virtual void OnProbesRefreshed(EventArgs e)
        {
            ProbesRefreshed?.Invoke(this, e);
        }

        protected virtual void OnMirrorsRefreshed(EventArgs e)
        {
            MirrorsRefreshed?.Invoke(this, e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop and dispose timer to avoid Tick after form is closed
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= RefreshTimer_Tick;
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }

            base.OnFormClosing(e);
        }

        private void headerViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create and show the options form modally with this form as owner, ensuring disposal.
            using (var optionsForm = new DisplayOptionsFrm())
            {
                optionsForm.ShowDialog(this);
                RefreshConnectedMirrors();
                RefreshConnectedProbes();
            }

            // If changes in the options affect the main form, refresh here:
            // RefreshConnectedProbes();
        }

        private void mirrorViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var optionsForm = new MirrorOptions())
            {
                optionsForm.ShowDialog(this);
            }
        }

        private void createStepListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var stepListForm = new StepEditor();
            stepListForm.Show(this);
        }

        private void celsiusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program._globalTemperatureC == false)
            {
                Program._globalTemperatureC = true;
                celsiusToolStripMenuItem.Checked = true;
                RefreshConnectedProbes();
            } 
            else if (Program._globalTemperatureC == true)
            {
                celsiusToolStripMenuItem.Checked = false;
                Program._globalTemperatureC = false;
                RefreshConnectedProbes();
            }
        }

        // Save column widths for probes when user resizes a column
        private void ListViewRotProbe_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            try
            {
                var opts = HeaderOptions.Load() ?? new HeaderOptions();
                foreach (ColumnHeader col in listViewRotProbe.Columns)
                {
                    if (string.IsNullOrWhiteSpace(col.Name))
                        continue;

                    // Ignore the case where Name might be numeric index or "Test" column originating from other forms
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
                // ignore save failures
            }
        }

        // Save column widths for mirrors when user resizes a column
        private void ListViewMirror_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
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
                // ignore save failures
            }
        }

        private void validationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var validationFrm = new ValidationFrm();
            validationFrm.Show();
        }

        //
        // New: Copy helpers and event handlers for listViewRotProbe
        //

        // Handles Ctrl+C on the list view
        private void ListViewRotProbe_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopyListViewSelectionToClipboard(listViewRotProbe, includeHeaders: true, copyAllIfNoneSelected: true);
                    e.Handled = true;
                }
            }
            catch { }
        }

        // Context menu: Copy selected (or all if none selected)
        private void CopySelectedMenu_Click(object sender, EventArgs e)
        {
            try
            {
                CopyListViewSelectionToClipboard(listViewRotProbe, includeHeaders: true, copyAllIfNoneSelected: true);
            }
            catch { }
        }

        // Context menu: Copy all rows
        private void CopyAllMenu_Click(object sender, EventArgs e)
        {
            try
            {
                CopyListViewSelectionToClipboard(listViewRotProbe, includeHeaders: true, copyAllIfNoneSelected: false);
            }
            catch { }
        }

        // Builds a tab-separated representation of selected rows (or all rows) and places it on the clipboard.
        // includeHeaders: when true the column headers are included as first row.
        // copyAllIfNoneSelected: when true and nothing is selected, all rows are copied; when false and nothing is
        // selected, nothing will be copied.
        private void CopyListViewSelectionToClipboard(ListView lv, bool includeHeaders = true, bool copyAllIfNoneSelected = true)
        {
            if (lv == null)
                return;

            if (lv.Columns == null || lv.Columns.Count == 0)
                return;

            var sb = new StringBuilder();

            if (includeHeaders)
            {
                for (int c = 0; c < lv.Columns.Count; c++)
                {
                    if (c > 0) sb.Append('\t');
                    sb.Append(lv.Columns[c].Text ?? string.Empty);
                }
                sb.AppendLine();
            }

            List<ListViewItem> rowsToCopy;
            if (lv.SelectedItems != null && lv.SelectedItems.Count > 0)
            {
                // Preserve displayed order
                rowsToCopy = lv.SelectedItems.Cast<ListViewItem>().OrderBy(it => it.Index).ToList();
            }
            else if (copyAllIfNoneSelected)
            {
                rowsToCopy = lv.Items.Cast<ListViewItem>().ToList();
            }
            else
            {
                // Nothing selected and we were asked not to copy all
                return;
            }

            foreach (var item in rowsToCopy)
            {
                for (int c = 0; c < lv.Columns.Count; c++)
                {
                    if (c > 0) sb.Append('\t');
                    string text = (c < item.SubItems.Count) ? item.SubItems[c].Text : string.Empty;
                    // sanitize tabs and newlines to keep TSV intact
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
                    }
                    sb.Append(text ?? string.Empty);
                }
                sb.AppendLine();
            }

            try
            {
                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                try { Debug.WriteLine($"CopyListViewSelectionToClipboard: failed to set clipboard: {ex}"); } catch { }
            }
        }

        private void hygrogenIPConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ipConfigFrm = new IPConfigFrm();
            ipConfigFrm.ShowDialog();
        }

        private void hygrogenDisplayOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ChamberOptions = new ChamberOptions();
            ChamberOptions.ShowDialog();
        }

        private void probesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var probeInventoryFrm = new ProbeInventoryFrm();
            probeInventoryFrm.ShowDialog();
        }

        private void standardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var standardInventoryFrm = new StandardInventoryFrm();
            standardInventoryFrm.ShowDialog();
        }
    }
}
