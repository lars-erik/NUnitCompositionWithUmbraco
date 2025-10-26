using NUnitComposition.DependencyInjection;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using UmbracoTestsComposition.Common;

namespace UmbracoTestsComposition.FeatureA;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerFixture, 
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[InjectionProvider(nameof(Services))]
public class FeatureAScope : UmbracoIntegrationTest
{
}