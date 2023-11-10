using JavaProject___Server.NET.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JavaProject___Server
{
    internal class Program
    {
        static TcpListener _listener;
        public static List<Client> _users;


        //Restfull API için gerekli değişkenler
        static HttpListener _httpListener;
        private static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>ChatApp RESTFULL API</title>" +
            "  </head>" +
            "  <body style=\"font-family:verdana;\">" +
            "    <p>{{Username: {0},UID: {1}}}</p>" +
            "  </body>" +
            "</html>";

        static void Main(string[] args)
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
                            Console.WriteLine(user.Username + "[/"+ user.IPAdress +"] named user uid is " + user.UID);
                        }
                    }
                    else if (input == "/clear")
                    {
                        Console.Clear();
                    }
                    else if (input == "/help")
                    {
                        Console.WriteLine("/exit - exit server");
                        Console.WriteLine("/users - show connected users");
                        Console.WriteLine("/clear - clear console");
                    }
                    else
                    {
                        Console.WriteLine("Unknown command");
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

        //Client bağlandığında diğer clientlere bağlandığını bildiriyor
        static void BroadcastConnection(Client client)
        {
            foreach (var user in _users)
            {
                foreach (var u in _users)
                {
                    if (user.UID != u.UID)
                    {
                        var packet = new PacketBuilder();
                        packet.WriteOpCode(1);
                        packet.WriteMessage(user.Username);
                        packet.WriteMessage(user.UID.ToString());
                        u.ClientSocket.Client.Send(packet.GetPacketBytes());
                    }
                }
            }
        }

        //Client Çıkış yaptığında bu fonksiyon çalışıyor
        public static void BroadcastDisconnect(string uid)
        {
            var disconnectedUser = _users.Where(x => x.UID.ToString() == uid).FirstOrDefault();
            if (disconnectedUser != null)
            {
                _users.Remove(disconnectedUser);
                var packet = new PacketBuilder();
                packet.WriteOpCode(10);
                packet.WriteMessage(uid);
                foreach (var u in _users)
                {
                    u.ClientSocket.Client.Send(packet.GetPacketBytes());
                }
            }
        }

        //Client farklı bir cliente mesaj paketi gönderdiğinde bu fonksiyon çalışıyor
        public static void SendMessageToUser(string msg, string user1UID, string user2UID)
        {
            foreach (var u in _users)
            {
                if (u.UID.ToString() == user1UID.ToString())
                {
                    var packet = new PacketBuilder();
                    packet.WriteOpCode(5);
                    packet.WriteMessage(msg);
                    packet.WriteMessage(_users.Where(x => x.UID.ToString() == user2UID).FirstOrDefault().Username);
                    packet.WriteMessage(user2UID);
                    u.ClientSocket.Client.Send(packet.GetPacketBytes());
                }
            }
        }
        //Client bir gruba mesaj paketi gönderdiğinde bu fonksiyon çalışıyor
        public static void SendMessageToGroup(string msg, string userUID, string groupUID)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(5);
            packet.WriteMessage(msg);
            packet.WriteMessage(_users.Where(x => x.UID.ToString() == groupUID).FirstOrDefault().Username);
            packet.WriteMessage(userUID);
            List<string> uids = userUID.Split(' ').ToList();
            foreach (string id in uids)
            {
                try
                {
                    _users.Where(x => x.UID.ToString() == id).FirstOrDefault().ClientSocket.Client.Send(packet.GetPacketBytes());
                }
                catch
                {

                }
            }
        }


        //Client grup açma paketini yolladığında bu kod çalışıyor
        public static void CreateGroup(string groupName, List<Client> clients)
        {

            string ids = "";
            foreach (Client client in clients)
            {
                ids += client.UID.ToString() + " ";
            }
            ids = ids.TrimEnd(' ');
            var packet = new PacketBuilder();
            packet.WriteOpCode(2);
            packet.WriteMessage(groupName);
            packet.WriteMessage(ids);
            foreach (Client client1 in clients)
            {
                client1.ClientSocket.Client.Send(packet.GetPacketBytes());
            }
        }

    }
}
