using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Integration.Implementations;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Community.Integration.Tests.Extensions.Database;

public class ConfigureReusableDbInterceptor : IInterceptor
{
    private readonly Type configureOptionsType;
    private readonly string configureOptionsMethodName;

    private static readonly PropertyInfo TestHelperProperty = typeof(UmbracoIntegrationTestBase).GetProperty("TestHelper", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly PropertyInfo ConfigurationProperty = typeof(UmbracoIntegrationTestBase).GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public ConfigureReusableDbInterceptor(Type configureOptionsType, string configureOptionsMethodName)
    {
        this.configureOptionsType = configureOptionsType;
        this.configureOptionsMethodName = configureOptionsMethodName;
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();

        var fixture = invocation.Proxy;
        var configuration = ((IConfiguration)ConfigurationProperty.GetValue(fixture)!);
        var testHelper = ((TestHelper)TestHelperProperty.GetValue(fixture)!);

        if (invocation.Method.Name == "CustomTestSetup")
        {
            var builder = (IUmbracoBuilder)invocation.Arguments[0];
            var services = builder.Services;

            var settings = new TestDatabaseSettings
            {
                FilesPath = Path.Combine(testHelper.WorkingDirectory, "databases"),
            };
            configuration.Bind("Tests:Database", settings);
            services.AddSingleton(settings);

            services.Configure<ReusableTestDatabaseOptions>(options =>
            {
                options.WorkingDirectory = testHelper.WorkingDirectory;

                var configureOptionsMethod = configureOptionsType.GetMethod(configureOptionsMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (configureOptionsMethod != null)
                {
                    configureOptionsMethod.Invoke(configureOptionsMethod.IsStatic ? null : fixture, [options]);
                }
                else
                {
                    throw new Exception($"{configureOptionsMethodName} is not static or a member in {configureOptionsType.Name}.");
                }
            });

            services.AddSingleton<ITestDatabase>(sp => sp.GetRequiredService<IReusableTestDatabase>());

            services.AddUnique<IUmbracoContextAccessor, TestUmbracoContextAccessor>();
        }
        else if (invocation.Method.Name == "ConfigureTestServices")
        {
            var services = (IServiceCollection)invocation.Arguments[0];

            var settings = (TestDatabaseSettings)services.Single(x => x.ServiceType == typeof(TestDatabaseSettings)).ImplementationInstance!;

            var databaseType = settings.DatabaseType switch
            {
                TestDatabaseSettings.TestDatabaseType.Sqlite => typeof(ReusableSqliteTestDatabase),
                TestDatabaseSettings.TestDatabaseType.SqlServer => typeof(ReusableSqlServerTestDatabase),
                _ => throw new Exception($"Reusable test database implementation for {settings.DatabaseType} not found.")
            };

            // TODO: Always pass settings as well?
            var db = Activator.CreateInstance(databaseType, [testHelper, (UmbracoIntegrationTestBase)invocation.Proxy]);
            typeof(UmbracoIntegrationTestBase).GetField("s_dbInstance", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, db);
            services.AddSingleton(typeof(IReusableTestDatabase), db);
        }
    }
}