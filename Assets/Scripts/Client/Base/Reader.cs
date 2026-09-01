using System;

namespace Client
{
    public struct Reader
    {
        private readonly byte[] _data;
        private int _offset;

        public Reader(byte[] data, int offset = 0)
        {
            _data = data;
            _offset = offset;
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

        public ulong ReadULong()
        {
            ulong value = BitConverter.ToUInt64(_data, _offset);
            _offset += 8;
            return value;
        }
    }
}
