using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The archive receipt 0x3011 (Archive receipt / archive all data receipt) is triggered by its receipt case alone
/// (contract §1.4, market-fr#221): no charge or pay items, no case data outside training, and a training one needs
/// no manager. The validator refuses them, and the archive receipt refuses them again before signing.
/// </summary>
public class ArchiveReceiptContentTests
{
    private const long Training = 0x0002_0000;

    private static ReceiptRequest Archive(long flags = 0, object? caseData = null) => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        ftReceiptCase = (ReceiptCase) (0x4652_2000_0000_3011 | flags),
        Currency = Currency.EUR,
        cbTerminalID = "T1",
        cbUser = "operator-1",
        ftReceiptCaseData = caseData,
        cbChargeItems = [],
        cbPayItems = [],
    };

    private static List<string> Errors(ReceiptRequest request) => new ReceiptValidationsFR().Validate(request).Errors.Select(x => x.ErrorCode).ToList();

    public static TheoryData<object?> EmptyCaseData => new() { null, "", " ", "{}", "null", JsonDocument.Parse("{}").RootElement, new { } };

    public static TheoryData<object?> CaseDataWithContent => new()
    {
        new { FR = new { ManagerId = "manager-7" } },
        "{\"FR\":{\"GuestCount\":2}}",
        "not json",
        JsonDocument.Parse("[1]").RootElement,
    };

    [Theory]
    [MemberData(nameof(EmptyCaseData))]
    public void AnArchiveReceiptWithoutCaseData_IsTaken(object? caseData)
        => Errors(Archive(caseData: caseData)).Should().BeEmpty();

    [Theory]
    [MemberData(nameof(CaseDataWithContent))]
    public void AnArchiveReceiptWithCaseData_IsRefused(object? caseData)
        => Errors(Archive(caseData: caseData)).Should().Contain("CaseDataNotAllowed");

    [Fact]
    public void ATrainingArchiveReceipt_NeedsNoManager()
        => Errors(Archive(Training)).Should().BeEmpty();

    [Fact]
    public void ATrainingArchiveReceipt_MayNameItsManager()
        => Errors(Archive(Training, new { FR = new { ManagerId = "manager-7" } })).Should().BeEmpty();

    [Fact]
    public void AnyOtherTrainingReceipt_StillNeedsItsManager()
        => Errors(new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) (0x4652_2000_0000_2000 | Training),
            Currency = Currency.EUR,
            cbChargeItems = [],
            cbPayItems = [],
        }).Should().Contain("TrainingRequiresManagerId");

    // The archive receipt itself, behind a validation the shared processor may pass over.

    private static async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        // StoredChainState blocks on its store while it is built: off the test framework's synchronisation context.
        var state = await Task.Run(() => new StoredChainState());
        var chains = state.Provider();
        var archive = new FRArchiveReceipt(new FRSigningPipeline(new FakeFRSSCD(), chains), chains,
            new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(new InMemoryQueueItemRepository())),
            new AsyncLazy<IMiddlewareReceiptJournalRepository>(() => Task.FromResult<IMiddlewareReceiptJournalRepository>(new InMemoryReceiptJournalRepository())),
            new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult<IMiddlewareActionJournalRepository>(new InMemoryActionJournalRepository())));
        var response = new ReceiptResponse { ftQueueID = state.QueueId, ftQueueItemID = Guid.NewGuid(), ftReceiptIdentification = "ft9#", ftReceiptMoment = DateTime.UtcNow, ftState = (State) StateFR.Success, ftSignatures = [] };
        var result = (await archive.SignAsync(new ProcessCommandRequest(new ftQueue { ftQueueId = state.QueueId, ftReceiptNumerator = 10 }, request, response)))!;
        return result.receiptResponse;
    }

    [Fact]
    public async Task ChargeItems_AreRefusedBeforeSigning()
    {
        var request = Archive();
        request.cbChargeItems = [new ChargeItem { Amount = 1m, Description = "x", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }];

        var response = await SignAsync(request);

        response.ftState.IsState(State.Error).Should().BeTrue();
        response.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent), "nothing is signed");
        response.ftReceiptIdentification.Should().NotContain("A");
    }

    [Fact]
    public async Task PayItems_AreRefusedBeforeSigning()
    {
        var request = Archive(Training);
        request.cbPayItems = [new PayItem { Amount = 1m, Description = "x", ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];

        (await SignAsync(request)).ftState.IsState(State.Error).Should().BeTrue("a training archive receipt carries no items either");
    }

    [Fact]
    public async Task CaseData_IsRefusedBeforeSigning()
        => (await SignAsync(Archive(caseData: new { FR = new { ManagerId = "manager-7" } }))).ftState.IsState(State.Error).Should().BeTrue();

    [Fact]
    public async Task ATrainingArchiveReceiptNamingItsManager_IsSigned()
        => (await SignAsync(Archive(Training, new { FR = new { ManagerId = "manager-7" } }))).ftState.IsState(State.Error).Should().BeFalse();

    [Fact]
    public async Task AnEmptyArchiveReceipt_IsSigned()
        => (await SignAsync(Archive(caseData: "{}"))).ftState.IsState(State.Error).Should().BeFalse();
}
