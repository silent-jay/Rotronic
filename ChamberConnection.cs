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
                    return ReadLineNoThrow();
                }
                catch
                {
                    CloseNoThrow();
                    return null;
                }
            }
        }

        private string ReadLineNoThrow()
        {
            // TCP is a stream: responses may arrive in pieces. We read until CRLF (or LF),
            // using the stream's ReadTimeout to bound how long we wait.
            var bytes = new List<byte>(128);
            bool gotAny = false;

            while (true)
            {
                int b;
                try
                {
                    b = stream.ReadByte();
                }
                catch (IOException)
                {
                    // timeout / socket error
                    return gotAny ? Encoding.ASCII.GetString(bytes.ToArray()).Trim('\r', '\n', '\0', ' ') : null;
                }

                if (b < 0)
                    return gotAny ? Encoding.ASCII.GetString(bytes.ToArray()).Trim('\r', '\n', '\0', ' ') : null;

                gotAny = true;
                bytes.Add((byte)b);

                int count = bytes.Count;
                if (count >= 2 && bytes[count - 2] == (byte)'\r' && bytes[count - 1] == (byte)'\n')
                    break;
                if (bytes[count - 1] == (byte)'\n')
                    break;

                // safety cap
                if (bytes.Count >= 8192)
                    break;
            }

            return Encoding.ASCII.GetString(bytes.ToArray()).Trim('\r', '\n', '\0', ' ');
        }

        public void CloseNoThrow()
        {
            lock (sync)
            {
                try { stream?.Close(); } catch { }
                try { client?.Close(); } catch { }
                stream = null;
                client = null;
            }
        }

        public void Dispose()
        {
            CloseNoThrow();
        }
    }
}
