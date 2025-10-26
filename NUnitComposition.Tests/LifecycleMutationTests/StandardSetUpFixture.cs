using NUnitComposition.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitComposition.Tests.LifecycleMutationTests;

[SetUpFixture]
[SingleThreaded]
public class StandardSetUpFixtureLogsToProgressInOneTime
{
    [OneTimeSetUp]
    [NonParallelizable]
    public void OneTimeSetUp()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OneTimeSetUp)}");
        var i = 0;
        while (i < 2000)
        {
            Thread.Sleep(100);
            TestContext.Progress.Write(".");
            i += 100;
        }
        TestContext.Progress.WriteLine();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OneTimeTearDown)}");
    }
}
