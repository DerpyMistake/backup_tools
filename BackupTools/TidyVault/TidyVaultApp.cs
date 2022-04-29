using Amazon.Glacier.Model;
using BitEffects.BackupTools.DB;
using System.Linq;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.TidyVault
{
    public class TidyVaultApp
    {
        readonly BackupAppContext<Options> context;
        readonly GlacierHelper glacier;

        GlacierConfig Config => options.AppConfig.GlacierConfig;
        Options options => context.options;
        ILogger logger => context.logger;
        IDatabase database => context.database;

        public TidyVaultApp(BackupAppContext<Options> context, GlacierHelper glacier)
        {
            this.context = context;
            this.glacier = glacier;
        }

        async public Task Run()
        {
            await AbortPendingUploads();
            await RemoveAbandonedArchives();
        }

        async Task AbortPendingUploads()
        {
            const string LOG_CONTEXT = "TidyVault.AbortPendingUploads";
            logger.AddInfo(LOG_CONTEXT, "Aborting all pending uploads to glacier.");

            var uploadsList = await glacier.FetchPendingUploads();
            foreach (var multipartUploadId in uploadsList)
            {
                var req = new AbortMultipartUploadRequest(Config.VaultName, multipartUploadId);
                _ = await glacier.glacierClient.AbortMultipartUploadAsync(req);
            }
        }

        /// <summary>
        /// Removes any archives of the same name created before the first primary archive.
        /// </summary>
        /// <returns></returns>
        async Task RemoveAbandonedArchives()
        {
            const string LOG_CONTEXT = "TidyVault.RemoveAbandonedArchives";
            logger.AddInfo(LOG_CONTEXT, "Fetching most recent inventory.");

            var inventory = await glacier.FetchInventory();
            var items = inventory
                .GroupBy(a => a.Metadata.Name.ToUpper())
                .ToDictionary(g => g.Key, g => g.ToArray());

            foreach (var kv in items)
            {
                logger.AddInfo(LOG_CONTEXT, $"Deleting all archives before the last full upload for {kv.Key}.");

                // Keep all archives down to and including the most recent base archive.
                var archivesToDelete = kv.Value
                    .OrderByDescending(a => a.CreationDate)
                    .SkipWhile(a => !a.Metadata.Tags.Contains(ArchiveTags.BaseArchive))
                    .Skip(1)
                    .ToArray();

                foreach (var arch in archivesToDelete)
                {
                    await glacier.DeleteArchive(arch.ArchiveId);
                }
            }
        }
    }
}
