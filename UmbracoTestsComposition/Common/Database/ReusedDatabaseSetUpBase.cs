using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Infrastructure.Persistence;

namespace UmbracoTestsComposition.Common.Database;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.None,
    Boot = false,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ServiceProvider]
public abstract class SeededUmbracoIntegrationTest : UmbracoIntegrationTest
{
    private TestDbMeta? databaseMeta;

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.Configure<ReusedTestDatabaseOptions>(options =>
        {
            options.WorkingDirectory = TestHelper.WorkingDirectory;
            ConfigureTestDatabaseOptions(options);
        });

        services.AddSingleton<ReusedTestDatabase>();
        services.AddSingleton<ITestDatabase>(sp => sp.GetRequiredService<ReusedTestDatabase>());
    }

    protected abstract void ConfigureTestDatabaseOptions(ReusedTestDatabaseOptions options);

    [OneTimeSetUp]
    public async Task EnsureReusedDatabaseAsync()
    {
        var testDatabase = Services.GetRequiredService<ReusedTestDatabase>();
        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Ensuring reused database...");
        var meta = testDatabase.EnsureDatabase();
        databaseMeta = meta;

        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Database set up with connection string: {meta.ConnectionString}");

        ConfigureUmbracoDatabase(meta);

        await testDatabase.EnsureSeeded();

        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Bootstrapping Umbraco context.");
        Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
    }

    [OneTimeTearDown]
    public void DetachDatabase()
    {
        if (databaseMeta != null)
        {
            var testDatabase = Services.GetRequiredService<ReusedTestDatabase>();
            TestContext.Progress.WriteLine($"[{GetType().Name}] Detaching reused database.");
            testDatabase.Detach(databaseMeta);
        }
    }

    private void ConfigureUmbracoDatabase(TestDbMeta meta)
    {
        var databaseFactory = Services.GetRequiredService<IUmbracoDatabaseFactory>();
        var connectionStrings = Services.GetRequiredService<IOptionsMonitor<ConnectionStrings>>();
        var runtimeState = Services.GetRequiredService<IRuntimeState>();

        databaseFactory.Configure(meta.ToStronglyTypedConnectionString());
        connectionStrings.CurrentValue.ConnectionString = meta.ConnectionString;
        connectionStrings.CurrentValue.ProviderName = meta.Provider;

        runtimeState.DetermineRuntimeLevel();
        Services.GetRequiredService<IEventAggregator>().Publish(new UnattendedInstallNotification());
    }
}