namespace NUnitComposition.Tests.LifecycleMutationTests;

[SetUpFixture]
public class StandardSetUpFixtureLogsToProgressInOneTime
{
    public void OneTimeSetUp()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OneTimeSetUp)}");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OneTimeTearDown)}");
    }
}
