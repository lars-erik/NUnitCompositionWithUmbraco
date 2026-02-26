using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Cms.Tests.Integration.TestServerTest;

namespace Umbraco.Community.Integration.Tests.Extensions.Database;

public class ReusableDatabaseAttribute : Attribute, IApplyToTest
{
    private readonly Type? configureOptionsType;
    private readonly string configureOptionsMethodName;

    public ReusableDatabaseAttribute(Type configureOptionsType, string configureOptionsMethodName)
    {
        this.configureOptionsType = configureOptionsType;
        this.configureOptionsMethodName = configureOptionsMethodName;
    }

    public ReusableDatabaseAttribute(string configureOptionsMethodName)
    {
        this.configureOptionsMethodName = configureOptionsMethodName;
    }

    public void ApplyToTest(Test test)
    {
        var type = test?.TypeInfo?.Type;
        if (test is not TestSuite suite || type == null)
        {
            return;
        }
        
        // TODO: Verify we are using a proxy?
        if (test is IExtendableLifecycle extendable && type.IsAssignableTo(typeof(UmbracoIntegrationTestBase)))
        {
            extendable.AddInterceptor(new ConfigureReusableDbInterceptor(configureOptionsType ?? type, configureOptionsMethodName));

            if (extendable.TypeInfo!.Type.IsAssignableTo(typeof(UmbracoIntegrationTest)))
            {
                extendable.AddPostHandler(nameof(UmbracoIntegrationTest.Setup), () => EnsureSeeded(extendable.Fixture!));
            }
            else if (extendable.TypeInfo!.Type.IsAssignableTo(typeof(UmbracoTestServerTestBase)))
            {
                try
                {
                    extendable.AddPostHandler(
                        extendable.OneTimeSetUpMethods.Single(x => x.Name == nameof(UmbracoTestServerTestBase.Setup) && x.TypeInfo.Type.IsAssignableTo(typeof(UmbracoTestServerTestBase))), 
                        () => EnsureSeeded(extendable.Fixture!)
                    );
                }
                catch (Exception e)
                {
                    throw new Exception("Didn't find, or found multiple methods named Setup that aren't in an override chain.", e);
                }
            }

            var cleanupMethod = new DelegateMethodWrapper(test.TypeInfo!.Type, GetType(), nameof(RemoveStaticDbInstance));
            extendable.OneTimeTearDownMethods = extendable.OneTimeTearDownMethods.Concat([cleanupMethod]).ToArray();
        }
        else
        {
            throw new Exception($"{nameof(ReusableDatabaseAttribute)} may only be applied to fixtures derived from {nameof(UmbracoIntegrationTestBase)}");
        }
    }

    private void EnsureSeeded(object integrationTest)
    {
        var services = (IServiceProvider)integrationTest.GetType().GetProperty("Services", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(integrationTest)!;
        var testDatabase = services.GetRequiredService<IReusableTestDatabase>();
        testDatabase.EnsureSeeded(services).GetAwaiter().GetResult();
    }

    public static void RemoveStaticDbInstance()
    {
        TestContext.Progress.WriteLine("Resetting dbinstance field");
        typeof(UmbracoIntegrationTestBase).GetField("_dbInstance", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, null);
    }
}