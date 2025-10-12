using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.FeatureB;

[Inject(nameof(Inject))]
public class FeatureBTestUsingDataTypeFromSameInstance
{
    private IDataTypeService dataTypeService = null!;

    public void Inject(IDataTypeService dataTypeService)
    {
        this.dataTypeService = dataTypeService;
    }

    [Test]
    public async Task CanGetTheScopeCreatedDataTypeFromInjectedService()
    {
        var dataType = await dataTypeService.GetAsync(FeatureBScope.DataTypeId);
        Assert.That(dataType, Is.Not.Null, "Didn't manage to get the datatype");
        Console.WriteLine($"We've got the data type: {dataType!.Name} - {dataType!.EditorAlias}");
    }
}