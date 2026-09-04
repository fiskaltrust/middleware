using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.AcceptanceTest.Emulator;
using fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>
/// The emulator is the evidence the acceptance suite rests on in CI, so the properties that make it
/// evidence are asserted themselves: a cassette answers the commands it recorded, and nothing else.
/// </summary>
public class PosNetPrinterEmulatorTests
{
    private static PosNetClient ClientFor(PosNetPrinterEmulator emulator)
        => new(new TcpPosNetTransport(new PosNetConfiguration
        {
            DeviceUrl = emulator.DeviceUrl,
            ConnectTimeoutMs = 2_000,
            ReceiveTimeoutMs = 2_000,
        }));

    [Fact]
    public async Task Replay_OfADifferentCommand_IsReportedAsAReplayFault()
    {
        var cassette = new Cassette { Exchanges = [new("scnt\t", "scnt\tbt85\t")] };
        using var emulator = PosNetPrinterEmulator.Replaying(cassette).Start();
        using var client = ClientFor(emulator);

        var act = () => client.ExecuteAsync(PosNetCommands.Scomm());

        await act.Should().ThrowAsync<PLDeviceErrorException>();
        emulator.ReplayFaults.Should().ContainSingle().Which.Should().Contain("recorded the command 'scnt")
            .And.Contain("the SCU sent 'scomm");
    }

    [Fact]
    public async Task Replay_PastTheEndOfTheRecording_IsReportedAsAReplayFault()
    {
        var cassette = new Cassette { Exchanges = [new("scomm\t", "scomm\tfsT\t")] };
        using var emulator = PosNetPrinterEmulator.Replaying(cassette).Start();
        using var client = ClientFor(emulator);

        await client.ExecuteAsync(PosNetCommands.Scomm());
        var act = () => client.ExecuteAsync(PosNetCommands.Scnt());

        // Improvising past the recording would answer for a device that was never asked.
        await act.Should().ThrowAsync<PLDeviceErrorException>();
        emulator.ReplayFaults.Should().ContainSingle().Which.Should().Contain("holds 1 exchange(s)");
    }

    [Fact]
    public async Task Replay_OfTheRecordedCommand_AnswersFromTheRecording()
    {
        var cassette = new Cassette { Exchanges = [new("scomm\t", "scomm\tfsT\tnuZBF 2101002392\t")] };
        using var emulator = PosNetPrinterEmulator.Replaying(cassette).Start();
        using var client = ClientFor(emulator);

        var response = await client.ExecuteAsync(PosNetCommands.Scomm());

        response.Parameters.Should().Contain(new KeyValuePair<string, string>("nu", "ZBF 2101002392"));
        emulator.ReplayFaults.Should().BeEmpty();
    }
}
