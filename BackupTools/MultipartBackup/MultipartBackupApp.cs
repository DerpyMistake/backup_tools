using Amazon.Glacier;
using Amazon.Glacier.Model;
using BitEffects.BackupTools.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.MultipartBackup
{
    public class MultipartBackupApp
    {
        readonly BackupAppContext<Options> context;
        readonly IGlacierBackup glacierBackup;
        
        Options options => context.options;
        ILogger logger => context.logger;
        IDatabase database => context.database;

        public MultipartBackupApp(BackupAppContext<Options> context, IGlacierBackup glacierBackup)
        {
            this.context = context;
            this.glacierBackup = glacierBackup;

            ValidateOptions();
        }

        private void ValidateOptions()
        {
            if (options.ChunkSize <= 0)
            {
                throw new Exception($"Invalid Chunk Size: {options.ChunkSize}");
            }
            
            if(options.Path.IsEmpty())
            {
                throw new Exception($"Missing Parameter: --path");
            }
        }

        async public Task Run()
        {
            const string LOG_CONTEXT = "MultipartBackup";

            if (!options.InputFile.IsEmpty())
            {
                this.logger.AddInfo(LOG_CONTEXT, $"Streaming from an input file: {options.InputFile}");
                using (var stream = File.Open(options.InputFile, FileMode.Open, FileAccess.Read))
                {
                    await PerformBackup(stream);
                }
            }
            else if (Console.IsInputRedirected)
            {
                this.logger.AddInfo(LOG_CONTEXT, "Streaming from standard input");
                using (var stream = Console.OpenStandardInput())
                {
                    await PerformBackup(stream);
                }
            }
        }

        async Task PerformBackup(Stream stream)
        {
            const string LOG_CONTEXT = "MultipartBackup.PerformBackup";

            var splitter = new StreamSplitter(options.ChunkSize, ProcessData);

            this.logger.AddInfo(LOG_CONTEXT, $"Creating the multipart upload on Glacier - chunkSize={options.ChunkSize}");
            string uploadId = await glacierBackup.InitiateMultipartUpload();
            if (!uploadId.IsEmpty())
            {
                await splitter.Run(stream);

                this.logger.AddInfo(LOG_CONTEXT, "Completing the multipart upload");
                string archiveId = await glacierBackup.CompleteMultipartUpload();

                this.logger.AddInfo(LOG_CONTEXT, "Saving the entry to the database");
                {
                    var entry = new BackupEntry(archiveId, options.Path);
                    bool hasExistingEntry = database.BackupEntries
                        .Any(e => e.Metadata.Path.EqualsCI(options.Path));

                    if (!hasExistingEntry)
                    {
                        this.logger.AddInfo(LOG_CONTEXT, "Adding a new base backup");
                        entry.Metadata.Tags.Add(ArchiveTags.BaseArchive);
                    }
                    else
                    {
                        this.logger.AddInfo(LOG_CONTEXT, "Adding a snapshot to an existing backup");
                        entry.Metadata.Tags.Add(ArchiveTags.Snapshot);
                    }

                    database.BackupEntries.Add(entry);
                }
            }
            else
            {
                throw new Exception("Unable to find the Upload ID");
            }
        }

        async Task ProcessData(byte[] buffer, int fileIndex)
        {
            const string LOG_CONTEXT = "MultipartBackup.ProcessData";

            if (buffer.Length > 0)
            {
                using (var ms = new MemoryStream(buffer))
                {
                    this.logger.AddInfo(LOG_CONTEXT, $"Uploading part {fileIndex} to glacier");
                    await glacierBackup.UploadPart(ms);
                }
            }
        }
    }
}
