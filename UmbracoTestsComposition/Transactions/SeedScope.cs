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
[OneTimeUmbracoSetup]
[InjectionProvider(nameof(Services))]
public class SeedScope : UmbracoIntegrationTest
{
    private int dataTypes;

    public SeedScope()
    {
        this.ExposeUmbracoTestAttribute(nameof(CountDataTypes));
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddUmbracoManagementApi();
    }

    [OneTimeSetUp]
    public async Task CountDataTypes()
    {
        dataTypes = (await GetRequiredService<IDataTypeService>().GetAllAsync()).Count();
    }

    [OneTimeTearDown]
    public async Task VerifySameCountOfDataTypes()
    {
        var currentDataTypes = (await GetRequiredService<IDataTypeService>().GetAllAsync()).Count();
        Assert.That(dataTypes, Is.EqualTo(currentDataTypes));
    }
}