using Microsoft.Extensions.DependencyInjection;
using NUnitComposition.Extensibility;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.Integration.Tests.Extensions;
using Umbraco.Community.Integration.Tests.Extensions.Database;

namespace UmbracoTestsComposition.ReusedDatabaseByAttribute;

public class DataTypeSeed
{
    public static void ConfigureSeeding(ReusableTestDatabaseOptions options)
    {
        options.NeedsNewSeed = _ => Task.FromResult(true);
        options.SeedData = async (services) =>
        {
            await TestContext.Progress.WriteLineAsync("Creating datatype");
            await services.GetRequiredService<IDataTypeService>().CreateAsync(
                new DataType
                (
                    new TextboxPropertyEditor
                    (
                        services.GetRequiredService<IDataValueEditorFactory>(),
                        services.GetRequiredService<IIOHelper>()
                    ),
                    services.GetRequiredService<IConfigurationEditorJsonSerializer>()
                )
                {
                    Name = "A seeded textbox"
                },
                Umbraco.Cms.Core.Constants.Security.SuperUserKey
            );
        };
    }
}

[UmbracoTest(
    Database = UmbracoTestOptions.Database.NewSchemaPerTest,
    Boot = true,
    Logger = UmbracoTestOptions.Logger.Console
)]
[ExtendableSetUpFixture]
[OneTimeUmbracoSetUp]
[ReusableDatabase(typeof(DataTypeSeed), nameof(DataTypeSeed.ConfigureSeeding))]
[ServiceProvider]
public class ReusedDbAttributeSetUp : UmbracoIntegrationTest
{
    [OneTimeSetUp]
    public void Initialize()
    {
        TestContext.Progress.WriteLine("We should have Umbraco running with our reusable test database here!");
        Assert.That(Services.GetRequiredService<ITestDatabase>(), Is.InstanceOf<IReusableTestDatabase>());
    }
}