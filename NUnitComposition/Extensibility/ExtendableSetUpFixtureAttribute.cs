using System.Diagnostics.CodeAnalysis;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensibility;

[AttributeUsage(AttributeTargets.Class)]
public class ExtendableSetUpFixtureAttribute : SetUpFixtureAttribute, IFixtureBuilder2
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
        if (!ValidateRulesCopiedFromBase(typeInfo, out reason))
        {
            reason ??= "Failed base validation rules without reason.";
            return false;
        }

        try
        {
            var allSetUps = typeInfo.GetMethodsWithAttribute<SetUpAttribute>(true).Select(x => x.MethodInfo).ToArray();
            var allOneTimeSetUps = fixture.OneTimeSetUpMethods.Select(x => x.MethodInfo).ToArray();
            var setupsInOneTimeSetUps = allOneTimeSetUps.Intersect(allSetUps);
            var remainingSetUps = allSetUps.Except(setupsInOneTimeSetUps);

            var allTearDowns = typeInfo.GetMethodsWithAttribute<TearDownAttribute>(true).Select(x => x.MethodInfo).ToArray();
            var allOneTimeTearDowns = fixture.OneTimeTearDownMethods.Select(x => x.MethodInfo).ToArray();
            var tearDownsInOneTimeTearDowns = allOneTimeTearDowns.Intersect(allTearDowns);
            var remainingTearDowns = allTearDowns.Except(tearDownsInOneTimeTearDowns);

            var remaining = remainingSetUps.Union(remainingTearDowns).ToArray();

            if (remaining.Length > 0)
            {
                reason =
                    "There are active SetUp or TearDown methods in the hierarchy. Move them using [MakeOneTimeLifecycle]. The candidates are: " +
                    Environment.NewLine + String.Join(Environment.NewLine, remaining.Select(x => x.Name));
                return false;
            }

        }
        catch(Exception ex)
        {
            // TODO: It seems VS or ReSharper can crash the entire VS process if we throw here?
            reason = $"Failed to validate lifecycle methods: {ex.Message}";
            return false;
        }

        return true;
    }

    private static bool ValidateRulesCopiedFromBase(ITypeInfo typeInfo, out string? reason)
    {
        if (!typeInfo.IsStaticClass)
        {
            if (typeInfo.IsAbstract)
            {
                reason = $"{typeInfo.FullName} is an abstract class";
                return false;
            }

            if (!typeInfo.HasConstructor([]))
            {
                reason = $"{typeInfo.FullName} does not have a default constructor";
                return false;
            }
        }

        reason = null;
        return true;
    }
}