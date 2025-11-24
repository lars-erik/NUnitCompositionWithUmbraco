using Microsoft.Extensions.DependencyInjection;
using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.ReusedDatabaseByAttribute;

[Inject(nameof(Inject))]
public class ReusedDbByAttributeFixture
{
    private IDataTypeService dataTypeService;

    public void Inject(IDataTypeService dataTypeService)
    {
        this.dataTypeService = dataTypeService;
    }

    [Test]
    public async Task CanReadData()
    {
        var dataTypes = await dataTypeService.GetAllAsync();
        Assert.That(dataTypes.ToList(), Has.Count.GreaterThan(0));
    }
}
