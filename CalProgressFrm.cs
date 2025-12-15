//todo: break in row for each step
// insert columns for setpoints

using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{
    public partial class CalProgressFrm : Form
    {
        // Cancellation source for the in-form soak countdown (used by Skip button)
        private CancellationTokenSource _soakCts;

        public CalProgressFrm(List<ListViewItem> selectedProbes, ListViewItem selectedMirror, List<StepClass> steps, bool manual)
        {
            InitializeComponent();

            // Clear probe humidity adjustment points before starting the sequence
            // For each selected ListViewItem, resolve a RotProbe (prefer Tag, fallback to connected snapshot)
            // and call Commands.SendHumidityDeleteAdjust(probe) directly (no reflection).
            if (selectedProbes != null)
            {
                try
                {
                    var connectedProbesSnapshot = Program.GetConnectedProbesSnapshot() ?? new List<RotProbe>();

                    foreach (var item in selectedProbes)
                    {
                        if (item == null) continue;

                        try
                        {
                            RotProbe resolvedProbe = null;

                            // Prefer direct Tag
                            try
                            {
                                if (item.Tag is RotProbe tagProbe)
                                    resolvedProbe = tagProbe;
                            }
                            catch { /* ignore Tag parsing errors */ }

                            // Fallback: try to match against connected probes
                            if (resolvedProbe == null && connectedProbesSnapshot.Count > 0)
                            {
                                var candidates = new List<string>();
                                if (!string.IsNullOrWhiteSpace(item.Text)) candidates.Add(item.Text.Trim());
                                for (int si = 0; si < item.SubItems.Count; si++)
                                {
                                    var st = item.SubItems[si]?.Text;
                                    if (!string.IsNullOrWhiteSpace(st)) candidates.Add(st.Trim());
                                }

                                foreach (var cp in connectedProbesSnapshot)
                                {
                                    if (cp == null) continue;
                                    try
                                    {
                                        if (!string.IsNullOrWhiteSpace(cp.DeviceName) && candidates.Any(c => string.Equals(c, cp.DeviceName, StringComparison.OrdinalIgnoreCase)))
                                        { resolvedProbe = cp; break; }
                                        if (!string.IsNullOrWhiteSpace(cp.SerialNumber) && candidates.Any(c => string.Equals(c, cp.SerialNumber, StringComparison.OrdinalIgnoreCase)))
                                        { resolvedProbe = cp; break; }
                                        if (!string.IsNullOrWhiteSpace(cp.ComPort) && candidates.Any(c => string.Equals(c, cp.ComPort, StringComparison.OrdinalIgnoreCase)))
                                        { resolvedProbe = cp; break; }
                                        if (!string.IsNullOrWhiteSpace(cp.ProbeType) && candidates.Any(c => c.IndexOf(cp.ProbeType, StringComparison.OrdinalIgnoreCase) >= 0))
                                        { resolvedProbe = cp; break; }

                                        var digitsFromItem = string.Empty;
                                        foreach (var c in candidates)
                                        {
                                            var d = Regex.Match(c ?? string.Empty, @"\d+").Value;
                                            if (!string.IsNullOrWhiteSpace(d)) { digitsFromItem = d; break; }
                                        }
                                        if (!string.IsNullOrWhiteSpace(digitsFromItem))
                                        {
                                            if (!string.IsNullOrWhiteSpace(cp.SerialNumber) && cp.SerialNumber.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                            { resolvedProbe = cp; break; }
                                            if (!string.IsNullOrWhiteSpace(cp.DeviceName) && cp.DeviceName.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                            { resolvedProbe = cp; break; }
                                        }
                                    }
                                    catch { /* ignore per-probe match errors */ }
                                }
                            }

                            if (resolvedProbe != null)
                            {
                                try
                                {
                                    // call the command directly (no reflection)
                                    Commands.SendHumidityDeleteAdjust(resolvedProbe);
                                }
                                catch
                                {
                                    // Swallow errors here to avoid blocking UI startup; consider logging if desired.
                                }
                            }
                        }
                        catch { /* ignore per-item errors */ }
                    }
                }
                catch { /* ignore snapshot/iteration errors */ }
            }

            //RunCalSequence(selectedProbes, selectedMirror, steps, manual):

            // Build a lookup of possible mirror identifiers -> display name (display name MUST be Mirror.ID)
            var mirrorLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string singleMirrorName = null;

            // Use live Program.ConnectedMirrors to resolve ListViewItems to Mirror objects (ListViewItem.Tag isn't set by RefreshConnectedMirrors)
            var connectedMirrors = Program.ConnectedMirrors ?? new List<Mirror>();

            if (selectedMirror != null)
            {
                var m = selectedMirror;
                if (m != null)
                {
                    Mirror matchedMirror = null;

                    // 1) If Tag is a Mirror use it directly
                    try
                    {
                        if (m.Tag is Mirror tagMirror)
                            matchedMirror = tagMirror;
                    }
                    catch { /* ignore */ }

                    // 2) Try to match against Program.ConnectedMirrors by comparing common properties to the ListViewItem text/subitems
                    if (matchedMirror == null && connectedMirrors.Count > 0)
                    {
                        // gather textual values from the ListViewItem for matching
                        var itemTexts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(m.Text)) itemTexts.Add(m.Text.Trim());
                        for (int i = 0; i < m.SubItems.Count; i++)
                        {
                            var t = m.SubItems[i]?.Text;
                            if (!string.IsNullOrWhiteSpace(t)) itemTexts.Add(t.Trim());
                        }

                        // Try matching by ID or IDN first
                        foreach (var mo in connectedMirrors)
                        {
                            if (mo == null) continue;
                            if (!string.IsNullOrWhiteSpace(mo.ID) && itemTexts.Any(it => string.Equals(it, mo.ID, StringComparison.OrdinalIgnoreCase)))
                            {
                                matchedMirror = mo;
                                break;
                            }
                            if (!string.IsNullOrWhiteSpace(mo.IDN) && itemTexts.Any(it => string.Equals(it, mo.IDN, StringComparison.OrdinalIgnoreCase)))
                            {
                                matchedMirror = mo;
                                break;
                            }
                        }

                        // If still no match, try contains/digits match
                        if (matchedMirror == null)
                        {
                            foreach (var mo in connectedMirrors)
                            {
                                if (mo == null) continue;
                                // compare item text contains the ID or IDN
                                if (!string.IsNullOrWhiteSpace(mo.ID) && itemTexts.Any(it => it.IndexOf(mo.ID, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    matchedMirror = mo;
                                    break;
                                }
                                if (!string.IsNullOrWhiteSpace(mo.IDN) && itemTexts.Any(it => it.IndexOf(mo.IDN, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    matchedMirror = mo;
                                    break;
                                }

                                // digits-only heuristic (e.g. "RH 123" -> "123")
                                var digitsFromItem = string.Empty;
                                foreach (var it in itemTexts)
                                {
                                    var d = Regex.Match(it ?? string.Empty, @"\d+").Value;
                                    if (!string.IsNullOrWhiteSpace(d))
                                    {
                                        digitsFromItem = d;
                                        break;
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(digitsFromItem))
                                {
                                    if (!string.IsNullOrWhiteSpace(mo.ID) && mo.ID.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        matchedMirror = mo;
                                        break;
                                    }
                                    if (!string.IsNullOrWhiteSpace(mo.IDN) && mo.IDN.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        matchedMirror = mo;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Choose display name: MUST prefer Mirror.ID when matched; fallback to visible text
                    string mirrorDisplayName = null;
                    if (matchedMirror != null)
                    {
                        mirrorDisplayName = !string.IsNullOrWhiteSpace(matchedMirror.ID) ? matchedMirror.ID :
                                            !string.IsNullOrWhiteSpace(matchedMirror.IDN) ? matchedMirror.IDN : m.Text;
                    }
                    else
                    {
                        mirrorDisplayName = m.Text ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(mirrorDisplayName))
                        mirrorDisplayName = m.Text ?? string.Empty;

                    // Populate the lookup with keys that may be used to match probes later.
                    // Keys: Mirror.ID, Mirror.IDN, visible item text, digits-only token, and each subitem text.
                    try
                    {
                        if (matchedMirror != null)
                        {
                            if (!string.IsNullOrWhiteSpace(matchedMirror.ID) && !mirrorLookup.ContainsKey(matchedMirror.ID))
                                mirrorLookup[matchedMirror.ID] = mirrorDisplayName;
                            if (!string.IsNullOrWhiteSpace(matchedMirror.IDN) && !mirrorLookup.ContainsKey(matchedMirror.IDN))
                                mirrorLookup[matchedMirror.IDN] = mirrorDisplayName;
                        }
                    }
                    catch { /* ignore */ }

                    // Add visible text and subitems as candidate keys
                    if (!string.IsNullOrWhiteSpace(m.Text) && !mirrorLookup.ContainsKey(m.Text))
                        mirrorLookup[m.Text] = mirrorDisplayName;
                    for (int i = 0; i < m.SubItems.Count; i++)
                    {
                        var t = m.SubItems[i]?.Text;
                        if (!string.IsNullOrWhiteSpace(t) && !mirrorLookup.ContainsKey(t))
                            mirrorLookup[t] = mirrorDisplayName;

                        // digits-only
                        var d = Regex.Match(t ?? string.Empty, @"\d+").Value;
                        if (!string.IsNullOrWhiteSpace(d) && !mirrorLookup.ContainsKey(d))
                            mirrorLookup[d] = mirrorDisplayName;
                    }

                    // Remember its display name as default
                    singleMirrorName = mirrorDisplayName;
                }
            }

            // Populate grid rows for each selected probe
            if (selectedProbes != null)
            {
                foreach (var p in selectedProbes)
                {
                    if (p == null) continue;

                    // Determine probe display/name
                    string probeName = null;
                    try
                    {
                        if (p.Tag is RotProbe rp)
                        {
                            probeName = !string.IsNullOrWhiteSpace(rp.DeviceName) ? rp.DeviceName :
                                        !string.IsNullOrWhiteSpace(rp.SerialNumber) ? rp.SerialNumber :
                                        !string.IsNullOrWhiteSpace(rp.ComPort) ? rp.ComPort :
                                        rp.ProbeType;
                        }
                    }
                    catch { /* ignore Tag parsing */ }

                    if (string.IsNullOrWhiteSpace(probeName))
                    {
                        probeName = p.Text;
                        // fallback to first non-empty subitem if text is empty
                        if (string.IsNullOrWhiteSpace(probeName))
                        {
                            for (int i = 0; i < p.SubItems.Count; i++)
                            {
                                var t = p.SubItems[i]?.Text;
                                if (!string.IsNullOrWhiteSpace(t))
                                {
                                    probeName = t.Trim();
                                    break;
                                }
                            }
                        }
                    }

                    // Determine mirror name for this probe
                    string mirrorName = null;

                    // If the caller selected exactly one mirror treat it as the mirror for all probes
                    if (selectedMirror != null)
                    {
                        mirrorName = singleMirrorName ?? string.Empty;
                    }
                    else
                    {
                        // Try to find a mirror id in the probe's Tag or subitems by matching mirrorLookup keys
                        try
                        {
                            // Build candidate strings from the probe item
                            var probeCandidates = new List<string>();
                            if (!string.IsNullOrWhiteSpace(p.Text)) probeCandidates.Add(p.Text.Trim());
                            for (int i = 0; i < p.SubItems.Count; i++)
                            {
                                var t = p.SubItems[i]?.Text;
                                if (!string.IsNullOrWhiteSpace(t)) probeCandidates.Add(t.Trim());
                            }

                            // If Tag is RotProbe try some of its properties
                            if (p.Tag is RotProbe rp2)
                            {
                                probeCandidates.Add(rp2.DeviceName);
                                probeCandidates.Add(rp2.SerialNumber);
                                probeCandidates.Add(rp2.ProbeType);
                                probeCandidates.Add(rp2.ComPort);
                            }

                            // Match candidates against mirrorLookup keys
                            foreach (var c in probeCandidates)
                            {
                                if (string.IsNullOrWhiteSpace(c)) continue;

                                // direct lookup
                                if (mirrorLookup.TryGetValue(c, out var val))
                                {
                                    mirrorName = val;
                                    break;
                                }

                                // digits-only
                                var digits = Regex.Match(c, @"\d+").Value;
                                if (!string.IsNullOrWhiteSpace(digits) && mirrorLookup.TryGetValue(digits, out val))
                                {
                                    mirrorName = val;
                                    break;
                                }

                                // contains match
                                foreach (var key in mirrorLookup.Keys)
                                {
                                    if (string.IsNullOrWhiteSpace(key)) continue;
                                    if (c.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        mirrorName = mirrorLookup[key];
                                        break;
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(mirrorName)) break;
                            }
                        }
                        catch { /* ignore */ }
                    }

                    // Add a row with ProbeName and MirrorName filled; remaining columns left blank
                    // Columns now: ProbeName, MirrorName, Step, ProbeTemp, MirrorTemp, TempSetpoint, TempError, TempPass, ProbeHumidity, MirrorHumidity, RHSetpoint, HumdityError, HumdityPass
                    dataGridView1.Rows.Add(new object[]
                    {
                        probeName ?? string.Empty,
                        mirrorName ?? string.Empty,
                        string.Empty, // Step
                        string.Empty, // ProbeTemp
                        string.Empty, // MirrorTemp
                        string.Empty, // Temp Setpoint
                        string.Empty, // TempError
                        string.Empty, // TemperaturePass
                        string.Empty, // ProbeHumidity
                        string.Empty, // MirrorHumdity
                        string.Empty, // RH Setpoint
                        string.Empty, // HumdityError
                        string.Empty  // HumdityPass
                    });
                }
            }

            // Clear any saved humidity adjustment points on each selected probe before starting the sequence.
            // For each selected ListViewItem that has a RotProbe in Tag, call Commands.SendHumidityDeleteAdjust(probe) via reflection.

            RunCalSequenceAsync(steps, selectedProbes, selectedMirror, manual).ConfigureAwait(true);
        }


        /*
        cal routine plan:
        step description and planned actions:
        Global: If manual is true, manually set chamber temperature and humdity, if set to false, NoManual(), do not start cal
        As-Left sequence Step:
            set temp and humidity on chamber
            start soak timer, show countdown for the time in masked text box.
                at end of soak timer
                Stability()
                    for each probe in selectedProbes, compare temperature of probe to temperature of mirror
                        if probe temp engineering unit is set to °F, convert to °C
                    for each probe in selectedProbes, compare humidity of probe to humidity of mirror
                    use value mirror temp - probe temp for temp error, mirror humidity - probe humidity for humidity error
                    take measurement 5 times, each measurement should be inserted into row under the probe it belongs to,
                    or under the last measurment for that probe, "Time:" going into probe name slot, and date time stamp in 
                    Mirror name. ex:
                    COL Probe Name      Mirror Name ....
                        "HC2\Rotprobe   "RH 123"    (null rest of values on line)
                        Time:            11/18/2025 13:23:17     23.00       23.15    0.15 ..
                        Time:
                        Time:
                        worst case                               show measurement with worst temperature error
                        average                                  average of each field
                    measurements should be 15 seconds apart to allow probe data to update from task running in the background.
                for each measurement, if evaluate is set to true for temp or humidity, take accuracy limit from step,
                use to create min/max value from mirror reading and compare to probe reading. if out of range, fail that field.
                update pass/fail column
            move to next step

        As-Found Step Sequence:
            Identical to sequence above, with exception at final measurement:
                execute following serial command to store a setpoint adjustment on the probe:
                    {##*HCA 0;1;0;##.##;}
                        where ## is probe address, * is probe type code (typically "F"), 0 is fixed, the first 1
                        means measure against reference instrument (mirror), the second 0 temporarily saves the value in the probe.
                        to form command, use probe class to get com port, address and probe type code for the selected probe,
                        use mirror humidity for ##.## value. form com command string and send via serial port to probe.
        Temperature Adj Sequence:
              TBI: basic sequence will be use 4 temperature values, including 0°C as setpoint in chamber and fixed humdity value such as 50% in chamber
              use probe temperature and mirror temperature to determine error
              use four temp points to determine new A,B,C and R0 constants for probe. Save constants somewhere for later use
              I need to research rotronic documentation before I can fully implement this.
        FinalAdj Sequence Step:
            have user review data. prompt to update humdity probe with adjustment values
            if update is selected, execute {*##HCA 0;1;1;;}, if user chooses to discard update, execute {*##HCA 0;1;3;;}
            TODO: research
            if temperature adj sequence has been ran, prompt to update with new constants
            execute series of *##EWR commands to write temp constants into probe memory...
            todos: understand 4 point curve equation and how to derive new constants, how to write constants into 4 byte sequence
            in order to write back into memory using ewr command with 000,000,000,000 byte values
        Final
            review calibration data
            save data from datagridview as xlsx.
            generate calibration report in pdf format
            
            

        */
        //suggested helper methods:
        private void NoManual()
        {
            MessageBox.Show("Not yet implemented");
            return;
        }

        private void Stability()
        {
            MessageBox.Show("Verify Probe and Mirror are stable");
            return;
            //TODO: automate
        }

        private void buttonSkip_Click(object sender, EventArgs e)
        {
            // When the in-form soak countdown is running, Skip cancels the wait.
            try
            {
                _soakCts?.Cancel();
            }
            catch { /* ignore */ }
        }
        public async Task RunCalSequenceAsync(List<StepClass> stepsList, List<ListViewItem> selectedProbes, ListViewItem selectedMirror, bool manual)
        {
            if (stepsList == null) return;

            // Confirm with user before proceeding (may lose unsaved adjustments)
            for (int idx = 0; idx < stepsList.Count; idx++)
            {
                var step = stepsList[idx];
                if (step == null) continue;

                if (string.Equals(step.Steps, "As-Left", StringComparison.OrdinalIgnoreCase))
                {
                    // pass numeric step number (1-based)
                    await ExecuteVerStepAsync(step, idx + 1, stepsList.Count, selectedProbes, selectedMirror, manual).ConfigureAwait(true);
                }
                else if (string.Equals(step.Steps, "As-Found", StringComparison.OrdinalIgnoreCase))
                {
                    await ExecuteAdjStepAsync(step, idx + 1, stepsList.Count, selectedProbes, selectedMirror, manual).ConfigureAwait(true);
                }
                else return;
            }
        }

        private async Task ExecuteAdjStepAsync(StepClass step, int stepNumber, int totalSteps, List<ListViewItem> selectedProbes, ListViewItem selectedMirror, bool manual)
        {

            /*
             * 
             * this method should be nearly identical to the ExecuteVerStepAsync method with these exceptions:
                If evalHumidity is checked, at the end of the the five measurements, run this command befor commencing next step: 
                Commands.SendHumidityTestPointSave(probe, mirror); for each probe in selectedProbes using the selectedMirror

             */

            // Implementation mirrors ExecuteVerStepAsync with the additional humidity-save call per-probe when step.EvalHumidity == true.

            if (!manual)
            {
                NoManual();
                return;
            }

            // Parse soak time
            TimeSpan soak = TimeSpan.Zero;
            if (!string.IsNullOrWhiteSpace(step?.SoakTime))
            {
                if (!TimeSpan.TryParse(step.SoakTime, out soak))
                {
                    if (double.TryParse(step.SoakTime, NumberStyles.Any, CultureInfo.InvariantCulture, out var mins))
                        soak = TimeSpan.FromMinutes(mins);
                }
            }

            if (soak.TotalSeconds > 0)
            {
                try
                {
                    _soakCts?.Cancel();
                    _soakCts = new CancellationTokenSource();
                    var token = _soakCts.Token;
                    try { buttonSkip.Enabled = true; } catch { }

                    try
                    {
                        await RunSoakCountdownAsync(soak, token).ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                        // Skip requested
                    }
                    finally
                    {
                        try
                        {
                            buttonSkip.Enabled = false;
                            maskedTextBoxTime.Text = string.Empty;
                        }
                        catch { }
                        _soakCts?.Dispose();
                        _soakCts = null;
                    }
                }
                catch { }
            }

            if (selectedProbes == null || selectedProbes.Count == 0)
            {
                MessageBox.Show("No probes selected for adjustment step.");
                return;
            }

            // Resolve mirror object if present
            Mirror mirrorObj = null;
            try
            {
                if (selectedMirror != null && selectedMirror.Tag is Mirror mtag)
                    mirrorObj = mtag;
            }
            catch { }

            if (mirrorObj == null)
            {
                mirrorObj = Program.ConnectedMirrors?.FirstOrDefault();
            }

            var connectedProbesSnapshot = Program.GetConnectedProbesSnapshot() ?? new List<RotProbe>();

            // Build resolved selections
            var resolvedSelections = new List<(ListViewItem Item, RotProbe Probe)>(selectedProbes.Count);
            foreach (var lv in selectedProbes)
            {
                if (lv == null)
                {
                    resolvedSelections.Add((lv, null));
                    continue;
                }

                RotProbe resolvedProbe = null;
                try
                {
                    if (lv.Tag is RotProbe t0)
                        resolvedProbe = t0;
                }
                catch { }

                if (resolvedProbe == null && connectedProbesSnapshot.Count > 0)
                {
                    var candidates = new List<string>();
                    if (!string.IsNullOrWhiteSpace(lv.Text)) candidates.Add(lv.Text.Trim());
                    for (int si = 0; si < lv.SubItems.Count; si++)
                    {
                        var st = lv.SubItems[si]?.Text;
                        if (!string.IsNullOrWhiteSpace(st)) candidates.Add(st.Trim());
                    }

                    foreach (var cp in connectedProbesSnapshot)
                    {
                        if (cp == null) continue;
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(cp.DeviceName) && candidates.Any(c => string.Equals(c, cp.DeviceName, StringComparison.OrdinalIgnoreCase)))
                            {
                                resolvedProbe = cp; break;
                            }
                            if (!string.IsNullOrWhiteSpace(cp.SerialNumber) && candidates.Any(c => string.Equals(c, cp.SerialNumber, StringComparison.OrdinalIgnoreCase)))
                            {
                                resolvedProbe = cp; break;
                            }
                            if (!string.IsNullOrWhiteSpace(cp.ComPort) && candidates.Any(c => string.Equals(c, cp.ComPort, StringComparison.OrdinalIgnoreCase)))
                            {
                                resolvedProbe = cp; break;
                            }
                            if (!string.IsNullOrWhiteSpace(cp.ProbeType) && candidates.Any(c => c.IndexOf(cp.ProbeType, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                resolvedProbe = cp; break;
                            }

                            var digitsFromItem = string.Empty;
                            foreach (var c in candidates)
                            {
                                var d = Regex.Match(c ?? string.Empty, @"\d+").Value;
                                if (!string.IsNullOrWhiteSpace(d)) { digitsFromItem = d; break; }
                            }
                            if (!string.IsNullOrWhiteSpace(digitsFromItem))
                            {
                                if (!string.IsNullOrWhiteSpace(cp.SerialNumber) && cp.SerialNumber.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    resolvedProbe = cp; break;
                                }
                                if (!string.IsNullOrWhiteSpace(cp.DeviceName) && cp.DeviceName.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    resolvedProbe = cp; break;
                                }
                            }
                        }
                        catch { }
                    }
                }

                resolvedSelections.Add((lv, resolvedProbe));
            }

            // Local InsertRowUnderHeader (UI-thread safe)
            int InsertRowUnderHeader(string probeDisplayName, object[] rowValues, int insertedCount)
            {
                try
                {
                    if (dataGridView1 == null) return insertedCount;

                    if (dataGridView1.InvokeRequired)
                    {
                        var obj = dataGridView1.Invoke(new Func<int>(() => InsertRowUnderHeader(probeDisplayName, rowValues, insertedCount)));
                        if (obj is int ri) return ri;
                        return insertedCount;
                    }

                    int headerIndex = FindProbeRowIndex(probeDisplayName);
                    if (headerIndex < 0)
                    {
                        dataGridView1.Rows.Add(rowValues);
                        return insertedCount + 1;
                    }

                    int insertIndex = headerIndex + 1 + insertedCount;
                    if (insertIndex < 0 || insertIndex > dataGridView1.Rows.Count)
                        insertIndex = dataGridView1.Rows.Count;

                    dataGridView1.Rows.Insert(insertIndex, rowValues);
                    return insertedCount + 1;
                }
                catch
                {
                    try
                    {
                        if (dataGridView1 != null && !dataGridView1.IsDisposed && !dataGridView1.Disposing)
                        {
                            if (dataGridView1.InvokeRequired)
                                dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(rowValues)));
                            else
                                dataGridView1.Rows.Add(rowValues);

                            return insertedCount + 1;
                        }
                    }
                    catch { }

                    return insertedCount;
                }
            }

            var probeTasks = new List<Task>();
            foreach (var entry in resolvedSelections)
            {
                var lv = entry.Item;
                var probeTag = entry.Probe;

                if (lv == null)
                    continue;

                // Compute display name
                string probeDisplayName = null;
                if (probeTag != null)
                {
                    probeDisplayName = !string.IsNullOrWhiteSpace(probeTag.DeviceName) ? probeTag.DeviceName :
                                       !string.IsNullOrWhiteSpace(probeTag.SerialNumber) ? probeTag.SerialNumber :
                                       !string.IsNullOrWhiteSpace(probeTag.ComPort) ? probeTag.ComPort :
                                       probeTag.ProbeType;
                }
                if (string.IsNullOrWhiteSpace(probeDisplayName))
                    probeDisplayName = lv.Text ?? string.Empty;

                int headerIndex = FindProbeRowIndex(probeDisplayName);
                if (headerIndex < 0)
                {
                    try
                    {
                        var headerRow = new object[]
                        {
                            probeDisplayName,
                            mirrorObj?.ID ?? string.Empty,
                            stepNumber.ToString(),
                            string.Empty,
                            string.Empty,
                            FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                            string.Empty,
                            string.Empty
                        };

                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(headerRow)));
                        }
                        else
                        {
                            dataGridView1.Rows.Add(headerRow);
                        }
                    }
                    catch { }
                }

                if (probeTag == null)
                {
                    // Insert an info row synchronously to indicate missing tag
                    var infoRow = new object[]
                    {
                        "Time:",
                        DateTime.Now.ToString("g", CultureInfo.InvariantCulture),
                        stepNumber.ToString(),
                        "Probe missing Tag (not resolved)",
                        string.Empty,
                        FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                        string.Empty,
                        "FAIL",
                        string.Empty,
                        string.Empty,
                        FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                        string.Empty,
                        "FAIL"
                    };
                    int dummy = 0;
                    dummy = InsertRowUnderHeader(probeDisplayName, infoRow, dummy);

                    // Insert a blank separator row for readability
                    var blankRow = new object[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
                    dummy = InsertRowUnderHeader(probeDisplayName, blankRow, dummy);

                    continue;
                }

                probeTasks.Add(Task.Run(async () =>
                {
                    int insertedCount = 0;
                    try
                    {
                        var tempMeasurements = new List<double>();
                        var tempErrors = new List<double>();
                        var humMeasurements = new List<double>();
                        var humErrors = new List<double>();

                        for (int i = 0; i < 5; i++)
                        {
                            double probeTemp = probeTag.Temperature;
                            string probeTempUnit = probeTag.TemperatureUnit ?? string.Empty;
                            if (IsFahrenheitUnit(probeTempUnit))
                                probeTemp = ConvertFahrenheitToCelsius(probeTemp);

                            double probeHum = probeTag.Humidity;

                            double mirrorTemp = mirrorObj?.MirrorTemp ?? double.NaN;
                            double mirrorHum = mirrorObj?.Humdity ?? double.NaN;

                            double tempError = (double.IsNaN(mirrorTemp) ? double.NaN : mirrorTemp - probeTemp);
                            double humError = (double.IsNaN(mirrorHum) ? double.NaN : mirrorHum - probeHum);

                            tempMeasurements.Add(probeTemp);
                            tempErrors.Add(tempError);
                            humMeasurements.Add(probeHum);
                            humErrors.Add(humError);

                            // Evaluate pass/fail or "No Eval"
                            bool tempPass = true;
                            bool humPass = true;
                            string tempResult;
                            string humResult;

                            if (step != null && step.EvalTemp && !double.IsNaN(mirrorTemp))
                            {
                                double allowedMin = mirrorTemp + step.MinTemperature;
                                double allowedMax = mirrorTemp + step.MaxTemperature;
                                tempPass = probeTemp >= allowedMin && probeTemp <= allowedMax;
                                tempResult = tempPass ? "PASS" : "FAIL";
                            }
                            else
                            {
                                tempResult = "No Eval";
                            }

                            if (step != null && step.EvalHumidity && !double.IsNaN(mirrorHum))
                            {
                                double allowedMin = mirrorHum + step.MinHumidity;
                                double allowedMax = mirrorHum + step.MaxHumidity;
                                humPass = probeHum >= allowedMin && probeHum <= allowedMax;
                                humResult = humPass ? "PASS" : "FAIL";
                            }
                            else
                            {
                                humResult = "No Eval";
                            }

                            var row = new object[]
                            {
                                "Time:",
                                DateTime.Now.ToString("g", CultureInfo.InvariantCulture),
                                stepNumber.ToString(),
                                FormatDouble(probeTemp),
                                FormatDouble(mirrorTemp),
                                FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                                FormatDouble(tempError),
                                tempResult,
                                FormatDouble(probeHum),
                                FormatDouble(mirrorHum),
                                FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                                FormatDouble(humError),
                                humResult
                            };

                            insertedCount = InsertRowUnderHeader(probeDisplayName, row, insertedCount);

                            // Wait 15 seconds unless last iteration
                            if (i < 4)
                                await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                        }

                        // After measurements: compute and insert Worst case and Average rows
                        if (tempErrors.Count > 0)
                        {
                            double worstTempError = tempErrors.Where(d => !double.IsNaN(d)).Select(Math.Abs).DefaultIfEmpty(0).Max();
                            double avgProbeTemp = tempMeasurements.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average();
                            double avgMirrorTemp = mirrorObj == null ? double.NaN : mirrorObj.MirrorTemp;

                            var worstRow = new object[]
                            {
                                "Worst case Temp:",
                                string.Empty,
                                stepNumber.ToString(),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                FormatDoubleSign(worstTempError),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, worstRow, insertedCount);

                            var avgRow = new object[]
                            {
                                "Average Temp:",
                                string.Empty,
                                stepNumber.ToString(),
                                FormatDouble(avgProbeTemp),
                                FormatDouble(avgMirrorTemp),
                                FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                                FormatDouble(tempErrors.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average()),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, avgRow, insertedCount);
                        }

                        if (humErrors.Count > 0)
                        {
                            double worstHumError = humErrors.Where(d => !double.IsNaN(d)).Select(Math.Abs).DefaultIfEmpty(0).Max();
                            double avgProbeHum = humMeasurements.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average();
                            double avgMirrorHum = mirrorObj == null ? double.NaN : mirrorObj.Humdity;

                            var worstHumRow = new object[]
                            {
                                "Worst case (RH):",
                                string.Empty,
                                stepNumber.ToString(),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                FormatDoubleSign(worstHumError),
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, worstHumRow, insertedCount);

                            var avgHumRow = new object[]
                            {
                                "Average (RH):",
                                string.Empty,
                                stepNumber.ToString(),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                FormatDouble(avgProbeHum),
                                FormatDouble(avgMirrorHum),
                                FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                                FormatDouble(humErrors.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average()),
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, avgHumRow, insertedCount);
                        }

                        // If this adjustment step requires writing humidity setpoint, attempt to call Commands.SendHumidityTestPointSave(probe, mirror)
                        if (step != null && step.EvalHumidity)
                        {
                            try
                            {
                                Commands.SendHumidityTestPointSave(probeTag, mirrorObj);
                            }
                            catch
                            {
                                // swallow; we already logged measurement results to grid
                            }
                        }

                        // Insert a blank separator row for readability at end of this probe's step block
                        var blank = new object[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
                        insertedCount = InsertRowUnderHeader(probeDisplayName, blank, insertedCount);
                    }
                    catch (Exception ex)
                    {
                        // Insert an error row if something goes wrong for this probe
                        var errorRow = new object[] {
                            "Time:",
                            DateTime.Now.ToString("g", CultureInfo.InvariantCulture),
                            stepNumber.ToString(),
                            $"Measurement error: {ex.Message}",
                            string.Empty,
                            FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                            string.Empty,
                            "FAIL",
                            string.Empty,
                            string.Empty,
                            FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                            string.Empty,
                            "FAIL"
                        };
                        int dummy = 0;
                        dummy = InsertRowUnderHeader(probeTag?.DeviceName ?? probeTag?.SerialNumber ?? probeTag?.ComPort ?? probeTag?.ProbeType ?? lv.Text ?? string.Empty, errorRow, dummy);
                    }
                }));
            }

            // Await all probe tasks to complete
            try
            {
                await Task.WhenAll(probeTasks).ConfigureAwait(true);
            }
            catch
            {
                // swallow; individual tasks insert their own error rows
            }
        }

        // Adds ExecuteVerStepAsync implementation

        private async Task ExecuteVerStepAsync(StepClass step, int stepNumber, int totalSteps, List<ListViewItem> selectedProbes, ListViewItem selectedMirror, bool manual)
        {
            // Respect manual mode contract
            if (!manual)
            {
                NoManual();
                return;
            }

            // Parse soak time
            TimeSpan soak = TimeSpan.Zero;
            if (!string.IsNullOrWhiteSpace(step?.SoakTime))
            {
                if (!TimeSpan.TryParse(step.SoakTime, out soak))
                {
                    if (double.TryParse(step.SoakTime, NumberStyles.Any, CultureInfo.InvariantCulture, out var mins))
                        soak = TimeSpan.FromMinutes(mins);
                }
            }

            // In-form soak countdown (cancellable)
            if (soak.TotalSeconds > 0)
            {
                try
                {
                    _soakCts?.Cancel();
                    _soakCts = new CancellationTokenSource();
                    var token = _soakCts.Token;
                    try { buttonSkip.Enabled = true; } catch { }

                    try
                    {
                        await RunSoakCountdownAsync(soak, token).ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                        // Skip requested
                    }
                    finally
                    {
                        try
                        {
                            buttonSkip.Enabled = false;
                            maskedTextBoxTime.Text = string.Empty;
                        }
                        catch { }
                        _soakCts?.Dispose();
                        _soakCts = null;
                    }
                }
                catch { }
            }

            // TODO: Stability();

            if (selectedProbes == null || selectedProbes.Count == 0)
            {
                MessageBox.Show("No probes selected for verification step.");
                return;
            }

            // Resolve mirror object if present
            Mirror mirrorObj = null;
            try
            {
                if (selectedMirror != null && selectedMirror.Tag is Mirror mtag)
                    mirrorObj = mtag;
            }
            catch { }

            if (mirrorObj == null)
            {
                mirrorObj = Program.ConnectedMirrors?.FirstOrDefault();
            }

            var connectedProbesSnapshot = Program.GetConnectedProbesSnapshot() ?? new List<RotProbe>();

            // Build resolved selections
            var resolvedSelections = new List<(ListViewItem Item, RotProbe Probe)>(selectedProbes.Count);
            foreach (var lv in selectedProbes)
            {
                if (lv == null)
                {
                    resolvedSelections.Add((lv, null));
                    continue;
                }

                RotProbe resolvedProbe = null;
                try
                {
                    if (lv.Tag is RotProbe t0)
                        resolvedProbe = t0;
                }
                catch { }

                if (resolvedProbe == null && connectedProbesSnapshot.Count > 0)
                {
                    var candidates = new List<string>();
                    if (!string.IsNullOrWhiteSpace(lv.Text)) candidates.Add(lv.Text.Trim());
                    for (int si = 0; si < lv.SubItems.Count; si++)
                    {
                        var st = lv.SubItems[si]?.Text;
                        if (!string.IsNullOrWhiteSpace(st)) candidates.Add(st.Trim());
                    }

                    foreach (var cp in connectedProbesSnapshot)
                    {
                        if (cp == null) continue;
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(cp.DeviceName) && candidates.Any(c => string.Equals(c, cp.DeviceName, StringComparison.OrdinalIgnoreCase)))
                            {
                                resolvedProbe = cp; break;
                            }
                            if (!string.IsNullOrWhiteSpace(cp.SerialNumber) && candidates.Any(c => string.Equals(c, cp.SerialNumber, StringComparison.OrdinalIgnoreCase)))
                            {
                                resolvedProbe = cp; break;
                            }
                            if (!string.IsNullOrWhiteSpace(cp.ComPort) && candidates.Any(c => string.Equals(c, cp.ComPort, StringComparison.OrdinalIgnoreCase)))
                            {
                                resolvedProbe = cp; break;
                            }
                            if (!string.IsNullOrWhiteSpace(cp.ProbeType) && candidates.Any(c => c.IndexOf(cp.ProbeType, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                resolvedProbe = cp; break;
                            }

                            var digitsFromItem = string.Empty;
                            foreach (var c in candidates)
                            {
                                var d = Regex.Match(c ?? string.Empty, @"\d+").Value;
                                if (!string.IsNullOrWhiteSpace(d)) { digitsFromItem = d; break; }
                            }
                            if (!string.IsNullOrWhiteSpace(digitsFromItem))
                            {
                                if (!string.IsNullOrWhiteSpace(cp.SerialNumber) && cp.SerialNumber.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    resolvedProbe = cp; break;
                                }
                                if (!string.IsNullOrWhiteSpace(cp.DeviceName) && cp.DeviceName.IndexOf(digitsFromItem, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    resolvedProbe = cp; break;
                                }
                            }
                        }
                        catch { }
                    }
                }

                resolvedSelections.Add((lv, resolvedProbe));
            }

            // Helper to insert a row under a probe header safely on UI thread.
            // Returns the updated insertedCount (previous + 1 on success, otherwise unchanged).

            int InsertRowUnderHeader(string probeDisplayName, object[] rowValues, int insertedCount)
            {
                try
                {
                    if (dataGridView1 == null) return insertedCount;

                    if (dataGridView1.InvokeRequired)
                    {
                        // Invoke on UI thread and get result
                        var obj = dataGridView1.Invoke(new Func<int>(() => InsertRowUnderHeader(probeDisplayName, rowValues, insertedCount)));
                        if (obj is int ri) return ri;
                        return insertedCount;
                    }

                    int headerIndex = FindProbeRowIndex(probeDisplayName);
                    if (headerIndex < 0)
                    {
                        // As a fallback, append at the end
                        dataGridView1.Rows.Add(rowValues);
                        return insertedCount + 1;
                    }

                    int insertIndex = headerIndex + 1 + insertedCount;
                    // Clamp insertIndex
                    if (insertIndex < 0 || insertIndex > dataGridView1.Rows.Count)
                        insertIndex = dataGridView1.Rows.Count;

                    dataGridView1.Rows.Insert(insertIndex, rowValues);
                    return insertedCount + 1;
                }
                catch
                {
                    // If UI insertion fails, try to append
                    try
                    {
                        if (dataGridView1 != null && !dataGridView1.IsDisposed && !dataGridView1.Disposing)
                        {
                            if (dataGridView1.InvokeRequired)
                                dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(rowValues)));
                            else
                                dataGridView1.Rows.Add(rowValues);

                            return insertedCount + 1;
                        }
                    }
                    catch { }

                    return insertedCount;
                }
            }

            // Ensure headers exist, insert missing-probe info rows synchronously, and start per-probe tasks
            var probeTasks = new List<Task>();
            foreach (var entry in resolvedSelections)
            {
                var lv = entry.Item;
                var probeTag = entry.Probe;

                if (lv == null)
                    continue;

                // Compute display name
                string probeDisplayName = null;
                if (probeTag != null)
                {
                    probeDisplayName = !string.IsNullOrWhiteSpace(probeTag.DeviceName) ? probeTag.DeviceName :
                                       !string.IsNullOrWhiteSpace(probeTag.SerialNumber) ? probeTag.SerialNumber :
                                       !string.IsNullOrWhiteSpace(probeTag.ComPort) ? probeTag.ComPort :
                                       probeTag.ProbeType;
                }
                if (string.IsNullOrWhiteSpace(probeDisplayName))
                    probeDisplayName = lv.Text ?? string.Empty;

                // Ensure header row exists
                int headerIndex = FindProbeRowIndex(probeDisplayName);
                if (headerIndex < 0)
                {
                    // Must marshal to UI thread to add header
                    try
                    {
                        var headerRow = new object[]
                        {
                            probeDisplayName,
                            mirrorObj?.ID ?? string.Empty,
                            stepNumber.ToString(), // Step (numeric)
                            string.Empty, // ProbeTemp
                            string.Empty, // MirrorTemp
                            FormatDouble(step?.TemperatureSetPoint ?? double.NaN), // TemperatureSetpoint
                            string.Empty, // TempError
                            string.Empty, // TemperaturePass
                            string.Empty, // ProbeHumidity
                            string.Empty, // MirrorHumdity
                            FormatDouble(step?.HumiditySetPoint ?? double.NaN), // HumiditySetpoint
                            string.Empty, // HumdityError
                            string.Empty  // HumdityPass
                        };

                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(headerRow)));
                        }
                        else
                        {
                            dataGridView1.Rows.Add(headerRow);
                        }
                    }
                    catch { }
                }

                if (probeTag == null)
                {
                    // Insert an info row synchronously to indicate missing tag
                    var infoRow = new object[]
                    {
                        "Time:",
                        DateTime.Now.ToString("g", CultureInfo.InvariantCulture),
                        stepNumber.ToString(),
                        "Probe missing Tag (not resolved)",
                        string.Empty,
                        FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                        string.Empty,
                        "FAIL",
                        string.Empty,
                        string.Empty,
                        FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                        string.Empty,
                        "FAIL"
                    };
                    int dummy = 0;
                    dummy = InsertRowUnderHeader(probeDisplayName, infoRow, dummy);

                    // Insert a blank separator row for readability
                    var blankRow = new object[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
                    dummy = InsertRowUnderHeader(probeDisplayName, blankRow, dummy);

                    continue;
                }

                // Start an independent task for this probe's measurements
                probeTasks.Add(Task.Run(async () =>
                {
                    int insertedCount = 0; // counts rows inserted under this header
                    try
                    {
                        var tempMeasurements = new List<double>();
                        var tempErrors = new List<double>();
                        var humMeasurements = new List<double>();
                        var humErrors = new List<double>();

                        for (int i = 0; i < 5; i++)
                        {
                            // Capture probe and mirror snapshot values
                            double probeTemp = probeTag.Temperature;
                            string probeTempUnit = probeTag.TemperatureUnit ?? string.Empty;
                            if (IsFahrenheitUnit(probeTempUnit))
                                probeTemp = ConvertFahrenheitToCelsius(probeTemp);

                            double probeHum = probeTag.Humidity;

                            double mirrorTemp = mirrorObj?.MirrorTemp ?? double.NaN;
                            double mirrorHum = mirrorObj?.Humdity ?? double.NaN;

                            double tempError = (double.IsNaN(mirrorTemp) ? double.NaN : mirrorTemp - probeTemp);
                            double humError = (double.IsNaN(mirrorHum) ? double.NaN : mirrorHum - probeHum);

                            tempMeasurements.Add(probeTemp);
                            tempErrors.Add(tempError);
                            humMeasurements.Add(probeHum);
                            humErrors.Add(humError);

                            // Evaluate pass/fail or "No Eval"
                            bool tempPass = true;
                            bool humPass = true;
                            string tempResult;
                            string humResult;

                            if (step != null && step.EvalTemp && !double.IsNaN(mirrorTemp))
                            {
                                double allowedMin = mirrorTemp + step.MinTemperature;
                                double allowedMax = mirrorTemp + step.MaxTemperature;
                                tempPass = probeTemp >= allowedMin && probeTemp <= allowedMax;
                                tempResult = tempPass ? "PASS" : "FAIL";
                            }
                            else
                            {
                                tempResult = "No Eval";
                            }

                            if (step != null && step.EvalHumidity && !double.IsNaN(mirrorHum))
                            {
                                double allowedMin = mirrorHum + step.MinHumidity;
                                double allowedMax = mirrorHum + step.MaxHumidity;
                                humPass = probeHum >= allowedMin && probeHum <= allowedMax;
                                humResult = humPass ? "PASS" : "FAIL";
                            }
                            else
                            {
                                humResult = "No Eval";
                            }

                            var row = new object[]
                            {
                                "Time:",
                                DateTime.Now.ToString("g", CultureInfo.InvariantCulture),
                                stepNumber.ToString(),
                                FormatDouble(probeTemp),
                                FormatDouble(mirrorTemp),
                                FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                                FormatDouble(tempError),
                                tempResult,
                                FormatDouble(probeHum),
                                FormatDouble(mirrorHum),
                                FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                                FormatDouble(humError),
                                humResult
                            };

                            insertedCount = InsertRowUnderHeader(probeDisplayName, row, insertedCount);

                            // Wait 15 seconds unless last iteration
                            if (i < 4)
                                await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                        }

                        // After measurements: compute and insert Worst case and Average rows
                        if (tempErrors.Count > 0)
                        {
                            double worstTempError = tempErrors.Where(d => !double.IsNaN(d)).Select(Math.Abs).DefaultIfEmpty(0).Max();
                            double avgProbeTemp = tempMeasurements.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average();
                            double avgMirrorTemp = mirrorObj == null ? double.NaN : mirrorObj.MirrorTemp;

                            var worstRow = new object[]
                            {
                                "Worst case Temp:",
                                string.Empty,
                                stepNumber.ToString(),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                FormatDoubleSign(worstTempError),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, worstRow, insertedCount);

                            var avgRow = new object[]
                            {
                                "Average Temp:",
                                string.Empty,
                                stepNumber.ToString(),
                                FormatDouble(avgProbeTemp),
                                FormatDouble(avgMirrorTemp),
                                FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                                FormatDouble(tempErrors.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average()),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, avgRow, insertedCount);
                        }

                        if (humErrors.Count > 0)
                        {
                            double worstHumError = humErrors.Where(d => !double.IsNaN(d)).Select(Math.Abs).DefaultIfEmpty(0).Max();
                            double avgProbeHum = humMeasurements.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average();
                            double avgMirrorHum = mirrorObj == null ? double.NaN : mirrorObj.Humdity;

                            var worstHumRow = new object[]
                            {
                                "Worst case (RH):",
                                string.Empty,
                                stepNumber.ToString(),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                FormatDoubleSign(worstHumError),
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, worstHumRow, insertedCount);

                            var avgHumRow = new object[]
                            {
                                "Average (RH):",
                                string.Empty,
                                stepNumber.ToString(),
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                FormatDouble(avgProbeHum),
                                FormatDouble(avgMirrorHum),
                                FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                                FormatDouble(humErrors.Where(d => !double.IsNaN(d)).DefaultIfEmpty(0).Average()),
                                string.Empty
                            };
                            insertedCount = InsertRowUnderHeader(probeDisplayName, avgHumRow, insertedCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Insert an error row if something goes wrong for this probe
                        var errorRow = new object[] {
                            "Time:",
                            DateTime.Now.ToString("g", CultureInfo.InvariantCulture),
                            stepNumber.ToString(),
                            $"Measurement error: {ex.Message}",
                            string.Empty,
                            FormatDouble(step?.TemperatureSetPoint ?? double.NaN),
                            string.Empty,
                            "FAIL",
                            string.Empty,
                            string.Empty,
                            FormatDouble(step?.HumiditySetPoint ?? double.NaN),
                            string.Empty,
                            "FAIL"
                        };
                        int dummy = 0;
                        dummy = InsertRowUnderHeader(probeTag?.DeviceName ?? probeTag?.SerialNumber ?? probeTag?.ComPort ?? probeTag?.ProbeType ?? lv.Text ?? string.Empty, errorRow, dummy);
                    }
                }));
            }

            // Await all probe tasks to complete
            try
            {
                await Task.WhenAll(probeTasks).ConfigureAwait(true);
            }
            catch
            {
                // swallow; individual tasks insert their own error rows
            }
        }

        // Helper: find the first row whose first cell text equals probeName (case-insensitive).
        private int FindProbeRowIndex(string probeName)
        {
            if (string.IsNullOrWhiteSpace(probeName)) return -1;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                try
                {
                    var cell = dataGridView1.Rows[i].Cells[0]?.Value as string;
                    if (!string.IsNullOrWhiteSpace(cell) && string.Equals(cell.Trim(), probeName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return i;
                }
                catch { /* ignore */ }
            }
            return -1;
        }

        // Helper: detect Fahrenheit units heuristically
        private bool IsFahrenheitUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return false;
            unit = unit.Trim().ToUpperInvariant();
            return unit.Contains("F") || unit.Contains("°F") || unit.Contains("FAHRENHEIT");
        }

        private double ConvertFahrenheitToCelsius(double f)
        {
            return (f - 32.0) * 5.0 / 9.0;
        }

        private string FormatDouble(double value)
        {
            if (double.IsNaN(value)) return string.Empty;
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        private string FormatDoubleSign(double value)
        {
            if (double.IsNaN(value)) return string.Empty;
            return (value >= 0 ? "+" : "-") + Math.Abs(value).ToString("F2", CultureInfo.InvariantCulture);
        }

        // Runs an in-form countdown, updating maskedTextBoxTime once per second.
        private async Task RunSoakCountdownAsync(TimeSpan duration, CancellationToken token)
        {
            var end = DateTime.UtcNow + duration;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var remaining = end - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    // final update to 00:00:00
                    UpdateMaskedTimeOnUi(TimeSpan.Zero);
                    return;
                }

                UpdateMaskedTimeOnUi(remaining);

                // Delay 1s and observe cancellation
                try
                {
                    await Task.Delay(1000, token).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    token.ThrowIfCancellationRequested();
                }
            }
        }

        // Safe UI update helper for maskedTextBoxTime (guards InvokeRequired)
        private void UpdateMaskedTimeOnUi(TimeSpan ts)
        {
            try
            {
                if (maskedTextBoxTime == null) return;
                var txt = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
                if (maskedTextBoxTime.InvokeRequired)
                {
                    maskedTextBoxTime.BeginInvoke(new Action(() => maskedTextBoxTime.Text = txt));
                }
                else
                {
                    maskedTextBoxTime.Text = txt;
                }
            }
            catch { /* ignore UI update errors */ }
        }
    }
}