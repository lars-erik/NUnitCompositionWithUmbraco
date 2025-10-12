using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensions;


public class ExtendableSetUpFixture : SetUpFixture, IExtendableLifecycle
{
    public override string TestType => nameof(SetUpFixture);

    public new ITypeInfo TypeInfo => base.TypeInfo!;

    public override string? MethodName => OneTimeSetUpMethods.First().Name;

    IMethodInfo[] IExtendableLifecycle.SetUpMethods
    {
        get => base.SetUpMethods;
        set => base.SetUpMethods = value;
    }

    IMethodInfo[] IExtendableLifecycle.TearDownMethods
    {
        get => base.TearDownMethods;
        set => base.TearDownMethods = value;
    }

    IMethodInfo[] IExtendableLifecycle.OneTimeSetUpMethods
    {
        get => base.OneTimeSetUpMethods;
        set => base.OneTimeSetUpMethods = value;
    }

    IMethodInfo[] IExtendableLifecycle.OneTimeTearDownMethods
    {
        get => base.OneTimeTearDownMethods;
        set => base.OneTimeTearDownMethods = value;
    }

    public ExtendableSetUpFixture(ITypeInfo type) : base(type)
    {
        SetUpMethods = TypeInfo.GetMethodsWithAttribute<SetUpAttribute>(true);
        TearDownMethods = TypeInfo.GetMethodsWithAttribute<TearDownAttribute>(true);
    }

    public ExtendableSetUpFixture(ExtendableSetUpFixture setupFixture, ITestFilter filter)
        : base(setupFixture, filter)
    {
        SetUpMethods = TypeInfo.GetMethodsWithAttribute<SetUpAttribute>(true);
        TearDownMethods = TypeInfo.GetMethodsWithAttribute<TearDownAttribute>(true);
    }

    public void DelayedValidate()
    {
        CheckSetUpTearDownMethods(SetUpMethods);
        CheckSetUpTearDownMethods(TearDownMethods);
        CheckSetUpTearDownMethods(OneTimeSetUpMethods);
        CheckSetUpTearDownMethods(OneTimeTearDownMethods);
    }

    public override TestSuite Copy(ITestFilter filter)
    {
        return new ExtendableSetUpFixture(this, filter);
    }

}