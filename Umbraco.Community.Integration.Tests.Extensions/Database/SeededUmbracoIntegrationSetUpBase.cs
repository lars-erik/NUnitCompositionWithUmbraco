using Microsoft.Extensions.Configuration;
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

namespace Umbraco.Community.Integration.Tests.Extensions.Database;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.None,
    Boot = false,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ServiceProvider]
public abstract class SeededUmbracoIntegrationSetUpBase(bool boot = false) : UmbracoIntegrationTest
{
    private TestDbMeta? databaseMeta;

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        var settings = new TestDatabaseSettings
        {
            FilesPath = Path.Combine(TestHelper.WorkingDirectory, "databases"),
        };
        Configuration.Bind("Tests:Database", settings);
        services.AddSingleton(settings);

        services.Configure<ReusableTestDatabaseOptions>(options =>
        {
            options.WorkingDirectory = TestHelper.WorkingDirectory;
            ConfigureTestDatabaseOptions(options);
        });

        var databaseType = settings.DatabaseType switch
        {
            TestDatabaseSettings.TestDatabaseType.Sqlite => typeof(ReusableSqliteTestDatabase),
            TestDatabaseSettings.TestDatabaseType.SqlServer => typeof(ReusableSqlServerTestDatabase),
            _ => throw new Exception($"Reusable test database implementation for {settings.DatabaseType} not found.")
        };

        services.AddSingleton(typeof(IReusableTestDatabase), sp => sp.CreateInstance(databaseType));
        services.AddSingleton<ITestDatabase>(sp => sp.GetRequiredService<IReusableTestDatabase>());


        services.AddUnique<IUmbracoContextAccessor, TestUmbracoContextAccessor>();
    }

    protected abstract void ConfigureTestDatabaseOptions(ReusableTestDatabaseOptions options);

    [OneTimeSetUp]
    public async Task EnsureReusedDatabaseAsync()
    {
        var testDatabase = Services.GetRequiredService<IReusableTestDatabase>();
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
            var testDatabase = GetRequiredService<IReusableTestDatabase>();
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