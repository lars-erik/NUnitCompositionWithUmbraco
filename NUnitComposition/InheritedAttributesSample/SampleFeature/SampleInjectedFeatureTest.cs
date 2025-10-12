using NUnitComposition.Extensions;

namespace NUnitComposition.SampleScope.SampleFeature;

[InjectedTestFixture<SampleScopedSetupFixture>]
public class SampleInjectedFeatureTest
{
    private readonly string someString;
    private readonly int someInt;

    public SampleInjectedFeatureTest()
    {

    }

    public SampleInjectedFeatureTest(string someString, int someInt)
    {
        this.someString = someString;
        this.someInt = someInt;
    }

    [Test]
    public void GotValuesInjectedFromInjectionSource()
    {
        Console.WriteLine($"We got values injected: {someString}, {someInt}");

        Assert.That(someString, Is.EqualTo("An injectable string"));
        Assert.That(someInt, Is.EqualTo(42));
    }
}