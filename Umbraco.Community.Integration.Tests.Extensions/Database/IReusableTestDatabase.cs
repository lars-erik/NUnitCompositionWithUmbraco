using Umbraco.Cms.Tests.Integration.Testing;

namespace UmbracoTestsComposition.Common.Database;

public interface IReusableTestDatabase : ITestDatabase
{
    TestDbMeta EnsureDatabase();
    Task EnsureSeeded(IServiceProvider serviceProvider);
    Task RestoreSnapshot();
}
