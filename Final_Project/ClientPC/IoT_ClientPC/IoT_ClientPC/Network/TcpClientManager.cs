using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace IoT_ClientPC.Network
{
    public class TcpClientManager
    {
        private TcpClient? client;
        private StreamReader? reader;
        private StreamWriter? writer;

        public bool IsConnected => client != null && client.Connected;

        public async Task ConnectAsync(string ip, int port)
        {
            client = new TcpClient();
            await client.ConnectAsync(ip, port);

            NetworkStream stream = client.GetStream();

            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public async Task SendAsync(string message)
        {
            if (writer == null)
                return;

            await writer.WriteLineAsync(message);
        }

        public async Task<string?> ReceiveAsync()
        {
            if (reader == null)
                return null;

            return await reader.ReadLineAsync();
        }

        public void Disconnect()
        {
            writer?.Close();
            reader?.Close();
            client?.Close();

            writer = null;
            reader = null;
            client = null;
        }
    }
}
