using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.AcceptanceTest.Emulator;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;

/// <summary>
/// The device an acceptance test runs against, built through the <see cref="ScuBootstrapper"/> like
/// the launcher would. Three modes, selected by environment:
/// <list type="bullet">
/// <item>nothing set — the emulator replays the test's cassette, or falls back to its hand-written
/// device model where no cassette has been recorded yet. This is what CI runs.</item>
/// <item><c>SCU_PL_POSNET_DEVICE_URL</c> — the real printer, with the transport decorated so the
/// same protocol assertions still work.</item>
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
    private readonly string _cassetteName;

    /// <summary>The configured printer address, or <c>null</c> when the suite runs against the emulator.</summary>
    public static string? HardwareDeviceUrl =>
        Environment.GetEnvironmentVariable(DeviceUrlVariable) is { Length: > 0 } url ? url : null;

    /// <summary>True while the suite talks to real hardware — tests that script device behaviour cannot run then.</summary>
    public static bool RunsAgainstHardware => HardwareDeviceUrl is not null;

    private static bool Recording => Environment.GetEnvironmentVariable(RecordVariable) is "1" or "true";

    /// <summary>The SCU under test, resolved from the bootstrapper's service collection.</summary>
    public IPLSSCD Sut { get; }

    /// <summary>The emulator serving this test, or <c>null</c> on a hardware run.</summary>
    public PosNetPrinterEmulator? Emulator { get; }

    public IEnumerable<PosNetResponse> SentCommands { get; }

    public IEnumerable<string> SentMnemonics { get; }

    /// <summary>Opens the target for the calling test; the test's name selects its cassette.</summary>
    public static PosNetTestTarget Open([CallerMemberName] string cassetteName = "")
    {
        if (HardwareDeviceUrl is { } url)
        {
            return new PosNetTestTarget(null, url, HardwareConnectTimeoutMs, HardwareReceiveTimeoutMs, cassetteName);
        }

        // Without a recording the emulator improvises from its own device model — that keeps the
        // suite runnable before the first cassette exists, and honest about which is which.
        var emulator = Cassette.Exists(cassetteName)
            ? PosNetPrinterEmulator.Replaying(Cassette.Load(cassetteName))
            : new PosNetPrinterEmulator();
        return new PosNetTestTarget(emulator.Start(), emulator.DeviceUrl, EmulatorConnectTimeoutMs, EmulatorReceiveTimeoutMs, cassetteName);
    }

    /// <summary>
    /// An emulator scripted to behave in a way no real device can be asked to — a rejected command,
    /// silence, a refused port. Never talks to hardware.
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
        return new PosNetTestTarget(emulator, emulator.DeviceUrl, EmulatorConnectTimeoutMs, EmulatorReceiveTimeoutMs, cassetteName: "");
    }

    private PosNetTestTarget(PosNetPrinterEmulator? emulator, string deviceUrl, int connectTimeoutMs, int receiveTimeoutMs, string cassetteName)
    {
        Emulator = emulator;
        _cassetteName = cassetteName;

        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = new Dictionary<string, object>
            {
                ["DeviceUrl"] = deviceUrl,
                ["ConnectTimeoutMs"] = connectTimeoutMs,
                ["ReceiveTimeoutMs"] = receiveTimeoutMs,
            },
        };
        var services = new ServiceCollection();
        bootstrapper.ConfigureServices(services);

        if (emulator is null)
        {
            // A real printer offers no introspection: decorating the transport is what gives the
            // hardware run the same transcript the emulator hands out for free.
            services.Replace(ServiceDescriptor.Singleton<IPosNetTransport>(provider =>
                new RecordingPosNetTransport(new TcpPosNetTransport(provider.GetRequiredService<PosNetConfiguration>()), deviceUrl)));
        }

        _services = services.BuildServiceProvider();
        Sut = _services.GetRequiredService<IPLSSCD>();
        _recorder = emulator is null ? (RecordingPosNetTransport)_services.GetRequiredService<IPosNetTransport>() : null;

        var transcript = _recorder?.Transcript ?? emulator!.Transcript;
        SentCommands = transcript.Commands;
        SentMnemonics = transcript.Mnemonics;
    }

    public void Dispose()
    {
        if (Recording && _recorder is not null && _cassetteName.Length > 0)
        {
            _recorder.Cassette.Save(_cassetteName);
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
