using System;
using System.IO;

namespace UmbracoTestsComposition.ReusedDatabase;

public static class ReusedDatabaseStorage
{
    private static readonly object SyncRoot = new();
    private static string? rootDirectory;

    public static string Initialize(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path must be provided.", nameof(rootPath));
        }

        lock (SyncRoot)
        {
            rootDirectory ??= rootPath;
            Directory.CreateDirectory(rootDirectory);
            return rootDirectory;
        }
    }

    public static string RootDirectory => rootDirectory ??
        throw new InvalidOperationException("The reused database storage has not been initialized.");

    public static string DatabaseFilePath => Path.Combine(RootDirectory, "reused-database.sqlite");

    private static string SeedVersionPath => Path.Combine(RootDirectory, "seed-version.txt");

    private static string OutdatedMarkerPath => Path.Combine(RootDirectory, "outdated.flag");

    public static bool ShouldRebuild(string expectedSeedVersion)
    {
        if (!File.Exists(DatabaseFilePath))
        {
            return true;
        }

        if (File.Exists(OutdatedMarkerPath))
        {
            return true;
        }

        if (!File.Exists(SeedVersionPath))
        {
            return true;
        }

        var currentVersion = File.ReadAllText(SeedVersionPath).Trim();
        return !string.Equals(currentVersion, expectedSeedVersion, StringComparison.Ordinal);
    }

    public static void MarkOutdated()
    {
        Directory.CreateDirectory(RootDirectory);
        File.WriteAllText(OutdatedMarkerPath, DateTimeOffset.UtcNow.ToString("O"));
    }

    public static void MarkSeeded(string seedVersion)
    {
        Directory.CreateDirectory(RootDirectory);
        File.WriteAllText(SeedVersionPath, seedVersion);

        if (File.Exists(OutdatedMarkerPath))
        {
            File.Delete(OutdatedMarkerPath);
        }
    }
}
