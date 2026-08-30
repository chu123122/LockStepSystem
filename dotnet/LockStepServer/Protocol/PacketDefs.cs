using System.Runtime.InteropServices;

namespace LockStepServer.Protocol;

public enum PacketType
{
    Join = 1,
    Response = 2,
    Command = 3,
    CommandSet = 4,
    InitWorld = 5,
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
public struct FramePacket
{
    public PacketType packet_type;

    public int frame_number;
    public int command_count;

    public PlayerInputCommand[] commands;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct JoinPacket
{
    public int id;
    public int frame_number;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerInputCommand
{
    public int id; // 客户端id
    public int command_type;
    public float x, y, z; // 移动位置
}
