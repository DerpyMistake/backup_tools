using System;
using System.IO;
using System.Linq;

namespace BitEffects.BackupTools.Restore
{
    public class Options : BitEffects.BackupTools.Options
    {
        const int DEFAULT_CHUNK_SIZE = 1024 * 1024 * 100;

        public int ChunkSize { get; set; } = DEFAULT_CHUNK_SIZE;
        
        [Alias("a")]
        [Alias("archive")]
        public string ArchiveName { get; set; }

        [Alias("cmd")]
        public string Command { get; set; } = "/bin/bash";

        [Alias("args")]
        public string CommandArguments { get; set; } = "./restore.sh $JOB_ID $OUTPUT_FOLDER";

        [Alias("job")]
        public string JobId { get; set; }

        [Alias("out")]
        public string OutputFolder { get; set; }

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
            else if (optionName.Equals(nameof(Command)))
            {
                this.Command = optionValues[0];
                this.CommandArguments = string.Join(" ", optionValues.Skip(1));

                return true;
            }

            return base.Convert(optionName, optionValues);
        }
    }
}
