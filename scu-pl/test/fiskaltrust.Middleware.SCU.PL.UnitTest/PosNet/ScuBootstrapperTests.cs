using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet;
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
}
