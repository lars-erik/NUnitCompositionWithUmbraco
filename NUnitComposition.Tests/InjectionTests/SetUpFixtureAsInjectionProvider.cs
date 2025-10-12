using NUnitComposition.DependencyInjection;
using NUnitComposition.Extensibility;
using NUnitComposition.ImaginaryLibrary;
using NUnitComposition.Lifecycle;

namespace NUnitComposition.Tests.InjectionTests;

[ExtendableSetUpFixture]
[MakeOneTimeLifecycle([nameof(SetUp)], [nameof(TearDown)])]
[InjectionProvider(nameof(GetProvider))]
public class SetUpFixtureAsInjectionProvider : ImaginaryLibraryTestBase
{
    public IServiceProvider GetProvider() => Services;
}