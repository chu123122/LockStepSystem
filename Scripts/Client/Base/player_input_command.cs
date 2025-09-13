using System.Runtime.InteropServices;
using UnityEngine;

namespace Client
{
    public struct PlayerInputState
    {
        public Vector3 MovePos;
    }

    public enum packet_type
    {
        Join = 1,
        Response = 2
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct player_input_command
    {
        public int id; // 客户端id

        //  public int frame_number;
        // public int input_index; // 客户端操作指令编号
        public float x, y, z; // 移动位置
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct frame_packet
    {
        public int frame_number;
        public int command_count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public player_input_command[] commands;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct join_packet
    {
        public int type_index;
        //public string player_name;
        public int id;
        public int frame_number;
    }
}