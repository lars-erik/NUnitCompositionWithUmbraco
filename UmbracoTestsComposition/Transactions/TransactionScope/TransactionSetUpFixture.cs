using NUnitComposition.DependencyInjection;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Api.Management.Factories;
using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.Transactions.TransactionScope;

[SetUpFixture]
[Inject(nameof(Inject), nameof(SetUpAfterInjection))]
public class TransactionSetUpFixture
{
    public static readonly Guid DataTypeId = new("a14dd08f-57de-443a-9cb2-e0cf61c23016");

    private IDataTypeService dataTypeService = null!;
    private IDataTypePresentationFactory dataTypeFactory = null!;
#pragma warning disable NUnit1032
    private ICoreScope coreScope = null!;
#pragma warning restore NUnit1032

    public void Inject(
        ICoreScopeProvider coreScopeProvider,
        IDataTypePresentationFactory dataTypeFactory,
        IDataTypeService dataTypeService
    )
    {
        coreScope = coreScopeProvider.CreateCoreScope(
            // Isolation mode?
            // Others?
            repositoryCacheMode: RepositoryCacheMode.Scoped,
            autoComplete: false
        );

        this.dataTypeService = dataTypeService;
        this.dataTypeFactory = dataTypeFactory;
    }

    public async Task SetUpAfterInjection()
    {
        await CreateDataType();
    }

    [OneTimeSetUp]
    public void ThisIsExcutedBeforeInject()
    {
        // TODO: Figure out how to make Inject run before OneTimeSetUps.
    }

    //[OneTimeSetUp]

    [OneTimeTearDown]
    public void TearDown()
    {
        coreScope?.Dispose();
    }

    private async Task CreateDataType()
    {
        var createModel = await dataTypeFactory.CreateAsync(new CreateDataTypeRequestModel
        {
            Id = DataTypeId,
            Name = "A test datatype",
            EditorAlias = Constants.PropertyEditors.Aliases.TextBox
        });

        Assert.That(createModel.Success, Is.True, "Couldn't create the create model.");

        var result = await dataTypeService.CreateAsync(
            createModel.Result,
            Constants.Security.SuperUserKey
        );

        Assert.That(result.Success, Is.True, () => $"Failed with status {result.Status} and exception message {result.Exception?.Message ?? "<No exception thrown>"}");
    }
}