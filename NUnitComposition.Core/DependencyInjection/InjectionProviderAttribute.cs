using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.DependencyInjection;

public class InjectionProviderAttribute : Attribute, IApplyToTest
{
    internal const string FactoryProperty = "InjectionProviderFactory";
    private const BindingFlags AllInstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
    private readonly string providerMemberName;

    public InjectionProviderAttribute(string providerMemberName)
    {
        this.providerMemberName = providerMemberName;
    }

    public void ApplyToTest(Test test)
    {
        if (test is not TestSuite suite || suite.TypeInfo?.Type == null)
        {
            return;
        }

        Func<IServiceProvider> factoryFunction = () =>
        {
            var fixtureType = test.Fixture!.GetType()!;
            var providerMethod = fixtureType.GetMethod(providerMemberName, AllInstanceMembers);
            var providerProperty = fixtureType.GetProperty(providerMemberName, AllInstanceMembers);
            return 
                providerMethod?.Invoke(test.Fixture, null) as IServiceProvider ??
                providerProperty?.GetValue(test.Fixture, null) as IServiceProvider
                ?? throw new Exception("The fixture or provider method is not available, or the provider method did not return an IServiceProvider.");
        };
        test.Properties.Add(FactoryProperty, factoryFunction);

        var contextCurrentTest = TestExecutionContext.CurrentContext.CurrentTest;
        contextCurrentTest.Properties.Add(FactoryProperty, factoryFunction);
    }
}