using NUnit.Framework.Interfaces;

namespace NUnitComposition.Extensibility;

internal interface IExtendableLifecycle
{
    IMethodInfo[] SetUpMethods { get; set; }
    IMethodInfo[] TearDownMethods { get; set; }
    IMethodInfo[] OneTimeSetUpMethods { get; set; }
    IMethodInfo[] OneTimeTearDownMethods { get; set; }
}