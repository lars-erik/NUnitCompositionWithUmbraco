using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace NUnitComposition.Extensions
{
    public class InjectedTestFixtureAttribute<T> : ScopedTestFixtureAttribute, IFixtureBuilder2 // , ITestData
    {
        private readonly Type injectionSource = typeof(T);
        private ParameterInfo[] parameters = [];

        // TODO: This makes JetBrains' runner think this is a parameterized fixture
        // We need to figure out how to "disable" parameterization when we use injection, or maybe even better - satisfy being a test case source.
        //object?[] ITestData.Arguments
        //{
        //    get
        //    {
        //        return TypeDiscovery.BuildDefaultArguments(parameters);
        //    }
        //}

        //public InjectedTestFixtureAttribute(Type injectionSource)
        //{
        //    this.injectionSource = injectionSource;
        //}

        IEnumerable<TestSuite> IFixtureBuilder2.BuildFrom(ITypeInfo typeInfo, IPreFilter filter)
        {
            var fixtureType = typeof(InjectedTestFixture<>).MakeGenericType(injectionSource);
            var ctor = fixtureType.GetConstructor([typeof(TestSuite), typeof(ITestFilter)]);
            if (ctor == null) throw new Exception($"{fixtureType} must expose public constructor with {typeof(TestSuite)} and {typeof(ITestFilter)} arguments.");

            parameters = TypeDiscovery.FindBestConstructorParameters(typeInfo.Type);

            var fixture = ScopedBuildFrom(typeInfo, filter);

            TestSuite wrappedFixture;
            try
            {
                wrappedFixture = (TestSuite)ctor!.Invoke([fixture, new EmptyFilter()]);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            yield return wrappedFixture;
        }
    }
}
