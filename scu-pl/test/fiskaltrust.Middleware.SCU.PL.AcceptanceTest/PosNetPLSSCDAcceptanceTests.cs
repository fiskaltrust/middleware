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
    public async Task CashSale_RunsTheFullTransaction_AndReturnsTheFiscalDocumentNumber()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        var result = await sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        emulator.ReceivedMnemonics.Should().Equal("trinit", "trline", "trpayment", "trend", "scnt");
        emulator.TransactionOpen.Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
    }

    [Fact]
    public async Task CardSaleWithChange_SettlesLikeTheSpecExample()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        await sut.ProcessReceiptAsync(PLReceiptExamples.CardSaleWithChange());

        emulator.ReceivedMnemonics.Should().Equal("trinit", "trline", "trpayment", "trpayment", "trend", "scnt");
        var trend = emulator.ReceivedCommands.Single(c => c.CommandId == "trend");
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("to", "200"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("re", "300"));
        trend.Parameters.Should().Contain(new KeyValuePair<string, string>("fp", "500"));
    }

    [Fact]
    public async Task NipReceipt_PrintsTheBuyersNip()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        await sut.ProcessReceiptAsync(PLReceiptExamples.NipReceipt());

        emulator.ReceivedMnemonics.Should().Equal("trinit", "trnipset", "trline", "trpayment", "trend", "scnt");
        var trnipset = emulator.ReceivedCommands.Single(c => c.CommandId == "trnipset");
        trnipset.Parameters.Should().Contain(new KeyValuePair<string, string>("ni", "1234563218"));
    }

    [Fact]
    public async Task ConsecutiveSales_ReuseTheConnection_AndNumberDocumentsSequentially()
    {
        using var emulator = new PosNetPrinterEmulator().Start();
        var sut = GetSUT(emulator);

        var first = await sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());
        var second = await sut.ProcessReceiptAsync(PLReceiptExamples.CashSale());

        emulator.ReceivedMnemonics.Should().HaveCount(10);
        emulator.TransactionOpen.Should().BeFalse();
        first.ReceiptResponse.ftSignatures.Single(s => s.Caption == "Numer dokumentu fiskalnego").Data.Should().Be("85");
        second.ReceiptResponse.ftSignatures.Single(s => s.Caption == "Numer dokumentu fiskalnego").Data.Should().Be("86");
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
