using NUnit.Framework.Internal;
using NUnitComposition.DependencyInjection;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using UmbracoTestsComposition.Common;

namespace UmbracoTestsComposition.Transactions;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerFixture,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ServiceProvider]
public class SeedScope : UmbracoIntegrationTest
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