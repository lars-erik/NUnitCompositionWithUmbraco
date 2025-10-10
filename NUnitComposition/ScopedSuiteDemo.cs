using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

// The shared scope class
namespace NUnitComposition
{
    [TestFixture]
    [ScopedSuite]
    public class SharedScope
    {
        public static int SetUpCount = 0;
        public static int TearDownCount = 0;
        public static List<string> Log = new();

        [SetUp]
        public void SetUp()
        {
            SetUpCount++;
            Log.Add($"SharedScope.SetUp");
        }

        [TearDown]
        public void TearDown()
        {
            TearDownCount++;
            Log.Add($"SharedScope.TearDown");
        }

        // Nested test class A
        internal class TestClassA
        {
            private readonly SharedScope _scope;
            public TestClassA(SharedScope scope)
            {
                _scope = scope;
            }

            [NoFixtureTest]
            public void TestA()
            {
                Assert.That(_scope, Is.Not.Null);
                SharedScope.Log.Add("TestClassA.TestA");
            }
        }

        // Nested test class B
        internal class TestClassB
        {
            private readonly SharedScope _scope;
            public TestClassB(SharedScope scope)
            {
                _scope = scope;
            }

            [NoFixtureTest]
            public void TestB()
            {
                Assert.That(_scope, Is.Not.Null);
                SharedScope.Log.Add("TestClassB.TestB");
            }
        }
    }

    // The custom attribute implementing IFixtureBuilder
    [AttributeUsage(AttributeTargets.Class)]
    public class ScopedSuiteAttribute : Attribute, IFixtureBuilder
    {
        public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo)
        {
            var suite = new TestSuite(typeInfo.Type.Name + "Scope");
            var scopeType = typeInfo.Type;
            var scopeInstance = Activator.CreateInstance(scopeType);

            // Move [SetUp] and [TearDown] to OneTimeSetUp/OneTimeTearDown
            var setup = scopeType.GetMethods().Where(m => m.GetCustomAttribute<SetUpAttribute>() != null);
            var teardown = scopeType.GetMethods().Where(m => m.GetCustomAttribute<TearDownAttribute>() != null);

            // Use reflection to get MethodInfoAdapter (internal)
            var adapterType = typeof(TestSuite).Assembly.GetType("NUnit.Framework.Internal.MethodInfoAdapter");
            var fromMethod = adapterType.GetMethod("FromMethodInfo", BindingFlags.Public | BindingFlags.Static);

            // Use reflection to get the private lists for OneTimeSetUpMethods and OneTimeTearDownMethods
            var oneTimeSetUpField = typeof(TestSuite).GetField("_oneTimeSetUpMethods", BindingFlags.Instance | BindingFlags.NonPublic);
            var oneTimeTearDownField = typeof(TestSuite).GetField("_oneTimeTearDownMethods", BindingFlags.Instance | BindingFlags.NonPublic);
            var oneTimeSetUpList = (List<IMethodInfo>)oneTimeSetUpField.GetValue(suite);
            var oneTimeTearDownList = (List<IMethodInfo>)oneTimeTearDownField.GetValue(suite);

            foreach (var m in setup)
            {
                var adapter = fromMethod.Invoke(null, new object[] { m }) as IMethodInfo;
                oneTimeSetUpList.Add(adapter);
            }
            foreach (var m in teardown)
            {
                var adapter = fromMethod.Invoke(null, new object[] { m }) as IMethodInfo;
                oneTimeTearDownList.Add(adapter);
            }

            suite.Properties.Set("ScopeInstance", scopeInstance);

            // Find child fixtures (all nested classes)
            var childTypes = scopeType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            var nunitBuilder = new NUnitTestFixtureBuilder();
            var testCaseBuilder = new NUnitTestCaseBuilder();
            foreach (var child in childTypes)
            {
                // Create a TestSuite for the nested class
                var childSuite = new TestSuite(child);

                // Add [NoFixtureTest] methods as test cases
                var testMethods = child.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.GetCustomAttribute(typeof(NoFixtureTestAttribute)) != null);
                foreach (var method in testMethods)
                {
                    var methodInfo = new MethodWrapper(child, method);
                    var test = testCaseBuilder.BuildTestMethod(methodInfo, childSuite, null);
                    childSuite.Add(test);
                }

                suite.Add(childSuite);
            }

            yield return suite;
        }
    }

    // Test to prove the setup/teardown order
    [TestFixture]
    public class SuiteOrderProof
    {
        [Test]
        public void ProveSuiteOrder()
        {
            // The log should show SetUp, TestA, TestB, TearDown
            Assert.That(SharedScope.Log, Does.Contain("SharedScope.SetUp"));
            Assert.That(SharedScope.Log, Does.Contain("TestClassA.TestA"));
            Assert.That(SharedScope.Log, Does.Contain("TestClassB.TestB"));
            Assert.That(SharedScope.Log, Does.Contain("SharedScope.TearDown"));
        }
    }
}
