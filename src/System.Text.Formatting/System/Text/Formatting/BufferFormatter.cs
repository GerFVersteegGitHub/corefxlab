﻿using System.Buffers;
using System.Text;

namespace System.Text.Formatting
{
    public class BufferFormatter : IFormatter
    {
        byte[] _buffer;
        int _count;

        EncodingData _encoding;
        ArrayPool<byte> _pool;

        public BufferFormatter(int capacity, EncodingData encoding, ArrayPool<byte> pool = null)
        {
            _encoding = encoding;
            _count = 0;
            _pool = pool;
            if(_pool == null)
            {
                _pool = ArrayPool<byte>.Shared;
            }
            _buffer = _pool.Rent(capacity);
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }
        public int CommitedByteCount
        {
            get { return _count; }
        }

        public void Clear()
        {
            _count = 0;
        }

        Span<byte> IFormatter.FreeBuffer
        {
            get
            {
                return new Span<byte>(_buffer, _count, _buffer.Length - _count);
            }
        }

        EncodingData IFormatter.Encoding
        {
            get
            {
                return _encoding;
            }
        }

        void IFormatter.ResizeBuffer(int desiredFreeBytesHint)
        {
            var newSize = desiredFreeBytesHint + _buffer.Length - _count;
            if(desiredFreeBytesHint == -1){
                newSize = _buffer.Length * 2;
            }

            var temp = _buffer;
            _buffer = _pool.Rent(newSize);
            Array.Copy(temp, 0, _buffer, 0, _count);
            _pool.Return(temp);
        }

        void IFormatter.CommitBytes(int bytes)
        {
            _count += bytes;
            if(_count > _buffer.Length)
            {
                throw new InvalidOperationException("More bytes commited than returned from FreeBuffer");
            }
        }
    }
}
