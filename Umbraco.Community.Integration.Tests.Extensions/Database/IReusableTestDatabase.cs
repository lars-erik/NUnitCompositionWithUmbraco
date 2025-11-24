using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Community.Integration.Tests.Extensions.Database;

public interface IReusableTestDatabase : ITestDatabase
{
    TestDbMeta EnsureDatabase(IServiceProvider? services);
    Task EnsureSeeded(IServiceProvider services);
    Task RestoreSnapshot();
}
