using System;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class AadeErrorContractTests
{
    [Fact]
    public void DuplicateInvoiceSignatureType_IsPinnedAgainstQueueGr()
    {
        // Mirrors the pin in QueueGR's InvoiceCounterReservationTests: the queue's
        // counter reservation matches FAILURE signatures of exactly this type to
        // advance the invoice counter past an aa AADE reported as already filed
        // ("number consumed, advance"). If the value drifts, the advance silently
        // stops triggering and duplicate rejections go back to permanently failing
        // the receipt without moving the counter.
        ((long) SignatureTypeGR.DuplicateInvoiceError).Should().Be(0x4752_2000_0000_001C);
    }

    [Fact]
    public void MarkDuplicateInvoiceFailure_RetypesTheFailureSignature_AndKeepsCategoryAndFlags()
    {
        var response = new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftCashBoxIdentification = "cashBoxIdentification",
            ftReceiptIdentification = "ft1#",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x4752_2000_0000_0000,
        };
        response.SetReceiptResponseError("{\"AADEError\":\"ValidationError\",\"Errors\":[{\"message\":\"duplicate invoice\",\"code\":\"233\"}]}");
        var typeBefore = response.ftSignatures.Single(s => s.Caption == "FAILURE").ftSignatureType;

        response.MarkDuplicateInvoiceFailure();

        var failure = response.ftSignatures.Single(s => s.Caption == "FAILURE");
        failure.ftSignatureType.IsType(SignatureTypeGR.DuplicateInvoiceError).Should().BeTrue();
        // Only the type nibbles change — the Failure category and any flags survive.
        failure.ftSignatureType.Should().Be(typeBefore.WithType(SignatureTypeGR.DuplicateInvoiceError));
    }
}
