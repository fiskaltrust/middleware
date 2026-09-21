using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Emulator;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;

/// <summary>
/// The device a test runs against, built through the <see cref="ScuBootstrapper"/> like the
/// launcher would. Three modes, selected by environment:
/// <list type="bullet">
/// <item>nothing set — the emulator replays the test's cassette, or falls back to its device model
/// where no cassette has been recorded yet. This is what CI runs.</item>
/// <item><c>SCU_PL_POSNET_DEVICE_URL</c> — the real printer, over TCP (<c>tcp://host:port</c>) or
/// its USB/COM interface (<c>serial://COM9</c>), with the transport decorated so the same protocol
/// assertions still work.</item>
/// <item>plus <c>SCU_PL_POSNET_RECORD=1</c> — the real printer, and what it answers is written to
/// the test's cassette. Review the diff before committing: a cassette is also written when the test
/// failed.</item>
/// </list>
/// </summary>
public sealed class PosNetTestTarget : IDisposable
{
    public const string DeviceUrlVariable = "SCU_PL_POSNET_DEVICE_URL";
    public const string RecordVariable = "SCU_PL_POSNET_RECORD";

    // The emulator answers instantly, which keeps the ambiguous-outcome test fast. A real printer
    // answers a trend only once the paper has moved.
    private const int EmulatorConnectTimeoutMs = 2_000;
    private const int EmulatorReceiveTimeoutMs = 750;
    private const int HardwareConnectTimeoutMs = 5_000;
    private const int HardwareReceiveTimeoutMs = 30_000;

    private readonly ServiceProvider _services;
    private readonly RecordingPosNetTransport? _recorder;
    private readonly CassetteStore? _cassettes;
    private readonly string _cassetteName;

    /// <summary>The configured printer address, or <c>null</c> when the suite runs against the emulator.</summary>
    public static string? HardwareDeviceUrl =>
        Environment.GetEnvironmentVariable(DeviceUrlVariable) is { Length: > 0 } url ? url : null;

    /// <summary>True while the suite talks to real hardware — tests that script device behaviour cannot run then.</summary>
    public static bool RunsAgainstHardware => HardwareDeviceUrl is not null;

    private static bool Recording => Environment.GetEnvironmentVariable(RecordVariable) is "1" or "true";

    /// <summary>The SCU under test, resolved from the bootstrapper's service collection.</summary>
    public IPLSSCD Sut { get; }

    /// <summary>The configuration the SCU was built with — the rate table in particular.</summary>
    public PosNetConfiguration Configuration { get; }

    /// <summary>
    /// Reads the device back over the very connection the SCU uses, so the read-backs are recorded
    /// into the cassette and replayed in order with everything else.
    /// </summary>
    public PosNetDeviceProbe Probe { get; }

    /// <summary>The emulator serving this test, or <c>null</c> on a hardware run.</summary>
    public PosNetPrinterEmulator? Emulator { get; }

    /// <summary>The commands the SCU sent, without the probe's read-backs.</summary>
    public IEnumerable<PosNetResponse> SentCommands { get; }

    public IEnumerable<string> SentMnemonics { get; }

    /// <summary>
    /// Opens the target for the calling test; the test's name selects its cassette in the given store.
    /// </summary>
    public static PosNetTestTarget Open(CassetteStore cassettes, [CallerMemberName] string cassetteName = "")
    {
        ArgumentNullException.ThrowIfNull(cassettes);
        if (HardwareDeviceUrl is { } url)
        {
            // A run against the printer reads the register's own PTU table, like production does:
            // pinning the customary one here would drive the device with slots it may not have, and
            // would keep the sfsk read out of everything the hardware run — and its recording —
            // proves.
            return new PosNetTestTarget(null, url, HardwareConnectTimeoutMs, HardwareReceiveTimeoutMs, cassettes, cassetteName, pinRateTable: false);
        }

        // Without a recording the emulator improvises from its device model — that keeps the suite
        // runnable before the first cassette exists, and honest about which is which.
        if (!cassettes.Exists(cassetteName))
        {
            var improvising = new PosNetPrinterEmulator();
            return new PosNetTestTarget(improvising.Start(), improvising.DeviceUrl, EmulatorConnectTimeoutMs, EmulatorReceiveTimeoutMs, cassettes, cassetteName, pinRateTable: true);
        }

        // Whether the table is pinned has to match the recording: a cassette taken with the table
        // configured holds no sfsk exchange and only replays with the pin, while one recorded off
        // the register carries the exchange and only replays without it. The cassette says which.
        var cassette = cassettes.Load(cassetteName);
        var recordedTheRateTableRead = cassette.Exchanges.Any(e => e.Command.StartsWith("sfsk", StringComparison.Ordinal));
        var emulator = PosNetPrinterEmulator.Replaying(cassette);
        return new PosNetTestTarget(emulator.Start(), emulator.DeviceUrl, EmulatorConnectTimeoutMs, EmulatorReceiveTimeoutMs, cassettes, cassetteName, pinRateTable: !recordedTheRateTableRead);
    }

