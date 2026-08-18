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
    /// The cassette file of a test, next to the sources. Resolved from the compile-time location of
    /// this file, so recording writes where the cassettes are committed rather than into bin/.
    /// </summary>
    public static string PathFor(string name)
        => Path.Combine(ProjectDirectory(), "Cassettes", $"{name}.json");

    public static bool Exists(string name) => File.Exists(PathFor(name));

    public static Cassette Load(string name)
    {
        var path = PathFor(name);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No cassette for '{name}'. Record one by running this test once against a printer: " +
                $"set {PosNetTestTarget.DeviceUrlVariable} and {PosNetTestTarget.RecordVariable}=1.", path);
        }
        return JsonSerializer.Deserialize<Cassette>(File.ReadAllText(path), s_json)
            ?? throw new InvalidDataException($"The cassette '{path}' is empty.");
    }

    public void Save(string name)
    {
        var path = PathFor(name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, s_json));
    }

    private static string ProjectDirectory([CallerFilePath] string thisFile = "")
        => Directory.GetParent(Path.GetDirectoryName(thisFile)!)!.FullName;
}
