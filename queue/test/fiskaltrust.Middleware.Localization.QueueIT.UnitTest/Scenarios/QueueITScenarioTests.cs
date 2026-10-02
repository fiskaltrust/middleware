using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Scenarios;

/// <summary>
/// The Italian queue on the v2 stream end to end: the JSON entry points of the bootstrapper, the in-memory
/// storage provider and a fake RT device behind the v1 client factory.
/// </summary>
public class QueueITScenarioTests
{
    private readonly Guid _cashBoxId = Guid.NewGuid();
    private readonly Guid _queueId = Guid.NewGuid();
    private readonly Guid _scuId = Guid.NewGuid();
    private readonly FakeITSSCD _sscd = new();
    private readonly FakeITSSCDClientFactory _clientFactory;
    private readonly Func<string, Task<string>> _sign;
    private readonly Func<string, Task<(System.Net.Mime.ContentType contentType, System.IO.Pipelines.PipeReader reader)>> _journal;

    public QueueITScenarioTests()
    {
        _clientFactory = new FakeITSSCDClientFactory(_sscd);
        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "scu-timeout-ms", 20000 },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue> { new() { ftQueueId = _queueId, ftCashBoxId = _cashBoxId, CountryCode = "IT", Timeout = 15000 } }) },
            { "init_ftQueueIT", JsonSerializer.Serialize(new List<ftQueueIT> { new() { ftQueueITId = _queueId, CashBoxIdentification = "00040005", ftSignaturCreationUnitITId = _scuId } }) },
            { "init_ftSignaturCreationUnitIT", JsonSerializer.Serialize(new List<ftSignaturCreationUnitIT> { new() { ftSignaturCreationUnitITId = _scuId, Url = "[\"rest://localhost:1401\",\"grpc://localhost:1400\"]" } }) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration { Account = new AccountMasterData { AccountId = Guid.NewGuid(), AccountName = "fiskaltrust Italia", VatId = "IT12345678901", Country = "IT" } }) },
        };

        var loggerFactory = LoggerFactory.Create(_ => { });
        var bootstrapper = new QueueITBootstrapper(_queueId, loggerFactory, _clientFactory, configuration, new InMemoryStorageProvider(loggerFactory, _queueId, configuration));
        _sign = bootstrapper.RegisterForSign();
        _journal = bootstrapper.RegisterForJournal();
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _sign(JsonSerializer.Serialize(request)));
        response.Should().NotBeNull();
        return response!;
    }

    private ReceiptRequest CreateRequest(ReceiptCase receiptCase, ulong flags = 0, string? cbPreviousReceiptReference = null, bool withItems = false)
    {
        var request = new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCase = (ReceiptCase) (TestHelpers.BaseState | (ulong) receiptCase | flags),
            cbChargeItems = [],
            cbPayItems = [],
        };
        if (cbPreviousReceiptReference is not null)
        {
            request.cbPreviousReceiptReference = cbPreviousReceiptReference;
        }
        if (withItems)
        {
            var sign = flags == 0 ? 1 : -1;
            request.cbChargeItems.Add(new ChargeItem { Position = 1, Quantity = sign * 1, Amount = sign * 122m, VATRate = 22m, VATAmount = sign * 22m, Description = "Item VAT 22%", ftChargeItemCase = (ChargeItemCase) 0x4954_2000_0020_0013, Moment = DateTime.UtcNow });
            request.cbPayItems.Add(new PayItem { Position = 1, Quantity = 1, Amount = sign * 122m, Description = "Cash", ftPayItemCase = (PayItemCase) 0x4954_2000_0000_0001, Moment = DateTime.UtcNow });
            request.cbCustomer = new { CustomerName = "Mario Rossi", CustomerTaxId = "RSSMRA85T10A562S" };
        }
        return request;
    }

    private async Task<List<ftActionJournal>> GetActionJournalsAsync()
    {
        var (_, reader) = await _journal(JsonSerializer.Serialize(new JournalRequest { ftJournalType = JournalType.ActionJournal }));
        using var stream = reader.AsStream();
        using var document = await JsonDocument.ParseAsync(stream);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftActionJournal>>(document.RootElement.GetRawText())!;
    }

    [Fact]
    public async Task TheLifeOfAnItalianQueue()
    {
        // A receipt before the activation is stored, but never reaches the RT device.
        var beforeActivation = await SignAsync(CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, withItems: true));
        beforeActivation.State().Should().Be(0x4954_2000_0000_0001, "the security mechanism is not active yet");
        _sscd.Requests.Should().BeEmpty();

        // Initial operation: the RT device is registered and the queue starts operating.
        var initialOperation = await SignAsync(CreateRequest(ReceiptCase.InitialOperationReceipt0x4001));
        using (new AssertionScope())
        {
            initialOperation.State().Should().Be(TestHelpers.BaseState);
            ((ulong) initialOperation.ftSignatures[0].ftSignatureType).Should().Be(0x4954_2000_0001_1001);
            initialOperation.ftSignatures[0].Data.Should().Contain($"Serial-Nr: {FakeITSSCD.SerialNumber}");
            _clientFactory.Configurations.Should().ContainSingle().Which.Url.Should().Be("grpc://localhost:1400/");
            _clientFactory.Configurations[0].Timeout.Should().Be(TimeSpan.FromSeconds(20));
            (await GetActionJournalsAsync()).Should().Contain(x => x.Type == "4954200000004001-ActivateQueueSCU");
        }

        // The zero receipt hands the state of the RT device back to the caller.
        var zeroReceipt = await SignAsync(CreateRequest(ReceiptCase.ZeroReceipt0x2000));
        using (new AssertionScope())
        {
            zeroReceipt.State().Should().Be(TestHelpers.BaseState);
            zeroReceipt.ftStateData.Should().BeOfType<JsonElement>().Which.GetProperty("CashStatus").GetString().Should().Be("ok");
        }

        // A sale: the RT device numbers the document, the queue builds the printable header and footer.
        var saleRequest = CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, withItems: true);
        var sale = await SignAsync(saleRequest);
        using (new AssertionScope())
        {
            sale.State().Should().Be(TestHelpers.BaseState);
            sale.ftReceiptIdentification.Should().EndWith("#0001-0001");
            sale.ftSignatures[0].Caption.Should().Be("[www.fiskaltrust.it]");
            sale.ftSignatures[0].Data.Should().Contain("DOCUMENTO N. 0001-0001").And.Contain("Cassa 00040005");
            sale.ftSignatures[1].Data.Should().Be("di vendita o prestazione");
            sale.ftCashBoxIdentification.Should().Be("00040005");
            _sscd.Requests.Last().cbCustomer.Should().Contain("RSSMRA85T10A562S", "cbCustomer reaches the SCU as the JSON string the v1 contract carries");
            _sscd.Requests.Last().cbChargeItems.Should().ContainSingle().Which.Amount.Should().Be(122m);
        }

        // A refund of the sale: the queue resolves the reference and hands the RT identification of the sale to the device.
        var refund = await SignAsync(CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund, saleRequest.cbReceiptReference, withItems: true));
        using (new AssertionScope())
        {
            refund.State().Should().Be(TestHelpers.BaseState);
            refund.ftReceiptIdentification.Should().EndWith("#0001-0002");
            refund.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)!.Data.Should().Be("0001");
            refund.GetSignatureItem(SignatureTypeIT.RTReferenceZNumber)!.Data.Should().Be("0001");
            refund.ftSignatures[1].Data.Should().Contain("emesso per RESO MERCE").And.Contain("N. 0001-0001 del");
            _sscd.Requests.Last().cbPreviousReceiptReference.Should().Be(saleRequest.cbReceiptReference);
        }

        // A refund of something the queue never saw is refused before the RT device is involved.
        var scuCalls = _sscd.Requests.Count;
        var unknownRefund = await SignAsync(CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, (ulong) ReceiptCaseFlags.Refund, "never-signed", withItems: true));
        unknownRefund.ftState.IsState(State.Error).Should().BeTrue();
        _sscd.Requests.Should().HaveCount(scuCalls);

        // A copy of the sale is printed with its references.
        var copy = await SignAsync(CreateRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010, cbPreviousReceiptReference: saleRequest.cbReceiptReference));
        copy.State().Should().Be(TestHelpers.BaseState);
        copy.GetSignatureItem(SignatureTypeIT.RTReferenceDocumentNumber)!.Data.Should().Be("0001");

        // The daily closing takes the Z number of the report.
        var dailyClosing = await SignAsync(CreateRequest(ReceiptCase.DailyClosing0x2011));
        using (new AssertionScope())
        {
            dailyClosing.State().Should().Be(TestHelpers.BaseState);
            dailyClosing.ftReceiptIdentification.Should().EndWith("#Z0001");
            (await GetActionJournalsAsync()).Should().Contain(x => x.Message == "Daily-Closing receipt was processed.");
        }

        // The next day starts a new numbering.
        var nextDaySale = await SignAsync(CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, withItems: true));
        nextDaySale.ftReceiptIdentification.Should().EndWith("#0002-0001");

        // Out of operation: the queue stops, later receipts are stored without the RT device again.
        var outOfOperation = await SignAsync(CreateRequest(ReceiptCase.OutOfOperationReceipt0x4002));
        using (new AssertionScope())
        {
            outOfOperation.State().Should().Be(0x4954_2000_0000_0001);
            ((ulong) outOfOperation.ftSignatures[0].ftSignatureType).Should().Be(0x4954_2000_0001_1002);
            (await GetActionJournalsAsync()).Should().Contain(x => x.Type == "4954200000004002-DeactivateQueueSCU");
        }

        scuCalls = _sscd.Requests.Count;
        var afterDeactivation = await SignAsync(CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001, withItems: true));
        afterDeactivation.State().Should().Be(0x4954_2000_0000_0001);
        _sscd.Requests.Should().HaveCount(scuCalls);
    }

    [Fact]
    public async Task ASecondInitialOperation_IsRefused()
    {
        (await SignAsync(CreateRequest(ReceiptCase.InitialOperationReceipt0x4001))).State().Should().Be(TestHelpers.BaseState);

        var second = await SignAsync(CreateRequest(ReceiptCase.InitialOperationReceipt0x4001));

        second.ftState.IsState(State.Error).Should().BeTrue();
        second.ftSignatures.Should().ContainSingle(x => x.Data == "The queue is already operational. It is not allowed to send another InitOperation Receipt");
    }
}
