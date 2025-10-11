using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnitComposition.SampleScope;

namespace NUnitComposition;

[SetUpFixture]
public class Root
{
    public static List<string> Log = null!;

    [OneTimeSetUp]
    public static void PrepareSharedLog()
    {
        Log = new();
        Log.Add("Root fixture created shared log.");
    }

    [OneTimeTearDown]
    public static void ReportLog()
    {
        Log.Add("Root fixture reporting on all that happened.");

        foreach (var logEntry in Log)
        {
            Console.WriteLine(logEntry);
        }

        Assert.That(Log, Has.Some.Contains(nameof(SampleScopedSetupFixture)));
        var scopedLogEntries = Log.Where(x => x.Contains(nameof(SampleScopedSetupFixture)));
        Console.WriteLine($"Root verified that \n -{String.Join("\n -", scopedLogEntries)}\nwere called from the scoped setup fixture.");
    }
}