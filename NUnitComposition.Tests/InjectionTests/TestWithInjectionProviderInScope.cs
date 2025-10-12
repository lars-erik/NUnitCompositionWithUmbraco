using NUnitComposition.DependencyInjection;
using NUnitComposition.ImaginaryLibrary;

namespace NUnitComposition.Tests.InjectionTests;

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
        try
        {
            Assert.That(dependency.StuffDone, Has.Length.GreaterThanOrEqualTo(2));
        }
        catch(Exception e)
        {
            Assert.Fail(e.Message);
        }
    }

    private async Task VerifyDependencyUse()
    {
        try
        {
            var initialCount = dependency.StuffDone.Length;
            await dependency.DoStuff();
            Assert.That(dependency.StuffDone, Has.Length.GreaterThan(initialCount));
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
}