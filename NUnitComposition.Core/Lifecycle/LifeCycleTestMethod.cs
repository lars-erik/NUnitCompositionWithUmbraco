using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Lifecycle;

public class LifeCycleTestMethod : TestMethod
{
    private readonly object fixture;

    public LifeCycleTestMethod(IMethodInfo method) : base(method)
    {
    }

    public LifeCycleTestMethod(object fixture, IMethodInfo method, Test? parentSuite) : base(method, parentSuite)
    {
        this.fixture = fixture;
    }

    public override string TestType => nameof(SetUpFixture);

    public override string XmlElementName => "test-suite";

    public override object? Fixture => fixture;
}