using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bindings;

namespace C_Server {
    class ServerHandleNetworkData {
        public const string masterServerKey = "The Master Server";
        public const string clientKey = "client";

        private delegate void Packet_(int index, byte[] data);
        private static Dictionary<int, Packet_> Packets;

        public static void InitializeNetworkPackages() {
            Console.WriteLine("Initialized Network Packages.");

            Packets = new Dictionary<int, Packet_> {
                { (int)ClientPackets.CFindMatch, HandleFindMatch },
                { (int)ClientPackets.CMatchServerStarted, HandleMatchServerStarted },
                { (int)ClientPackets.CRequestPlayerData, HandleRequestPlayerData }
            };
        }

        public static void HandleNetworkInformation(int index, byte[] data) {
            Packet_ Packet;
            PacketBuffer buffer = new PacketBuffer(data);
            int packetNumber = buffer.ReadInteger();

            if (Packets.TryGetValue(packetNumber, out Packet)) {
                Packet.Invoke(index, data);
            }
        }

        private static void HandleFindMatch(int index, byte[] data) {
            Console.WriteLine("Player {0} requested to find match.", index);

            ServerTCP.AddToQueue(index);
        }

        private static void HandleMatchServerStarted(int index, byte[] data) {
            Console.WriteLine("Match {0} has readied its server.", index);

            ServerTCP.OnMatchServerReady(index);
        }

        private static void HandleRequestPlayerData(int index, byte[] data) {
            PacketBuffer buffer = new PacketBuffer();
            buffer.WriteBytes(data);
            buffer.ReadInteger();
            int playerIndex = buffer.ReadInteger();
            buffer.Dispose();

            Console.WriteLine("Match {0} has requested player data on player {1}.", index, playerIndex);

            ServerTCP.SendPlayerDataToMatch(playerIndex, index);
        }
    }
}
