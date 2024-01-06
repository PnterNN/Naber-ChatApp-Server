using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace JavaProject___Server.NET.IO
{
    internal class PacketReader : BinaryReader
    {
        private NetworkStream _ns;
        public PacketReader(NetworkStream ns) : base(ns)
        {
            _ns = ns;
        }

        //Paketin mesajını okuyor
        public string ReadMessage()
        {
            byte[] msgBuffer;
            var length = ReadInt32();
            msgBuffer = new byte[length];
            _ns.Read(msgBuffer, 0, length);

            var msg = Encoding.UTF8.GetString(msgBuffer);
            _ns.Flush();
            return msg;
        }


        public byte[] ReadAudioMessage()
        {
            byte[] msgBuffer;
            var length = ReadInt32();
            msgBuffer = new byte[length];
            var opa = _ns.Read(msgBuffer, 0, length);
            byte[] copiedBuffer = new byte[opa];
            Array.Copy(msgBuffer, 0, copiedBuffer, 0, opa);
            return copiedBuffer;
        }

        public byte[] ReadScreenPicture()
        {
            byte[] msgBuffer;

            var length = ReadInt32();

            msgBuffer = new byte[length];

            Console.WriteLine("Received " + length);

            var opa = _ns.Read(msgBuffer, 0, length);

            Console.WriteLine("Read " + opa);

            byte[] copiedBuffer = new byte[opa];

            Array.Copy(msgBuffer, 0, copiedBuffer, 0, opa);

            Console.WriteLine("Returned array " + copiedBuffer.Length);

            return copiedBuffer;
        }
    }
}
