using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.PL.AcceptanceTest.Emulator;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>
/// Acceptance tests in the shape of the Italian SCU acceptance suite — the SUT is built through
/// the ScuBootstrapper like the launcher would — but market-scoped: instead of a hardware printer
/// they run against an in-process POSNET emulator on real TCP, so the whole stack (transport,
/// codec, transaction flow) is exercised in CI without a device.
/// </summary>
public class PosNetPLSSCDAcceptanceTests
{
    private static IPLSSCD GetSUT(PosNetPrinterEmulator emulator)
    {
        var bootstrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = new Dictionary<string, object>
            {
                ["DeviceUrl"] = emulator.DeviceUrl,
                // Short receive timeout keeps the ambiguous-outcome test fast.
                ["ReceiveTimeoutMs"] = 750,
                ["ConnectTimeoutMs"] = 2000,
            },
        };
        var services = new ServiceCollection();
        bootstrapper.ConfigureServices(services);
        return services.BuildServiceProvider().GetRequiredService<IPLSSCD>();
    }

    [Fact]
    public async Task CashSale_RunsTheFullTransactionAndClosesIt()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        var result = await sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        result.ReceiptResponse.Should().NotBeNull();
        emulator.ReceivedMnemonics.Should().Equal("trinit", "trline", "trpayment", "trend");
        emulator.TransactionOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CardSaleWithChange_SettlesLikeTheSpecExample()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        await sut.ProcessReceiptAsync(PLReceiptExamples.CardSaleWithChange());

        emulator.ReceivedMnemonics.Should().Equal("trinit", "trline", "trpayment", "trpayment", "trend");
        var trend = emulator.ReceivedCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "200"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("re", "300"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("fp", "500"));
    }

    [Fact]
    public async Task ConsecutiveSales_ReuseTheConnection()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        await sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());
        await sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        emulator.ReceivedMnemonics.Should().HaveCount(8);
        emulator.TransactionOpen.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroReceipt_ReadsTheDeviceStatus()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        await sut.ProcessReceiptAsync(PLReceiptExamples.ZeroReceipt());

        emulator.ReceivedMnemonics.Should().Equal("scomm");
    }

    [Fact]
    public async Task GetInfo_ReportsTheFiscalizedRegister()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        var info = await sut.GetInfoAsync();

        var deviceInfo = PLDeviceInfo.FromPLSSCDInfo(info);
        deviceInfo!.FiscalizationState.Should().Be(PLFiscalizationState.Fiscalized);
    }

    [Fact]
    public async Task RejectedLine_CancelsTheOpenTransactionOnTheDevice()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        emulator.ErrorOn("trline", 2005);
        var sut = GetSUT(emulator);

        var act = () => sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(2005);
        emulator.ReceivedMnemonics.Should().Equal("trinit", "trline", "prncancel");
        emulator.TransactionOpen.Should().BeFalse();
    }

    [Fact]
    public async Task SilentPrinter_IsAmbiguous_NothingElseIsSent()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        emulator.SwallowOn("trpayment");
        var sut = GetSUT(emulator);

        var act = () => sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
        // Exactly one trpayment and no cleanup afterwards: the device may have printed — the
        // operator must verify before anything is sent again (triple-print protection).
        emulator.ReceivedMnemonics.Should().Equal("trinit", "trline", "trpayment");
    }

    [Fact]
    public async Task UnreachablePrinter_FailsAsDeviceUnreachable()
    {
        using var emulator = new PosNetPrinterEmulator().StartUnreachable();
        var sut = GetSUT(emulator);

        var act = () => sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        await act.Should().ThrowAsync<PLDeviceUnreachableException>();
        emulator.ReceivedMnemonics.Should().BeEmpty();
    }
}
