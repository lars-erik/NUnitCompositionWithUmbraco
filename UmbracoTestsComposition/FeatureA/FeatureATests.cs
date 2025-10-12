using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.FeatureA;

[Inject(nameof(Inject))]
public class FeatureATests
{
    private IDataTypeService dataTypeService = null!;

    public void Inject(IDataTypeService dataTypeService)
    {
        this.dataTypeService = dataTypeService;
    }

    [Test]
    public async Task CanGetDataTypeFromInjectedService()
    {
        var allTypes = (await dataTypeService.GetAllAsync()).Take(3).ToList();
        Console.WriteLine($"We've got data types like {String.Join(',', allTypes.Select(x => x.Name))}...");
        Assert.That(allTypes, Has.Count.GreaterThan(0));
    }
}