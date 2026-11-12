using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// An invoice prints its sales-line count, as a ticket does (R12 FAC-LIG-NBR, archive-v2 §1.2 <c>LineCount</c> on chain I).
/// The count is already signed by the LNE data set, so nothing signed changes.
/// </summary>
public class InvoiceLineCountTests
{
    public static TheoryData<ReceiptCase> Invoices => new()
    {
        ReceiptCase.InvoiceUnknown0x1000, ReceiptCase.InvoiceB2C0x1001, ReceiptCase.InvoiceB2B0x1002, ReceiptCase.InvoiceB2G0x1003,
    };

    [Theory]
    [MemberData(nameof(Invoices))]
    public async Task AnInvoice_PrintsItsSalesLineCount(ReceiptCase receiptCase)
    {
        var sut = new InvoiceCommandProcessorFR(new FRSigningPipeline(new FakeFRSSCD(), new StoredChainState().Provider()));
        var request = new ProcessCommandRequest(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest
            {
                ftReceiptCase = receiptCase.WithCountry("FR"),
                Currency = Currency.EUR,
                cbChargeItems =
                [
                    new ChargeItem { Position = 1, Amount = 10, VATRate = 20, Quantity = 1, Description = "a", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
                    new ChargeItem { Position = 2, Amount = 5, VATRate = 20, Quantity = 1, Description = "b", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
                    new ChargeItem { Position = 3, Amount = -1, VATRate = 20, Quantity = 1, Description = "remise", ftChargeItemCase = (ChargeItemCase) ((long) ChargeItemCase.NormalVatRate.WithCountry("FR") | (long) ChargeItemCaseFlags.ExtraOrDiscount) },
                ],
                cbPayItems = [],
            },
            new ReceiptResponse { ftQueueID = Guid.NewGuid(), ftQueueItemID = Guid.NewGuid(), ftReceiptIdentification = "ft1#", ftReceiptMoment = DateTime.UtcNow, ftState = (State) StateFR.Success, ftSignatures = [] });

        var response = receiptCase switch
        {
            ReceiptCase.InvoiceUnknown0x1000 => await sut.InvoiceUnknown0x1000Async(request),
            ReceiptCase.InvoiceB2C0x1001 => await sut.InvoiceB2C0x1001Async(request),
            ReceiptCase.InvoiceB2B0x1002 => await sut.InvoiceB2B0x1002Async(request),
            _ => await sut.InvoiceB2G0x1003Async(request),
        };

        response.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "Nombre de lignes").Which.Data.Should().Be("2");
    }
}
