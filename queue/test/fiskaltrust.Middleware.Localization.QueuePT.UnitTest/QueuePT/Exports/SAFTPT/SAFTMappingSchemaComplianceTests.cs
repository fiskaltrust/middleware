using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Exports.SAFTPT;

public class SAFTMappingSchemaComplianceTests
{
    private static readonly AccountMasterData _accountMasterData = new()
    {
        TaxId = "999999990",
        AccountName = "Test Company",
        Street = "Rua Teste",
        City = "Lisboa",
        Zip = "1000-000",
        Country = "PT"
    };

    [Fact]
    public void EmptyExport_ShouldOmitTaxTable()
    {
        var data = new SaftExporter().SerializeAuditFile(_accountMasterData, [], 0);

        // TaxTable requires at least one TaxTableEntry, an empty <TaxTable /> fails XSD validation
        Encoding.UTF8.GetString(data).Should().NotContain("TaxTable");
    }

    [Fact]
    public void ChargeItemWithEmptyUnitAndProductGroup_ShouldNotProduceEmptyElements()
    {
        var exporter = CreateExporter();
        var receipt = CreateInvoice("ftD#FT ft20250a62/1", new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc), unit: "", productGroup: "  ");

        var auditFile = exporter.CreateAuditFile(_accountMasterData, [receipt]);

        auditFile.SourceDocuments.SalesInvoices.Invoice.Should().ContainSingle()
            .Which.Line.Should().ContainSingle()
            .Which.UnitOfMeasure.Should().Be("Unit");
        auditFile.MasterFiles.Product.Should().ContainSingle()
            .Which.ProductGroup.Should().BeNull();
    }

    [Fact]
    public void ChargeItemWithUnitAndProductGroup_ShouldBeExported()
    {
        var exporter = CreateExporter();
        var receipt = CreateInvoice("ftD#FT ft20250a62/1", new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc), unit: "kg", productGroup: "Food");

        var auditFile = exporter.CreateAuditFile(_accountMasterData, [receipt]);

        auditFile.SourceDocuments.SalesInvoices.Invoice.Single().Line.Single().UnitOfMeasure.Should().Be("kg");
        auditFile.MasterFiles.Product!.Single().ProductGroup.Should().Be("Food");
        auditFile.MasterFiles.TaxTable!.TaxTableEntry.Should().ContainSingle();
    }

    [Fact]
    public void Header_ShouldCoverMonthsOfExportedDocuments()
    {
        var exporter = CreateExporter();
        var receipts = new List<(ReceiptRequest, ReceiptResponse)>
        {
            CreateInvoice("ftD#FT ft20250a62/1", new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc)),
            CreateInvoice("ftD#FT ft20250a62/2", new DateTime(2025, 3, 2, 10, 0, 0, DateTimeKind.Utc))
        };

        var auditFile = exporter.CreateAuditFile(_accountMasterData, receipts);

        auditFile.Header.FiscalYear.Should().Be(2025);
        auditFile.Header.StartDate.Should().Be(new DateTime(2025, 1, 1));
        auditFile.Header.EndDate.Should().Be(new DateTime(2025, 3, 31));
    }

    [Fact]
    public void Header_WithoutDocuments_ShouldCoverCurrentMonth()
    {
        var auditFile = new SaftExporter().CreateAuditFile(_accountMasterData, [], 0);

        var now = DateTime.UtcNow;
        auditFile.Header.FiscalYear.Should().Be(now.Year);
        auditFile.Header.StartDate.Should().Be(new DateTime(now.Year, now.Month, 1));
        auditFile.Header.EndDate.Should().Be(new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)));
    }

    private static SaftExporter CreateExporter()
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository
            .Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(EmptyQueueItems());

        var documentStatusProvider = new DocumentStatusProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));
        return new SaftExporter(documentStatusProvider);
    }

    private static (ReceiptRequest, ReceiptResponse) CreateInvoice(string receiptIdentification, DateTime receiptMoment, string? unit = null, string? productGroup = null)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbReceiptReference = receiptIdentification,
            cbReceiptMoment = receiptMoment,
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_1001,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "Line item",
                    Amount = 100m,
                    VATRate = 23m,
                    Unit = unit,
                    ProductGroup = productGroup,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Description = "Cash",
                    Amount = 100m,
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001
                }
            ],
            cbUser = "Operator"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftReceiptIdentification = receiptIdentification,
            ftReceiptMoment = receiptMoment,
            ftState = State.Success,
            ftSignatures = []
        };

        return (receiptRequest, receiptResponse);
    }

    private static async IAsyncEnumerable<ftQueueItem> EmptyQueueItems()
    {
        yield break;
    }
}
