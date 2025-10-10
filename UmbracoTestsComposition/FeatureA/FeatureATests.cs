using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.FeatureA;

public class FeatureATests
{
    [Test]
    public async Task Can_Use_Umbraco_From_The_Scoped_Setup()
    {
        var dataTypeService = FeatureAScope.Instance!.Services.GetRequiredService<IDataTypeService>();
        var allTypes = (await dataTypeService.GetAllAsync()).Take(3).ToList();
        Console.WriteLine($"We've got data types like {String.Join(',', allTypes.Select(x => x.Name))}...");
        Assert.That(allTypes, Has.Count.GreaterThan(0));
    }
}