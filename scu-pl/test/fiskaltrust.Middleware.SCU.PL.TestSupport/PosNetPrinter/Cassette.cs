using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;

/// <summary>One command and the answer the real printer gave to it. A null response means silence.</summary>
public sealed record CassetteExchange(string Command, string? Response);

/// <summary>
/// A recording of one test's conversation with a real POSNET printer, replayed by the emulator when
/// no device is at hand. Stored are the raw payloads — everything between STX and the checksum —
/// rather than whole frames: the framing is deterministic and re-computed on replay, while the
/// payload keeps the file readable and diffable in review. Decoding into
/// <see cref="PosNet.Protocol.PosNetResponse"/> and re-encoding would not be lossless, as it drops
/// field order and tokens. Where a cassette is read from and written to is the
/// <see cref="CassetteStore"/>'s business.
/// </summary>
public sealed class Cassette
{
    internal static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string? RecordedAgainst { get; set; }

    public DateTime? RecordedAtUtc { get; set; }

    public List<CassetteExchange> Exchanges { get; set; } = [];
}

/// <summary>
/// The folder a test project keeps its cassettes in — two folders, in fact, and they differ on purpose.
/// </summary>
/// <remarks>
/// Replay reads from the output folder the test project copies its <c>Cassettes/</c> into. Reading
/// from the compile-time source path instead would make the whole suite depend on that path still
/// existing: on a build agent, in a container or from published test binaries it does not,
/// <see cref="Exists"/> would answer false, and every test would quietly fall back to the emulator's
/// device model — asserting against improvised answers while the committed recordings prove nothing.
/// Recording writes next to the sources, so a new cassette lands where it is committed rather than in
/// an output folder the next clean build discards. That path is resolved by each test project from
/// the compile-time location of one of its own files (see <see cref="ForTestProject"/>), which is
/// sound: recording only ever happens on a developer machine, against a printer.
/// </remarks>
public sealed class CassetteStore
{
    public CassetteStore(string replayDirectory, string recordDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordDirectory);
        ReplayDirectory = replayDirectory;
        RecordDirectory = recordDirectory;
    }

    /// <summary>
    /// The store of a test project whose csproj copies <c>Cassettes/**</c> to the output folder.
    /// </summary>
    /// <param name="projectDirectory">The project's source folder, typically derived from a <c>[CallerFilePath]</c> inside it.</param>
    public static CassetteStore ForTestProject(string projectDirectory)
        => new(Path.Combine(AppContext.BaseDirectory, "Cassettes"), Path.Combine(projectDirectory, "Cassettes"));

    public string ReplayDirectory { get; }

    public string RecordDirectory { get; }

    public string ReplayPathFor(string name) => Path.Combine(ReplayDirectory, $"{name}.json");

    public string RecordPathFor(string name) => Path.Combine(RecordDirectory, $"{name}.json");

    public bool Exists(string name) => File.Exists(ReplayPathFor(name));

    public Cassette Load(string name)
    {
        var path = ReplayPathFor(name);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No cassette for '{name}'. Record one by running this test once against a printer: " +
                $"set {PosNetTestTarget.DeviceUrlVariable} and {PosNetTestTarget.RecordVariable}=1.", path);
        }
        return JsonSerializer.Deserialize<Cassette>(File.ReadAllText(path), Cassette.Json)
            ?? throw new InvalidDataException($"The cassette '{path}' is empty.");
    }

    /// <summary>Writes the recording to the source folder; it is replayed after the next build.</summary>
    public void Save(Cassette cassette, string name)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        var path = RecordPathFor(name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(cassette, Cassette.Json));
    }
}
