using System;
using System.Net.Sockets;
using Bindings;

namespace C_Server {
    class Client {
        public int index;
        public string ip;
        public Socket socket;
        public bool closing = false;
        public Match joinedMatch;

        private byte[] _buffer = new byte[1024];

        public void StartClient() {
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            closing = false;
        }

        private void ReceiveCallback(IAsyncResult ar) {
            Socket socket = (Socket)ar.AsyncState;

            try {
                int received = socket.EndReceive(ar);

                if (received <= 0) {
                    CloseClient(index);
                } else {
                    byte[] dataBuffer = new byte[received];
                    Array.Copy(_buffer, dataBuffer, received);

                    ServerHandleNetworkData.HandleNetworkInformation(index, dataBuffer);

                    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                }
            } catch {
                CloseClient(index);
            }
        }

        private void CloseClient(int index) {
            closing = true;
            socket.Close();

            ServerTCP.OnClientDisconnect(this);

            Console.WriteLine("Connection from {0} has been terminated.", ip);
        }
    }
}
