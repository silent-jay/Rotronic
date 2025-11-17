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
        // Timer to refresh listViewRotProbe periodically
        private System.Windows.Forms.Timer _refreshTimer;

        public Main()
        {
            InitializeComponent();
            // Initialize timer
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 15_000; // 15 seconds
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Initial population
            RefreshConnectedProbes();
            RefreshConnectedMirrors();
        }

        // Pseudocode / Plan:
        // 1. Load HeaderOptions (or create defaults).
        // 2. Clear existing columns (ColumnHeaderCollection is never null).
        // 3. For each visible ordered entry:
        //    a. Build a ColumnHeader instance and set Name, Text, Width and TextAlign.
        //    b. Add the ColumnHeader object to listViewRotProbe.Columns.
        // 4. This avoids calling a non-existent overload of Columns.Add that takes 4 args.
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshConnectedProbes();
            RefreshConnectedMirrors();
        }

        private void RefreshConnectedMirrors()
        {
            /*No mirror available right now. This is a placeholder for future implementation.
             *Will implement communication with mirror device when available, for now we're setting it up to use dummy data.
             */

             
        }

        // Refreshes listViewRotProbe with contents of Program.ConnectedProbes, mapping HeaderOptions.Field -> RotProbe property names
        private void RefreshConnectedProbes()
        {
            if (listViewRotProbe == null)
                return;

            try
            {
                listViewRotProbe.BeginUpdate();
                listViewRotProbe.Items.Clear();

                var probes = Program.ConnectedProbes;
                if (probes == null)
                {
                    listViewRotProbe.Columns.Clear();
                    return;
                }

                // Load display options to determine which logical fields/columns to show
                var displayOptions = HeaderOptions.Load() ?? new HeaderOptions();
                var visibleFields = displayOptions.GetVisibleOrdered().ToList();
                if (visibleFields.Count == 0)
                {
                    listViewRotProbe.Columns.Clear();
                    return;
                }

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

                // If we still don't know element type, fall back to showing ToString() values
                listViewRotProbe.View = View.Details;
                listViewRotProbe.FullRowSelect = true;
                listViewRotProbe.Columns.Clear();

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
                        Width = 120,
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
                                    break;
                                case "devicemodel":
                                    matched = findProp("Model") ?? findProp("DeviceModel");
                                    break;
                                // add other heuristics here as needed
                            }
                        }

                        // Handle the consistent misspelling in RotProbe: "Humdity" vs the enum "Humidity"
                        // Try swapping "Humidity" <-> "Humdity" so fields like HumidityUnit map to HumdityUnit property
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
                                        values.Add(v?.ToString() ?? string.Empty);
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
            }
            finally
            {
                listViewRotProbe.EndUpdate();
            }
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
    }
}
