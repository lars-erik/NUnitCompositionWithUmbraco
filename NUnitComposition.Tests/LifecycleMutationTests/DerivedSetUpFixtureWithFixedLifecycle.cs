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
    public async Task OneTimeSetUp()
    {
        if (instance != null)
        {
            throw new Exception("There's already an instance of MutatedSetUpFixture. Only one instance is allowed.");
        }
        instance = this;

        await TestContext.Progress.WriteLineAsync($"{GetType().Name}.{nameof(OneTimeSetUp)}");
    }

    [OneTimeTearDown]
    public void OnetimeTearDown()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OnetimeTearDown)}");
        Assert.That(GetRequiredService<IImaginaryDependency>().StuffDone, Has.Length.GreaterThan(0));

        instance = null;
    }
}