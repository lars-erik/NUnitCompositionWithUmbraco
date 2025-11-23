using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnitComposition.Extensibility;
using NUnitComposition.Lifecycle;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Tests.Common;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Implementations;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Cms.Tests.Integration.TestServerTest;
using Umbraco.Community.Integration.Tests.Extensions;
using Umbraco.Community.Integration.Tests.Extensions.Database;

namespace UmbracoTestsComposition.ReusedDatabaseByAttribute;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerTest,
    Boot = true,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ReusableDatabase]
//[ServiceProvider]
public class ReusedDbAttributeSetUp : UmbracoIntegrationTest
{
    private static ReusedDbAttributeSetUp instance;

    public static IServiceProvider ServiceProvider => instance.Services;

    [OneTimeSetUp]
    public void Initialize()
    {
        instance = this;
    }
}

public class ReusableDatabaseAttribute : Attribute
    , IApplyToTest
    //, ITestAction
{
    string interceptMethod = null!;

    public ReusableDatabaseAttribute(string? interceptMethod = null)
    {
        this.interceptMethod = interceptMethod ?? nameof(UmbracoIntegrationTest.Setup);
    }

    public void ApplyToTest(Test test)
    {
        var type = test?.TypeInfo?.Type;
        if (test is not TestSuite suite || type == null)
        {
            return;
        }
        
        if (test is IExtendableLifecycle extendable && type.IsAssignableTo(typeof(UmbracoIntegrationTestBase)))
        {
            var method = extendable.OneTimeSetUpMethods.SingleOrDefault(x => x.Name == interceptMethod && x.GetParameters().Length == 0);
            if (method == null)
            {
                throw new Exception($"Method '{interceptMethod}' not found on {type}. " +
                                    $"Unless {type} is derived from {nameof(UmbracoIntegrationTest)} or {nameof(UmbracoTestServerTestBase)} " +
                                    $"you need to pass the setup method name to the {nameof(ReusableDatabaseAttribute)} constructor.");
            }

            var index = extendable.OneTimeSetUpMethods.IndexOf(method);
            extendable.OneTimeSetUpMethods[index] = new ReplaceStaticDbFieldWithReusableDatabase(
                method.TypeInfo,
                method.MethodInfo
            );
        }
        else
        {
            throw new Exception($"{nameof(ReusableDatabaseAttribute)} may only be applied to fixtures derived from {nameof(UmbracoIntegrationTestBase)}");
        }
    }
}

internal class ReplaceStaticDbFieldWithReusableDatabase : IMethodInfo, IEquatable<ReplaceStaticDbFieldWithReusableDatabase>
{
    public object? Invoke(object? fixture, params object?[]? args)
    {
        if (fixture == null || fixture is not UmbracoIntegrationTestBase) throw new ArgumentException("Fixture must be a non null UmbracoIntegrationTest", nameof(fixture));

        var originalContext = TestExecutionContext.CurrentContext;
        var originalTest = originalContext.CurrentTest;

        try
        {
            var fixtureType = fixture.GetType();
            var proxiedFixture = new ProxyGenerator().CreateClassProxyWithTarget(fixtureType, [typeof(IUmbracoLookalikeSetupMethods)], fixture, new ConfigureReusableDbInterceptor());

            fixtureType = proxiedFixture.GetType();
            var stubMethod = fixtureType.GetMethod(nameof(IUmbracoLookalikeSetupMethods.Setup))!;

            var methodInfo = new MethodWrapper(fixtureType, stubMethod);
            var setupMethodWrapper = new LifeCycleTestMethod(proxiedFixture, methodInfo, originalTest)
            {
                Parent = originalTest
            };
            ((TestSuite)originalTest).Add(setupMethodWrapper);
            originalContext.CurrentTest = setupMethodWrapper;
            originalContext.TestObject = proxiedFixture;

            var result = Reflect.InvokeMethod(MethodInfo, proxiedFixture, args);

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            originalContext.TestObject = fixture;
            originalContext.CurrentTest = originalTest;
        }

        return null;
    }

    /// <summary>
    /// Construct a MethodWrapper for a Type and a MethodInfo.
    /// </summary>
    public ReplaceStaticDbFieldWithReusableDatabase(ITypeInfo typeInfo, MethodInfo method)
    {
        TypeInfo = typeInfo;
        MethodInfo = method;
    }

    #region IMethod Implementation

    /// <summary>
    /// Gets the Type from which this method was reflected.
    /// </summary>
    public ITypeInfo TypeInfo { get; }

    /// <summary>
    /// Gets the MethodInfo for this method.
    /// </summary>
    public MethodInfo MethodInfo { get; }

    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    public string Name
    {
        get { return MethodInfo.Name; }
    }

