using JavaProject___Server.NET.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using JavaProject___Server.NET.SQL;

namespace JavaProject___Server
{

    public class Client
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string IPAdress { get; set; }
        public string UID { get; set; }


        public TcpClient ClientSocket { get; set; }

        PacketReader _packetReader;

        //Client giriş yapınca Sunucu clientin UID'sini ve username'ini kaydediyor ve sunucu loguna bilgi düşüyor
        public Client(TcpClient client)
        {

            //sql bağlandığında uid, mesajları ve username i sql den çekicez 

            ClientSocket = client;
            _packetReader = new PacketReader(ClientSocket.GetStream());
            try
            {
                IPAdress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                MySqlDataBase sql = new MySqlDataBase();


                bool status = false;
                while (!status)
                {
                    var opcode = _packetReader.ReadByte();
                    switch (opcode)
                    {
                        //opcode 0 ise kullanıcı kayıt oluyor
                        case 0:
                            Username = _packetReader.ReadMessage();
                            Email = _packetReader.ReadMessage();
                            Password = _packetReader.ReadMessage();
                            Console.WriteLine("[" + DateTime.Now + "]: [/" + IPAdress + "] user tried to sign up, checking information...");
                            if (sql.CheckRegisterUser(Username, Email))
                            {
                                Program.SendRegisterInfo(this, false);
                                Console.WriteLine("[" + DateTime.Now + "]: [/" + IPAdress + "] user already registered, username: " + Username);
                            }
                            else
                            {
                                UID = Guid.NewGuid().ToString();
                                Program.sendInfoToClient(this, Username, UID);
                                Console.WriteLine("[" + DateTime.Now + "]: [/" + IPAdress + "] user registered, username: " + Username);
                                sql.InsertUser(Username, UID, Email, Password);
                                status = true;
                                Program.SendRegisterInfo(this, true);
                                
                                Task.Run(() => Procces(sql));
                                sql.createUserStorage(this);
                            }
                            break;
                        //opcode 1 ise kullanıcı giriş yapıyor
                        case 1:
                            Email = _packetReader.ReadMessage();
                            Password = _packetReader.ReadMessage();
                            Console.WriteLine("[" + DateTime.Now + "]: [/" + IPAdress + "] user tried to sign in, checking information...");
                            if (sql.CheckLoginUser(Email, Password))
                            {

                                Username = sql.getName(Email);
                                UID = sql.getUID(Email);
                                Program.sendInfoToClient(this, Username, UID);
                                Console.WriteLine("[" + DateTime.Now + "]: [/" + IPAdress + "] user logged in, username: " + this.Username);
                                status = true;
                                Dictionary<int, List<string>> infos = sql.getMessages(this);
                                foreach (KeyValuePair<int,List<string>> info in infos)
                                {
                                    foreach(string s in info.Value)
                                    {
                                        Console.WriteLine(info.Key + " " + s);
                                    }
                                }
                                Program.SendLoginInfo(this, true);
                                
                                Task.Run(() => Procces(sql));
                            }
                            else
                            {
                                Console.WriteLine("[" + DateTime.Now + "]: [/" + IPAdress + "] user unknown account: " + Email);
                                Program.SendLoginInfo(this, false);
                            }
                            break;
                        //opcode yanlış ise bu hatayı veriyor konsola yazdırıyor
                        default:
                            Console.WriteLine("[" + DateTime.Now + "]: [/" + IPAdress + "] user unknown opcode: " + opcode);
                            break;
                    }
                }
            }
            catch
            {

            }
        }


        //Clientin paketlerini okuyor
        void Procces(MySqlDataBase sql)
        {
            
            while (true)
            {
                try
                {
                    var opcode = _packetReader.ReadByte();
                    switch (opcode)
                    {
                        case 5:
                            var message = _packetReader.ReadMessage();
                            var contactUID = _packetReader.ReadMessage();
                            var firstMessage = _packetReader.ReadMessage();
                            sql.InsertMessage(this.Username, this.Username, contactUID, "imagelink", message, DateTime.Now.ToString(), firstMessage);
                            sql.InsertMessage(Program._users.Where(x => x.UID.ToString() == contactUID).FirstOrDefault().Username, this.Username, contactUID, "imagelink", message, DateTime.Now.ToString(), firstMessage);
                            Program.SendMessageToUser(message,contactUID,UID);
                            break;       

                        //Eğer yanlış bir opcode gelirse bu hatayı veriyor konsola yazdırıyor
                        default:
                            Console.WriteLine("[" + DateTime.Now + "]: Unknown opcode: " + opcode);
                            break;
                    }
                }
                catch
                {
                    //Eğer Client Programı kapatırsa ve ya interneti giderse sunucu kullanıcının bilgilerini siliyor
                    Console.WriteLine("[" + DateTime.Now + "]: " + Username + "[/" + IPAdress + "] has disconnected.");
                    Program.BroadcastDisconnect(this);
                    ClientSocket.Close();
                    break;
                }
            }
        }
    }
}
