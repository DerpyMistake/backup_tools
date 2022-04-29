using System;
using System.IO;
using System.Threading.Tasks;

namespace BitEffects
{
    /// <summary>
    /// Split a binary stream into multiple parts and send each one to a callback
    /// </summary>
    public class StreamSplitter
    {
        readonly int chunkSize;
        int fileIndex = 0;

        public delegate void ProcessDataDelegate(byte[] data, int index);
        public delegate Task ProcessDataAsyncDelegate(byte[] data, int index);
        readonly ProcessDataAsyncDelegate processDataAsync = null;

        public StreamSplitter(int chunkSize, ProcessDataDelegate processData)
        {
            this.chunkSize = chunkSize;
            this.processDataAsync = (data, index) =>
            {
                processData(data, index);
                return Task.CompletedTask;
            };
        }

        public StreamSplitter(int chunkSize, ProcessDataAsyncDelegate processDataAsync)
        {
            this.chunkSize = chunkSize;
            this.processDataAsync = processDataAsync;
        }

        async public Task Run(Stream stream)
        {
            ByteBuffer data = new ByteBuffer();
            byte[] buffer = new byte[1024 * 16];
            while (stream.CanRead)
            {
                int len = stream.Read(buffer, 0, buffer.Length);

                if (len > 0)
                {
                    data.Write(buffer, len);
                    while (data.Length > chunkSize)
                    {
                        await this.processDataAsync(data.Read(chunkSize), fileIndex++);
                    }
                }
                else
                {
                    break;
                }
            }

            await this.processDataAsync(data.Read(chunkSize), fileIndex++);
        }
    }
}
