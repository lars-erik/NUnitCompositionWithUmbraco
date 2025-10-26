using NUnitComposition.Extensibility;
using NUnitComposition.ImaginaryLibrary;
using NUnitComposition.Lifecycle;

namespace NUnitComposition.Tests.LifecycleMutationTests;

[ExtendableSetUpFixture]
[MakeOneTimeLifecycle([nameof(SetUp)], [nameof(TearDown)])]
public class DerivedSetUpFixtureWithFixedLifecycle : ImaginaryLibraryTestBase
{
    private static DerivedSetUpFixtureWithFixedLifecycle? instance;

    public static DerivedSetUpFixtureWithFixedLifecycle Instance
    {
        get
        {
            if (instance == null) throw new Exception("There's no available instance yet. Make sure to use from right time in lifecycle.");
            return instance;
        }
    }

    public new T GetRequiredService<T>() where T : notnull => base.GetRequiredService<T>();

    [OneTimeSetUp]
    [NonParallelizable]
    public async Task OneTimeSetUp()
    {
        await TestContext.Progress.WriteLineAsync($"{GetType().Name}.{nameof(OneTimeSetUp)}");
        if (instance != null)
        {
            throw new Exception("There's already an instance of MutatedSetUpFixture. Only one instance is allowed.");
        }
        instance = this;

        var i = 0;
        while (i < 2000)
        {
            Thread.Sleep(100);
            await TestContext.Progress.WriteAsync(".");
            i += 100;
        }
        await TestContext.Progress.WriteLineAsync();

    }

    [OneTimeTearDown]
    public void OnetimeTearDown()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OnetimeTearDown)}");
        Assert.That(GetRequiredService<IImaginaryDependency>().StuffDone, Has.Length.GreaterThan(0));

        instance = null;
    }
}