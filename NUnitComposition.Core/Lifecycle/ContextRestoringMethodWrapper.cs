using System;
using System.Collections.Concurrent;
using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Lifecycle;

/// <summary>
/// Wraps a method so we can temporarily swap NUnit's current test node while invoking it.
/// NUnit 4's <see cref="MethodWrapper"/> exposes <c>Invoke</c> as a non-virtual member, so we re-implement
/// <see cref="IMethodInfo"/> by delegation instead of subclassing.
/// </summary>
public sealed class ContextRestoringMethodWrapper : IMethodInfo
{
    private static readonly ConcurrentDictionary<MethodInfo, Test> OriginalTests = new();

    private readonly IMethodInfo inner;

    private ContextRestoringMethodWrapper(IMethodInfo inner)
    {
        this.inner = inner;
    }

    public static IMethodInfo Wrap(IMethodInfo method)
    {
        return method is ContextRestoringMethodWrapper ? method : new ContextRestoringMethodWrapper(method);
    }

    public static void RememberOriginalTest(MethodInfo method, Test originalTest)
    {
        OriginalTests[method] = originalTest;
    }

    private static Test? GetOriginalTest(MethodInfo method)
    {
        return OriginalTests.TryGetValue(method, out var test) ? test : null;
    }

    public ITypeInfo TypeInfo => inner.TypeInfo;

    public MethodInfo MethodInfo => inner.MethodInfo;

    public string Name => inner.Name;

    public bool IsAbstract => inner.IsAbstract;

    public bool IsPublic => inner.IsPublic;

    public bool IsStatic => inner.IsStatic;

    public bool ContainsGenericParameters => inner.ContainsGenericParameters;

    public bool IsGenericMethod => inner.IsGenericMethod;

    public bool IsGenericMethodDefinition => inner.IsGenericMethodDefinition;

    public ITypeInfo ReturnType => inner.ReturnType;

    public IParameterInfo[] GetParameters() => inner.GetParameters();

    public Type[] GetGenericArguments() => inner.GetGenericArguments();

    public IMethodInfo MakeGenericMethod(params Type[] typeArguments)
    {
        return Wrap(inner.MakeGenericMethod(typeArguments));
    }

    public object? Invoke(object? fixture, params object?[]? args)
    {
        var context = TestExecutionContext.CurrentContext;
        var currentTest = context.CurrentTest;
        var replacement = GetOriginalTest(inner.MethodInfo);
        var swapped = false;

        try
        {
            if (replacement is not null && !ReferenceEquals(replacement, currentTest))
            {
                context.CurrentTest = replacement;
                swapped = true;
            }

            return inner.Invoke(fixture, args);
        }
        finally
        {
            if (swapped)
            {
                context.CurrentTest = currentTest;
            }
        }
    }

    public T[] GetCustomAttributes<T>(bool inherit) where T : class
    {
        return inner.GetCustomAttributes<T>(inherit);
    }

    public bool IsDefined<T>(bool inherit) where T : class
    {
        return inner.IsDefined<T>(inherit);
    }
}
