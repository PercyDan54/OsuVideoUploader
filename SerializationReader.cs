// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Text;

namespace OsuVideoUploader
{
    /// <summary> SerializationReader.  Extends BinaryReader to add additional data types,
    /// handle null strings and simplify use with ISerializable. </summary>
    public class SerializationReader : BinaryReader
    {
        private readonly Stream stream;

        public SerializationReader(Stream s)
            : base(s, Encoding.UTF8)
        {
            stream = s;
        }

        public int RemainingBytes => (int)(stream.Length - stream.Position);

        /// <summary> Reads a string from the buffer.  Overrides the base implementation so it can cope with nulls. </summary>
        public override string ReadString()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            if (ReadByte() == 0) return null;

            return base.ReadString();
        }

        /// <summary> Reads a byte array from the buffer, handling nulls and the array length. </summary>
        public byte[] ReadByteArray()
        {
            int len = ReadInt32();
            if (len > 0) return ReadBytes(len);
            if (len < 0) return null;

            return Array.Empty<byte>();
        }

        /// <summary> Reads a char array from the buffer, handling nulls and the array length. </summary>
        public char[] ReadCharArray()
        {
            int len = ReadInt32();
            if (len > 0) return ReadChars(len);
            if (len < 0) return null;

            return Array.Empty<char>();
        }

        /// <summary> Reads a DateTime from the buffer. </summary>
        public DateTime ReadDateTime()
        {
            long ticks = ReadInt64();
            if (ticks < 0) throw new IOException("Bad ticks count read!");

            return new DateTime(ticks, DateTimeKind.Utc);
        }
    }
}
