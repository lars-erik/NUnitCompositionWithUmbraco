using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

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

public class TestWrapSetUpTearDownAttribute : Attribute, IWrapSetUpTearDown
{
    public TestCommand Wrap(TestCommand command)
    {
        return new FakeExecutionContextCommand(command);
    }
}

public class FakeExecutionContextCommand : BeforeAndAfterTestCommand
{
    public FakeExecutionContextCommand(TestCommand innerCommand) : base(innerCommand)
    {
        BeforeTest = (ctx) =>
        {

        };
        
        AfterTest = (ctx) =>
        {

        };
    }
}