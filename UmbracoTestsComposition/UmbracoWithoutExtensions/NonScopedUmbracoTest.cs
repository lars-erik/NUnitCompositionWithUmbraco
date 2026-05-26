using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.UmbracoWithoutExtensions;

[Description("This is here just to show one lonesome test that has to derive directly from UmbracoIntegrationTest")]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
public class NonScopedUmbracoTest : UmbracoIntegrationTest
{
    [Test]
    public async Task GetsServicesFromBase()
    {
        var dataTypeService = GetRequiredService<IDataTypeService>();
        var allTypes = await dataTypeService.GetAllAsync();
        Assert.That(allTypes.ToList(), Has.Count.GreaterThan(0));
    }
}