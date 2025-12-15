/*
 * todo:
 * global ceslius setting troubleshooting
 * Auto detect new devices during runtime
 * temperature adjustment sequence not working still
 * Chamber API
 * PDF report generation
 * prevent mirror/probes in use on one cal instance from being used on another
 */
using DocumentFormat.OpenXml.Vml.Office;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{
    internal static class Program
    {
        public static bool _globalTemperatureC = true;
        private static readonly List<SerialPort> activePorts = new List<SerialPort>();
        private static readonly object portLock = new object();

        // New: list of connected probes and synchronization for that list
        private static readonly object probesLock = new object();
        public static List<RotProbe> ConnectedProbes { get; private set; } = new List<RotProbe>();

        // Mirrors
        private static readonly object mirrorsLock = new object();
        public static List<Mirror> ConnectedMirrors { get; private set; } = new List<Mirror>();

        // New: timer to periodically refresh data
        private static System.Threading.Timer probeTimer;
        private static readonly object timerLock = new object();
        private static bool monitoringStarted = false;
        private const int ProbeReadTimeoutMs = 500;   // read loop timeout for probe queries (ms)
        private const int ProbePollSleepMs = 20;      // sleep between ReadExisting polls (ms)
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Ensure we will close the ports when the app exits and turn off mirror control
            Application.ApplicationExit += (s, e) =>
            {
                StopProbeMonitoring();
                // attempt to set mirrors control off
                try { SetAllMirrorControl(false); } catch { }
                CloseActivePorts();
            };
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                StopProbeMonitoring();
                try { SetAllMirrorControl(false); } catch { }
                CloseActivePorts();
            };
            // Start discovery + periodic updates
            StartProbeMonitoring(2000); // 15 seconds
            // Create the form instance (calls InitializeComponent)
            var mainForm = new Main();
            Application.Run(mainForm);
        }



        // Start periodic monitoring (threadpool timer)
        public static void StartProbeMonitoring(int intervalMs = 2000)
        {
            lock (timerLock)
            {
                if (monitoringStarted)
                    return;

                // dueTime = 0 -> run immediately, period = intervalMs
                probeTimer = new System.Threading.Timer(MonitorCallback, intervalMs, 0, intervalMs);
                monitoringStarted = true;
                try { Debug.WriteLine($"StartProbeMonitoring: started with interval {intervalMs}ms"); } catch { }
            }
        }

        // Helper: send command and read response with polling loop
        private static string SendRead(SerialPort port, string cmd, int timeoutMs)
        {
            try
            {
                try { port.DiscardInBuffer(); } catch { }
                port.Write(cmd);
            }
            catch (Exception ex)
            {
                try { Debug.WriteLine($"SendRead: write failed: {ex}"); } catch { }
                return string.Empty;
            }

            var sb = new StringBuilder();
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    var chunk = port.ReadExisting();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        sb.Append(chunk);
                        // allow remaining bytes to arrive
                        Thread.Sleep(20);
                        continue;
                    }
                }
                catch
                {
                    // ignore read errors
                }
                Thread.Sleep(20);
            }
            sw.Stop();
            return sb.ToString().Trim();
        }

        // Discover mirrors by scanning COM ports with mirror baud/settings.
        public static List<Mirror> MirrorComTest()
        {
            var result = new List<Mirror>();
            var ports = SerialPort.GetPortNames();

            if (ports == null || ports.Length == 0)
                return result;

            foreach (var portName in ports.OrderBy(p => p))
            {
                try
                {
                    // If an active port exists with same name and different baud -> skip scanning that port
                    lock (portLock)
                    {
                        var existing = activePorts.FirstOrDefault(p => string.Equals(p.PortName, portName, StringComparison.OrdinalIgnoreCase) && p.IsOpen);
                        if (existing != null && existing.BaudRate != 9600)
                        {
                            // port is already opened for another device with different settings - skip it
                            continue;
                        }
                        if (existing != null && existing.BaudRate == 9600)
                        {
                            // we can reuse this port instance for discovery
                            var respMirror = SendRead(existing, "IDN?\r\n", 500);
                            if (!string.IsNullOrWhiteSpace(respMirror) && respMirror.IndexOf("473", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var m = new Mirror { ComPort = portName, ID = respMirror, IDN = "473" };
                                // get serial number
                                var snMirror = SendRead(existing, "SN?\r\n", 500);
                                if (!string.IsNullOrWhiteSpace(snMirror))
                                    m.SerialNumber = snMirror.Trim();
                                // enable control once
                                try { existing.Write("Control = 1\r\n"); } catch { }
                                result.Add(m);
                            }
                            continue;
                        }
                    }

                    // Create a new SerialPort for testing (do not close if mirror found)
                    var sp = new SerialPort(portName)
                    {
                        BaudRate = 9600,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One,
                        Handshake = Handshake.None,
                        ReadTimeout = 500,
                        WriteTimeout = 500,
                        Encoding = Encoding.ASCII,
                        DtrEnable = false,
                        RtsEnable = false
                    };

                    try
                    {
                        sp.Open();
                    }
                    catch (Exception exOpen)
                    {
                        try { Debug.WriteLine($"MirrorComTest: Failed to open {portName}: {exOpen}"); } catch { }
                        try { sp.Dispose(); } catch { }
                        continue;
                    }

                    // Clear and send IDN?
                    try { sp.DiscardInBuffer(); } catch { }
                    try { sp.Write("IDN?\r\n"); } catch { }

                    var respBuilder = new StringBuilder();
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < 500)
                    {
                        try
                        {
                            var chunk = sp.ReadExisting();
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                respBuilder.Append(chunk);
                                Thread.Sleep(20);
                                continue;
                            }
                        }
                        catch { }
                        Thread.Sleep(20);
                    }
                    sw.Stop();

                    var resp = respBuilder.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(resp) && resp.IndexOf("473", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var m = new Mirror { ComPort = portName, ID = resp, IDN = "473" };

                        // ask for SN? once
                        var sn = SendRead(sp, "SN?\r\n", 500);
                        if (!string.IsNullOrWhiteSpace(sn))
                            m.SerialNumber = sn.Trim();

                        // enable control (turn mirror control on)
                        try { sp.Write("Control = 1\r\n"); } catch { }

                        result.Add(m);

                        // Keep port open by adding to activePorts
                        lock (portLock)
                        {
                            try
                            {
                                if (!activePorts.Any(p => string.Equals(p.PortName, sp.PortName, StringComparison.OrdinalIgnoreCase) && p.IsOpen))
                                {
                                    activePorts.Add(sp);
                                }
                                else
                                {
                                    try { sp.Close(); } catch { }
                                    try { sp.Dispose(); } catch { }
                                }
                            }
                            catch (Exception exAdd)
                            {
                                try { Debug.WriteLine($"MirrorComTest: Failed to add {portName} to activePorts: {exAdd}"); } catch { }
                                try { sp.Close(); } catch { }
                                try { sp.Dispose(); } catch { }
                            }
                        }
                    }
                    else
                    {
                        // Not a mirror -> close
                        try { sp.Close(); } catch { }
                        try { sp.Dispose(); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"MirrorComTest: exception on port {portName}: {ex}"); } catch { }
                }
            }

            return result;
        }

        private const string FakeMirrorComPort = "__FAKE_MIRROR__";

        /// <summary>
        /// Add a removable fake mirror for testing UI/logic without touching serial ports.
        /// Use RemoveFakeMirrors() to remove it and restore normal discovery/update behavior.
        /// </summary>
        public static void AddFakeMirror(
            string idn = "473",
            double mirrorTemp = 23.000,
            double humidity = 10.000,
            double dewPoint = 0.500,
            string serialNumber = "27-123456",
            bool stable = true)
        {
            var fake = new Mirror
            {
                ComPort = FakeMirrorComPort,
                IDN = idn,
                ID = idn,
                MirrorTemp = mirrorTemp,
                Humdity = humidity,
                DewPoint = dewPoint,
                SerialNumber = serialNumber,
                Stable = stable
            };

            lock (mirrorsLock)
            {
                if (ConnectedMirrors == null)
                    ConnectedMirrors = new List<Mirror>();

                // If a fake mirror with the same serial number exists, update its values in-place so any UI references stay valid.
                if (!string.IsNullOrWhiteSpace(serialNumber))
                {
                    var existing = ConnectedMirrors.FirstOrDefault(m =>
                        string.Equals(m?.SerialNumber, serialNumber, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        // Update the existing fake mirror fields in-place so any UI references stay valid.
                        existing.IDN = fake.IDN;
                        existing.ID = fake.ID;
                        existing.MirrorTemp = fake.MirrorTemp;
                        existing.Humdity = fake.Humdity;
                        existing.DewPoint = fake.DewPoint;
                        existing.Stable = fake.Stable;
                        try { Debug.WriteLine("AddFakeMirror: updated existing fake mirror"); } catch { }
                        return;
                    }
                }

                // No existing mirror with that serial -> add new fake mirror.
                ConnectedMirrors.Add(fake);
            }

            try { Debug.WriteLine("AddFakeMirror: fake mirror added"); } catch { }
        }

        /// <summary>
        /// Remove any fake mirrors previously added with `AddFakeMirror`.
        /// </summary>
        public static void RemoveFakeMirrors()
        {
            lock (mirrorsLock)
            {
                if (ConnectedMirrors == null || ConnectedMirrors.Count == 0)
                    return;

                ConnectedMirrors.RemoveAll(m => string.Equals(m?.ComPort, FakeMirrorComPort, StringComparison.OrdinalIgnoreCase));
            }
            try { Debug.WriteLine("RemoveFakeMirrors: fake mirror(s) removed"); } catch { }
        }

        /// <summary>
        /// Update all fake mirrors previously added with AddFakeMirror.
        /// This updates fields in-place so any UI references to the Mirror instances will reflect the changes.
        /// </summary>
        public static void UpdateAllFakeMirrors(double mirrorTemp, double humidity, double dewPoint, string idn = null, bool? stable = null)
        {
            lock (mirrorsLock)
            {
                if (ConnectedMirrors == null || ConnectedMirrors.Count ==0)
                    return;

                foreach (var m in ConnectedMirrors.Where(m => string.Equals(m?.ComPort, FakeMirrorComPort, StringComparison.OrdinalIgnoreCase)))
                {
                    if (m == null)
                        continue;

                    if (!string.IsNullOrEmpty(idn))
                        m.IDN = idn;
                    m.MirrorTemp = mirrorTemp;
                    m.Humdity = humidity;
                    m.DewPoint = dewPoint;
                    if (stable.HasValue)
                        m.Stable = stable.Value;
                }
            }
            try { Debug.WriteLine("UpdateAllFakeMirrors: updated fake mirrors"); } catch { }
        }

        // Update DP? and Tx? for known mirrors. Works with real mirrors (serial IO) and with fake mirrors
        // whose ComPort == FakeMirrorComPort (no IO, just preserve supplied values).
        public static List<Mirror> UpdateMirrorData(List<Mirror> mirrors)
        {
            var updated = new List<Mirror>();
            if (mirrors == null || mirrors.Count == 0)
                return updated;

            foreach (var m in mirrors)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.ComPort))
                    continue;

                // If this is a fake mirror marker, skip serial IO and return the instance as-is
                if (string.Equals(m.ComPort, FakeMirrorComPort, StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure basic fields exist (defensive)
                    if (string.IsNullOrEmpty(m.IDN))
                        m.IDN = "473";
                    // Add to updated list unchanged (no serial IO)
                    updated.Add(m);
                    continue;
                }

                SerialPort sp = null;
                lock (portLock)
                {
                    sp = activePorts.FirstOrDefault(p => string.Equals(p.PortName, m.ComPort, StringComparison.OrdinalIgnoreCase) && p.IsOpen);
                }

                if (sp == null)
                {
                    try { Debug.WriteLine($"UpdateMirrorData: no active port for {m.ComPort}"); } catch { }
                    continue;
                }

                try
                {
                    // Query dew point
                    var dpResp = SendRead(sp, "DP?\r", 500);
                    if (!string.IsNullOrWhiteSpace(dpResp))
                    {
                        double dp;
                        if (double.TryParse(dpResp.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out dp))
                            m.DewPoint = dp;
                    }
                    // Query humidity
                    var hum = SendRead(sp, "RH?\r", 500);
                    if (!string.IsNullOrWhiteSpace(hum))
                    {
                        double dp;
                        if (double.TryParse(hum.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out dp))
                            m.Humdity = dp;
                    }

                    // Query mirror temp (Tx? uses external temp or mirror temp depending on device)
                    var tmResp = SendRead(sp, "Tx?\r", 500);
                    if (!string.IsNullOrWhiteSpace(tmResp))
                    {
                        double tm;
                        if (double.TryParse(tmResp.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out tm))
                            m.MirrorTemp = tm;
                    }

                    // Optionally query Stable? if device supports
                    var stableResp = SendRead(sp, "Stable?\r\n", 250);
                    if (!string.IsNullOrWhiteSpace(stableResp))
                    {
                        var stab = stableResp.Trim();
                        if (stab == "1" || stab.Equals("true", StringComparison.OrdinalIgnoreCase))
                            m.Stable = true;
                        else if (stab == "0" || stab.Equals("false", StringComparison.OrdinalIgnoreCase))
                            m.Stable = false;
                    }

                    updated.Add(m);
                }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"UpdateMirrorData: failed for {m.ComPort}: {ex}"); } catch { }
                }
            }

            return updated;
        }

        // Set control for all discovered mirrors (true -> 1, false -> 0)
        private static void SetAllMirrorControl(bool on)
        {
            List<Mirror> snapshot;
            lock (mirrorsLock)
            {
                snapshot = ConnectedMirrors?.ToList() ?? new List<Mirror>();
            }

            foreach (var m in snapshot)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.ComPort))
                    continue;

                SerialPort sp = null;
                lock (portLock)
                {
                    sp = activePorts.FirstOrDefault(p => string.Equals(p.PortName, m.ComPort, StringComparison.OrdinalIgnoreCase) && p.IsOpen);
                }

                if (sp == null)
                    continue;

                try
                {
                    var cmd = on ? "Control = 1\r" : "Control = 0\r";
                    sp.Write(cmd);
                }
                catch { }
            }
        }

        public static List<RotProbe> MirrorData()
        {
            // keep a simple placeholder; real discovery happens in MonitorCallback via MirrorComTest
            return new List<RotProbe>();
        }

        // Stop monitoring and dispose timer
        public static void StopProbeMonitoring()
        {
            lock (timerLock)
            {
                if (!monitoringStarted)
                    return;

                try
                {
                    probeTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
                catch { }

                try
                {
                    probeTimer?.Dispose();
                }
                catch { }

                probeTimer = null;
                monitoringStarted = false;
                try { Debug.WriteLine("StopProbeMonitoring: stopped"); } catch { }
            }
        }

        // Safe snapshot for UI/forms
        public static List<RotProbe> GetConnectedProbesSnapshot()
        {
            lock (probesLock)
            {
                // Return shallow copy to avoid callers mutating internal list
                return ConnectedProbes?.ToList() ?? new List<RotProbe>();
            }
        }

        public static List<Mirror> GetConnectedMirrorsSnapshot()
        {
            lock (mirrorsLock)
            {
                return ConnectedMirrors?.ToList() ?? new List<Mirror>();
            }
        }

        // Timer callback: discover on first run (ComTest), otherwise update existing (probes + mirrors)
        private static void MonitorCallback(object state)
        {
            // Prevent overlapping timer callbacks
            try
            {
                probeTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch { }

            try
            {
                // MIRRORS DISCOVERY (moved before probes so mirrors get first claim on ports)
                lock (mirrorsLock)
                {
                    if (ConnectedMirrors == null || ConnectedMirrors.Count == 0)
                    {
                        try
                        {
                            var discoveredMirrors = MirrorComTest() ?? new List<Mirror>();
                            ConnectedMirrors = discoveredMirrors;
                            try { Debug.WriteLine($"MonitorCallback: discovered {discoveredMirrors.Count} mirrors"); } catch { }
                        }
                        catch (Exception ex)
                        {
                            try { Debug.WriteLine($"MonitorCallback: MirrorComTest failed: {ex}"); } catch { }
                        }
                    }
                }

                // PROBES DISCOVERY
                lock (probesLock)
                {
                    if (ConnectedProbes == null || ConnectedProbes.Count == 0)
                    {
                        try
                        {
                            var discovered = ComTest() ?? new List<RotProbe>();
                            ConnectedProbes = discovered;
                            try { Debug.WriteLine($"MonitorCallback: discovered {discovered.Count} probes"); } catch { }
                        }
                        catch (Exception ex)
                        {
                            try { Debug.WriteLine($"MonitorCallback: ComTest failed: {ex}"); } catch { }
                        }
                    }
                }

                // Otherwise, update data for existing probes
                List<RotProbe> probeSnapshot;
                lock (probesLock)
                {
                    // Work on a shallow copy to avoid holding lock during IO
                    probeSnapshot = ConnectedProbes.ToList();
                }

                List<RotProbe> updatedProbes = null;
                try
                {
                    updatedProbes = UpdateData(probeSnapshot) ?? new List<RotProbe>();
                }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"MonitorCallback: UpdateData threw: {ex}"); } catch { }
                }

                if (updatedProbes != null && updatedProbes.Count > 0)
                {
                    // Merge updated probes into the main ConnectedProbes list
                    lock (probesLock)
                    {
                        foreach (var up in updatedProbes)
                        {
                            if (up == null || string.IsNullOrWhiteSpace(up.ComPort))
                                continue;

                            var idx = ConnectedProbes.FindIndex(p => string.Equals(p?.ComPort, up.ComPort, StringComparison.OrdinalIgnoreCase));
                            if (idx >= 0)
                            {
                                // Replace reference so UI consumers see the updated object instance
                                ConnectedProbes[idx] = up;
                            }
                            else
                            {
                                // New probe discovered during update
                                ConnectedProbes.Add(up);
                            }
                        }
                    }
                }

                // Update mirrors periodically (DP? and Tx?)
                List<Mirror> mirrorSnapshot;
                lock (mirrorsLock)
                {
                    mirrorSnapshot = ConnectedMirrors?.ToList() ?? new List<Mirror>();
                }

                if (mirrorSnapshot != null && mirrorSnapshot.Count > 0)
                {
                    var updatedMirrors = UpdateMirrorData(mirrorSnapshot);
                    if (updatedMirrors != null && updatedMirrors.Count > 0)
                    {
                        lock (mirrorsLock)
                        {
                            foreach (var um in updatedMirrors)
                            {
                                if (um == null || string.IsNullOrWhiteSpace(um.ComPort))
                                    continue;

                                // Prefer to match by SerialNumber when available (fake mirrors share the same ComPort)
                                int idx = -1;
                                if (!string.IsNullOrWhiteSpace(um.SerialNumber))
                                {
                                    idx = ConnectedMirrors.FindIndex(x => string.Equals(x?.SerialNumber, um.SerialNumber, StringComparison.OrdinalIgnoreCase));
                                }

                                // Fallback: try to match by reference (updatedMirrors may contain same object instances)
                                if (idx <0)
                                {
                                    idx = ConnectedMirrors.FindIndex(x => Object.ReferenceEquals(x, um));
                                }

                                // Final fallback: match by ComPort (legacy behavior) only if no serial/reference match
                                if (idx <0)
                                {
                                    idx = ConnectedMirrors.FindIndex(x => string.Equals(x?.ComPort, um.ComPort, StringComparison.OrdinalIgnoreCase));
                                }

                                if (idx >=0)
                                {
                                    ConnectedMirrors[idx] = um;
                                }
                                else
                                {
                                    ConnectedMirrors.Add(um);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exOuter)
            {
                try { Debug.WriteLine($"MonitorCallback: unexpected error: {exOuter}"); } catch { }
            }
            finally
            {
                // Re-enable the periodic timer (use same interval you started with)
                try
                {
                    probeTimer?.Change(2000, 2000); // adjust interval if you changed it elsewhere
                }
                catch { }
            }
        }

        private static RotProbe CalibrationCoefficients(RotProbe probe)
        {
            if (probe == null)
                return null;

            if (string.IsNullOrWhiteSpace(probe.ComPort))
                return probe;

            SerialPort sp = null;
            lock (portLock)
            {
                sp = activePorts
                    .FirstOrDefault(p => string.Equals(p.PortName, probe.ComPort, StringComparison.OrdinalIgnoreCase) && p.IsOpen);
            }

            if (sp == null)
            {
                try { System.Diagnostics.Debug.WriteLine($"CalibrationCoefficients: no active port for {probe.ComPort}"); } catch { }
                return probe;
            }

            // Helper: send command and read response with a simple polling loop
            Func<SerialPort, string, int, string> SendRead = (port, cmd, timeoutMs) =>
            {
                try
                {
                    try { port.DiscardInBuffer(); } catch { }
                    port.Write(cmd);
                }
                catch (Exception ex)
                {
                    try { System.Diagnostics.Debug.WriteLine($"SendRead: write failed: {ex}"); } catch { }
                    return string.Empty;
                }

                var sb = new StringBuilder();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    try
                    {
                        var chunk = port.ReadExisting();
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            sb.Append(chunk);
                            // allow remaining bytes to arrive
                            Thread.Sleep(20);
                            continue;
                        }
                    }
                    catch
                    {
                        // ignore read errors
                    }
                    Thread.Sleep(20);
                }
                sw.Stop();
                return sb.ToString().Trim();
            };

            // Helper: parse first four numeric tokens as bytes and convert to single-precision float
            Func<string, double> ParseFourByteFloatFromResponse = (response) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(response))
                        return double.NaN;

                    // isolate payload starting at '{' if present
                    var start = response.IndexOf('{');
                    var payload = start >= 0 ? response.Substring(start + 1) : response;
                    // trim trailing braces/control chars
                    payload = payload.Trim().TrimEnd('}', '\r', '\n');

                    // First attempt: split on ';' (preserve empty entries) and extract numeric substrings from each segment.
                    var rawSegments = payload.Split(new[] { ';' }, StringSplitOptions.None)
                                             .Select(s => s.Trim())
                                             .ToArray();

                    int foundIndex = -1;
                    int[] foundVals = null;

                    for (int i = 0; i + 3 < rawSegments.Length; i++)
                    {

                        var vals = new int[4];
                        bool ok = true;
                        for (int j = 0; j < 4; j++)
                        {
                            var seg = rawSegments[i + j];

                            // If segment is null treat as empty string
                            if (seg == null)
                                seg = string.Empty;

                            // If seg is longer than 3 characters, trim to only the last 3 characters
                            if (seg.Length > 3)
                            {
                                seg = seg.Substring(seg.Length - 3);
                            }

                            // Extract first numeric substring (1-3 digits) within the segment
                            var m = System.Text.RegularExpressions.Regex.Match(seg ?? string.Empty, @"(\d{1,3})");
                            if (!m.Success)
                            {
                                ok = false;
                                break;
                            }
                            int v;
                            if (!int.TryParse(m.Groups[1].Value, out v) || v < 0 || v > 255)
                            {
                                ok = false;
                                break;
                            }
                            vals[j] = v;
                        }
                        if (ok)
                        {
                            foundIndex = i;
                            foundVals = vals;
                            break;
                        }
                    }

                    if (foundIndex < 0)
                    {
                        // fallback: try to extract numeric tokens anywhere (old behavior)
                        var matches = System.Text.RegularExpressions.Regex.Matches(payload, @"\b(\d{1,3})\b");
                        if (matches.Count < 4)
                            return double.NaN;

                        var fallbackBytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            int v;
                            if (!int.TryParse(matches[i].Groups[1].Value, out v))
                                return double.NaN;
                            fallbackBytes[i] = (byte)(v & 0xFF);
                        }

                        // Device order is fixed (MSB first / big-endian). BitConverter expects machine endianness.
                        // Convert to the machine endianness before ToSingle.
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(fallbackBytes);

                        float ffb = BitConverter.ToSingle(fallbackBytes, 0);
                        return (float.IsInfinity(ffb) || float.IsNaN(ffb)) ? double.NaN : (double)ffb;
                    }

                    // Build bytes from the found numeric values. Device transmits in fixed order (big-endian);
                    var bytes = new byte[4];
                    bytes[0] = (byte)(foundVals[0] & 0xFF);
                    bytes[1] = (byte)(foundVals[1] & 0xFF);
                    bytes[2] = (byte)(foundVals[2] & 0xFF);
                    bytes[3] = (byte)(foundVals[3] & 0xFF);

                    // Convert big-endian bytes -> machine endianness for BitConverter
                    //if (BitConverter.IsLittleEndian)
                    //    Array.Reverse(bytes);

                    float value = BitConverter.ToSingle(bytes, 0);
                    if (float.IsInfinity(value) || float.IsNaN(value))
                        return double.NaN;

                    return (double)value;
                }
                catch (Exception ex)
                {
                    try { System.Diagnostics.Debug.WriteLine($"ParseFourByteFloatFromResponse: {ex}"); } catch { }
                    return double.NaN;
                }
            };

            // Map addresses to actions
            var addressMap = new[]
            {
                new { Addr = 1295, Assign = new Action<double>(v => probe.PT100CoeffA = v) },
                new { Addr = 1299, Assign = new Action<double>(v => probe.PT100CoeffB = v) },
                new { Addr = 1300, Assign = new Action<double>(v => probe.PT100CoeffC = v) },
                new { Addr = 1278, Assign = new Action<double>(v => probe.TempOffset = v) },
                new { Addr = 1287, Assign = new Action<double>(v => probe.TempConversion = v) }
            };

            // Determine device type character to use in command
            string deviceTypeStr = null;
            try
            {
                if (probe.DeviceType != '\0')
                    deviceTypeStr = probe.DeviceType.ToString();
                else if (!string.IsNullOrEmpty(probe.DeviceModel))
                    deviceTypeStr = probe.DeviceModel.Substring(0, 1);
            }
            catch { /* ignore */ }

            if (string.IsNullOrEmpty(deviceTypeStr))
                deviceTypeStr = "F"; // sensible default

            var probeAddr = string.IsNullOrEmpty(probe.ProbeAddress) ? "00" : probe.ProbeAddress;

            foreach (var entry in addressMap)
            {
                try
                {
                    // Format: "{<DeviceType><ProbeAddress>ERD 0;<address>;004}\r"
                    var cmd = "{" + deviceTypeStr + probeAddr + "ERD 0;" + entry.Addr.ToString(CultureInfo.InvariantCulture) + ";004}\r";
                    var resp = SendRead(sp, cmd, 500);
                    var val = ParseFourByteFloatFromResponse(resp);
                    entry.Assign(val);
                    // brief pause between commands
                    Thread.Sleep(20);
                }
                catch (Exception ex)
                {
                    try { System.Diagnostics.Debug.WriteLine($"CalibrationCoefficients: error reading addr {entry.Addr}: {ex}"); } catch { }
                }
            }

            return probe;
        }
        // ComTest now returns a list of RotProbe objects parsed from device responses.
        public static List<RotProbe> ComTest()
        {
            string command = "{ 99RDD}\r";
            var ports = SerialPort.GetPortNames();

            if (ports == null || ports.Length == 0)
            {
                // No COM ports found; nothing to do.
                return new List<RotProbe>();
            }

            var results = new List<RotProbe>();

            foreach (var portName in ports.OrderBy(p => p))
            {
                try
                {
                    // Quick check: if we already have this port open, skip
                    lock (portLock)
                    {
                        if (activePorts.Any(p => string.Equals(p.PortName, portName, StringComparison.OrdinalIgnoreCase) && p.IsOpen))
                        {
                            continue;
                        }
                    }

                    // Skip ports already known as mirrors to avoid stomping the mirror connection
                    lock (mirrorsLock)
                    {
                        if (ConnectedMirrors != null && ConnectedMirrors.Any(m => string.Equals(m.ComPort, portName, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }

                    // Create but do not wrap in using: may be kept open
                    var sp = new SerialPort(portName)
                    {
                        BaudRate = 19200,
                        Parity = Parity.None,
                        DataBits = 8,
                        StopBits = StopBits.One,
                        Handshake = Handshake.None,
                        ReadTimeout = 500,
                        WriteTimeout =500 ,
                        Encoding = Encoding.ASCII,
                        DtrEnable = false,
                        RtsEnable = false
                    };

                    try
                    {
                        sp.Open();
                    }
                    catch (Exception exOpen)
                    {
                        // Opening this port failed; record debug and try next port
                        try { Debug.WriteLine($"ComTest: Failed to open {portName}: {exOpen}"); } catch { }
                        try { sp.Dispose(); } catch { }
                        continue;
                    }

                    // Clear input buffer before sending
                    try { sp.DiscardInBuffer(); } catch { /* ignore */ }

                    // Send command
                    try { sp.Write(command); } catch (Exception) { /* ignore write errors */ }

                    // Read response with small polling loop (collect but do not log)
                    var sw = Stopwatch.StartNew();
                    var responseBuilder = new StringBuilder();

                    while (sw.ElapsedMilliseconds < 500)
                    {
                        try
                        {
                            string chunk = sp.ReadExisting();
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                responseBuilder.Append(chunk);
                                // brief pause to allow remaining bytes
                                Thread.Sleep(20);
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                            // ReadExisting typically doesn't throw on timeout; ignore
                        }

                        // no data currently available; wait a bit before next poll
                        Thread.Sleep(20);
                    }

                    sw.Stop();

                    // Decide whether to keep the port open
                    if (responseBuilder.Length > 0)
                    {
                        // Trim the response
                        var trimmedResponse = responseBuilder.ToString().Trim();

                        // Build RotProbe from the response
                        try
                        {
                            var probe = ParseResponseToRotProbe(sp.PortName, trimmedResponse);
                            if (probe != null)
                            {
                                results.Add(probe);

                                // Keep the port open for the lifetime of the application
                                lock (portLock)
                                {
                                    try
                                    {
                                        // Re-check to avoid duplicates if race occurred
                                        if (!activePorts.Any(p => string.Equals(p.PortName, sp.PortName, StringComparison.OrdinalIgnoreCase) && p.IsOpen))
                                        {
                                            activePorts.Add(sp);
                                        }
                                        else
                                        {
                                            // Duplicate found; close this instance
                                            try { sp.Close(); } catch { }
                                            try { sp.Dispose(); } catch { }
                                        }
                                    }
                                    catch (Exception exAdd)
                                    {
                                        try { Debug.WriteLine($"ComTest: Failed to add {portName} to activePorts: {exAdd}"); } catch { }
                                        try { sp.Close(); } catch { }
                                        try { sp.Dispose(); } catch { }
                                    }
                                }
                            }
                            else
                            {
                                // parsing failed -> dispose port
                                try { sp.Close(); } catch { }
                                try { sp.Dispose(); } catch { }
                            }
                        }
                        catch (Exception exParse)
                        {
                            try { Debug.WriteLine($"ComTest: Failed to parse response from {portName}: {exParse}"); } catch { }
                            try { sp.Close(); } catch { }
                            try { sp.Dispose(); } catch { }
                        }
                    }
                    else
                    {
                        // No response: close/dispose this port
                        try { sp.Close(); } catch { }
                        try { sp.Dispose(); } catch { }
                    }
                }
                catch (Exception exPort)
                {
                    // Minimal non-persistent reporting for diagnostics
                    try { Debug.WriteLine($"ComTest: exception on port {portName}: {exPort}"); } catch { }
                }
            }

            // Run CalibrationCoefficients for each discovered probe.
            for (int i = 0; i < results.Count; i++)
            {
                try
                {
                    var updated = CalibrationCoefficients(results[i]);
                    if (updated != null)
                    {
                        results[i] = updated;
                    }
                }
                catch (Exception exCal)
                {
                    try { Debug.WriteLine($"ComTest: CalibrationCoefficients failed for {results[i]?.ComPort}: {exCal}"); } catch { }
                }
            }
            results = UpdateData(results);
            return results;
        }

        private static RotProbe ParseResponseToRotProbe(string comPort, string response)
        {
            /*
            PSEUDOCODE / DETAILED PLAN:
            - Defensive checks: return null on null/whitespace response.
            - Extract payload starting at '{' if present; trim trailing '}', CR, LF.
            - Remove trailing single-character checksum-like last segment if present.
            - Split into parts on ';' preserving empty entries and trim each part.
            - If parts length < 10 -> return null (unexpected format).
            - Create RotProbe and set ComPort.
            - Parse the first part which can contain a device token and a numeric probe code, e.g. "F00erd 050" or "F00rdd 001".
              - Split the first part on whitespace into tokens.
              - The device token (first token) yields DeviceType (first char) and ProbeAddress (substring starting at index 1, up to 2 chars).
              - Extract the numeric probe identifier (ProbeType) robustly:
                - Prefer the last digit-group found in the entire first segment (regex \d+).
                - If none found, fallback to the second whitespace token if present.
              - IMPORTANT: Strip the leading device token and any intervening whitespace from the first part so that the first part
                becomes only the numeric probe code (e.g. "050"). Update parts[0] to this stripped value so subsequent parsing that
                might rely on parts[0] receives the numeric-only string.
            - Parse numeric fields from parts[] defensively using InvariantCulture; assign to RotProbe properties.
            - Return the constructed RotProbe.
            */

            // Defensive checks
            if (string.IsNullOrWhiteSpace(response))
                return null;

            // Extract payload starting at '{' if present
            var payloadStart = response.IndexOf('{');
            var payload = payloadStart >= 0 ? response.Substring(payloadStart + 1) : response;

            // Trim and remove trailing braces/control characters
            payload = payload.Trim();
            payload = payload.TrimEnd('}', '\r', '\n');

            // Remove trailing checksum-like single-character segment if present
            var segmentsForChecksum = payload.Split(';');
            if (segmentsForChecksum.Length > 0)
            {
                var last = segmentsForChecksum[segmentsForChecksum.Length - 1].Trim();
                if (last.Length == 1)
                {
                    payload = string.Join(";", segmentsForChecksum.Take(segmentsForChecksum.Length - 1));
                }
            }

            // Split into parts
            var parts = payload.Split(new[] { ';' }, StringSplitOptions.None)
                               .Select(p => p.Trim())
                               .ToArray();

            if (parts.Length < 10)
            {
                try { Debug.WriteLine($"ParseResponseToRotProbe: unexpected parts length {parts.Length} for response '{response}'"); } catch { }
                return null;
            }

            var rp = new RotProbe();
            rp.ComPort = comPort;

            // First part contains combined device info and probe type, e.g. "F00rdd 001" or "F00erd 050"
            var first = parts[0];
            var firstTokens = first.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Extract device token (first whitespace token) to determine DeviceType and ProbeAddress
            if (firstTokens.Length >= 1)
            {
                var devToken = firstTokens[0];
                if (!string.IsNullOrEmpty(devToken))
                {
                    rp.DeviceType = devToken[0];

                    // ProbeAddress: characters starting at 1 (typically 1 or 2 chars)
                    if (devToken.Length >= 2)
                    {
                        var addrStr = devToken.Substring(1, Math.Min(2, devToken.Length - 1));
                        rp.ProbeAddress = addrStr;
                    }
                }
            }

            // Extract numeric-only probe identifier (e.g., "050") from the first segment.
            // Priority: last digit group found in the entire first segment; fallback to second whitespace token.
            string numericProbeType = null;
            try
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(first ?? string.Empty, @"\d+");
                if (matches.Count > 0)
                {
                    numericProbeType = matches[matches.Count - 1].Value;
                }
                else if (firstTokens.Length >= 2)
                {
                    numericProbeType = firstTokens[1];
                }
            }
            catch
            {
                // ignore regex failures
            }

            // Strip the leading device token (e.g., "F00erd ") from the first part so parts[0] becomes the numeric-only probe type.
            // This ensures downstream parsing that may inspect parts[0] sees "050" rather than "F00erd 050".
            if (!string.IsNullOrEmpty(numericProbeType))
            {
                var idx = first.IndexOf(numericProbeType, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    first = first.Substring(idx).Trim();
                }
                else if (firstTokens.Length >= 2)
                {
                    first = firstTokens[1].Trim();
                }
                parts[0] = first;
            }

            rp.ProbeType = numericProbeType;

            // Helper for parsing doubles using InvariantCulture
            Func<string, double> parseDoubleOrNaN = s =>
            {
                double v;
                return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : double.NaN;
            };

            // Map fields defensively by index based on observed format
            // parts[1] = Humidity
            if (parts.Length > 1)
            {
                rp.Humidity = parseDoubleOrNaN(parts[1]);
            }
            // parts[2] = HumidityUnit
            if (parts.Length > 2)
            {
                rp.HumidityUnit = parts[2];
            }
            // parts[3] = HumidityAlarm (string "000" expected)
            if (parts.Length > 3)
            {
                rp.HumidityAlarm = !string.Equals(parts[3], "000", StringComparison.OrdinalIgnoreCase);
            }
            // parts[4] = HumidityTrend
            if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4]))
            {
                rp.HumidityTrend = parts[4][0];
            }

            // parts[5] = Temperature
            if (parts.Length > 5)
            {
                rp.Temperature = parseDoubleOrNaN(parts[5]);
            }

            if (parts.Length > 6)
            {
                // Replace any '?' placeholder with the Unicode degree symbol U+00B0
                var unit = parts[6];
                if (!string.IsNullOrEmpty(unit))
                {
                    unit = unit.Replace('?', '\u00B0');
                }
                rp.TemperatureUnit = unit;
            }

            // parts[7] = TemperatureAlarm
            if (parts.Length > 7)
            {
                rp.TemperatureAlarm = !string.Equals(parts[7], "000", StringComparison.OrdinalIgnoreCase);
            }
            // parts[8] = TemperatureTrend
            if (parts.Length > 8 && !string.IsNullOrEmpty(parts[8]))
            {
                rp.TemperatureTrend = parts[8][0];
            }

            // parts[9] = CalculatedParameter
            if (parts.Length > 9)
            {
                rp.CalculatedParameter = parts[9];
            }
            // parts[10] = CalculatedValue
            if (parts.Length > 10)
            {
                var val = parts[10];
                // some values are '---.-' which can't be parsed
                rp.CalculatedValue = parseDoubleOrNaN(val);
            }
            // parts[11] = CalculatedUnit
            if (parts.Length > 11)
            {
                rp.CalculatedUnit = parts[11];
            }
            // parts[12] = CalculatedAlarm
            if (parts.Length > 12)
            {
                rp.CalculatedAlarm = !string.Equals(parts[12], "000", StringComparison.OrdinalIgnoreCase);
            }
            // parts[13] = CalculatedTrend
            if (parts.Length > 13 && !string.IsNullOrEmpty(parts[13]))
            {
                rp.CalculatedTrend = parts[13][0];
            }
            // parts[14] = DeviceModel
            if (parts.Length > 14)
                rp.DeviceModel = parts[14];

            // parts[15] = FirmwareVersion (observed location)
            if (parts.Length > 15)
            {
                rp.FirmwareVersion = parts[15];
            }
            // parts[16] = SerialNumber
            if (parts.Length > 16)
            {
                rp.SerialNumber = parts[16];
            }
            // parts[17] = DeviceName
            if (parts.Length > 17)
            {
                rp.DeviceName = parts[17];
            }
            // parts[18] = AlarmByte
            if (parts.Length > 18)
            {
                rp.AlarmByte = parts[18];
            }
            if (rp.TemperatureUnit == "°F" && _globalTemperatureC)
            {
                rp.CelsiusHelper = true;
                rp.TemperatureUnit = "°C";
                rp.Temperature = Math.Round((rp.Temperature - 32) * 5.0 / 9.0, 2);
            }
            return rp;
        }

        // PSEUDOCODE / DETAILED PLAN:
        // - For each RotProbe in ConnectedProbes:
        //   - Find an open SerialPort in activePorts whose PortName matches probe.ComPort (case-insensitive).
        //   - If no port found, skip this probe (do not throw), continue to next.
        //   - Build the TST command using probe.DeviceType and probe.ProbeAddress:
        //       command = "{" + deviceType + probeAddress + "TST 10}\r"
        //   - Send the command and read the response with a short polling loop:
        //       * Discard input buffer, write command.
        //       * Loop until timeout (e.g., 2000ms) and collect ReadExisting() chunks.
        //       * After each non-empty chunk, allow a small delay to gather remaining bytes.
        //   - If no response, skip this probe.
        //   - Trim response, isolate payload after '{' and trim trailing '}', CR, LF.
        //   - Split payload on ';' preserving empty entries and trim each segment.
        //   - Map expected segments to fields:
        //       0: header segment like "F00tst 17822" -> extract first integer => HumidityCount
        //       1: HumdityRaw (double)
        //       2: HumidityFactoryCorrection (double)
        //       3: HumidityUserCorrection (double)
        //       4: HumidityTemperatureCorrection (double)
        //       5: HumidityDriftCorrection (double)
        //       6: Humidity (double)
        //       7: TemperatureCount (int) (may contain leading zeros)
        //       8: Resistance (double)
        //       9: Temperature (double)
        //   - Parse numbers defensively using InvariantCulture; on parse failure assign NaN (for doubles) or leave default (for ints).
        //   - Update the probe object's properties in-place.
        //   - Add the updated probe to the returned list.
        // - Return list of updated probe instances.
        public static List<RotProbe> UpdateData(List<RotProbe> ConnectedProbes)
        {
            var updatedProbes = new List<RotProbe>();
            if (ConnectedProbes == null || ConnectedProbes.Count == 0)
                return updatedProbes;

            foreach (var probe in ConnectedProbes)
            {
                if (probe == null || string.IsNullOrWhiteSpace(probe.ComPort))
                    continue;

                SerialPort sp = null;
                lock (portLock)
                {
                    sp = activePorts.FirstOrDefault(p => string.Equals(p.PortName, probe.ComPort, StringComparison.OrdinalIgnoreCase) && p.IsOpen);
                }

                if (sp == null)
                {
                    try { Debug.WriteLine($"UpdateData: no active port for {probe.ComPort}"); } catch { }
                    continue;
                }

                // Helper: send command and read response with a polling loop
                Func<SerialPort, string, int, string> SendRead = (port, cmd, timeoutMs) =>
                {
                    try
                    {
                        try { port.DiscardInBuffer(); } catch { }
                        port.Write(cmd);
                    }
                    catch (Exception ex)
                    {
                        try { Debug.WriteLine($"UpdateData: write failed to {port.PortName}: {ex}"); } catch { }
                        return string.Empty;
                    }

                    var sb = new StringBuilder();
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < timeoutMs)
                    {
                        try
                        {
                            var chunk = port.ReadExisting();
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                sb.Append(chunk);
                                // give device a moment to finish sending
                                Thread.Sleep(20);
                                continue;
                            }
                        }
                        catch
                        {
                            // ignore read errors
                        }
                        Thread.Sleep(50);
                    }
                    sw.Stop();
                    return sb.ToString().Trim();
                };

                try
                {
                    var deviceTypeChar = probe.DeviceType != '\0' ? probe.DeviceType : 'F';
                    var probeAddr = string.IsNullOrEmpty(probe.ProbeAddress) ? "00" : probe.ProbeAddress;
                    var cmd = "{" + deviceTypeChar + probeAddr + "TST 10}\r";

                    var resp = SendRead(sp, cmd, 500);
                    if (string.IsNullOrWhiteSpace(resp))
                    {
                        // no response for this probe
                        continue;
                    }

                    // Isolate payload after '{' if present
                    var start = resp.IndexOf('{');
                    var payload = start >= 0 ? resp.Substring(start + 1) : resp;
                    payload = payload.Trim();
                    payload = payload.TrimEnd('}', '\r', '\n');

                    // Split on ';' preserving empty entries and trim parts
                    var parts = payload.Split(new[] { ';' }, StringSplitOptions.None)
                                       .Select(p => p.Trim())
                                       .ToArray();

                    if (parts.Length < 1)
                    {
                        continue;
                    }

                    // Helper parse methods
                    Func<string, double> parseDoubleOrNaN = s =>
                    {
                        double v;
                        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : double.NaN;
                    };
                    Func<string, int> parseIntOrZero = s =>
                    {
                        int v;
                        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
                    };

                    // New helper: extract the most relevant numeric substring from a header string.
                    // Prefer the last numeric group with length >= 3; otherwise return the last group if any.
                    Func<string, string> ExtractMostRelevantNumeric = header =>
                    {
                        if (string.IsNullOrWhiteSpace(header))
                            return string.Empty;
                        try
                        {
                            var matches = System.Text.RegularExpressions.Regex.Matches(header, @"\d+");
                            if (matches.Count == 0)
                                return string.Empty;

                            // Prefer a longer group near the end (likely measurement value), avoid small device-id groups like "00"
                            for (int i = matches.Count - 1; i >= 0; i--)
                            {
                                var v = matches[i].Value;
                                if (v.Length >= 3)
                                    return v;
                            }

                            // If none >= 3, return the last group found
                            return matches[matches.Count - 1].Value;
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    };

                    // parts[0]: like "F00tst 17822" -> extract the most relevant numeric token as HumidityCount
                    try
                    {
                        var header = parts[0] ?? string.Empty;
                        var numericToken = ExtractMostRelevantNumeric(header);

                        if (!string.IsNullOrEmpty(numericToken))
                        {
                            probe.HumidityCount = parseIntOrZero(numericToken);
                        }
                        else
                        {
                            // fallback: extract first numeric occurrence
                            var m = System.Text.RegularExpressions.Regex.Match(header, @"\d+");
                            if (m.Success)
                            {
                                probe.HumidityCount = parseIntOrZero(m.Value);
                            }
                        }
                    }
                    catch { /* ignore */ }

                    // Map other parts by index with defensive checks
                    if (parts.Length > 1)
                        probe.HumdityRaw = parseDoubleOrNaN(parts[1]);
                    if (parts.Length > 2)
                        probe.HumidityFactoryCorrection = parseDoubleOrNaN(parts[2]);
                    if (parts.Length > 3)
                        probe.HumidityUserCorrection = parseDoubleOrNaN(parts[3]);
                    if (parts.Length > 4)
                        probe.HumidityTemperatureCorrection = parseDoubleOrNaN(parts[4]);
                    if (parts.Length > 5)
                        probe.HumidityDriftCorrection = parseDoubleOrNaN(parts[5]);
                    if (parts.Length > 6)
                        probe.Humidity = parseDoubleOrNaN(parts[6]);

                    if (parts.Length > 7)
                    {
                        // TemperatureCount may contain leading zeros; use int parse if possible
                        var tc = parts[7];
                        // Trim any non-digit prefix/suffix and take first run of digits
                        var m2 = System.Text.RegularExpressions.Regex.Match(tc ?? string.Empty, @"\d+");
                        if (m2.Success)
                            probe.TemperatureCount = parseIntOrZero(m2.Value);
                    }

                    if (parts.Length > 8)
                        probe.Resistance = parseDoubleOrNaN(parts[8]);
                    if (parts.Length > 9)
                        probe.Temperature = parseDoubleOrNaN(parts[9]);
                    if (_globalTemperatureC && probe.CelsiusHelper)
                    {
                        probe.Temperature = Math.Round((probe.Temperature - 32) * 5.0 / 9.0, 2);
                        probe.TemperatureUnit = "°C";
                    }
                    else if (!_globalTemperatureC && probe.CelsiusHelper)
                    {
                        probe.TemperatureUnit = "°F";
                    }
                    // Add updated probe to results
                    updatedProbes.Add(probe);
                }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"UpdateData: failed for {probe.ComPort}: {ex}"); } catch { }
                    // continue with other probes
                }
            }

            return updatedProbes;
        }



        private static void CloseActivePorts()
        {
            lock (portLock)
            {
                if (activePorts.Count == 0)
                {
                    return;
                }

                foreach (var sp in activePorts.ToList())
                {
                    try
                    {
                        if (sp != null)
                        {
                            try
                            {
                                if (sp.IsOpen)
                                {
                                    try { sp.Close(); } catch { }
                                }
                            }
                            catch { /* ignore */ }

                            try { sp.Dispose(); } catch { }
                        }
                    }
                    catch
                    {
                        // swallow any exception during cleanup
                    }
                }

                activePorts.Clear();
            }
        }

        // PSEUDOCODE / DETAILED PLAN (for new fake probe helpers):
        // - Add a constant FakeProbeComPort = "__FAKE_PROBE__".
        // - Implement AddFakeProbe(...) with parameters to allow setting common RotProbe fields:
        //     - probeType, probeAddress, deviceType, humidity, temperature, temperatureUnit,
        //       calculatedParameter, calculatedValue, deviceModel, deviceName, serialNumber.
        //   Steps inside AddFakeProbe:
        //     - Build a new RotProbe instance with ComPort = FakeProbeComPort and populate fields.
        //     - If temperatureUnit == "°F" and _globalTemperatureC is true -> mark CelsiusHelper and convert to °C.
        //     - Lock probesLock, ensure ConnectedProbes list exists.
        //     - If serialNumber provided and a fake probe with same serial exists -> update that instance in-place and return.
        //     - Otherwise add the new fake probe to ConnectedProbes.
        //     - Debug.WriteLine for visibility.
        // - Implement RemoveFakeProbes():
        //     - Lock probesLock and remove all probes whose ComPort equals FakeProbeComPort.
        //     - Debug.WriteLine.
        // - Implement UpdateAllFakeProbes(...) to update values of all fake probes in-place:
        //     - Lock probesLock; iterate ConnectedProbes where ComPort == FakeProbeComPort and set fields provided.
        //     - Handle temperature unit/ conversion and CelsiusHelper similarly.
        //     - Debug.WriteLine.
        // - Use the same locking & debug style as existing mirror helpers so UI references remain valid.

        private const string FakeProbeComPort = "__FAKE_PROBE__";

        /// <summary>
        /// Add a removable fake RotProbe for testing UI/logic without touching serial ports.
        /// Use RemoveFakeProbes() to remove them and restore normal discovery/update behavior.
        /// If serialNumber matches an existing fake probe, update that instance in-place.
        /// </summary>
        public static void AddFakeProbe(
            string ComPort = "COM",
            string ProbeType = "001",
            int HumidityCount = 19300,
            double HumdityRaw = 10.0,
            double HumidityUserCorrection = 0.0,
            double HumidityTemperatureCorrection = 0.0,
            double HumidityDriftCorrection = 0.0,
            string HumidityUnit = "%rh",
            bool HumidityAlarm = false,
            char HumidityTrend = '=',
            double Temperature = 23.0,
            int TemperatureCount = 12345,
            double Resistance = 109.52,
            double PT100CoeffA = 0.003908299841,
            double PT100CoeffB = -0.000000577499974951934,
            double PT100CoeffC = 4.79382386195842E-21,
            double TempOffset = 0.0,
            double TempConversion = 364.4709167,
            string TemperatureUnit = "°C",
            bool TemperatureAlarm = false,
            char TemperatureTrend = '=',
            string CalculatedParameter = "n.c.",
            double CalculatedValue = 0.0,
            string CalculatedUnit = "°C",
            bool CalculatedAlarm = false,
            char CalculatedTrend = '=',
            string DeviceModel = "Fake",
            string FirmwareVersion = "V1.2",
            string SerialNumber = "000001",
            string DeviceName = "Fake Probe",
            string AlarmByte = "00",
            char DeviceType = 'F',
            string ProbeAddress = "00")

        {
            var fake = new RotProbe
            {
                ComPort = ComPort,
                ProbeType = ProbeType,
                HumidityCount = HumidityCount,
                HumdityRaw = HumdityRaw,
                HumidityUserCorrection = HumidityUserCorrection,
                HumidityTemperatureCorrection = HumidityTemperatureCorrection,
                HumidityDriftCorrection = HumidityDriftCorrection,
                HumidityUnit = HumidityUnit,
                HumidityAlarm = HumidityAlarm,
                HumidityTrend = HumidityTrend,
                Temperature = Temperature,
                TemperatureCount = TemperatureCount,
                Resistance = Resistance,
                PT100CoeffA = PT100CoeffA,
                PT100CoeffB = PT100CoeffB,
                PT100CoeffC = PT100CoeffC,
                TempOffset = TempOffset,
                TempConversion = TempConversion,
                TemperatureUnit = TemperatureUnit,
                TemperatureAlarm = TemperatureAlarm,
                TemperatureTrend = TemperatureTrend,
                CalculatedParameter = CalculatedParameter,
                CalculatedValue = CalculatedValue,
                CalculatedUnit = CalculatedUnit,
                CalculatedAlarm = CalculatedAlarm,
                CalculatedTrend = CalculatedTrend,
                DeviceModel = DeviceModel,
                FirmwareVersion = FirmwareVersion,
                SerialNumber = SerialNumber,
                DeviceName = DeviceName,
                AlarmByte = AlarmByte,
                DeviceType = DeviceType,
                ProbeAddress = ProbeAddress
            };

            // Mirror existing behavior: if probe reports °F but global setting is Celsius, mark CelsiusHelper and convert
            if (string.Equals(TemperatureUnit, "°F", StringComparison.Ordinal) && _globalTemperatureC)
            {
                fake.CelsiusHelper = true;
                fake.TemperatureUnit = "°C";
                fake.Temperature = Math.Round((fake.Temperature - 32) * 5.0 / 9.0, 2);
            }

            lock (probesLock)
            {
                if (ConnectedProbes == null)
                    ConnectedProbes = new List<RotProbe>();

                // If a fake probe with the same serial number exists, update its values in-place so any UI references stay valid.
                if (!string.IsNullOrWhiteSpace(SerialNumber))
                {
                    var existing = ConnectedProbes.FirstOrDefault(p =>
                        string.Equals(p?.SerialNumber, SerialNumber, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.ProbeType = fake.ProbeType;
                        existing.ProbeAddress = fake.ProbeAddress;
                        existing.DeviceType = fake.DeviceType;
                        existing.Humidity = fake.Humidity;
                        existing.Temperature = fake.Temperature;
                        existing.TemperatureUnit = fake.TemperatureUnit;
                        existing.CalculatedParameter = fake.CalculatedParameter;
                        existing.CalculatedValue = fake.CalculatedValue;
                        existing.DeviceModel = fake.DeviceModel;
                        existing.DeviceName = fake.DeviceName;
                        existing.CelsiusHelper = fake.CelsiusHelper;
                        // keep ComPort and SerialNumber unchanged (should already match)
                        try { Debug.WriteLine("AddFakeProbe: updated existing fake probe"); } catch { }
                        return;
                    }
                }

                // No existing fake with that serial -> add new fake probe.
                ConnectedProbes.Add(fake);
            }

            try { Debug.WriteLine("AddFakeProbe: fake probe added"); } catch { }
        }

        /// <summary>
        /// Remove any fake probes previously added with AddFakeProbe.
        /// </summary>
        public static void RemoveFakeProbes()
        {
            lock (probesLock)
            {
                if (ConnectedProbes == null || ConnectedProbes.Count == 0)
                    return;

                ConnectedProbes.RemoveAll(p => string.Equals(p?.ComPort, FakeProbeComPort, StringComparison.OrdinalIgnoreCase));
            }
            try { Debug.WriteLine("RemoveFakeProbes: fake probe(s) removed"); } catch { }
        }

        /// <summary>
        /// Update all fake probes previously added with AddFakeProbe.
        /// This updates fields in-place so any UI references to the RotProbe instances will reflect the changes.
        /// Only non-null parameters are applied (for nullable types).
        /// </summary>
        public static void UpdateAllFakeProbes(
            double? humidity = null,
            double? temperature = null,
            string temperatureUnit = null,
            string probeType = null,
            string probeAddress = null,
            string calculatedParameter = null,
            double? calculatedValue = null,
            string deviceModel = null,
            string deviceName = null)
        {
            lock (probesLock)
            {
                if (ConnectedProbes == null || ConnectedProbes.Count == 0)
                    return;

                foreach (var p in ConnectedProbes.Where(p => string.Equals(p?.ComPort, FakeProbeComPort, StringComparison.OrdinalIgnoreCase)))
                {
                    if (p == null)
                        continue;

                    if (!string.IsNullOrEmpty(probeType))
                        p.ProbeType = probeType;
                    if (!string.IsNullOrEmpty(probeAddress))
                        p.ProbeAddress = probeAddress;
                    if (!string.IsNullOrEmpty(deviceModel))
                        p.DeviceModel = deviceModel;
                    if (!string.IsNullOrEmpty(deviceName))
                        p.DeviceName = deviceName;
                    if (humidity.HasValue)
                        p.Humidity = humidity.Value;
                    if (calculatedParameter != null)
                        p.CalculatedParameter = calculatedParameter;
                    if (calculatedValue.HasValue)
                        p.CalculatedValue = calculatedValue.Value;

                    if (temperature.HasValue)
                        p.Temperature = temperature.Value;

                    if (!string.IsNullOrEmpty(temperatureUnit))
                    {
                        // If unit is F but global is C, set CelsiusHelper and convert to °C to match existing UpdateData behavior
                        if (string.Equals(temperatureUnit, "°F", StringComparison.Ordinal) && _globalTemperatureC)
                        {
                            p.CelsiusHelper = true;
                            p.TemperatureUnit = "°C";
                            // Only convert if we have a numeric temperature value
                            if (!double.IsNaN(p.Temperature))
                                p.Temperature = Math.Round((p.Temperature - 32) * 5.0 / 9.0, 2);
                        }
                        else
                        {
                            p.CelsiusHelper = false;
                            p.TemperatureUnit = temperatureUnit;
                        }
                    }
                }
            }
            try { Debug.WriteLine("UpdateAllFakeProbes: updated fake probes"); } catch { }
        }
    }
}
