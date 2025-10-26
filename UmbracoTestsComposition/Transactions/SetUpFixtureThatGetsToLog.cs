using NUnitComposition.Extensibility;
using NUnitComposition.Tests.LifecycleMutationTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;

namespace UmbracoTestsComposition.Transactions;

[SetUpFixture]
[TestApplyToTestAndContext(nameof(SetUpFixtureThatGetsToLog))]
internal class SetUpFixtureThatGetsToLog
{
    private readonly TestExecutionContext? currentContext;
    private readonly Test? currentTest;

    public SetUpFixtureThatGetsToLog()
    {
        currentContext = TestExecutionContext.CurrentContext;
        currentTest = currentContext.CurrentTest;
    }

    [OneTimeSetUp]
    [TestApplyToTestAndContext(nameof(ButThisDo))]
    public void ButThisDo()
    {
        TestContext.Progress.WriteLine("WHY DOES THIS GET TO LOG TO PROGRESS?");

    }
}
