using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetPLSSCDTests
{
    /// <summary>The table is pinned here; reading it off the register is covered by its own tests below.</summary>
    private static readonly PosNetConfiguration s_configuration = new() { DeviceUrl = "tcp://localhost:6666", VatRateTable = PosNetConfiguration.DefaultVatRateTable() };

    private static readonly PosNetConfiguration s_configurationWithoutRateTable = new() { DeviceUrl = "tcp://localhost:6666" };

    private static PosNetPLSSCD CreateSut(FakePosNetTransport transport)
        => new(s_configuration, new PosNetClient(transport));

    private static ProcessRequest CreateSaleRequest(decimal amount = 9.99m, decimal payment = 9.99m) => new()
    {
        ReceiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
            cbChargeItems =
            [
                new ChargeItem { Description = "Candies", Amount = amount, Quantity = 1m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0011 },
            ],
            cbPayItems =
            [
                new PayItem { Description = "Cash", Amount = payment, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001 },
            ],
        },
        ReceiptResponse = new ReceiptResponse { ftCashBoxIdentification = "test", ftQueueID = Guid.NewGuid() },
    };

    [Fact]
    public async Task ProcessReceiptAsync_Sale_SendsTheFullCommandSequence_AndReadsTheFiscalNumber()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        result.ReceiptResponse.ftReceiptIdentification.Should().EndWith("85");
        // The numer unikatowy is a legal element of the fiscal document and identifies the register.
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer unikatowy" && s.Data == "ZBF 2101002392");
        result.ReceiptResponse.ftCashBoxIdentification.Should().Be("ZBF 2101002392");
    }

    [Fact]
    public async Task ProcessReceiptAsync_NipReceipt_SendsTrnipsetInsideTheTransaction()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0020_0001;
        request.ReceiptRequest.cbCustomer = """{"CustomerVATId": "123-456-32-18"}""";

        await sut.ProcessReceiptAsync(request);

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trnipset", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
        transport.SentPayloads.Single(p => p.StartsWith("trnipset")).Should().Contain("ni1234563218");
    }

    [Fact]
    public async Task ProcessReceiptAsync_NipReceiptWithoutCustomerVatId_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0020_0001;


        (await sut.ProcessReceiptAsync(request)).FailureMessage();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_Sale_AnchorsTheReceiptInTheRegister()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        // The receipt number repeats on a register with a daily counter; numer unikatowy + daily
        // report + receipt number + device moment do not, and di links into the protected memory.
        var signatures = result.ReceiptResponse.ftSignatures;
        signatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.CurrentDailyReportNumber).Which.Data.Should().Be("13");
        // The report is still to be printed, so the sale does not claim the closing's signature type.
        signatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ZReportNumber);
        signatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber).Which.Data.Should().Be("96");
        signatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.DeviceMoment).Which.Data.Should().Be("2026-09-08T17:54:51+02:00");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AnotherReceiptCompletedBeforeTheReadback_LeavesTheReceiptUnanchored()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.CompleteAnotherReceiptAfterTrend();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        // Every number read back describes the last document, which is now someone else's: none
        // of them may be attributed to this receipt — but the printed receipt does not fail.
        result.ReceiptResponse.ftSignatures.Should().NotContain(s =>
            (ulong)s.ftSignatureType == (ulong)SignatureTypePL.FiscalDocumentNumber
            || (ulong)s.ftSignatureType == (ulong)SignatureTypePL.CurrentDailyReportNumber
            || (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber
            || (ulong)s.ftSignatureType == (ulong)SignatureTypePL.DeviceMoment);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer unikatowy");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AnotherReceiptCanceledBeforeTheReadback_LeavesTheReceiptUnanchored()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.CancelAnotherReceiptAfterTrend();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        // bn moved by exactly one — this receipt — but bt numbers canceled receipts too, so the last
        // receipt number is now the canceled one's and must not be signed as this receipt's.
        result.ReceiptResponse.ftSignatures.Should().NotContain(s =>
            (ulong)s.ftSignatureType == (ulong)SignatureTypePL.FiscalDocumentNumber
            || (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber);
    }

    [Fact]
    public async Task ProcessReceiptAsync_IsoClockWithoutOffset_ReadsTheWallClockAsPolishTime()
    {
        var transport = FakePosNetTransport.Confirming();
        // Parsed on its own, tm would take the host's zone; da is the register's wall clock.
        transport.Answer("rtcget", "rtcget	da2026-10-09;11:49	tm2026-10-09T11:49:13	");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.DeviceMoment)
            .Which.Data.Should().Be("2026-10-09T11:49+02:00");
    }

    [Fact]
    public async Task ProcessReceiptAsync_FailedGuardReadback_DoesNotFailTheReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("scnt", occurrence: 2);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        // The register still answers its state and is idle, so the failed guard is final: no waiting.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt", "!sprn", "!sdev");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Numer dokumentu fiskalnego");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber);
    }

    /// <summary>
    /// The connection drops after trend, during the footer lines (measured on a POSNET over USB by
    /// pulling the cable): the receipt is fiscal, so the SCU waits until the register is back and idle,
    /// then reads the receipt's numbers — a fiscal receipt is never reported without them because of a
    /// cut. The footer failure is reported, with the instruction not to send the receipt again.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_ConnectionLostAfterTrend_WaitsForTheRegister_AndStillReportsTheReceiptNumber()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.Disconnect(atMnemonic: "trftrln", commands: 12);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted)
            .Which.Data.Should().Be("the printer stopped answering or was disconnected — the additional lines were not printed. The fiscal receipt was issued: do not send it again.");
        // Nothing that prints was sent again: one trend, no prncancel.
        transport.SentMnemonics.Count(m => m == "trend").Should().Be(1);
        transport.SentMnemonics.Should().NotContain("prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_FailedCounterReadBeforeTheSale_OpensNoTransaction()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("scnt");
        var sut = CreateSut(transport);


        (await sut.ProcessReceiptAsync(CreateSaleRequest())).FailureMessage().Should().Contain("could not be reached");
        transport.SentMnemonics.Should().Equal("scomm", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_FailedDocumentNumberRead_KeepsTheRestOfTheAnchor()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("eclastdocnoget");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftSignatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.DeviceMoment);
    }

    [Fact]
    public async Task ProcessReceiptAsync_DocumentNotFoundInTheProtectedMemory_SignsNoDocumentNumber()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.Answer("eclastdocnoget", "eclastdocnoget	di0	");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftSignatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber);
    }

    [Fact]
    public async Task ProcessReceiptAsync_FirmwareWithoutIsoClock_ReadsTheWallClockAsPolishTime()
    {
        var transport = FakePosNetTransport.Confirming();
        // POT-I-DEV-05 firmware answers rtcget with da only; winter time here, so +01:00.
        transport.Answer("rtcget", "rtcget	da2026-01-15;9:05	");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.DeviceMoment)
            .Which.Data.Should().Be("2026-01-15T09:05+01:00");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceErrorMidTransaction_WithAnUnansweredCancel_IsStillNotIssued()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trline", 382);
        transport.FailAmbiguously("prncancel");
        var sut = CreateSut(transport);


        // Whether the cancel reached the device does not matter for the receipt: trend was never
        // sent, and an open transaction cannot become a fiscal receipt on its own.
        (await sut.ProcessReceiptAsync(CreateSaleRequest())).FailureMessage().Should().Contain("NOT issued").And.Contain("error 382");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceErrorMidTransaction_CancelsAndSurfacesTheDeviceError()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trline", 382);
        var sut = CreateSut(transport);


        (await sut.ProcessReceiptAsync(CreateSaleRequest())).FailureMessage().Should().Contain("error 382");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousOutcomeBeforeTrend_IsNotIssued_AndCancelsTheOpenReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("trpayment");
        var sut = CreateSut(transport);


        // Before trend nothing can have been fiscalized, so the answer is definite.
        (await sut.ProcessReceiptAsync(CreateSaleRequest())).FailureMessage().Should().Contain("NOT issued");
        // The ambiguous command is never resent; the open receipt is canceled.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "prncancel");
    }

    /// <summary>
    /// A register with an open cover still accepts a whole receipt and prints it once the cover is
    /// closed — fiscalized long after the sale was reported as failed. The sale is refused before
    /// anything that could print is sent.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_CoverOpenBeforeTheSale_IsNotIssued_AndNothingThatCouldPrintIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.ReportFault(mechanism: 1);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.FailureMessage().Should().Contain("NOT issued").And.Contain("cover is open");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev");
    }

    [Fact]
    public async Task ProcessReceiptAsync_PaperRunsOutWhileTheReceiptIsBuilt_CancelsBeforeTrend_AndIsNotIssued()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.ReportFault(mechanism: 5, fromCheck: 2);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.FailureMessage().Should().Contain("NOT issued").And.Contain("no paper");
        // trend — the one command that fiscalizes — is never sent; the open receipt is canceled.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "prncancel");
    }

    /// <summary>
    /// The cover-open case seen on a real POSNET: trend went out, the register stayed silent and
    /// printed the receipt later. The SCU waits for the register and reports what it did — here, the
    /// receipt it issued — instead of an error for a document that exists.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_SilentTrendTheRegisterExecuted_IsReportedAsTheIssuedReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.SilenceTrend(executes: true);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        transport.SentMnemonics.Count(m => m == "trend").Should().Be(1, "trend is never resent");
    }

    [Fact]
    public async Task ProcessReceiptAsync_SilentTrendTheRegisterDidNotExecute_IsNotIssued_AndCancels()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.SilenceTrend(executes: false);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.FailureMessage().Should().Contain("NOT issued").And.Contain("no receipt was completed");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "!sprn", "!sdev", "scnt", "prncancel");
    }

    /// <summary>
    /// What the real POSNET does with an open cover: trend is accepted, the register goes silent and
    /// reports "no paper, waiting for the operator" with trend still queued — and prints the receipt
    /// the moment the cover is closed. The SCU waits, however long, and then reports the receipt.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_SilentTrendWhileTheCoverIsOpen_WaitsUntilTheRegisterIsIdle_ThenReportsTheReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.SilenceTrend(executes: true);
        // Checks 1 and 2 are the readiness checks; from the third on the register is stuck for four polls.
        transport.ReportFault(mechanism: 5, deviceStatus: 3, queueEmpty: false, fromCheck: 3, untilCheck: 6);
        var configuration = new PosNetConfiguration
        {
            DeviceUrl = "tcp://localhost:6666",
            VatRateTable = PosNetConfiguration.DefaultVatRateTable(),
            OutcomeResolutionPollIntervalMs = 1,
        };
        var sut = new PosNetPLSSCD(configuration, new PosNetClient(transport));

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        // The counters are only read once the register is idle again — a held receipt has not updated them yet.
        var afterTrend = transport.SentMnemonics.Skip(transport.SentMnemonics.IndexOf("trend") + 1).ToList();
        afterTrend.IndexOf("scnt").Should().Be(afterTrend.Count(m => m == "!sprn") * 2, "every poll before it was a stuck !sprn/!sdev pair");
        afterTrend.Count(m => m == "!sprn").Should().Be(5);
    }

    [Fact]
    public async Task ProcessReceiptAsync_PrinterWaitingForTheOperatorBeforeTheSale_IsNotIssued()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.ReportFault(mechanism: 0, deviceStatus: 3);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.FailureMessage().Should().Contain("NOT issued").And.Contain("waiting for the operator");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev");
    }

    /// <summary>
    /// A late answer to an earlier command arriving in place of the counter read: the outcome of that
    /// read is unknown, but nothing that can print has been sent, so the sale is definitely not issued.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_UnansweredCounterReadBeforeTheSale_IsNotIssued()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("scnt");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.FailureMessage().Should().Contain("NOT issued");
        transport.SentMnemonics.Should().Equal("scomm", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_ZeroReceipt_ReturnsThePrinterStateInFtStateData()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2000;

        var result = await sut.ProcessReceiptAsync(request);

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        var printer = ((JsonElement)result.ReceiptResponse.ftStateData!).GetProperty("PL").GetProperty("PrinterStatus");
        printer.GetProperty("Ready").GetBoolean().Should().BeTrue();
        printer.GetProperty("Mechanism").GetInt64().Should().Be(0);
        printer.GetProperty("FiscalMode").GetBoolean().Should().BeTrue();
        printer.GetProperty("UniqueDeviceNumber").GetString().Should().Be("ZBF 2101002392");
        printer.GetProperty("DeviceType").GetString().Should().Be("POSNET THERMAL HD ONLINE");
        printer.GetProperty("SoftwareVersion").GetString().Should().Be("21.01");
        transport.SentMnemonics.Should().Equal("!sprn", "!sdev", "!sprn", "!sdev", "scomm", "getrealid");
    }

    /// <summary>A printer holding a receipt is not idle: its state is the answer, and the synchronous read that would queue behind the stuck command is not sent.</summary>
    [Fact]
    public async Task ProcessReceiptAsync_ZeroReceiptWhileThePrinterIsStuck_ReportsTheProblem_WithoutQueueingBehindIt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.ReportFault(mechanism: 5, deviceStatus: 3, queueEmpty: false);
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2000;

        var result = await sut.ProcessReceiptAsync(request);

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        var printer = ((JsonElement)result.ReceiptResponse.ftStateData!).GetProperty("PL").GetProperty("PrinterStatus");
        printer.GetProperty("Ready").GetBoolean().Should().BeFalse();
        printer.GetProperty("Problem").GetString().Should().Contain("no paper");
        printer.GetProperty("DeviceStatus").GetInt64().Should().Be(3);
        printer.GetProperty("QueueEmpty").GetBoolean().Should().BeFalse();
        transport.SentMnemonics.Should().Equal("!sprn", "!sdev");
    }

    /// <summary>
    /// The zero receipt runs outside the device lock, so it answers while a sale waits for the printer.
    /// Its synchronous reads (scomm, getrealid) would wait behind that sale's commands, so they are only
    /// sent while no sale holds the lock: here the sale waits for the register's counters after a silent
    /// trend, and the zero receipt returns the asynchronous state without them.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_ZeroReceiptWhileASaleHoldsThePrinter_SkipsTheSynchronousReads()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.SilenceTrend(executes: true);
        transport.FailUnreachable("scnt", occurrence: 2);
        var sut = CreateSut(transport);

        var sale = sut.ProcessReceiptAsync(CreateSaleRequest());
        var waited = TimeSpan.Zero;
        while (transport.CountSent("scnt") < 3)
        {
            waited.Should().BeLessThan(TimeSpan.FromSeconds(10), "the sale should be waiting for the register's counters");
            await Task.Delay(10);
            waited += TimeSpan.FromMilliseconds(10);
        }
        var zeroReceipt = CreateSaleRequest();
        zeroReceipt.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2000;
        zeroReceipt.ReceiptRequest.cbChargeItems = [];
        zeroReceipt.ReceiptRequest.cbPayItems = [];

        var result = await sut.ProcessReceiptAsync(zeroReceipt);

        var printer = ((JsonElement)result.ReceiptResponse.ftStateData!).GetProperty("PL").GetProperty("PrinterStatus");
        printer.GetProperty("Ready").GetBoolean().Should().BeTrue();
        printer.GetProperty("FiscalMode").ValueKind.Should().Be(JsonValueKind.Null);
        printer.GetProperty("DeviceType").ValueKind.Should().Be(JsonValueKind.Null);
        transport.CountSent("getrealid").Should().Be(0);
        transport.CountSent("scomm").Should().Be(1, "only the sale reads scomm, before its receipt");

        transport.Recover("scnt");
        (await sale).ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
    }

    private static ProcessRequest CreateDailyClosingRequest()
    {
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2011;
        request.ReceiptRequest.cbReceiptMoment = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];
        return request;
    }

    private static PosNetPLSSCD CreateQuicklyPollingSut(FakePosNetTransport transport) => new(new PosNetConfiguration
    {
        DeviceUrl = "tcp://localhost:6666",
        VatRateTable = PosNetConfiguration.DefaultVatRateTable(),
        OutcomeResolutionPollIntervalMs = 1,
    }, new PosNetClient(transport));

    /// <summary>
    /// The daily report is held like a receipt: a POSNET that cannot print it prints it once the fault
    /// is cleared. The SCU waits for the register and reports the report it printed.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_SilentDailyReportWhileTheCoverIsOpen_WaitsAndReportsThePrintedReport()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.SilenceDailyReport(executes: true);
        transport.ReportFault(mechanism: 5, deviceStatus: 3, queueEmpty: false, fromCheck: 2, untilCheck: 5);
        var sut = CreateQuicklyPollingSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateDailyClosingRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer raportu dobowego").Which.Data.Should().Be("13");
        transport.SentMnemonics.Count(m => m == "dailyrep").Should().Be(1, "dailyrep is never resent");
    }

    [Fact]
    public async Task ProcessReceiptAsync_SilentDailyReportTheRegisterDidNotPrint_IsNotIssued()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.SilenceDailyReport(executes: false);
        var sut = CreateQuicklyPollingSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateDailyClosingRequest());

        result.FailureMessage().Should().Contain("daily report was NOT issued").And.Contain("no report was printed");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "dailyrep", "!sprn", "!sdev", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DailyClosingWhilePrinterWaitsForTheOperator_IsNotIssued_AndDailyrepIsNeverSent()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.ReportFault(mechanism: 5, deviceStatus: 3);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateDailyClosingRequest());

        result.FailureMessage().Should().Contain("daily report was NOT issued").And.Contain("no paper");
        transport.SentMnemonics.Should().NotContain("dailyrep");
    }

    [Fact]
    public async Task ProcessReceiptAsync_SaleWithCashierAndRegister_ConfiguresTheFooterBeforeTheReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.cbUser = "238689";
        request.ReceiptRequest.cbTerminalID = "02";

        var result = await sut.ProcessReceiptAsync(request);

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "ftrcfg", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
        transport.SentPayloads.Single(p => p.StartsWith("ftrcfg")).Should().StartWith("ftrcfg	cc238689	cn02	ca0	bb0	#");
    }

    /// <summary>The register would refuse it with error 10; the SCU refuses it first, so nothing at all reaches the device.</summary>
    [Fact]
    public async Task ProcessReceiptAsync_RegisterNumberTooLong_IsRejectedBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.cbTerminalID = "HM-PL0027-SSCO";

        var result = await sut.ProcessReceiptAsync(request);

        result.FailureMessage().Should().Contain("cbTerminalID 'HM-PL0027-SSCO' has 14 characters");
        transport.SentMnemonics.Should().BeEmpty();
    }

    private static ProcessRequest CreateSaleWithFooterLinesRequest()
    {
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCaseData = new { PL = new { printout = new { lines = new object[] { new { text = "Dziękujemy!" }, new { text = "Zapraszamy" } } } } };
        return request;
    }

    private static ProcessRequest CreateSaleWithReceiptLinesAndCardSlipRequest()
    {
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCaseData = new { cbReceiptLines = new[] { "Thank you, H&M" } };
        request.ReceiptRequest.cbPayItems[0].ftPayItemCaseData = new { cbPayItemLines = new[] { "CARDHOLDER COPY", "", "APPROVED" } };
        return request;
    }

    /// <summary>
    /// The general additional lines print inside the fiscal receipt as left-aligned additional lines
    /// (trftrln id41) — the receipt lines, then the card slip — so receipt and slip are one document
    /// with one header number and no cut between them.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_ReceiptLinesAndCardSlip_PrintInsideTheReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithReceiptLinesAndCardSlipRequest());

        transport.SentMnemonics.SkipWhile(m => m != "trend").Should().Equal(
            "trend", "trftrln", "trftrln", "trftrln", "trftrln", "trftrend", "eclastdocnoget", "rtcget", "scnt");
        transport.SentPayloads.Where(p => p.StartsWith("trftrln")).Select(p => p[..p.LastIndexOf('#')]).Should().Equal(
            "trftrln\tid41\tnaThank you, H&&M\t", "trftrln\tid41\tnaCARDHOLDER COPY\t", "trftrln\tid41\tna \t", "trftrln\tid41\tnaAPPROVED\t");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted);
    }

    [Fact]
    public async Task ProcessReceiptAsync_RejectedCardSlipLine_KeepsTheFiscalReceipt_AndSaysNotToResendIt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trftrln", 2000);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithReceiptLinesAndCardSlipRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Dodatkowe linie nie wydrukowane")
            .Which.Data.Should().StartWith("?2000").And.EndWith("The fiscal receipt was issued: do not send it again.");
        transport.SentMnemonics.Should().NotContain("prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_SaleWithFooterLines_PrintsThemAfterTrend()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "trftrln", "trftrln", "trftrend", "eclastdocnoget", "rtcget", "scnt");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted);
    }

    /// <summary>
    /// After trend the receipt is fiscal. Whatever happens to an additional line — rejected,
    /// unanswered, connection lost — failing the receipt would make the POS resend it and print a
    /// second fiscal receipt, so the sale succeeds with the failure in the response.
    /// </summary>
    [Theory]
    [InlineData("device error")]
    [InlineData("ambiguous")]
    [InlineData("unreachable")]
    [InlineData("unreadable")]
    [InlineData("unexpected")]
    public async Task ProcessReceiptAsync_FailedFooterLineAfterTrend_DoesNotFailTheFiscalReceipt(string failure)
    {
        var transport = FakePosNetTransport.Confirming();
        switch (failure)
        {
            case "device error": transport.FailWithDeviceError("trftrln", 2000); break;
            case "ambiguous": transport.FailAmbiguously("trftrln"); break;
            case "unreachable": transport.FailUnreachable("trftrln"); break;
            case "unreadable": transport.AnswerUnreadably("trftrln"); break;
            case "unexpected": transport.FailUnexpectedly("trftrln"); break;
        }
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        // No cancel (the receipt is closed) and no retry of the line; the footer is closed and the
        // receipt is anchored as usual.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "trftrln", "trftrend", "eclastdocnoget", "rtcget", "scnt");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
    }

    [Theory]
    [InlineData("device error")]
    [InlineData("ambiguous")]
    [InlineData("unreachable")]
    [InlineData("unexpected")]
    public async Task ProcessReceiptAsync_FailedFooterCloseAfterTrend_DoesNotFailTheFiscalReceipt(string failure)
    {
        var transport = FakePosNetTransport.Confirming();
        switch (failure)
        {
            case "device error": transport.FailWithDeviceError("trftrend", 2000); break;
            case "ambiguous": transport.FailAmbiguously("trftrend"); break;
            case "unreachable": transport.FailUnreachable("trftrend"); break;
            case "unexpected": transport.FailUnexpectedly("trftrend"); break;
        }
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
    }

    /// <summary>
    /// The register answered trend, but the answer cannot be read. The SCU does not guess: it asks the
    /// register's counters, which show the receipt was completed, and reports the sale as issued.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_UnreadableAnswerToTrend_IsResolvedFromTheCounters_AndCompletesTheSale()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.AnswerUnreadably("trend");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "!sprn", "!sdev", "scnt", "eclastdocnoget", "rtcget", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousDailyReport_IsSettledFromTheCounters()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("dailyrep");
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2011;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];


        // The register is asked once it is idle again: its counters show no report, so the answer is definite.
        (await sut.ProcessReceiptAsync(request)).FailureMessage().Should().Contain("daily report was NOT issued");
    }

    /// <summary>
    /// The SCU never throws: a queue — in-process or remote, where exception types do not survive —
    /// reads every outcome off the response.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_UnexpectedFailure_ComesBackAsAnErrorResponse()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnexpectedly("trinit");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.FailureMessage().Should().Contain("NOT issued").And.Contain("transport bug");
        // Only the failure is reported — no identity or other signature of a document that was not issued.
        result.ReceiptResponse.ftSignatures.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceUnreachableAtTrinit_IsNotIssued_WithoutCancel()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("trinit");
        var sut = CreateSut(transport);


        (await sut.ProcessReceiptAsync(CreateSaleRequest())).FailureMessage().Should().Contain("NOT issued").And.Contain("connection refused");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit");
    }

    [Fact]
    public async Task ProcessReceiptAsync_UnsettledPayments_FailValidationBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);


        (await sut.ProcessReceiptAsync(CreateSaleRequest(amount: 9.99m, payment: 5.00m))).FailureMessage();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_Invoice_IsRejectedWithoutDeviceInteraction()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_1002;


        (await sut.ProcessReceiptAsync(request)).FailureMessage();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_ZeroReceipt_ReadsTheDeviceStatus()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2000;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        await sut.ProcessReceiptAsync(request);

        transport.SentMnemonics.Should().Equal("!sprn", "!sdev", "!sprn", "!sdev", "scomm", "getrealid");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DailyClosing_PrintsTheDailyReport_AndReportsItsNumber()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2011;
        request.ReceiptRequest.cbReceiptMoment = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        var result = await sut.ProcessReceiptAsync(request);

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "dailyrep", "scnt");
        // The register validates the date against its own clock, so the day closed is the receipt
        // moment in Warsaw time — 12:00 CEST on the 8th here — not the host's local day.
        transport.SentPayloads.Single(p => p.StartsWith("dailyrep")).Should().StartWith("dailyrep	da2026-09-08	");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer raportu dobowego").Which.Data.Should().Be("13");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DailyClosing_ClosesTheRegistersDay_NotTheHostsUtcDay()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2011;
        // 00:30 Warsaw time on the 9th — a middleware host reading its own UTC clock would close
        // the 8th, a day the register has already left.
        request.ReceiptRequest.cbReceiptMoment = new DateTime(2026, 9, 8, 22, 30, 0, DateTimeKind.Utc);
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        await sut.ProcessReceiptAsync(request);

        transport.SentPayloads.Single(p => p.StartsWith("dailyrep")).Should().StartWith("dailyrep	da2026-09-09	");
    }

    [Fact]
    public async Task ProcessReceiptAsync_MonthlyClosing_PrintsTheMonthlyReport_ForTheRegistersMonth()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2012;
        // 00:30 Warsaw time on 1 October — the host's UTC clock would still be in September.
        request.ReceiptRequest.cbReceiptMoment = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc);
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        var result = await sut.ProcessReceiptAsync(request);

        transport.SentMnemonics.Should().Equal("scomm", "monthlyrep");
        transport.SentPayloads.Single(p => p.StartsWith("monthlyrep")).Should().StartWith("monthlyrep	da2026-10-01	");
        // A periodic report advances no counter, so it signs no report number.
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Numer raportu dobowego");
    }

    [Fact]
    public async Task ProcessReceiptAsync_YearlyClosing_PrintsAPeriodicReport_FromNewYearToTheDayClosed()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2013;
        request.ReceiptRequest.cbReceiptMoment = new DateTime(2026, 12, 31, 18, 0, 0, DateTimeKind.Utc);
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        await sut.ProcessReceiptAsync(request);

        // The register has no yearly report — and the settlement report (raport rozliczeniowy) would
        // end its fiscal life — so the year is a plain periodic report by dates.
        transport.SentMnemonics.Should().Equal("scomm", "periodicrepbydates");
        transport.SentPayloads.Single(p => p.StartsWith("periodicrepbydates")).Should().StartWith("periodicrepbydates	fd2026-01-01	td2026-12-31	");
    }

    private static ProcessRequest CreateEReceiptSaleRequest(string eReceiptCustomerId = "KID0123456789ABC")
    {
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCaseData = new { PL = new { eReceipt = new { customerId = eReceiptCustomerId } } };
        return request;
    }

    [Fact]
    public async Task ProcessReceiptAsync_EReceiptSale_BindsTheIdzStrictlyBeforeTrinit()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest());

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "eparagonidznext", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt", "eparagonbufferget");
        transport.SentPayloads.Single(p => p.StartsWith("eparagonidznext")).Should().Contain("idKID0123456789ABC");
        transport.SentPayloads.Single(p => p.StartsWith("eparagonbufferget")).Should().Contain("hd3054");
    }

    [Fact]
    public async Task ProcessReceiptAsync_EReceiptSale_EnrichesTheResponseWithTheEDocumentIdAndDeliveryState()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest());

        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Identyfikator eDokumentu" && s.Data == "3054");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Status eDokumentu" && s.Data == "electronic (st1)");
    }

    [Fact]
    public async Task ProcessReceiptAsync_EReceiptBindingRejectedWith2034_FailsBeforeAnythingIsSentForTheReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("eparagonidznext", 2034);
        var sut = CreateSut(transport);


        // ERR_NO_FISC_MODE gets the explaining message, and the rejected binding is the last frame
        // on the wire: no trinit was sent, so there is no transaction to cancel and no paper.
        var failure = (await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest())).FailureMessage();
        failure.Should().Contain("error 2034");
        failure.Should().Contain("not fiscalized").And.Contain("eDokument");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "eparagonidznext");
    }

    [Fact]
    public async Task ProcessReceiptAsync_EReceiptBindingRejected_SurfacesTheDeviceErrorWithoutCancel()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("eparagonidznext", 2004);
        var sut = CreateSut(transport);


        (await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest())).FailureMessage().Should().Contain("error 2004");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "eparagonidznext");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousEReceiptBinding_IsNeverRetriedAndSendsNothingFurther()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("eparagonidznext");
        var sut = CreateSut(transport);


        (await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest())).FailureMessage().Should().Contain("NOT issued");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "eparagonidznext");
    }

    [Fact]
    public async Task ProcessReceiptAsync_FailedEDocumentStatusReadback_DoesNotFailTheReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("eparagonbufferget", 2035);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest());

        // The document is closed on the register when the buffer is read — like the scnt readback,
        // a failure leaves the delivery state absent but keeps the eDokument id and the receipt.
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Identyfikator eDokumentu" && s.Data == "3054");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Status eDokumentu");
    }

    [Fact]
    public async Task ProcessReceiptAsync_RejectedFooterLineThenUnexpectedFailureClosingIt_DoesNotFailTheFiscalReceipt()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trftrln", 2000);
        transport.FailUnexpectedly("trftrend");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
    }

    /// <summary>
    /// Every readback runs after the document is printed: no failure of one — anticipated or not —
    /// may turn the issued document into an error response.
    /// </summary>
    [Theory]
    [InlineData("eclastdocnoget")]
    [InlineData("rtcget")]
    [InlineData("eparagonbufferget")]
    public async Task ProcessReceiptAsync_UnexpectedReadbackFailureAfterTrend_DoesNotFailTheReceipt(string mnemonic)
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnexpectedly(mnemonic);
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest());

        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Identyfikator eDokumentu");
    }

    [Fact]
    public async Task ProcessReceiptAsync_UnexpectedCounterReadAfterTheDailyReport_DoesNotFailTheClosing()
    {
        var transport = FakePosNetTransport.Confirming();
        // The second scnt — the first is the baseline read before the report.
        transport.FailUnexpectedly("scnt", occurrence: 2);
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2011;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        var result = await sut.ProcessReceiptAsync(request);

        // The report is printed; only its number is not known.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "dailyrep", "scnt");
        result.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Numer raportu dobowego");
    }

    [Fact]
    public async Task ProcessReceiptAsync_OverlongEReceiptCustomerId_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);


        (await sut.ProcessReceiptAsync(CreateEReceiptSaleRequest(new string('A', 129)))).FailureMessage();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_SaleWithoutEReceiptCustomerId_NeverTouchesTheEParagonCommands()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Identyfikator eDokumentu" || s.Caption == "Status eDokumentu");
    }

    [Fact]
    public async Task ProcessReceiptAsync_Return_PrintsTheGoodsReturn_WithoutAFiscalDocumentNumber()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(amount: -3.69m, payment: -3.69m);
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)(0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.Refund);

        var result = await sut.ProcessReceiptAsync(request);

        transport.SentMnemonics.Should().Equal("scomm", "stocash");
        transport.SentPayloads.Single(p => p.StartsWith("stocash")).Should().StartWith("stocash	kw369	");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Numer dokumentu fiskalnego");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Zwrot towaru (wydruk niefiskalny)").Which.Data.Should().Be("3.69");
        result.ReceiptResponse.ftCashBoxIdentification.Should().Be("ZBF 2101002392");
    }

    [Fact]
    public async Task ProcessReceiptAsync_ReturnWithASalePosition_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(amount: 3.69m, payment: -3.69m);
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)(0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.Refund);


        (await sut.ProcessReceiptAsync(request)).FailureMessage();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInfoAsync_WithoutAConfiguredRateTable_ReadsThePtuTableOffTheRegister()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = new PosNetPLSSCD(s_configurationWithoutRateTable, new PosNetClient(transport));

        var info = await sut.GetInfoAsync();

        transport.SentMnemonics.Should().Equal("scomm", "sfsk");
        var table = PLDeviceInfo.FromPLSSCDInfo(info)!.VatRateTable;
        // The fake register answers like the office printer: A 23, B 8, C 5, D 0, E–G not in use.
        table.Select(e => (e.PtuSlot, e.VatRatePercent, e.IsExempt)).Should().Equal(
            ("A", 23m, false), ("B", 8m, false), ("C", 5m, false), ("D", 0m, false));
    }

    [Fact]
    public async Task ProcessReceiptAsync_WithoutAConfiguredRateTable_ReadsTheTableOnce_BeforeTheFirstSale()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = new PosNetPLSSCD(s_configurationWithoutRateTable, new PosNetClient(transport));

        await sut.ProcessReceiptAsync(CreateSaleRequest());
        await sut.ProcessReceiptAsync(CreateSaleRequest());

        // Mapping needs the slots, so the table is read before anything else; identity and table are then reused.
        transport.SentMnemonics.Should().Equal(
            "sfsk", "scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt",
            "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_WithARateTheRegisterDoesNotHave_FailsBeforeTheTransactionIsOpened()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = new PosNetPLSSCD(s_configurationWithoutRateTable, new PosNetClient(transport));
        var request = CreateSaleRequest();
        // NotTaxable resolves to the exempt slot, which the register does not report.
        request.ReceiptRequest.cbChargeItems[0].ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0018;


        (await sut.ProcessReceiptAsync(request)).FailureMessage();
        transport.SentMnemonics.Should().Equal("sfsk");
    }

    [Fact]
    public async Task GetInfoAsync_MapsTheFiscalModeFromScomm()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var info = await sut.GetInfoAsync();

        var deviceInfo = PLDeviceInfo.FromPLSSCDInfo(info);
        deviceInfo.Should().NotBeNull();
        deviceInfo!.FiscalizationState.Should().Be(PLFiscalizationState.Fiscalized);
        deviceInfo.VatRateTable.Should().NotBeEmpty();
        deviceInfo.UniqueDeviceNumber.Should().Be("ZBF 2101002392");
    }

    /// <summary>
    /// A POSNET Online printer answers the status flags as T (tak) and N (nie) — the 1/0 form of
    /// the protocol description is accepted as well. Anything else must not be read as non-fiscal:
    /// a fiscalized register reported as NonFiscal can never activate a PL queue.
    /// </summary>
    [Theory]
    [InlineData("scomm	fsT	tzN	ts0	hrT	tdN	", PLFiscalizationState.Fiscalized)]
    [InlineData("scomm	fsN	tzN	ts0	hrT	tdN	", PLFiscalizationState.NonFiscal)]
    [InlineData("scomm	fs1	tz1	ts0	hr1	", PLFiscalizationState.Fiscalized)]
    [InlineData("scomm	fs0	tz1	ts0	hr1	", PLFiscalizationState.NonFiscal)]
    [InlineData("scomm	tzN	ts0	hrT	", PLFiscalizationState.Unknown)]
    [InlineData("scomm	fsX	tzN	ts0	hrT	", PLFiscalizationState.Unknown)]
    public async Task GetInfoAsync_ReadsTheStatusFlagsOfARealPrinter(string status, PLFiscalizationState expected)
    {
        var transport = FakePosNetTransport.Confirming();
        transport.AnswerScommWith(status);
        var sut = CreateSut(transport);

        var info = await sut.GetInfoAsync();

        PLDeviceInfo.FromPLSSCDInfo(info)!.FiscalizationState.Should().Be(expected);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ConsecutiveSales_ReadTheRegisterIdentityOnlyOnce()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        await sut.ProcessReceiptAsync(CreateSaleRequest());
        await sut.ProcessReceiptAsync(CreateSaleRequest());

        transport.SentMnemonics.Should().Equal(
            "scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt",
            "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_QuantityWithAWholeGroszUnitPrice_SendsPriceQuantityAndValueConsistently()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(amount: 9.99m, payment: 9.99m);
        request.ReceiptRequest.cbChargeItems[0].Quantity = 3m;

        await sut.ProcessReceiptAsync(request);

        // 999 gr over 3 units: price × quantity has to equal the line value the register totalizes.
        transport.SentPayloads.Single(p => p.StartsWith("trline")).Should().Contain("pr333").And.Contain("il3.000").And.Contain("wa999");
    }

    [Fact]
    public async Task ProcessReceiptAsync_QuantityWithoutAWholeGroszUnitPrice_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(amount: 10.00m, payment: 10.00m);
        request.ReceiptRequest.cbChargeItems[0].Quantity = 3m;


        // 1000 gr over 3 units would print 3 × 333 gr = 999 gr next to a value of 1000 gr.
        (await sut.ProcessReceiptAsync(request)).FailureMessage();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_DiscountPosition_RidesAlongOnTheSaleLine()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(payment: 7.99m);
        request.ReceiptRequest.cbChargeItems.Add(new ChargeItem
        {
            Description = "Rabat",
            Amount = -2m,
            Quantity = 1m,
            ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0011,
        });

        await sut.ProcessReceiptAsync(request);

        // The queue passes discounts through (they do not make a document a return), and a register
        // has no position for one: it travels as the rabat of the line it follows. The line keeps
        // its own value (wa) and the receipt is settled with the discounted total.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
        transport.SentPayloads.Single(p => p.StartsWith("trline")).Should().Contain("wa999").And.Contain("rd1").And.Contain("rw200");
        transport.SentPayloads.Single(p => p.StartsWith("trend")).Should().Contain("to799");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DiscountWithNoPositionInFrontOfIt_BecomesASubtotalDiscount()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest(payment: 7.99m);
        request.ReceiptRequest.cbChargeItems.Insert(0, new ChargeItem
        {
            Description = "Rabat",
            Amount = -2m,
            Quantity = 1m,
            ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0011,
        });

        await sut.ProcessReceiptAsync(request);

        // It cannot belong to a line, so it is a rabat od podsumy — sent after every line and
        // before the payments, which is where the register applies it.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "!sprn", "!sdev", "trinit", "trline", "trdiscntsubtot", "trpayment", "!sprn", "!sdev", "trend", "eclastdocnoget", "rtcget", "scnt");
        transport.SentPayloads.Single(p => p.StartsWith("trdiscntsubtot")).Should().Contain("rd1").And.Contain("rw200");
        transport.SentPayloads.Single(p => p.StartsWith("trend")).Should().Contain("to799");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DescriptionWithATab_DoesNotInjectAProtocolField()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.cbChargeItems[0].Description = "Candies	vt0	pr1";

        await sut.ProcessReceiptAsync(request);

        // One vt and one pr, and the name travels as text: a TAB inside a value would otherwise
        // open further protocol fields and silently change the PTU slot and the price.
        var trline = transport.SentPayloads.Single(p => p.StartsWith("trline"));
        trline.Should().StartWith("trline	naCandies vt0 pr1	vt1	pr999	#");
    }

    /// <summary>
    /// A scripted transport: confirms every command (scomm answers with a fiscal-mode status) and
    /// can be armed to fail a specific mnemonic in one of the three failure modes.
    /// </summary>
    private sealed class FakePosNetTransport : IPosNetTransport
    {
        private readonly Dictionary<string, Func<Exception>> _failures = [];
        private readonly Dictionary<string, int> _failFromOccurrence = [];
        private readonly Dictionary<string, string> _answers = [];
        private readonly HashSet<string> _unreadable = [];

        public List<string> SentMnemonics { get; } = [];

        public List<string> SentPayloads { get; } = [];

        private string _scommResponse = "scomm	fsT	tzN	ts0	hrT	nuZBF 2101002392	tdN	";

        /// <summary>Completed receipts of the day (bn = bt); every confirmed trend completes one.</summary>
        private int _completedReceipts = 84;

        private bool _anotherReceiptAfterTrend;

        /// <summary>Canceled receipts of the day (bc); bt numbers completed and canceled receipts alike.</summary>
        private int _canceledReceipts;

        private bool _anotherReceiptCanceledAfterTrend;

        /// <summary>The fault !sprn/!sdev report: mechanism (1 cover open, 5 no paper) and device state (3 waiting for the operator).</summary>
        private int _mechanismStatus;
        private int _deviceStatus;
        private bool _queueEmpty = true;

        /// <summary>The state checks (1-based, one per !sprn) during which the fault is reported; before and after it the printer is idle.</summary>
        private int _faultFrom = int.MaxValue;
        private int _faultUntil = int.MaxValue;

        /// <summary>
        /// How trend behaves when it does not answer: null answers normally; true executes it (the
        /// receipt is completed) and stays silent; false stays silent without executing it.
        /// </summary>
        private bool? _silentTrendExecutes;

        /// <summary>Like <see cref="_silentTrendExecutes"/>, for dailyrep: true prints the report and stays silent.</summary>
        private bool? _silentDailyReportExecutes;

        /// <summary>Daily reports in fiscal memory (rd) and printouts (hn); a daily report advances both.</summary>
        private int _dailyReports = 12;
        private int _printouts = 140;

        public static FakePosNetTransport Confirming() => new();

        /// <summary>
        /// The printer reports a fault from the <paramref name="fromCheck"/>-th state check on (one check
        /// is one !sprn), up to and including <paramref name="untilCheck"/>; outside that it is idle.
        /// </summary>
        public void ReportFault(int mechanism, int deviceStatus = 0, bool queueEmpty = true, int fromCheck = 1, int untilCheck = int.MaxValue)
        {
            _mechanismStatus = mechanism;
            _deviceStatus = deviceStatus;
            _queueEmpty = queueEmpty;
            _faultFrom = fromCheck;
            _faultUntil = untilCheck;
        }

        private bool FaultNow()
        {
            var check = SentMnemonics.Count(m => m == "!sprn");
            return check >= _faultFrom && check <= _faultUntil;
        }

        /// <summary>trend is not answered; <paramref name="executes"/> says whether the register still completed the receipt.</summary>
        public void SilenceTrend(bool executes) => _silentTrendExecutes = executes;

        /// <summary>dailyrep is not answered; <paramref name="executes"/> says whether the register still printed the report.</summary>
        public void SilenceDailyReport(bool executes) => _silentDailyReportExecutes = executes;

        /// <summary>Answers the status read with a specific payload — device firmwares differ.</summary>
        public void AnswerScommWith(string payload) => _scommResponse = payload;

        /// <summary>Answers a command with a fixed payload instead of the scripted one.</summary>
        public void Answer(string mnemonic, string payload) => _answers[mnemonic] = payload;

        /// <summary>Another interface completes a receipt right after this one, before the readback.</summary>
        public void CompleteAnotherReceiptAfterTrend() => _anotherReceiptAfterTrend = true;

        /// <summary>Another interface opens and cancels a receipt right after this one, before the readback.</summary>
        public void CancelAnotherReceiptAfterTrend() => _anotherReceiptCanceledAfterTrend = true;

        public void FailWithDeviceError(string mnemonic, int errorCode)
            => _failures[mnemonic] = () => new ExpectedDeviceError(errorCode);

        public void FailAmbiguously(string mnemonic)
            => _failures[mnemonic] = () => new PosNetAmbiguousResponseException("no response within the receive timeout");

        public void FailUnreachable(string mnemonic)
            => _failures[mnemonic] = () => new PLDeviceUnreachableException("connection refused");

        /// <summary>A failure the SCU does not anticipate — not one of its own exception types.</summary>
        public void FailUnexpectedly(string mnemonic)
            => _failures[mnemonic] = () => new InvalidOperationException("transport bug");

        /// <summary>Fails a command unexpectedly from its <paramref name="occurrence"/>-th sending on (1-based).</summary>
        public void FailUnexpectedly(string mnemonic, int occurrence)
        {
            FailUnexpectedly(mnemonic);
            _failFromOccurrence[mnemonic] = occurrence;
        }

        /// <summary>
        /// The connection drops at the first <paramref name="atMnemonic"/>: it and the next commands, up to
        /// <paramref name="commands"/> in all, fail as a dropped connection does; then the register is back.
        /// </summary>
        public void Disconnect(string atMnemonic, int commands)
        {
            _disconnectAt = atMnemonic;
            _disconnectedCommands = commands;
        }

        private string? _disconnectAt;
        private int _disconnectedCommands;

        /// <summary>The register executes the command but its answer arrives corrupted (CRC mismatch).</summary>
        public void AnswerUnreadably(string mnemonic) => _unreadable.Add(mnemonic);

        /// <summary>Fails a command from its <paramref name="occurrence"/>-th sending on (1-based) — scnt is sent before and after a sale.</summary>
        public void FailUnreachable(string mnemonic, int occurrence)
        {
            FailUnreachable(mnemonic);
            _failFromOccurrence[mnemonic] = occurrence;
        }

        private readonly object _sync = new();

        /// <summary>How many times a command was sent — safe to call while another task still talks to the printer.</summary>
        public int CountSent(string mnemonic)
        {
            lock (_sync)
            {
                return SentMnemonics.Count(m => m == mnemonic);
            }
        }

        /// <summary>A command armed to fail answers normally again from now on.</summary>
        public void Recover(string mnemonic)
        {
            lock (_sync)
            {
                _failures.Remove(mnemonic);
            }
        }

        /// <summary>One command at a time, as on the wire — tests may run a zero receipt next to a waiting sale.</summary>
        public Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                return SendReceive(frame);
            }
        }

        private Task<byte[]> SendReceive(byte[] frame)
        {
            var payload = Encoding.ASCII.GetString(frame, 1, frame.Length - 2);
            var mnemonic = payload.Split('	')[0];
            SentMnemonics.Add(mnemonic);
            SentPayloads.Add(payload);

            if (_disconnectAt == mnemonic)
            {
                _disconnectAt = null;
            }
            if (_disconnectAt is null && _disconnectedCommands > 0)
            {
                _disconnectedCommands--;
                throw new PosNetAmbiguousResponseException("Reading from the printer failed.");
            }

            var armed = !_failFromOccurrence.TryGetValue(mnemonic, out var from) || SentMnemonics.Count(m => m == mnemonic) >= from;
            if (armed && _failures.TryGetValue(mnemonic, out var failure))
            {
                var exception = failure();
                if (exception is ExpectedDeviceError deviceError)
                {
                    return Task.FromResult(PosNetProtocolTests.EncodeResponse($"{mnemonic}	?{deviceError.ErrorCode}	"));
                }
                throw exception;
            }

            if (mnemonic == "dailyrep" && _silentDailyReportExecutes is { } reportExecutes)
            {
                if (reportExecutes)
                {
                    _dailyReports++;
                    _printouts++;
                }
                throw new PosNetAmbiguousResponseException("no response within the receive timeout");
            }

            if (mnemonic == "dailyrep")
            {
                _dailyReports++;
                _printouts++;
            }

            if (mnemonic == "trend" && _silentTrendExecutes is { } executes)
            {
                if (executes)
                {
                    _completedReceipts++;
                }
                throw new PosNetAmbiguousResponseException("no response within the receive timeout");
            }

            if (mnemonic == "trend")
            {
                _completedReceipts += _anotherReceiptAfterTrend ? 2 : 1;
                _canceledReceipts += _anotherReceiptCanceledAfterTrend ? 1 : 0;
            }

            // Asynchronous commands are answered under their plain mnemonic.
            if (mnemonic == "!sprn")
            {
                return Task.FromResult(PosNetProtocolTests.EncodeResponse($"sprn	pr{(FaultNow() ? _mechanismStatus : 0)}	"));
            }
            if (mnemonic == "!sdev")
            {
                var faulty = SentMnemonics.Count(m => m == "!sprn") >= _faultFrom && SentMnemonics.Count(m => m == "!sprn") <= _faultUntil;
                return Task.FromResult(PosNetProtocolTests.EncodeResponse(
                    $"sdev	ds{(faulty ? _deviceStatus : 0)}	cpT	qe{(faulty && !_queueEmpty ? "N" : "T")}	peN	tl0	"));
            }

            var responsePayload = _answers.TryGetValue(mnemonic, out var answer) ? answer : mnemonic switch
            {
                // The T/N flags and the numer unikatowy in the shape a POSNET Online printer answers them.
                "scomm" => _scommResponse,
                "scnt" => $"scnt	rd{_dailyReports}	hn{_printouts}	bn{_completedReceipts}	bc{_canceledReceipts}	bt{_completedReceipts + _canceledReceipts}	fn3	",
                // One protected-memory sequence across all document types, ahead of the receipt number.
                "eclastdocnoget" => $"eclastdocnoget	di{_completedReceipts + 11}	",
                "rtcget" => "rtcget	da2026-09-08;17:54	tm2026-09-08T17:54:51+02:00	",
                // The factory identity, as getrealid answers it on a POSNET Thermal HD Online.
                "getrealid" => "getrealid	nmPOSNET THERMAL HD ONLINE	vr21.01	nuZBF 2101002392	",
                // The rate table as the office printer reports it after fiscalization: E–G not in use.
                "sfsk" => "sfsk	fsT	cl0	rd12	vt1	va23,00	vb8,00	vc5,00	vd0,00	ve101,00	vf101,00	vg101,00	rw2026-09-03;16:54	nuZBF 2101002392	",
                // The e-paragon binding confirms with the unique eDokument id; the buffer record
                // reports an electronic document (prN = no paper) that reached the hub (st1).
                "eparagonidznext" => "eparagonidznext	ha3054	",
                "eparagonbufferget" => "eparagonbufferget	hd3054	prN	st1	",
                _ => $"{mnemonic}	",
            };
            var responseFrame = PosNetProtocolTests.EncodeResponse(responsePayload);
            if (_unreadable.Contains(mnemonic))
            {
                responseFrame[2] ^= 0xFF;
            }
            return Task.FromResult(responseFrame);
        }

        public void Dispose() { }

        private sealed class ExpectedDeviceError(int errorCode) : Exception
        {
            public int ErrorCode { get; } = errorCode;
        }
    }
}
