using Microsoft.Extensions.DependencyInjection;
using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core.Models;
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
        var dataTypes = (await dataTypeService.GetAllAsync()).ToArray();
        Assert.That(dataTypes, Has.Length.GreaterThan(0));
        Assert.That(dataTypes, Has.One.With.Property(nameof(IDataType.Name)).EqualTo("A seeded textbox"));
    }
}
