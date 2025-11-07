using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Data;
using System.Data.Common;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common.Database;

public class ReusedTestDatabase : BaseTestDatabase, ITestDatabase
{
    private const string FolderName = "reused-databases";

    private readonly object syncRoot = new();
    private readonly TestUmbracoDatabaseFactoryProvider databaseFactoryProvider;
    private readonly ILoggerFactory loggerFactory;
    private readonly IOptions<ReusedTestDatabaseOptions> options;
    private TestDbMeta? meta;
    private bool wasRebuilt = false;

    public ReusedTestDatabase
    (
        TestUmbracoDatabaseFactoryProvider databaseFactoryProvider,
        ILoggerFactory loggerFactory,
        IOptions<ReusedTestDatabaseOptions> options
    )
    {
        this.databaseFactoryProvider = databaseFactoryProvider;
        this.loggerFactory = loggerFactory;
        this.options = options;
        _databaseFactory = databaseFactoryProvider.Create();
        _loggerFactory = loggerFactory;

        InitializeMetadata();
    }

    public TestDbMeta EnsureDatabase()
    {
        lock (syncRoot)
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

    public override TestDbMeta AttachEmpty() => AttachSchema();

    public override TestDbMeta AttachSchema()
    {
        return EnsureDatabase();
    }

    public override void Detach(TestDbMeta id)
    {
        // File based SQLite database is reused between tests.
    }

    protected override void Initialize()
    {
        if (meta != null)
        {
            return;
        }

        InitializeMetadata();
    }

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

    protected override DbConnection GetConnection(TestDbMeta meta) => new SqliteConnection(meta.ConnectionString);

    protected override void RebuildSchema(IDbCommand command, TestDbMeta meta)
    {
        // Schema rebuilding handled by RebuildDatabaseFile. The base implementation is unused for this database.
    }

    protected override void ResetTestDatabase(TestDbMeta meta)
    {
        // Reset is not required as we reuse the same file between runs.
    }

    public override void TearDown()
    {
        // Nothing to dispose. The database file is intentionally preserved between runs.
    }

    private void RebuildWithSchema()
    {
        var databaseDirectory = Path.GetDirectoryName(meta!.Path)!;
        var databasePath = meta.Path!;
        Directory.CreateDirectory(databaseDirectory);
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
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