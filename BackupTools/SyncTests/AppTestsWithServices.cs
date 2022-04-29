using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace BitEffects.BackupTools.Sync.Tests
{
    public class AppTestsWithServices
    {
        ServiceCollection services;
        ServiceProvider provider;
        protected IServiceScope scope { get; private set; }

        [OneTimeSetUp]
        virtual public void OneTimeSetUp()
        {
            services = new ServiceCollection();
            services.AddScoped<DB.IDatabase>(provider => ActivatorUtilities.CreateInstance<Database>(provider));
            services.AddScoped<ILogger>(provider => ActivatorUtilities.CreateInstance<LoggerStub>(provider));
        }

        [SetUp]
        virtual public void SetUp()
        {
            provider = services.BuildServiceProvider();
            scope = provider.CreateScope();
        }

        [TearDown]
        virtual public void TearDown()
        {
            scope?.Dispose();
            provider?.Dispose();
        }
    }
}