using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rotronic
{
    internal sealed class ChamberConnection : IDisposable
    {
        private readonly IPAddress address;
        private readonly int port;
        private readonly int connectTimeoutMs;
        private readonly int readTimeoutMs;

        private readonly object sync = new object();
        private TcpClient client;
        private NetworkStream stream;

        // Buffer for TCP stream reassembly (TCP is not message-framed)
        private readonly byte[] readBuf = new byte[4096];
        private readonly StringBuilder recvBuffer = new StringBuilder(4096);

        public ChamberConnection(IPAddress address, int port, int connectTimeoutMs, int readTimeoutMs)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            this.address = address;
            this.port = port;
            this.connectTimeoutMs = connectTimeoutMs;
            this.readTimeoutMs = readTimeoutMs;
        }

        public bool IsConnected
        {
            get
            {
                lock (sync)
                {
                    return client != null && client.Connected && stream != null;
                }
            }
        }

        public bool EnsureConnected()
        {
            lock (sync)
            {
                if (client != null && client.Connected && stream != null)
                    return true;

                CloseNoThrow();

                var c = new TcpClient();
                try
                {
                    var connectTask = c.ConnectAsync(address, port);
                    bool connected = connectTask.Wait(connectTimeoutMs);
                    if (!connected || !c.Connected)
                    {
                        try { c.Close(); } catch { }
                        return false;
                    }

                    var s = c.GetStream();
                    s.ReadTimeout = readTimeoutMs;
                    s.WriteTimeout = readTimeoutMs;

                    client = c;
                    stream = s;

                    recvBuffer.Clear();
                    return true;
                }
                catch
                {
                    try { c.Close(); } catch { }
                    return false;
                }
            }
        }

        public bool Send(string command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            lock (sync)
            {
                if (!EnsureConnected())
                    return false;

                var payload = Encoding.ASCII.GetBytes(command);
                try
                {
                    stream.Write(payload, 0, payload.Length);
                    stream.Flush();
                    return true;
                }
                catch
                {
                    CloseNoThrow();
                    return false;
                }
            }
        }

        public string Query(string command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            lock (sync)
            {
                if (!EnsureConnected())
                    return null;

                // If the device sent unsolicited/stale data earlier, keep it from polluting the next response.
                // We only do a best-effort drain, bounded by the current ReadTimeout.
                DrainAvailableNoThrow();

                var payload = Encoding.ASCII.GetBytes(command);
                try
                {
                    stream.Write(payload, 0, payload.Length);
                    stream.Flush();
                }
                catch
                {
                    CloseNoThrow();
                    return null;
                }

                try
                {
                    return ReadResponseLineNoThrow();
                }
                catch
                {
                    CloseNoThrow();
                    return null;
                }
            }
        }

        private void DrainAvailableNoThrow()
        {
            try
            {
                if (stream == null)
                    return;

                // Read any immediately-available bytes (non-blocking via DataAvailable)
                while (stream.DataAvailable)
                {
                    int n = stream.Read(readBuf, 0, readBuf.Length);
                    if (n <= 0)
                        break;

                    recvBuffer.Append(Encoding.ASCII.GetString(readBuf, 0, n));

                    // If buffer grows too large, drop it entirely (stale noise)
                    if (recvBuffer.Length > 64 * 1024)
                    {
                        recvBuffer.Clear();
                        break;
                    }
                }

                // Also drop any complete lines already sitting in the buffer.
                // Those are responses to earlier requests we don't care about anymore.
                while (TryPopLineFromBuffer(out _)) { }
            }
            catch
            {
                // ignore drain failures
            }
        }

        private string ReadResponseLineNoThrow()
        {
            // First, if a full line is already buffered, return it.
            if (TryPopLineFromBuffer(out var line))
                return line;

            while (true)
            {
                int n;
                try
                {
                    n = stream.Read(readBuf, 0, readBuf.Length);
                }
                catch (IOException)
                {
                    // timeout / socket error
                    return TryPopLineFromBuffer(out line) ? line : null;
                }

                if (n <= 0)
                    return TryPopLineFromBuffer(out line) ? line : null;

                recvBuffer.Append(Encoding.ASCII.GetString(readBuf, 0, n));

                if (TryPopLineFromBuffer(out line))
                    return line;

                // safety cap: avoid unbounded growth if terminator never arrives
                if (recvBuffer.Length >= 64 * 1024)
                {
                    var s = recvBuffer.ToString().Trim('\r', '\n', '\0', ' ');
                    recvBuffer.Clear();
                    return s.Length == 0 ? null : s;
                }
            }
        }

        private bool TryPopLineFromBuffer(out string line)
        {
            line = null;
            if (recvBuffer.Length == 0)
                return false;

            var s = recvBuffer.ToString();

            int lf = s.IndexOf('\n');
            if (lf < 0)
                return false;

            int lineEnd = lf;
            // Support CRLF: strip preceding CR
            int len = lineEnd;
            if (len > 0 && s[len - 1] == '\r')
                len--;

            line = s.Substring(0, len).Trim('\r', '\n', '\0', ' ');

            // Remove consumed data (through LF)
            recvBuffer.Clear();
            if (lf + 1 < s.Length)
                recvBuffer.Append(s.Substring(lf + 1));

            return true;
        }

        public void CloseNoThrow()
        {
            lock (sync)
            {
                try { stream?.Close(); } catch { }
                try { client?.Close(); } catch { }
                stream = null;
                client = null;
                try { recvBuffer.Clear(); } catch { }
            }
        }

        public void Dispose()
        {
            CloseNoThrow();
        }
    }
}
