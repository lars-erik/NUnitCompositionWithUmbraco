using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Persistence.Sqlite;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.ReusedDatabase;

public class ReusedDatabaseTestDatabase : BaseTestDatabase, ITestDatabase
{
    private readonly object syncRoot = new();
    private readonly TestUmbracoDatabaseFactoryProvider databaseFactoryProvider;
    private readonly ILoggerFactory loggerFactory;
    private TestDbMeta? meta;

    public ReusedDatabaseTestDatabase(
        TestUmbracoDatabaseFactoryProvider databaseFactoryProvider,
        ILoggerFactory loggerFactory)
    {
        this.databaseFactoryProvider = databaseFactoryProvider;
        this.loggerFactory = loggerFactory;
        _databaseFactory = databaseFactoryProvider.Create();
        _loggerFactory = loggerFactory;
    }

    public (TestDbMeta Meta, bool Created) EnsureDatabase()
    {
        lock (syncRoot)
        {
            Initialize();
            var needsRebuild = ReusedDatabaseStorage.ShouldRebuild(ReusedDatabaseSeed.SeedVersion);
            if (needsRebuild)
            {
                RebuildDatabaseFile();
                return (meta!, true);
            }

            return (meta!, false);
        }
    }

    public static void MarkDatabaseOutdated() => ReusedDatabaseStorage.MarkOutdated();

    public override TestDbMeta AttachEmpty() => AttachSchema();

    public override TestDbMeta AttachSchema()
    {
        return EnsureDatabase().Meta;
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

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = ReusedDatabaseStorage.DatabaseFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            Pooling = false,
            ForeignKeys = true
        };

        meta = new TestDbMeta(
            name: "ReusedDatabase",
            isEmpty: false,
            connectionString: builder.ConnectionString,
            providerName: Constants.ProviderName,
            path: ReusedDatabaseStorage.DatabaseFilePath);
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

    private void RebuildDatabaseFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReusedDatabaseStorage.DatabaseFilePath)!);
        if (File.Exists(ReusedDatabaseStorage.DatabaseFilePath))
        {
            File.Delete(ReusedDatabaseStorage.DatabaseFilePath);
        }

        var dbFactory = databaseFactoryProvider.Create();
        dbFactory.Configure(meta!.ToStronglyTypedConnectionString());

        using var database = (UmbracoDatabase)dbFactory.CreateDatabase();
        using var transaction = database.GetTransaction();

        var options = new TestOptionsMonitor<InstallDefaultDataSettings>(
            new InstallDefaultDataSettings { InstallData = InstallDefaultDataOption.All });

        var schemaCreator = new DatabaseSchemaCreator(
            database,
            loggerFactory.CreateLogger<DatabaseSchemaCreator>(),
            loggerFactory,
            new UmbracoVersion(),
            Mock.Of<IEventAggregator>(),
            options);

        schemaCreator.InitializeDatabaseSchema();
        transaction.Complete();

        ReusedDatabaseStorage.MarkSeeded(ReusedDatabaseSeed.SeedVersion);
    }
}
