using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// market-fr#221: the seller's electronic address (BT-34) is eInvoicing data. The LNE and self-certified
/// SCUs neither sign nor report it. A configuration that still carries it starts, with one warning.
/// </summary>
public class ElectronicAddressTests
{
    private static Dictionary<string, object> Configuration(string privateKey, string? electronicAddress)
    {
        var configuration = new Dictionary<string, object>
        {
            ["Siret"] = FRTestData.Siret,
            ["PrivateKey"] = privateKey,
            ["CertificateSerialNumber"] = "CERT-4711",
            ["LneCertificateNumber"] = "LNE-1",
        };
        if (electronicAddress is not null)
        {
            configuration["ElectronicAddress"] = electronicAddress;
        }
        return configuration;
    }

    private static IMiddlewareBootstrapper Bootstrapper(bool lne, Dictionary<string, object> configuration) => lne
        ? new fiskaltrust.Middleware.SCU.FR.LNE.ScuBootstrapper { Configuration = configuration }
        : new fiskaltrust.Middleware.SCU.FR.SelfCertified.ScuBootstrapper { Configuration = configuration };

    private static (ServiceProvider provider, LogSink sink) Start(IMiddlewareBootstrapper bootstrapper)
    {
        var sink = new LogSink();
        var services = new ServiceCollection();
        services.AddSingleton(sink);
        services.AddSingleton(typeof(ILogger<>), typeof(SinkLogger<>));
        bootstrapper.ConfigureServices(services);
        return (services.BuildServiceProvider(), sink);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AConfiguredAddress_StartsTheScu_WithOneWarning_AndIsNeitherSignedNorReported(bool lne)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        // A field separator was refused while the address was data-set field 6; it is ignored now.
        var (provider, sink) = Start(Bootstrapper(lne, Configuration(privateKey, "123456789|X")));
        using var __ = provider;

        var scu = provider.GetRequiredService<IFRSSCD>();
        provider.GetRequiredService<IFRSSCD>();

        sink.Warnings.Should().ContainSingle().Which.Should().Contain("ElectronicAddress").And.Contain("BT-34");
        var info = JsonDocument.Parse((await scu.GetInfoAsync()).InfoData!).RootElement;
        info.TryGetProperty("ElectronicAddress", out var _).Should().BeFalse("BT-34 is not reported");
        var (response, _) = await scu.ProcessReceiptAsync(FRTestData.Request(), null);
        var dataSet = FRReceiptQrCode.SignedContentOf(response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);
        dataSet.Split('|').Should().HaveCount(25);
        dataSet.Split('|')[5].Should().BeEmpty("field 6 is reserved, formerly BT-34");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithoutAnAddress_NothingIsLogged(bool lne)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var (provider, sink) = Start(Bootstrapper(lne, Configuration(privateKey, null)));
        using var __ = provider;

        provider.GetRequiredService<IFRSSCD>();

        sink.Warnings.Should().BeEmpty();
    }

    /// <summary>The Infocert SCU likewise ignores the address, with one warning, and signs it nowhere.</summary>
    [Theory]
    [InlineData("123456789", 1)]
    [InlineData(null, 0)]
    public async Task Infocert_IgnoresAConfiguredAddress_WithOneWarning(string? electronicAddress, int warnings)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        // The Infocert SCU requires the NF525 mention keys outside a sandbox.
        var configuration = Configuration(privateKey, electronicAddress);
        configuration["SoftwareCategory"] = "B";
        configuration["AttestationNumber"] = "0546";
        configuration["SoftwareVersion"] = "06.07";
        var (provider, sink) = Start(new fiskaltrust.Middleware.SCU.FR.InfoCert.ScuBootstrapper { Configuration = configuration });
        using var __ = provider;

        var scu = provider.GetRequiredService<IFRSSCD>();
        provider.GetRequiredService<IFRSSCD>();

        sink.Warnings.Should().HaveCount(warnings);
        sink.Warnings.Where(x => !(x.Contains("ElectronicAddress") && x.Contains("BT-34"))).Should().BeEmpty();
        var (response, _) = await scu.ProcessReceiptAsync(FRTestData.Request(), null);
        var jws = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        FRJws.Decode(jws).SignedString.Should().NotContain("123456789");
    }

    public sealed class LogSink
    {
        public List<string> Warnings { get; } = new();
    }

    public sealed class SinkLogger<T> : ILogger<T>
    {
        private readonly LogSink _sink;

        public SinkLogger(LogSink sink) => _sink = sink;

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                _sink.Warnings.Add(formatter(state, exception));
            }
        }
    }
}
