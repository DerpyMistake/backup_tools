using BitEffects.BackupTools.DB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BitEffects.BackupTools.Sync
{
    class Program
    {
        /// <summary>
        /// Sync creates a snapshot archive with snapshot details it
        /// can use to accurately restore the files
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        async static Task Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddBackupAppContext<Options>(args)
                .AddGlacierClient();

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                try
                {
                    var app = ActivatorUtilities.CreateInstance<SyncApp>(scope.ServiceProvider);
                    await app.Run();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Sync:[Unhandled Exception]: {ex.Message}");

                    scope.ServiceProvider.GetRequiredService<ILogger>()
                        ?.AddError("Sync:[Unhandled Exception]", ex.Message);

                    Environment.Exit(1);
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
