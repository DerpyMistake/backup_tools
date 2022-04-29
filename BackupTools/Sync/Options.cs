namespace BitEffects.BackupTools.Sync
{
    public class Options : BitEffects.BackupTools.Options
    {
        [Alias("c")]
        public bool Backup { get; set; }

        [Alias("x")]
        public bool Restore { get; set; }

        [Alias("path")]
        public string ArchivePath { get; set; }

        [Alias("o")]
        public string OutputFile { get; set; }

        [Alias("i")]
        public string InputFile { get; set; }

        [Alias("g")]
        [Alias("sn")]
        public string Snapshot { get; set; }
    }
}
