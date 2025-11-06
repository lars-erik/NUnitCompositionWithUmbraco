using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Cms.Web.Common;
using UmbracoTestsComposition.Common;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace UmbracoTestsComposition.ReusedDatabase;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.None,
    Boot = false,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ServiceProvider]
public abstract class ReusedDatabaseSetUpBase : UmbracoIntegrationTest
{
    private TestDbMeta? databaseMeta;

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        var root = Path.Combine(TestHelper.WorkingDirectory, nameof(ReusedDatabase));
        ReusedDatabaseStorage.Initialize(root);

        services.AddSingleton<ReusedDatabaseTestDatabase>();
        services.AddSingleton<ITestDatabase>(sp => sp.GetRequiredService<ReusedDatabaseTestDatabase>());
    }

    [OneTimeSetUp]
    public async Task EnsureReusedDatabaseAsync()
    {
        var testDatabase = Services.GetRequiredService<ReusedDatabaseTestDatabase>();
        TestContext.Progress.WriteLine($"[{GetType().Name}] Ensuring reused database...");
        var (meta, created) = testDatabase.EnsureDatabase();
        databaseMeta = meta;

        TestContext.Progress.WriteLine($"[{GetType().Name}] Database {(created ? "created" : "reused")} with connection string: {meta.ConnectionString}");

        ConfigureUmbracoDatabase(meta);
        var seeded = await EnsureSeedDataAsync();
        if (created || seeded)
        {
            Interlocked.Increment(ref SeededDatabaseFixture.SeedCount);
            TestContext.Progress.WriteLine($"[{GetType().Name}] Seeded database content. created={created}, seeded={seeded}");
        }
        else
        {
            TestContext.Progress.WriteLine($"[{GetType().Name}] Database already contained seeded content.");
        }

        TestContext.Progress.WriteLine($"[{GetType().Name}] Bootstrapping Umbraco context.");
        Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
    }

    [OneTimeTearDown]
    public void DetachDatabase()
    {
        if (databaseMeta != null)
        {
            var testDatabase = Services.GetRequiredService<ReusedDatabaseTestDatabase>();
            TestContext.Progress.WriteLine($"[{GetType().Name}] Detaching reused database.");
            testDatabase.Detach(databaseMeta);
        }
    }

    protected static void MarkDatabaseOutdated() => ReusedDatabaseTestDatabase.MarkDatabaseOutdated();

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

    private async Task<bool> EnsureSeedDataAsync()
    {
        var contentTypeService = Services.GetRequiredService<IContentTypeService>();
        var scopeProvider = Services.GetRequiredService<ICoreScopeProvider>();

        using var scope = scopeProvider.CreateCoreScope(autoComplete: true);
        var existing = contentTypeService.Get(ReusedDatabaseSeed.DocumentTypeAlias);
        if (existing != null)
        {
            TestContext.Progress.WriteLine($"[{GetType().Name}] Seed data already exists.");
            return false;
        }

        TestContext.Progress.WriteLine($"[{GetType().Name}] Creating seed document type '{ReusedDatabaseSeed.DocumentTypeAlias}'.");
        var contentType = ContentTypeBuilder.CreateBasicContentType(
            ReusedDatabaseSeed.DocumentTypeAlias,
            ReusedDatabaseSeed.DocumentTypeName);
        contentType.Key = ReusedDatabaseSeed.DocumentTypeKey;
        contentType.AllowedAsRoot = true;

        var attempt = await contentTypeService.CreateAsync(
            contentType,
            Constants.Security.SuperUserKey);

        if (!attempt.Success)
        {
            throw new InvalidOperationException(
                $"Failed to create the reused database content type: {attempt.Result}");
        }

        scope.Complete();
        TestContext.Progress.WriteLine($"[{GetType().Name}] Seed document type created successfully.");
        return true;
    }
}
