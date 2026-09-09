using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class ScuBootstrapperTests
{
    [Fact]
    public void ConfigureServices_ResolvesTheSameSCUInstanceForEveryScope()
    {
        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = new Dictionary<string, object> { ["DeviceUrl"] = "tcp://192.168.1.50:6666" },
        };
        var services = new ServiceCollection();

        bootstrapper.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IPLSSCD>();
        var second = provider.CreateScope().ServiceProvider.GetRequiredService<IPLSSCD>();
        first.Should().BeOfType<PosNetPLSSCD>().And.BeSameAs(second);

        var configuration = provider.GetRequiredService<PosNetConfiguration>();
        configuration.ParseDeviceEndpoint().Should().Be(("192.168.1.50", 6666));
        provider.GetRequiredService<IPosNetTransport>().Should().BeOfType<TcpPosNetTransport>();
    }

    [Fact]
    public void ConfigureServices_WithASerialDeviceUrl_DrivesThePrinterOverItsUsbOrComPort()
    {
        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            // As the Portal would hand it over: every parameter a string.
            Configuration = new Dictionary<string, object> { ["DeviceUrl"] = "usb://COM9", ["SerialBaudRate"] = "115200" },
        };
        var services = new ServiceCollection();

        bootstrapper.ConfigureServices(services);

        // Building the transport must not touch the port: the SCU is constructed long before the
        // first receipt, and possibly on a machine where the printer is not plugged in yet.
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IPLSSCD>().Should().BeOfType<PosNetPLSSCD>();
        provider.GetRequiredService<IPosNetTransport>().Should().BeOfType<SerialPosNetTransport>()
            .Which.PortName.Should().Be("COM9");
    }

    [Fact]
    public void ConfigureServices_WithoutADeviceUrl_FailsWithAClearError()
    {
        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = [],
        };
        var services = new ServiceCollection();

        var act = () => bootstrapper.ConfigureServices(services);

        act.Should().Throw<PLValidationException>().WithMessage("*DeviceUrl*");
    }

    [Fact]
    public void ConfigureServices_WithAnUnsupportedSerialSetting_FailsWhenConfigured_NotAtTheFirstReceipt()
    {
        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = new Dictionary<string, object> { ["DeviceUrl"] = "serial://COM9", ["SerialParity"] = "Mark" },
        };
        var services = new ServiceCollection();

        var act = () => bootstrapper.ConfigureServices(services);

        act.Should().Throw<PLValidationException>().WithMessage("*SerialParity*");
    }
}
