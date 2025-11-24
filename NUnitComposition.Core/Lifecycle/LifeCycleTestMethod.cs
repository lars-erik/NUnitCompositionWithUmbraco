using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Lifecycle;

public class LifeCycleTestMethod : TestMethod, ITest
{
    private readonly object fixture;

    public LifeCycleTestMethod(object fixture, IMethodInfo method, Test? parentSuite) : base(method, parentSuite)
    {
        this.fixture = fixture;
        Parent = parentSuite;
        parentSuite!.Tests.Add(this);
    }

    public override string TestType => nameof(SetUpFixture);

    public override string XmlElementName => "test-suite";

    public override object? Fixture => fixture;
}