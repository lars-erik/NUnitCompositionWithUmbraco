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
public class FeatureAScope() : ScopedUmbracoIntegrationSetupFixture(nameof(ScopedSetup))
{
    private static FeatureAScope? instance;

    public static FeatureAScope Instance => instance!;

    public new IServiceProvider Services => base.Services;

    [SetUp]
    public void ScopedSetup()
    {
        UmbTestRoot.Log.Add($"{nameof(FeatureAScope)} {nameof(ScopedSetup)} called");

        instance = this;
    }

    [TearDown]
    public void ScopedTeardown()
    {
        instance = null;

        UmbTestRoot.Log.Add($"{nameof(FeatureAScope)} {nameof(ScopedTeardown)} called");
    }
}