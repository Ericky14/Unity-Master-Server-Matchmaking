using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Bindings;

namespace C_Server {
    class ServerTCP {
        public static bool running = false;

        public static List<Client> clientList = new List<Client>();
        public static List<Client> queue = new List<Client>();
        public static List<Match> availableMatches = new List<Match>();
        public static List<Match> readyMatches = new List<Match>();
        public static List<Match> startedMatches = new List<Match>();

        private static Client[] _clients = new Client[Constants.MAX_PLAYERS];
        private static Match[] _matches = new Match[Constants.MAX_MATCHES];
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public static void SetupServer() {
            running = true;
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Constants.MASTER_SERVER_PORT));
            _serverSocket.Listen(10);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            Task.Factory.StartNew(ProcessQueue);
            Task.Factory.StartNew(MatchStartQueue);

            Console.WriteLine("Server listening at {0}:{1}.", IPAddress.Any, Constants.MASTER_SERVER_PORT);
        }

        private static void AcceptCallback(IAsyncResult ar) {
            Socket socket = _serverSocket.EndAccept(ar);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

            byte[] tempData = new byte[1024];
            socket.ReceiveTimeout = (int)Constants.CONNECT_TIMEOUT * 1000;

            try {
                PacketBuffer buffer = new PacketBuffer();
                buffer.WriteServerPacket(ServerPackets.SAskIfClientOrServer);
                SendDataTo(socket, buffer.ToArray());
                buffer.Dispose();

                socket.Receive(tempData);

                PacketBuffer receivedbuffer = new PacketBuffer(tempData);
                ClientPackets packet = receivedbuffer.ReadClientPacket();
                string key = receivedbuffer.ReadString();
                int index = receivedbuffer.ReadInteger();

                if (packet != ClientPackets.CSendKey) {
                    throw new Exception("Wrong packet sent to server.");
                }

                if (key == ServerHandleNetworkData.masterServerKey) {
                    ConnectMatchServer(socket, index);
                } else if (key == ServerHandleNetworkData.clientKey) {
                    ConnectPlayer(socket);
                } else {
                    throw new Exception("Wrong key sent to server.");
                }
            } catch {
                socket.Close();
            }
        }

        private static void ConnectMatchServer(Socket socket, int matchIndex) {
            Match match = _matches[matchIndex];

            if (matchIndex < 0) {
                throw new Exception("Wrong match index sent to server.");
            } else if (match != null) {
                match.started = true;
                match.socket = socket;
                match.StartClient();

                Console.WriteLine("Match {0} with port {1} connected.", matchIndex, match.port);

                SendMatchConnectionReady(match);
            } else {
                throw new Exception("Non-existant match.");
            }
        }

        private static void ConnectPlayer(Socket socket) {
            for (int i = 0; i < Constants.MAX_PLAYERS; i++) {
                if (_clients[i] == null) {
                    Client client = new Client();

                    _clients[i] = client;
                    clientList.Add(client);

                    client.socket = socket;
                    client.index = i;
                    client.ip = socket.RemoteEndPoint.ToString();
                    client.StartClient();

                    Console.WriteLine("Connection from '{0}' received as player.", client.ip);

                    SendPlayerConnectionReady(client);

                    return;
                }
            }

            Console.WriteLine("Player limit reached, cannot connect.");

            throw new Exception("Too many players.");
        }

        public static void SendDataTo(int index, byte[] data) {
            SendDataTo(_clients[index].socket, data);
        }

        public static void SendDataTo(Socket socket, byte[] data) {
            byte[] sizeInfo = new byte[4];
            sizeInfo[0] = (byte)data.Length;
            sizeInfo[1] = (byte)(data.Length >> 8);
            sizeInfo[2] = (byte)(data.Length >> 16);
            sizeInfo[3] = (byte)(data.Length >> 24);

            socket.Send(sizeInfo);
            socket.Send(data);
        }

        public static void SendAlert(Client player, string text) {
            Console.WriteLine("Sending alert to player {0}.", player.index);

            PacketBuffer buffer = new PacketBuffer();
            buffer.WriteServerPacket(ServerPackets.SAlert);
            buffer.WriteString(text);

            SendDataTo(player.socket, buffer.ToArray());
            buffer.Dispose();
        }

        public static void SendMatchConnectionReady(Match match) {
            Console.WriteLine("Sending connection ready to match {0}.", match.index);

            PacketBuffer buffer = new PacketBuffer();
            buffer.WriteServerPacket(ServerPackets.SMatchConnectionReady);
            buffer.WriteInteger(match.players.Count);

            match.players.ForEach((player) => {
                buffer.WriteInteger(player.index);
            });

            SendDataTo(match.socket, buffer.ToArray());
            buffer.Dispose();
        }

        public static void SendPlayerConnectionReady(Client player) {
            Console.WriteLine("Sending connection ready to player {0}.", player.index);

            PacketBuffer buffer = new PacketBuffer();
            buffer.WriteServerPacket(ServerPackets.SPlayerConnectionReady);
            buffer.WriteInteger(player.index);
            SendDataTo(player.socket, buffer.ToArray());
            buffer.Dispose();
        }

        public static void OnClientDisconnect(Client client) {
            queue.Remove(client);
            clientList.Remove(client);
            _clients[client.index] = null;
        }

        #region Matchmaking

        public static void AddToQueue(int index) {
            queue.Add(_clients[index]);
        }

        private static void ProcessQueue() {
            while (running) {
                if (queue.Count > 0) {
                    if (availableMatches.Count > 0) {
                        availableMatches[0].AddPlayer(queue[0]);
                        queue.RemoveAt(0);

                        if (availableMatches[0].ready) {
                            readyMatches.Add(availableMatches[0]);
                            availableMatches.RemoveAt(0);
                        }
                    } else {
                        for (int i = 0; i < _matches.Length; i++) {
                            if (_matches[i] == null) {
                                Console.WriteLine("Create new match {0}.", i);

                                Match match = _matches[i] = new Match(i);
                                availableMatches.Add(match);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void MatchStartQueue() {
            while (running) {
                if (readyMatches.Count > 0) {
                    Task.Factory.StartNew(readyMatches[0].StartMatch);
                    startedMatches.Add(readyMatches[0]);
                    readyMatches.RemoveAt(0);
                }
            }
        }

        public static void OnMatchServerReady(int index) {
            Match match = _matches[index];

            if (match != null) {
                match.players.ForEach((player) => {
                    ConnectPlayerToMatch(player, match);
                });
            } else {
                Console.WriteLine("No match found for readied match {0}.", index);
            }
        }

        public static void OnMatchStop(Match match) {
            availableMatches.Remove(match);
            readyMatches.Remove(match);
            startedMatches.Remove(match);
            _matches[match.index] = null;
        }

        public static void ConnectPlayerToMatch(Client player, Match match) {
            Console.WriteLine("Connect player {0} to match {1}.", player.index, match.index);

            PacketBuffer buffer = new PacketBuffer();
            buffer.WriteServerPacket(ServerPackets.SConnectToMatch);
            buffer.WriteInteger(match.index);
            buffer.WriteInteger(match.port);
            SendDataTo(player.socket, buffer.ToArray());
            buffer.Dispose();
        }

        public static void SendPlayerDataToMatch(int playerIndex, int matchIndex) {
            Client player = _clients[playerIndex];
            Match match = _matches[matchIndex];

            if (player != null && match != null) {
                Console.WriteLine("Sending player {0} data to match {1}.", playerIndex, matchIndex);

                PacketBuffer buffer = new PacketBuffer();
                buffer.WriteServerPacket(ServerPackets.SSendPlayerData);
                buffer.WriteInteger(playerIndex);
                buffer.WriteString("Name placeholder");
                SendDataTo(match.socket, buffer.ToArray());
                buffer.Dispose();
            } else {
                Console.WriteLine("Invalid data requested for player {0} from match {1}.", playerIndex, matchIndex);
            }
        }

        #endregion
    }
}
