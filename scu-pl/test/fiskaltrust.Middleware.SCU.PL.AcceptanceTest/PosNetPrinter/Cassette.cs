using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;

/// <summary>One command and the answer the real printer gave to it. A null response means silence.</summary>
public sealed record CassetteExchange(string Command, string? Response);

/// <summary>
/// A recording of one test's conversation with a real POSNET printer, replayed by the emulator when
/// no device is at hand. Stored are the raw payloads — everything between STX and the checksum —
/// rather than whole frames: the framing is deterministic and re-computed on replay, while the
/// payload keeps the file readable and diffable in review. Decoding into
/// <see cref="PosNet.Protocol.PosNetResponse"/> and re-encoding would not be lossless, as it drops
/// field order and tokens.
/// </summary>
public sealed class Cassette
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string? RecordedAgainst { get; set; }

    public DateTime? RecordedAtUtc { get; set; }

    public List<CassetteExchange> Exchanges { get; set; } = [];

    /// <summary>
    /// The cassette a test replays, from the output folder the csproj copies <c>Cassettes/</c> into.
    /// Reading from the compile-time source path instead would make the whole suite depend on that
    /// path still existing: on a build agent, in a container or from published test binaries it does
    /// not, <see cref="Exists"/> would answer false, and every test would quietly fall back to the
    /// emulator's hand-written model — asserting against improvised answers while the committed
    /// recordings prove nothing.
    /// </summary>
    public static string ReplayPathFor(string name)
        => Path.Combine(AppContext.BaseDirectory, "Cassettes", $"{name}.json");

    /// <summary>
    /// Where recording writes: next to the sources, so a new cassette lands where it is committed
    /// rather than in an output folder the next clean build discards. Resolved from the compile-time
    /// location of this file, which is sound here — recording only ever happens on a developer
    /// machine, against a printer.
    /// </summary>
    public static string RecordPathFor(string name)
        => Path.Combine(ProjectDirectory(), "Cassettes", $"{name}.json");

    public static bool Exists(string name) => File.Exists(ReplayPathFor(name));

    public static Cassette Load(string name)
    {
        var path = ReplayPathFor(name);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No cassette for '{name}'. Record one by running this test once against a printer: " +
                $"set {PosNetTestTarget.DeviceUrlVariable} and {PosNetTestTarget.RecordVariable}=1.", path);
        }
        return JsonSerializer.Deserialize<Cassette>(File.ReadAllText(path), s_json)
            ?? throw new InvalidDataException($"The cassette '{path}' is empty.");
    }

    /// <summary>Writes the recording to the source folder; it is replayed after the next build.</summary>
    public void Save(string name)
    {
        var path = RecordPathFor(name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, s_json));
    }

    private static string ProjectDirectory([CallerFilePath] string thisFile = "")
        => Directory.GetParent(Path.GetDirectoryName(thisFile)!)!.FullName;
}
