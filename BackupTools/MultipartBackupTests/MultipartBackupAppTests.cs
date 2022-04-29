using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using BitEffects.BackupTools.DB;

namespace BitEffects.BackupTools.MultipartBackup.Tests
{
    [TestFixture]
    public class MultipartBackupAppTests
    {
        string inputFile;
        IServiceCollection services;
        ServiceProvider provider;
        IServiceScope scope;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            inputFile = Path.GetTempFileName();
            File.WriteAllText(inputFile, "ABCDEFG");

            Options options = new Options()
            {
                Path = "path/to/file",
                ChunkSize = 3,
                InputFile = inputFile
            };

            services = new ServiceCollection()
                .AddSingleton(provider => options)
                .AddSingleton<BitEffects.BackupTools.Options>(provider => options)
                .AddSingleton<ILogger>(provider => ActivatorUtilities.CreateInstance<Logger>(provider))
                .AddScoped<IDatabase>(provider => ActivatorUtilities.CreateInstance<Database>(provider))
                .AddScoped(provider => ActivatorUtilities.CreateInstance<BackupAppContext<Options>>(provider))
                .AddScoped<IGlacierBackup>(provider => ActivatorUtilities.CreateInstance<GlacierBackupStub>(provider));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            File.Delete(inputFile);
        }

        [SetUp]
        public void SetUp()
        {
            provider = services.BuildServiceProvider();
            scope = provider.CreateScope();
        }

        [TearDown]
        public void TearDown()
        {
            scope?.Dispose();
            provider?.Dispose();
        }

        [Test]
        async public Task BackupDBEntryCreated()
        {
            var app = ActivatorUtilities.CreateInstance<MultipartBackupApp>(scope.ServiceProvider);
            await app.Run();

            var database = scope.ServiceProvider.GetRequiredService<IDatabase>() as Database;
            var entry = database.BackupEntries.FirstOrDefault();

            Assert.AreEqual(1, database.BackupEntries.Count);
            Assert.AreEqual("ID", entry.ArchiveId);
            Assert.AreEqual("file", entry.Metadata.Name);
            Assert.AreEqual("path/to/file", entry.Metadata.Path);
            Assert.AreEqual(ArchiveTags.BaseArchive, entry.Metadata.Tags.FirstOrDefault());
        }

        [Test]
        async public Task GlacierBackupRunsAsExpected()
        {
            var app = ActivatorUtilities.CreateInstance<MultipartBackupApp>(scope.ServiceProvider);
            await app.Run();

            var glacierBackup = scope.ServiceProvider.GetRequiredService<IGlacierBackup>() as GlacierBackupStub;
            var data = Encoding.UTF8.GetString(glacierBackup.Buffer);

            Assert.AreEqual(3, glacierBackup.FileCount);
            Assert.AreEqual("ABCDEFG", data);
        }

        [Test]
        public void DatabaseIsDisposedOutOfScope()
        {
            Database db;

            using (var scope = provider.CreateScope())
            {
                db = scope.ServiceProvider.GetRequiredService<IDatabase>() as Database;
            }

            Assert.IsTrue(db.WasDisposed);
        }

        class LoggerStub : ILogger
        {
            public void AddEntry(LogEntryType type, string context, string message) { }
        }

        class GlacierBackupStub : IGlacierBackup
        {
            readonly ByteBuffer buffer = new ByteBuffer();

            int fileCount = 0;

            public int FileCount => fileCount;
            public byte[] Buffer => buffer.Read(buffer.Length);

            public Task<string> CompleteMultipartUpload()
            {
                return Task.FromResult("ID");
            }

            public Task<string> InitiateMultipartUpload()
            {
                return Task.FromResult("ID");
            }

            public Task UploadPart(Stream stream)
            {
                fileCount++;

                var reader = new BinaryReader(stream);
                var data = reader.ReadBytes((int)stream.Length);
                buffer.Write(data, data.Length);

                return Task.CompletedTask;
            }
        }

        public class Database : DB.IDatabase, IDisposable
        {
            public List<LogEntry> LogEntries { get; set;} = new List<LogEntry>();
            public List<BackupEntry> BackupEntries { get; set; } = new List<BackupEntry>();

            public bool WasDisposed { get; private set; } = false;

            public void Dispose()
            {
                this.WasDisposed = true;
            }
        }
    }
}