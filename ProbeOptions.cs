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
    public partial class DisplayOptionsFrm : Form
    {
        /*
         PSEUDOCODE / PLAN (detailed):
         
         1. Create HeaderOptions instance (already present as displayOptions).
         2. Ensure the list boxes are empty before populating:
            - clear `listBoxShow.Items`
            - clear `listBoxDNS.Items`
         3. Fetch the dictionary of options: displayOptions.Options
            Each entry is KeyValuePair<Field, ColumnOption>
         4. Partition options into three groups:
            - orderedVisible: Visible == true && Order > 0
              -> Sort by Order ascending, then by Field name as tie-breaker
            - unorderedVisible: Visible == true && Order == 0
              -> These should appear after all orderedVisible items. Any stable order is fine; use Field name order.
            - hidden: Visible == false
              -> These go to listBoxDNS in any order
         5. For display text use ColumnOption.HeaderText if not null/empty, otherwise fall back to Field.ToString()
         6. Add items:
            - Add orderedVisible items to `listBoxShow` in sorted order
            - Then add unorderedVisible items to `listBoxShow`
            - Add hidden items to `listBoxDNS`
         7. Leave other form initialization unchanged.
        */

        public DisplayOptionsFrm()
        {
            // Ensure controls are created first
            InitializeComponent();

            // Load persisted options (falls back to defaults if file missing)
            var displayOptions = HeaderOptions.Load() ?? new HeaderOptions();

            // Guard: ensure controls exist and options are available
            if (listBoxShow == null || listBoxDNS == null || displayOptions?.Options == null)
                return;

            // Clear existing items
            listBoxShow.Items.Clear();
            listBoxDNS.Items.Clear();

            var options = displayOptions.Options; // Dictionary<HeaderOptions.Field, ColumnOption>

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
            Func<KeyValuePair<HeaderOptions.Field, HeaderOptions.ColumnOption>, string> getText =
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
            var headerOptions = HeaderOptions.Load();
            if (headerOptions?.Options == null)
                return;

            // Helper to get display text for a stored option (same logic used when populating lists)
            Func<KeyValuePair<HeaderOptions.Field, HeaderOptions.ColumnOption>, string> getText =
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
                HeaderOptions.Field matchedField = default(HeaderOptions.Field);
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

        private void buttonDel_Click(object sender, EventArgs e)
        {
            /*
             PSEUDOCODE / PLAN (detailed):

             Goal:
             - Move selected item(s) from `listBoxShow` to `listBoxDNS`.
             - After moving, select the item in `listBoxShow` that was immediately
               after the last moved item in the original list. If there is no such
               item, select the previous available item. If `listBoxShow` becomes
               empty, clear selection.

             Steps:
             1. Guard: return if either list box is null.
             2. Get selected indices from `listBoxShow`. If none, return.
             3. Copy selected indices to a sorted ascending list to preserve order.
             4. Determine `count`, `minIndex`, `maxIndex`.
             5. Capture items to move in ascending index order into `itemsToMove`.
             6. Remove captured indices from `listBoxShow` by iterating indices in descending order.
             7. Append `itemsToMove` to `listBoxDNS` in captured order.
             8. Compute candidate selection index in `listBoxShow`:
                - originalNext = maxIndex + 1
                - candidate = originalNext - count = maxIndex - count + 1
                - If list is empty -> clear selection and return.
                - Clamp candidate into [0, listBoxShow.Items.Count - 1].
             9. Set `listBoxShow.SelectedIndex` to the computed candidate.
            */

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

        /*
         PSEUDOCODE / DETAILED PLAN:

         Goal:
         - Move the selected item(s) from `listBoxDNS` to `listBoxShow`.
         - After moving, highlight the item that was immediately after the last moved item
           in `listBoxDNS` (the "next" item). If there is no next item, select the previous
           available item. If the DNS list becomes empty, do not select anything.
         - If nothing is selected in `listBoxDNS`, do nothing.

         Steps:
         1. Guard checks:
            - If `listBoxDNS` or `listBoxShow` are null, return.
            - If `listBoxDNS.SelectedIndices` is null or has Count == 0, return.

         2. Collect selected indices into a list of ints, sorted ascending:
            - This preserves the original top-to-bottom order of selected items.

         3. Determine:
            - `minIndex` = smallest selected index
            - `maxIndex` = largest selected index
            - `count` = number of selected items

         4. Extract the items to move in ascending order (so the moved items keep their
            original order when appended to `listBoxShow`):
            - For each index in ascending selectedIndices: capture `listBoxDNS.Items[index]`
              into a temporary list `itemsToMove`.

         5. Remove the selected items from `listBoxDNS`:
            - Iterate the selected indices in descending order and call `listBoxDNS.Items.RemoveAt(idx)`
              so removal doesn't shift yet-to-be-removed indices.

         6. Append the captured `itemsToMove` to `listBoxShow.Items` in the same order
            they were captured (ascending original order).

         7. Compute the index to select in `listBoxDNS` after removals:
            - The original "next" index in DNS would have been `originalNext = maxIndex + 1`.
            - After removing `count` items that were all <= maxIndex, the new index of that
              originalNext item is `candidate = originalNext - count = maxIndex - count + 1`.
            - If `candidate` is within [0, listBoxDNS.Items.Count - 1], select it.
            - If `candidate` >= listBoxDNS.Items.Count (no next item), select the last item:
              `listBoxDNS.Items.Count - 1`.
            - If `candidate` < 0 (happens when removals cleared items above), and list is not empty,
              select index 0.
            - If the DNS list is empty after removal, do not select anything.

         8. Set `listBoxDNS.SelectedIndex` to the computed index and ensure focus is not changed
            otherwise.

         Notes:
         - Preserve object identity of moved items (do not duplicate strings unnecessarily).
         - Handle single and multiple selections consistently.
         - Keep the order of moved items consistent with their appearance in `listBoxDNS`.
        */
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

    }
}