    /// <summary>
    /// An emulator scripted to behave in a way no real device can be asked to — a rejected command,
    /// silence, a refused port, a particular device state. Never talks to hardware.
    /// </summary>
    public static PosNetTestTarget Scripted(Action<PosNetPrinterEmulator>? configure = null, bool unreachable = false)
    {
        var emulator = new PosNetPrinterEmulator();
        configure?.Invoke(emulator);
        if (unreachable)
        {
            emulator.StartUnreachable();
        }
        else
        {
            emulator.Start();
        }
        // No pinned rate table: a scripted register reports its own, and the SCU has to read it.
        return new PosNetTestTarget(emulator, emulator.DeviceUrl, EmulatorConnectTimeoutMs, EmulatorReceiveTimeoutMs, cassettes: null, cassetteName: "", pinRateTable: false);
    }

    /// <param name="pinRateTable">
    /// Configures the customary PTU table instead of letting the SCU read it off the register. The
    /// cassettes committed so far were recorded with the table configured, so they hold no
    /// <c>sfsk</c> exchange and pinning is what keeps them replayable; a cassette recorded off the
    /// register carries the exchange and replays without the pin. Runs against a printer never pin.
    /// </param>
    private PosNetTestTarget(PosNetPrinterEmulator? emulator, string deviceUrl, int connectTimeoutMs, int receiveTimeoutMs, CassetteStore? cassettes, string cassetteName, bool pinRateTable)
    {
        Emulator = emulator;
        _cassettes = cassettes;
        _cassetteName = cassetteName;

        var configuration = new Dictionary<string, object>
        {
            ["DeviceUrl"] = deviceUrl,
            ["ConnectTimeoutMs"] = connectTimeoutMs,
            ["ReceiveTimeoutMs"] = receiveTimeoutMs,
        };
        if (pinRateTable)
        {
            configuration["VatRateTable"] = PosNetConfiguration.DefaultVatRateTable();
        }
        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = configuration,
        };
        var services = new ServiceCollection();
        bootstrapper.ConfigureServices(services);

        if (emulator is null)
        {
            // A real printer offers no introspection: decorating the transport is what gives the
            // hardware run the same transcript the emulator hands out for free.
            services.Replace(ServiceDescriptor.Singleton<IPosNetTransport>(provider =>
                new RecordingPosNetTransport(PosNetTransportFactory.Create(provider.GetRequiredService<PosNetConfiguration>()), deviceUrl)));
        }

        _services = services.BuildServiceProvider();
        Sut = _services.GetRequiredService<IPLSSCD>();
        Configuration = _services.GetRequiredService<PosNetConfiguration>();
        _recorder = emulator is null ? (RecordingPosNetTransport)_services.GetRequiredService<IPosNetTransport>() : null;

        var transcript = _recorder?.Transcript ?? emulator!.Transcript;
        SentCommands = transcript.Commands;
        SentMnemonics = transcript.Mnemonics;
        // The same PosNetClient singleton the SCU holds: one connection, one command at a time.
        Probe = new PosNetDeviceProbe(_services.GetRequiredService<PosNetClient>(), transcript);
    }

    public void Dispose()
    {
        if (Recording && _recorder is not null && _cassettes is not null && _cassetteName.Length > 0)
        {
            _cassettes.Save(_recorder.Cassette, _cassetteName);
        }
        _services.Dispose();
        var replayFaults = Emulator?.ReplayFaults.ToList() ?? [];
        Emulator?.Dispose();

        // A replay that left its recording proves nothing about the device, so it fails the test
        // rather than passing on improvised answers — but only when the test would otherwise pass.
        // Disposal runs while an exception from the test body is still propagating, and throwing
        // here would replace it: a drifted cassette would then hide the assertion failure it caused.
        // The drift is not lost in that case, because a fault is answered with a device error the
        // SCU raises inside the test.
        if (replayFaults.Count > 0 && !ExceptionInFlight)
        {
            throw new InvalidOperationException(
                $"The cassette '{_cassetteName}' no longer matches what the SCU sends: {string.Join(" ", replayFaults)}");
        }
    }

    /// <summary>
    /// Whether an exception is currently propagating, i.e. whether this disposal is unwinding a
    /// failing test. There is no first-class way to ask; the runtime exposes it only through the
    /// exception pointers, which are zero on a normal return.
    /// </summary>
    private static bool ExceptionInFlight => Marshal.GetExceptionPointers() != IntPtr.Zero;
}
