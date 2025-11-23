using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Management.Controllers.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Community.Integration.Tests.Extensions.Database;

namespace UmbracoTestsComposition.TestServerReusedDatabase;

public abstract class TestServerReusedDatabaseSampleSetUpBase() : SeededUmbracoTestServerSetUpBase<BackOfficeController>(true)
{
    public const string TestDocumentTypeId = "c9e9dd58-7c5f-47fc-9788-78a9b6fbf68d";
    public const string TestDocumentTypeAlias = "reusedDatabaseDocType";
    protected static bool ReseedTrigger = true;

    protected override void ConfigureTestDatabaseOptions(ReusedTestDatabaseOptions options)
    {
        options.NeedsNewSeed = _ => Task.FromResult(ReseedTrigger);
        options.SeedData = async (_) =>
        {
            await SeedData();
            TestServerReusedDatabaseIsOnlySeededOnce.SeedCount++;
            ReseedTrigger = false;
        };
    }

    protected async Task SeedData()
    {
        var contentTypeService = Services.GetRequiredService<IContentTypeService>();
        var scopeProvider = Services.GetRequiredService<ICoreScopeProvider>();

        using var scope = scopeProvider.CreateCoreScope(autoComplete: true);

        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Creating seed document type 'reusedDatabaseDocType'.");
        var contentType = ContentTypeBuilder.CreateBasicContentType(TestDocumentTypeAlias, "Reused Database Document");
        contentType.Key = new(TestDocumentTypeId);
        contentType.AllowedAsRoot = true;

        var attempt = await contentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);

        if (!attempt.Success)
        {
            throw new InvalidOperationException($"Failed to create the reused database content type: {attempt.Result}");
        }

        scope.Complete();

        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Seed document type created successfully.");
    }
}
