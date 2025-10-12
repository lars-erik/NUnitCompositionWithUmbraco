using NUnitComposition.Extensions;

namespace NUnitComposition.SampleScope.SampleFeature
{
    [ScopedTestFixture]
    public class SampleScopedFeatureTest
    {
        [Test]
        public void HasAccessToStateFromScopedSetupInstance()
        {
            Root.Log.Add($"{nameof(SampleScopedFeatureTest)} {nameof(HasAccessToStateFromScopedSetupInstance)} called.");
            Assert.That(SampleScopedSetupFixture.State, Is.EqualTo("Some state from setup"));
        }
    }
}
