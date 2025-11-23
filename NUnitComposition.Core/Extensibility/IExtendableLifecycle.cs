using Castle.DynamicProxy;
using NUnit.Framework.Interfaces;

namespace NUnitComposition.Extensibility;

public interface IExtendableLifecycle : ITest
{
    IMethodInfo[] SetUpMethods { get; set; }
    IMethodInfo[] TearDownMethods { get; set; }
    IMethodInfo[] OneTimeSetUpMethods { get; set; }
    IMethodInfo[] OneTimeTearDownMethods { get; set; }
    void SetProxy(object proxy);
    void AddInterceptor(IInterceptor interceptor);
}