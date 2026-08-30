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

    public const int MaxPacketSize = 4 + 4 + 128 * 28;

    /// <summary>
    /// 把消息结构序列化为网络字节
    /// </summary>
    public static byte[] SerializedPacket(PacketHeader header, object body)
    {
        Writer w = new Writer();
        w.WriteInt(header.type);
        switch ((PacketType)header.type)
        {
            case PacketType.Response:
                JoinPacket j = (JoinPacket)body;
                w.WriteInt(j.id);
                w.WriteInt(j.frame_number);
                break;

            case PacketType.Command:
                PlayerInputCommand c = (PlayerInputCommand)body;
                w.WriteInt(c.id);
                w.WriteInt(c.command_type);
                w.WriteFloat(c.x);
                w.WriteFloat(c.y);
                w.WriteFloat(c.z);
                break;

            case PacketType.CommandSet:
                FramePacket f = (FramePacket)body;
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

            case PacketType.InitWorld:
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
        if (Deserializer.TryGetValue((PacketType)header.type, out Func<Reader, object?> deserialize))
        {
            return (header, deserialize(r));
        }

        throw new ArgumentException($"反序列化:未知包类型 {header.type}");
    }

    /// <summary>
    /// 构造下发的指令集包
    /// </summary>
    public static FramePacket BuildCommandSetPacket(int frame, List<PlayerInputCommand> commands)
    {
        return new FramePacket()
        {
            packet_type = PacketType.CommandSet,
            frame_number = frame,
            command_count = commands.Count,
            commands = commands.ToArray(),
        };
    }

    /// <summary>
    /// 解析指令集
    /// </summary>
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
                x = r.ReadFloat(), y = r.ReadFloat(), z = r.ReadFloat(),
            };
        }

        fp.commands = cmds;
        return fp;
    }

    private static readonly Dictionary<PacketType, Func<Reader, object?>> Deserializer = new()
    {
        [PacketType.Response] = r => new JoinPacket { id = r.ReadInt(), frame_number = r.ReadInt() },
        [PacketType.Command] = r => new PlayerInputCommand
        {
            id = r.ReadInt(), command_type = r.ReadInt(),
            x = r.ReadFloat(), y = r.ReadFloat(), z = r.ReadFloat(),
        },
        [PacketType.CommandSet] = r => ReadFramePacket(r),
    };
}
