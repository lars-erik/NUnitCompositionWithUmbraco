using NUnit.Framework.Internal;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common;

public abstract class ScopedUmbracoIntegrationSetupFixture : UmbracoIntegrationTest
{
    protected ScopedUmbracoIntegrationSetupFixture(string setupMethod)
    {
        var executionContext = TestExecutionContext.CurrentContext;
        var methodInfo = GetType().GetMethod(setupMethod)!;
        executionContext.CurrentTest = new TestMethod(new MethodWrapper(GetType(), methodInfo));
    }
}