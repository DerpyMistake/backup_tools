using System;
using System.Collections.Generic;

namespace BitEffects.BackupTools.DB
{
    public interface IDatabase
    {
        List<LogEntry> LogEntries { get; }
        List<BackupEntry> BackupEntries { get; }
    }

    public class LogEntry
    {
        public string AppName { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public LogEntryType Type { get; set; }
        public string Context { get; set; }
        public string Message { get; set; }

    }

    public class BackupEntry
    {
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public string ArchiveId { get; set; }
        public ArchiveMetaData Metadata { get; set; }

        public BackupEntry() { }
        public BackupEntry(string archiveId, string path)
        {
            this.ArchiveId = archiveId;
            this.Metadata = new ArchiveMetaData(path);
        }
    }
}
