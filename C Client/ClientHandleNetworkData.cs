using System;
using System.Collections.Generic;
using Bindings;

namespace C_Client {
    class ClientHandleNetworkData {
        private delegate void Packet_(byte[] data);
        private static Dictionary<int, Packet_> Packets;

        public static void InitializeNetworkPackages() {
            Console.WriteLine("Initialized Network Packages");

            Packets = new Dictionary<int, Packet_> {
                { (int)ServerPackets.SPlayerConnectionReady, HandleConnectionReady }
            };
        }

        public static void HandleNetworkInformation(byte[] data) {
            Packet_ Packet;
            PacketBuffer buffer = new PacketBuffer();
            buffer.WriteBytes(data);
            int packetNumber = buffer.ReadInteger();
            buffer.Dispose();

            if (Packets.TryGetValue(packetNumber, out Packet)) {
                Packet.Invoke(data);
            }
        }

        private static void HandleConnectionReady(byte[] data) {
            PacketBuffer buffer = new PacketBuffer();
            buffer.WriteBytes(data);
            buffer.ReadInteger();
            string message = buffer.ReadString();
            buffer.Dispose();

            Console.WriteLine(message);
        }
    }
}
