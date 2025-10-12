using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common;

public static class UmbracoTestExtensions
{
    public static void ExposeUmbracoTestAttribute(this UmbracoIntegrationTestBase umbracoTest, string stubMethodName)
    {
        var executionContext = TestExecutionContext.CurrentContext;
        var methodInfo = new MethodWrapper(umbracoTest.GetType(), stubMethodName);
        executionContext.CurrentTest = new TestMethod(methodInfo);
    }
}