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
using System.Security.Cryptography;

namespace JavaProject___Server
{
    internal class Program
    {
        static TcpListener _listener;
        public static List<Client> _users;

        public static void Main()
        {
            //Burda chat sunucusunu başlatıyoruz 9001 portunu kullanıyor
            _users = new List<Client>();
            _listener = new TcpListener(IPAddress.Any, 9001);
            Console.WriteLine("Starting chat server on *:9001");
            Console.WriteLine("Starting NAudio server on *:9000");
            _listener.Start();
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
            while (true)
            {

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

        public static void sendVoiceMessage(byte[] voice, string messageUID, string senderUID, Client client)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(22);
            packet.WriteAudioMessage(voice);
            packet.WriteMessage(messageUID);
            packet.WriteMessage(senderUID);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }


        public static void sendFriendRemove(Client client, string username)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(19);
            packet.WriteMessage(username);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }
        public static void sendFriendRequestDecline(Client client, string username)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(18);
            packet.WriteMessage(username);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }
        public static void sendFriendRequestAccept(Client client, string username)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(17);
            packet.WriteMessage(username);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }
        public static void sendFriendRequestCancel(Client client, string username)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(16);
            packet.WriteMessage(username);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }

        public static void sendFriendRequest(Client client, string username)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(15);
            packet.WriteMessage(username);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }
        public static void SendToManyPackets(Client client)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(10);
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }

        public static void SendUsersInfo(Client client)
        {
            var packet = new PacketBuilder();
            packet.WriteOpCode(11);
            packet.WriteMessage("");
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
            foreach (Client user in _users)
            {
                var packet2 = new PacketBuilder();
                packet.WriteOpCode(12);
                packet2.WriteMessage(user.Username);
                packet2.WriteMessage(user.UID);
                client.ClientSocket.Client.Send(packet2.GetPacketBytes());
            }
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

        public static void deleteTweet(string tweetUID)
        {
            foreach (var user in _users)
            {
                var packet = new PacketBuilder();
                packet.WriteOpCode(14);
                packet.WriteMessage(tweetUID);
                user.ClientSocket.Client.Send(packet.GetPacketBytes());
            }
                
        }
        public static void sendFriends(MySqlDataBase sql, Client client)
        {
            Dictionary<int, List<string>> friends = sql.getFriend(client.Username);
            var packet = new PacketBuilder();
            packet.WriteOpCode(20);
            packet.WriteMessage(friends.Values.Count.ToString());
            foreach (var infos in friends.Values)
            {
                for (int i = 0; i < 3; i++)
                {
                    packet.WriteMessage(infos[i]);
                }
            }
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }
        public static void sendTweets(MySqlDataBase sql, Client client)
        {
            Dictionary<int, List<string>> tweets = sql.getTweets();
            var packet = new PacketBuilder();
            packet.WriteOpCode(13);
            packet.WriteMessage(tweets.Values.Count.ToString());
            foreach (var infos in tweets.Values)
            {
                for (int i = 0; i < 6; i++)
                {
                    packet.WriteMessage(infos[i]);
                }
            }
            client.ClientSocket.Client.Send(packet.GetPacketBytes());
        }

        public static void sendRegisterConnectionInfo()
        {
            foreach (var user in _users)
            {
                foreach (var u in _users)
                {
                    if (user.UID != u.UID)
                    {
                        var packet = new PacketBuilder();
                        packet.WriteOpCode(21);
                        packet.WriteMessage(user.Username);
                        packet.WriteMessage(user.UID.ToString());
                        u.ClientSocket.Client.Send(packet.GetPacketBytes());
                    }
                }
            }
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
                        Console.WriteLine("" + u.Username + " adlı kullanıcıya mesajları gönderildi");
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
            try
            {
                foreach (var u in _users)
                {

                    u.ClientSocket.Client.Send(packet.GetPacketBytes());

                }
            }
            catch 
            { 

            }
            
        }

        public static void likeTweet(string UID, string tweetUID)
        {
            foreach (var u in _users)
            {
                var packet = new PacketBuilder();
                packet.WriteOpCode(11);
                packet.WriteMessage(UID);

                packet.WriteMessage(tweetUID);
                u.ClientSocket.Client.Send(packet.GetPacketBytes());
            }
        }

        public static void sendTweet(string username, string tweetUID, string tweet)
        {
           
            foreach (var u in _users)
            {
                var packet = new PacketBuilder();
                packet.WriteOpCode(12);
                packet.WriteMessage(username);
                packet.WriteMessage(tweet);
                packet.WriteMessage(tweetUID);
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
