using Microsoft.Extensions.Logging;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerFixture,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ServiceProvider]
public abstract class ScopedUmbracoIntegrationTest : UmbracoIntegrationTest
{
}
