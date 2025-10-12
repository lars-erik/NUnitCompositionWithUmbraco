using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnitComposition.ImaginaryLibrary;

namespace NUnitComposition.MutationTests;

public class TestsInMutatedSetupFixtureScope
{
    [Test]
    public async Task CanAccessScope()
    {
        TestContext.Progress.WriteLine($"{GetType().Name}.{nameof(CanAccessScope)}");

        var dependency = MutatedSetUpFixture.Instance.GetRequiredService<IImaginaryDependency>();
        Assert.That(dependency, Is.Not.Null);
        await dependency.DoStuff();
    }
}