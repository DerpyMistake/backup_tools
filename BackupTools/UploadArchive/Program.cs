using System;
using System.IO;

namespace BitEffects.UploadArchive
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public class GlacierConfig
    {
        public string accessKeyID { get; set; }
        public string secretAccessKey { get; set; }
    }

    public class UploadOptions : IConvertOptions
    {
        public GlacierConfig Config { get; set; }

        public bool Convert(string optionName, string[] optionValues)
        {
            if (optionName.Equals(nameof(Config)))
            {
                if (optionValues?.Length == 1 && File.Exists(optionValues[0]))
                {
                    string json = File.ReadAllText(optionValues[0]);
                    this.Config = json.Deserialize<GlacierConfig>();
                }
                else
                {
                    this.Config = new GlacierConfig();
                    Console.Error.WriteLine($"Invalid Configuration Specified.");
                }

                return true;
            }

            return false;
        }
    }
}
