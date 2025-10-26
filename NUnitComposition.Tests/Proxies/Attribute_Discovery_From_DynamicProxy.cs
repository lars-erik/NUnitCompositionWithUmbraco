using Castle.DynamicProxy;
using NUnitComposition.Lifecycle;

namespace NUnitComposition.Tests.Proxies;

internal class Attribute_Discovery_From_DynamicProxy
{
    [Test]
    public void Makes_Attribute_Available_Via_ProxyWithTarget_With_Additional_Interface()
    {
        var fixture = new DummyClass();
        var proxy = new ProxyGenerator().CreateClassProxyWithTarget(typeof(DummyClass), [typeof(IFakeSetUpMethods)], fixture, new FakeSetUpMethodInterceptor());
        var proxyType = proxy.GetType();
        var method = proxyType.GetMethod(nameof(IFakeSetUpMethods.FakeSetUpMethod));
        Assert.That(method, Is.Not.Null);

        var attr = ((AttributeToBeDiscoveredAttribute[])method.GetCustomAttributes(typeof(AttributeToBeDiscoveredAttribute), true)).FirstOrDefault();
        var type = method.DeclaringType;

        while (type != null && type != typeof(object))
        {
            var attr2 = ((AttributeToBeDiscoveredAttribute[])type.GetCustomAttributes(typeof(AttributeToBeDiscoveredAttribute), true)).FirstOrDefault();
            if (attr2 != null)
            {
                attr = attr2;
            }

            type = type.BaseType;
        }

        Assert.That(attr, Is.Not.Null);
    }
}

internal class AttributeToBeDiscoveredAttribute : Attribute
{
}

[AttributeToBeDiscovered]
public class DummyClass
{
}

