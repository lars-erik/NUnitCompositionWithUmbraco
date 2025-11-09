using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common.Database;

public class ReusedSqliteTestDatabase : IReusableTestDatabase
{
    private const string FolderName = "reused-databases";
    private const string DatabaseFileName = "reused-database.sqlite";

    private readonly Lock lockObj = new();
    private readonly TestUmbracoDatabaseFactoryProvider databaseFactoryProvider;
    private readonly ILoggerFactory loggerFactory;
    private readonly IOptions<ReusedTestDatabaseOptions> options;
    private TestDbMeta? meta;
    private bool wasRebuilt;

    public ReusedSqliteTestDatabase
    (
        TestUmbracoDatabaseFactoryProvider databaseFactoryProvider,
        ILoggerFactory loggerFactory,
        IOptions<ReusedTestDatabaseOptions> options
    )
    {
        this.databaseFactoryProvider = databaseFactoryProvider;
        this.options = options;
        this.loggerFactory = loggerFactory;

        InitializeMetadata();
    }

    public TestDbMeta EnsureDatabase()
    {
        lock (lockObj)
        {
            if (ShouldRebuild())
            {
                RebuildWithSchema();
            }
            else
            {
                TestContext.Progress.WriteLine("Restoring from snapshot");
                File.Copy(meta!.Path.Replace(".sqlite", "-snapshot.sqlite"), meta!.Path, overwrite: true);
            }

            return meta!;
        }
    }

    public async Task EnsureSeeded(IServiceProvider serviceProvider)
    {
        var shouldSeed = wasRebuilt || await (options?.Value?.NeedsNewSeed?.Invoke(meta!) ?? Task.FromResult(false));
        if (shouldSeed)
        {
            TestContext.Progress.WriteLine("Seeding database");
            await (options?.Value?.SeedData?.Invoke(serviceProvider) ?? Task.CompletedTask);
            
            TestContext.Progress.WriteLine("Writing snapshot");
            var snapshotPath = meta.Path!.Replace(".sqlite", "-snapshot.sqlite");
            var dbFactory = databaseFactoryProvider.Create();
            dbFactory.Configure(meta!.ToStronglyTypedConnectionString());
            using var database = (UmbracoDatabase)dbFactory.CreateDatabase();
            database.Execute($"VACUUM INTO '{snapshotPath}';");
        }
    }

    public TestDbMeta AttachEmpty() => AttachSchema();

    public TestDbMeta AttachSchema() => EnsureDatabase();

    public void Detach(TestDbMeta id) { }

    private void InitializeMetadata()
    {
        var filePath = Path.GetFullPath(Path.Combine(FolderName, DatabaseFileName), options.Value.WorkingDirectory);

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
        if (!File.Exists(meta!.Path))
        {
            return true;
        }

        return options.Value.NeedsNewSeed?.Invoke(meta!).GetAwaiter().GetResult() ?? false;
    }
}