using NUnit.Framework.Internal;
using NUnitComposition.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using UmbracoTestsComposition.Common;

namespace UmbracoTestsComposition.FeatureA;

[ScopedSetupFixture]
[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerFixture, 
    Logger = UmbracoTestOptions.Logger.Console
)]
public class FeatureAScope() : ScopedUmbracoIntegrationSetupFixture<FeatureAScope>(nameof(ScopedSetup))
{
    [SetUp]
    public void ScopedSetup()
    {
        UmbTestRoot.Log.Add($"{nameof(FeatureAScope)} {nameof(ScopedSetup)} called");
    }

    [TearDown]
    public void ScopedTeardown()
    {
        UmbTestRoot.Log.Add($"{nameof(FeatureAScope)} {nameof(ScopedTeardown)} called");
    }
}