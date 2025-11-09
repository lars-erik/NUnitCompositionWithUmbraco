using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using System.Diagnostics;
using System.Reflection;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Security;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Integration.Testing;
using static Umbraco.Cms.Infrastructure.Persistence.UmbracoDatabase;

namespace UmbracoTestsComposition.Common.Database;

public class ReusedSqlServerTestDatabase : IReusableTestDatabase
{
    private const string DatabaseName = "reused-database";

    private readonly Lock lockObj = new();
    private readonly TestUmbracoDatabaseFactoryProvider databaseFactoryProvider;
    private readonly IUmbracoDatabaseFactory databaseFactory;
    private readonly TestDatabaseSettings settings;
    private readonly ILoggerFactory loggerFactory;
    private readonly ReusedTestDatabaseOptions options;
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
        this.options = options.Value;
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
                wasRebuilt = true;
            }

            return meta!;
        }
    }

    public async Task EnsureSeeded(IServiceProvider serviceProvider)
    {
        var shouldSeed = wasRebuilt || await (options.NeedsNewSeed?.Invoke(meta!) ?? Task.FromResult(false));
        if (shouldSeed)
        {
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

            await (options.SeedData?.Invoke(serviceProvider) ?? Task.CompletedTask);

            await CreateSnapshot();
        }
        else
        {
            await RestoreSnapshot();
        }
    }

    private async Task CreateSnapshot()
    {
        var watch = new Stopwatch();
        watch.Start();
        await TestContext.Progress.WriteLineAsync("Creating snapshot");

        await using var cn = new SqlConnection(settings.SQLServerMasterConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        var snapshotPath = Path.GetFullPath($"{DatabaseName}-snapshot.ss", options.WorkingDirectory);
        cmd.CommandText =
            $"""
             CREATE DATABASE [{DatabaseName}-snapshot]
             ON (NAME = [{DatabaseName}], FILENAME = '{snapshotPath}')
             AS SNAPSHOT OF [{DatabaseName}]
             """;

        await cmd.ExecuteNonQueryAsync();

        await TestContext.Progress.WriteLineAsync($"Snapshot created {watch.Elapsed}");
    }

    public async Task RestoreSnapshot()
    {
        var watch = new Stopwatch();
        watch.Start();
        await TestContext.Progress.WriteLineAsync("Restoring snapshot");

        await using var cn = new SqlConnection(settings.SQLServerMasterConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = 
            $"""
             ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
             RESTORE DATABASE [{DatabaseName}] FROM DATABASE_SNAPSHOT = '{DatabaseName}-snapshot';
             ALTER DATABASE [{DatabaseName}] SET MULTI_USER;
             """;
        await cmd.ExecuteNonQueryAsync();

        await TestContext.Progress.WriteLineAsync($"Snapshot restored {watch.Elapsed}");
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
        var needsRebuild = !exists || (options.NeedsNewSeed?.Invoke(meta!).GetAwaiter().GetResult() ?? false);
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
            try
            {
                cmd.CommandText = $"ALTER DATABASE [{DatabaseName}-snapshot] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // We'll check if it exists later
            }

            try
            {
                cmd.CommandText = $"DROP DATABASE [{DatabaseName}-snapshot]";
                cmd.ExecuteNonQuery();
            }
            catch
            {

            }

            try
            {
                cmd.CommandText = $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                cmd.ExecuteNonQuery();
            }
            catch
            {
            }
            cmd.CommandText = $"DROP DATABASE [{DatabaseName}]";
            cmd.ExecuteNonQuery();
        }

        cmd.CommandText = $"CREATE DATABASE [{DatabaseName}]";
        cmd.ExecuteNonQuery();
        cn.Close();

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
