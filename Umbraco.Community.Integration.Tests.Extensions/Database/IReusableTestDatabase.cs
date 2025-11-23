using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Community.Integration.Tests.Extensions.Database;

public interface IReusableTestDatabase : ITestDatabase
{
    TestDbMeta EnsureDatabase();
    Task EnsureSeeded(IServiceProvider serviceProvider);
    Task RestoreSnapshot();
}
