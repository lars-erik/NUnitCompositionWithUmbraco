using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitComposition.SampleScope;

public class NamespaceScopedNUnitFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitFixture)} {nameof(OneTimeSetUp)} called.");
    }

    [SetUp]
    public void SetUp()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitFixture)} {nameof(SetUp)} called.");
    }

    [Test]
    public void NUnitScopedTest()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitFixture)} {nameof(NUnitScopedTest)} called.");
        Assert.Pass();
    }

    [Test]
    public void AnotherNUnitScopedTest()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitFixture)} {nameof(AnotherNUnitScopedTest)} called.");
        Assert.Pass();
    }

    [TearDown]
    public void TearDown()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitFixture)} {nameof(TearDown)} called.");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Root.Log.Add($"{nameof(NamespaceScopedNUnitFixture)} {nameof(OneTimeTearDown)} called.");
    }
}