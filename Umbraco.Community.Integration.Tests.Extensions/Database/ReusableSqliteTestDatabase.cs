using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Security;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Integration.Implementations;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Community.Integration.Tests.Extensions.Database;

public class ReusableSqliteTestDatabase : IReusableTestDatabase
{
    private readonly UmbracoIntegrationTestBase invocationProxy;
    private const string FolderName = "reused-databases";
    private const string DatabaseFileName = "reused-database.sqlite";

    private readonly Lock lockObj = new();
    private TestUmbracoDatabaseFactoryProvider databaseFactoryProvider;
    private ILoggerFactory loggerFactory;
    private IOptions<ReusableTestDatabaseOptions> options;
    private TestDbMeta? meta;
    private bool wasRebuilt;

    private bool resolve;

    public ReusableSqliteTestDatabase
    (
        TestUmbracoDatabaseFactoryProvider databaseFactoryProvider,
        ILoggerFactory loggerFactory,
        IOptions<ReusableTestDatabaseOptions> options
    )
    {
        this.databaseFactoryProvider = databaseFactoryProvider;
        this.options = options;
        this.loggerFactory = loggerFactory;

        InitializeMetadata(this.options.Value.WorkingDirectory);

        resolve = false;
    }

    public ReusableSqliteTestDatabase
        (TestHelper testHelper, UmbracoIntegrationTestBase invocationProxy)
    {
        this.invocationProxy = invocationProxy;
        resolve = true;

        InitializeMetadata(testHelper.WorkingDirectory);
    }

    public TestDbMeta EnsureDatabase()
    {
        lock (lockObj)
        {
            if (resolve)
            {
                var services = (IServiceProvider)invocationProxy.GetType().GetProperty("Services", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(invocationProxy)!;
                options = services.GetRequiredService<IOptions<ReusableTestDatabaseOptions>>();
                loggerFactory = services.GetRequiredService<ILoggerFactory>();
                databaseFactoryProvider = services.GetRequiredService<TestUmbracoDatabaseFactoryProvider>();
            }

            if (ShouldRebuild())
            {
                RebuildWithSchema();
            }
            else
            {
                RestoreSnapshot().GetAwaiter().GetResult();
            }

            return meta!;
        }
    }

    public async Task RestoreSnapshot()
    {
        await TestContext.Progress.WriteLineAsync("Restoring from snapshot");
        File.Copy(meta!.Path.Replace(".sqlite", "-snapshot.sqlite"), meta!.Path, overwrite: true);
    }

    public async Task EnsureSeeded(IServiceProvider serviceProvider)
    {
        var shouldSeed = wasRebuilt || await (options?.Value?.NeedsNewSeed?.Invoke(meta!) ?? Task.FromResult(false));

        if (shouldSeed)
        {
            await TestContext.Progress.WriteLineAsync("Seeding database");

            using (var scope = serviceProvider.CreateScope())
            {
                var openIdDictManager = scope.ServiceProvider.GetService<IOpenIddictApplicationManager?>();
                if (openIdDictManager != null)
                {
                    var initializer = scope.ServiceProvider.GetService<IBackOfficeApplicationManager>();
                    if (initializer != null)
                    {
                        await initializer.EnsureBackOfficeApplicationAsync([new Uri("https://localhost")]);
                    }
                }
            }

            await (options?.Value?.SeedData?.Invoke(serviceProvider) ?? Task.CompletedTask);

            await TestContext.Progress.WriteLineAsync("Writing snapshot");
            var snapshotPath = meta!.Path!.Replace(".sqlite", "-snapshot.sqlite");
            var dbFactory = databaseFactoryProvider.Create();
            dbFactory.Configure(meta!.ToStronglyTypedConnectionString());
            using var database = (UmbracoDatabase)dbFactory.CreateDatabase();
            await database.ExecuteAsync($"VACUUM INTO '{snapshotPath}';");
        }
    }

    public TestDbMeta AttachEmpty() => AttachSchema();

    public TestDbMeta AttachSchema() => EnsureDatabase();

    public void Detach(TestDbMeta id) { }

    private void InitializeMetadata(string workingDirectory)
    {
        var filePath = Path.GetFullPath(Path.Combine(FolderName, DatabaseFileName), workingDirectory);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            Pooling = false,
            ForeignKeys = true
        };

        meta = new TestDbMeta
        (
            name: "ReusedDatabase",
            isEmpty: false,
            connectionString: builder.ConnectionString,
            providerName: global::Umbraco.Cms.Persistence.Sqlite.Constants.ProviderName,
            path: builder.DataSource
        );
    }

    private void RebuildWithSchema()
    {
        TestContext.Progress.WriteLine("Creating database with schema");

        var databaseDirectory = Path.GetDirectoryName(meta!.Path)!;
        var databasePath = meta.Path!;
        var snapshotPath = meta.Path!.Replace(".sqlite", "-snapshot.sqlite");

        lock (lockObj)
        {
            Directory.CreateDirectory(databaseDirectory);
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
            if (File.Exists(snapshotPath))
            {
                File.Delete(snapshotPath);
            }
        }

        var dbFactory = databaseFactoryProvider.Create();
        dbFactory.Configure(meta!.ToStronglyTypedConnectionString());

        using var database = (UmbracoDatabase)dbFactory.CreateDatabase();
        using var transaction = database.GetTransaction();

        var installOptions = new TestOptionsMonitor<InstallDefaultDataSettings>(
            new InstallDefaultDataSettings { InstallData = InstallDefaultDataOption.All });

        var schemaCreator = new DatabaseSchemaCreator(
            database,
            loggerFactory.CreateLogger<DatabaseSchemaCreator>(),
            loggerFactory,
            new UmbracoVersion(),
            Mock.Of<IEventAggregator>(),
            installOptions);

        schemaCreator.InitializeDatabaseSchema();
        transaction.Complete();

        wasRebuilt = true;

        TestContext.Progress.WriteLine("Database with schema created");
    }

    public bool ShouldRebuild()
    {
        if (!File.Exists(meta!.Path) || !File.Exists(meta!.Path.Replace(".sqlite", "-snapshot.sqlite")))
        {
            return true;
        }

        return options.Value.NeedsNewSeed?.Invoke(meta!).GetAwaiter().GetResult() ?? false;
    }
}