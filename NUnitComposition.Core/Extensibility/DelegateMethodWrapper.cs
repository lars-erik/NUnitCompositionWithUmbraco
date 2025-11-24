using System.Reflection;
using NUnit.Framework.Internal;
using NUnitComposition.Extensibility;

namespace Umbraco.Community.Integration.Tests.Extensions.Database;

internal class DelegateMethodWrapper : MethodWrapperBase<DelegateMethodWrapper>
{
    private readonly MethodInfo methodInfo;

    public DelegateMethodWrapper(Type fixtureType, Type type, string methodName) : base(fixtureType, type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!)
    {
        methodInfo = MethodInfo;
    }
    public override object? Invoke(object? fixture, params object?[]? args)
    {
        try
        {
            return Reflect.InvokeMethod(methodInfo, null, args);
        }
        catch (Exception ex)
        {
            TestExecutionContext.CurrentContext.CurrentTest.MakeInvalid(ex, $"Failed to execute delegate method {MethodInfo.Name}: {ex.Message}");
            return null;
        }
    }
}