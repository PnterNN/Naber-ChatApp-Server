using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace JavaProject___Server.NET.IO
{
    internal class PacketBuilder
    {
        MemoryStream _ms;

        private object locker = new object();
        public PacketBuilder()
        {
            _ms = new MemoryStream();
        }

        //Paketin ne tür olduğunu belirliyor
        public void WriteOpCode(byte opcode)
        {
            lock (locker)
            {
                _ms.WriteByte(opcode);
            }
        }

        //Paketin içindeki mesajı yazıyor
        public void WriteMessage(string msg)
        {
            lock (locker)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
                _ms.Write(lengthBytes, 0, lengthBytes.Length);
                _ms.Write(messageBytes, 0, messageBytes.Length);
            }
            //byte[] buff = BitConverter.GetBytes(msg.Length);
            //_ms.Write(buff, 0, buff.Length);
            //_ms.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
        }
        public void WriteAudioMessage(byte[] msg, int startingIndex, int endingIndex)
        {
            lock (locker)
            {
                var msgLength = msg.Length;

                _ms.Write(BitConverter.GetBytes(msgLength), 0, BitConverter.GetBytes(msg.Length).Length);

                _ms.Write(msg, 0, msgLength);
            }
        }
        public byte[] GetPacketBytes()
        {
            lock (locker)
            {
                var sendPacket = _ms.ToArray();
                Clear(_ms);
                return sendPacket;
            }
        }
        public void Clear(MemoryStream source)
        {
            lock (locker)
            {
                byte[] buffer = source.GetBuffer();
                Array.Clear(buffer, 0, buffer.Length);
                source.Position = 0;
                source.SetLength(0);
            }
        }

        public void WriteScreenImageMessage(byte[] bitmap)
        {
            lock (locker)
            {
                int imageLength = bitmap.Length;

                _ms.Write(BitConverter.GetBytes(imageLength), 0, BitConverter.GetBytes(imageLength).Length);

                _ms.Write(bitmap, 0, imageLength);
            }
        }

    }
}
