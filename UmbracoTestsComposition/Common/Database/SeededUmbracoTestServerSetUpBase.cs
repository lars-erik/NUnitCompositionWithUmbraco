using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnitComposition.Extensibility;
using OpenIddict.Abstractions;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Controllers.Security;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Api.Management.ViewModels.Security;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Security;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Cms.Tests.Integration.TestServerTest;

namespace UmbracoTestsComposition.Common.Database;

[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ServiceProvider]
public abstract class SeededUmbracoTestServerSetUpBase<TMainController> : UmbracoTestServerTestBase
    where TMainController : ManagementApiControllerBase
{
    private readonly bool authorize;

    #region Database Setup

    ReusedTestDatabase testDatabase = null!;

    private TestDbMeta? databaseMeta;
    public TestDbMeta DatabaseMeta => databaseMeta!;

    private bool ranTestServiceConfig = false;
    private bool ranCustomTestSetup = false;

    protected SeededUmbracoTestServerSetUpBase(bool authorize = false)
    {
        this.authorize = authorize;
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        ranTestServiceConfig = true;

        base.ConfigureTestServices(services);

        services.Configure<ReusedTestDatabaseOptions>(options =>
        {
            options.WorkingDirectory = TestHelper.WorkingDirectory;
            ConfigureTestDatabaseOptions(options);
        });

        services.AddSingleton<ReusedTestDatabase>();
        services.AddSingleton<ITestDatabase>(sp => sp.GetRequiredService<ReusedTestDatabase>());

        services.AddKeyedTransient<HttpClient>("TestServerClient", (_, key) => {
            if (authorize)
            {
                // TODO: Check if we have creds for unattended install to use instead.
                AuthenticateClientAsync(Client, "admin@example.com", "adminadminadmin", true).GetAwaiter().GetResult();
            }
            return Client;
        });
    }

    protected abstract void ConfigureTestDatabaseOptions(ReusedTestDatabaseOptions options);

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        ranCustomTestSetup = true;

        var existingFactories = builder.Services.Where(x => x.ServiceType == typeof(IHostedService) && x.ImplementationFactory?.Target == this);
        builder.Services.Remove(existingFactories.First());
        builder.Services.AddTransient<IHostedService>(sp => new TestDatabaseHostedLifecycleService(() =>
        {
            testDatabase = sp.GetRequiredService<ReusedTestDatabase>();
            var logger = sp.GetRequiredService<ILogger<SeededUmbracoTestServerSetUpBase<TMainController>>>();
            logger.LogInformation($"Ensuring reused database");
            var meta = testDatabase.EnsureDatabase();
            databaseMeta = meta;

            logger.LogInformation($"Database set up with connection string: {meta.ConnectionString}");

            ConfigureUmbracoDatabase(sp, databaseMeta);
        }));
    }

    protected override void CustomTestAuthSetup(IServiceCollection services)
    {
        // This is in an awkward method, but it's the best place to validate just before the web app factory is done "booting".
        if (!ranCustomTestSetup || !ranTestServiceConfig) throw new Exception("base.ConfigureTestServices() and base.CustomTestSetup() must be called when overridden. Otherwise the test database won't be attached.");
    }

    [OneTimeSetUp]
    public async Task EnsureReusedDatabaseAsync()
    {
        await TestContext.Progress.WriteLineAsync($"[{GetType().Name}] Ensuring seeded database.");
        await testDatabase.EnsureSeeded(Services);
    }

    [OneTimeTearDown]
    public void DetachDatabase()
    {
        if (databaseMeta != null)
        {
            TestContext.Progress.WriteLine($"[{GetType().Name}] Detaching reused database.");
            testDatabase.Detach(databaseMeta);
        }
    }

    private void ConfigureUmbracoDatabase(IServiceProvider sp, TestDbMeta meta)
    {
        var databaseFactory = sp.GetRequiredService<IUmbracoDatabaseFactory>();
        var connectionStrings = sp.GetRequiredService<IOptionsMonitor<ConnectionStrings>>();
        var runtimeState = sp.GetRequiredService<IRuntimeState>();

        databaseFactory.Configure(meta.ToStronglyTypedConnectionString());
        connectionStrings.CurrentValue.ConnectionString = meta.ConnectionString;
        connectionStrings.CurrentValue.ProviderName = meta.Provider;

        runtimeState.DetermineRuntimeLevel();
        sp.GetRequiredService<IEventAggregator>().Publish(new UnattendedInstallNotification());
    }

    #endregion

    #region HTTP Client Setup

    [OneTimeSetUp]
    public Task SetupRequestHeaders()
    {
        Client.DefaultRequestHeaders
            .Accept
            .Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        return Task.CompletedTask;
    }

    protected abstract Expression<Func<TMainController, object>> MethodSelector { get; }

    protected virtual string Url => GetManagementApiUrl(MethodSelector);

    protected async Task AuthenticateClientAsync(HttpClient client, string username, string password, bool isAdmin) =>
        await AuthenticateClientAsync(client,
            async userService =>
            {
                IUser user;
                if (isAdmin)
                {
                    user = await userService.GetRequiredUserAsync(global::Umbraco.Cms.Core.Constants.Security.SuperUserKey);
                    user.Username = user.Email = username;
                    userService.Save(user);
                }
                else
                {
                    user = (await userService.CreateAsync(
                        global::Umbraco.Cms.Core.Constants.Security.SuperUserKey,
                        new UserCreateModel
                        {
                            Email = username,
                            Name = username,
                            UserName = username,
                            UserGroupKeys = new HashSet<Guid>(new[] { global::Umbraco.Cms.Core.Constants.Security.EditorGroupKey })
                        },
                        true)).Result.CreatedUser!;
                }

                return (user, password);
            });


    protected async Task AuthenticateClientAsync(HttpClient client, Func<IUserService, Task<(IUser user, string Password)>> createUser)
    {

        OpenIddictApplicationDescriptor backofficeOpenIddictApplicationDescriptor;
        var scopeProvider = GetRequiredService<ICoreScopeProvider>();

        string? username;
        string? password;

        using (var scope = scopeProvider.CreateCoreScope())
        {
            var userService = GetRequiredService<IUserService>();
            using var serviceScope = GetRequiredService<IServiceScopeFactory>().CreateScope();
            var userManager = serviceScope.ServiceProvider.GetRequiredService<ICoreBackOfficeUserManager>();

            var userCreationResult = await createUser(userService);
            username = userCreationResult.user.Username;
            password = userCreationResult.Password;
            var userKey = userCreationResult.user.Key;

            var token = await userManager.GeneratePasswordResetTokenAsync(userCreationResult.user);


            var changePasswordAttempt = await userService.ChangePasswordAsync(userKey,
                new ChangeUserPasswordModel
                {
                    NewPassword = password,
                    ResetPasswordToken = token.Result.ToUrlBase64(),
                    UserKey = userKey
                });

            Assert.IsTrue(changePasswordAttempt.Success);

            var backOfficeApplicationManager =
                serviceScope.ServiceProvider.GetRequiredService<IBackOfficeApplicationManager>() as
                    BackOfficeApplicationManager;

            // TODO: This is about to become internal, so we might either end up having to use reflect, or we need to demand it _won't_ be hidden.
            backofficeOpenIddictApplicationDescriptor =
                backOfficeApplicationManager.BackofficeOpenIddictApplicationDescriptor(client.BaseAddress);

            scope.Complete();
        }

        var loginModel = new LoginRequestModel { Username = username, Password = password };

        // Login to ensure the cookie is set (used in next request)
        var loginResponse = await client.PostAsync(
            GetManagementApiUrl<BackOfficeController>(x => x.Login(CancellationToken.None, null)), JsonContent.Create(loginModel));

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode, await loginResponse.Content.ReadAsStringAsync());

        var codeVerifier = "12345"; // Just a dummy value we use in tests
        var codeChallange = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(codeVerifier)))
            .TrimEnd("=");

        var authorizationUrl = GetManagementApiUrl<BackOfficeController>(x => x.Authorize(CancellationToken.None)) +
                  $"?client_id={backofficeOpenIddictApplicationDescriptor.ClientId}&response_type=code&redirect_uri={WebUtility.UrlEncode(backofficeOpenIddictApplicationDescriptor.RedirectUris.FirstOrDefault()?.AbsoluteUri)}&code_challenge_method=S256&code_challenge={codeChallange}";
        var authorizeResponse = await client.GetAsync(authorizationUrl);

        Assert.AreEqual(HttpStatusCode.Found, authorizeResponse.StatusCode, await authorizeResponse.Content.ReadAsStringAsync());

        var tokenResponse = await client.PostAsync("/umbraco/management/api/v1/security/back-office/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = codeVerifier,
                ["client_id"] = backofficeOpenIddictApplicationDescriptor.ClientId,
                ["code"] = HttpUtility.ParseQueryString(authorizeResponse.Headers.Location.Query).Get("code"),
                ["redirect_uri"] =
                    backofficeOpenIddictApplicationDescriptor.RedirectUris.FirstOrDefault().AbsoluteUri
            }));

        var tokenModel = await tokenResponse.Content.ReadFromJsonAsync<TokenModel>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenModel.AccessToken);
    }

    private class TokenModel
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }
    }


    #endregion
}