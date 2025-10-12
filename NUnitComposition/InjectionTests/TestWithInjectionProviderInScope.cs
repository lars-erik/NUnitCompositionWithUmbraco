using NUnitComposition.ImaginaryLibrary;
using NUnitComposition.DependencyInjection;

namespace NUnitComposition.InjectionTests;

[Inject(nameof(Inject))]
public class TestWithInjectionProviderInScope
{
    private IImaginaryDependency dependency = null!;
    private int injectCalls = 0;

    public void Inject(IImaginaryDependency dependency)
    {
        this.dependency = dependency;

        injectCalls++;
    }

    [Test]
    public async Task CanUseInjectedServices()
    {
        await VerifyDependencyUse();
    }

    [Test]
    public async Task CanUseInjectedServicesForSeveralTests()
    {
        await VerifyDependencyUse();
    }

    [OneTimeTearDown]
    public void VerifyInjectWasCalledOnce()
    {
        Assert.That(injectCalls, Is.EqualTo(1));
    }

    [OneTimeTearDown]
    public void VerifySameInstanceWasCalled()
    {
        Assert.That(dependency.StuffDone, Has.Length.GreaterThanOrEqualTo(2));
    }

    private async Task VerifyDependencyUse()
    {
        var initialCount = dependency.StuffDone.Length;
        await dependency.DoStuff();
        Assert.That(dependency.StuffDone, Has.Length.GreaterThan(initialCount));
    }
}