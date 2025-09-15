using System.Runtime.InteropServices;
using UnityEngine;

namespace Client
{
    public struct PlayerInputState
    {
        public Vector3 MovePos;
        public command_type Type;
    }

    public enum packet_type
    {
        Join = 1,
        Response = 2,
        Command= 3,
        CommandSet=4
    }

    public enum command_type
    {
        Create=1,
        Move=2,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct packet_header
    {
        public int packet_type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct player_input_command
    {
        public int packet_type;

        public int id; // 客户端id
        public int command_type;
        public float x, y, z; // 移动位置
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct frame_packet
    {
        public int packet_type;
        
        public int frame_number;
        public int command_count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public player_input_command[] commands;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct join_packet
    {
        public int packet_type;
        
        public int id;
        public int frame_number;
    }
}