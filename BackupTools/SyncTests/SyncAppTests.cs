using NUnit.Framework;
using BitEffects.BackupTools.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BitEffects.BackupTools.DB;
using Microsoft.Extensions.DependencyInjection;

namespace BitEffects.BackupTools.Sync.Tests
{
    [TestFixture]
    public class SyncAppTests : AppTestsWithServices
    {
        const string ARCHIVE_FILE_NAME = "test.tar";
        const string SNAPSHOT_FILE_NAME = "snap.gz";

        string tmpDirectory;
        string archiveHoldingDirectory => Path.Combine(tmpDirectory, "Holding");
        string archiveSourceDirectory => Path.Combine(tmpDirectory, "Source");
        string archiveDestinationDirectory => Path.Combine(tmpDirectory, "Destination");

        [OneTimeSetUp]
        override public void OneTimeSetUp()
        {
            tmpDirectory = Path.GetTempFileName();
            File.Delete(tmpDirectory);

            Directory.CreateDirectory(archiveHoldingDirectory);
            Directory.CreateDirectory(archiveSourceDirectory);
            Directory.CreateDirectory(archiveDestinationDirectory);

            AddTestFile("file1.txt", 1000, 2000);
            AddTestFile("file2.txt", 2000, 3000);
            AddTestFile("file3.txt", 3000, 4000);
            AddTestFile("file4.txt", 4000, 5000);
            AddTestFile("file5.txt", 5000, 6000);
            AddTestFile("file7.txt", 7000, 8000);

            base.OneTimeSetUp();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Directory.Delete(tmpDirectory, true);
        }

        void AddTestFile(string fname, int start, int end)
        {
            string content = string.Join("\n", Enumerable.Range(start, end).Select(i => i.ToString()));
            File.WriteAllText(Path.Combine(archiveSourceDirectory, fname), content);
        }

        void ValidateTestFileContents(string fname, int start, int end)
        {
            string expectedContent = string.Join("\n", Enumerable.Range(start, end).Select(i => i.ToString()));
            string content = File.ReadAllText(Path.Combine(archiveDestinationDirectory, fname));

            Assert.AreEqual(expectedContent, content, $"Validating {fname} Content");
        }

        [Test, Order(1)]
        async public Task BackupBaseTest()
        {
            Options options = new Options()
            {
                Backup = true,
                OutputFile = Path.Combine(archiveHoldingDirectory, $"01_{ARCHIVE_FILE_NAME}"),
                Snapshot = Path.Combine(archiveHoldingDirectory, SNAPSHOT_FILE_NAME),
                ArchivePath = archiveSourceDirectory
            };

            SyncApp app = ActivatorUtilities.CreateInstance<SyncApp>(scope.ServiceProvider, options);
            await app.Run();
        }

        [Test, Order(2)]
        async public Task BackupSnapshotTest()
        {
            Options options = new Options()
            {
                Backup = true,
                OutputFile = Path.Combine(archiveHoldingDirectory, $"02_{ARCHIVE_FILE_NAME}"),
                Snapshot = Path.Combine(archiveHoldingDirectory, SNAPSHOT_FILE_NAME),
                ArchivePath = archiveSourceDirectory
            };

            File.Delete(Path.Combine(archiveSourceDirectory, "file1.txt"));
            AddTestFile("file5.txt", 5500, 6000);
            AddTestFile("file6.txt", 6000, 7000);
            File.Move(
                Path.Combine(archiveSourceDirectory, "file7.txt"),
                Path.Combine(archiveSourceDirectory, "file8.txt")
            );

            SyncApp app = ActivatorUtilities.CreateInstance<SyncApp>(scope.ServiceProvider, options);
            await app.Run();
        }

        [Test, Order(3)]
        async public Task RestoreBaseTest()
        {
            Options options = new Options()
            {
                Restore = true,
                InputFile = Path.Combine(archiveHoldingDirectory, $"01_{ARCHIVE_FILE_NAME}"),
                Snapshot = Path.Combine(archiveHoldingDirectory, SNAPSHOT_FILE_NAME),
                ArchivePath = archiveDestinationDirectory
            };

            SyncApp app = ActivatorUtilities.CreateInstance<SyncApp>(scope.ServiceProvider, options);
            await app.Run();

            ValidateTestFileContents("file1.txt", 1000, 2000);
            ValidateTestFileContents("file2.txt", 2000, 3000);
            ValidateTestFileContents("file3.txt", 3000, 4000);
            ValidateTestFileContents("file4.txt", 4000, 5000);
            ValidateTestFileContents("file5.txt", 5000, 6000);
            ValidateTestFileContents("file7.txt", 7000, 8000);
        }

        [Test, Order(4)]
        async public Task RestoreSnapshotTest()
        {
            Options options = new Options()
            {
                Restore = true,
                InputFile = Path.Combine(archiveHoldingDirectory, $"02_{ARCHIVE_FILE_NAME}"),
                Snapshot = Path.Combine(archiveHoldingDirectory, SNAPSHOT_FILE_NAME),
                ArchivePath = archiveDestinationDirectory,
            };

            SyncApp app = ActivatorUtilities.CreateInstance<SyncApp>(scope.ServiceProvider, options);
            await app.Run();

            Assert.IsFalse(File.Exists(Path.Combine(archiveDestinationDirectory, "file1.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(archiveDestinationDirectory, "file7.txt")));
            ValidateTestFileContents("file2.txt", 2000, 3000);
            ValidateTestFileContents("file3.txt", 3000, 4000);
            ValidateTestFileContents("file4.txt", 4000, 5000);
            ValidateTestFileContents("file5.txt", 5500, 6000);
            ValidateTestFileContents("file6.txt", 6000, 7000);
            ValidateTestFileContents("file8.txt", 7000, 8000);
        }
    }

    class LoggerStub : ILogger
    {
        public void AddEntry(LogEntryType type, string context, string message) { }
    }

    public class Database : DB.IDatabase, IDisposable
    {
        public List<LogEntry> LogEntries { get; set; } = new List<LogEntry>();
        public List<BackupEntry> BackupEntries { get; set; } = new List<BackupEntry>();

        public bool WasDisposed { get; private set; } = false;

        public void Dispose()
        {
            this.WasDisposed = true;
        }
    }
}