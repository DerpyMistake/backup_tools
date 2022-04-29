using System;

namespace BitEffects
{
    /// <summary>
    /// Create a FIFO buffer for bytes.
    /// </summary>
    public class ByteBuffer
    {
        byte[] bytes;
        public int Length { get; private set; } = 0;

        /// <summary>
        /// Create a buffer for collecting bytes
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the buffer</param>
        public ByteBuffer(int initialCapacity = 1024 * 1024)
        {
            this.bytes = new byte[initialCapacity];
        }

        /// <summary>
        /// Copy an array of bytes to the buffer
        /// </summary>
        /// <param name="data">The source array</param>
        /// <param name="size">The number of bytes to copy from the source array</param>
        public void Write(byte[] data, int size)
        {
            size = Math.Min(data.Length, size);
            while (bytes.Length < Length + size)
            {
                byte[] newBytes = new byte[bytes.Length * 2];
                bytes.CopyTo(newBytes, 0);
                bytes = newBytes;
            }

            for (int i = 0; i < size; ++i)
            {
                bytes[Length + i] = data[i];
            }
            Length += size;
        }

        /// <summary>
        /// Read some bytes from the buffer and adjust the pointer.
        /// </summary>
        /// <param name="size">The number of bytes to read</param>
        /// <returns></returns>
        public byte[] Read(int size)
        {
            if (this.Length == 0)
            {
                return new byte[0];
            }

            byte[] res = new byte[Math.Min(Length, size)];
            if (res.Length > 0)
            {
                // Copy the bytes to the result array
                bytes.AsSpan(0, res.Length).CopyTo(res.AsSpan());

                // Shift all of the bytes to the left
                bytes.AsSpan(res.Length, this.Length - res.Length).CopyTo(bytes.AsSpan());
                this.Length -= res.Length;
            }
            return res;
        }
    }
}
