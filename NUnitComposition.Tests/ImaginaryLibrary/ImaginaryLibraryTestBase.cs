using Microsoft.Extensions.DependencyInjection;

namespace NUnitComposition.ImaginaryLibrary;

[SingleThreaded]
[NonParallelizable]
public abstract class ImaginaryLibraryTestBase
{
    private IServiceProvider provider = null!;

    protected IServiceProvider Services => provider;

    protected T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

    [SetUp]
    public void SetUp()
    {
        TestContext.Progress.WriteLine($"{nameof(ImaginaryLibraryTestBase)}.{nameof(SetUp)}");
        var services = new ServiceCollection();
        services.AddSingleton<IImaginaryDependency, ImaginaryDependency>();
        provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        TestContext.Progress.WriteLine($"{nameof(ImaginaryLibraryTestBase)}.{nameof(TearDown)}");
        if (provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}