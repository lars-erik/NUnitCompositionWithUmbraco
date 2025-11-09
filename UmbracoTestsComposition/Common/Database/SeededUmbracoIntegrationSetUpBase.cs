using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common.Database;

public abstract class SeededUmbracoIntegrationSetUpBase(bool boot = false)
    : SeededUmbracoIntegrationSetUpBase<ReusedSqliteTestDatabase>(boot)
{
}

[UmbracoTest(
    Database = UmbracoTestOptions.Database.None,
    Boot = false,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ServiceProvider]
public abstract class SeededUmbracoIntegrationSetUpBase<TDatabase>(bool boot = false) : UmbracoIntegrationTest
    where TDatabase : class, IReusableTestDatabase
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

        services.AddSingleton<TDatabase>();
        services.AddSingleton<ITestDatabase>(sp => sp.GetRequiredService<TDatabase>());

        services.AddUnique<IUmbracoContextAccessor, TestUmbracoContextAccessor>();
    }

    protected abstract void ConfigureTestDatabaseOptions(ReusedTestDatabaseOptions options);

    [OneTimeSetUp]
    public async Task EnsureReusedDatabaseAsync()
    {
        var testDatabase = GetRequiredService<TDatabase>();
        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Ensuring reused database...");
        var meta = testDatabase.EnsureDatabase();
        databaseMeta = meta;

        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Database set up with connection string: {meta.ConnectionString}");

        ConfigureUmbracoDatabase(meta);

        if (boot)
        {
            await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Bootstrapping Umbraco context.");
            GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        }

        await testDatabase.EnsureSeeded(Services);
    }

    [OneTimeTearDown]
    public void DetachDatabase()
    {
        if (databaseMeta != null)
        {
            var testDatabase = GetRequiredService<ReusedSqliteTestDatabase>();
            TestContext.Progress.WriteLine($"[{GetType().Name}] Detaching reused database.");
            testDatabase.Detach(databaseMeta);
        }
    }

    private void ConfigureUmbracoDatabase(TestDbMeta meta)
    {
        var databaseFactory = GetRequiredService<IUmbracoDatabaseFactory>();
        var connectionStrings = GetRequiredService<IOptionsMonitor<ConnectionStrings>>();
        var runtimeState = GetRequiredService<IRuntimeState>();

        databaseFactory.Configure(meta.ToStronglyTypedConnectionString());
        connectionStrings.CurrentValue.ConnectionString = meta.ConnectionString;
        connectionStrings.CurrentValue.ProviderName = meta.Provider;

        runtimeState.DetermineRuntimeLevel();
        GetRequiredService<IEventAggregator>().Publish(new UnattendedInstallNotification());
    }
}