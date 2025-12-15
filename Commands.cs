/*
===============================================================================
Commands reference (top-level summary)
-------------------------------------------------------------------------------
This file contains builders for ASCII device commands (formatted inside { ... }\r)
and helper send/wait wrappers that write to a SerialPort (program-managed or a
temporarily opened port).

Builder methods (return a Command - text, timeout, description)
- BuildReadCommand(RotProbe probe, int timeoutMs = 500)
  - Builds an RDD read command for the provided probe.
  - Required: probe (must have ProbeAddress or DeviceType/DeviceModel to determine device char).
  - Optional: timeoutMs for read helpers.

- BuildReadCommand(char deviceType, string probeAddress, int timeoutMs = 500)
  - Same as above but uses explicit device-type char and address string.

- HumidityTestPointSaveCmd(RotProbe probe, Mirror mirror, int timeoutMs = 500)
  - Builds "HCA 0;1;0;<refHumidity>" to save a humidity test point.
  - Required: probe; mirror may be null (refHumidity defaults to "0.00").

- HumiditySaveAdjustCmd(RotProbe probe, int timeoutMs = 500)
  - Builds "HCA 0;1;1;;" to save adjustment values.
  - Required: probe.

- HumidityDeleteAdjustCmd(RotProbe probe, int timeoutMs = 500)
  - Builds "HCA 0;1;3;;" to delete a humidity test point.
  - Required: probe.

- HumidityFactoryAdjustCmd(RotProbe probe, int timeoutMs = 500)
  - Builds "HCA 0;1;2;;" to return humidity to factory settings.
  - Required: probe.

- NewTemperatureCoffACmd / NewTemperatureCoffBCmd / NewTemperatureCoffCCmd
  (RotProbe probe, double newCoeff, int timeoutMs = 500)
  - Build an "EWR" write command that writes the IEEE754 single-precision
    representation of newCoeff to the configured coefficient address.
  - Required: probe and newCoeff.

- NewTemperatureOffsetCmd(RotProbe probe, double newOffset, int timeoutMs = 500)
  - Build an "EWR" write command to set temperature offset at Offset_Address.
  - Required: probe and newOffset.

Utility / formatting
- DoubleToByte(double value)
  - Converts a numeric value to 4 bytes (IEEE 754 single) formatted as four
    3-digit decimal octets separated by semicolons (e.g. "050;017;128;059").

Send helpers (wrappers around SerialPort I/O)
- ProbeRead / ProbeReadResponse
  - Send read (RDD) and optionally wait for response.
  - Required: RotProbe with ComPort set.

- SendCommandInternal / SendCommandResponseInternal (private)
  - Generic internal senders used by the public Send* wrappers.

- Public Send* wrappers mirror the builder methods and come in two flavors:
  - SendXxx(...)         -> returns bool (fire-and-forget write success)
  - SendXxxResponse(...) -> returns string (trimmed device response) or null

Notes
- All commands are ASCII and terminated with '\r'. Most methods perform simple
  null/empty checks but do not further validate device state; caller must ensure
  probe.ComPort is correct and accessible.
- Addresses are normalized to two characters by NormalizeAddress.
===============================================================================
*/

