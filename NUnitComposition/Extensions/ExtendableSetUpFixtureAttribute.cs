using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System.Diagnostics.CodeAnalysis;

namespace NUnitComposition.Extensions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ExtendableSetUpFixtureAttribute : SetUpFixtureAttribute, IFixtureBuilder2, IExtendableTestBuilder
{
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
        var fixture = new ExtendableSetUpFixture(typeInfo);
        fixture.ApplyAttributesToTest(typeInfo.Type);

        if (fixture.RunState != RunState.NotRunnable)
        {
            string? reason = null;
            if (!IsValidFixtureType(fixture, typeInfo, ref reason))
                fixture.MakeInvalid(reason);
        }

        fixture.DelayedValidate();

        return [fixture];
    }

    private bool IsValidFixtureType(ExtendableSetUpFixture fixture, ITypeInfo typeInfo, [NotNullWhen(false)] ref string? reason)
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

        if (fixture.SetUpMethods.Any() || fixture.TearDownMethods.Any())
        {
            reason = $"{typeInfo.Type.Name} may not have any SetUp or TearDown methods.";
        }

        return true;
    }
}