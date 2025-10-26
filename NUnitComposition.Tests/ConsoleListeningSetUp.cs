using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnitComposition.Tests;
using System.Text;
using Castle.DynamicProxy;

namespace NUnitComposition.Tests;

[SetUpFixture]
internal class ConsoleListeningSetUp
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
    }
}
