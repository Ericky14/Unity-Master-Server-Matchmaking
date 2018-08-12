using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Sockets;
using Bindings;

namespace C_Server {
    class Match {
        private static List<int> availablePorts = new List<int>();

        public Socket socket;
        public int index;
        public int port;
        public bool ready = false;
        public bool started = false;
        public List<Client> players = new List<Client>();

        private byte[] _buffer = new byte[1024];
        private Process process;

        static Match() {
            for (int i = 1; i <= 10; i++) {
                availablePorts.Add(Constants.MASTER_SERVER_PORT + i);
            }
        }

        public Match(int index) {
            this.index = index;
        }

        public void StartClient() {
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        private void ReceiveCallback(IAsyncResult ar) {
            Socket socket = (Socket)ar.AsyncState;

            try {
                int received = socket.EndReceive(ar);

                if (received <= 0) {
                    CloseClient();
                } else {
                    byte[] dataBuffer = new byte[received];
                    Array.Copy(_buffer, dataBuffer, received);

                    ServerHandleNetworkData.HandleNetworkInformation(index, dataBuffer);

                    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                }
            } catch {
                CloseClient();
            }
        }

        private void CloseClient() {
            players.ForEach((player) => {
                player.joinedMatch = null;
            });

            socket.Close();
            availablePorts.Add(port);
            ServerTCP.OnMatchStop(this);

            if (!process.HasExited) {
                process.Kill();
            }

            Console.WriteLine("Connection from match {0} has been terminated.", index);
        }

        private bool OnApplicationClose(Program.CtrlTypes type) {
            if (process != null) {
                process.Kill();
            }

            return true;
        }

        public void StartMatch() {
            while (availablePorts.Count < 0) {
            }

            port = availablePorts[0];
            availablePorts.RemoveAt(0);

            process = new Process();
            // process.EnableRaisingEvents = false;
            process.StartInfo.Arguments = String.Format(
                "{0} {1} {2} {3} {4} {5}",
                // Constants.CMD_BATCHMODE,
                Constants.CMD_PORT, port,
                Constants.CMD_INDEX, index,
                Constants.CMD_KEY, "\"" + ServerHandleNetworkData.masterServerKey.ToString() + "\""
            );

            process.StartInfo.FileName = "E:\\Unity Projects\\Hockey - Game\\Build\\Hockey - Game.exe";
            process.Start();

            Program.SetConsoleCtrlHandler(new Program.HandlerRoutine(OnApplicationClose), true);
        }

        public void AddPlayer(Client player) {
            if (player == null) {
                return;
            }

            Console.WriteLine("Player {0} joined match {1}.", player.index, index);

            player.joinedMatch = this;
            players.Add(player);

            if (players.Count >= 2) {
                Console.WriteLine("Match {0} ready.", index);

                ready = true;
            }
        }
    }
}
