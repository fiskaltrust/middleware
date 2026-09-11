using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class SerialPosNetTransportTests
{
    /// <summary>
    /// A port name no machine has, so opening fails before a byte is sent — on Windows as a missing
    /// COM port, elsewhere as a missing device path.
    /// </summary>
    private const string AbsentPort = "serial://COM254";

    [Fact]
    public async Task SendReceive_WhenThePortCannotBeOpened_IsDeviceUnreachable_NotAmbiguous()
    {
        using var transport = PosNetTransportFactory.Create(new PosNetConfiguration { DeviceUrl = AbsentPort, SendTimeoutMs = 1_000, ReceiveTimeoutMs = 1_000 });

        var act = () => transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        // Nothing was delivered, so the queue may send the command again — which is exactly what
        // an ambiguous outcome must never allow.
        var failure = (await act.Should().ThrowAsync<PLDeviceUnreachableException>()).Which;
        failure.Should().BeOfType<PLDeviceUnreachableException>();
        failure.Message.Should().Contain("COM254");
    }

    [Fact]
    public async Task SendReceive_AfterAFailedOpen_TriesAgainOnTheNextCommand()
    {
        using var transport = PosNetTransportFactory.Create(new PosNetConfiguration { DeviceUrl = AbsentPort, SendTimeoutMs = 1_000, ReceiveTimeoutMs = 1_000 });
        var frame = PosNetFrame.Encode(PosNetCommands.Scomm());

        await transport.Invoking(t => t.SendReceiveAsync(frame)).Should().ThrowAsync<PLDeviceUnreachableException>();
        // A second command is not poisoned by the first failure — the port is simply opened again.
        await transport.Invoking(t => t.SendReceiveAsync(frame)).Should().ThrowAsync<PLDeviceUnreachableException>();
    }

    [Fact]
    public void Factory_PicksTheTransportFromTheDeviceUrl()
    {
        using var serial = PosNetTransportFactory.Create(new PosNetConfiguration { DeviceUrl = "usb://COM9" });
        using var tcp = PosNetTransportFactory.Create(new PosNetConfiguration { DeviceUrl = "tcp://192.168.1.50:6666" });

        serial.Should().BeOfType<SerialPosNetTransport>().Which.PortName.Should().Be("COM9");
        tcp.Should().BeOfType<TcpPosNetTransport>();
    }
}
