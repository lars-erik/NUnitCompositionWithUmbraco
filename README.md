# NUnit Composition with Umbraco

## Overview / Problem statement

Umbraco publishes their own tests as NuGet packages for implementors to re-use
instead of writing tons of setup code.

However, the Umbraco base classes set up and tear down the Umbraco instance for each test.  
They may set up the database once per fixture, but each test gets a fresh Umbraco instance.  
You'll have to track whether your own schema is created in your own setup method.

This is a problem if you want to run a large number of tests that all need the same schema.  
This repository contains an example of how to work around this by "hacking" the NUnit
pipeline and changing the UmbracoIntegrationTest setup and teardown methods to be onetime variants.

As of this writing, the "scoped setup fixtures" expose a static instance property that holds
a reference to the currently active "scoped setup fixture". It is absolutely not ready for
parallel threads or other multi-threaded uses.

## Important classes

There are subject of consideration for a proper NuGet package or possibly inclusion in Umbraco.

| Class | Description | Dependencies |
| --- | --- | --- |
| [ScopedSetupFixtureAttribute](./NUnitComposition/Extensions/ScopedSetupFixtureAttribute.cs) | A replacement for the NUnit [SetUpFixture] attribute that allows for scoped setup/teardown when inheriting library base classes that use [SetUp] and [TearDown]. | Possible to use with NUnit alone |
| [ScopedUmbracoIntegrationSetupFixture](./UmbracoTestsComposition/Common/ScopedUmbracoIntegrationSetupFixture.cs) | An intermediate base class between a scoped setup fixture and UmbracoIntegrationTest, providing access to the Umbraco instance | Dependent on UmbracoIntegrationTests |

## Example

**A scoped setup fixture**

```csharp
[ScopedSetupFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
public class ScopedSetupFixture : ScopedUmbracoIntegrationSetupFixture<MyScopedSetupFixture>
{
    // The name passed to base is necessary to fake a current test for Umbraco's base classes to find the UmbracoTest attribute.
    // It has to point to a method in _this_ class, not from any base class.
    public ScopedSetupFixture() : base(nameof(CreateData))
    {
    }

    [SetUp]
    public void CreateData()
    {
        // You can use GetRequiredService<T>() to use Umbraco services here
    }

    [TearDown]
    public void Cleanup()
    {
        // Perform one-time cleanup here
    }
}
```

**A test with access to the scoped fixture**

```csharp
public class ATestWithAccessToTheFixture
{
    [Test]
    public void ICanReachUmbraco()
    {
        var dataTypeService = ScopedSetupFixture.Instance.Services.GetRequiredService<IDataTypeService>();
        var allTypes = (await dataTypeService.GetAllAsync()).Take(3).ToList();
        Console.WriteLine($"We've got data types like {String.Join(',', allTypes.Select(x => x.Name))}...");
        Assert.That(allTypes, Has.Count.GreaterThan(0));
    }
}
```

## Hopes and dreams

- Allow services from the scope to be injected via constructor injection
- Make another base class for scoped `UmbracoTestServerTestBase`
- Allow scope hierarchies utilizing `ICoreScopeProvider` and `IServiceScope` (Possibly another attribute?)
- Apply transactions such that each test fixture or test can roll back changes
- Make sure informational exceptions are thrown if parallel tests are attempted, unless it can work
- Make it a NuGet package or PR to Umbraco