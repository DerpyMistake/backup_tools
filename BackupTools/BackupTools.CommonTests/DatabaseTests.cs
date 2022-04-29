using NUnit.Framework;
using BitEffects.BackupTools.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BitEffects.BackupTools.DB.Tests
{
    [TestFixture]
    public class DatabaseTests
    {
        string tmpPath;
        Database databaseA;
        Database databaseB;
        Options options;

        [SetUp]
        public void SetUp()
        {
            tmpPath = Path.GetTempFileName();
            File.Delete(tmpPath);
            Directory.CreateDirectory(tmpPath);

            options = new Options() { DBPath = tmpPath };
            databaseA = new Database(options);
            databaseB = new Database(options);
        }

        [TearDown]
        public void TearDown()
        {
            databaseA.Dispose();
            databaseB.Dispose();
            Directory.Delete(tmpPath, true);
        }

        [Test]
        async public Task MutexEnsuresSafeUpdates()
        {
            List<Task> tasks = new List<Task>();

            int idx = 1;
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(addRecords(databaseA, idx++));
                tasks.Add(addRecords(databaseB, idx++));
                tasks.Add(addRecords(databaseA, idx++));
                tasks.Add(addRecords(databaseB, idx++));
                await Task.Delay(50);
            }

            Task.WaitAll(tasks.ToArray());
            databaseA.Save();
            databaseB.Save();

            var entriesA = databaseA.LogEntries.Select(e => e.Message).OrderBy(e => e).ToHashSet();
            var entriesB = databaseB.LogEntries.Select(e => e.Message).OrderBy(e => e).ToHashSet();

            Assert.IsTrue(entriesA.SequenceEqual(entriesB));
            Assert.AreEqual(databaseA.LogEntries.Count, databaseB.LogEntries.Count);

            async Task addRecords(Database db, int id)
            {
                db.LogEntries.Add(new LogEntry()
                {
                    Type = LogEntryType.Info,
                    AppName = "Test",
                    Context = "Test",
                    Message = $"Test_{id}",
                });
                db.Save();

                await Task.Delay(150);
            }
        }

        [Test]
        public void LogEntriesSerializeProperly()
        {
            var entry = new LogEntry()
            {
                AppName = "Test",
                Context = "Context",
                Message = "Message",
                Type = LogEntryType.Error,
                Time = DateTime.Today
            };
            var origJSON = entry.Serialize();
            var newJSON = origJSON.Deserialize<LogEntry>().Serialize();

            Assert.AreEqual(origJSON, newJSON);
        }

        [Test]
        public void BackupEntriesSerializeProperly()
        {
            var entry = new BackupEntry("AAA", "/path/to/file")
            {
                CreationDate = DateTime.Today
            };
            var origJSON = entry.Serialize();
            var newJSON = origJSON.Deserialize<BackupEntry>().Serialize();

            Assert.AreEqual(origJSON, newJSON);
        }
    }
}