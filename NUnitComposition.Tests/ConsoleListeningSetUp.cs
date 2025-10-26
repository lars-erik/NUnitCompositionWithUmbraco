using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnitComposition.Tests;
using System.Text;

namespace NUnitComposition.Tests;

[SetUpFixture]
internal class ConsoleListeningSetUp
{
    private static ConsoleListeningSetUp? instance;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        instance = this;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        instance = null;
    }
}
