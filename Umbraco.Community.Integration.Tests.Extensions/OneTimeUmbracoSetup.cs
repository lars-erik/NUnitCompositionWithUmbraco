using NUnitComposition.Lifecycle;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Community.Integration.Tests.Extensions;

public class OneTimeUmbracoSetUpAttribute() : MakeOneTimeLifecycleAttribute(
    [nameof(UmbracoIntegrationTest.Setup), nameof(UmbracoIntegrationTest.SetUp_Logging)],
    [nameof(UmbracoIntegrationTest.TearDown), nameof(UmbracoIntegrationTest.TearDownAsync), nameof(UmbracoIntegrationTest.FixtureTearDown), nameof(UmbracoIntegrationTest.TearDown_Logging)]
)
{
}