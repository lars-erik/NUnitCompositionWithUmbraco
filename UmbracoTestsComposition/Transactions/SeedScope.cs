using Umbraco.Cms.Core.Services;
using Umbraco.Community.Integration.Tests.Extensions;

namespace UmbracoTestsComposition.Transactions;

public class SeedScope : ScopedUmbracoIntegrationTest
{
    private int dataTypes;

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddUmbracoManagementApi();
    }

    [OneTimeSetUp]
    public async Task CountDataTypes()
    {
        await TestContext.Progress.WriteLineAsync("This gets logged to the progress output.");

        dataTypes = (await GetRequiredService<IDataTypeService>().GetAllAsync()).Count();
    }

    [OneTimeTearDown]
    public async Task VerifySameCountOfDataTypes()
    {
        var currentDataTypes = (await GetRequiredService<IDataTypeService>().GetAllAsync()).Count();
        Assert.That(dataTypes, Is.EqualTo(currentDataTypes));
    }
}