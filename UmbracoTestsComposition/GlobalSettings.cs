using System.Diagnostics;
using NUnit.Framework.Internal;
using Serilog;
using ThreadState = System.Diagnostics.ThreadState;

namespace UmbracoTestsComposition;

[SetUpFixture]
public class GlobalSettings : GlobalSetupTeardown
{
    [OneTimeTearDown]
    public void CheckForLingeringThreads()
    {
        TestContext.Progress.WriteLine("==== Active Threads in last teardown ====");
        foreach (var t in Process.GetCurrentProcess().Threads.Cast<ProcessThread>())
            TestContext.Progress.WriteLine($"Thread {t.Id}: {t.ThreadState} {(t.ThreadState == ThreadState.Wait ? t.WaitReason : "N/A")} {t.Site?.GetType().Name} {t.Container?.GetType().Name}");
        TestContext.Progress.WriteLine();

        Log.CloseAndFlush();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        TestContext.Progress.WriteLine("==== Active Threads after closing and flushing serilog plus GC finalizing ====");
        foreach (var t in Process.GetCurrentProcess().Threads.Cast<ProcessThread>())
            TestContext.Progress.WriteLine($"Thread {t.Id}: {t.ThreadState} {(t.ThreadState == ThreadState.Wait ? t.WaitReason : "N/A")} {t.Site?.GetType().Name} {t.Container?.GetType().Name}");
        TestContext.Progress.WriteLine();

    }
}