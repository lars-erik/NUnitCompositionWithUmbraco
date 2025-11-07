using NUnit.Framework;
using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.ReusedDatabase.ReusedDatabaseB;

[Inject(nameof(Inject))]
public class ReusedDatabaseBTests
{
    private IContentTypeService contentTypeService = null!;
    private ICoreScopeProvider scopeProvider = null!;

    public void Inject(IContentTypeService contentTypeService, ICoreScopeProvider scopeProvider)
    {
        this.contentTypeService = contentTypeService;
        this.scopeProvider = scopeProvider;
    }

    [Test]
    public void DocumentTypeExists()
    {
        TestContext.Progress.WriteLine("[ReusedDatabaseB] Running DocumentTypeExists test.");
        using var scope = scopeProvider.CreateCoreScope(autoComplete: true);
        var contentType = contentTypeService.Get("reusedDatabaseDocType");
        Assert.That(contentType, Is.Not.Null, "The reused database should contain the seeded document type.");
    }
}