    /// <summary>
    /// Gets a value indicating whether the method is abstract.
    /// </summary>
    public bool IsAbstract
    {
        get
        {
            return false; //MethodInfo.IsAbstract;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the method is public.
    /// </summary>
    public bool IsPublic
    {
        get { return MethodInfo.IsPublic; }
    }

    /// <summary>
    /// Gets a value indicating whether the method is static.
    /// </summary>
    public bool IsStatic => MethodInfo.IsStatic;

    /// <summary>
    /// Gets a value indicating whether the method contains unassigned generic type parameters.
    /// </summary>
    public bool ContainsGenericParameters
    {
        get { return MethodInfo.ContainsGenericParameters; }
    }

    /// <summary>
    /// Gets a value indicating whether the method is a generic method.
    /// </summary>
    public bool IsGenericMethod
    {
        get { return MethodInfo.IsGenericMethod; }
    }

    /// <summary>
    /// Gets a value indicating whether the MethodInfo represents the definition of a generic method.
    /// </summary>
    public bool IsGenericMethodDefinition
    {
        get { return MethodInfo.IsGenericMethodDefinition; }
    }

    /// <summary>
    /// Gets the return Type of the method.
    /// </summary>
    public ITypeInfo ReturnType
    {
        get { return new TypeWrapper(MethodInfo.ReturnType); }
    }

    /// <summary>
    /// Gets the parameters of the method.
    /// </summary>
    /// <returns></returns>
    public IParameterInfo[] GetParameters()
    {
        var parameters = MethodInfo.GetParameters();
        var result = new IParameterInfo[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            result[i] = new ParameterWrapper(this, parameters[i]);

        return result;
    }

    /// <summary>
    /// Returns the Type arguments of a generic method or the Type parameters of a generic method definition.
    /// </summary>
    public Type[] GetGenericArguments()
    {
        return MethodInfo.GetGenericArguments();
    }

    /// <summary>
    /// Replaces the type parameters of the method with the array of types provided and returns a new IMethodInfo.
    /// </summary>
    /// <param name="typeArguments">The type arguments to be used</param>
    /// <returns>A new IMethodInfo with the type arguments replaced</returns>
    public IMethodInfo MakeGenericMethod(params Type[] typeArguments)
    {
        return new MethodWrapper(TypeInfo.Type, MethodInfo.MakeGenericMethod(typeArguments));
    }

    /// <summary>
    /// Returns an array of custom attributes of the specified type applied to this method
    /// </summary>
    public T[] GetCustomAttributes<T>(bool inherit) where T : class
    {
        return MethodInfo.GetAttributes<T>(inherit);
    }

    /// <summary>
    /// Gets a value indicating whether one or more attributes of the specified type are defined on the method.
    /// </summary>
    public bool IsDefined<T>(bool inherit) where T : class
    {
        return MethodInfo.HasAttribute<T>(inherit);
    }

    /// <summary>
    /// Override ToString() so that error messages in NUnit's own tests make sense
    /// </summary>
    public override string ToString()
    {
        return MethodInfo.Name;
    }

    #endregion

    public bool Equals(ReplaceStaticDbFieldWithReusableDatabase? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TypeInfo.Equals(other.TypeInfo) && MethodInfo.Equals(other.MethodInfo);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ReplaceStaticDbFieldWithReusableDatabase)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TypeInfo, MethodInfo);
    }

    public static bool operator ==(ReplaceStaticDbFieldWithReusableDatabase? left, ReplaceStaticDbFieldWithReusableDatabase? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ReplaceStaticDbFieldWithReusableDatabase? left, ReplaceStaticDbFieldWithReusableDatabase? right)
    {
        return !Equals(left, right);
    }
}

public interface IUmbracoLookalikeSetupMethods
{
    //TestHelper TestHelper { get; }
    //IConfiguration Configuration { get; }
    void Setup();
    //void ConfigureServices(IServiceCollection services);
}

public class ConfigureReusableDbInterceptor : IInterceptor
{
    private static readonly PropertyInfo TestHelperProperty = typeof(UmbracoIntegrationTestBase).GetProperty("TestHelper", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly PropertyInfo ConfigurationProperty = typeof(UmbracoIntegrationTestBase).GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public void Intercept(IInvocation invocation)
    {
        // TODO: Find integration test name
        if (invocation.Method.Name == "CustomTestSetup" && false)
        {
            var fixture = //(IExposeUmbracoTestThings)
                invocation.Proxy;
            var builder = (IUmbracoBuilder)invocation.Arguments[0];
            var services = builder.Services;

            var configuration = ((IConfiguration)ConfigurationProperty.GetValue(fixture)!);
            var testHelper = ((TestHelper)TestHelperProperty.GetValue(fixture)!);

            var settings = new TestDatabaseSettings
            {
                FilesPath = Path.Combine(testHelper.WorkingDirectory, "databases"),
            };
            configuration.Bind("Tests:Database", settings);
            services.AddSingleton(settings);

            services.Configure<ReusedTestDatabaseOptions>(options =>
            {
                options.WorkingDirectory = testHelper.WorkingDirectory;
                // TODO: Inject the seed method
                // ConfigureTestDatabaseOptions(options);
            });

            var databaseType = settings.DatabaseType switch
            {
                TestDatabaseSettings.TestDatabaseType.Sqlite => typeof(ReusableSqliteTestDatabase),
                TestDatabaseSettings.TestDatabaseType.SqlServer => typeof(ReusableSqlServerTestDatabase),
                _ => throw new Exception($"Reusable test database implementation for {settings.DatabaseType} not found.")
            };

            services.AddSingleton(typeof(IReusableTestDatabase), sp => sp.CreateInstance(databaseType));
            services.AddSingleton<ITestDatabase>(sp => sp.GetRequiredService<IReusableTestDatabase>());

            services.AddUnique<IUmbracoContextAccessor, TestUmbracoContextAccessor>();
        }

        invocation.Proceed();

    }
}