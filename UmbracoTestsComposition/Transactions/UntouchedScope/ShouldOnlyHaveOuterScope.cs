using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core.Services;
using UmbracoTestsComposition.Transactions.TransactionScope;

namespace UmbracoTestsComposition.Transactions.UntouchedScope;

[Inject(nameof(Inject))]
public class ShouldOnlyHaveOuterScope
{
    private IDataTypeService dataTypeService;

    public void Inject(IDataTypeService dataTypeService)
    {
        this.dataTypeService = dataTypeService;
    }

    [Test]
    public async Task DoesNotHaveDataType()
    {
        var dataType = await dataTypeService.GetAsync(TransactionSetUpFixture.DataTypeId);
        Assert.That(dataType, Is.Null, "Should never get the datatype from the other transaction scope");
    }
}