using NUnitComposition.Extensions;

namespace NUnitComposition.SampleScope;

[ScopedSetupFixture]
public class SampleScopedFixture
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Root.Log.Add($"{nameof(SampleScopedFixture)} {nameof(OneTimeSetup)} called.");
    }

    [SetUp]
    public void SetUp()
    {
        Root.Log.Add($"{nameof(SampleScopedFixture)} {nameof(SetUp)} called (should be mutated to onetime).");
    }

    [TearDown]
    public void TearDown()
    {
        Root.Log.Add($"{nameof(SampleScopedFixture)} {nameof(TearDown)} called (should be mutated to onetime).");
    }

    [TearDown]
    public void OneTimeTearDown()
    {
        Root.Log.Add($"{nameof(SampleScopedFixture)} {nameof(OneTimeTearDown)} called.");
    }
}