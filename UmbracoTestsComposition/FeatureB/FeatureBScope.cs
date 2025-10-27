using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoTestsComposition.Common;

namespace UmbracoTestsComposition.FeatureB;

public class FeatureBScope : ScopedUmbracoIntegrationTest
{
    public static readonly Guid DataTypeId = new Guid("215cdc52-4225-40d8-9c9a-36c560d4de7c");

    [OneTimeSetUp]
    public async Task CreateDataTypeForScope()
    {
        var configSerializer = Services.GetRequiredService<IConfigurationEditorJsonSerializer>();
        var dataTypeService = Services.GetRequiredService<IDataTypeService>();
        var editorCollection = Services.GetRequiredService<DataEditorCollection>();
        var textBoxEditor = editorCollection.Single(x => x.Alias == Constants.PropertyEditors.Aliases.TextBox);

        var result = await dataTypeService.CreateAsync(
            new DataType(textBoxEditor, configSerializer)
            {
                Key = DataTypeId,
                Name = "A test datatype"
            },
            Constants.Security.SuperUserKey);

        Assert.That(result.Success, Is.True, () => $"Failed with status {result.Status} and exception message {result.Exception?.Message ?? "<No exception thrown>"}");
    }
}