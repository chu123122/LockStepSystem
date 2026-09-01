using System;

namespace LockStepServer;

internal struct Writer
{
    private byte[] _bytes;
    private int _offset;

    public Writer() : this(512)
    {
    }

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
        byte[] dst = new byte[_offset];
        Array.Copy(_bytes, dst, _offset);
        return dst;
    }
}

internal struct Reader
{
    private readonly byte[] _data;
    private int _offset;

    public Reader(byte[] data)
    {
        _data = data;
        _offset = 0;
    }

    public ulong ReadULong()
    {
        ulong value = BitConverter.ToUInt64(_data, _offset);
        _offset += 8;
        return value;
    }

    public int ReadInt()
    {
        if (_offset + 4 > _data.Length)
            throw new ArgumentException($"数据不足 @ 偏移 {_offset}");
        int value = BitConverter.ToInt32(_data, _offset);
        _offset += 4;
        return value;
    }

    public float ReadFloat()
    {
        float value = BitConverter.ToSingle(_data, _offset);
        _offset += 4;
        return value;
    }
}
