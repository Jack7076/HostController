using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

namespace HostController
{
    class Client
    {
        public Socket socket { get; set; }
        public int id { get; set; }
        public MemoryStream memory { get; set; }
        public DateTime age { get; set; }
        private string chunkEnd = "PZLNETED";
        private RSACryptoServiceProvider RSACSP;
        private CryptoManager cryptMan = new CryptoManager();
        public Client(Socket socket, int id)
        {
            this.socket = socket;
            this.id = id;
            this.memory = new MemoryStream();
            this.age = DateTime.Now;
            RSACSP.ImportParameters(cryptMan.getRSAParameters());
        }

        public void terminateConnection()
        {
            this.socket.Close();
            cleanUp();
            this.memory = null;
        }

        public void cleanUp()
        {
            this.memory.Dispose();
            this.memory = new MemoryStream();
        }

        public void sendData(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            sendData(dataBytes);
        }
        public void sendData(byte[] data)
        {
            data = cryptMan.PrivateEncryption(data);
            byte[][] dataQueue = ByteArrayToChunks(data, 512);
            foreach (byte[] currentData in dataQueue)
            {
                Debug.WriteLine("Server: Send: " + byteArrayToHexString(currentData) + " Size of current is: " + currentData.Length);
                this.socket.Send(currentData);
            }
            Debug.WriteLine("Server: Send: " + byteArrayToHexString(Encoding.UTF8.GetBytes(chunkEnd)) + " Size of current is: " + Encoding.UTF8.GetBytes(chunkEnd).Length);
            this.socket.Send(Encoding.UTF8.GetBytes(chunkEnd));
        }

        private static byte[][] ByteArrayToChunks(byte[] byteData, long BufferSize)
        {
            byte[][] chunks = byteData.Select((value, index) => new { PairNum = Math.Floor(index / (double)BufferSize), value }).GroupBy(pair => pair.PairNum).Select(grp => grp.Select(g => g.value).ToArray()).ToArray();
            return chunks;
        }

        private static string byteArrayToHexString(byte[] byteArray)
        {
            return BitConverter.ToString(byteArray).Replace("-", "");
        }
    }
}
