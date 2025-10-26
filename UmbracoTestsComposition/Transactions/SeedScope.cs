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
[OneTimeUmbracoSetup]
[InjectionProvider(nameof(Services))]
public class SeedScope : UmbracoIntegrationTest
{
    private int dataTypes;
    private readonly TestExecutionContext? originalContext;
    private readonly Test? originalTest;
    private readonly TestExecutionContext? currentContext;
    private readonly Test? currentTest;

    public SeedScope()
    {
        originalContext = TestExecutionContext.CurrentContext;
        originalTest = originalContext.CurrentTest;
        this.ExposeUmbracoTestAttribute(nameof(CountDataTypes));
        currentContext = TestExecutionContext.CurrentContext;
        currentTest = originalContext.CurrentTest;
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddUmbracoManagementApi();
    }

    [OneTimeSetUp]
    public async Task CountDataTypes()
    {
        TestExecutionContext.CurrentContext.CurrentTest = originalTest;

        await TestContext.Progress.WriteLineAsync("WHY DOESN'T THIS GET TO LOG TO PROGRESS?");

        dataTypes = (await GetRequiredService<IDataTypeService>().GetAllAsync()).Count();
    }

    [OneTimeTearDown]
    public async Task VerifySameCountOfDataTypes()
    {
        var currentDataTypes = (await GetRequiredService<IDataTypeService>().GetAllAsync()).Count();
        Assert.That(dataTypes, Is.EqualTo(currentDataTypes));
    }
}