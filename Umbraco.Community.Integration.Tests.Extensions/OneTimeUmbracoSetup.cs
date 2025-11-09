using NUnitComposition.Lifecycle;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common;

public class OneTimeUmbracoSetUpAttribute() : MakeOneTimeLifecycleAttribute(
    [nameof(UmbracoIntegrationTest.Setup), nameof(UmbracoIntegrationTest.SetUp_Logging)],
    [nameof(UmbracoIntegrationTest.TearDown), nameof(UmbracoIntegrationTest.TearDownAsync), nameof(UmbracoIntegrationTest.FixtureTearDown), nameof(UmbracoIntegrationTest.TearDown_Logging)]
)
{
}