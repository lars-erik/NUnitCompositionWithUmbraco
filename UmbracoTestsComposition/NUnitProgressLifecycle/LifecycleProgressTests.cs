using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;

namespace UmbracoTestsComposition.NUnitProgressLifecycle;

[SetUpFixture]
public class LifecycleProgressSetup
{
    private static Test CurrentTest => TestExecutionContext.CurrentContext.CurrentTest;

    [OneTimeSetUp]
    public static void OneTimeSetUp()
    {
        TestContext.Progress.WriteLine($"{nameof(LifecycleProgressSetup)}.{nameof(OneTimeSetUp)} => {CurrentTest.TestType} {CurrentTest.MethodName} {CurrentTest.Method}");
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        TestContext.Progress.WriteLine($"{nameof(LifecycleProgressSetup)}.{nameof(OneTimeTearDown)} => {CurrentTest.TestType} {CurrentTest.MethodName} {CurrentTest.Method}");
    }
}

public class LifecycleProgressTests
{
    private static Test CurrentTest => TestExecutionContext.CurrentContext.CurrentTest;

    [OneTimeSetUp]
    public static void OneTimeSetUp()
    {
        TestContext.Progress.WriteLine($"{nameof(LifecycleProgressTests)}.{nameof(OneTimeSetUp)} => {CurrentTest.TestType} {CurrentTest.MethodName} {CurrentTest.Method}");
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        TestContext.Progress.WriteLine($"{nameof(LifecycleProgressTests)}.{nameof(OneTimeTearDown)} => {CurrentTest.TestType} {CurrentTest.MethodName} {CurrentTest.Method}");
    }

    [SetUp]
    public static void SetUp()
    {
        TestContext.Progress.WriteLine($"{nameof(LifecycleProgressTests)}.{nameof(SetUp)} => {CurrentTest.TestType} {CurrentTest.MethodName} {CurrentTest.Method}");
    }

    [TearDown]
    public static void TearDown()
    {
        TestContext.Progress.WriteLine($"{nameof(LifecycleProgressTests)}.{nameof(TearDown)} => {CurrentTest.TestType} {CurrentTest.MethodName} {CurrentTest.Method}");
    }

    [Test]
    public static void ATest()
    {
        TestContext.Progress.WriteLine($"{nameof(LifecycleProgressTests)}.{nameof(ATest)} => {CurrentTest.TestType} {CurrentTest.MethodName} {CurrentTest.Method}");
    }
}
