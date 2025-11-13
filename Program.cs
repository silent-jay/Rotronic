using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

namespace Rotronic
{
    internal static class Program
    {
        private static readonly List<SerialPort> activePorts = new List<SerialPort>();
        private static readonly object portLock = new object();

        // New: list of connected probes and synchronization for that list
        private static readonly object probesLock = new object();
        public static List<RotProbe> ConnectedProbes { get; private set; } = new List<RotProbe>();

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

            // Ensure we will close the ports when the app exits
            Application.ApplicationExit += (s, e) =>
            {
                StopProbeMonitoring();
                CloseActivePorts();
            };
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                StopProbeMonitoring();
                CloseActivePorts();
            };

            // Start discovery + periodic updates
            StartProbeMonitoring(15000); // 15 seconds

            // Create the form instance (calls InitializeComponent)
            var mainForm = new Main();

            Application.Run(mainForm);
        }

        // Start periodic monitoring (threadpool timer)
        public static void StartProbeMonitoring(int intervalMs = 15000)
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

        // Timer callback: discover on first run (ComTest), otherwise update existing
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
                // If no probes discovered yet, run ComTest to populate list and active ports
                lock (probesLock)
                {
                    if (ConnectedProbes == null || ConnectedProbes.Count == 0)
                    {
                        try
                        {
                            var discovered = ComTest() ?? new List<RotProbe>();
                            ConnectedProbes = discovered;
                            try { Debug.WriteLine($"MonitorCallback: discovered {discovered.Count} probes"); } catch { }
                            return;
                        }
                        catch (Exception ex)
                        {
                            try { Debug.WriteLine($"MonitorCallback: ComTest failed: {ex}"); } catch { }
                            return;
                        }
                    }
                }

                // Otherwise, update data for existing probes
                List<RotProbe> snapshot;
                lock (probesLock)
                {
                    // Work on a shallow copy to avoid holding lock during IO
                    snapshot = ConnectedProbes.ToList();
                }

                List<RotProbe> updated;
                try
                {
                    updated = UpdateData(snapshot) ?? new List<RotProbe>();
                }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"MonitorCallback: UpdateData threw: {ex}"); } catch { }
                    return;
                }

                if (updated.Count == 0)
                    return;

                // Merge updated probes into the main ConnectedProbes list
                lock (probesLock)
                {
                    foreach (var up in updated)
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
            catch (Exception exOuter)
            {
                try { Debug.WriteLine($"MonitorCallback: unexpected error: {exOuter}"); } catch { }
            }
            finally
            {
                // Re-enable the periodic timer (use same interval you started with)
                try
                {
                    probeTimer?.Change(15000, 15000); // adjust interval if you changed it elsewhere
                }
                catch { }
            }
        }

        private static RotProbe CalibrationCoefficients(RotProbe probe)
        {
            /*
            Pseudocode / Plan (detailed):
            - Validate probe and locate its open SerialPort in activePorts.
            - Define SendRead(port, cmd, timeoutMs):
              - Discard input buffer, write command, then poll ReadExisting until timeout collecting chunks.
            - Define ParseFourByteFloatFromResponse(response):
              - If empty -> NaN.
              - Extract payload after '{', trim trailing braces/CR/LF.
              - Attempt to find four consecutive numeric byte tokens.
                - Split payload on ';' using StringSplitOptions.None to preserve empty positions.
                - For each candidate window of 4 segments:
                  - From each segment, extract the first numeric substring (1-3 digits) if present.
                  - Try parse each numeric substring to int and ensure 0..255.
                  - If all 4 succeed, use those values as bytes (device sends big-endian).
                - If no such 4-segment window found, fallback to scanning the entire payload
                  for numeric tokens (Regex.Matches) and use the first 4 matches.
              - Convert the 4 bytes (big-endian) to a Single, handling machine endianness, return as double or NaN on error.
            - For each address in addressMap:
              - Build ERD command, call SendRead, parse float, assign to probe fields.
            - Return probe.
            */
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
            // Pseudocode / plan (detailed):
            // 1. Enumerate serial ports and return an empty list if none found.
            // 2. For each port:
            //    a. Skip if already open and tracked in activePorts.
            //    b. Create and open a SerialPort with desired settings.
            //    c. Clear input buffer and send discovery command "{ 99RDD}\r".
            //    d. Read response using a short polling loop collecting chunks.
            //    e. If a response was received:
            //         i. Trim the response and parse it to a RotProbe via ParseResponseToRotProbe.
            //        ii. If parsing succeeded, add the RotProbe to results.
            //       iii. Keep the underlying SerialPort open by adding it to activePorts (avoid duplicates).
            //        iv. If parsing failed, close and dispose the SerialPort instance.
            //    f. If no response received, close and dispose the SerialPort instance.
            //    g. Catch and log non-fatal exceptions and continue to the next port.
            // 3. AFTER scanning all ports, iterate the discovered RotProbe list and call CalibrationCoefficients
            //    for each probe to fetch/assign calibration values. Do this sequentially with try/catch
            //    since CalibrationCoefficients uses shared serial ports and is IO-bound; assigning the
            //    returned value back into the list if a new object is returned.
            // 4. Return the (possibly updated) results list.
            //
            // Rationale: calling CalibrationCoefficients after discovery ensures ports remain open (if kept)
            // and that parsing logic is separated from calibration reads. Sequential calls are safer here
            // because CalibrationCoefficients expects the corresponding SerialPort to be present in activePorts.

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
            // Use index-based loop so we can replace the item if CalibrationCoefficients returns a new object.
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
            // parts[2] = HumdityUnit
            if (parts.Length > 2)
            {
                rp.HumdityUnit = parts[2];
            }
            // parts[3] = HumidityAlarm (string "000" expected)
            if (parts.Length > 3)
            {
                rp.HumidityAlarm = !string.Equals(parts[3], "000", StringComparison.OrdinalIgnoreCase);
            }
            // parts[4] = HumdityTrend
            if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4]))
            {
                rp.HumdityTrend = parts[4][0];
            }

            // parts[5] = Temperature
            if (parts.Length > 5)
            {
                rp.Temperature = parseDoubleOrNaN(parts[5]);
            }
            // parts[6] = TemperatureUnit
            if (parts.Length > 6)
            {
                rp.TemperatureUnit = parts[6];
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
        //       2: HumdityFactoryCorrection (double)
        //       3: HumdityUserCorrection (double)
        //       4: HumdityTemperatureCorrection (double)
        //       5: HumdityDriftCorrection (double)
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
                        probe.HumdityFactoryCorrection = parseDoubleOrNaN(parts[2]);
                    if (parts.Length > 3)
                        probe.HumdityUserCorrection = parseDoubleOrNaN(parts[3]);
                    if (parts.Length > 4)
                        probe.HumdityTemperatureCorrection = parseDoubleOrNaN(parts[4]);
                    if (parts.Length > 5)
                        probe.HumdityDriftCorrection = parseDoubleOrNaN(parts[5]);
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
    }
}
