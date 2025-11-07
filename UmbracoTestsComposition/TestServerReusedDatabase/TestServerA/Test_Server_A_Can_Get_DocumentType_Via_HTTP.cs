using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using NUnitComposition.DependencyInjection;

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
        Assert.That(() => response.EnsureSuccessStatusCode(), Throws.Nothing, () => $"{response.StatusCode}: {response.ReasonPhrase}\n{response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
        var json = await response.Content.ReadAsStreamAsync();
        var jsonObject = await JsonNode.ParseAsync(json);
        jsonObject!.AsObject().TryGetPropertyValue("alias", out var aliasNode);
        Assert.That(aliasNode?.GetValue<string>(), Is.EqualTo(TestServerReusedDatabaseSampleSetUpBase.TestDocumentTypeAlias));
    }
}
