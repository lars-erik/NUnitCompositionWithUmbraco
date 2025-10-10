using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // TODO: Should we assert it all, or do that closer to the scopes?
    }
}