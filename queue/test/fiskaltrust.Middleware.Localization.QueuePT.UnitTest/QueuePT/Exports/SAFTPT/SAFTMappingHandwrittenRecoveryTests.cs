using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Exports.SAFTPT;

public class SAFTMappingHandwrittenRecoveryTests
{
    [Fact]
    public void HandwrittenInvoice_ShouldUseManualDate_SystemEntryDateAndManualHashControlSyntax()
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository
            .Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(EmptyQueueItems());

        var documentStatusProvider = new DocumentStatusProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));
        var exporter = new SaftExporter(documentStatusProvider);

        var manualDocumentDate = new DateTime(2022, 1, 14, 0, 0, 0, DateTimeKind.Utc);
        var systemEntryDate = new DateTime(2026, 2, 10, 10, 30, 0, DateTimeKind.Utc);

        var receiptRequest = new ReceiptRequest
        {
            cbReceiptReference = "F/23",
            cbReceiptMoment = manualDocumentDate,
            ftReceiptCase = (ReceiptCase)5788286605450543105,
            ftReceiptCaseData = new ftReceiptCaseDataPayload
            {
                PT = new ftReceiptCaseDataPortugalPayload
                {
                    Series = "A",
                    Number = 105
                }
            },
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "Manual line item",
                    Amount = 100m,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)5788286605450018835
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Description = "Cash",
                    Amount = 100m,
                    ftPayItemCase = (PayItemCase)5788286605450018817
                }
            ],
            cbUser = "Operator"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = "ftD#FT ft20250a62/1",
            ftReceiptMoment = systemEntryDate,
            ftState = State.Success,
            ftSignatures = []
        };

        var auditFile = exporter.CreateAuditFile(
            new AccountMasterData
            {
                TaxId = "999999990",
                AccountName = "Test Company",
                Street = "Rua Teste",
                City = "Lisboa",
                Zip = "1000-000",
                Country = "PT"
            },
            [(receiptRequest, receiptResponse)]);

        var invoice = auditFile.SourceDocuments.SalesInvoices.Invoice.Should().ContainSingle().Subject;
        invoice.DocumentStatus.SourceBilling.Should().Be("M");
        invoice.HashControl.Should().Be("1-FTM A/105");
        invoice.InvoiceDate.Should().Be(manualDocumentDate);
        invoice.SystemEntryDate.Should().Be(systemEntryDate);
    }

    private static async IAsyncEnumerable<ftQueueItem> EmptyQueueItems()
    {
        yield break;
    }
}
