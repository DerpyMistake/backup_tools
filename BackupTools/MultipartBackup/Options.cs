using System;
using System.IO;

namespace BitEffects.BackupTools.MultipartBackup
{
    public class Options : BitEffects.BackupTools.Options
    {
        const int DEFAULT_CHUNK_SIZE = 1024 * 1024 * 128;

        public int ChunkSize { get; set; } = DEFAULT_CHUNK_SIZE;
        public string Path { get; set; }
        public string InputFile { get; set; }

        override public bool Convert(string optionName, string[] optionValues)
        {
            if (optionName.Equals(nameof(ChunkSize)))
            {
                this.ChunkSize = optionValues[0].ToByteSize();
                if (this.ChunkSize <= 0)
                {
                    this.ChunkSize = DEFAULT_CHUNK_SIZE;
                }

                return true;
            }

            return base.Convert(optionName, optionValues);
        }
    }
}
