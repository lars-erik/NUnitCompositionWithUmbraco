using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.FeatureA;

public class FeatureAMoreTests
{
    [Test]
    public async Task Can_Even_Mutate_Stuff_Here_Though_Not_Recommended()
    {
        var configSerializer = FeatureAScope.Instance.Services.GetRequiredService<IConfigurationEditorJsonSerializer>();
        var dataTypeService = FeatureAScope.Instance.Services.GetRequiredService<IDataTypeService>();
        var editorCollection = FeatureAScope.Instance.Services.GetRequiredService<DataEditorCollection>();
        var textBoxEditor = editorCollection.Single(x => x.Alias == Constants.PropertyEditors.Aliases.TextBox);
        var result = await dataTypeService.CreateAsync(
            new DataType(textBoxEditor, configSerializer)
            {
                Name = "A test datatype"
            }, 
            Constants.Security.SuperUserKey);

        Assert.That(result.Success, Is.True, () => $"Failed with status {result.Status} and exception message {result.Exception?.Message ?? "<No exception thrown>"}");
    }
}