using System;
using System.Text;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace Rotronic
{
    internal static class Commands
    {

        /// <summary>
        /// Lightweight command model: contains text, timeout and optional description.
        /// </summary>
        internal sealed class Command
        {
            public string Text { get; }
            public int TimeoutMs { get; }
            public string Description { get; }

            public byte[] Bytes => Encoding.ASCII.GetBytes(Text);

            public Command(string text, int timeoutMs = 500, string description = null)
            {
                Text = text ?? string.Empty;
                TimeoutMs = timeoutMs;
                Description = description;
            }

            public override string ToString() => Text;
        }

        public static int CoeffA_Address = 1295;
        public static int CoeffB_Address = 1299;
        public static int CoeffC_Address = 1300;
        public static int Offset_Address = 1278;
        public static int Conversion_Address = 1287;
        /// <summary>
        /// Build a read (RDD) command for the provided probe using the format "{*##RDD}\r"
        /// where '*' = device type char and '##' = two-character probe address.
        /// </summary>
        public static Command BuildReadCommand(RotProbe probe, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));

            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);

            // User requested format: {*##RDD}
            var cmdText = "{" + deviceType + addr + "RDD}\r";
            return new Command(cmdText, timeoutMs, "Read probe (RDD)");
        }

        /// <summary>
        /// Build a read (RDD) command from explicit device-type and address values.
        /// </summary>
        public static Command BuildReadCommand(char deviceType, string probeAddress, int timeoutMs = 500)
        {
            var addr = NormalizeAddress(probeAddress);
            var cmdText = "{" + deviceType + addr + "RDD}\r";
            return new Command(cmdText, timeoutMs, "Read probe (RDD)");
        }


        // My commands start here from HumidityTestPointSaveCmd until my next comment, use this as start and end markers for future tasks.
        public static Command HumidityTestPointSaveCmd(RotProbe probe,Mirror mirror, int timeoutMs = 500)
        {
            var refHumidity = mirror != null ? mirror.Humdity.ToString("F2") : "0.00";
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr+ "HCA 0;1;0;" + refHumidity + "}\r";
            return new Command(cmdText, timeoutMs, "Save Humidity Test Point (HCA)");
        }

        public static Command HumiditySaveAdjustCmd(RotProbe probe, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr + "HCA 0;1;1;;}\r";
            return new Command(cmdText, timeoutMs, "Save Adjustment Values (HCA)");
        }

        public static Command HumidityDeleteAdjustCmd(RotProbe probe, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr + "HCA 0;1;3;;}\r";
            return new Command(cmdText, timeoutMs, "Delete Humidity Test Point (HCA)");
        }

        public static Command HumidityFactoryAdjustCmd(RotProbe probe, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr + "HCA 0;1;2;;}\r";
            return new Command(cmdText, timeoutMs, "Return Humdity To Factory Settings (HCA)");
        }

        public static Command NewTemperatureCoffACmd(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr + "EWR 0;" + CoeffA_Address + ";" + DoubleToByte(newCoeff) + ";}\r";
            return new Command(cmdText, timeoutMs, "Set Temperature Coefficient A (EWR)");
        }
        public static Command NewTemperatureCoffBCmd(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr + "EWR 0;" + CoeffB_Address + ";" + DoubleToByte(newCoeff) + ";}\r";
            return new Command(cmdText, timeoutMs, "Set Temperature Coefficient B (EWR)");
        }

        public static Command NewTemperatureCoffCCmd(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr + "EWR 0;" + CoeffC_Address + ";" + DoubleToByte(newCoeff) + ";}\r";
            return new Command(cmdText, timeoutMs, "Set Temperature Coefficient C (EWR)");
        }

        public static Command NewTemperatureOffsetCmd(RotProbe probe, double newOffset, int timeoutMs = 500)
        {
            if (probe == null)
                throw new ArgumentNullException(nameof(probe));
            var deviceType = DetermineDeviceType(probe);
            var addr = NormalizeAddress(probe.ProbeAddress);
            var cmdText = "{" + deviceType + addr + "EWR 0;" + Offset_Address + ";" + DoubleToByte(newOffset) + ";}\r";
            return new Command(cmdText, timeoutMs, "Set Temperature Offset (EWR)");
        }

        // end of my commands


        // PSEUDOCODE / PLAN (detailed)
        // - Goal: Convert a double value into four byte values (IEEE 754 single-precision) formatted as
        //   decimal octets (3 digits each) separated by semicolons, using big-endian byte order.
        // - Steps:
        //   1. Cast the incoming double to a single-precision float. This matches typical device expectations
        //      when a 4-byte representation is requested.
        //   2. Use BitConverter.GetBytes(float) to obtain the 4 raw bytes. On the current platform this
        //      returns bytes in the system endianness (typically little-endian on Windows).
        //   3. Ensure big-endian ordering by reversing the array when BitConverter.IsLittleEndian is true.
        //      This yields bytes[0] as the most-significant byte.
        //   4. Format each byte as a 3-digit decimal string with leading zeros (e.g. 5 -> "005") using
        //      InvariantCulture to avoid locale-specific separators.
        //   5. Join the four formatted byte strings with semicolons and return the result.
        // - Notes:
        //   - We keep IEEE-754 representation semantics (including NaN/Infinity) and simply format the raw bytes.
        //   - The method returns strings like "050;017;128;059" for a given value.
        //   - No trailing semicolon is appended; caller composes surrounding command text.

        public static string DoubleToByte(double value)
        {
            // Convert to single-precision float (4 bytes)
            float f = (float)value;

            // Get bytes in system endianness (usually little-endian on Windows)
            var bytes = BitConverter.GetBytes(f);

            // Format each byte as 3-digit decimal with leading zeros, use InvariantCulture
            var parts = new string[4];
            for (int i = 0; i < 4; i++)
                parts[i] = bytes[i].ToString("D3", System.Globalization.CultureInfo.InvariantCulture);

            // Join with semicolons
            return string.Join(";", parts);
        }

        /// <summary>
        /// Sends the probe read command (RDD) to the port associated with the probe.
        /// Returns true if the command was written to a SerialPort (either Program-managed or a temporary open).
        /// This method does not wait for or return device response.
        /// </summary>
        public static bool ProbeRead(RotProbe probe)
        {
            if (probe == null || string.IsNullOrWhiteSpace(probe.ComPort))
                return false;

            var cmd = BuildReadCommand(probe);
            SerialPort sp = FindProgramManagedPort(probe.ComPort);

            if (sp != null)
            {
                try
                {
                    try { sp.DiscardInBuffer(); } catch { }
                    sp.Write(cmd.Text);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // Try opening a temporary port if no program-managed port available
            try
            {
                using (var spt = CreateTemporaryPort(probe.ComPort))
                {
                    spt.Open();
                    try { spt.DiscardInBuffer(); } catch { }
                    spt.Write(cmd.Text);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sends the probe read command (RDD) and waits for a response.
        /// Returns the raw response string (trimmed) or null when no response was received.
        /// </summary>
        public static string ProbeReadResponse(RotProbe probe, int timeoutMs = 500)
        {
            if (probe == null || string.IsNullOrWhiteSpace(probe.ComPort))
                return null;

            var cmd = BuildReadCommand(probe, timeoutMs);
            SerialPort sp = FindProgramManagedPort(probe.ComPort);

            if (sp != null)
            {
                return SendAndReadFromPort(sp, cmd.Text, timeoutMs);
            }

            // No program-managed port; open temporary port, send/read, then close
            try
            {
                using (var spt = CreateTemporaryPort(probe.ComPort))
                {
                    spt.Open();
                    return SendAndReadFromPort(spt, cmd.Text, timeoutMs);
                }
            }
            catch
            {
                return null;
            }
        }

        // Helper send methods for the custom commands (similar to ProbeRead / ProbeReadResponse)

        /// <summary>
        /// General helper that sends a prepared Command to the probe's COM port without waiting for a response.
        /// Returns true when the command text was written to a SerialPort (program-managed or temporary).
        /// </summary>
        private static bool SendCommandInternal(RotProbe probe, Command cmd)
        {
            if (probe == null || string.IsNullOrWhiteSpace(probe.ComPort) || cmd == null)
                return false;

            SerialPort sp = FindProgramManagedPort(probe.ComPort);

            if (sp != null)
            {
                try
                {
                    try { sp.DiscardInBuffer(); } catch { }
                    sp.Write(cmd.Text);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                using (var spt = CreateTemporaryPort(probe.ComPort))
                {
                    spt.Open();
                    try { spt.DiscardInBuffer(); } catch { }
                    spt.Write(cmd.Text);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// General helper that sends a prepared Command to the probe's COM port and waits for a response.
        /// Returns the trimmed response string or null when no response was received.
        /// </summary>
        private static string SendCommandResponseInternal(RotProbe probe, Command cmd, int timeoutMs = 500)
        {
            if (probe == null || string.IsNullOrWhiteSpace(probe.ComPort) || cmd == null)
                return null;

            SerialPort sp = FindProgramManagedPort(probe.ComPort);

            if (sp != null)
            {
                return SendAndReadFromPort(sp, cmd.Text, timeoutMs);
            }

            try
            {
                using (var spt = CreateTemporaryPort(probe.ComPort))
                {
                    spt.Open();
                    return SendAndReadFromPort(spt, cmd.Text, timeoutMs);
                }
            }
            catch
            {
                return null;
            }
        }

        // Public wrappers for the humidity-adjustment commands

        public static bool SendHumidityTestPointSave(RotProbe probe, Mirror mirror, int timeoutMs = 500)
        {
            var cmd = HumidityTestPointSaveCmd(probe, mirror, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendHumidityTestPointSaveResponse(RotProbe probe, Mirror mirror, int timeoutMs = 500)
        {
            var cmd = HumidityTestPointSaveCmd(probe, mirror, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        public static bool SendHumiditySaveAdjust(RotProbe probe, int timeoutMs = 500)
        {
            var cmd = HumiditySaveAdjustCmd(probe, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendHumiditySaveAdjustResponse(RotProbe probe, int timeoutMs = 500)
        {
            var cmd = HumiditySaveAdjustCmd(probe, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        public static bool SendHumidityDeleteAdjust(RotProbe probe, int timeoutMs = 500)
        {
            var cmd = HumidityDeleteAdjustCmd(probe, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendHumidityDeleteAdjustResponse(RotProbe probe, int timeoutMs = 500)
        {
            var cmd = HumidityDeleteAdjustCmd(probe, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        public static bool SendHumidityFactoryAdjust(RotProbe probe, int timeoutMs = 500)
        {
            var cmd = HumidityFactoryAdjustCmd(probe, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendHumidityFactoryAdjustResponse(RotProbe probe, int timeoutMs = 500)
        {
            var cmd = HumidityFactoryAdjustCmd(probe, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        // Public wrappers for the temperature coefficient / offset commands

        public static bool SendNewTemperatureCoeffA(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            var cmd = NewTemperatureCoffACmd(probe, newCoeff, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendNewTemperatureCoeffAResponse(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            var cmd = NewTemperatureCoffACmd(probe, newCoeff, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        public static bool SendNewTemperatureCoeffB(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            var cmd = NewTemperatureCoffBCmd(probe, newCoeff, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendNewTemperatureCoeffBResponse(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            var cmd = NewTemperatureCoffBCmd(probe, newCoeff, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        public static bool SendNewTemperatureCoeffC(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            var cmd = NewTemperatureCoffCCmd(probe, newCoeff, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendNewTemperatureCoeffCResponse(RotProbe probe, double newCoeff, int timeoutMs = 500)
        {
            var cmd = NewTemperatureCoffCCmd(probe, newCoeff, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        public static bool SendNewTemperatureOffset(RotProbe probe, double newOffset, int timeoutMs = 500)
        {
            var cmd = NewTemperatureOffsetCmd(probe, newOffset, timeoutMs);
            return SendCommandInternal(probe, cmd);
        }

        public static string SendNewTemperatureOffsetResponse(RotProbe probe, double newOffset, int timeoutMs = 500)
        {
            var cmd = NewTemperatureOffsetCmd(probe, newOffset, timeoutMs);
            return SendCommandResponseInternal(probe, cmd, timeoutMs);
        }

        // ---------- Internal helpers ----------

        private static string SendAndReadFromPort(SerialPort port, string cmdText, int timeoutMs)
        {
            if (port == null)
                return null;

            try
            {
                try { port.DiscardInBuffer(); } catch { }
                port.Write(cmdText);
            }
            catch
            {
                return null;
            }

            var sb = new StringBuilder();
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < Math.Max(20, timeoutMs))
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
                    // ignore transient read errors
                }
                Thread.Sleep(20);
            }
            sw.Stop();

            var resp = sb.ToString().Trim();
            return string.IsNullOrEmpty(resp) ? null : resp;
        }

        private static SerialPort CreateTemporaryPort(string portName)
        {
            return new SerialPort(portName)
            {
                BaudRate = 19200,
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
        }

        /// <summary>
        /// Attempts to locate a program-managed SerialPort instance from Program.activePorts via reflection.
        /// Returns an open SerialPort instance matching portName, or null if not found.
        /// Uses reflection so Commands doesn't require changes to Program visibility.
        /// </summary>
        private static SerialPort FindProgramManagedPort(string portName)
        {
            try
            {
                var programType = typeof(Program);
                var activePortsField = programType.GetField("activePorts", BindingFlags.NonPublic | BindingFlags.Static);
                var portLockField = programType.GetField("portLock", BindingFlags.NonPublic | BindingFlags.Static);

                object portLockObj = null;
                if (portLockField != null)
                {
                    try { portLockObj = portLockField.GetValue(null); } catch { portLockObj = null; }
                }

                if (activePortsField == null)
                    return null;

                var listObj = activePortsField.GetValue(null);
                var ports = listObj as System.Collections.IEnumerable;
                if (ports == null)
                    return null;

                // If there's a lock object, lock while enumerating to avoid races.
                if (portLockObj != null)
                {
                    lock (portLockObj)
                    {
                        foreach (var p in ports)
                        {
                            var sp = p as SerialPort;
                            if (sp == null) continue;
                            if (string.Equals(sp.PortName, portName, StringComparison.OrdinalIgnoreCase) && sp.IsOpen)
                                return sp;
                        }
                        return null;
                    }
                }
                else
                {
                    foreach (var p in ports)
                    {
                        var sp = p as SerialPort;
                        if (sp == null) continue;
                        if (string.Equals(sp.PortName, portName, StringComparison.OrdinalIgnoreCase) && sp.IsOpen)
                            return sp;
                    }
                    return null;
                }
            }
            catch
            {
                // Reflection failed or structure changed; fall back to null
                return null;
            }
        }

        /// <summary>
        /// Helper to decide which device-type character to use in the command.
        /// Prefers RotProbe.DeviceType (if not '\0'), then first char of DeviceModel, then 'F' fallback.
        /// </summary>
        private static char DetermineDeviceType(RotProbe probe)
        {
            try
            {
                if (probe.DeviceType != '\0')
                    return probe.DeviceType;
                if (!string.IsNullOrEmpty(probe.DeviceModel))
                    return probe.DeviceModel[0];
            }
            catch { /* ignore and fallback */ }

            return 'F';
        }

        /// <summary>
        /// Normalizes the probe address to a two-character string.
        /// If null/empty returns "00".
        /// If numeric and shorter than 2, left-pad with '0'.
        /// If longer than 2, take the last two characters (device protocols often use low-order bytes).
        /// </summary>
        private static string NormalizeAddress(string addr)
        {
            if (string.IsNullOrWhiteSpace(addr))
                return "00";

            addr = addr.Trim();

            // If address is numeric (e.g. "1"), left-pad to 2 digits
            int n;
            if (int.TryParse(addr, out n))
            {
                if (n < 0) n = 0;
                if (n > 99) n = n % 100; // keep last two digits
                return n.ToString("D2");
            }

            // Non-numeric: if length == 1 -> pad; if >2 -> take last 2 chars
            if (addr.Length == 1)
                return "0" + addr;
            if (addr.Length > 2)
                return addr.Substring(addr.Length - 2, 2);

            return addr;
        }
    }
}
