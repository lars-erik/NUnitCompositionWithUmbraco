using NUnitComposition.DependencyInjection;
using NUnitComposition.Extensibility;
using NUnitComposition.Lifecycle;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using UmbracoTestsComposition.Common;

namespace UmbracoTestsComposition.FeatureA;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerFixture, 
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[MakeOneTimeLifecycle(
    [nameof(Setup), nameof(SetUp_Logging)],
    [nameof(TearDown), nameof(TearDownAsync), nameof(FixtureTearDown), nameof(TearDown_Logging)]
)]
[InjectionProvider(nameof(Services))]
public class FeatureAScope : UmbracoIntegrationTest
{
    // TODO: Could we possibly add this using Castle.DynamicProxy or something?
    public void StubForUmbracoTestDiscovery() {}

    public FeatureAScope()
    {
        // TODO: Figure out how to avoid having to do this in each setup fixture.
        // Umbraco's TestOptionAttributeBase looks for the UmbracoTest attribute via the current test method or its declaring type.
        // We need to set a dummy test method from this exact type.
        // It could possibly be done by sneaking it in to the first instance of onetime setups, but we still need a declared method on this type.
        this.ExposeUmbracoTestAttribute(nameof(StubForUmbracoTestDiscovery));
    }
}