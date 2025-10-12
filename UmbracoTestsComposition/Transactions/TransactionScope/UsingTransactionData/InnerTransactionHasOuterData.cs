using NUnitComposition.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace UmbracoTestsComposition.Transactions.TransactionScope.UsingTransactionData;

[Inject(nameof(Inject))]
public class InnerTransactionHasOuterData
{
    private IDataTypeService dataTypeService = null!;

    public void Inject(IDataTypeService dataTypeService)
    {
        this.dataTypeService = dataTypeService;
    }

    [Test]
    public async Task HasDataType()
    {
        var dataType = await dataTypeService.GetAsync(TransactionSetUpFixture.DataTypeId);
        Assert.That(dataType, Is.Not.Null, "Should have the data type in the same scope");
    }

}