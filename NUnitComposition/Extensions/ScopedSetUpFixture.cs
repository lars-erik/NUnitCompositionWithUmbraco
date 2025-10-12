using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensions;

[Obsolete("Use ExtendableSetUpFixture with MakeOneTimeLifecycle")]
public class ScopedSetupFixture : SetUpFixture
{
    public ScopedSetupFixture(ITypeInfo type) : base(type)
    {
        Name = GetName(type);

        SetUpMethods = [];
        TearDownMethods = [];

        var setups = TypeInfo
            .GetMethodsWithAttribute<SetUpAttribute>(true).OrderBy(CountBaseClasses)
            .Union(TypeInfo.GetMethodsWithAttribute<OneTimeSetUpAttribute>(true))
            .ToArray();
        OneTimeSetUpMethods = setups;
        OneTimeTearDownMethods = TypeInfo
            .GetMethodsWithAttribute<OneTimeTearDownAttribute>(true)
            .Union(TypeInfo.GetMethodsWithAttribute<TearDownAttribute>(true))
            .ToArray();

        CheckSetUpTearDownMethods(OneTimeSetUpMethods);
        CheckSetUpTearDownMethods(OneTimeTearDownMethods);
    }

    private static int CountBaseClasses(IMethodInfo x)
    {
        var declaringType = x.MethodInfo.DeclaringType;
        var bases = 0;
        while (declaringType?.BaseType != null)
        {
            bases++;
            declaringType = declaringType.BaseType;
        }
        return bases;
    }

    private static string GetName(ITypeInfo type)
    {
        var name = type.Namespace ?? "[default namespace]";
        var index = name.LastIndexOf('.');
        if (index > 0)
            name = name.Substring(index + 1);
        return name;
    }

    public ScopedSetupFixture(ScopedSetupFixture setupFixture, ITestFilter filter)
        : base(setupFixture, filter)
    {
    }

    public override string TestType => nameof(SetUpFixture);

    public new ITypeInfo TypeInfo => base.TypeInfo!;

    public override string? MethodName => OneTimeSetUpMethods.First().Name;

    public override TestSuite Copy(ITestFilter filter)
    {
        return new ScopedSetupFixture(this, filter);
    }
}