using System;
using System.Collections.Generic;
using LockStepCore.Level;
using LockStepServer.Protocol;

namespace LockStepServer;

/// <summary>
/// 协议序列化
/// </summary>
public static class Utils
{
    public const int CommandSize = 20;

    public const int MaxPacketSize = 4 + 4 + 128 * 36;

    /// <summary>
    /// 把消息结构序列化为网络字节
    /// </summary>
    public static byte[] SerializedPacket(PacketHeader header, object body)
    {
        Writer w = new Writer();
        w.WriteInt(header.type);
        switch ((PacketTypeS2C)header.type)
        {
            case PacketTypeS2C.Response:
                JoinPacket j = (JoinPacket)body;
                w.WriteInt(j.id);
                w.WriteInt(j.frame_number);
                w.WriteInt(j.version);
                break;

            case PacketTypeS2C.FrameS2C:
                FramePacketS2C f = (FramePacketS2C)body;
                w.WriteInt(f.frame_number);
                w.WriteInt(f.command_count);
                foreach (PlayerInputCommand cmd in f.commands)
                {
                    w.WriteInt(cmd.id);
                    w.WriteInt(cmd.command_type);
                    w.WriteFloat(cmd.x);
                    w.WriteFloat(cmd.y);
                    w.WriteFloat(cmd.z);
                }

                break;

            case PacketTypeS2C.InitWorld:
                List<EntitySpawn> spawns = (List<EntitySpawn>)body;
                w.WriteInt(spawns.Count);
                foreach (EntitySpawn s in spawns)
                {
                    w.WriteInt(s.EntityId);
                    w.WriteInt((int)s.Shape);
                    w.WriteFloat(s.X);
                    w.WriteFloat(s.Y);
                    w.WriteFloat(s.Z);
                    w.WriteFloat(s.SizeX);
                    w.WriteFloat(s.SizeY);
                    w.WriteFloat(s.SizeZ);
                    w.WriteInt(s.IsDynamic ? 1 : 0);
                }

                break;

            case PacketTypeS2C.HashError:
                HashErrorPacket he = (HashErrorPacket)body;
                w.WriteInt(he.frame_number);
                break;

            default:
                throw new ArgumentException($"序列化:未知包类型 {header.type}");
        }

        return w.ToBytes();
    }

    /// <summary>
    /// 把网络字节反序列化为消息
    /// </summary>
    public static (PacketHeader Header, object? Body) DeserializedPacket(byte[] data)
    {
        Reader r = new Reader(data);
        PacketHeader header = new PacketHeader { type = r.ReadInt() };
        if (Deserializer.TryGetValue((PacketTypeC2S)header.type, out Func<Reader, object?> deserialize))
        {
            return (header, deserialize(r));
        }

        throw new ArgumentException($"反序列化:未知包类型 {header.type}");
    }

    private static readonly Dictionary<PacketTypeC2S, Func<Reader, object?>> Deserializer = new()
    {
        [PacketTypeC2S.FrameC2S] = r => ReadFramePacketC2S(r),
    };

    /// <summary>
    /// 构造下发的指令集包
    /// </summary>
    public static FramePacketS2C BuildCommandSetPacket(int frame, List<PlayerInputCommand> commands)
    {
        return new FramePacketS2C()
        {
            packet_type = PacketTypeS2C.FrameS2C,
            frame_number = frame,
            command_count = commands.Count,
            commands = commands.ToArray(),
        };
    }


    private static FramePacketC2S ReadFramePacketC2S(Reader r)
    {
        return new FramePacketC2S
        {
            command = new PlayerInputCommand
            {
                id = r.ReadInt(),
                command_type = r.ReadInt(),
                x = r.ReadFloat(), y = r.ReadFloat(), z = r.ReadFloat(),
            },
            frame_number = r.ReadInt(),
            hash = r.ReadULong(),
        };
    }
}