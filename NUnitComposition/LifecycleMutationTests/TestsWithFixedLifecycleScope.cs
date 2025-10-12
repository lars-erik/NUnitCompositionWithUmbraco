using NUnitComposition.ImaginaryLibrary;

namespace NUnitComposition.LifecycleMutationTests;

public class TestsWithFixedLifecycleScope
{
    [Test]
    public async Task CanAccessScope()
    {
        var dependency = DerivedSetUpFixtureWithFixedLifecycle.Instance.GetRequiredService<IImaginaryDependency>();
        Assert.That(dependency, Is.Not.Null);
        await dependency.DoStuff();
    }
}