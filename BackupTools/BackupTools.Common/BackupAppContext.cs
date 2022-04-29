using BitEffects.BackupTools.DB;
using Microsoft.Extensions.DependencyInjection;

namespace BitEffects.BackupTools
{
    public class BackupAppContext<TOptions>
        where TOptions : Options
    {
        readonly public TOptions options;
        readonly public ILogger logger;
        readonly public IDatabase database;

        public BackupAppContext(TOptions options, ILogger logger, IDatabase database)
        {
            this.options = options;
            this.logger = logger;
            this.database = database;
        }
    }

    static public class BackupAppContextExtensions
    {
        static public IServiceCollection AddBackupAppContext<TOptions>(this IServiceCollection services, string[] args)
            where TOptions : Options
        {
            var options = CommandLineUtility.ParseOptions<TOptions>(args);

            return services
                .AddSingleton(provider => options)
                .AddSingleton<BitEffects.BackupTools.Options>(provider => options)
                .AddSingleton<ILogger>(provider => ActivatorUtilities.CreateInstance<Logger>(provider))
                .AddScoped<IDatabase>(provider => ActivatorUtilities.CreateInstance<Database>(provider))
                .AddScoped(provider => ActivatorUtilities.CreateInstance<BackupAppContext<TOptions>>(provider));
        }
    }
}
