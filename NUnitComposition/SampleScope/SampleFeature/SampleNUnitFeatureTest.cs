using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitComposition.SampleScope.SampleFeature
{
    public class SampleNUnitFeatureTest
    {
        [Test]
        public void NUnitFeatureTest()
        {
            Root.Log.Add($"{nameof(SampleNUnitFeatureTest)} {nameof(NUnitFeatureTest)} called.");
            Assert.Pass();
        }

        [Test]
        public void AnotherNUnitFeatureTest()
        {
            Root.Log.Add($"{nameof(SampleNUnitFeatureTest)} {nameof(AnotherNUnitFeatureTest)} called.");
            Assert.Pass();
        }

    }
}
