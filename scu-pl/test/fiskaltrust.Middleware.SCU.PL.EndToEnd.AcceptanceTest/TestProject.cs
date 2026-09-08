using System.Runtime.CompilerServices;
using fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;
using Xunit;

// One printer, one test at a time: on a hardware run two tests sharing the device would interleave
// their transactions, and the recorded cassettes would not be reproducible.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest;

/// <summary>This project's cassettes and business cases: read from the output folder, cassettes recorded next to these sources.</summary>
internal static class TestProject
{
    public static CassetteStore Cassettes { get; } = CassetteStore.ForTestProject(ProjectDirectory());

    /// <summary>
    /// The JSON request of a committed business case, by its folder name — the same key the test
    /// launcher serves it under (<c>POST /samples/{name}</c>).
    /// </summary>
    public static string BusinessCase(string name)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "BusinessCases", name);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"There is no business case '{name}' under {Path.Combine(AppContext.BaseDirectory, "BusinessCases")}.");
        }
        // Every file counts, not only *.json — the same rule the test launcher serves these folders
        // by, so a case that passes here is one the launcher can serve too.
        var files = Directory.EnumerateFiles(directory).ToList();
        if (files.Count != 1)
        {
            throw new InvalidOperationException($"The business case '{name}' holds {files.Count} files; one request per case is the layout.");
        }
        return File.ReadAllText(files[0]);
    }

    private static string ProjectDirectory([CallerFilePath] string thisFile = "") => Path.GetDirectoryName(thisFile)!;
}
