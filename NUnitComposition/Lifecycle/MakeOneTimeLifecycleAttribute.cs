using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnitComposition.Extensibility;

namespace NUnitComposition.Lifecycle;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MakeOneTimeLifecycleAttribute : Attribute, IApplyToTest
{
    private readonly string[] setUpNames;
    private readonly string[] tearDownNames;

    public MakeOneTimeLifecycleAttribute(string[] setUpNames, string[] tearDownNames)
    {
        this.setUpNames = setUpNames;
        this.tearDownNames = tearDownNames;
    }

    public void ApplyToTest(Test test)
    {
        if (test is not TestSuite suite || suite.TypeInfo?.Type == null)
        {
            return;
        }

        if (test is IExtendableLifecycle extendable)
        {
            var setUpsToMove = extendable.SetUpMethods.Where(x => setUpNames.Contains(x.Name)).ToArray();
            var tearDownsToMove = extendable.TearDownMethods.Where(x => tearDownNames.Contains(x.Name)).ToArray();

            // This should ensure that the base setup/teardowns are executed before and after the concrete onetime variants.
            var newOneTimeSetUps = setUpsToMove.Union(extendable.OneTimeSetUpMethods).ToArray();
            var newOneTimeTearDowns = extendable.OneTimeTearDownMethods.Union(tearDownsToMove).ToArray();

            var remainingSetUps = extendable.SetUpMethods.Except(setUpsToMove).ToArray();
            var remainingTearDowns = extendable.TearDownMethods.Except(tearDownsToMove).ToArray();

            extendable.SetUpMethods = remainingSetUps;
            extendable.TearDownMethods = remainingTearDowns;
            extendable.OneTimeSetUpMethods = newOneTimeSetUps;
            extendable.OneTimeTearDownMethods = newOneTimeTearDowns;
        }
        else
        {
            throw new Exception($"{nameof(MakeOneTimeLifecycleAttribute)} must be applied to a test fixture with an {nameof(IExtendableLifecycle)} implementation like {nameof(ExtendableSetUpFixture)}.");
        }
    }
}
