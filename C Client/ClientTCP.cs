using System;
using System.Net.Sockets;
using Bindings;

namespace C_Client {
    class ClientTCP {
        private static Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public static void ConnectToServer() {
            Console.WriteLine("Connecting to server...");

            _clientSocket.BeginConnect("127.0.0.1", 14000, new AsyncCallback(ConnectCallback), _clientSocket);
        }

        private static void ConnectCallback(IAsyncResult ar) {
            _clientSocket.EndConnect(ar);

            while (_clientSocket.Connected) {
                OnReceive();
            }
        }

        private static void OnReceive() {
            byte[] _sizeInfo = new byte[4];
            byte[] _receivedBuffer = new byte[1024];
            int totalRead = 0;
            int currentRead = 0;

            try {
                totalRead = _clientSocket.Receive(_sizeInfo);
                currentRead = totalRead;

                if (totalRead <= 0) {
                    Console.WriteLine("You are not connected to the server.");
                } else {
                    while (totalRead < _sizeInfo.Length && currentRead > 0) {
                        currentRead = _clientSocket.Receive(_sizeInfo, totalRead, _sizeInfo.Length - totalRead, SocketFlags.None);
                        totalRead += currentRead;
                    }

                    int messageSize = 0;
                    messageSize |= _sizeInfo[0];
                    messageSize |= (_sizeInfo[1] << 8);
                    messageSize |= (_sizeInfo[2] << 16);
                    messageSize |= (_sizeInfo[3] << 24);

                    byte[] data = new byte[messageSize];

                    totalRead = _clientSocket.Receive(data, 0, data.Length, SocketFlags.None);
                    currentRead = totalRead;

                    while (totalRead < messageSize && currentRead > 0) {
                        currentRead = _clientSocket.Receive(data, totalRead, data.Length - totalRead, SocketFlags.None);
                        totalRead += currentRead;
                    }

                    ClientHandleNetworkData.HandleNetworkInformation(data);
                }
            } catch {
                _clientSocket.Close();

                Console.WriteLine("Connection to server terminated.");
            }
        }

        public static void SendData(byte[] data) {
            _clientSocket.Send(data);
        }
    }
}
