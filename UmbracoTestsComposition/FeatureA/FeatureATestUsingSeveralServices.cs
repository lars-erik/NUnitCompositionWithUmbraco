using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.FeatureA;

[Inject(nameof(Inject))]
public class FeatureATestUsingSeveralServices
{
    private IDataTypeService dataTypeService = null!;
    private DataEditorCollection editorCollection = null!;
    private IConfigurationEditorJsonSerializer editorSerializer = null!;

    public void Inject(IDataTypeService dataTypeService, DataEditorCollection editorCollection, IConfigurationEditorJsonSerializer editorSerializer)
    {
        this.dataTypeService = dataTypeService;
        this.editorCollection = editorCollection;
        this.editorSerializer = editorSerializer;
    }

    [Test]
    public async Task CanCreateDataTypeUsingAllNecessaryServices()
    {
        var textBoxEditor = editorCollection.Single(x => x.Alias == Constants.PropertyEditors.Aliases.TextBox);
        var result = await dataTypeService.CreateAsync(
            new DataType(textBoxEditor, editorSerializer)
            {
                Name = "A test datatype"
            }, 
            Constants.Security.SuperUserKey);

        Assert.That(result.Success, Is.True, () => $"Failed with status {result.Status} and exception message {result.Exception?.Message ?? "<No exception thrown>"}");
    }
}