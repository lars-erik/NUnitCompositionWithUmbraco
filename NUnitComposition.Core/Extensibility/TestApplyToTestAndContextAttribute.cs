using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensibility;

public class TestApplyToTestAndContextAttribute : Attribute, IApplyToContext, IApplyToTest
{
    private readonly string name;

    public TestApplyToTestAndContextAttribute(string name)
    {
        this.name = name;
    }

    public void ApplyToContext(TestExecutionContext context)
    {
        
    }

    public void ApplyToTest(Test test)
    {
        
    }
}
