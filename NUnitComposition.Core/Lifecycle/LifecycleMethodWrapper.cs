using System.Reflection;
using NUnit.Framework.Internal;
using Umbraco.Community.Integration.Tests.Extensions;

namespace NUnitComposition.Lifecycle;

/// <summary>
/// Adds a child Test with a dynamic Fixture to TestContext.CurrentTest that has a directly declared method.
/// This can be used to facilitate custom attribute discovery on setup fixtures with no direct methods of their own.
/// </summary>
internal class LifecycleMethodWrapper : MethodWrapperBase<LifecycleMethodWrapper>
{
    private List<Action> postHandlers = new();

    public void AddPostHandler(Action action)
    {
        postHandlers.Add(action);
    }

    public override object? Invoke(object? fixture, params object?[]? args)
    {
        if (fixture == null) throw new ArgumentNullException(nameof(fixture));

        var originalContext = TestExecutionContext.CurrentContext;
        var originalTest = originalContext.CurrentTest;

        try
        {
            var proxiedFixture = fixture;

            var fixtureType = proxiedFixture.GetType();
            var methodName = MethodInfo.Name;
            var inheritedMethod = fixtureType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
            var methodInfo = new MethodWrapper(fixtureType, inheritedMethod);

            // This is only really here to get around the UmbracoTestOptionBase checking for a TestMethod
            var setupMethodWrapper = new LifeCycleTestMethod(proxiedFixture, methodInfo, originalTest);
            originalContext.CurrentTest = setupMethodWrapper;

            var result = Reflect.InvokeMethod(methodInfo.MethodInfo, proxiedFixture, args);

            originalContext.CurrentTest = originalTest;

            foreach (var handler in postHandlers)
            {
                handler.Invoke();
            }

            return result;

        }
        catch (Exception ex)
        {
            originalTest.MakeInvalid(ex, $"{MethodInfo.Name} failed: {ex.InnerException?.Message ?? ex.Message}.");
            return null;
        }
        finally
        {
            originalContext.CurrentTest = originalTest;
        }
    }

    /// <summary>
    /// Construct a MethodWrapper for a Type and a MethodInfo.
    /// </summary>
    public LifecycleMethodWrapper(Type type, MethodInfo method) : base(type, method)
    {
    }
}