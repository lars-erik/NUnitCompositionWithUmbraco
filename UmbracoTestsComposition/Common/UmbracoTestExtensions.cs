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
        var currentTest = executionContext.CurrentTest;
        var methodInfo = new MethodWrapper(umbracoTest.GetType(), stubMethodName);
        if (currentTest is Test originalTest)
        {
            // The fixtures call this during construction so the original suite node remains discoverable
            // while the base one-time setup executes under a stub method that Umbraco can interrogate.
            ContextRestoringMethodWrapper.RememberOriginalTest(methodInfo.MethodInfo, originalTest);
        }
        var setupMethodWrapper = new LifeCycleTestMethod(methodInfo, currentTest)
        {
            Parent = currentTest
        };

        ((TestSuite)currentTest).Add(setupMethodWrapper);
        executionContext.CurrentTest = setupMethodWrapper;
    }
}

public class LifeCycleTestMethod : TestMethod
{
    public LifeCycleTestMethod(IMethodInfo method) : base(method)
    {
    }

    public LifeCycleTestMethod(IMethodInfo method, Test? parentSuite) : base(method, parentSuite)
    {
    }

    public override string TestType => nameof(SetUpFixture);

    public override string XmlElementName => "test-suite";
}
