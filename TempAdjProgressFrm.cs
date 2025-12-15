using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;

namespace Rotronic
{
    public partial class TempAdjProgressFrm : Form
    {

        // - Workflow:
        //     * User selects probes and mirror (from main UI).
        //     * For each test point: soak then take multiple samples (mirror temp, probe temp, resistance).
        //     * For coefficient calculation we assume a fixed chamber humidity (10% for these runs).
        //     * We fit the simplified Callendar-Van Dusen polynomial for PT100:
        //         R(T) = R0 * (1 + A*T + B*T^2)      (we ignore the cubic/low-temp terms)
        //       which rearranges to:
        //         y = A*T + B*T^2    where y = R/R0 - 1
        //     * We perform a linear least-squares fit for A and B from the collected (T,R) pairs.
        //     * Only samples within chamber range [0,50] °C are used for the fit.
        // - After fitting we display computed A, B and offset and offer to write A, B and offset to the devices.

        // --- state ---
        private readonly List<ListViewItem> _selectedProbeItems;
        private readonly ListViewItem _selectedMirrorItem;
        private readonly bool _manualMode;

        // mapped runtime objects (matched from Program snapshot)
        private readonly List<RotProbe> _probes = new List<RotProbe>();
        private object _mirrorObj = null; // Mirror (unknown compile-time shape) instance

        // timers
        private System.Windows.Forms.Timer _soakTimer;
        private System.Windows.Forms.Timer _sampleTimer;

        // soak/sample settings
        private TimeSpan _soakRemaining;
        private readonly TimeSpan _soakInitial = TimeSpan.FromMinutes(15); // comment suggested 15 minutes
        private readonly int _sampleCount = 5;
        //todo: change timer back to 15 seconds after testing
        private readonly int _sampleIntervalSec = 1;

        // sample bookkeeping: probe -> list of mirror/probe pairs
        // EXPANDED: include probe temperature count (stored as counts/1000.0)
        private readonly Dictionary<RotProbe, List<(double mirrorTemp, double probeTemp, double resistance, double probeCount)>> _samples =
            new Dictionary<RotProbe, List<(double, double, double, double)>>();

        private readonly Dictionary<RotProbe, List<(double mirrorTemp, double probeTemp, double resistance, double probeCount)>> _averages =
            new Dictionary<RotProbe, List<(double, double, double, double)>>();



        private int _samplesTaken = 0;

        // in-memory simple log (used to preserve non-sample step messages)
        private readonly List<string> _stepLog = new List<string>();
        private readonly object _logLock = new object();

        private int _currentAdjustmentStep = 0; // new: tracks which step we're on (0-based)

        /*
        dataGridView is populated with column names. additional columns for each sample point can be added dynamically.
        listBox is used to show a log of sample activity grouped by probe.
        Temperature Calculations: 
        ...
         */
        public TempAdjProgressFrm(List<ListViewItem> selectedProbes, ListViewItem selectedMirror, bool manual)
        {
            InitializeComponent();

            // store references
            _selectedProbeItems = selectedProbes?.ToList() ?? new List<ListViewItem>();
            _selectedMirrorItem = selectedMirror;
            _manualMode = manual;


            // initialize UI grid rows
            InitializeGridRows();

            // populate step list on form open
            FillStepList();

            // attempt to map selected listview items to runtime RotProbe and Mirror instances
            MapSelectedDevicesToRuntimeObjects();

            // init sample storage
            foreach (var p in _probes)
                _samples[p] = new List<(double, double, double, double)>();

            foreach (var p in _probes)
                _averages[p] = new List<(double, double, double, double)>();

            // prepare timers
            _soakRemaining = _soakInitial;
            maskedTextBoxTime.Text = FormatTimeSpan(_soakRemaining);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // listBox default
            listBox1.Items.Clear();
            AddStep("Temperature adjustment process started.");
            AddStep($"Manual mode: {(_manualMode ? "ON" : "OFF")}. Selected {_probes.Count} probe(s).");

            // Start the first adjustment step (will prompt per-step if manual)
            _currentAdjustmentStep = 0;
            StartNextAdjustmentStep();

        }

        private void InitializeGridRows()
        {
            // Ensure grid cleared of any example rows
            dataGridViewAdjData.Rows.Clear();

            // Add a row per selected probe and populate ProbeName (best-effort)
            foreach (var lvi in _selectedProbeItems)
            {
                // Try to find a human-readable probe name in subitems
                string probeDisplay = FindBestProbeDisplayNameFromListViewItem(lvi);

                var rowIdx = dataGridViewAdjData.Rows.Add();
                var row = dataGridViewAdjData.Rows[rowIdx];
                row.Cells["ProbeName"].Value = probeDisplay;
                // other cells left blank until samples arrive
            }
        }

