using Amazon.Glacier;
using Amazon.Glacier.Model;
using BitEffects.BackupTools.DB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BitEffects.BackupTools
{
    public class GlacierHelper
    {
        readonly Options options;
        readonly IDatabase database;
        readonly public AmazonGlacierClient glacierClient;

        GlacierConfig Config => options.AppConfig.GlacierConfig;

        public GlacierHelper(Options options, IDatabase database, AmazonGlacierClient glacierClient)
        {
            this.options = options;
            this.database = database;
            this.glacierClient = glacierClient;
        }

        public async Task<string[]> FetchPendingUploads()
        {
            var req = new ListMultipartUploadsRequest(Config.VaultName);
            var res = await glacierClient.ListMultipartUploadsAsync(req);

            return res.UploadsList
                .Select(u => u.MultipartUploadId)
                .ToArray();
        }

        public async Task<BackupEntry[]> FetchInventory(bool waitForReady = true)
        {
            string jobId = null;

            // Get the current list of jobs so we can avoid restarting it
            {
                var jobs = (await FetchJobs())
                    .Where(j => j.InventoryRetrievalParameters != null)
                    .Where(j => j.StatusCode != StatusCode.Failed)
                    .OrderByDescending(j => j.CreationDate)
                    .ToArray();

                var firstJob = jobs.FirstOrDefault();
                var completedJob = jobs.FirstOrDefault(j => j.StatusCode == StatusCode.Succeeded);

                jobId = firstJob?.JobId;

                // If there is no job, start a new job and use its ID
                if (firstJob == null)
                {
                    jobId = await StartInventoryJob();
                }
                else if (firstJob.StatusCode == StatusCode.Succeeded)
                {
                    // If the first job is complete and is too old, start a new job
                    if (firstJob.CreationDate.AddDays(1) < DateTime.UtcNow.Date)
                    {
                        _ = await StartInventoryJob();
                    }
                }
                // If the first job is not complete, use the first completed job
                else if (completedJob != null)
                {
                    jobId = completedJob.JobId;
                }
            }

            // Wait for the job to complete
            if (waitForReady)
            {
                while (await IsJobReady(jobId) == false)
                {
                    await Task.Delay(TimeSpan.FromMinutes(15));
                }
            }

            // Once the job is ready, grab the inventory list
            if (await IsJobReady(jobId))
            {
                var req = new GetJobOutputRequest(Config.VaultName, jobId, null);
                var res = await glacierClient.GetJobOutputAsync(req);

                using (var reader = new StreamReader(res.Body))
                {
                    var inventory = reader.ReadToEnd().Deserialize<VaultInventory>();
                    if (inventory != null)
                    {
                        // Sync our local database
                        this.database.BackupEntries.Clear();
                        this.database.BackupEntries.AddRange(
                            inventory.ArchiveList
                            .OrderBy(a => a.CreationDate)
                            .Select(a => new BackupEntry(a.ArchiveId, a.Metadata.Path)
                            {
                                Metadata = a.Metadata,
                                CreationDate = a.CreationDate
                            })
                        );
                    }

                    return this.database.BackupEntries.ToArray();
                }
            }
            else
            {
                return this.database.BackupEntries.ToArray();
            }
        }

        private async Task<string> StartInventoryJob()
        {
            var jobParams = new JobParameters("JSON", "inventory-retrieval", null, "BitEffects Inventory");

            var req = new InitiateJobRequest(Config.VaultName, jobParams);
            var res = await glacierClient.InitiateJobAsync(req);

            return res.JobId;
        }

        public async Task<List<GlacierJobDescription>> FetchJobs()
        {
            var req = new ListJobsRequest(Config.VaultName);
            var res = await glacierClient.ListJobsAsync(req);

            return res.JobList;
        }

        public async Task<string[]> FindArchives(string archiveName)
        {
            var entries = database.BackupEntries
                .Where(e => e.Metadata.Name.EqualsCI(archiveName))
                .OrderBy(e => e.Metadata.Created)
                .ToArray();

            if (entries.Length > 0 && entries.All(e => !e.ArchiveId.IsEmpty()))
            {
                return entries.Select(e => e.ArchiveId).ToArray();
            }
            else
            {
                var inventory = await FetchInventory();

                return inventory
                    .Where(a => a.Metadata.Name.EqualsCI(archiveName))
                    .OrderBy(a => a.CreationDate)
                    .Select(a => a.ArchiveId)
                    .ToArray();
            }
        }

        public async Task<bool> IsJobReady(string jobId)
        {
            var req = new DescribeJobRequest(Config.VaultName, jobId);
            var res = await glacierClient.DescribeJobAsync(req);

            if (res.StatusCode == StatusCode.Failed)
            {
                throw new Exception($"Job {jobId} Failed.");
            }

            return res.StatusCode != StatusCode.InProgress;
        }

        public async Task DeleteArchive(string archiveId)
        {
            var req = new DeleteArchiveRequest(Config.VaultName, archiveId);
            _ = await glacierClient.DeleteArchiveAsync(req);
        }
    }

    static public class GlacierHelperExtensions{
        static public IServiceCollection AddGlacierClient(this IServiceCollection services)
        {
            services.AddScoped<AmazonGlacierClient>(provider =>
            {
                var options = provider.GetRequiredService<Options>();

                var clConfig = new AmazonGlacierConfig()
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.AppConfig.GlacierConfig.Region)
                };

                return new AmazonGlacierClient(
                    options.AppConfig.GlacierConfig.AccessKeyID,
                    options.AppConfig.GlacierConfig.SecretAccessKey,
                    clConfig
                );
            });

            services.AddScoped<GlacierHelper>(provider => ActivatorUtilities.CreateInstance<GlacierHelper>(provider));

            return services;
        }
    }
}
