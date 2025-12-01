using NUnit.Framework.Interfaces;

namespace NUnitComposition.Extensibility;

public static class FilterContext
{
    private static readonly HashSet<IPreFilter> preFilters = new();

    public static IEnumerable<IPreFilter> PreFilters => preFilters.AsEnumerable();

    public static void RegisterPreFilter(IPreFilter? preFilter)
    {
        if (preFilter != null)
        {
            preFilters.Add(preFilter);
        }
    }

    private static readonly HashSet<ITestFilter> testFilters = new();

    public static IEnumerable<ITestFilter> TestFilters => testFilters.AsEnumerable();

    public static void RegisterTestFilter(ITestFilter? testFilter)
    {
        if (testFilter != null)
        {
            testFilters.Add(testFilter);
        }
    }
}
