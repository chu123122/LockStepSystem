using System;
using LockStepServer.Protocol;

namespace LockStepServer;

public static class Utils
{
    public const int CommandSize = 20;

    public const int MaxPacketSize = 4 + 4 + 4 + 10 * CommandSize;

    public static byte[] SerializedPacket(PacketHeader header, object body)
    {
        var w = new Writer();
        w.WriteInt(header.packet_type);              
        switch ((PacketType)header.packet_type)
        {
            case PacketType.Response:                
                var j = (JoinPacket)body;
                w.WriteInt(j.id);
                w.WriteInt(j.frame_number);
                break;

            case PacketType.Command:                 
                var c = (PlayerInputCommand)body;
                w.WriteInt(c.id);
                w.WriteInt(c.command_type);
                w.WriteFloat(c.x); w.WriteFloat(c.y); w.WriteFloat(c.z);
                break;

            case PacketType.CommandSet:             
                var f = (FramePacket)body;
                w.WriteInt(f.frame_number);
                w.WriteInt(f.command_count);
                foreach (var cmd in f.commands)      
                {
                    w.WriteInt(cmd.id);
                    w.WriteInt(cmd.command_type);
                    w.WriteFloat(cmd.x); w.WriteFloat(cmd.y); w.WriteFloat(cmd.z);
                }
                break;

            default:
                throw new ArgumentException($"序列化:未知包类型 {header.packet_type}");
        }
        return w.ToBytes();
    }

    // 反序列化分派注册表:包类型 → 反序列化函数(与 SerializedPacket 的逻辑一一对应)
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

    public static (PacketHeader Header, object? Body) DeserializedPacket(byte[] data)
    {
        var r = new Reader(data);
        var header = new PacketHeader { packet_type = r.ReadInt() };
        if (Deserializer.TryGetValue((PacketType)header.packet_type, out var deserialize))
        {
            return (header, deserialize(r));
        }
        throw new ArgumentException($"反序列化:未知包类型 {header.packet_type}");
    }

    private static FramePacket ReadFramePacket(Reader r)
    {
        var fp = new FramePacket { frame_number = r.ReadInt(), command_count = r.ReadInt() };
        var cmds = new PlayerInputCommand[10];
        for (var i = 0; i < 10; i++)
        {
            cmds[i] = new PlayerInputCommand
            {
                id = r.ReadInt(), command_type = r.ReadInt(),
                x = r.ReadFloat(), y = r.ReadFloat(), z = r.ReadFloat(),
            };
        }
        fp.commands = cmds;
        return fp;
    }

    private struct Writer
    {
        private byte[] _bytes;
        private int _offset;

        // 注意:struct 的 new Writer() 只认"显式无参构造";只有"带默认参数的构造"时,new Writer() 会被当作 default 跳过构造(实测字段不初始化)。
        public Writer() : this(512) { }
        public Writer(int capacity)
        {
            _bytes = new byte[capacity];
            _offset = 0;
        }

        public void WriteInt(int value) => Write(BitConverter.GetBytes(value));
        public void WriteFloat(float value) => Write(BitConverter.GetBytes(value));

        private void Write(byte[] data)
        {
            if (_offset + data.Length > _bytes.Length)
                Array.Resize(ref _bytes, _bytes.Length * 2);
            Array.Copy(data, 0, _bytes, _offset, data.Length);
            _offset += data.Length;
        }

        public byte[] ToBytes()
        {
            var dst = new byte[_offset];
            Array.Copy(_bytes, dst, _offset);
            return dst;
        }
    }

    private struct Reader
    {
        private readonly byte[] _data;
        private int _offset;

        public Reader(byte[] data)
        {
            _data = data;
            _offset = 0;
        }

        public int ReadInt()
        {
            if (_offset + 4 > _data.Length)
                throw new ArgumentException($"数据不足 @ 偏移 {_offset}");
            var value = BitConverter.ToInt32(_data, _offset);
            _offset += 4;
            return value;
        }

        public float ReadFloat()
        {
            var value = BitConverter.ToSingle(_data, _offset);
            _offset += 4;
            return value;
        }
    }
}
