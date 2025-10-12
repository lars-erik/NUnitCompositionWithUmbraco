using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitComposition.SampleScope;

[SetUpFixture]
public class NamespaceScopedNUnitSetupFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitSetupFixture)} {nameof(OneTimeSetUp)} called.");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitSetupFixture)} {nameof(OneTimeTearDown)} called.");
    }
}