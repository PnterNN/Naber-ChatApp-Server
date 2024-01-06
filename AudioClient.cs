using JavaProject___Server.NET.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JavaProject___Server
{
    internal class AudioClient
    {
        public string UID { get; set; }
        public string Username { get; set; }
        public bool MicrophoneState { get; set; }

        public TcpClient AudioClientSocket { get; set; }
        public PacketReader packetReader { get; set; }

        public AudioClient(Client client)
        {
            AudioClientSocket = client.ClientSocket;

            UID = client.UID;
            Username = client.Username;
            MicrophoneState = true;

            packetReader = new PacketReader(AudioClientSocket.GetStream());
            Task.Run(() => Process());
        }

        private void Process()
        {
            while (true)
            {
            }
        }

    }
}
