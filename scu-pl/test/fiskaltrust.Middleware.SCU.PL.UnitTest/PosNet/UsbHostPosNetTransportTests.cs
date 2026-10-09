using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// The USB host transport against a scripted link, so the semantics that matter legally — what
/// counts as delivered, what may be resent — are pinned without an Android device or a printer.
/// </summary>
public class UsbHostPosNetTransportTests
{
    private const string DeviceUrl = "usbhost://1424:10B0";

    private static readonly byte[] s_answer = [0x02, (byte)'s', (byte)'c', 0x09, (byte)'#', (byte)'1', (byte)'2', (byte)'3', (byte)'4', 0x03];

    [Theory]
    [InlineData("usbhost://1424:10B0", 0x1424, 0x10B0)]
    [InlineData("usbhost://1424:10b0", 0x1424, 0x10B0)]
    [InlineData(" UsbHost://0403:6001 ", 0x0403, 0x6001)]
    [InlineData("usbhost://1:2", 0x0001, 0x0002)]
    public void ParseDeviceAddress_ReadsAUsbDeviceByVendorAndProductId(string deviceUrl, int vendorId, int productId)
    {
        PosNetDeviceAddress.Parse(deviceUrl).Should().Be(new PosNetDeviceAddress.UsbHost(vendorId, productId));
    }

    [Theory]
    [InlineData("usbhost://")]
    [InlineData("usbhost://1424")]
    [InlineData("usbhost://12345:10B0")]
    [InlineData("usbhost://COM9")]
    public void ParseDeviceAddress_RejectsAUsbAddressWithoutBothIds(string deviceUrl)
    {
        var act = () => PosNetDeviceAddress.Parse(deviceUrl);

        act.Should().Throw<PLValidationException>().WithMessage("*usbhost://1424:10B0*");
    }

    [Fact]
    public void UsbHostAddress_PrintsAsItIsWritten()
    {
        new PosNetDeviceAddress.UsbHost(0x1424, 0x10B0).ToString().Should().Be(DeviceUrl);
    }

    [Fact]
    public void LineCoding_CarriesThePrintersDefaults()
    {
        var lineCoding = CdcLineCoding.From(new PosNetConfiguration { DeviceUrl = DeviceUrl });

        // 115200 = 0x0001C200, little endian; 1 stop bit, no parity, 8 data bits.
        lineCoding.ToBytes().Should().Equal(0x00, 0xC2, 0x01, 0x00, 0, 0, 8);
    }

    [Fact]
    public void LineCoding_CarriesTheConfiguredSerialSettings()
    {
        var lineCoding = CdcLineCoding.From(new PosNetConfiguration { DeviceUrl = DeviceUrl, SerialBaudRate = 9600, SerialParity = "Even", SerialStopBits = 2 });

        lineCoding.ToBytes().Should().Equal(0x80, 0x25, 0x00, 0x00, 2, 2, 8);
    }

    [Fact]
    public void FromConfiguration_WithAUsbAddress_ValidatesTheSerialSettingsItSendsAsLineCoding()
    {
        var act = () => PosNetConfiguration.FromConfiguration(new Dictionary<string, object> { ["DeviceUrl"] = DeviceUrl, ["SerialParity"] = "Mark" });

        act.Should().Throw<PLValidationException>().WithMessage("*SerialParity*");
    }

    [Fact]
    public void Factory_WithoutAUsbHostLinkFactory_SaysWhatTheHostHasToRegister()
    {
        var act = () => PosNetTransportFactory.Create(new PosNetConfiguration { DeviceUrl = DeviceUrl });

        act.Should().Throw<NotSupportedException>().WithMessage($"*{nameof(IPosNetUsbHostLinkFactory)}*");
    }

    [Fact]
    public void ConfigureServices_WithAUsbAddress_UsesTheLinkFactoryTheHostRegistered_WithoutOpeningTheDevice()
    {
        var links = new ScriptedLinkFactory();
        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = new Dictionary<string, object> { ["DeviceUrl"] = DeviceUrl },
        };
        var services = new ServiceCollection();
        services.AddSingleton<IPosNetUsbHostLinkFactory>(links);

