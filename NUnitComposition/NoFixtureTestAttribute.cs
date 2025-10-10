using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace NUnitComposition
{
    /// <summary>
    /// Custom test attribute that does NOT imply a fixture, unlike NUnit's [Test].
    /// Implements ITestBuilder so NUnit recognizes it as a test method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NoFixtureTestAttribute : NUnitAttribute, ITestBuilder
    {
        public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
        {
            yield return new NUnitTestCaseBuilder().BuildTestMethod(method, suite, null);
        }
    }
}
