using NUnitComposition.Extensions;
using NUnitComposition.ImaginaryLibrary;
using NUnitComposition.Mutation;

namespace NUnitComposition.MutationTests;

[ExtendableSetUpFixture]
[MakeOneTimeLifecycle([nameof(SetUp)], [nameof(TearDown)])]
public class MutatedSetUpFixture : ImaginaryLibraryTestBase
{
    private static MutatedSetUpFixture? instance;

    public static MutatedSetUpFixture Instance
    {
        get
        {
            if (instance == null) throw new Exception("There's no available instance yet. Make sure to use from right time in lifecycle.");
            return instance;
        }
    }

    public new T GetRequiredService<T>() where T : notnull => base.GetRequiredService<T>();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OneTimeSetUp)}");
        if (instance != null)
        {
            throw new Exception("There's already an instance of MutatedSetUpFixture. Only one instance is allowed.");
        }
        instance = this;
    }

    [OneTimeTearDown]
    public void OnetimeTearDown()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(OnetimeTearDown)}");
        Assert.That(GetRequiredService<IImaginaryDependency>().StuffDone, Has.Length.GreaterThan(0));

        instance = null;
    }
}