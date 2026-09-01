using System.Runtime.InteropServices;

namespace LockStepServer.Protocol;

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
public struct HashErrorPacket
{
    public int frame_number;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct JoinPacket
{
    public int id;
    public int frame_number;
    public int version;
}
