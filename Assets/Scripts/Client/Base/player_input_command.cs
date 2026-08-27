using System.Runtime.InteropServices;
using UnityEngine;

namespace Client
{
    public struct PlayerInputState
    {
        public Vector3 MovePos;
        public CommandType Type;
    }

    public enum PacketType
    {
        Join = 1,
        Response = 2,
        Command = 3,
        CommandSet = 4,
    }

    public enum CommandType
    {
        Create = 1,
        Move = 2,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public int packet_type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerInputCommand
    {
        public int id; // 客户端id
        public int command_type;
        public float x, y, z; // 移动位置
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FramePacket
    {
        public int frame_number;
        public int command_count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public PlayerInputCommand[] commands;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct JoinPacket
    {
        public int id;
        public int frame_number;
    }
}
