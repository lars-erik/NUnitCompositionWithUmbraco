namespace UmbracoTestsComposition.Common.Database;

public class ReusedTestDatabaseOptions
{
    public Func<Task<bool>>? NeedsNewSeed { get; set; }

    public Func<Task>? SeedData { get; set; }

    public required string WorkingDirectory { get; set; }
}
