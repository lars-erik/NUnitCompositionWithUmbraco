using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common.Database;

public class ReusedTestDatabaseOptions
{
    public Func<TestDbMeta, Task<bool>>? NeedsNewSeed { get; set; }

    public Func<IServiceProvider, Task>? SeedData { get; set; }

    public string WorkingDirectory { get; set; } = null!;
}
