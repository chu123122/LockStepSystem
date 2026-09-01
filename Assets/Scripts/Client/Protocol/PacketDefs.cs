using System.Runtime.InteropServices;

namespace Client.Protocol
{
    public enum PacketTypeC2S
    {
        Join = 1,
        FrameC2S = 2,
    }

    public enum PacketTypeS2C
    {
        Response = 1,
        FrameS2C = 2,
        InitWorld = 3,
        HashError = 4,
    }

    public enum CommandType
    {
        Create = 1,
        Move = 2,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public int type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FramePacketC2S
    {
        public PlayerInputCommand command;
        public int frame_number;
        public ulong hash;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FramePacketS2C
    {
        public PacketTypeS2C packet_type;

        public int frame_number;
        public int command_count;

        public PlayerInputCommand[] commands;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct JoinPacket
    {
        public int id;
        public int frame_number;
        public int version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerInputCommand
    {
        public int id; // 客户端id
        public int command_type;
        public float x, y, z; // 移动位置
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HashErrorPacket
    {
        public int frame_number;
    }
}
