using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Integration.Testing;
using static Umbraco.Cms.Infrastructure.Persistence.UmbracoDatabase;

namespace UmbracoTestsComposition.Common.Database;

public class ReusedSqlServerTestDatabase : IReusableTestDatabase
{
    private const string DatabaseName = "reused-databases";

    private readonly Lock lockObj = new();
    private readonly TestUmbracoDatabaseFactoryProvider databaseFactoryProvider;
    private readonly IUmbracoDatabaseFactory databaseFactory;
    private readonly TestDatabaseSettings settings;
    private readonly ILoggerFactory loggerFactory;
    private readonly IOptions<ReusedTestDatabaseOptions> options;
    private TestDbMeta? meta;
    private bool wasRebuilt;
    
    protected UmbracoDatabase.CommandInfo[] cachedDatabaseInitCommands = new UmbracoDatabase.CommandInfo[0];

    public ReusedSqlServerTestDatabase(
        TestDatabaseSettings settings,
        TestUmbracoDatabaseFactoryProvider databaseFactoryProvider,
        ILoggerFactory loggerFactory,
        IOptions<ReusedTestDatabaseOptions> options
    )
    {
        this.databaseFactoryProvider = databaseFactoryProvider;
        this.databaseFactory = databaseFactoryProvider.Create();
        this.settings = settings;
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
        meta = TestDbMeta.CreateWithMasterConnectionString(DatabaseName, false, settings.SQLServerMasterConnectionString);
    }
    
    public bool ShouldRebuild()
    {
        using var cn = new SqlConnection(settings.SQLServerMasterConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sysdatabases WHERE name = @name";
        cmd.Parameters.AddWithValue("name", DatabaseName);
        var exists = 1.Equals(cmd.ExecuteScalar());
        var needsRebuild = !exists || (options.Value.NeedsNewSeed?.Invoke(meta!).GetAwaiter().GetResult() ?? false);
        return needsRebuild;
    }

    private static readonly PropertyInfo LogCommandsProperty = typeof(UmbracoDatabase).GetProperty("LogCommands", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly PropertyInfo CommandsProperty = typeof(UmbracoDatabase).GetProperty("Commands", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private void RebuildWithSchema()
    {
        using var cn = new SqlConnection(settings.SQLServerMasterConnectionString);
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sysdatabases WHERE name = @name";
        cmd.Parameters.AddWithValue("name", DatabaseName);
        var exists = 1.Equals(cmd.ExecuteScalar());
        if (exists)
        {
            cmd.CommandText = $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
            cmd.ExecuteNonQuery();
            cmd.CommandText = $"DROP DATABASE [{DatabaseName}]";
            cmd.ExecuteNonQuery();
        }

        cmd.CommandText = $"CREATE DATABASE [{DatabaseName}]";
        cmd.ExecuteNonQuery();
        cmd.CommandText = $"ALTER DATABASE [{DatabaseName}] SET READ_COMMITTED_SNAPSHOT ON";
        cmd.ExecuteNonQuery();

        databaseFactory.Configure(meta.ToStronglyTypedConnectionString());

        using (var database = (UmbracoDatabase)databaseFactory.CreateDatabase())
        {
            LogCommandsProperty.SetValue(database, true);

            using (var transaction = database.GetTransaction())
            {
                var options =
                    new TestOptionsMonitor<InstallDefaultDataSettings>(
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

                cachedDatabaseInitCommands = ((IEnumerable<CommandInfo>)CommandsProperty.GetValue(database))
                    .Where(x => !x.Text.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
        }
    }

}
