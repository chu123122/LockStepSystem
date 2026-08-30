using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Client.Protocol;
using LockStepCore.Level;
using LockStepCore.Physics;

namespace Client.Base
{
    public static class Utils
    {
        public static byte[] StructToBytes(object obj)
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }
        
        public static (PacketHeader Header, object? Body) DeserializedPacket(byte[] data)
        {
            Reader r = new Reader(data);
            PacketHeader header = new PacketHeader { type = r.ReadInt() };
            if (Deserializer.TryGetValue((PacketType)header.type, out Func<Reader, object?>? deserialize))
            {
                return (header, deserialize(r));
            }

            throw new ArgumentException($"反序列化:未知包类型 {header.type}");
        }

     

        private static readonly Dictionary<PacketType, Func<Reader, object?>> Deserializer = new()
        {
            [PacketType.Response] = r => new JoinPacket { id = r.ReadInt(), frame_number = r.ReadInt() },
            [PacketType.CommandSet] = r => ReadFramePacket(r),
            [PacketType.InitWorld] = r => ReadInitWorld(r),
        };

        private static FramePacket ReadFramePacket(Reader r)
        {
            FramePacket fp = new FramePacket { frame_number = r.ReadInt(), command_count = r.ReadInt() };
            PlayerInputCommand[] cmds = new PlayerInputCommand[fp.command_count];
            for (int i = 0; i < cmds.Length; i++)
            {
                cmds[i] = new PlayerInputCommand
                {
                    id = r.ReadInt(),
                    command_type = r.ReadInt(),
                    x = r.ReadFloat(),
                    y = r.ReadFloat(),
                    z = r.ReadFloat(),
                };
            }

            fp.commands = cmds;
            return fp;
        }
        private static List<EntitySpawn> ReadInitWorld(Reader r)
        {
            int count = r.ReadInt();
            List<EntitySpawn> spawns = new List<EntitySpawn>(count);
            for (int i = 0; i < count; i++)
            {
                spawns.Add(new EntitySpawn
                {
                    EntityId = r.ReadInt(),
                    Shape = (Shape)r.ReadInt(),
                    X = r.ReadFloat(),
                    Y = r.ReadFloat(),
                    Z = r.ReadFloat(),
                    SizeX = r.ReadFloat(),
                    SizeY = r.ReadFloat(),
                    SizeZ = r.ReadFloat(),
                    IsDynamic = r.ReadInt() != 0,
                });
            }

            return spawns;
        }
    }
}