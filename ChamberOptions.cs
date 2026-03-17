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
    public partial class ChamberOptions : Form
    {
        public ChamberOptions()
        {
            InitializeComponent();

            var displayOptions = ChamberDisplayOptions.Load() ?? new ChamberDisplayOptions();

            if (listBoxShow == null || listBoxDNS == null || displayOptions?.Options == null)
                return;

            listBoxShow.Items.Clear();
            listBoxDNS.Items.Clear();

            var options = displayOptions.Options;

            var orderedVisible = options
                .Where(kv => kv.Value != null && kv.Value.Visible && kv.Value.Order > 0)
                .OrderBy(kv => kv.Value.Order)
                .ThenBy(kv => kv.Key.ToString())
                .ToList();

            var unorderedVisible = options
                .Where(kv => kv.Value != null && kv.Value.Visible && kv.Value.Order == 0)
                .OrderBy(kv => kv.Key.ToString())
                .ToList();

            var hidden = options
                .Where(kv => kv.Value != null && !kv.Value.Visible)
                .OrderBy(kv => kv.Key.ToString())
                .ToList();

            Func<KeyValuePair<ChamberDisplayOptions.Field, ChamberDisplayOptions.ColumnOption>, string> getText =
                kv => string.IsNullOrEmpty(kv.Value.HeaderText) ? kv.Key.ToString() : kv.Value.HeaderText;

            foreach (var kv in orderedVisible)
                listBoxShow.Items.Add(getText(kv));

            foreach (var kv in unorderedVisible)
                listBoxShow.Items.Add(getText(kv));

            foreach (var kv in hidden)
                listBoxDNS.Items.Add(getText(kv));
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            var headerOptions = ChamberDisplayOptions.Load();
            if (headerOptions?.Options == null)
                return;

            Func<KeyValuePair<ChamberDisplayOptions.Field, ChamberDisplayOptions.ColumnOption>, string> getText =
                kv => string.IsNullOrEmpty(kv.Value.HeaderText) ? kv.Key.ToString() : kv.Value.HeaderText;

            var keys = headerOptions.Options.Keys.ToList();
            foreach (var k in keys)
            {
                var opt = headerOptions.Options[k];
                opt.Visible = false;
                opt.Order = 0;
            }

            for (int i = 0; i < listBoxShow.Items.Count; i++)
            {
                var itemText = (listBoxShow.Items[i] ?? string.Empty).ToString();

                bool matched = false;
                ChamberDisplayOptions.Field matchedField = default(ChamberDisplayOptions.Field);
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
                    opt.Order = i + 1;
                }
            }

            headerOptions.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCan_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonUp_Click(object sender, EventArgs e)
        {
            if (listBoxShow == null)
                return;

            int n = listBoxShow.Items.Count;
            if (n == 0)
                return;

            if (listBoxShow.SelectedIndices == null || listBoxShow.SelectedIndices.Count == 0)
                return;

            var selected = new bool[n];
            foreach (int idx in listBoxShow.SelectedIndices)
            {
                if (idx >= 0 && idx < n)
                    selected[idx] = true;
            }

            if (selected[0])
                return;

            var items = listBoxShow.Items.Cast<object>().ToList();

            for (int i = 1; i < n; i++)
            {
                if (selected[i] && !selected[i - 1])
                {
                    var tmp = items[i - 1];
                    items[i - 1] = items[i];
                    items[i] = tmp;

                    selected[i - 1] = true;
                    selected[i] = false;
                }
            }

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

            if (listBoxShow.SelectedIndices == null || listBoxShow.SelectedIndices.Count == 0)
                return;

            var selected = new bool[n];
            foreach (int idx in listBoxShow.SelectedIndices)
            {
                if (idx >= 0 && idx < n)
                    selected[idx] = true;
            }

            if (selected[n - 1])
                return;

            var items = listBoxShow.Items.Cast<object>().ToList();

            for (int i = n - 2; i >= 0; i--)
            {
                if (selected[i] && !selected[i + 1])
                {
                    var tmp = items[i + 1];
                    items[i + 1] = items[i];
                    items[i] = tmp;

                    selected[i + 1] = true;
                    selected[i] = false;
                }
            }

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

            var indices = selectedIndices.Cast<int>().OrderBy(i => i).ToList();
            int count = indices.Count;
            int maxIndex = indices.Last();

            var itemsToMove = new List<object>(count);
            foreach (var idx in indices)
            {
                if (idx >= 0 && idx < listBoxShow.Items.Count)
                    itemsToMove.Add(listBoxShow.Items[idx]);
            }

            foreach (var idx in indices.OrderByDescending(i => i))
            {
                if (idx >= 0 && idx < listBoxShow.Items.Count)
                    listBoxShow.Items.RemoveAt(idx);
            }

            foreach (var item in itemsToMove)
                listBoxDNS.Items.Add(item);

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

            var indices = selectedIndices.Cast<int>().OrderBy(i => i).ToList();
            int count = indices.Count;
            int maxIndex = indices.Last();

            var itemsToMove = new List<object>(count);
            foreach (var idx in indices)
            {
                if (idx >= 0 && idx < listBoxDNS.Items.Count)
                    itemsToMove.Add(listBoxDNS.Items[idx]);
            }

            foreach (var idx in indices.OrderByDescending(i => i))
            {
                if (idx >= 0 && idx < listBoxDNS.Items.Count)
                    listBoxDNS.Items.RemoveAt(idx);
            }

            foreach (var item in itemsToMove)
                listBoxShow.Items.Add(item);

            int candidate = maxIndex - count + 1;

            if (listBoxDNS.Items.Count == 0)
            {
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
