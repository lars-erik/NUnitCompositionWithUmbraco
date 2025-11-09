using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.DependencyInjection;

[AttributeUsage(AttributeTargets.Class)]
public class InjectAttribute : Attribute, ITestAction
{
    private readonly string injectionMethodName;
    private readonly string? setUpAfterInjection;
    private readonly bool beforeEach;

    public InjectAttribute(string injectionMethod, string? setUpAfterInjection = null, bool beforeEach = false)
    {
        this.injectionMethodName = injectionMethod;
        this.setUpAfterInjection = setUpAfterInjection;
        this.beforeEach = beforeEach;
    }

    public void BeforeTest(ITest test)
    {
        if (!beforeEach && (!test.IsSuite || test.Fixture == null))
        {
            // TODO: Log or throw?
            return;
        }

        ITest? currentTest = TestExecutionContext.CurrentContext.CurrentTest;
        Func<IServiceProvider>? providerFactory = null;
        while (currentTest != null && (providerFactory = currentTest.Properties.Get(InjectionProviderAttribute.FactoryProperty) as Func<IServiceProvider>) == null)
        {
            currentTest = currentTest.Parent;
        }
        
        if (providerFactory == null)
        {
            throw new Exception("No service provider factory found in scope.");
        }

        var provider = providerFactory();
        if (provider == null)
        {
            throw new Exception("The service provider factory returned null.");
        }

        // TODO: Move this test to the ctor if we can access the type there?
        var injectionMethod = test.Fixture.GetType().GetMethod(injectionMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (injectionMethod == null)
        {
            throw new Exception($"Configured inject method {injectionMethod} was not found on {test.GetType().Name}");
        }

        var args = new List<object?>();
        foreach (var param in injectionMethod.GetParameters())
        {
            var service = provider.GetService(param.ParameterType);
            if (service == null)
            {
                throw new Exception($"No service of type {param.ParameterType} found.");
            }
            args.Add(service);
        }

        if (injectionMethod.ReturnType.IsAssignableTo(typeof(Task)))
        {
            var task = (Task)injectionMethod.Invoke(test.Fixture, args.ToArray())!;
            task.GetAwaiter().GetResult();
        }
        else
        {
            injectionMethod.Invoke(test.Fixture, args.ToArray());
        }

        if (!String.IsNullOrWhiteSpace(setUpAfterInjection))
        {
            var setUpMethod = test.Fixture.GetType().GetMethod(setUpAfterInjection, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (setUpMethod == null)
            {
                throw new Exception($"Configured setUp method {setUpAfterInjection} was not found on {test.GetType().Name}");
            }

            if (setUpMethod.ReturnType.IsAssignableTo(typeof(Task)))
            {
                var task = (Task)setUpMethod.Invoke(test.Fixture, null)!;
                task.GetAwaiter().GetResult();
            }
            else
            {
                setUpMethod.Invoke(test.Fixture, null);
            }
        }
    }

    public void AfterTest(ITest test)
    {
        var currentContextTest = TestExecutionContext.CurrentContext.CurrentTest;
    }

    public ActionTargets Targets => ActionTargets.Suite | ActionTargets.Test;
}