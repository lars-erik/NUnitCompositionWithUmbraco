using System.Reflection;
using Castle.DynamicProxy;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnitComposition.Extensibility;
using Umbraco.Community.Integration.Tests.Extensions;

namespace NUnitComposition.Lifecycle;

/// <summary>
/// Adds a child Test with a dynamic Fixture to TestContext.CurrentTest that has a directly declared method.
/// This can be used to facilitate custom attribute discovery on setup fixtures with no direct methods of their own.
/// </summary>
internal class FakeDirectSetUpMethodWrapper : IMethodInfo, IEquatable<FakeDirectSetUpMethodWrapper>
{
    /// <summary>
    /// Invokes the method, converting any TargetInvocationException to an NUnitException.
    /// </summary>
    /// 
    /// <param name="fixture">The object on which to invoke the method</param>
    /// <param name="args">The argument list for the method</param>
    /// <returns>The return value from the invoked method</returns>
    public object? Invoke(object? fixture, params object?[]? args)
    {
        if (fixture == null) throw new ArgumentNullException(nameof(fixture));

        var originalContext = TestExecutionContext.CurrentContext;
        var originalTest = originalContext.CurrentTest;

        try
        {
            var proxiedFixture = fixture;

            var fixtureType = proxiedFixture.GetType();
            var stubMethod = fixtureType.GetMethod(nameof(IUmbracoLookalikeSetupMethods.FakeSetup))!;

            var methodInfo = new MethodWrapper(fixtureType, stubMethod);
            var setupMethodWrapper = new LifeCycleTestMethod(proxiedFixture, methodInfo, originalTest)
            {
                Parent = originalTest
            };

            ((TestSuite)originalTest).Add(setupMethodWrapper);
            originalContext.CurrentTest = setupMethodWrapper;
            originalContext.TestObject = proxiedFixture;

            var result = Reflect.InvokeMethod(MethodInfo, proxiedFixture, args);

            return result;
        }
        catch(Exception ex)
        {
            throw;
        }
        finally
        {
            originalContext.TestObject = fixture;
            originalContext.CurrentTest = originalTest;
        }
    }

    /// <summary>
    /// Construct a MethodWrapper for a Type and a MethodInfo.
    /// </summary>
    public FakeDirectSetUpMethodWrapper(Type type, MethodInfo method)
    {
        TypeInfo = new TypeWrapper(type);
        MethodInfo = method;
    }

    /// <summary>
    /// Construct a MethodInfo for a given Type and method name.
    /// </summary>
    public FakeDirectSetUpMethodWrapper(Type type, string methodName)
    {
        TypeInfo = new TypeWrapper(type);
        MethodInfo = type.GetMethod(methodName);
    }

    #region IMethod Implementation

    /// <summary>
    /// Gets the Type from which this method was reflected.
    /// </summary>
    public ITypeInfo TypeInfo { get; }

    /// <summary>
    /// Gets the MethodInfo for this method.
    /// </summary>
    public MethodInfo MethodInfo { get; }

    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    public string Name
    {
        get { return MethodInfo.Name; }
    }

    /// <summary>
    /// Gets a value indicating whether the method is abstract.
    /// </summary>
    public bool IsAbstract
    {
        get { return MethodInfo.IsAbstract; }
    }

    /// <summary>
    /// Gets a value indicating whether the method is public.
    /// </summary>
    public bool IsPublic
    {
        get { return MethodInfo.IsPublic; }
    }

    /// <summary>
    /// Gets a value indicating whether the method is static.
    /// </summary>
    public bool IsStatic => MethodInfo.IsStatic;

    /// <summary>
    /// Gets a value indicating whether the method contains unassigned generic type parameters.
    /// </summary>
    public bool ContainsGenericParameters
    {
        get { return MethodInfo.ContainsGenericParameters; }
    }

    /// <summary>
    /// Gets a value indicating whether the method is a generic method.
    /// </summary>
    public bool IsGenericMethod
    {
        get { return MethodInfo.IsGenericMethod; }
    }

    /// <summary>
    /// Gets a value indicating whether the MethodInfo represents the definition of a generic method.
    /// </summary>
    public bool IsGenericMethodDefinition
    {
        get { return MethodInfo.IsGenericMethodDefinition; }
    }

    /// <summary>
    /// Gets the return Type of the method.
    /// </summary>
    public ITypeInfo ReturnType
    {
        get { return new TypeWrapper(MethodInfo.ReturnType); }
    }

    /// <summary>
    /// Gets the parameters of the method.
    /// </summary>
    /// <returns></returns>
    public IParameterInfo[] GetParameters()
    {
        var parameters = MethodInfo.GetParameters();
        var result = new IParameterInfo[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            result[i] = new ParameterWrapper(this, parameters[i]);

        return result;
    }

    /// <summary>
    /// Returns the Type arguments of a generic method or the Type parameters of a generic method definition.
    /// </summary>
    public Type[] GetGenericArguments()
    {
        return MethodInfo.GetGenericArguments();
    }

    /// <summary>
    /// Replaces the type parameters of the method with the array of types provided and returns a new IMethodInfo.
    /// </summary>
    /// <param name="typeArguments">The type arguments to be used</param>
    /// <returns>A new IMethodInfo with the type arguments replaced</returns>
    public IMethodInfo MakeGenericMethod(params Type[] typeArguments)
    {
        return new MethodWrapper(TypeInfo.Type, MethodInfo.MakeGenericMethod(typeArguments));
    }

    /// <summary>
    /// Returns an array of custom attributes of the specified type applied to this method
    /// </summary>
    public T[] GetCustomAttributes<T>(bool inherit) where T : class
    {
        return MethodInfo.GetAttributes<T>(inherit);
    }

    /// <summary>
    /// Gets a value indicating whether one or more attributes of the specified type are defined on the method.
    /// </summary>
    public bool IsDefined<T>(bool inherit) where T : class
    {
        return MethodInfo.HasAttribute<T>(inherit);
    }
    
    /// <summary>
    /// Override ToString() so that error messages in NUnit's own tests make sense
    /// </summary>
    public override string ToString()
    {
        return MethodInfo.Name;
    }

    #endregion

    public bool Equals(FakeDirectSetUpMethodWrapper? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TypeInfo.Equals(other.TypeInfo) && MethodInfo.Equals(other.MethodInfo);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FakeDirectSetUpMethodWrapper)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TypeInfo, MethodInfo);
    }

    public static bool operator ==(FakeDirectSetUpMethodWrapper? left, FakeDirectSetUpMethodWrapper? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(FakeDirectSetUpMethodWrapper? left, FakeDirectSetUpMethodWrapper? right)
    {
        return !Equals(left, right);
    }
}