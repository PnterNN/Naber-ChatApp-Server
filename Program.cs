using JavaProject___Server.NET.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using System.Net.Sockets;
using System.Threading.Tasks;
using MySqlX.XDevAPI;
using JavaProject___Server.NET.SQL;
using Microsoft.Win32;

namespace JavaProject___Server
{
    internal class Program
    {
        static TcpListener _listener;
        static TcpListener _listenerAudio;
        public static List<Client> _users;
        public static List<AudioClient> _audioUsers;

        private static object locker = new object();

        

        //Restfull API için gerekli değişkenler
        static HttpListener _httpListener;
        private static readonly string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>ChatApp RESTFULL API</title>" +
            "  </head>" +
            "  <body style=\"font-family:verdana;\">" +
            "    <p>{{Username: {0},UID: {1}}}</p>" +
            "  </body>" +
            "</html>";

        public static void Main()
        {

            //Burda chat sunucusunu başlatıyoruz 9001 portunu kullanıyor
            _users = new List<Client>();
            _listener = new TcpListener(IPAddress.Any, 9001);
            _listenerAudio = new TcpListener(IPAddress.Any, 9000);
            Console.WriteLine("Starting chat server on *:9001");
            Console.WriteLine("Starting NAudio server on *:9000");
            _listener.Start();
            _listenerAudio.Start();
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var entry = _listener.AcceptTcpClientAsync();
                    var client = new Client(await entry);
                }
            });
            //konsolda yetkili komutları kullanmak için
            _ = Task.Run(() =>
            {
                while (true)
                {
                    string[] args = Console.ReadLine().Split(' ');
                    if (args[0] == "/exit")
                    {
                        Environment.Exit(0);
                    }
                    else if (args[0] == "/users")
                    {
                        foreach (var user in _users)
                        {
                            Console.WriteLine("[" + DateTime.Now + "]: "+ user.Username + "[/"+ user.IPAdress +"] named user uid is " + user.UID);
                        }
                    }
                    else if (args[0] == "/clear")
                    {
                        Console.Clear();
                    }
                    else if (args[0] == "/help")
                    {
                        Console.WriteLine("[" + DateTime.Now + "]: /exit - exit server");
                        Console.WriteLine("[" + DateTime.Now + "]: /users - show connected users");
                        Console.WriteLine("[" + DateTime.Now + "]: /clear - clear console");
                    }
                    else
                    {
                        Console.WriteLine("[" + DateTime.Now + "]: Unknown command");
                    }
                }
            }); 
            //Burda Restfull api sunucuyu başlatıyoruz 8000 portunu kullanıyor
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8000/");
            Console.WriteLine("Starting restfull api on *:8000");
            _httpListener.Start();
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
        }
        //Restfull api için gerekli Bilgileri kullanılan fonksiyon
        public static async Task HandleIncomingConnections()
        {
            while (true)
            {
                HttpListenerContext ctx = await _httpListener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                string username = "NOT FOUND";
                string uid = "NOT FOUND";
                foreach (var user in _users)
                {
                    if (req.Url.AbsolutePath.ToString() == "/username/" + user.Username)
                    {
                        Console.WriteLine(user.Username + " named user sending informations...");
                        username = user.Username;
                        uid = user.UID.ToString();
                    }
                    if (req.Url.AbsolutePath.ToString() == "/uid/" + user.UID)
                    {
                        Console.WriteLine(user.Username + " named user sending informations...");
                        username = user.Username;
                        uid = user.UID.ToString();
                    }
                }
                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, username, uid));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        private static List<Client> convertUidToClient(string clientID)
        {
            List<Client> clients = new List<Client>();
            string[] id = clientID.Split(' ');
            foreach (var user in _users)
            {
                foreach (var uid in id)
                {
                    if (user.UID.ToString() == uid)
                    {
                        clients.Add(user);
                    }
                }
            }
            return clients;
        }

        public static void SendRegisterInfo(Client client, bool state)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(0);
            packet.WriteMessage(state.ToString());
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }
        public static void SendLoginInfo(Client client, bool state)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(1);
            packet.WriteMessage(state.ToString());
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }
        public static void sendUserInfo(Client client, string username, string uid)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(2);
            packet.WriteMessage(username);
            packet.WriteMessage(uid);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }

        public static void sendConnectionInfo(MySqlDataBase sql)
        {
            foreach (var user in _users)
            {
                foreach (var u in _users)
                {
                    if (user.UID != u.UID)
                    {
                        //u kendisi
                        //user programa giren kişi
                        var packet = new PacketBuilder();
                        packet.WriteOpCode(3);
                        packet.WriteMessage(user.Username);
                        packet.WriteMessage(user.UID.ToString());
                        Dictionary<int, List<string>> messages = sql.getMessages(u);
                        int messagecount = 0;
                        foreach (var message in messages.Values)
                        {
                            if (message[1] == user.UID.ToString())
                            {
                                messagecount++;
                            }
                        }
                        packet.WriteMessage(messagecount.ToString());
                        foreach (var message in messages.Values)
                        {
                            if (message[1] == user.UID.ToString())
                            {
                                for (int i = 0; i < 7; i++)
                                {
                                    packet.WriteMessage(message[i]);
                                }
                            }
                        }
                        u.ClientSocket.Client.Send(packet.GetPacketBytes());
                    }
                }
            }
        }
        public static void sendDisconnectionInfo(Client client)
        {
            _users.Remove(client);
            var packet = new PacketBuilder();
            packet.WriteOpCode(4);
            packet.WriteMessage(client.UID);
            foreach (var u in _users)
            {
                u.ClientSocket.Client.Send(packet.GetPacketBytes());
            }
        }
        public static void sendMessage(string msg, string contactUID, string senderUID, string messageUID)
        {
            if (contactUID.Contains(" "))
            {
                List<Client> clients = convertUidToClient(contactUID);
                foreach (Client client in clients)
                {
                    try
                    {
                        var packet = new PacketBuilder();
                        packet.WriteOpCode(5);
                        packet.WriteMessage(msg);
                        packet.WriteMessage(_users.Where(x => x.UID.ToString() == senderUID).FirstOrDefault().Username);
                        packet.WriteMessage(contactUID);
                        packet.WriteMessage(messageUID);
                        client.ClientSocket.Client.Send(packet.GetPacketBytes());
                    }
                    catch
                    {
                        //Kullanıcı Uygulamadan çıkmış olabilir sql ile burda çok fazla uğrasılacak!!!
                    }
                }
            }
            else
            {
                foreach (var u in _users)
                {
                    if (u.UID.ToString() == contactUID.ToString())
                    {
                        try
                        {
                            var packet = new PacketBuilder();
                            packet.WriteOpCode(5);
                            packet.WriteMessage(msg);
                            packet.WriteMessage(_users.Where(x => x.UID.ToString() == senderUID).FirstOrDefault().Username);
                            packet.WriteMessage(senderUID);
                            packet.WriteMessage(messageUID);
                            u.ClientSocket.Client.Send(packet.GetPacketBytes());
                        }
                        catch
                        {
                            //Kullanıcı Uygulamadan çıkmış olabilir sql ile burda çok fazla uğrasılacak!!!
                        }
                    }
                }
            }
        }

        public static void BroadcastAudio(byte[] audioBuffer)
        {
            foreach (var audioUser in _audioUsers)
            {
                var broadcastAudioPacket = new PacketBuilder();
                broadcastAudioPacket.WriteAudioMessage(audioBuffer, 0, audioBuffer.Length);
                audioUser.AudioClientSocket.Client.Send(broadcastAudioPacket.GetPacketBytes());
            }
        }

        public static void createGroup(string groupName, string clientIDS)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(6);
            packet.WriteMessage(groupName);
            packet.WriteMessage(clientIDS);

            List<Client> clients = convertUidToClient(clientIDS);
            foreach (Client client in clients)
            {
                client.ClientSocket.Client.Send(packet.GetPacketBytes());
            }
        }
        public static void deleteMessage(string messageUID, string userUID, string contactUID)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(7);
            packet.WriteMessage(contactUID);
            packet.WriteMessage(messageUID);
            foreach (var u in _users)
            {
                if (u.UID.ToString() == userUID)
                {
                    u.ClientSocket.Client.Send(packet.GetPacketBytes());
                }
            }
        }
        public static void BroadcastMutedState(string currentColor, string UID)
        {
            foreach (var user in _users)
            {
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(8);
                broadcastPacket.WriteMessage(currentColor);
                broadcastPacket.WriteMessage(UID);
                user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }
        }
    }
}
