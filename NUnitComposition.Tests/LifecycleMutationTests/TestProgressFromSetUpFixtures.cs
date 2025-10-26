using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitComposition.Tests.LifecycleMutationTests;

public class TestProgressFromSetUpFixtures
{
    [SetUp]

    public void JustASetUp() {}

    [Test]
    public void Standard_SetUp_Has_Logged()
    {
        TestContext.Progress.WriteLine("Just another line for good measure.");

        //Assert.That(
        //    ConsoleListeningSetUp.LogBuilder.ToString(),
        //    Contains.Substring(nameof(StandardSetUpFixtureLogsToProgressInOneTime))
        //    );
    }
}
