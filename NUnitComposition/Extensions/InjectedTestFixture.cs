using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensions;

public class InjectedTestFixture<T> : ScopedTestFixture
    where T : IInjectionSource
{
    private readonly ParameterInfo[] parameters;

    public override object?[] Arguments
    {
        get
        {
            try
            {
                if (!T.HasInstance)
                {
                    return TypeDiscovery.BuildDefaultArguments(parameters);
                }

                var instance = T.Instance;
                var parameterInstances = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameterInstances[i] = instance.Get(parameters[i].ParameterType);
                }

                return parameterInstances;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }

    public InjectedTestFixture(TestSuite fixture, ITestFilter filter) : base(fixture, filter)
    {
        parameters = TypeDiscovery.FindBestConstructorParameters(fixture);
    }

    public override TestSuite Copy(ITestFilter filter)
    {
        return new InjectedTestFixture<T>(this, filter);
    }

}

public class TypeDiscovery
{
    private static T? CreateDefault<T>() => default;

    internal static object?[] BuildDefaultArguments(ParameterInfo[] parameters)
    {
        // TODO: This might really blow up, but we'll see. The tradeoff is to require a parameterless constructor, but that seems dumb.
        var defaultMethod = typeof(TypeDiscovery).GetMethod(nameof(CreateDefault), BindingFlags.Static | BindingFlags.NonPublic)!;
        var arguments = parameters.Select(x =>
        {
            return defaultMethod.MakeGenericMethod(x.ParameterType).Invoke(null, []);
        }).ToArray();
        return arguments;
    }

    internal static ConstructorInfo FindBestConstructor(TestSuite fixture)
    {
        return FindBestConstructor(fixture.TypeInfo!.Type);
    }

    internal static ConstructorInfo FindBestConstructor(Type fixtureType)
    {
        // TODO: Can we find the ctor that our source satisfies _the most_ for, rather than just the longest? What to do for conflicts?
        var ctors = fixtureType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(x => x.GetParameters().Length);
        var longestCtor = ctors.FirstOrDefault();
        if (longestCtor == null)
            throw new InvalidOperationException($"Type {fixtureType.FullName} has no public constructors");
        return longestCtor;
    }

    internal static ParameterInfo[] FindBestConstructorParameters(TestSuite fixture)
    {
        return FindBestConstructor(fixture).GetParameters();
    }

    internal static ParameterInfo[] FindBestConstructorParameters(Type fixtureType)
    {
        return FindBestConstructor(fixtureType).GetParameters();
    }

}