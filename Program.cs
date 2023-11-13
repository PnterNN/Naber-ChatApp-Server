using JavaProject___Server.NET.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using System.Net.Sockets;
using System.Threading.Tasks;
using MySqlX.XDevAPI;

namespace JavaProject___Server
{
    internal class Program
    {
        static TcpListener _listener;
        public static List<Client> _users;


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
            Console.WriteLine("Starting chat server on *:9001");
            _listener.Start();
            _ = Task.Run(() =>
            {
                while (true)
                {
                    var client = new Client(_listener.AcceptTcpClient());
                    _users.Add(client);
                    BroadcastConnection(client);
                }
            });
            //konsolda yetkili komutları kullanmak için
            _ = Task.Run(() =>
            {
                while (true)
                {
                    var input = Console.ReadLine();
                    if (input == "/exit")
                    {
                        Environment.Exit(0);
                    }
                    else if ( input == "/users")
                    {
                        foreach (var user in _users)
                        {
                            Console.WriteLine("[" + DateTime.Now + "]: "+ user.Username + "[/"+ user.IPAdress +"] named user uid is " + user.UID);
                        }
                    }
                    else if (input == "/clear")
                    {
                        Console.Clear();
                    }
                    else if (input == "/help")
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
        static void BroadcastConnection(Client client)
        {
            foreach (var user in _users)
            {
                foreach (var u in _users)
                {
                    if (user.UID != u.UID)
                    {
                        var packet = new PacketBuilder();
                        packet.WriteOpCode(3);
                        packet.WriteMessage(user.Username);
                        packet.WriteMessage(user.UID.ToString());
                        u.ClientSocket.Client.Send(packet.GetPacketBytes());

                    }
                }
            }
        }
        public static void BroadcastDisconnect(Client client)
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
        public static void SendMessageToUser(string msg, string contactUID, string senderUID)
        {
            foreach (var u in _users)
            {
                if (u.UID.ToString() == contactUID.ToString())
                {
                    var packet = new PacketBuilder();
                    packet.WriteOpCode(5);
                    packet.WriteMessage(msg);
                    packet.WriteMessage(_users.Where(x => x.UID.ToString() == senderUID).FirstOrDefault().Username);
                    packet.WriteMessage(senderUID);
                    u.ClientSocket.Client.Send(packet.GetPacketBytes());
                }
            }
        }
        public static void SendMessageToGroup(string msg, string contactUID, string senderUID)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(5);
            packet.WriteMessage(msg);
            packet.WriteMessage(_users.Where(x => x.UID.ToString() == senderUID).FirstOrDefault().Username);
            packet.WriteMessage(contactUID);
            List<Client> clients = convertUidToClient(contactUID);
            foreach (Client client in clients)
            {
                try
                {
                    client.ClientSocket.Client.Send(packet.GetPacketBytes());
                }
                catch
                {
                    //Kullanıcı Uygulamadan çıkmış olabilir sql ile burda çok fazla uğrasılacak!!!
                }
            }
        }
        public static void SendCreatedGroup(string groupName, string clientIDS)
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
        public static void sendInfoToClient(Client client, string username, string uid)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(2);
            packet.WriteMessage(username);
            packet.WriteMessage(uid);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
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
    }
    // OpCodes
    //0 - Register
    //1 - Login
    //2 - Info
    //3 - New Connection
    //4 - Disconnect
    //5 - Message - Group Message
    //6 - Group Created
}
