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

            return meta!;
        }
    }

    public async Task EnsureSeeded(IServiceProvider serviceProvider)
    {
        var shouldSeed = wasRebuilt || await (options?.Value?.NeedsNewSeed?.Invoke(meta!) ?? Task.FromResult(false));
        if (shouldSeed)
        {
            await (options?.Value?.SeedData?.Invoke(serviceProvider) ?? Task.CompletedTask);
        }
    }

    public TestDbMeta AttachEmpty() => AttachSchema();

    public TestDbMeta AttachSchema() => EnsureDatabase();

    public void Detach(TestDbMeta id) { }

    private void InitializeMetadata()
    {
        var filePath = Path.GetFullPath(Path.Combine(FolderName, "reused-database.sqlite"), options.Value.WorkingDirectory);

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
        var databaseDirectory = Path.GetDirectoryName(meta!.Path)!;
        var databasePath = meta.Path!;

        lock (lockObj)
        {
            Directory.CreateDirectory(databaseDirectory);
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
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