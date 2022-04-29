using BitEffects.BackupTools.DB;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.Sync
{
    public class SyncApp
    {
        const string SNAPSHOT_FILE_NAME = "snapshot.gz";
        const string SNAPSHOT_FOLDER = "snapshot";

        readonly Options options;
        readonly ILogger logger;

        public SyncApp(Options options, ILogger logger)
        {
            this.options = options;
            this.logger = logger;

            ValidateOptions();
        }

        private void ValidateOptions()
        {
            if (options.ArchivePath.IsEmpty())
            {
                throw new Exception("Missing Parameter: --path");
            }

            if (options.Backup)
            {
                if (options.Snapshot.IsEmpty())
                {
                    throw new Exception("Missing Parameter: --snapshot");
                }
            }
            else if (options.Restore)
            {

            }
            else
            {
                throw new Exception("Missing Parameter: At least one of -c or -x must be specified.");
            }
        }

        async public Task Run()
        {
            if (options.Backup)
            {
                logger.AddInfo("SyncApp", "Creating a new Sync archive.");
                await Backup();
            }
            else if (options.Restore)
            {
                logger.AddInfo("SyncApp", "Extracting a new Sync archive.");
                await Restore();
            }
        }

        async Task Backup()
        {
            const string LOG_CONTEXT = "SynceApp.Backup";

            logger.AddInfo(LOG_CONTEXT, "Loading the snapshot collection.");
            var snapshotCollection = await SnapshotCollection.Load(options.Snapshot, options.ArchivePath);
            var baseSnapshot = snapshotCollection.BuildSnapshot(DateTime.UtcNow);
            var dirInfo = new DirectoryInfo(options.ArchivePath);

            logger.AddInfo(LOG_CONTEXT, "Loading the file tree.");
            var newSnapshot = new Snapshot()
            {
                Entries = Directory.EnumerateFiles(options.ArchivePath, "*", SearchOption.AllDirectories)
                    .Select(p => new FileInfo(p))
                    .Select(fi => new Snapshot.Entry(fi, dirInfo.FullName))
                    .ToList()
            };

            logger.AddInfo(LOG_CONTEXT, "Computing the differences.");
            Snapshot diff = baseSnapshot.Syncronize(newSnapshot).Trim();
            if (diff.Entries.Count > 0)
            {
                snapshotCollection.Snapshots.Add(diff);
                await snapshotCollection.Save(options.Snapshot);

                using Stream stream = options.OutputFile.IsEmpty()
                    ? Console.OpenStandardOutput()
                    : new FileStream(options.OutputFile, FileMode.Create, FileAccess.Write);

                logger.AddInfo(LOG_CONTEXT, $"Creating the archive for {dirInfo.FullName}.");
                using (TarArchive arch = TarArchive.CreateOutputTarArchive(stream, Encoding.UTF8))
                {
                    arch.RootPath = dirInfo.FullName;

                    // First, write the snapshot file
                    {
                        var ent = TarEntry.CreateEntryFromFile(options.Snapshot);
                        ent.TarHeader.Name = SNAPSHOT_FILE_NAME;
                        arch.WriteEntry(ent, false);
                    }

                    string inputPath = new DirectoryInfo(options.ArchivePath ?? "./").FullName;
                    string outputFile = options.OutputFile.IsEmpty()
                        ? null
                        : new FileInfo(options.OutputFile).FullName;
                    
                    // Then, add all the files to the snapshot folder
                    foreach (var entry in diff.Entries.Where(e => e.Action == Snapshot.EntryAction.Add))
                    {
                        try
                        {
                            string entryPath = Path.Combine(inputPath, entry.Path);
                            FileInfo fi = new FileInfo(entryPath);

                            if (!options.OutputFile.IsEmpty() && fi.FullName == outputFile)
                            {
                                continue;
                            }

                            // Attempt to open the file
                            using (var tmpStream = fi.OpenRead()) { }

                            var ent = TarEntry.CreateEntryFromFile(fi.FullName);
                            ent.TarHeader.Name = $"{SNAPSHOT_FOLDER}/{entry.Path}";
                            arch.WriteEntry(ent, false);
                        }
                        catch (Exception ex)
                        {
                            logger.AddError(LOG_CONTEXT, ex.Message);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No Changes Detected.");
            }
        }

        async Task Restore()
        {
            string tmpFile = Path.GetTempFileName();
            File.Delete(tmpFile);

            string tmpOutputPath = Path.GetFileName(tmpFile);
            if (!options.ArchivePath.IsEmpty())
            {
                tmpOutputPath = Path.Combine(options.ArchivePath, tmpOutputPath);
            }

            // Extract the archive
            logger.AddInfo("SyncApp.Restore", "Extracting to the temp location.");
            {
                using Stream stream = options.InputFile.IsEmpty()
                    ? Console.OpenStandardInput()
                    : new FileStream(options.InputFile, FileMode.Open, FileAccess.Read);

                using (TarArchive arch = TarArchive.CreateInputTarArchive(stream, Encoding.UTF8))
                {
                    arch.SetKeepOldFiles(false);

                    try
                    {
                        arch.ExtractContents(tmpOutputPath);
                    }
                    catch (Exception ex)
                    {
                        logger.AddError("SyncApp.Restore", ex.Message);
                    }
                }
            }

            // Read the snapshot data and remove any files from the target locations
            logger.AddInfo("SyncApp.Restore", "Removing deleted files.");
            {
                string snapshotFile = Path.Combine(tmpOutputPath, SNAPSHOT_FILE_NAME);
                var snapshotCollection = await SnapshotCollection.Load(snapshotFile, options.ArchivePath);

                var snapshot = snapshotCollection.Snapshots
                    .OrderByDescending(s => s.Date)
                    .FirstOrDefault();

                if (snapshot != null)
                {
                    string root = options.ArchivePath ?? string.Empty;
                    foreach (var entry in snapshot.Entries.Where(e => e.Action == Snapshot.EntryAction.Remove))
                    {
                        try
                        {
                            string path = Path.Combine(root, entry.Path);
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.AddError("SyncApp.Restore", ex.Message);
                        }
                    }

                    foreach (var entry in snapshot.Entries.Where(e => e.Action == Snapshot.EntryAction.Move))
                    {
                        try
                        {
                            string pathFrom = Path.Combine(root, entry.MoveFrom);
                            string pathTo = Path.Combine(root, entry.Path);
                            if (File.Exists(pathFrom))
                            {
                                File.Move(pathFrom, pathTo);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.AddError("SyncApp.Restore", ex.Message);
                        }
                    }
                }

                File.Delete(snapshotFile);
            }

            // Move all of the files into the target locations
            logger.AddInfo("SyncApp.Restore", "Moving all files to final locations");
            {
                string tmpRoot = new DirectoryInfo(Path.Combine(tmpOutputPath, SNAPSHOT_FOLDER)).FullName;
                string root = new DirectoryInfo(options.ArchivePath ?? "./").FullName;

                foreach (var fname in Directory.EnumerateFiles(tmpOutputPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var targetFile = new FileInfo(Path.Combine(root, fname.Substring(tmpRoot.Length + 1)));

                        Directory.CreateDirectory(Path.GetDirectoryName(targetFile.FullName));
                        File.Move(fname, targetFile.FullName, true);
                    }
                    catch(Exception ex)
                    {
                        logger.AddError("SyncApp.Restore", ex.Message);
                    }
                }

                logger.AddInfo("SyncApp.Restore", "Cleaning up");
                Directory.Delete(tmpOutputPath, true);
            }
        }
    }
}
