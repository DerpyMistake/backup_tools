using Amazon.Glacier;
using BitEffects.BackupTools.DB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.Restore
{
    /// <summary>
    /// Submit a restore job to the glacier servers and/or stream the results to stdout
    /// </summary>
    class Program
    {
        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddBackupAppContext<Options>(args)
                .AddGlacierClient();

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                try
                {
                    var app = ActivatorUtilities.CreateInstance<RestoreApp>(scope.ServiceProvider);
                    await app.Run();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Unhandled Exception]: {ex.Message}");

                    scope.ServiceProvider.GetRequiredService<ILogger>()
                        ?.AddError("[Unhandled Exception]", ex.Message);
                }
                finally
                {
                    (scope.ServiceProvider.GetRequiredService<IDatabase>() as Database)
                        ?.Save();
                }
            }
        }
    }
}
