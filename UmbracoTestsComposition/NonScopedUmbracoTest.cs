using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using UmbracoTestsComposition.FeatureA;

namespace UmbracoTestsComposition;

[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
public class NonScopedUmbracoTest : UmbracoIntegrationTest
{
    [SetUp]
    public void InstanceSetup()
    {
        Console.WriteLine("Setting up just like we're used to.");
    }

    [Test]
    public async Task NonScopedTest()
    {
        var dataTypeService = GetRequiredService<IDataTypeService>();
        var allTypes = await dataTypeService.GetAllAsync().ToAsyncEnumerable().ToListAsync();
        Assert.That(allTypes, Has.Count.GreaterThan(0));
    }
}