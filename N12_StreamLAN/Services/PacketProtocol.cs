using System;

namespace Server_StreamLAN.Services
{

    public static class PacketProtocol
    {
        public const int HeaderSize = 5;
        public const byte FlagKeyFrame = 0x01;
        public const byte FlagPaused   = 0x02;
        public const byte FlagAudio    = 0x04;

        public static byte[] Pack(uint seqNo, byte flags, byte[] data)
        {
            var packet = new byte[HeaderSize + data.Length];
            packet[0] = (byte)(seqNo & 0xFF);
            packet[1] = (byte)((seqNo >> 8)  & 0xFF);
            packet[2] = (byte)((seqNo >> 16) & 0xFF);
            packet[3] = (byte)((seqNo >> 24) & 0xFF);
            packet[4] = flags;
            Buffer.BlockCopy(data, 0, packet, HeaderSize, data.Length);
            return packet;
        }

        public static bool Unpack(byte[] packet, out uint seqNo, out byte flags, out byte[] data)
        {
            seqNo = 0; flags = 0; data = Array.Empty<byte>();
            if (packet == null || packet.Length <= HeaderSize) return false;
            seqNo = (uint)(packet[0] | (packet[1] << 8) | (packet[2] << 16) | (packet[3] << 24));
            flags = packet[4];
            data = new byte[packet.Length - HeaderSize];
            Buffer.BlockCopy(packet, HeaderSize, data, 0, data.Length);
            return true;
        }
    }
}
