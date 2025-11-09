namespace UmbracoTestsComposition.Common.Database;

public interface IBackofficeClientAuthenticator
{
    Task AuthenticateClientAsync(HttpClient client, string username, string password, bool isAdmin);
}