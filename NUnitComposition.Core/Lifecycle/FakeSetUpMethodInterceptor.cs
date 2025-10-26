using Castle.DynamicProxy;

namespace NUnitComposition.Lifecycle;

public class FakeSetUpMethodInterceptor : IInterceptor
{
    public FakeSetUpMethodInterceptor()
    {
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();
    }
}