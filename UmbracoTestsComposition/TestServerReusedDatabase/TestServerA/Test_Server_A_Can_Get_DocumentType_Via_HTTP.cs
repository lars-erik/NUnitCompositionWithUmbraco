using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Api.Management.ViewModels.DocumentType;

namespace UmbracoTestsComposition.TestServerReusedDatabase.TestServerA;

[Inject(nameof(Inject))]
public class Test_Server_A_Can_Get_DocumentType_Via_HTTP
{
    private HttpClient client = null!;

    public void Inject(IServiceProvider provider)
    {
        client = provider.GetKeyedService<HttpClient>("TestServerClient")!;
    }

    [Test]
    public async Task And_Read_The_Results()
    {
        var response = await client.GetAsync("/umbraco/management/api/v1/document-type/" + TestServerReusedDatabaseSampleSetUpBase.TestDocumentTypeId);
        AssertSuccess(response);
        var json = await response.Content.ReadAsStreamAsync();
        var jsonObject = await JsonNode.ParseAsync(json);
        jsonObject!.AsObject().TryGetPropertyValue("alias", out var aliasNode);
        Assert.That(aliasNode?.GetValue<string>(), Is.EqualTo(TestServerReusedDatabaseSampleSetUpBase.TestDocumentTypeAlias));
    }

    [Test]
    public async Task Write_And_Hope_It_Is_Rolled_Back()
    {
        var updateModel = new UpdateDocumentTypeRequestModel
        {
            Alias = TestServerReusedDatabaseSampleSetUpBase.TestDocumentTypeAlias,
            Name = "Updated name",
            Icon = "icon-documenttype"
        };
        var putResponse = await client.PutAsJsonAsync("/umbraco/management/api/v1/document-type/" + TestServerReusedDatabaseSampleSetUpBase.TestDocumentTypeId, updateModel);
        AssertSuccess(putResponse);

        var response = await client.GetAsync("/umbraco/management/api/v1/document-type/" + TestServerReusedDatabaseSampleSetUpBase.TestDocumentTypeId);
        AssertSuccess(response);
        var json = await response.Content.ReadAsStreamAsync();
        var jsonObject = await JsonNode.ParseAsync(json);
        Assert.That(jsonObject["name"].GetValue<string>(), Is.EqualTo("Updated name"));
    }

    private static void AssertSuccess(HttpResponseMessage response)
    {
        Assert.That(() => response.EnsureSuccessStatusCode(), Throws.Nothing, () => $"{response.StatusCode}: {response.ReasonPhrase}\n{response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
    }
}
