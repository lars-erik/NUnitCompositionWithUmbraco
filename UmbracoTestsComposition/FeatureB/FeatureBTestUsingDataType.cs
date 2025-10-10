using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.FeatureB;

public class FeatureBTestUsingDataType
{
    [Test]
    public async Task Get_The_Scoped_DataType()
    {
        var dataTypeService = FeatureBScope.Instance!.Services.GetRequiredService<IDataTypeService>();
        var dataType = await dataTypeService.GetAsync(FeatureBScope.DataTypeId);
        Assert.That(dataType, Is.Not.Null, "Didn't manage to get the datatype");
        Console.WriteLine($"We've got the data type: {dataType!.Name} - {dataType!.EditorAlias}");
    }
}