        bootstrapper.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IPLSSCD>().Should().BeOfType<PosNetPLSSCD>();
        provider.GetRequiredService<IPosNetTransport>().Should().BeOfType<UsbHostPosNetTransport>()
            .Which.Address.Should().Be(new PosNetDeviceAddress.UsbHost(0x1424, 0x10B0));
        // The SCU is built long before the first receipt, possibly before the printer is plugged in.
        links.Opened.Should().Be(0);
    }

    [Fact]
    public async Task SendReceive_OpensTheDeviceWithTheLineCoding_WritesTheFrame_AndAssemblesTheAnswerFromPieces()
    {
        var links = new ScriptedLinkFactory { Answer = () => [s_answer[..3], s_answer[3..7], s_answer[7..]] };
        using var transport = Create(links);
        var frame = PosNetFrame.Encode(PosNetCommands.Scomm());

        var response = await transport.SendReceiveAsync(frame);

        response.Should().Equal(s_answer);
        links.LastAddress.Should().Be(new PosNetDeviceAddress.UsbHost(0x1424, 0x10B0));
        links.LastLineCoding!.ToBytes().Should().Equal(0x00, 0xC2, 0x01, 0x00, 0, 0, 8);
        links.Link!.Written.Should().ContainSingle().Which.Should().Equal(frame);
    }

    [Fact]
    public async Task SendReceive_DropsWhatAnEarlierCommandLeftOnTheLine_BeforeSending()
    {
        var links = new ScriptedLinkFactory { Answer = () => [s_answer] };
        using var transport = Create(links);
        await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));
        // The late answer to a command that timed out arrives while the till is idle.
        links.Link!.Pending.Enqueue([0x02, (byte)'l', (byte)'a', (byte)'t', (byte)'e', 0x03]);

        var response = await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        response.Should().Equal(s_answer);
    }

    [Fact]
    public async Task SendReceive_KeepsTheLinkOpenBetweenCommands()
    {
        var links = new ScriptedLinkFactory { Answer = () => [s_answer] };
        using var transport = Create(links);

        await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));
        await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        links.Opened.Should().Be(1);
    }

    [Fact]
    public async Task SendReceive_AfterThePrinterWasUnpluggedWhileIdle_OpensItAgain_InsteadOfReportingAnAmbiguousOutcome()
    {
        var links = new ScriptedLinkFactory { Answer = () => [s_answer] };
        using var transport = Create(links);
        await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));
        var first = links.Link!;
        first.IsAttached = false;

        var response = await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        response.Should().Equal(s_answer);
        links.Opened.Should().Be(2);
        first.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task SendReceive_WhenTheDeviceIsNotAttached_IsDeviceUnreachable_NotAmbiguous()
    {
        var links = new ScriptedLinkFactory { OpenFailure = new IOException("not attached") };
        using var transport = Create(links);

        var act = () => transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        // Nothing was delivered, so the queue may send the command again.
        var failure = (await act.Should().ThrowAsync<PLDeviceUnreachableException>()).Which;
        failure.Should().BeOfType<PLDeviceUnreachableException>();
        failure.Message.Should().Contain(DeviceUrl);
    }

    [Fact]
    public async Task SendReceive_WithoutUsbPermission_IsDeviceUnreachable_AndSaysSo()
    {
        var links = new ScriptedLinkFactory { OpenFailure = new UnauthorizedAccessException("no permission") };
        using var transport = Create(links);

        var act = () => transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        var failure = (await act.Should().ThrowAsync<PLDeviceUnreachableException>()).Which;
        failure.Should().BeOfType<PLDeviceUnreachableException>();
        failure.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task SendReceive_WhenClearingStaleInputFails_IsDeviceUnreachable_BecauseNothingWasSentYet()
    {
        var links = new ScriptedLinkFactory { Answer = () => [s_answer] };
        using var transport = Create(links);
        await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));
        links.Link!.ReadFailure = new IOException("bulk transfer failed");

        var act = () => transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        (await act.Should().ThrowAsync<PLDeviceUnreachableException>()).Which.Should().BeOfType<PLDeviceUnreachableException>();
        links.Link.Written.Should().HaveCount(1, "the second frame never went out");
    }

    [Fact]
    public async Task SendReceive_WhenWritingFails_IsAmbiguous()
    {
        var links = new ScriptedLinkFactory { WriteFailure = new IOException("bulk transfer failed") };
        using var transport = Create(links);

        var act = () => transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
    }

    [Fact]
    public async Task SendReceive_WhenNoAnswerArrives_IsAmbiguous_AndTheNextCommandOpensTheDeviceAgain()
    {
        var links = new ScriptedLinkFactory { Answer = () => [] };
        using var transport = Create(links, receiveTimeoutMs: 100);

        var act = () => transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        (await act.Should().ThrowAsync<PosNetAmbiguousResponseException>()).Which.Message.Should().Contain("100 ms");
        links.Answer = () => [s_answer];
        (await transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()))).Should().Equal(s_answer);
        links.Opened.Should().Be(2);
    }

    [Fact]
    public async Task SendReceive_WhenThePrinterIsUnpluggedWhileItsAnswerIsAwaited_IsAmbiguous_WithoutWaitingOutTheDeadline()
    {
        var links = new ScriptedLinkFactory();
        links.Answer = () =>
        {
            // Unplugged right after taking the frame: whether it executed the command is unknown.
            links.Link!.IsAttached = false;
            return [];
        };
        using var transport = Create(links, receiveTimeoutMs: 30_000);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var act = () => transport.SendReceiveAsync(PosNetFrame.Encode(PosNetCommands.Scomm()));

        (await act.Should().ThrowAsync<PosNetAmbiguousResponseException>()).Which.Message.Should().Contain("detached");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    private static IPosNetTransport Create(ScriptedLinkFactory links, int receiveTimeoutMs = 1_000)
        => PosNetTransportFactory.Create(new PosNetConfiguration { DeviceUrl = DeviceUrl, SendTimeoutMs = 1_000, ReceiveTimeoutMs = receiveTimeoutMs }, links);

    private sealed class ScriptedLinkFactory : IPosNetUsbHostLinkFactory
    {
        /// <summary>The chunks the device sends in answer to each frame written.</summary>
        public Func<byte[][]> Answer { get; set; } = () => [];
        public Exception? OpenFailure { get; init; }
        public Exception? WriteFailure { get; init; }
        public int Opened { get; private set; }
        public ScriptedLink? Link { get; private set; }
        public PosNetDeviceAddress.UsbHost? LastAddress { get; private set; }
        public CdcLineCoding? LastLineCoding { get; private set; }

        public IPosNetUsbHostLink Open(PosNetDeviceAddress.UsbHost address, CdcLineCoding lineCoding)
        {
            Opened++;
            LastAddress = address;
            LastLineCoding = lineCoding;
            if (OpenFailure is not null)
            {
                throw OpenFailure;
            }
            Link = new ScriptedLink(this);
            return Link;
        }
    }

    private sealed class ScriptedLink(ScriptedLinkFactory factory) : IPosNetUsbHostLink
    {
        public Queue<byte[]> Pending { get; } = new();
        public List<byte[]> Written { get; } = [];
        public bool IsAttached { get; set; } = true;
        public bool Disposed { get; private set; }
        public Exception? ReadFailure { get; set; }

        public void Write(byte[] buffer, int timeoutMs)
        {
            if (factory.WriteFailure is not null)
            {
                throw factory.WriteFailure;
            }
            Written.Add(buffer.ToArray());
            foreach (var chunk in factory.Answer())
            {
                Pending.Enqueue(chunk);
            }
        }

        public int Read(byte[] buffer, int timeoutMs)
        {
            if (ReadFailure is not null)
            {
                throw ReadFailure;
            }
            if (Pending.TryDequeue(out var chunk))
            {
                chunk.CopyTo(buffer, 0);
                return chunk.Length;
            }
            // Nothing there: the platform call would wait out its timeout.
            Thread.Sleep(Math.Min(timeoutMs, 5));
            return 0;
        }

        public void Dispose() => Disposed = true;
    }
}
