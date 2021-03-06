// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text.Utf8;

namespace System.Buffers.Writer
{
    public ref partial struct BufferWriter<T> where T : IBufferWriter<byte>
    {
        public void Write(string value)
        {
            ReadOnlySpan<byte> utf16Bytes = value.AsSpan().AsBytes();
            int totalConsumed = 0;
            while (true)
            {
                var status = Encodings.Utf16.ToUtf8(utf16Bytes.Slice(totalConsumed), Buffer, out int consumed, out int written);
                switch (status)
                {
                    case OperationStatus.Done:
                        Advance(written);
                        return;
                    case OperationStatus.DestinationTooSmall:
                        Advance(written);
                        Enlarge();
                        break;
                    case OperationStatus.NeedMoreData:
                    case OperationStatus.InvalidData:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
                totalConsumed += consumed;
            }
        }

        // TODO: implement all the overloads taking TransformationFormat
        //public void Write(string value, TransformationFormat format);

        public void WriteLine(string value)
        {
            Write(value);
            Write(NewLine);
        }

        //public void WriteLine(string value, TransformationFormat format);

        public void Write(Utf8String value) => Write(value.Bytes);

        //public void Write(Utf8String value, TransformationFormat format);

        public void WriteLine(Utf8String value)
        {
            Write(value.Bytes);
            Write(NewLine);
        }

        //public void WriteLine(Utf8String value, TransformationFormat format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> value)
        {
            if (_span.Length >= value.Length)
            {
                value.CopyTo(_span);
                Advance(value.Length);
            }
            else
            {
                WriteMultiBuffer(value);
            }
        }

        //public void Write(ReadOnlySpan<byte> value, TransformationFormat format)

        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (_span.Length == 0)
                {
                    EnsureMore();
                }

                var writable = Math.Min(source.Length, _span.Length);
                source.Slice(0, writable).CopyTo(_span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }
    }
}
