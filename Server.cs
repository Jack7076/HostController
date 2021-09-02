using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace HostController
{
    class Server
    {
        private List<Client> rootClientsList { get; set; }
        private RSACryptoServiceProvider RSACSP = new RSACryptoServiceProvider();
        private CryptoManager cryptMan = new CryptoManager();

        public Server(List<Client> rootClientsList)
        {
            RSACSP.PersistKeyInCsp = false;
            this.rootClientsList = rootClientsList;
            RSACSP.ImportParameters(cryptMan.getRSAParameters());
        }

        public static List<Client> _clients = new List<Client>();
        public int Port = 4099;
        public IPAddress BindAddress = IPAddress.Any;

        Socket _svSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        byte[] _buffer = new byte[1024];

        public void setupServer()
        {
            Debug.WriteLine("Server: Begin Setup of Server");
            _svSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            _svSocket.Listen(3);
            _svSocket.BeginAccept(new AsyncCallback(acceptNewConnection), null);
        }

        private void acceptNewConnection(IAsyncResult AR)
        {
            Socket socket = _svSocket.EndAccept(AR);
            Client newClient = new Client(socket, _clients.Count);
            _clients.Add(newClient);
            this.rootClientsList = _clients;
            Debug.WriteLine("Server: Accepted new Client Connection (id): " + newClient.id.ToString());
            newClient.socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(writeCurrentData), newClient);
            _svSocket.BeginAccept(new AsyncCallback(acceptNewConnection), null);
        }

        public void heartbeat()
        {
            for(int i = 0; i < _clients.Count; i++)
            { 
                if ((DateTime.Now - _clients[i].age).TotalSeconds >= 10)
                {
                    try
                    {
                        _clients[i].sendData("AAA");
                    }
                    catch (SocketException)
                    {
                        _clients[i].terminateConnection();
                        _clients.Remove(_clients[i]);
                    }
                }
            }
            for(int i = 0; i < _clients.Count; i++)
            {
                if ((DateTime.Now - _clients[i].age).TotalSeconds >= 60)
                {
                    _clients[i].terminateConnection();
                    _clients.Remove(_clients[i]);
                }
            }
        }

        private void writeCurrentData(IAsyncResult AR)
        {
            try
            {
                Client client = (Client)AR.AsyncState;
                client.age = DateTime.Now;
                Socket socket = client.socket;
                int received = socket.EndReceive(AR);
                byte[] dataFlush = new byte[received];
                Array.Copy(_buffer, dataFlush, received);
                try
                {
                    Debug.WriteLine("Server: Got Data: " + Encoding.UTF8.GetString(dataFlush));
                }
                catch (DecoderFallbackException)
                {
                    Debug.WriteLine("Server: Got Data: " + byteArrayToHexString(dataFlush));
                }

                try
                {
                    if (Encoding.UTF8.GetString(dataFlush) == "PZLNETED")
                    {
                        dataFinished(client);
                    } else
                    {
                        client.memory.Write(dataFlush, 0, dataFlush.Length);
                    }
                }
                catch (DecoderFallbackException) 
                {
                    client.memory.Write(dataFlush, 0, dataFlush.Length);
                }

                //client.sendData("OK!");
                client.socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(writeCurrentData), client);
            }
            catch (SocketException) { }
        }

        private void dataFinished(Client client)
        {
            byte[] allrecdata = client.memory.ToArray();
            allrecdata = RSACSP.Decrypt(allrecdata, false);
            client.cleanUp();
            Debug.WriteLine("Server: Data Writes Complete");
            Debug.WriteLine("Server: Final Data Message was: " + byteArrayToHexString(allrecdata) + " Total Byte Size: " + allrecdata.Length);
            client.sendData("OK!");
        }

        private void sendCallback(IAsyncResult AR)
        {
            Client client = (Client)AR.AsyncState;
            Socket socket = client.socket;
            socket.EndSend(AR);
        }

        private string byteArrayToHexString(byte[] byteArray)
        {
            return BitConverter.ToString(byteArray).Replace("-", "");
        }
    }
}
