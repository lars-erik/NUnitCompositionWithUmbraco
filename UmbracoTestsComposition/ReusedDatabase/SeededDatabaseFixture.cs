using NUnit.Framework;

namespace UmbracoTestsComposition.ReusedDatabase;

[SetUpFixture]
public class SeededDatabaseFixture
{
    public static int SeedCount;

    [OneTimeTearDown]
    public void EnsureDatabaseSeededOnce()
    {
        TestContext.Progress.WriteLine($"Seed count verified: {SeedCount}");
        Assert.That(SeedCount, Is.LessThanOrEqualTo(1), "The reused database should only be seeded once per test run.");
    }
}
