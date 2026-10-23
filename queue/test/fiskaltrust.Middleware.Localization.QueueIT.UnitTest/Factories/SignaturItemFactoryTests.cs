using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Factories;

public class SignaturItemFactoryTests
{
    private static readonly DateTime _documentMoment = new(2026, 6, 29, 10, 9, 0);

    private static ReceiptResponse BuildResponse(string? lotteryCode, string documentType = "POSRECEIPT")
    {
        return new ReceiptResponse
        {
            ftCashBoxIdentification = TestHelpers.CashBoxIdentification,
            ftSignatures = TestHelpers.CreateRTSignatures(zNumber: 3, documentNumber: 539, _documentMoment, documentType, lotteryCode)
        };
    }

    [Fact]
    public void CreatePOSReceiptFormatSignatures_WithLotteryCode_IncludesCodiceLotteriaInFooter()
    {
        var signatures = SignaturItemFactory.CreatePOSReceiptFormatSignatures(BuildResponse("DT1MV66K"));

        var footer = signatures.Single(x => x.Caption == "[www.fiskaltrust.it]");
        footer.Data.Should().Contain("29-06-2026 10:09").And.Contain("DOCUMENTO N. 0003-0539").And.Contain("Codice Lotteria: DT1MV66K").And.Contain($"Cassa {TestHelpers.CashBoxIdentification}");
    }

    [Fact]
    public void CreatePOSReceiptFormatSignatures_WithoutLotteryCode_OmitsCodiceLotteria()
    {
        var signatures = SignaturItemFactory.CreatePOSReceiptFormatSignatures(BuildResponse(null));

        var footer = signatures.Single(x => x.Caption == "[www.fiskaltrust.it]");
        footer.Data.Should().NotContain("Codice Lotteria");
    }

    [Fact]
    public void CreatePOSReceiptFormatSignatures_ForRTServer_PrintsTheElectronicSignature()
    {
        var response = new ReceiptResponse
        {
            ftCashBoxIdentification = TestHelpers.CashBoxIdentification,
            ftSignatures = TestHelpers.CreateRTSignatures(zNumber: 3, documentNumber: 539, _documentMoment, customerId: "RSSMRA85T10A562S", shaMetadata: "ABCDEF")
        };

        var footer = SignaturItemFactory.CreatePOSReceiptFormatSignatures(response).Single(x => x.Caption == "[www.fiskaltrust.it]").Data;

        footer.Should().Contain("Cod. Fisc./P.IVA: RSSMRA85T10A562S").And.Contain($"Server RT {TestHelpers.RTSerialNumber}").And.Contain("-----FIRMA ELETTRONICA-----\r\nABCDEF".Replace("\r\n", Environment.NewLine));
    }

    [Fact]
    public void CreatePOSReceiptFormatSignatures_UnreferencedRefund_RendersND_WithoutThrowing()
    {
        var header = SignaturItemFactory.CreatePOSReceiptFormatSignatures(BuildResponse(null, "REFUND"))
            .Single(x => x.Caption == "DOCUMENTO COMMERCIALE").Data;

        header.Should().Contain("emesso per RESO MERCE");
        header.Should().Contain("ND");
        header.Should().NotContain("del ");
    }

    [Fact]
    public void CreatePOSReceiptFormatSignatures_ReferencedVoid_RendersTheReferencedDocument()
    {
        var response = BuildResponse(null, "VOID");
        response.ftSignatures.AddRange(
        [
            TestHelpers.RTSignature(SignatureTypeIT.RTReferenceZNumber, "<reference-z-number>", "0002"),
            TestHelpers.RTSignature(SignatureTypeIT.RTReferenceDocumentNumber, "<reference-doc-number>", "0017"),
            TestHelpers.RTSignature(SignatureTypeIT.RTReferenceDocumentMoment, "<reference-timestamp>", "2026-06-28 18:00:00"),
        ]);

        var header = SignaturItemFactory.CreatePOSReceiptFormatSignatures(response).Single(x => x.Caption == "DOCUMENTO COMMERCIALE").Data;

        header.Should().Contain("emesso per ANNULLAMENTO").And.Contain("N. 0002-0017 del 28-06-2026");
    }

    [Fact]
    public void CreateInitialOperationSignature_KeepsThePreV2Value()
    {
        var queueIT = new ftQueueIT { ftQueueITId = Guid.NewGuid() };

        var signature = SignaturItemFactory.CreateInitialOperationSignature(queueIT, new RTInfo { SerialNumber = "96SRT001239" });

        ((ulong) signature.ftSignatureType).Should().Be(0x4954_2000_0001_1001);
        signature.Data.Should().Be($"Queue-ID: {queueIT.ftQueueITId} Serial-Nr: 96SRT001239");
    }

    [Fact]
    public void CreateOutOfOperationSignature_KeepsThePreV2Value()
    {
        var queueIT = new ftQueueIT { ftQueueITId = Guid.NewGuid() };

        var signature = SignaturItemFactory.CreateOutOfOperationSignature(queueIT);

        ((ulong) signature.ftSignatureType).Should().Be(0x4954_2000_0001_1002);
        signature.Data.Should().Be($"Queue-ID: {queueIT.ftQueueITId}");
    }
}
