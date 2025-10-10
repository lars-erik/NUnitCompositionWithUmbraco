using NUnit.Framework.Internal;
using Umbraco.Cms.Tests.Integration.Testing;
using UmbracoTestsComposition.FeatureA;

namespace UmbracoTestsComposition.Common;

public abstract class ScopedUmbracoIntegrationSetupFixture : UmbracoIntegrationTest
{
    private static ScopedUmbracoIntegrationSetupFixture? instance = null;

    protected static T? GetInstance<T>()
        where T : ScopedUmbracoIntegrationSetupFixture<T>
    {
        return instance as T;
    }

    private protected ScopedUmbracoIntegrationSetupFixture()
    {
        instance = this;
    }

    ~ScopedUmbracoIntegrationSetupFixture()
    {
        instance = null;
    }
}

public abstract class ScopedUmbracoIntegrationSetupFixture<T> : ScopedUmbracoIntegrationSetupFixture
    where T : ScopedUmbracoIntegrationSetupFixture<T>
{
    public static T Instance
    {
        get
        {
            var typedInstance = GetInstance<T>();

            if (typedInstance == null)
            {
                // TODO: Just check if the attribute is there and tell about it.
                throw new Exception("There's no instance for this setup fixture yet. Did you add [ScopedSetupFixture] to the scope fixture?");
            }

            return typedInstance!;
        }
    }

    public new IServiceProvider Services => base.Services;

    protected ScopedUmbracoIntegrationSetupFixture(string setupMethod)
    {
        var method = GetType().GetMethod(setupMethod);
        if (method == null || method.DeclaringType != GetType())
        {
            throw new Exception($"The setup method ({setupMethod}) has to be declared on the setup fixture ({GetType().Name} itself. It cannot be a derived one.");
        }

        var executionContext = TestExecutionContext.CurrentContext;
        var methodInfo = GetType().GetMethod(setupMethod)!;
        executionContext.CurrentTest = new TestMethod(new MethodWrapper(GetType(), methodInfo));
    }
}