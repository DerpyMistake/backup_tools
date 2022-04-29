namespace BitEffects.BackupTools
{
    public class GlacierConfig
    {
        public string AccessKeyID { get; set; }
        public string SecretAccessKey { get; set; }
        public string VaultName { get; set; }
        public string Region { get; set; }
    }

    public class AppConfig
    {
        public GlacierConfig GlacierConfig { get; set; }
        public string[] Folders { get; set; }
    }
}
