using NUnitComposition.Extensions;

namespace NUnitComposition.SampleScope;

[ScopedSetupFixture]
public class ScopedSetupFixture
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Root.Log.Add($"{nameof(ScopedSetupFixture)} {nameof(OneTimeSetup)} called.");
    }

    [SetUp]
    public void SetUp()
    {
        Root.Log.Add($"{nameof(ScopedSetupFixture)} {nameof(SetUp)} called (should be mutated to onetime).");
    }

    [TearDown]
    public void TearDown()
    {
        Root.Log.Add($"{nameof(ScopedSetupFixture)} {nameof(TearDown)} called (should be mutated to onetime).");
    }

    [TearDown]
    public void OneTimeTearDown()
    {
        Root.Log.Add($"{nameof(ScopedSetupFixture)} {nameof(OneTimeTearDown)} called.");
    }
}