using Castle.DynamicProxy;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnitComposition.Lifecycle;
using System.Diagnostics;

namespace NUnitComposition.Extensibility;

public class ExtendableSetUpFixture : SetUpFixture, IExtendableLifecycle
{
    public override string TestType => nameof(SetUpFixture);

    public new ITypeInfo TypeInfo => base.TypeInfo!;

    public override string? MethodName => OneTimeSetUpMethods.FirstOrDefault()?.Name;

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

    private readonly List<IInterceptor> interceptors = new();

    public IInterceptor[] Interceptors
    {
        get => interceptors.ToArray();
    }

    public object Proxy { get; private set; }

    public ExtendableSetUpFixture(ITypeInfo type) : base(type)
    {
        SetUpMethods = TypeInfo.GetMethodsWithAttribute<SetUpAttribute>(true);
        TearDownMethods = TypeInfo.GetMethodsWithAttribute<TearDownAttribute>(true);
    }

    public ExtendableSetUpFixture(ExtendableSetUpFixture setupFixture, ITestFilter filter)
        : base(setupFixture, filter)
    {
        SetUpMethods = setupFixture.SetUpMethods;
        TearDownMethods = setupFixture.TearDownMethods;
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
        FilterContext.RegisterTestFilter(filter);

        var copy = new ExtendableSetUpFixture(this, filter)
        {
            Proxy = Proxy,
            Fixture = Proxy
        };
        copy.interceptors.AddRange(interceptors);

        return copy;
    }

    public void SetFixture(object proxy)
    {
        this.Proxy = proxy;
        Fixture = proxy;
    }

    public void AddInterceptor(IInterceptor interceptor) => interceptors.Add(interceptor);

    public void AddPostHandler(string methodName, Action handler)
    {
        var setupMethod = OneTimeSetUpMethods.OfType<LifecycleMethodWrapper>().FirstOrDefault(x => x.Name == methodName);
        if (setupMethod == null)
        {
            MakeInvalid($"Cannot add post handler. No OneTimeSetUp method found with name '{methodName}'.");
            return;
        }
        setupMethod.AddPostHandler(handler);
    }

    public void AddPostHandler(IMethodInfo setUpTearDownMethod, Action handler)
    {
        var setupMethod = OneTimeSetUpMethods.OfType<LifecycleMethodWrapper>().FirstOrDefault(x => x == setUpTearDownMethod);
        if (setupMethod == null)
        {
            MakeInvalid($"Cannot add post handler. No OneTimeSetUp method found with name '{setUpTearDownMethod.Name}'.");
            return;
        }
        setupMethod.AddPostHandler(handler);
    }
}