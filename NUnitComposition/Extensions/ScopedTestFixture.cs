using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensions;

[Obsolete("Use ExtendableSetUpFixture with MakeOneTimeLifecycle")]
public class ScopedTestFixture : TestFixture
{
    private object? fixture;

    public override object? Fixture
    {
        get => fixture;
        set => fixture = value;
    }

    public override string TestType => nameof(TestFixture);

    public ScopedTestFixture(TestSuite fixture, ITestFilter filter)
        : base(fixture.TypeInfo!)
    {
        FullName = fixture.FullName;
        Method = fixture.Method;
        RunState = fixture.RunState;
        this.fixture = fixture.Fixture;

        foreach (var key in fixture.Properties.Keys)
            foreach (var val in fixture.Properties[key])
                Properties.Add(key, val);

        foreach (var child in fixture.Tests)
        {
            if (filter.Pass(child))
            {
                if (child.IsSuite)
                {
                    var childSuite = ((TestSuite)child).Copy(filter);
                    childSuite.Parent = this;
                    Add(childSuite);
                }
                else
                {
                    Add((Test)child);
                }
            }
        }
    }

    public override TestSuite Copy(ITestFilter filter)
    {
        return new ScopedTestFixture(this, filter);
    }
}