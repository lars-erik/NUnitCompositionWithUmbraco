using Lucene.Net.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnitComposition.Extensibility;
using NUnitComposition.Lifecycle;
using System.Linq.Expressions;
using Umbraco.Cms.Api.Management.Controllers.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.ManagementApi;
using Umbraco.Community.Integration.Tests.Extensions;
using Umbraco.Community.Integration.Tests.Extensions.Database;

namespace UmbracoTestsComposition.TestServerReusedDatabase;

[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture, Boot = true, Logger = UmbracoTestOptions.Logger.Serilog)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[MakeOneTimeLifecycle(tearDownNames:[nameof(TearDownClient)])]
[ServiceProvider]
[ReusableDatabase(nameof(ConfigureTestDatabaseOptions))]
public abstract class TestServerReusedDatabaseSampleSetUpBase : ManagementApiTest<BackOfficeController>
{
    public const string TestDocumentTypeId = "c9e9dd58-7c5f-47fc-9788-78a9b6fbf68d";
    public const string TestDocumentTypeAlias = "reusedDatabaseDocType";
    protected static bool ReseedTrigger = true;

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddKeyedTransient<HttpClient>("TestServerClient", (_, _) =>
        {
            var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost/", UriKind.Absolute),
            });
            AddOnFixtureTearDown(() => client.Dispose());
            AuthenticateClientAsync(client, "admin@example.com", "adminadminadmin", true).GetAwaiter().GetResult();
            TestContext.Progress.WriteLine($"Authenticated client with header {client.DefaultRequestHeaders.Authorization}");
            return client;
        });
    }

    protected static void ConfigureTestDatabaseOptions(ReusableTestDatabaseOptions options)
    {
        options.NeedsNewSeed = _ => Task.FromResult(ReseedTrigger);
        options.SeedData = async (services) =>
        {
            await SeedData(services);
            TestServerReusedDatabaseIsOnlySeededOnce.SeedCount++;
            ReseedTrigger = false;
        };
    }

    protected static async Task SeedData(IServiceProvider services)
    {
        var contentTypeService = services.GetRequiredService<IContentTypeService>();
        var scopeProvider = services.GetRequiredService<ICoreScopeProvider>();

        using var scope = scopeProvider.CreateCoreScope(autoComplete: true);

        await TestContext.Progress.WriteLineAsync($"[{typeof(TestServerReusedDatabaseSampleSetUpBase)}] Creating seed document type 'reusedDatabaseDocType'.");
        var contentType = ContentTypeBuilder.CreateBasicContentType(TestDocumentTypeAlias, "Reused Database Document");
        contentType.Key = new(TestDocumentTypeId);
        contentType.AllowedAsRoot = true;

        var attempt = await contentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);

        if (!attempt.Success)
        {
            throw new InvalidOperationException($"Failed to create the reused database content type: {attempt.Result}");
        }

        scope.Complete();

        await TestContext.Progress.WriteLineAsync($"[{typeof(TestServerReusedDatabaseSampleSetUpBase)}] Seed document type created successfully.");
    }

    protected override Expression<Func<BackOfficeController, object>> MethodSelector
    {
        get => _ => _.Token();
        set => throw new NotImplementedException();
    }
}