        // PSEUDOCODE / PLAN:
        // 1. If lvi is null return empty string.
        // 2. Collect all non-empty subitem texts trimmed.
        // 3. Prefer a subitem that most likely represents the device name:
        //    - Skip any subitem that looks like a COM port (starts with "COM", case-insensitive).
        //    - Prefer subitems that contain at least one letter (to avoid numeric serials/addresses).
        // 4. If no candidate found, fall back to the first non-empty subitem.
        // 5. If still nothing, return the ListViewItem.Text or empty string.
        private string FindBestProbeDisplayNameFromListViewItem(ListViewItem lvi)
        {
            if (lvi == null) return string.Empty;

            var subitems = lvi.SubItems.Cast<ListViewItem.ListViewSubItem>()
                                       .Select(s => (s.Text ?? string.Empty).Trim())
                                       .Where(t => !string.IsNullOrEmpty(t))
                                       .ToList();

            // Try to prefer something that looks like a DeviceName:
            // - Not a COM port (skip items that start with "COM")
            // - Contains at least one letter (to avoid pure numeric serials/addresses)
            foreach (var text in subitems)
            {
                if (text.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (text.Any(char.IsLetter))
                    return text;
            }

            // If none matched the "DeviceName-like" heuristic, fall back to the first non-empty subitem
            if (subitems.Count > 0)
                return subitems[0];

            // Last resort: use the main item text
            return lvi.Text ?? string.Empty;
        }

        private void MapSelectedDevicesToRuntimeObjects()
        {
            // Get snapshots from Program (thread-safe copy)
            var probeSnapshot = Program.GetConnectedProbesSnapshot();
            var mirrorSnapshot = Program.ConnectedMirrors; // Program stores a list already

            // Map probes: best-effort using multiple candidate keys from the listview item
            foreach (var lvi in _selectedProbeItems)
            {
                var candidates = lvi.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => (s.Text ?? "").Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                RotProbe matched = null;
                foreach (var p in probeSnapshot)
                {
                    if (p == null) continue;
                    if (candidates.Any(c => string.Equals(c, p.ComPort, StringComparison.OrdinalIgnoreCase))) { matched = p; break; }
                    if (!string.IsNullOrEmpty(p.SerialNumber) && candidates.Any(c => string.Equals(c, p.SerialNumber, StringComparison.OrdinalIgnoreCase))) { matched = p; break; }
                    if (!string.IsNullOrEmpty(p.DeviceName) && candidates.Any(c => string.Equals(c, p.DeviceName, StringComparison.OrdinalIgnoreCase))) { matched = p; break; }
                    if (!string.IsNullOrEmpty(p.ProbeAddress) && candidates.Any(c => string.Equals(c, p.ProbeAddress, StringComparison.OrdinalIgnoreCase))) { matched = p; break; }
                }
                if (matched == null)
                {
                    // last resort: try match by any overlapping token
                    foreach (var p in probeSnapshot)
                    {
                        if (p == null) continue;
                        if (candidates.Any(c => !string.IsNullOrEmpty(c) && (p.ComPort ?? "").IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    (p.DeviceName ?? "").IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    (p.SerialNumber ?? "").IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            matched = p;
                            break;
                        }
                    }
                }
                if (matched != null)
                {
                    _probes.Add(matched);
                    AddStep($"Mapped probe display '{FindBestProbeDisplayNameFromListViewItem(lvi)}' -> runtime probe on {matched.ComPort}.");
                }
                else
                {
                    AddStep($"Warning: could not map selected probe '{FindBestProbeDisplayNameFromListViewItem(lvi)}' to a connected probe.");
                }
            }

            // Map mirror (single)
            if (_selectedMirrorItem != null && mirrorSnapshot != null)
            {
                var mirrorCandidates = _selectedMirrorItem.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => (s.Text ?? "").Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                foreach (var m in mirrorSnapshot)
                {
                    if (m == null) continue;
                    var props = m.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    // build simple string set from known properties
                    var values = new List<string>();
                    foreach (var prop in props)
                    {
                        try
                        {
                            var v = prop.GetValue(m)?.ToString();
                            if (!string.IsNullOrWhiteSpace(v)) values.Add(v);
                        }
                        catch { }
                    }
                    if (mirrorCandidates.Any(c => values.Any(v => string.Equals(v, c, StringComparison.OrdinalIgnoreCase))))
                    {
                        _mirrorObj = m;
                        AddStep($"Mapped selected mirror to runtime mirror (type {m.GetType().Name}).");
                        break;
                    }
                }
                if (_mirrorObj == null)
                    AddStep("Warning: could not map selected mirror to a connected mirror.");
            }
        }

        private void StartSoakTimer()
        {
            // create and start UI timer (1 second tick)
            _soakTimer = new System.Windows.Forms.Timer();
            _soakTimer.Interval = 1000;
            _soakTimer.Tick += SoakTimer_Tick;
            _soakTimer.Start();

            AddStep($"Soak started for {_soakInitial.TotalMinutes} minutes.");
        }

        private void SoakTimer_Tick(object sender, EventArgs e)
        {
            _soakRemaining = _soakRemaining.Subtract(TimeSpan.FromSeconds(1));
            if (_soakRemaining < TimeSpan.Zero) _soakRemaining = TimeSpan.Zero;

            maskedTextBoxTime.Text = FormatTimeSpan(_soakRemaining);

            if (_soakRemaining == TimeSpan.Zero)
            {
                // stop soak timer
                try
                {
                    _soakTimer.Stop();
                    _soakTimer.Tick -= SoakTimer_Tick;
                    _soakTimer.Dispose();
                    _soakTimer = null;
                }
                catch { }

                AddStep("Soak complete; beginning sampling sequence.");
                BeginSamplingSequence();
            }
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            // mm:ss display
            return string.Format("{0:D2}:{1:D2}", (int)ts.TotalMinutes, ts.Seconds);
        }

        private void BeginSamplingSequence()
        {
            _samplesTaken = 0;
            maskedTextBoxTime.Text = "SAMP";
            // sampleTimer ticks once per sample interval
            _sampleTimer = new System.Windows.Forms.Timer();
            _sampleTimer.Interval = _sampleIntervalSec * 1000;
            _sampleTimer.Tick += SampleTimer_Tick;
            // take first sample immediately
            TakeSample();
            _sampleTimer.Start();
        }

        private void SampleTimer_Tick(object sender, EventArgs e)
        {
            TakeSample();
        }

        private void TakeSample()
        {
            _samplesTaken++;
            AddStep($"Taking sample {_samplesTaken} of {_sampleCount}...");

            // Refresh snapshot from Program (gives the most recent device state)
            var snapshot = Program.GetConnectedProbesSnapshot();

            // read mirror temperature if available
            double mirrorTemp = double.NaN;
            if (_mirrorObj != null)
            {
                mirrorTemp = GetMirrorTemperature(_mirrorObj);
            }

            // read mirror humidity if available (kept for grid display)
            double mirrorHumidity = double.NaN;
            if (_mirrorObj != null)
            {
                mirrorHumidity = GetMirrorHumidity(_mirrorObj);
            }

            // iterate probes and record sample values
            for (int i = 0; i < _probes.Count; i++)
            {
                var p = _probes[i];
                if (p == null) continue;

                // Try to find the latest state for this probe in Program snapshot (match by ComPort or Serial)
                var current = snapshot.FirstOrDefault(x =>
                    string.Equals(x?.ComPort ?? "", p.ComPort ?? "", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(x?.SerialNumber) && string.Equals(x.SerialNumber, p.SerialNumber, StringComparison.OrdinalIgnoreCase)));

                if (current == null)
                {
                    AddStep($"Warning: probe {p.ComPort ?? p.DeviceName} not found in snapshot for sample.");
                    continue;
                }

                // Normalize probe temperature to Celsius based on probe's reported engineering unit.
                double probeTemp = GetProbeTemperatureCelsius(current);
                double resistance = double.IsNaN(current.Resistance) ? double.NaN : current.Resistance;

                // read probe temperature count and convert to counts/1000.0 (store as double)
                double probeCount = double.NaN;
                try
                {
                    // TemperatureCount is int in RotProbe; divide by 1000.0 per requirements
                    probeCount = current.TemperatureCount / 1000.0;
                }
                catch { probeCount = double.NaN; }

                // store sample (use the 'current' instance as key if available in dictionary)
                RotProbe sampleKey = null;
                if (_samples.ContainsKey(current))
                    sampleKey = current;
                else if (_samples.ContainsKey(p))
                    sampleKey = p;
                else
                {
                    sampleKey = _samples.Keys.FirstOrDefault(k =>
                        string.Equals(k?.ComPort ?? "", current.ComPort ?? "", StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(k?.SerialNumber) && string.Equals(k.SerialNumber, current.SerialNumber, StringComparison.OrdinalIgnoreCase)));
                }

                if (sampleKey == null)
                {
                    // create entry if needed (defensive)
                    sampleKey = current;
                    _samples[sampleKey] = new List<(double, double, double, double)>();
                }

                _samples[sampleKey].Add((mirrorTemp, probeTemp, resistance, probeCount));

                // probe humidity (handle misspelling and possible unit fields)
                var probeHumidityTuple = GetProbeHumidity(current);
                double probeHumidity = probeHumidityTuple.humidity;
                string probeHumidityUnit = probeHumidityTuple.unit;

                // Update grid display (row mapping by ProbeName cell)
                UpdateGridRowForProbe(current, mirrorTemp, probeTemp, resistance, probeHumidity, probeHumidityUnit, mirrorHumidity);
            }

            // After storing samples, update the listBox in a single batch so probe samples appear together
            RefreshDisplay();

            // Validation: simple check for high variation in last two samples for each probe (if at least 2 samples)
            foreach (var kv in _samples)
            {
                var list = kv.Value;
                if (list.Count >= 2)
                {
                    var last = list[list.Count - 1];
                    var prev = list[list.Count - 2];
                    if (!double.IsNaN(last.probeTemp) && !double.IsNaN(prev.probeTemp))
                    {
                        if (Math.Abs(last.probeTemp - prev.probeTemp) > 0.1)
                        {
                            AddStep($"Warning: probe {kv.Key.ComPort ?? kv.Key.DeviceName} temperature changed >0.1°C between samples ({prev.probeTemp:F3} -> {last.probeTemp:F3}). Consider repeating.");
                        }
                    }
                }
            }

            if (_samplesTaken >= _sampleCount)
            {
                // stop sampling timer
                try
                {
                    _sampleTimer.Stop();
                    _sampleTimer.Tick -= SampleTimer_Tick;
                    _sampleTimer.Dispose();
                    _sampleTimer = null;
                }
                catch { }

                AddStep("Sampling complete for this step.");

                // --- NEW: compute and store averages for this measurement series (last _sampleCount samples) ---
                try
                {
                    foreach (var kv in _samples)
                    {
                        var probe = kv.Key;
                        var allSamples = kv.Value;
                        if (allSamples == null || allSamples.Count == 0) continue;

                        // determine the slice that corresponds to the most recent series of samples
                        int takeCount = Math.Min(_sampleCount, allSamples.Count);
                        var lastSeries = allSamples.Skip(allSamples.Count - takeCount).Take(takeCount).ToList();

                        double avgMirror = lastSeries.Where(s => !double.IsNaN(s.mirrorTemp)).Select(s => s.mirrorTemp).DefaultIfEmpty(double.NaN).Average();
                        double avgProbe = lastSeries.Where(s => !double.IsNaN(s.probeTemp)).Select(s => s.probeTemp).DefaultIfEmpty(double.NaN).Average();
                        double avgResistance = lastSeries.Where(s => !double.IsNaN(s.resistance)).Select(s => s.resistance).DefaultIfEmpty(double.NaN).Average();
                        double avgCount = lastSeries.Where(s => !double.IsNaN(s.probeCount)).Select(s => s.probeCount).DefaultIfEmpty(double.NaN).Average();

                        // Ensure an entry exists in _averages for this probe
                        if (!_averages.ContainsKey(probe))
                            _averages[probe] = new List<(double, double, double, double)>();

                        _averages[probe].Add((avgMirror, avgProbe, avgResistance, avgCount));

                        AddStep($"Stored average for probe {probe.ComPort ?? probe.DeviceName}: mirror={FormatDoubleOrNA(avgMirror)}, probe={FormatDoubleOrNA(avgProbe)}, R={FormatDoubleOrNA(avgResistance)}, Count={FormatDoubleOrNA(avgCount)}");
                    }
                }
                catch (Exception ex)
                {
                    AddStep($"Error while computing/storing averages: {ex.Message}");
                }

                // Instead of immediately calculating coefficients for the entire sequence,
                // ask whether to proceed to another step (if manual) or finish.
                OnSamplingStepComplete();
            }
            else
            {
                AddStep($"Waiting {_sampleIntervalSec} seconds before next sample.");
            }
        }

        private double GetMirrorTemperature(object mirror)
        {
            if (mirror == null) return double.NaN;
            try
            {
                var tProp = mirror.GetType().GetProperty("MirrorTemp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                            ?? mirror.GetType().GetProperty("ExternalTemp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                            ?? mirror.GetType().GetProperty("Temperature", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                            ?? null;
                if (tProp != null)
                {
                    var v = tProp.GetValue(mirror);
                    if (v is double d) return d;
                    if (v is float f) return f;
                    double parsed;
                    if (v != null && double.TryParse(v.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                        return parsed;
                }
            }
            catch { }
            return double.NaN;
        }

        // New: get mirror humidity with flexible property name handling (handles "Humdity" misspelling)
        private double GetMirrorHumidity(object mirror)
        {
            if (mirror == null) return double.NaN;
            try
            {
                var candidates = new[] { "Humdity", "Humidity", "MirrorHumidity", "ExternalHumidity" };
                foreach (var name in candidates)
                {
                    var p = mirror.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (p == null) continue;
                    var v = p.GetValue(mirror);
                    if (v == null) continue;
                    if (v is double dd) return dd;
                    if (v is float ff) return ff;
                    double parsed;
                    if (double.TryParse(v.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                        return parsed;
                }
            }
            catch { }
            return double.NaN;
        }

        // Get probe humidity and unit, handle RotProbe misspellings (Humdity*) and alternative names via reflection
        private (double humidity, string unit) GetProbeHumidity(RotProbe probe)
        {
            if (probe == null) return (double.NaN, null);

            try
            {
                // Direct property access (preferred)
                // RotProbe defines a public Humidity property; use that first.
                double h = double.NaN;
                string unit = null;

                try { h = probe.Humidity; } catch { h = double.NaN; }

                // Unit: try both correct and misspelled names
                try { unit = probe.HumidityUnit ?? probe.TemperatureUnit; } catch { }

                // If humidity still NaN, try reflection for other candidate property names
                if (double.IsNaN(h))
                {
                    var type = probe.GetType();
                    var names = new[] { "Humdity", "HumdityRaw", "Humdity", "HumidityRaw", "Humidity" };
                    foreach (var name in names)
                    {
                        var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (prop == null) continue;
                        try
                        {
                            var val = prop.GetValue(probe);
                            if (val == null) continue;
                            if (val is double dv) { h = dv; break; }
                            if (val is float fv) { h = fv; break; }
                            double parsed;
                            if (double.TryParse(val.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                            {
                                h = parsed;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                // Try to find a unit property if none obtained from direct access
                if (string.IsNullOrWhiteSpace(unit))
                {
                    var type = probe.GetType();
                    var candidateNames = new[] { "HumidityUnit", "HumidityUnit", "HumdityUnitRaw", "Unit" };
                    foreach (var n in candidateNames)
                    {
                        var prop = type.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (prop == null) continue;
                        try
                        {
                            var v = prop.GetValue(probe);
                            if (v != null)
                            {
                                unit = v.ToString();
                                break;
                            }
                        }
                        catch { }
                    }
                }

                return (h, unit);
            }
            catch
            {
                return (double.NaN, null);
            }
        }

        // New helper: normalize a RotProbe temperature to Celsius based on the probe's reported unit.
        // Uses direct TemperatureUnit when available, falls back to reflection if needed.
        private double GetProbeTemperatureCelsius(RotProbe probe)
        {
            if (probe == null) return double.NaN;

            double t;
            try { t = probe.Temperature; } catch { t = double.NaN; }

            if (double.IsNaN(t)) return double.NaN;

            string unit = null;
            try { unit = probe.TemperatureUnit; } catch { unit = null; }

            if (string.IsNullOrWhiteSpace(unit))
            {
                try
                {
                    var type = probe.GetType();
                    var candidateNames = new[] { "TemperatureUnit", "TempUnit", "Unit", "Temperature_Unit" };
                    foreach (var n in candidateNames)
                    {
                        var prop = type.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (prop == null) continue;
                        var v = prop.GetValue(probe);
                        if (v != null)
                        {
                            unit = v.ToString();
                            break;
                        }
                    }
                }
                catch { unit = null; }
            }

            if (IsFahrenheitUnit(unit))
            {
                return ConvertFahrenheitToCelsius(t);
            }

            // otherwise assume Celsius
            return t;
        }

        // Heuristic to detect Fahrenheit-like unit strings (covers "F", "°F", "Fahrenheit", etc.)
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

        private void UpdateGridRowForProbe(RotProbe probe, double mirrorTemp, double probeTemp, double resistance, double probeHumidity = double.NaN, string probeHumidityUnit = null, double mirrorHumidity = double.NaN)
        {
            if (probe == null) return;

            // helper to set a cell if column exists
            Action<DataGridViewRow, string, object> TrySetCell = (row, colName, val) =>
            {
                if (row == null || string.IsNullOrEmpty(colName)) return;
                if (dataGridViewAdjData.Columns.Contains(colName))
                {
                    try { row.Cells[colName].Value = val; } catch { /* ignore */ }
                }
            };

            // find the row by matching ProbeName cell text to available keys (ComPort/DeviceName/ProbeAddress)
            for (int r = 0; r < dataGridViewAdjData.Rows.Count; r++)
            {
                var row = dataGridViewAdjData.Rows[r];
                var cellVal = row.Cells["ProbeName"].Value?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(cellVal)) continue;

                bool match = string.Equals(cellVal, probe.ComPort, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(cellVal, probe.DeviceName, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(cellVal, probe.ProbeAddress, StringComparison.OrdinalIgnoreCase)
                             || cellVal.IndexOf(probe.ComPort ?? "", StringComparison.OrdinalIgnoreCase) >= 0;

                if (match)
                {
                    // MirrorTemp, ProbeTemp, ProbeHumidity, MirrorHumidity, CoeffA, CoeffB, Resistance, Offset

                    // temperature & resistance & coeffs (existing behavior)
                    TrySetCell(row, "MirrorTemp", double.IsNaN(mirrorTemp) ? string.Empty : mirrorTemp.ToString("F3"));
                    TrySetCell(row, "ProbeTemp", double.IsNaN(probeTemp) ? string.Empty : probeTemp.ToString("F3"));
                    TrySetCell(row, "Resistance", double.IsNaN(resistance) ? string.Empty : resistance.ToString("F3"));
                    TrySetCell(row, "CoeffA", probe.PT100CoeffA.ToString("G9"));
                    TrySetCell(row, "CoeffB", probe.PT100CoeffB.ToString("G9"));
                    TrySetCell(row, "Offset", probe.TempOffset.ToString("G9"));

                    // humidity: attempt to set multiple possible column names safely
                    // Determine probe humidity & unit if not passed
                    double ph = probeHumidity;
                    string phUnit = probeHumidityUnit;
                    if (double.IsNaN(ph))
                    {
                        var tup = GetProbeHumidity(probe);
                        ph = tup.humidity;
                        phUnit = tup.unit;
                    }

                    // Mirror humidity (value supplied or obtained)
                    double mh = mirrorHumidity;
                    if (double.IsNaN(mh) && _mirrorObj != null)
                        mh = GetMirrorHumidity(_mirrorObj);

                    // Candidate column names to populate for probe humidity
                    var probeHumidityColumns = new[] { "ProbeHumidity", "Humidity", "HumidityProbe", "ProbeHum", "Probe_Humidity" };
                    foreach (var col in probeHumidityColumns)
                    {
                        if (dataGridViewAdjData.Columns.Contains(col))
                        {
                            TrySetCell(row, col, double.IsNaN(ph) ? string.Empty : ph.ToString("F3"));
                            break;
                        }
                    }

                    // Candidate column names for humidity unit
                    var probeHumidityUnitCols = new[] { "ProbeHumidityUnit", "HumidityUnit", "HumUnit", "ProbeHumUnit" };
                    foreach (var col in probeHumidityUnitCols)
                    {
                        if (dataGridViewAdjData.Columns.Contains(col))
                        {
                            TrySetCell(row, col, string.IsNullOrWhiteSpace(phUnit) ? string.Empty : phUnit);
                            break;
                        }
                    }

                    // Candidate mirror humidity columns
                    var mirrorHumidityCols = new[] { "MirrorHumidity", "Mirror_Humidity", "HumidityMirror" };
                    foreach (var col in mirrorHumidityCols)
                    {
                        if (dataGridViewAdjData.Columns.Contains(col))
                        {
                            TrySetCell(row, col, double.IsNaN(mh) ? string.Empty : mh.ToString("F3"));
                            break;
                        }
                    }

                    // Candidate columns for probe count (counts/1000)
                    var probeCountCols = new[] { "ProbeCount", "TemperatureCount", "Count" };
                    foreach (var col in probeCountCols)
                    {
                        if (dataGridViewAdjData.Columns.Contains(col))
                        {
                            // Try to obtain a recent count from the probe object if available
                            double pc = double.NaN;
                            try { pc = probe.TemperatureCount / 1000.0; } catch { }
                            TrySetCell(row, col, double.IsNaN(pc) ? string.Empty : pc.ToString("F3"));
                            break;
                        }
                    }

                    break;
                }
            }
        }

        // Solves a 3x3 linear system with Gaussian elimination and partial pivoting.
        // Returns false if singular/ill-conditioned.
        private bool Solve3x3(double[,] M, double[] y, out double[] x)
        {
            x = new double[3];
            // augmented matrix 3x4
            var a = new double[3, 4];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) a[i, j] = M[i, j];
                a[i, 3] = y[i];
            }

            // Gaussian elimination with partial pivoting
            for (int col = 0; col < 3; col++)
            {
                // pivot selection
                int pivot = col;
                double maxAbs = Math.Abs(a[col, col]);
                for (int r = col + 1; r < 3; r++)
                {
                    double v = Math.Abs(a[r, col]);
                    if (v > maxAbs) { maxAbs = v; pivot = r; }
                }

                if (maxAbs <= 1e-15) // singular or ill-conditioned
                    return false;

                // swap rows if needed
                if (pivot != col)
                {
                    for (int c = col; c < 4; c++)
                    {
                        var tmp = a[col, c];
                        a[col, c] = a[pivot, c];
                        a[pivot, c] = tmp;
                    }
                }

                // normalize pivot row
                double diag = a[col, col];
                for (int c = col; c < 4; c++) a[col, c] /= diag;

                // eliminate other rows
                for (int r = 0; r < 3; r++)
                {
                    if (r == col) continue;
                    double factor = a[r, col];
                    if (factor == 0.0) continue;
                    for (int c = col; c < 4; c++)
                        a[r, c] -= factor * a[col, c];
                }
            }

            for (int i = 0; i < 3; i++) x[i] = a[i, 3];
            return true;
        }

        private void CalculateAndDisplayCoefficients()
        {

            foreach (var kv in _averages)
            {
                var probe = kv.Key;
                var avgList = kv.Value;
                if (avgList == null || avgList.Count == 0) continue;

                // Build valid (T, R) pairs using mirror/reference temperature from averages.
                var points = new List<(double T, double R)>();
                foreach (var t in avgList)
                {
                    double T = t.mirrorTemp;
                    double R = t.resistance;
                    if (double.IsNaN(T) || double.IsNaN(R)) continue;
                    // Only use points in [0,50] °C as per original plan
                    if (T < 0.0 || T > 50.0) continue;
                    points.Add((T, R));
                }

                // Build valid (T, Count) pairs using averaged probe count (counts/1000)
                var countPoints = new List<(double T, double C)>();
                foreach (var t in avgList)
                {
                    double T = t.mirrorTemp;
                    double C = t.probeCount;
                    if (double.IsNaN(T) || double.IsNaN(C)) continue;
                    if (T < 0.0 || T > 50.0) continue;
                    countPoints.Add((T, C));
                }

                // Compute simple display averages (from all averaged rows, not only filtered points)
                double avgMirrorAll = avgList.Where(s => !double.IsNaN(s.mirrorTemp)).Select(s => s.mirrorTemp).DefaultIfEmpty(double.NaN).Average();
                double avgProbeAll = avgList.Where(s => !double.IsNaN(s.probeTemp)).Select(s => s.probeTemp).DefaultIfEmpty(double.NaN).Average();
                double avgResistanceAll = avgList.Where(s => !double.IsNaN(s.resistance)).Select(s => s.resistance).DefaultIfEmpty(double.NaN).Average();
                double avgCountAll = avgList.Where(s => !double.IsNaN(s.probeCount)).Select(s => s.probeCount).DefaultIfEmpty(double.NaN).Average();

                // newOffsetTemp will be computed from fitted intercept if available, otherwise NaN
                double newOffset = double.NaN;

                if (points.Count < 3)
                {
                    AddStep($"Probe {probe.ComPort ?? probe.DeviceName}: insufficient averaged points ({points.Count}) in [0,50]°C to estimate R0/A/B. Showing existing coefficients.");
                    // update grid with existing coeffs + avg resistance/offset (leave offset as N/A)
                    for (int r = 0; r < dataGridViewAdjData.Rows.Count; r++)
                    {
                        var row = dataGridViewAdjData.Rows[r];
                        var cellVal = row.Cells["ProbeName"].Value?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(cellVal)) continue;
                        if (string.Equals(cellVal, probe.ComPort, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(cellVal, probe.DeviceName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(cellVal, probe.ProbeAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            row.Cells["CoeffA"].Value = probe.PT100CoeffA.ToString("G9");
                            row.Cells["CoeffB"].Value = probe.PT100CoeffB.ToString("G9");
                            row.Cells["Resistance"].Value = double.IsNaN(avgResistanceAll) ? string.Empty : avgResistanceAll.ToString("F3");
                            if (dataGridViewAdjData.Columns.Contains("R0"))
                                row.Cells["R0"].Value = string.Empty;
                            row.Cells["Offset"].Value = "N/A";
                            if (dataGridViewAdjData.Columns.Contains("ProbeCountAvg"))
                                row.Cells["ProbeCountAvg"].Value = double.IsNaN(avgCountAll) ? string.Empty : avgCountAll.ToString("F6");
                            break;
                        }
                    }
                    continue;
                }

                // Build normal equation sums for design matrix [1, T, T^2]
                double n = points.Count;
                double sT = 0.0, sT2 = 0.0, sT3 = 0.0, sT4 = 0.0;
                double sR = 0.0, sRT = 0.0, sRT2 = 0.0;
                foreach (var p in points)
                {
                    double T = p.T;
                    double T2 = T * T;
                    double R = p.R;
                    sT += T;
                    sT2 += T2;
                    sT3 += T2 * T;
                    sT4 += T2 * T2;
                    sR += R;
                    sRT += R * T;
                    sRT2 += R * T2;
                }

                var XT_X = new double[3, 3]
                {
                        { n,    sT,   sT2 },
                        { sT,   sT2,  sT3 },
                        { sT2,  sT3,  sT4 }
                };
                var XT_y = new double[3] { sR, sRT, sRT2 };

                if (!Solve3x3(XT_X, XT_y, out double[] pvec))
                {
                    AddStep($"Probe {probe.ComPort ?? probe.DeviceName}: linear system singular or ill-conditioned; cannot estimate coefficients.");
                    continue;
                }

                double p0 = pvec[0]; // R0
                double p1 = pvec[1];
                double p2 = pvec[2];

                if (Math.Abs(p0) < 1e-12)
                {
                    AddStep($"Probe {probe.ComPort ?? probe.DeviceName}: solved R0 is too small or zero; aborting coefficient extraction.");
                    continue;
                }

                double R0 = p0;
                double A = p1 / p0;
                double B = p2 / p0;

                // Fit counts polynomial (counts stored as counts/1000)
                double projectedCount = double.NaN;
                if (countPoints.Count >= 3)
                {
                    // reuse XT_X (same T powers) but build XT_y for counts
                    double sC = 0.0, sCT = 0.0, sCT2 = 0.0;
                    foreach (var cp in countPoints)
                    {
                        double T = cp.T;
                        double T2 = T * T;
                        double C = cp.C;
                        sC += C;
                        sCT += C * T;
                        sCT2 += C * T2;
                    }
                    var XT_y_counts = new double[3] { sC, sCT, sCT2 };
                    if (Solve3x3(XT_X, XT_y_counts, out double[] cvec))
                    {
                        double c0 = cvec[0]; // intercept -> projected count at 0°C (counts/1000)
                        projectedCount = c0;
                        // compute offset in counts/1000 per ohm relative to existing TempConversion
                        // per user instruction: newOffset = projectedCount / projectedResistance - RotProbe.TempConversion
                        if (!double.IsNaN(R0) && Math.Abs(R0) > 1e-12)
                        {
                            newOffset = (projectedCount / R0) - probe.TempConversion;
                        }
                    }
                }

                // Diagnostics: RMSE and R^2 on used points
                double ssRes = 0.0;
                double ssTot = 0.0;
                double meanR = sR / n;
                foreach (var pt in points)
                {
                    double Rpred = R0 * (1.0 + A * pt.T + B * pt.T * pt.T);
                    double err = pt.R - Rpred;
                    ssRes += err * err;
                    double dmean = pt.R - meanR;
                    ssTot += dmean * dmean;
                }
                double rmse = Math.Sqrt(ssRes / n);
                double r2 = (ssTot <= 0.0) ? 1.0 : Math.Max(0.0, 1.0 - ssRes / ssTot);

                // Update grid row for probe
                for (int r = 0; r < dataGridViewAdjData.Rows.Count; r++)
                {
                    var row = dataGridViewAdjData.Rows[r];
                    var cellVal = row.Cells["ProbeName"].Value?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(cellVal)) continue;
                    if (string.Equals(cellVal, probe.ComPort, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cellVal, probe.DeviceName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cellVal, probe.ProbeAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Cells["CoeffA"].Value = A.ToString("G9");
                        row.Cells["CoeffB"].Value = B.ToString("G9");
                        row.Cells["Resistance"].Value = double.IsNaN(avgResistanceAll) ? string.Empty : avgResistanceAll.ToString("F3");
                        if (dataGridViewAdjData.Columns.Contains("R0"))
                            row.Cells["R0"].Value = R0.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                        // Offset now reported in counts/1000 per ohm (difference vs TempConversion)
                        row.Cells["Offset"].Value = double.IsNaN(newOffset) ? "N/A" : newOffset.ToString("G9");
                        if (dataGridViewAdjData.Columns.Contains("ProbeCountAvg"))
                            row.Cells["ProbeCountAvg"].Value = double.IsNaN(avgCountAll) ? string.Empty : avgCountAll.ToString("F6");
                        break;
                    }
                }

                AddStep($"Probe {probe.ComPort ?? probe.DeviceName}: Estimated R0={R0:F6} Ω, A={A:G9}, B={B:G9}, points={points.Count}, RMSE={rmse:F6}, R²={r2:F4}" +
                        $"{(double.IsNaN(projectedCount) ? "" : $", projectedCount@0°C={projectedCount:F6}")}{(double.IsNaN(newOffset) ? "" : $", newOffset={newOffset:G9}")}");
            }

            // Do not write to devices here — user will verify results first.
            AddStep("Coefficient estimation complete. Review values in the grid; writing to probes is disabled until verification.");
        }

        private string FormatDoubleOrNA(double v)
        {
            return double.IsNaN(v) ? "N/A" : v.ToString("F6");
        }

        private void WriteCalculatedCoefficientsToProbes()
        {
            AddStep("Writing calculated offsets to probes (attempt)...");

            foreach (var kv in _averages)
            {
                var probe = kv.Key;
                var avgList = kv.Value;
                if (avgList == null || avgList.Count == 0)
                {
                    AddStep($"Skipping probe {probe.ComPort ?? probe.DeviceName}: no averaged data.");
                    continue;
                }

                // Build valid (T, R) pairs using mirror/reference temperature from averages.
                var points = new List<(double T, double R)>();
                foreach (var t in avgList)
                {
                    double T = t.mirrorTemp;
                    double R = t.resistance;
                    if (double.IsNaN(T) || double.IsNaN(R)) continue;
                    if (T < 0.0 || T > 50.0) continue;
                    points.Add((T, R));
                }

                // Build valid (T, C) pairs for counts (counts/1000)
                var countPoints = new List<(double T, double C)>();
                foreach (var t in avgList)
                {
                    double T = t.mirrorTemp;
                    double C = t.probeCount;
                    if (double.IsNaN(T) || double.IsNaN(C)) continue;
                    if (T < 0.0 || T > 50.0) continue;
                    countPoints.Add((T, C));
                }

                if (points.Count < 3)
                {
                    AddStep($"Skipping probe {probe.ComPort ?? probe.DeviceName}: insufficient points ({points.Count}) for R0 estimation.");
                    continue;
                }

                // Build normal equation sums for design matrix [1, T, T^2]
                double n = points.Count;
                double sT = 0.0, sT2 = 0.0, sT3 = 0.0, sT4 = 0.0;
                double sR = 0.0, sRT = 0.0, sRT2 = 0.0;
                foreach (var p in points)
                {
                    double T = p.T;
                    double T2 = T * T;
                    double R = p.R;
                    sT += T;
                    sT2 += T2;
                    sT3 += T2 * T;
                    sT4 += T2 * T2;
                    sR += R;
                    sRT += R * T;
                    sRT2 += R * T2;
                }

                var XT_X = new double[3, 3]
                {
                        { n,    sT,   sT2 },
                        { sT,   sT2,  sT3 },
                        { sT2,  sT3,  sT4 }
                };
                var XT_y = new double[3] { sR, sRT, sRT2 };

                if (!Solve3x3(XT_X, XT_y, out double[] pvec))
                {
                    AddStep($"Skipping probe {probe.ComPort ?? probe.DeviceName}: linear system singular for R fit.");
                    continue;
                }

                double p0 = pvec[0]; // R0
                if (Math.Abs(p0) < 1e-12)
                {
                    AddStep($"Skipping probe {probe.ComPort ?? probe.DeviceName}: solved R0 is too small or zero.");
                    continue;
                }

                double R0 = p0;

                // Fit counts polynomial if possible to get projected count at 0°C
                double projectedCount = double.NaN;
                if (countPoints.Count >= 3)
                {
                    double sC = 0.0, sCT = 0.0, sCT2 = 0.0;
                    foreach (var cp in countPoints)
                    {
                        double T = cp.T;
                        double T2 = T * T;
                        double C = cp.C;
                        sC += C;
                        sCT += C * T;
                        sCT2 += C * T2;
                    }
                    var XT_y_counts = new double[3] { sC, sCT, sCT2 };
                    if (Solve3x3(XT_X, XT_y_counts, out double[] cvec))
                    {
                        projectedCount = cvec[0]; // counts/1000 at 0°C
                    }
                }

                if (double.IsNaN(projectedCount))
                {
                    AddStep($"Skipping probe {probe.ComPort ?? probe.DeviceName}: insufficient count data to project counts at 0°C.");
                    continue;
                }

                // Per user instruction:
                // newOffset = projectedCount / projectedResistance - RotProbe.TempConversion
                double newOffset = (projectedCount / R0) - probe.TempConversion;

                // Attempt to write offset using Commands helper (fire-and-forget)
                bool ok = Commands.SendNewTemperatureOffset(probe, newOffset);
                AddStep($"Write Offset to {probe.ComPort ?? probe.DeviceName}: {(ok ? "OK" : "FAILED")} (newOffset={newOffset:G9}, projectedCount={projectedCount:F6}, R0={R0:F6})");
            }

            AddStep("Write sequence complete.");
        }

        // AddStep now stores messages and triggers a full batch refresh of the listBox
        private void AddStep(string text)
        {
            var entry = $"{DateTime.Now:HH:mm:ss} - {text}";
            lock (_logLock)
            {
                _stepLog.Add(entry);
            }
            RefreshDisplay();
        }

        // PSEUDOCODE / DETAILED PLAN:
        // 1. Ensure UI-thread execution (reinvoke via BeginInvoke if necessary).
        // 2. Begin updating the ListBox to avoid flicker.
        // 3. Clear existing items.
        // 4. Append the chronological _stepLog entries (thread-safe, locked).
        // 5. If there are general log lines, append a separator line.
        // 6. For each probe in _probes:
        //    a. Skip null probes or probes with no samples recorded in _samples.
        //    b. Add a header line "Probe: <probeName>".
        //    c. For each sample at index i in the probe's sample list:
        //         - Compute the adjustment step number that the sample belongs to:
        //             stepNum = (i / _sampleCount) + 1   (integer division)
        //             If _sampleCount <= 0 fall back to (i + 1) to avoid div-by-zero.
        //         - Compute the sample number within the step:
        //             sampleNum = (i % _sampleCount) + 1  (if _sampleCount > 0)
        //         - Format probeTemp and mirrorTemp (use "N/A" when NaN).
        //         - Build a single line including stepNum and sampleNum and add to listbox.
        //    d. Add an empty line to separate probe blocks.
        // 7. Auto-scroll the listbox to the last item if any were added.
        // 8. End the update block.
        //
        // NOTES:
        // - This approach infers the step number from sample ordering and the configured
        //   _sampleCount. It is defensive against _sampleCount being zero.
        // - All UI updates occur on the UI thread via BeginInvoke when required.
        private void RefreshDisplay()
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.BeginInvoke((Action)(() => RefreshDisplay()));
                return;
            }

            listBox1.BeginUpdate();
            try
            {
                listBox1.Items.Clear();

                // Add general log lines
                lock (_logLock)
                {
                    foreach (var line in _stepLog)
                        listBox1.Items.Add(line);
                }

                // Separator
                if (listBox1.Items.Count > 0)
                    listBox1.Items.Add("--- Samples (grouped by probe) ---");

                // Grouped sample lines: for each probe, list its samples consecutively.
                foreach (var probe in _probes)
                {
                    if (probe == null) continue;
                    string probeName = probe.DeviceName ?? probe.ComPort ?? probe.ProbeAddress ?? "UnknownProbe";

                    if (!_samples.ContainsKey(probe)) continue;
                    var samples = _samples[probe];
                    if (samples == null || samples.Count == 0) continue;

                    // Optional header for the probe block (keeps blocks visually separated)
                    listBox1.Items.Add($"Probe: {probeName}");

                    for (int i = 0; i < samples.Count; i++)
                    {
                        var s = samples[i];

                        // Determine step number and sample number within the step.
                        int stepNum;
                        int sampleNum;
                        if (_sampleCount > 0)
                        {
                            stepNum = (i / _sampleCount) + 1;           // integer division groups samples into steps
                            sampleNum = (i % _sampleCount) + 1;        // sample index within the step (1-based)
                        }
                        else
                        {
                            // Defensive fallback
                            stepNum = i + 1;
                            sampleNum = 1;
                        }

                        string probeTempStr = double.IsNaN(s.probeTemp) ? "N/A" : s.probeTemp.ToString("F3");
                        string mirrorTempStr = double.IsNaN(s.mirrorTemp) ? "N/A" : s.mirrorTemp.ToString("F3");

                        var line = $"{stepNum} | {sampleNum} | {probeName} | {probeTempStr} | {mirrorTempStr}";
                        listBox1.Items.Add(line);
                    }

                    // blank line between probe blocks
                    listBox1.Items.Add(string.Empty);
                }

                // Auto-scroll to the last item
                if (listBox1.Items.Count > 0)
                    listBox1.TopIndex = listBox1.Items.Count - 1;
            }
            finally
            {
                listBox1.EndUpdate();
            }
        }

        private string FormatDoubleNullable(double v)
        {
            return double.IsNaN(v) ? string.Empty : v.ToString("F3");
        }

        private void buttonSkip_Click(object sender, EventArgs e)
        {
            // Skip soak time
            if (_soakTimer != null)
            {
                _soakRemaining = TimeSpan.Zero;
                maskedTextBoxTime.Text = FormatTimeSpan(_soakRemaining);
                AddStep("Soak skipped by user.");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                if (_soakTimer != null)
                {
                    _soakTimer.Stop();
                    _soakTimer.Tick -= SoakTimer_Tick;
                    _soakTimer.Dispose();
                    _soakTimer = null;
                }
                if (_sampleTimer != null)
                {
                    _sampleTimer.Stop();
                    _sampleTimer.Tick -= SampleTimer_Tick;
                    _sampleTimer.Dispose();
                    _sampleTimer = null;
                }
            }
            catch { }
        }

        // Start a new adjustment step (soak + sampling). If manual mode prompt the user before starting the soak.
        // If the user cancels the prompt we finish the sequence and calculate coefficients from collected data.
        private void StartNextAdjustmentStep()
        {
            // If there are no probes mapped, finish early.
            if (_probes.Count == 0)
            {
                AddStep("No mapped probes to adjust; aborting sequence.");
                CalculateAndDisplayCoefficients();
                return;
            }

            // Defensive: ensure the step list exists and has rows
            if (dataGridViewStepList == null || dataGridViewStepList.Rows.Count == 0)
            {
                AddStep("No adjustment steps defined in the step list; aborting sequence.");
                CalculateAndDisplayCoefficients();
                return;
            }

            // If we've exhausted the rows, finish the sequence
            if (_currentAdjustmentStep >= dataGridViewStepList.Rows.Count)
            {
                AddStep("All adjustment steps completed; calculating coefficients with collected data.");
                CalculateAndDisplayCoefficients();
                return;
            }

            // Helper to safely extract cell text (falls back to index if named column missing)
            string GetCellText(DataGridViewRow stepRow, int idx)
            {
                if (stepRow == null) return null;
                if (stepRow.Cells.Count > idx)
                    return stepRow.Cells[idx].Value?.ToString();
                return null;
            }

            // Helper to parse a double from messy cell text (removes non-number chars except . and -)
            Func<string, double> ParseDoubleFromCell = (s) =>
            {
                if (string.IsNullOrWhiteSpace(s)) return double.NaN;
                var cleaned = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == '-' || c == '+').ToArray());
                if (string.IsNullOrWhiteSpace(cleaned)) return double.NaN;
                double parsed;
                if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                    return parsed;
                return double.NaN;
            };

            // Read the current row (column 0 = Step, 1 = Temperature, 2 = Humidity per user's specification)
            var row = dataGridViewStepList.Rows[_currentAdjustmentStep];
            string stepText = GetCellText(row, 0) ?? (_currentAdjustmentStep + 1).ToString();
            string tempText = GetCellText(row, 1);
            string humText = GetCellText(row, 2);

            double tempVal = ParseDoubleFromCell(tempText);
            double humVal = ParseDoubleFromCell(humText);

            string tempFmt = double.IsNaN(tempVal) ? "N/A" : tempVal.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            string humFmt = double.IsNaN(humVal) ? "N/A" : humVal.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

            if (!_manualMode)
            {
                // Dummy behaviour for auto mode as requested
                MessageBox.Show(this, "Not yet implemented", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AddStep("Auto mode not yet implemented");
                return;
            }

            // Manual mode: prompt user with values from the current step row
            AddStep($"Manual: prompt user to set chamber to test point #{_currentAdjustmentStep + 1}.");
            var prompt = $"Set chamber to {tempFmt} °C and {humFmt} %rh";
            var res = MessageBox.Show(this, prompt, $"Manual Mode - Step {stepText}", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

            if (res != DialogResult.OK)
            {
                AddStep("User cancelled further adjustment steps");
                return;
            }

            // Start soak for this step
            _soakRemaining = _soakInitial;
            maskedTextBoxTime.Text = FormatTimeSpan(_soakRemaining);
            StartSoakTimer();
        }

        // Called when a sampling sequence for a step has finished (i.e., sampleCount samples taken).
        // Decides whether to start another adjustment step or to finish and compute coefficients.
        private void OnSamplingStepComplete()
        {
            AddStep($"Sampling step #{_currentAdjustmentStep + 1} complete.");

            // increment step index in anticipation of next step
            _currentAdjustmentStep++;

            // If we have a step list, and we've reached or passed the last row, finish and compute coefficients.
            if (dataGridViewStepList != null)
            {
                if (_currentAdjustmentStep == dataGridViewStepList.Rows.Count - 1)
                {
                    AddStep("Adjustment data gathered, calculating new coefficients");
                    try
                    {
                        CalculateAndDisplayCoefficients();
                    }
                    catch (Exception ex)
                    {
                        AddStep($"Error during coefficient calculation: {ex.Message}");
                    }
                    return;
                }
            }

            if (_manualMode)
            {
                AddStep($"Proceed to next step #{_currentAdjustmentStep + 1}.");
                StartNextAdjustmentStep();
                return;
            }
            else
            {
                // Auto multi-step behavior not implemented - finish for now
                AddStep("Auto multi-step not implemented");
                return;
            }
        }
        // PSEUDOCODE / PLAN (detailed):
        // 1. Add a method `FillStepList` that populates `dataGridViewStepList` when the form opens.
        // 2. Ensure the DataGridView has three columns: "Step", "Humidity", "Temperature".
        //    - If they don't exist, create them via `Columns.Add(name, header)`.
        // 3. Clear any existing rows to start from a clean state.
        // 4. Use the temperature series: [0, 5, 15, 25, 35, 45, 50].
        // 5. For each temperature value, add a new row:
        //    - "Step" column = the one-based index of the row (1,2,...).
        //    - "Humidity" column = "10%"
        //    - "Temperature" column = the numeric temperature value (as string or number).
        // 6. Call `FillStepList()` from the form constructor after grid initialization so the list is populated on form open.
        // 7. Make the method defensive:
        //    - If `dataGridViewStepList` is null, return silently.
        //    - If column exists already, reuse it.
        // 8. Keep the UI thread usage (no cross-thread invocation required since constructor runs on UI thread).

        private void FillStepList()
        {
            // Defensive: ensure DataGridView exists
            if (dataGridViewStepList == null)
                return;

            // Ensure columns exist - add if missing
            void EnsureColumn(string name, string header)
            {
                if (!dataGridViewStepList.Columns.Contains(name))
                    dataGridViewStepList.Columns.Add(name, header);
            }

            EnsureColumn("Step", "Step");
            EnsureColumn("Temperature", "Temperature (°C)");
            EnsureColumn("Humidity", "Humidity (%rh)");

            // Clear existing rows
            dataGridViewStepList.Rows.Clear();

            // Temperature series requested
            var temps = new[] { 0, 3, 5, 15, 25, 35, 45, 50 };

            for (int i = 0; i < temps.Length; i++)
            {
                int rowIndex = dataGridViewStepList.Rows.Add();
                var row = dataGridViewStepList.Rows[rowIndex];

                // Step index starting at 1
                if (dataGridViewStepList.Columns.Contains("Step"))
                    row.Cells["Step"].Value = (i + 1).ToString();

                // Temperature value
                if (dataGridViewStepList.Columns.Contains("Temperature"))
                    row.Cells["Temperature"].Value = temps[i].ToString();


                // Humidity 10%
                if (dataGridViewStepList.Columns.Contains("Humidity"))
                    row.Cells["Humidity"].Value = "10%";
            }
        }
    }
}