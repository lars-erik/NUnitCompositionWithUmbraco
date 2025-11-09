using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Scoping;
using IScopeProvider = Umbraco.Cms.Infrastructure.Scoping.IScopeProvider;

namespace UmbracoTestsComposition.TestServerReusedDatabase.TestServerA;

public class TestServerASetUp : TestServerReusedDatabaseSampleSetUpBase
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddSingleton<IStartupFilter, AddTransactionMiddleware>();
    }

    [OneTimeTearDown]
    public void DisposeTransaction()
    {
        TransactionMiddleware.Reset();
    }
}

public class AddTransactionMiddleware : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<TransactionMiddleware>();
            next(app);
        };
    }
}

public class TransactionMiddleware : IDisposable
{
    public static ICoreScope? CoreScope;
    private static bool createdScope = false;

    private readonly RequestDelegate next;

    public TransactionMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public static void Reset()
    {
        CoreScope?.Dispose();
        createdScope = false;
    }

    public async Task InvokeAsync(HttpContext ctx, ICoreScopeProvider scopeProvider)
    {
        if (!createdScope)
        {
            CoreScope = scopeProvider.CreateCoreScope(autoComplete: false, isolationLevel: IsolationLevel.Snapshot);
            
            createdScope = true;
        }

        await next(ctx);
    }

    public void Dispose()
    {
        CoreScope?.Dispose();
    }
}
