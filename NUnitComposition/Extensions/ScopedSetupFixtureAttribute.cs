using System.Diagnostics.CodeAnalysis;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ScopedSetupFixtureAttribute : SetUpFixtureAttribute, IFixtureBuilder
{
    // TODO: Figure out if we want to implement IFixtureBuilder2 as well

    IEnumerable<TestSuite> IFixtureBuilder.BuildFrom(ITypeInfo typeInfo)
    {
        return BuildFrom(typeInfo);
    }

    public new IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo)
    {
        SetUpFixture fixture = new ScopedSetUpFixture(typeInfo);

        if (fixture.RunState != RunState.NotRunnable)
        {
            string? reason = null;
            if (!IsValidFixtureType(typeInfo, ref reason))
                fixture.MakeInvalid(reason);
        }

        fixture.ApplyAttributesToTest(typeInfo.Type);

        return new TestSuite[] { fixture };
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

        // These'll be "moved" by the ScopedSetUpFixture instance by just adding them to the onetimes
        /*
        var attributesToMove = new[]
        {
            typeof(SetUpAttribute),
            typeof(TearDownAttribute)
        };

        foreach (Type invalidType in attributesToMove)
        {
            if (typeInfo.HasMethodWithAttribute(invalidType))
            {
                reason = invalidType.Name + " attribute is soon to be changed to OneTime...";
                return false;
            }
        }
        */

        return true;
    }
}

// TODO: Need to figure out how to replicate the dispose fixture command since the IDisposableFixture interface is internal to NUnit
// TODO: Can we just use the built-in SetUpFixture?
public class ScopedSetUpFixture : SetUpFixture // , IDisposableFixture
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SetUpFixture"/> class.
    /// </summary>
    public ScopedSetUpFixture(ITypeInfo type) : base(type)
    {
        Name = GetName(type);

        SetUpMethods = [];
        TearDownMethods = [];

        OneTimeSetUpMethods = TypeInfo
            .GetMethodsWithAttribute<OneTimeSetUpAttribute>(true)
            .Union(TypeInfo.GetMethodsWithAttribute<SetUpAttribute>(true))
            .ToArray();
        OneTimeTearDownMethods = TypeInfo
            .GetMethodsWithAttribute<OneTimeTearDownAttribute>(true)
            .Union(TypeInfo.GetMethodsWithAttribute<TearDownAttribute>(true))
            .ToArray();

        CheckSetUpTearDownMethods(OneTimeSetUpMethods);
        CheckSetUpTearDownMethods(OneTimeTearDownMethods);
    }

    private static string GetName(ITypeInfo type)
    {
        var name = type.Namespace ?? "[default namespace]";
        var index = name.LastIndexOf('.');
        if (index > 0)
            name = name.Substring(index + 1);
        return name;
    }

    /// <summary>
    /// Creates a copy of the given suite with only the descendants that pass the specified filter.
    /// </summary>
    /// <param name="setUpFixture">The <see cref="SetUpFixture"/> to copy.</param>
    /// <param name="filter">Determines which descendants are copied.</param>
    public ScopedSetUpFixture(ScopedSetUpFixture setUpFixture, ITestFilter filter)
        : base(setUpFixture, filter)
    {
    }

    #endregion

    #region Test Suite Overrides

    public override string TestType => nameof(SetUpFixture);

    /// <summary>
    /// Gets the TypeInfo of the fixture used in running this test.
    /// </summary>
    public new ITypeInfo TypeInfo => base.TypeInfo!;

    /// <summary>
    /// Creates a filtered copy of the test suite.
    /// </summary>
    /// <param name="filter">Determines which descendants are copied.</param>
    public override TestSuite Copy(ITestFilter filter)
    {
        return new ScopedSetUpFixture(this, filter);
    }

    #endregion
}