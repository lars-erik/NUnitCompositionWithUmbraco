# NUnit Composition with Umbraco

## Overview / Problem statement

**This package has an [InjectionSource] and an [Inject] attribute that may be used to enrich any NUnit suite, 
but the [MakeOneTimeLifecycle] attribute is mainly targeted at Umbraco.**

Umbraco publishes their own tests as NuGet packages for implementors to re-use
instead of writing tons of setup code.

However, the Umbraco base classes set up and tear down the Umbraco instance for each test.  
They may set up the database once per fixture, but each test gets a fresh Umbraco instance.  
You'll have to track whether your own schema is created in your own setup method.

This is a problem if you want to run a large number of tests that all need the same schema.  
This repository contains an example of how to work around this by "hacking" the NUnit
pipeline and modifying the UmbracoIntegrationTest setup and teardown methods to be onetime variants.  

Once the lifecycle is modified to run as one-time variants, we can utilize the `[Inject]` attribute
to inject services from the Umbraco `ServiceProvider` to child tests in the scope.

## Important attributes

The tern "scope fixture" is used to descibe an NUnit fixture with only `OneTimeSetUp` and `OneTimeTearDown` methods.

| Attribute | Description | Dependencies |
| --- | --- | --- |
| `[ExtendableSetUpFixture]` | Replacement for `[SetUpFixture]` Allows IApplyToTest implementations to mutate the lifecycle methods via IExtendableLifecycle. Allows for further extension attributes to "get to do more". | NUnit |
| `[MakeOneTimeLifecycle]` | Moves `[SetUp]` and `[TearDown]` methods to one-time lifecycle. Necessary to use `UmbracoIntegrationTest` and others as base classes for scoped setup fixtures. | NUnit |
| `[InjectionSource]` | Allows a scope fixture to expose an `IServiceProvider` instance to child tests. | NUnit |
| `[Inject]` | Allows a test fixture to receive services from the closest `[InjectionSource]` in the hierarchy. | NUnit |

## General Example

// TODO... 👼

## Umbraco Example

There are two example "scoped" sets of tests in the [FeatureA](./UmbracoTestsComposition/FeatureA) and [FeatureB](./UmbracoTestsComposition/FeatureB) folders of the [UmbracoTestsComposition](./UmbracoTestsComposition) project.

As with NUnit SetUpFixtures, the extandable setup fixture must be in the root namespace of the tests that need it.  
Because there's a lot of singletons and stuff in Umbraco, it is likely not possible, and at least not recommended
to have more than one scoped setup fixture per namespace, and no further ones in child namespaces.  
As of writing Umbraco's base classes already specify `[SingleThreaded]` and `[NonParallelizable]`.

**An Umbraco-scoped setup fixture**

```csharp
namespace FeatureA;

[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerFixture, 
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[MakeOneTimeLifecycle(
    [nameof(Setup), nameof(SetUp_Logging)],
    [nameof(TearDown), nameof(TearDownAsync), nameof(FixtureTearDown), nameof(TearDown_Logging)]
)]
[InjectionProvider(nameof(Services))]
public class FeatureAScope : UmbracoIntegrationTest
{
    public void StubForUmbracoTestDiscovery() {}

    public FeatureAScope()
    {
        // Umbraco's TestOptionAttributeBase looks for the UmbracoTest attribute via the current test method or its declaring type.
        // We need to set a dummy test method from this exact setup fixture.
        // It could possibly be done by sneaking it in to the first instance of onetime setups, but we still need a declared method on this type.
        this.ExposeUmbracoTestAttribute(nameof(StubForUmbracoTestDiscovery));
    }
}
```

**A couple of test fixtures with access to the scoped setup fixture**

```csharp
[Inject(nameof(Inject))]
public class FeatureATests
{
    private IDataTypeService dataTypeService = null!;

    public void Inject(IDataTypeService dataTypeService)
    {
        this.dataTypeService = dataTypeService;
    }

    [Test]
    public async Task CanGetDataTypeFromInjectedService()
    {
        var allTypes = (await dataTypeService.GetAllAsync()).Take(3).ToList();
        Console.WriteLine($"We've got data types like {String.Join(',', allTypes.Select(x => x.Name))}...");
        Assert.That(allTypes, Has.Count.GreaterThan(0));
    }
}

[Inject(nameof(Inject))]
public class FeatureATestUsingSeveralServices
{
    private IDataTypeService dataTypeService = null!;
    private DataEditorCollection editorCollection = null!;
    private IConfigurationEditorJsonSerializer editorSerializer = null!;

    public void Inject(IDataTypeService dataTypeService, DataEditorCollection editorCollection, IConfigurationEditorJsonSerializer editorSerializer)
    {
        this.dataTypeService = dataTypeService;
        this.editorCollection = editorCollection;
        this.editorSerializer = editorSerializer;
    }

    [Test]
    public async Task CanCreateDataTypeUsingAllNecessaryServices()
    {
        var textBoxEditor = editorCollection.Single(x => x.Alias == Constants.PropertyEditors.Aliases.TextBox);
        var result = await dataTypeService.CreateAsync(
            new DataType(textBoxEditor, editorSerializer)
            {
                Name = "A test datatype"
            }, 
            Constants.Security.SuperUserKey);

        Assert.That(result.Success, Is.True, () => $"Failed with status {result.Status} and exception message {result.Exception?.Message ?? "<No exception thrown>"}");
    }
}
```

## Hopes and dreams

### In general
- Make sure informational exceptions are thrown if parallel tests are attempted, unless it can work
- Figure out how to throw for failed setup without crashing VS
- Make it a NuGet package

### With Umbraco
- Allow scope hierarchies utilizing `ICoreScopeProvider` and `IServiceScope` (Possibly another attribute?)
- Apply transactions such that each test fixture or test can roll back changes
- Make a PR to Umbraco for an option on base tests to start Umbraco in onetimeset as well as setup exclusively.
