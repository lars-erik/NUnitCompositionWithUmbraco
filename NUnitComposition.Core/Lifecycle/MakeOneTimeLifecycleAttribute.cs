using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnitComposition.Extensibility;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace NUnitComposition.Lifecycle;

[AttributeUsage(AttributeTargets.Class)]
public class MakeOneTimeLifecycleAttribute : Attribute, IApplyToTest, IApplyToContext
{
    private readonly string[] setUpNames;
    private readonly string[] tearDownNames;

    public MakeOneTimeLifecycleAttribute(string[] setUpNames, string[] tearDownNames)
    {
        this.setUpNames = setUpNames;
        this.tearDownNames = tearDownNames;
    }

    public void ApplyToContext(TestExecutionContext context)
    {

    }

    public void ApplyToTest(Test test)
    {
        if (test is not TestSuite suite || suite.TypeInfo?.Type == null)
        {
            return;
        }
        else
        {

        }

        if (test is IExtendableLifecycle extendable)
        {
            var setUpsToMove = extendable.SetUpMethods.Where(x => setUpNames.Contains(x.Name)).ToArray();
            var tearDownsToMove = extendable.TearDownMethods.Where(x => tearDownNames.Contains(x.Name)).ToArray();

            // This should ensure that the base setup/teardowns are executed before and after the concrete onetime variants.
            var newOneTimeSetUps = setUpsToMove
                .Select(mi => new InterceptingMethodWrapper(mi.TypeInfo.Type, mi.MethodInfo))
                .Union(extendable.OneTimeSetUpMethods)
                .ToArray();
            var newOneTimeTearDowns = extendable.OneTimeTearDownMethods.Union(tearDownsToMove).ToArray();

            var remainingSetUps = extendable.SetUpMethods.Except(setUpsToMove).ToArray();
            var remainingTearDowns = extendable.TearDownMethods.Except(tearDownsToMove).ToArray();

            extendable.SetUpMethods = remainingSetUps;
            extendable.TearDownMethods = remainingTearDowns;

            extendable.OneTimeSetUpMethods = newOneTimeSetUps;
            extendable.OneTimeTearDownMethods = newOneTimeTearDowns;
        }
        else
        {
            throw new Exception($"{nameof(MakeOneTimeLifecycleAttribute)} must be applied to a test fixture with an {nameof(IExtendableLifecycle)} implementation like {nameof(ExtendableSetUpFixture)}.");
        }
    }
}

public class LifeCycleTestMethod : TestMethod
{
    public LifeCycleTestMethod(IMethodInfo method) : base(method)
    {
    }

    public LifeCycleTestMethod(IMethodInfo method, Test? parentSuite) : base(method, parentSuite)
    {
    }

    public override string TestType => nameof(SetUpFixture);

    public override string XmlElementName => "test-suite";
}

public class InterceptingMethodWrapper : IMethodInfo, IEquatable<InterceptingMethodWrapper>
{
    /// <summary>
    /// Invokes the method, converting any TargetInvocationException to an NUnitException.
    /// </summary>
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
            var anyDirectMethod = fixture.GetType().GetMethods().First();
            var directDeclaration = anyDirectMethod.DeclaringType == fixture.GetType();
            var methodInfo = new MethodWrapper(fixture.GetType(), anyDirectMethod);
            var setupMethodWrapper = new LifeCycleTestMethod(methodInfo, originalTest)
            {
                Parent = originalTest
            };
            ((TestSuite)originalTest).Add(setupMethodWrapper);
            originalContext.CurrentTest = setupMethodWrapper;

            var result = Reflect.InvokeMethod(MethodInfo, fixture, args);
            return result;
        }
        catch(Exception ex)
        {
            throw;
        }
        finally
        {
            originalContext.CurrentTest = originalTest;
        }
    }

    /// <summary>
    /// Construct a MethodWrapper for a Type and a MethodInfo.
    /// </summary>
    public InterceptingMethodWrapper(Type type, MethodInfo method)
    {
        TypeInfo = new TypeWrapper(type);
        MethodInfo = method;
    }

    /// <summary>
    /// Construct a MethodInfo for a given Type and method name.
    /// </summary>
    public InterceptingMethodWrapper(Type type, string methodName)
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

    public bool Equals(InterceptingMethodWrapper? other)
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
        return Equals((InterceptingMethodWrapper)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TypeInfo, MethodInfo);
    }

    public static bool operator ==(InterceptingMethodWrapper? left, InterceptingMethodWrapper? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(InterceptingMethodWrapper? left, InterceptingMethodWrapper? right)
    {
        return !Equals(left, right);
    }
}

internal static class InternalNUnitExtensions
{
    public static bool IsStatic(this Type type)
    {
        return type.GetTypeInfo().IsAbstract && type.GetTypeInfo().IsSealed;
    }

    public static bool HasAttribute<T>(this ICustomAttributeProvider attributeProvider, bool inherit)
    {
        return attributeProvider.IsDefined(typeof(T), inherit);
    }

    public static bool HasAttribute<T>(this Type type, bool inherit)
    {
        return ((ICustomAttributeProvider)type.GetTypeInfo()).HasAttribute<T>(inherit);
    }

    public static T[] GetAttributes<T>(this ICustomAttributeProvider attributeProvider, bool inherit) where T : class
    {
        return (T[])attributeProvider.GetCustomAttributes(typeof(T), inherit);
    }

    public static T[] GetAttributes<T>(this Assembly assembly) where T : class
    {
        return assembly.GetAttributes<T>(inherit: false);
    }

    public static T[] GetAttributes<T>(this Type type, bool inherit) where T : class
    {
        return ((ICustomAttributeProvider)type.GetTypeInfo()).GetAttributes<T>(inherit);
    }

    public static IEnumerable Skip(this IEnumerable enumerable, long skip)
    {
        var iterator = enumerable.GetEnumerator();
        using (iterator as IDisposable)
        {
            while (skip-- > 0)
            {
                if (!iterator.MoveNext())
                    yield break;
            }

            while (iterator.MoveNext())
            {
                yield return iterator.Current;
            }
        }
    }
}
