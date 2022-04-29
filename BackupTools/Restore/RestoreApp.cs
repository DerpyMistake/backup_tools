using Amazon.Glacier;
using Amazon.Glacier.Model;
using BitEffects.BackupTools.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.Restore
{
    public class RestoreApp
    {
        readonly BackupAppContext<Options> context;
        readonly GlacierHelper glacier;

        Options options => context.options;
        ILogger logger => context.logger;
        IDatabase database => context.database;

        GlacierConfig Config => options.AppConfig.GlacierConfig;
        AmazonGlacierClient Client => glacier.glacierClient;

        public RestoreApp(BackupAppContext<Options> context, GlacierHelper glacier)
        {
            this.context = context;
            this.glacier = glacier;
        }

        async public Task Run()
        {
            if (options.JobId.IsEmpty())
            {
                if (options.ArchiveName.IsEmpty())
                {
                    await RestoreAllArchives();
                }
                else
                {
                    await SubmitJobs(options.ArchiveName);
                    await ProcessJobs(options.ArchiveName);
                }
            }
            else
            {
                await DownloadArchive(options.JobId);
            }
        }

        async Task RestoreAllArchives()
        {
            const string LOG_CONTEXT = "Restore.RestoreAllArchives";

            logger.AddInfo(LOG_CONTEXT, "Restoring all archives.");
            string[] distinctNames = this.database
                .BackupEntries
                .DistinctBy(e => e.Metadata.Name.ToUpper())
                .Select(e => e.Metadata.Name)
                .ToArray();

            if (distinctNames.Length == 0)
            {
                logger.AddInfo(LOG_CONTEXT, "No archives found in the database. Fetching inventory from glacier.");
                distinctNames = (await glacier.FetchInventory(true))
                    .DistinctBy(e => e.Metadata.Name.ToUpper())
                    .Select(e => e.Metadata.Name)
                    .ToArray();
            }

            foreach (string archiveName in distinctNames)
            {
                await SubmitJobs(archiveName);
            }
            
            foreach (string archiveName in distinctNames)
            {
                await ProcessJobs(archiveName);
            }
        }

        Dictionary<string, List<string>> archiveJobs = new Dictionary<string, List<string>>();
        async Task SubmitJobs(string archiveName)
        {
            const string LOG_CONTEXT = "Restore.SubmitJobs";

            List<string> jobs = new List<string>();

            logger.AddInfo(LOG_CONTEXT, "Submitting archive retrieval requests to glacier.");

            // Get the current list of jobs so we can avoid restarting them
            var existingJobs = (await glacier.FetchJobs())
                .Where(j => !j.ArchiveId.IsEmpty())
                .Where(j => j.StatusCode != StatusCode.Failed)
                .OrderByDescending(j => j.CreationDate)
                .GroupBy(j => j.ArchiveId)
                .ToDictionary(g => g.Key, g => g.First());

            // Create a job to retrieve each of the backup sets
            var archives = await glacier.FindArchives(archiveName);
            foreach (var archiveId in archives)
            {
                if (existingJobs.TryGetValue(archiveId, out var job))
                {
                    jobs.Add(job.JobId);
                }
                else
                {
                    var jobParams = new JobParameters(null, "archive-retrieval", archiveId, "RestoreApp");
                    var jobReq = new InitiateJobRequest(Config.VaultName, jobParams);
                    var jobRes = await Client.InitiateJobAsync(jobReq);

                    jobs.Add(jobRes.JobId);
                }
            }

            archiveJobs[archiveName] = jobs;
        }

        async Task ProcessJobs(string archiveName)
        {
            const string LOG_CONTEXT = "Restore.SubmitJobs";
            logger.AddInfo(LOG_CONTEXT, $"Waiting for {archiveName} to be ready.");

            // Wait for each job to complete, in order, restoring as we go
            if (archiveJobs.TryGetValue(archiveName, out var jobs))
            {
                foreach (var jobId in jobs)
                {
                    while (await glacier.IsJobReady(jobId) == false)
                    {
                        await Task.Delay(TimeSpan.FromHours(1));
                    }

                    await LaunchRestoreProcess(jobId);
                }
            }
        }

        async Task DownloadArchive(string jobId)
        {
            const string LOG_CONTEXT = "Restore.DownloadArchive";
            logger.AddInfo(LOG_CONTEXT, $"Downloading and streaming job {jobId}.");

            var req = new GetJobOutputRequest(Config.VaultName, jobId, null);
            var res = await glacier.glacierClient.GetJobOutputAsync(req);
            if (res.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                using (var stdout = Console.OpenStandardOutput())
                {
                    await res.Body.CopyToAsync(stdout);
                }
            }
        }

        Task LaunchRestoreProcess(string jobId)
        {
            const string LOG_CONTEXT = "Restore.DownloadArchive";
            logger.AddInfo(LOG_CONTEXT, $"Executing the restore command for {jobId}.");

            // Launch the process to download and install the related archive
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = options.Command,
                    Arguments = options.CommandArguments
                        .Replace("$ARCHIVE_NAME", options.ArchiveName)
                        .Replace("$GLACIER_JOB_ID", jobId)
                }
            };
            process.Start();
            process.WaitForExit();

            return Task.CompletedTask;
        }
    }
}
