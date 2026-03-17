using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace Rotronic
{
    internal static class ChamberCommands
    {
        private const int DefaultPort = 6341;
        private const int DefaultConnectTimeoutMs = 1500;
        private const int DefaultReadTimeoutMs = 2000;

        private static readonly object ConnectionLock = new object();
        private static readonly Dictionary<string, ChamberConnection> Connections = new Dictionary<string, ChamberConnection>(StringComparer.OrdinalIgnoreCase);

        private const string FakeIp = "fake";
        private static readonly object FakeLock = new object();
        private static FakeChamberState FakeState = new FakeChamberState();

        private sealed class FakeChamberState
        {
            public double Temp = 23.0;
            public double Rh = 35.0;
            public bool TempControl;
            public bool RhControl;
            public double TempSp = 23.0;
            public double RhSp = 35.0;
            public bool TempStable = true;
            public bool RhStable = true;
            public string Name = "Fake Chamber";
            public string Version = "FAKE";
            public string ControllerSerial = "FAKE";
        }

        private static bool IsFakeChamber(Chamber chamber)
        {
            var ip = (chamber?.IPAddress ?? string.Empty).Trim();

            return string.Equals(ip, "fake", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ip, "__FAKE_CHAMBER__", StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureCrlf(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null/empty.", nameof(command));

            command = command.Trim();
            if (command.EndsWith("\r\n", StringComparison.Ordinal))
                return command;
            if (command.EndsWith("\n", StringComparison.Ordinal))
                return command.TrimEnd('\n') + "\r\n";
            if (command.EndsWith("\r", StringComparison.Ordinal))
                return command + "\n";
            return command + "\r\n";
        }

        private static IPAddress GetAddress(Chamber chamber)
        {
            if (chamber == null)
                throw new ArgumentNullException(nameof(chamber));
            if (string.IsNullOrWhiteSpace(chamber.IPAddress))
                throw new ArgumentException("Chamber IPAddress is not set.", nameof(chamber));
            if (!IPAddress.TryParse(chamber.IPAddress.Trim(), out var address))
                throw new ArgumentException("Chamber IPAddress is not a valid IP.", nameof(chamber));
            return address;
        }

        private static string GetKey(IPAddress address, int port)
        {
            return address + ":" + port.ToString(CultureInfo.InvariantCulture);
        }

        private static ChamberConnection GetOrCreateConnection(IPAddress address, int port, int connectTimeoutMs, int readTimeoutMs)
        {
            var key = GetKey(address, port);
            lock (ConnectionLock)
            {
                if (Connections.TryGetValue(key, out var existing) && existing != null)
                    return existing;

                var created = new ChamberConnection(address, port, connectTimeoutMs, readTimeoutMs);
                Connections[key] = created;
                return created;
            }
        }

        public static void Close(Chamber chamber, int port = DefaultPort)
        {
            if (chamber == null)
                return;

            IPAddress address;
            try { address = GetAddress(chamber); }
            catch { return; }

            var key = GetKey(address, port);
            ChamberConnection conn = null;

            lock (ConnectionLock)
            {
                if (Connections.TryGetValue(key, out conn))
                    Connections.Remove(key);
            }

            try { conn?.Dispose(); } catch { }
        }

        public static void CloseAll()
        {
            List<ChamberConnection> toClose;
            lock (ConnectionLock)
            {
                toClose = new List<ChamberConnection>(Connections.Values);
                Connections.Clear();
            }

            foreach (var c in toClose)
            {
                try { c?.Dispose(); } catch { }
            }
        }

        public static bool Send(Chamber chamber, string command, int port = DefaultPort,
            int connectTimeoutMs = DefaultConnectTimeoutMs, int readTimeoutMs = DefaultReadTimeoutMs)
        {
            if (IsFakeChamber(chamber))
                return HandleFakeSet(chamber, command);
            var address = GetAddress(chamber);
            var conn = GetOrCreateConnection(address, port, connectTimeoutMs, readTimeoutMs);
            return conn.Send(EnsureCrlf(command));
        }

        public static string SendResponse(Chamber chamber, string command, int port = DefaultPort,
            int connectTimeoutMs = DefaultConnectTimeoutMs, int readTimeoutMs = DefaultReadTimeoutMs)
        {
            if (IsFakeChamber(chamber))
                return HandleFakeQuery(chamber, command);
            var address = GetAddress(chamber);
            var conn = GetOrCreateConnection(address, port, connectTimeoutMs, readTimeoutMs);
            return conn.Query(EnsureCrlf(command));
        }

        private static bool HandleFakeSet(Chamber chamber, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            var trimmed = command.Trim();
            if (trimmed.EndsWith("\r\n", StringComparison.Ordinal))
                trimmed = trimmed.Substring(0, trimmed.Length - 2);

            var eq = trimmed.IndexOf('=');
            if (eq <= 0)
                return false;

            var key = trimmed.Substring(0, eq).Trim();
            var val = trimmed.Substring(eq + 1).Trim();

            lock (FakeLock)
            {
                if (string.Equals(key, "TempControl", StringComparison.OrdinalIgnoreCase))
                {
                    FakeState.TempControl = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                    try { if (chamber != null) chamber.TempControl = FakeState.TempControl; } catch { }
                    return true;
                }
                if (string.Equals(key, "RHControl", StringComparison.OrdinalIgnoreCase))
                {
                    FakeState.RhControl = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                    try { if (chamber != null) chamber.HumControl = FakeState.RhControl; } catch { }
                    return true;
                }
                if (string.Equals(key, "TempSP", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var sp))
                    {
                        FakeState.TempSp = sp;
                        try { if (chamber != null) chamber.TemperatureSP = sp; } catch { }
                        return true;
                    }
                    return false;
                }
                if (string.Equals(key, "RHSP", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var sp))
                    {
                        FakeState.RhSp = sp;
                        try { if (chamber != null) chamber.HumiditySP = sp; } catch { }
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        private static string HandleFakeQuery(Chamber chamber, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return string.Empty;

            var trimmed = command.Trim();
            if (trimmed.EndsWith("\r\n", StringComparison.Ordinal))
                trimmed = trimmed.Substring(0, trimmed.Length - 2);

            if (trimmed.EndsWith("?", StringComparison.Ordinal))
                trimmed = trimmed.Substring(0, trimmed.Length - 1);

            lock (FakeLock)
            {
                switch (trimmed)
                {
                    case "Temp":
                        return FakeState.Temp.ToString(CultureInfo.InvariantCulture);
                    case "TempRef":
                        return FakeState.Temp.ToString(CultureInfo.InvariantCulture);
                    case "TempControl":
                        return FakeState.TempControl ? "1" : "0";
                    case "TempSP":
                        return FakeState.TempSp.ToString(CultureInfo.InvariantCulture);
                    case "TempStable":
                        return FakeState.TempStable ? "1" : "0";
                    case "RH":
                        return FakeState.Rh.ToString(CultureInfo.InvariantCulture);
                    case "RHRef":
                        return FakeState.Rh.ToString(CultureInfo.InvariantCulture);
                    case "RHControl":
                        return FakeState.RhControl ? "1" : "0";
                    case "RHSP":
                        return FakeState.RhSp.ToString(CultureInfo.InvariantCulture);
                    case "RHStable":
                        return FakeState.RhStable ? "1" : "0";
                    case "WaterLevel":
                        return "100";
                    case "Desiccant1DP":
                        return "-20";
                    case "HC2-SSerial":
                        return "FAKE";
                    case "DesiccantHC2-SSerial":
                        return "FAKE";
                    case "Version":
                        return FakeState.Version;
                    case "ControllerSerial":
                        return FakeState.ControllerSerial;
                    case "Name":
                        return FakeState.Name;
                    case "Reference":
                        return "0";
                    case "ExtRefSerial":
                        return "";
                    case "ExtRefTemp":
                    case "ExtRefDP":
                    case "ExtRefDPCorr":
                    case "ExtRefFP":
                    case "ExtRefRH":
                        return "0";
                    case "ExtRefControl":
                    case "ExtRefStable":
                        return "0";
                    case "ProgramRun":
                        return "0";
                    default:
                        return string.Empty;
                }
            }
        }

        // Query helpers (use GetXxx naming so callers don't have to use '?' in method names)
        public static string GetTemp(Chamber chamber) => SendResponse(chamber, "Temp?");
        public static string GetTempRef(Chamber chamber) => SendResponse(chamber, "TempRef?");
        public static string GetTempControl(Chamber chamber) => SendResponse(chamber, "TempControl?");
        public static string GetTempSP(Chamber chamber) => SendResponse(chamber, "TempSP?");
        public static string GetTempStable(Chamber chamber) => SendResponse(chamber, "TempStable?");

        public static string GetRH(Chamber chamber) => SendResponse(chamber, "RH?");
        public static string GetRHRef(Chamber chamber) => SendResponse(chamber, "RHRef?");
        public static string GetRHControl(Chamber chamber) => SendResponse(chamber, "RHControl?");
        public static string GetRHSP(Chamber chamber) => SendResponse(chamber, "RHSP?");
        public static string GetRHStable(Chamber chamber) => SendResponse(chamber, "RHStable?");

        public static string GetDesiccant1DP(Chamber chamber) => SendResponse(chamber, "Desiccant1DP?");
        public static string GetWaterLevel(Chamber chamber) => SendResponse(chamber, "WaterLevel?");
        public static string GetHC2SSerial(Chamber chamber) => SendResponse(chamber, "HC2-SSerial?");
        public static string GetDesiccantHC2SSerial(Chamber chamber) => SendResponse(chamber, "DesiccantHC2-SSerial?");
        public static string GetVersion(Chamber chamber) => SendResponse(chamber, "Version?");
        public static string GetControllerSerial(Chamber chamber) => SendResponse(chamber, "ControllerSerial?");
        public static string GetName(Chamber chamber) => SendResponse(chamber, "Name?");
        public static string GetReference(Chamber chamber) => SendResponse(chamber, "Reference?");

        public static string GetExtRefSerial(Chamber chamber) => SendResponse(chamber, "ExtRefSerial?");
        public static string GetExtRefTemp(Chamber chamber) => SendResponse(chamber, "ExtRefTemp?");
        public static string GetExtRefDP(Chamber chamber) => SendResponse(chamber, "ExtRefDP?");
        public static string GetExtRefDPCorr(Chamber chamber) => SendResponse(chamber, "ExtRefDPCorr?");
        public static string GetExtRefFP(Chamber chamber) => SendResponse(chamber, "ExtRefFP?");
        public static string GetExtRefRH(Chamber chamber) => SendResponse(chamber, "ExtRefRH?");
        public static string GetExtRefControl(Chamber chamber) => SendResponse(chamber, "ExtRefControl?");
        public static string GetExtRefStable(Chamber chamber) => SendResponse(chamber, "ExtRefStable?");

        public static string GetProgramRun(Chamber chamber) => SendResponse(chamber, "ProgramRun?");

        // Setters
        public static bool SetTempControl(Chamber chamber, bool enabled) => Send(chamber, "TempControl=" + (enabled ? "1" : "0"));
        public static bool SetRHControl(Chamber chamber, bool enabled) => Send(chamber, "RHControl=" + (enabled ? "1" : "0"));

        public static bool SetTempSP(Chamber chamber, double setPointC)
            => Send(chamber, "TempSP=" + setPointC.ToString(CultureInfo.InvariantCulture));

        public static bool SetRHSP(Chamber chamber, double setPointRh)
            => Send(chamber, "RHSP=" + setPointRh.ToString(CultureInfo.InvariantCulture));

        // Mirror passthrough when chamber mediates the mirror connection: E=<mirror command>
        public static string SendMirrorQuery(Chamber chamber, string mirrorCommand)
        {
            if (string.IsNullOrWhiteSpace(mirrorCommand))
                throw new ArgumentException("Mirror command cannot be null/empty.", nameof(mirrorCommand));
            return SendResponse(chamber, "E=" + mirrorCommand.Trim());
        }
    }
}
