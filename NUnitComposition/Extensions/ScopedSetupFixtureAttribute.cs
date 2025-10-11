using System.Diagnostics.CodeAnalysis;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ScopedSetupFixtureAttribute : SetUpFixtureAttribute, IFixtureBuilder2
{
    // TODO: Figure out if we want to implement IFixtureBuilder2 as well

    IEnumerable<TestSuite> IFixtureBuilder.BuildFrom(ITypeInfo typeInfo)
    {
        return BuildFrom(typeInfo);
    }

    public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo, IPreFilter filter)
    {
        return BuildFrom(typeInfo);
    }

    public new IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo)
    {
        var fixture = new ScopedSetupFixture(typeInfo);

        if (fixture.RunState != RunState.NotRunnable)
        {
            string? reason = null;
            if (!IsValidFixtureType(typeInfo, ref reason))
                fixture.MakeInvalid(reason);
        }

        fixture.ApplyAttributesToTest(typeInfo.Type);
        
        return [fixture];
    }

    private static bool IsValidFixtureType(ITypeInfo typeInfo, [NotNullWhen(false)] ref string? reason)
    {
        if (!typeInfo.IsStaticClass)
        {
            if (typeInfo.IsAbstract)
            {
                reason = $"{typeInfo.FullName} is an abstract class";
                return false;
            }

            if (!typeInfo.HasConstructor(Array.Empty<Type>()))
            {
                reason = $"{typeInfo.FullName} does not have a default constructor";
                return false;
            }
        }

        return true;
    }

}