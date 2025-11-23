namespace Umbraco.Community.Integration.Tests.Extensions.Database;

public interface IBackofficeClientAuthenticator
{
    Task AuthenticateClientAsync(HttpClient client, string username, string password, bool isAdmin);
}