using System;
using System.IO;

namespace BitEffects.BackupTools
{
    public class Options : IConvertOptions
    {
        const string DEFAULT_APP_CONFIG_FILE = "app_config.json";
        const string DEFAULT_DB_PATH = "db";

        public AppConfig AppConfig { get; set; } = new AppConfig();
        public string DBPath { get; set; } = DEFAULT_DB_PATH;

        public Options()
        {
            if (File.Exists(DEFAULT_APP_CONFIG_FILE))
            {
                string json = File.ReadAllText(DEFAULT_APP_CONFIG_FILE);
                this.AppConfig = json.Deserialize<AppConfig>() ?? new AppConfig();
            }
        }

        virtual public bool Convert(string optionName, string[] optionValues)
        {
            if (optionName.Equals(nameof(AppConfig)))
            {
                if (optionValues?.Length == 1 && File.Exists(optionValues[0]))
                {
                    string json = File.ReadAllText(optionValues[0]);
                    this.AppConfig = json.Deserialize<AppConfig>();
                }
                else
                {
                    Console.Error.WriteLine($"Invalid Configuration Specified.");
                }

                this.AppConfig = this.AppConfig ?? new AppConfig();

                return true;
            }

            return false;
        }
    }
}
