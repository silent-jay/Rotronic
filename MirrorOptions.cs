using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{
    public partial class MirrorOptions : Form
    {
        
        public MirrorOptions()
        {
            InitializeComponent();

            var displayOptions = MirrorDisplayOptions.Load() ?? new MirrorDisplayOptions();

            // Guard: ensure controls exist and options are available
            if (listBoxShow == null || listBoxDNS == null || displayOptions?.Options == null)
                return;

            // Clear existing items
            listBoxShow.Items.Clear();
            listBoxDNS.Items.Clear();

            var options = displayOptions.Options;

            // Ordered visible: Visible == true && Order > 0, sorted by Order then Field name
            var orderedVisible = options
                .Where(kv => kv.Value != null && kv.Value.Visible && kv.Value.Order > 0)
                .OrderBy(kv => kv.Value.Order)
                .ThenBy(kv => kv.Key.ToString())
                .ToList();

            // Unordered visible: Visible == true && Order == 0, stable by Field name
            var unorderedVisible = options
                .Where(kv => kv.Value != null && kv.Value.Visible && kv.Value.Order == 0)
                .OrderBy(kv => kv.Key.ToString())
                .ToList();

            // Hidden (not visible)
            var hidden = options
                .Where(kv => kv.Value != null && !kv.Value.Visible)
                .OrderBy(kv => kv.Key.ToString())
                .ToList();

            // Helper to get display text
            Func<KeyValuePair<MirrorDisplayOptions.Field, MirrorDisplayOptions.ColumnOption>, string> getText =
                kv => string.IsNullOrEmpty(kv.Value.HeaderText) ? kv.Key.ToString() : kv.Value.HeaderText;

            // Add ordered visible first
            foreach (var kv in orderedVisible)
                listBoxShow.Items.Add(getText(kv));

            // Then add unordered visible (Order == 0) below the last ordered entry
            foreach (var kv in unorderedVisible)
                listBoxShow.Items.Add(getText(kv));

            // Add hidden items to listBoxDNS
            foreach (var kv in hidden)
                listBoxDNS.Items.Add(getText(kv));
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            // Load existing saved options (keeps defaults for missing entries)
            var headerOptions = MirrorDisplayOptions.Load();
            if (headerOptions?.Options == null)
                return;

            // Helper to get display text for a stored option (same logic used when populating lists)
            Func<KeyValuePair<MirrorDisplayOptions.Field, MirrorDisplayOptions.ColumnOption>, string> getText =
                kv => string.IsNullOrEmpty(kv.Value.HeaderText) ? kv.Key.ToString() : kv.Value.HeaderText;

            // First set all entries to hidden/unspecified order so we have a clean baseline.
            var keys = headerOptions.Options.Keys.ToList();
            foreach (var k in keys)
            {
                var opt = headerOptions.Options[k];
                opt.Visible = false;
                opt.Order = 0;
            }

            // Save order and visible=true for items in listBoxShow (order preserved)
            for (int i = 0; i < listBoxShow.Items.Count; i++)
            {
                var itemText = (listBoxShow.Items[i] ?? string.Empty).ToString();

                // Find matching Field by comparing the display text
                bool matched = false;
                MirrorDisplayOptions.Field matchedField = default(MirrorDisplayOptions.Field);
                foreach (var kv in headerOptions.Options)
                {
                    if (string.Equals(getText(kv), itemText, StringComparison.Ordinal))
                    {
                        matchedField = kv.Key;
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    var opt = headerOptions.Options[matchedField];
                    opt.Visible = true;
                    opt.Order = i + 1; // save 1-based order
                }
                // If no match found, ignore the item (defensive)
            }

            // Items remaining (those not set visible above) should remain hidden (Visible=false).
            // listBoxDNS items are therefore already correctly handled by the baseline reset.

            // Persist changes
            headerOptions.Save();

            // Close dialog with OK result
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCan_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void buttonUp_Click(object sender, EventArgs e)
        {
            if (listBoxShow == null)
                return;

            int n = listBoxShow.Items.Count;
            if (n == 0)
                return;

            // If nothing selected, do nothing
            if (listBoxShow.SelectedIndices == null || listBoxShow.SelectedIndices.Count == 0)
                return;

            // Build selected flags
            var selected = new bool[n];
            foreach (int idx in listBoxShow.SelectedIndices)
            {
                if (idx >= 0 && idx < n)
                    selected[idx] = true;
            }

            // If any selected item is at the top, do nothing
            if (selected[0])
                return;

            // Snapshot items
            var items = listBoxShow.Items.Cast<object>().ToList();

            // Move selected items up by one where possible (left-to-right)
            for (int i = 1; i < n; i++)
            {
                if (selected[i] && !selected[i - 1])
                {
                    // swap items
                    var tmp = items[i - 1];
                    items[i - 1] = items[i];
                    items[i] = tmp;

                    // swap selected flags
                    selected[i - 1] = true;
                    selected[i] = false;
                }
            }

            // Update listbox and restore selection
            listBoxShow.BeginUpdate();
            try
            {
                listBoxShow.Items.Clear();
                for (int i = 0; i < items.Count; i++)
                    listBoxShow.Items.Add(items[i]);

                listBoxShow.ClearSelected();
                for (int i = 0; i < selected.Length; i++)
                    if (selected[i])
                        listBoxShow.SetSelected(i, true);
            }
            finally
            {
                listBoxShow.EndUpdate();
            }
        }

        private void buttonDwn_Click(object sender, EventArgs e)
        {
            if (listBoxShow == null)
                return;

            int n = listBoxShow.Items.Count;
            if (n == 0)
                return;

            // If nothing selected, do nothing
            if (listBoxShow.SelectedIndices == null || listBoxShow.SelectedIndices.Count == 0)
                return;

            // Build selected flags
            var selected = new bool[n];
            foreach (int idx in listBoxShow.SelectedIndices)
            {
                if (idx >= 0 && idx < n)
                    selected[idx] = true;
            }

            // If any selected item is at the bottom, do nothing
            if (selected[n - 1])
                return;

            // Snapshot items
            var items = listBoxShow.Items.Cast<object>().ToList();

            // Move selected items down by one where possible (right-to-left)
            for (int i = n - 2; i >= 0; i--)
            {
                if (selected[i] && !selected[i + 1])
                {
                    // swap items
                    var tmp = items[i + 1];
                    items[i + 1] = items[i];
                    items[i] = tmp;

                    // swap selected flags
                    selected[i + 1] = true;
                    selected[i] = false;
                }
            }

            // Update listbox and restore selection
            listBoxShow.BeginUpdate();
            try
            {
                listBoxShow.Items.Clear();
                for (int i = 0; i < items.Count; i++)
                    listBoxShow.Items.Add(items[i]);

                listBoxShow.ClearSelected();
                for (int i = 0; i < selected.Length; i++)
                    if (selected[i])
                        listBoxShow.SetSelected(i, true);
            }
            finally
            {
                listBoxShow.EndUpdate();
            }
        }

        private void buttonDel_Click(object sender, EventArgs e)
        {
            if (listBoxShow == null || listBoxDNS == null)
                return;

            var selectedIndices = listBoxShow.SelectedIndices;
            if (selectedIndices == null || selectedIndices.Count == 0)
                return;

            // Sorted ascending to preserve top-to-bottom order when moving
            var indices = selectedIndices.Cast<int>().OrderBy(i => i).ToList();
            int count = indices.Count;
            int minIndex = indices.First();
            int maxIndex = indices.Last();

            // Capture items to move (ascending order)
            var itemsToMove = new List<object>(count);
            foreach (var idx in indices)
            {
                if (idx >= 0 && idx < listBoxShow.Items.Count)
                    itemsToMove.Add(listBoxShow.Items[idx]);
            }

            // Remove from Show in descending order so indices remain valid
            foreach (var idx in indices.OrderByDescending(i => i))
            {
                if (idx >= 0 && idx < listBoxShow.Items.Count)
                    listBoxShow.Items.RemoveAt(idx);
            }

            // Append captured items to DNS preserving original order
            foreach (var item in itemsToMove)
                listBoxDNS.Items.Add(item);

            // Compute the index to select in Show after removal:
            // originalNext = maxIndex + 1
            // candidate = originalNext - count = maxIndex - count + 1
            int candidate = maxIndex - count + 1;

            if (listBoxShow.Items.Count == 0)
            {
                listBoxShow.ClearSelected();
                return;
            }

            if (candidate < 0)
                candidate = 0;
            if (candidate >= listBoxShow.Items.Count)
                candidate = listBoxShow.Items.Count - 1;

            listBoxShow.SelectedIndex = candidate;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (listBoxDNS == null || listBoxShow == null)
                return;

            var selectedIndices = listBoxDNS.SelectedIndices;
            if (selectedIndices == null || selectedIndices.Count == 0)
                return;

            // Copy selected indices to a sorted list (ascending)
            var indices = selectedIndices.Cast<int>().OrderBy(i => i).ToList();
            int count = indices.Count;
            int minIndex = indices.First();
            int maxIndex = indices.Last();

            // Capture items to move in ascending order to preserve original order
            var itemsToMove = new List<object>(count);
            foreach (var idx in indices)
            {
                // Defensive check
                if (idx >= 0 && idx < listBoxDNS.Items.Count)
                    itemsToMove.Add(listBoxDNS.Items[idx]);
            }

            // Remove items from DNS in descending order so indices remain valid while removing
            foreach (var idx in indices.OrderByDescending(i => i))
            {
                if (idx >= 0 && idx < listBoxDNS.Items.Count)
                    listBoxDNS.Items.RemoveAt(idx);
            }

            // Append captured items to Show list in original order
            foreach (var item in itemsToMove)
                listBoxShow.Items.Add(item);

            // Compute the index to select in DNS after removals:
            // originalNext = maxIndex + 1
            // candidate = originalNext - count = maxIndex - count + 1
            int candidate = maxIndex - count + 1;

            if (listBoxDNS.Items.Count == 0)
            {
                // Nothing to select
                listBoxDNS.ClearSelected();
                return;
            }

            if (candidate < 0)
                candidate = 0;
            if (candidate >= listBoxDNS.Items.Count)
                candidate = listBoxDNS.Items.Count - 1;

            listBoxDNS.SelectedIndex = candidate;
        }
    }
}
