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
        // Timer to refresh listView2 periodically
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
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshConnectedProbes();
        }

        // Refreshes listView2 with contents of Program.ConnectedProbes
        private void RefreshConnectedProbes()
        {
            if (listView2 == null)
                return;

            try
            {
                listView2.BeginUpdate();
                listView2.Items.Clear();

                // If Program.ConnectedProbes is null, treat as empty
                var probes = Program.ConnectedProbes;
                if (probes == null)
                    return;

                Type elementType = null;
                var probesType = probes.GetType();
                var enumInterface = probesType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumInterface != null)
                {
                    elementType = enumInterface.GetGenericArguments()[0];
                }

                // If we couldn't determine element type from generic interface, get first element to infer type
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

                // If still no element type and no items, nothing to display
                if (elementType == null)
                    return;

                // Get public instance properties
                var props = elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                // Configure listView2 for details view and clear/prepare columns
                listView2.View = View.Details;
                listView2.FullRowSelect = true;
                listView2.Columns.Clear();

                if (props.Length == 0)
                {
                    // Fallback: single column showing .ToString()
                    listView2.Columns.Add("Value", -2, HorizontalAlignment.Left);

                    if (enumer != null)
                    {
                        foreach (var itemObj in enumer)
                        {
                            var text = itemObj?.ToString() ?? string.Empty;
                            var lvi = new ListViewItem(text);
                            listView2.Items.Add(lvi);
                        }
                    }
                }
                else
                {
                    // Create columns for each property
                    foreach (var p in props)
                    {
                        listView2.Columns.Add(p.Name, 120, HorizontalAlignment.Left);
                    }

                    if (enumer != null)
                    {
                        foreach (var itemObj in enumer)
                        {
                            if (itemObj == null)
                                continue;

                            var values = new List<string>(props.Length);
                            foreach (var p in props)
                            {
                                try
                                {
                                    var v = p.GetValue(itemObj);
                                    values.Add(v?.ToString() ?? string.Empty);
                                }
                                catch
                                {
                                    values.Add(string.Empty);
                                }
                            }

                            // First property becomes the item text, rest are subitems
                            var lvi = new ListViewItem(values.Count > 0 ? values[0] : string.Empty);
                            for (int i = 1; i < values.Count; i++)
                                lvi.SubItems.Add(values[i]);

                            listView2.Items.Add(lvi);
                        }
                    }
                }

                // Auto-resize columns to fit content
                for (int i = 0; i < listView2.Columns.Count; i++)
                {
                    listView2.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            finally
            {
                listView2.EndUpdate();
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
    }
}
