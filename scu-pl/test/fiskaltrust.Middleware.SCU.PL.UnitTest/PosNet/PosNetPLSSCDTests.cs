using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
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

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
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

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trnipset", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
        transport.SentPayloads.Single(p => p.StartsWith("trnipset")).Should().Contain("ni1234563218");
    }

    [Fact]
    public async Task ProcessReceiptAsync_NipReceiptWithoutCustomerVatId_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0020_0001;

        var act = () => sut.ProcessReceiptAsync(request);

        await act.Should().ThrowAsync<PLValidationException>();
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
        transport.Answer("rtcget", "rtcget\tda2026-10-09;11:49\ttm2026-10-09T11:49:13\t");
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

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => s.Caption == "Numer dokumentu fiskalnego");
        result.ReceiptResponse.ftSignatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber);
    }

    [Fact]
    public async Task ProcessReceiptAsync_FailedCounterReadBeforeTheSale_OpensNoTransaction()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("scnt");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        await act.Should().ThrowAsync<PLDeviceUnreachableException>();
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
        transport.Answer("eclastdocnoget", "eclastdocnoget\tdi0\t");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftSignatures.Should().NotContain(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.ProtectedMemoryDocumentNumber);
    }

    [Fact]
    public async Task ProcessReceiptAsync_FirmwareWithoutIsoClock_ReadsTheWallClockAsPolishTime()
    {
        var transport = FakePosNetTransport.Confirming();
        // POT-I-DEV-05 firmware answers rtcget with da only; winter time here, so +01:00.
        transport.Answer("rtcget", "rtcget\tda2026-01-15;9:05\t");
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.DeviceMoment)
            .Which.Data.Should().Be("2026-01-15T09:05+01:00");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousCancelAfterDeviceError_PropagatesTheAmbiguity()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trline", 382);
        transport.FailAmbiguously("prncancel");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        // Whether the cancel reached the device is unknown, so the receipt must surface the
        // ambiguous/unreachable state rather than the (already handled) device error.
        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceErrorMidTransaction_CancelsAndSurfacesTheDeviceError()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("trline", 382);
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(382);
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "prncancel");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousOutcome_IsNeverRetriedAndSendsNothingFurther()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("trpayment");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
        // The ambiguous command is sent exactly once and nothing (not even a cancel) follows —
        // a blind retry or cleanup could duplicate or destroy a successfully printed document.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment");
    }

    private static ProcessRequest CreateSaleWithFooterLinesRequest()
    {
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCaseData = new { PL = new { printout = new { lines = new object[] { new { text = "Dziękujemy!" }, new { text = "Zapraszamy" } } } } };
        return request;
    }

    [Fact]
    public async Task ProcessReceiptAsync_SaleWithFooterLines_PrintsThemAfterTrend()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment", "trend", "trftrln", "trftrln", "trftrend", "eclastdocnoget", "rtcget", "scnt");
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
    public async Task ProcessReceiptAsync_FailedFooterLineAfterTrend_DoesNotFailTheFiscalReceipt(string failure)
    {
        var transport = FakePosNetTransport.Confirming();
        switch (failure)
        {
            case "device error": transport.FailWithDeviceError("trftrln", 2000); break;
            case "ambiguous": transport.FailAmbiguously("trftrln"); break;
            case "unreachable": transport.FailUnreachable("trftrln"); break;
            case "unreadable": transport.AnswerUnreadably("trftrln"); break;
        }
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        // No cancel (the receipt is closed) and no retry of the line; the footer is closed and the
        // receipt is anchored as usual.
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment", "trend", "trftrln", "trftrend", "eclastdocnoget", "rtcget", "scnt");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
    }

    [Theory]
    [InlineData("device error")]
    [InlineData("ambiguous")]
    [InlineData("unreachable")]
    public async Task ProcessReceiptAsync_FailedFooterCloseAfterTrend_DoesNotFailTheFiscalReceipt(string failure)
    {
        var transport = FakePosNetTransport.Confirming();
        switch (failure)
        {
            case "device error": transport.FailWithDeviceError("trftrend", 2000); break;
            case "ambiguous": transport.FailAmbiguously("trftrend"); break;
            case "unreachable": transport.FailUnreachable("trftrend"); break;
        }
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleWithFooterLinesRequest());

        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.AdditionalPrintoutNotPrinted);
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer dokumentu fiskalnego" && s.Data == "85");
    }

    /// <summary>
    /// The register answered trend, so it most likely printed the receipt — an answer that cannot
    /// be read is an unknown outcome, not a failure that could be resent safely.
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_UnreadableAnswerToTrend_IsAnUnknownOutcome_AndSendsNothingFurther()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.AnswerUnreadably("trend");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        await act.Should().ThrowAsync<PLDeviceOutcomeUnknownException>();
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment", "trend");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousDailyReport_IsAnUnknownOutcome()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("dailyrep");
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2011;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        var act = () => sut.ProcessReceiptAsync(request);

        // The report may have been printed: the caller must see "check the register", not "retry".
        var failure = (await act.Should().ThrowAsync<PLDeviceOutcomeUnknownException>()).Which;
        PLStateMapper.Map(failure).Should().Be((State)StatePL.DeviceOutcomeUnknownError);
    }

    [Fact]
    public async Task ProcessReceiptAsync_DeviceUnreachable_PropagatesWithoutCancel()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailUnreachable("trinit");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest());

        await act.Should().ThrowAsync<PLDeviceUnreachableException>();
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit");
    }

    [Fact]
    public async Task ProcessReceiptAsync_UnsettledPayments_FailValidationBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateSaleRequest(amount: 9.99m, payment: 5.00m));

        await act.Should().ThrowAsync<PLValidationException>();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_Invoice_IsRejectedWithoutDeviceInteraction()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0000_1002;

        var act = () => sut.ProcessReceiptAsync(request);

        await act.Should().ThrowAsync<PLValidationException>();
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

        transport.SentMnemonics.Should().Equal("scomm");
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

        transport.SentMnemonics.Should().Equal("scomm", "dailyrep", "scnt");
        // The register validates the date against its own clock, so the day closed is the receipt
        // moment in Warsaw time — 12:00 CEST on the 8th here — not the host's local day.
        transport.SentPayloads.Single(p => p.StartsWith("dailyrep")).Should().StartWith("dailyrep\tda2026-09-08\t");
        result.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "Numer raportu dobowego").Which.Data.Should().Be("12");
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

        transport.SentPayloads.Single(p => p.StartsWith("dailyrep")).Should().StartWith("dailyrep\tda2026-09-09\t");
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
        transport.SentPayloads.Single(p => p.StartsWith("monthlyrep")).Should().StartWith("monthlyrep\tda2026-10-01\t");
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
        transport.SentPayloads.Single(p => p.StartsWith("periodicrepbydates")).Should().StartWith("periodicrepbydates\tfd2026-01-01\ttd2026-12-31\t");
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

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "eparagonidznext", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt", "eparagonbufferget");
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

        var act = () => sut.ProcessReceiptAsync(CreateEReceiptSaleRequest());

        // ERR_NO_FISC_MODE gets the explaining message, and the rejected binding is the last frame
        // on the wire: no trinit was sent, so there is no transaction to cancel and no paper.
        var thrown = await act.Should().ThrowAsync<PLDeviceErrorException>();
        thrown.Which.ErrorCode.Should().Be(2034);
        thrown.Which.Message.Should().Contain("not fiscalized").And.Contain("eDokument");
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "eparagonidznext");
    }

    [Fact]
    public async Task ProcessReceiptAsync_EReceiptBindingRejected_SurfacesTheDeviceErrorWithoutCancel()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailWithDeviceError("eparagonidznext", 2004);
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateEReceiptSaleRequest());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(2004);
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "eparagonidznext");
    }

    [Fact]
    public async Task ProcessReceiptAsync_AmbiguousEReceiptBinding_IsNeverRetriedAndSendsNothingFurther()
    {
        var transport = FakePosNetTransport.Confirming();
        transport.FailAmbiguously("eparagonidznext");
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateEReceiptSaleRequest());

        await act.Should().ThrowAsync<PosNetAmbiguousResponseException>();
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "eparagonidznext");
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
    public async Task ProcessReceiptAsync_OverlongEReceiptCustomerId_FailsBeforeAnyFrameIsSent()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var act = () => sut.ProcessReceiptAsync(CreateEReceiptSaleRequest(new string('A', 129)));

        await act.Should().ThrowAsync<PLValidationException>();
        transport.SentMnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_SaleWithoutEReceiptCustomerId_NeverTouchesTheEParagonCommands()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);

        var result = await sut.ProcessReceiptAsync(CreateSaleRequest());

        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
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
        transport.SentPayloads.Single(p => p.StartsWith("stocash")).Should().StartWith("stocash\tkw369\t");
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

        var act = () => sut.ProcessReceiptAsync(request);

        await act.Should().ThrowAsync<PLValidationException>();
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
            "sfsk", "scomm", "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt",
            "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
    }

    [Fact]
    public async Task ProcessReceiptAsync_WithARateTheRegisterDoesNotHave_FailsBeforeTheTransactionIsOpened()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = new PosNetPLSSCD(s_configurationWithoutRateTable, new PosNetClient(transport));
        var request = CreateSaleRequest();
        // NotTaxable resolves to the exempt slot, which the register does not report.
        request.ReceiptRequest.cbChargeItems[0].ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0018;

        var act = () => sut.ProcessReceiptAsync(request);

        await act.Should().ThrowAsync<PLValidationException>();
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
    [InlineData("scomm\tfsT\ttzN\tts0\thrT\ttdN\t", PLFiscalizationState.Fiscalized)]
    [InlineData("scomm\tfsN\ttzN\tts0\thrT\ttdN\t", PLFiscalizationState.NonFiscal)]
    [InlineData("scomm\tfs1\ttz1\tts0\thr1\t", PLFiscalizationState.Fiscalized)]
    [InlineData("scomm\tfs0\ttz1\tts0\thr1\t", PLFiscalizationState.NonFiscal)]
    [InlineData("scomm\ttzN\tts0\thrT\t", PLFiscalizationState.Unknown)]
    [InlineData("scomm\tfsX\ttzN\tts0\thrT\t", PLFiscalizationState.Unknown)]
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
            "scomm", "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt",
            "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
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

        var act = () => sut.ProcessReceiptAsync(request);

        // 1000 gr over 3 units would print 3 × 333 gr = 999 gr next to a value of 1000 gr.
        await act.Should().ThrowAsync<PLValidationException>();
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
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
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
        transport.SentMnemonics.Should().Equal("scomm", "scnt", "trinit", "trline", "trdiscntsubtot", "trpayment", "trend", "eclastdocnoget", "rtcget", "scnt");
        transport.SentPayloads.Single(p => p.StartsWith("trdiscntsubtot")).Should().Contain("rd1").And.Contain("rw200");
        transport.SentPayloads.Single(p => p.StartsWith("trend")).Should().Contain("to799");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DescriptionWithATab_DoesNotInjectAProtocolField()
    {
        var transport = FakePosNetTransport.Confirming();
        var sut = CreateSut(transport);
        var request = CreateSaleRequest();
        request.ReceiptRequest.cbChargeItems[0].Description = "Candies\tvt0\tpr1";

        await sut.ProcessReceiptAsync(request);

        // One vt and one pr, and the name travels as text: a TAB inside a value would otherwise
        // open further protocol fields and silently change the PTU slot and the price.
        var trline = transport.SentPayloads.Single(p => p.StartsWith("trline"));
        trline.Should().StartWith("trline\tnaCandies vt0 pr1\tvt1\tpr999\t#");
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

        private string _scommResponse = "scomm\tfsT\ttzN\tts0\thrT\tnuZBF 2101002392\ttdN\t";

        /// <summary>Completed receipts of the day (bn = bt); every confirmed trend completes one.</summary>
        private int _completedReceipts = 84;

        private bool _anotherReceiptAfterTrend;

        /// <summary>Canceled receipts of the day (bc); bt numbers completed and canceled receipts alike.</summary>
        private int _canceledReceipts;

        private bool _anotherReceiptCanceledAfterTrend;

        public static FakePosNetTransport Confirming() => new();

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

        /// <summary>The register executes the command but its answer arrives corrupted (CRC mismatch).</summary>
        public void AnswerUnreadably(string mnemonic) => _unreadable.Add(mnemonic);

        /// <summary>Fails a command from its <paramref name="occurrence"/>-th sending on (1-based) — scnt is sent before and after a sale.</summary>
        public void FailUnreachable(string mnemonic, int occurrence)
        {
            FailUnreachable(mnemonic);
            _failFromOccurrence[mnemonic] = occurrence;
        }

        public Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
        {
            var payload = Encoding.ASCII.GetString(frame, 1, frame.Length - 2);
            var mnemonic = payload.Split('\t')[0];
            SentMnemonics.Add(mnemonic);
            SentPayloads.Add(payload);

            var armed = !_failFromOccurrence.TryGetValue(mnemonic, out var from) || SentMnemonics.Count(m => m == mnemonic) >= from;
            if (armed && _failures.TryGetValue(mnemonic, out var failure))
            {
                var exception = failure();
                if (exception is ExpectedDeviceError deviceError)
                {
                    return Task.FromResult(PosNetProtocolTests.EncodeResponse($"{mnemonic}\t?{deviceError.ErrorCode}\t"));
                }
                throw exception;
            }

            if (mnemonic == "trend")
            {
                _completedReceipts += _anotherReceiptAfterTrend ? 2 : 1;
                _canceledReceipts += _anotherReceiptCanceledAfterTrend ? 1 : 0;
            }

            var responsePayload = _answers.TryGetValue(mnemonic, out var answer) ? answer : mnemonic switch
            {
                // The T/N flags and the numer unikatowy in the shape a POSNET Online printer answers them.
                "scomm" => _scommResponse,
                "scnt" => $"scnt\trd12\tbn{_completedReceipts}\tbc{_canceledReceipts}\tbt{_completedReceipts + _canceledReceipts}\tfn3\t",
                // One protected-memory sequence across all document types, ahead of the receipt number.
                "eclastdocnoget" => $"eclastdocnoget\tdi{_completedReceipts + 11}\t",
                "rtcget" => "rtcget\tda2026-09-08;17:54\ttm2026-09-08T17:54:51+02:00\t",
                // The rate table as the office printer reports it after fiscalization: E–G not in use.
                "sfsk" => "sfsk\tfsT\tcl0\trd12\tvt1\tva23,00\tvb8,00\tvc5,00\tvd0,00\tve101,00\tvf101,00\tvg101,00\trw2026-09-03;16:54\tnuZBF 2101002392\t",
                // The e-paragon binding confirms with the unique eDokument id; the buffer record
                // reports an electronic document (prN = no paper) that reached the hub (st1).
                "eparagonidznext" => "eparagonidznext\tha3054\t",
                "eparagonbufferget" => "eparagonbufferget\thd3054\tprN\tst1\t",
                _ => $"{mnemonic}\t",
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
