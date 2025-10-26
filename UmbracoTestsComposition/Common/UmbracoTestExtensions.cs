using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnitComposition.Lifecycle;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common;

public static class UmbracoTestExtensions
{
    public static void ExposeUmbracoTestAttribute(this UmbracoIntegrationTestBase umbracoTest, string stubMethodName)
    {
        var executionContext = TestExecutionContext.CurrentContext;
        var currentTest = TestExecutionContext.CurrentContext.CurrentTest;
        var methodInfo = new MethodWrapper(umbracoTest.GetType(), stubMethodName);
        var setupMethodWrapper = new LifeCycleTestMethod(methodInfo, currentTest)
        {
            Parent = currentTest
        };
        ((TestSuite)currentTest).Add(setupMethodWrapper);
        executionContext.CurrentTest = setupMethodWrapper;
    }
}
