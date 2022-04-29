using System;
using System.Collections.Generic;

namespace BitEffects.BackupTools
{
    static public class ArchiveTags
    {
        public const string Snapshot = "snapshot";
        public const string BaseArchive = "base-archive";
    }

    public class ArchiveMetaData
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public HashSet<string> Tags { get; set; } = new HashSet<string>();

        public ArchiveMetaData(string name, string path)
        {
            this.Name = name;
            this.Path = path;
        }

        [System.Text.Json.Serialization.JsonConstructor]
        public ArchiveMetaData(string path)
            : this(System.IO.Path.GetFileName(path), path)
        {
        }

        static public ArchiveMetaData FromDescription(string description)
        {
            string name = description.SplitFirst("|", out description);
            string path = description.SplitFirst("|", out description);
            string date = description.SplitFirst("|", out description);
            string tags = description;

            return new ArchiveMetaData(name, path)
            {
                Created = new DateTime(date.ToLong(), DateTimeKind.Utc),
                Tags = new HashSet<string>(tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            };
        }

        public string ToDescription()
        {
            return $"{Name}|{Path}|{Created.Ticks}|{string.Join(',', Tags)}";
        }
    }
}
