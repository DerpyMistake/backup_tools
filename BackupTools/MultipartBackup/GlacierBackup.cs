using Amazon.Glacier;
using Amazon.Glacier.Model;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.MultipartBackup
{
    class GlacierBackup : IGlacierBackup
    {
        readonly Options options;
        readonly AmazonGlacierClient glacierClient;

        List<string> treeHashes = new List<string>();
        string uploadId;
        int fileIndex = 0;
        long totalSize = 0;

        public GlacierBackup(Options options)
        {
            this.options = options;

            var clConfig = new AmazonGlacierConfig()
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.AppConfig.GlacierConfig.Region)
            };

            glacierClient = new AmazonGlacierClient(
                options.AppConfig.GlacierConfig.AccessKeyID,
                options.AppConfig.GlacierConfig.SecretAccessKey,
                clConfig
            );
        }

        async Task<string> IGlacierBackup.InitiateMultipartUpload()
        {
            var archiveMetaData = new ArchiveMetaData(options.Path);
            var req = new InitiateMultipartUploadRequest(
                options.AppConfig.GlacierConfig.VaultName,
                archiveMetaData.ToDescription(),
                options.ChunkSize
            );

            var res = await glacierClient.InitiateMultipartUploadAsync(req);
            uploadId = res.UploadId;

            return res.UploadId;
        }

        async Task<string> IGlacierBackup.CompleteMultipartUpload()
        {
            var req = new CompleteMultipartUploadRequest(
                options.AppConfig.GlacierConfig.VaultName,
                uploadId,
                totalSize.ToString(),
                TreeHashGenerator.CalculateTreeHash(treeHashes)
            );

            var res = await glacierClient.CompleteMultipartUploadAsync(req);

            return res.ArchiveId;
        }

        async Task IGlacierBackup.UploadPart(Stream stream)
        {
            totalSize += stream.Length;

            string checksum = TreeHashGenerator.CalculateTreeHash(stream);
            treeHashes.Add(checksum);

            int startIdx = fileIndex * options.ChunkSize;
            string range = $"bytes {startIdx}-{totalSize - 1}/*";
            var req = new UploadMultipartPartRequest(
                options.AppConfig.GlacierConfig.VaultName,
                uploadId,
                checksum,
                range,
                stream
            );

            fileIndex++;
            await glacierClient.UploadMultipartPartAsync(req);
        }
    }
}
