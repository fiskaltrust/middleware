using System.Runtime.CompilerServices;
using fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;
using Xunit;

// More than one class in this project talks to the register, and on a hardware run that is one
// printer: two receipts in flight at once would interleave their transactions and the read-backs
// around them, and a cassette recorded that way would not replay. One test at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>This project's cassettes: replayed from the output folder, recorded next to these sources.</summary>
internal static class TestProject
{
    public static CassetteStore Cassettes { get; } = CassetteStore.ForTestProject(ProjectDirectory());

    private static string ProjectDirectory([CallerFilePath] string thisFile = "") => Path.GetDirectoryName(thisFile)!;
}
