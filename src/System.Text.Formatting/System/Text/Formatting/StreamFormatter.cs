﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Buffers;
using System.Text;

namespace System.Text.Formatting
{
    public struct StreamFormatter : IFormatter, IDisposable
    {
        Stream _stream;
        EncodingData _encoding;
        byte[] _buffer;
        ArrayPool<byte> _pool;

        public StreamFormatter(Stream stream, ArrayPool<byte> pool) : this(stream, EncodingData.InvariantUtf16, pool)
        {
        }

        public StreamFormatter(Stream stream, EncodingData encoding, ArrayPool<byte> pool, int bufferSize = 256)
        {
            _pool = pool;
            _buffer = null;
            if (bufferSize > 0)
            {
                _buffer = _pool.Rent(bufferSize);
            }
            _encoding = encoding;
            _stream = stream;
        }

        Span<byte> IFormatter.FreeBuffer
        {
            get
            {
                if (_buffer == null)
                {
                    _buffer = _pool.Rent(256);
                }
                return new Span<byte>(_buffer);
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
            var newSize = _buffer.Length * 2;
            if(desiredFreeBytesHint != -1){
                newSize = desiredFreeBytesHint;
            }
            var temp = _buffer;
            _buffer = _pool.Rent(newSize);
            _pool.Return(temp);
        }

        // ISSUE
        // I would like to lazy write to the stream, but unfortunatelly this seems to be exclusive with this type being a struct. 
        // If the write was lazy, passing this struct by value could result in data loss.
        // A stack frame could write more data to the buffer, and then when the frame pops, the infroamtion about how much was written could be lost. 
        // On the other hand, I cannot make this type a class and keep using it as it can be used today (i.e. pass streams around and create instances of this type on demand).
        // Too bad we don't support move semantics and stack only structs.
        void IFormatter.CommitBytes(int bytes)
        {
            _stream.Write(_buffer, 0, bytes);
        }

        /// <summary>
        /// Returns buffers to the pool
        /// </summary>
        public void Dispose()
        {
            _pool.Return(_buffer);
            _buffer = null;
            _stream = null;
        }
    }
}
