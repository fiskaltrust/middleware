using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The JET items the middleware adds so the POS sends the event (ADR 0009), market-fr#200 review of
/// 2026-11-07: lifecycle 260/127, closings 50/60, copies 155/156, and the start-up check 250/255,
/// 450 and 270.
/// </summary>
public class JetItemsTests
{
    private static ReceiptRequest Request(ReceiptCase receiptCase, bool training = false) => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        ftReceiptCase = training ? receiptCase.WithCountry("FR").WithFlag(ReceiptCaseFlags.Training) : receiptCase.WithCountry("FR"),
        cbReceiptReference = "R-1",
        Currency = Currency.EUR,
    };

    private static ReceiptResponse Response() => new()
    {
        ftQueueID = Guid.NewGuid(),
        ftQueueItemID = Guid.NewGuid(),
        ftReceiptIdentification = "ft1#E1",
        ftReceiptMoment = DateTime.UtcNow,
        ftSignatures = [],
    };

    private static List<string> JetCodes(ReceiptResponse response) => response.ftSignatures
        .Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent))
        .Select(x => JsonDocument.Parse(x.Data).RootElement.GetProperty("FR").GetProperty("Code").GetString()!)
        .ToList();

    [Theory]
    [InlineData(ReceiptCase.InitialOperationReceipt0x4001, "260")]
    [InlineData(ReceiptCase.OutOfOperationReceipt0x4002, "127")]
    [InlineData(ReceiptCase.DailyClosing0x2011, "50")]
    [InlineData(ReceiptCase.MonthlyClosing0x2012, "50")]
    [InlineData(ReceiptCase.YearlyClosing0x2013, "60")]
    public void SignedReceipt_CarriesTheJetItemOfItsCase(ReceiptCase receiptCase, string code)
    {
        var response = Response();

        FRJetItems.AddForSignedReceipt(Request(receiptCase), response);

        JetCodes(response).Should().Equal(code);
        var data = JsonDocument.Parse(response.ftSignatures.Single().Data).RootElement.GetProperty("FR");
        data.GetProperty("Message").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("Information").GetString().Should().Contain("cbReceiptReference=R-1").And.Contain("ftReceiptIdentification=ft1#E1");
    }

    [Theory]
    [InlineData(ReceiptCase.ShiftClosing0x2010)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.ZeroReceipt0x2000)]
    public void OtherReceipts_CarryNoJetItem(ReceiptCase receiptCase)
    {
        var response = Response();

        FRJetItems.AddForSignedReceipt(Request(receiptCase), response);

        JetCodes(response).Should().BeEmpty();
    }

    [Fact]
    public void TrainingClosing_CarriesNoJetItem()
    {
        var response = Response();

        FRJetItems.AddForSignedReceipt(Request(ReceiptCase.DailyClosing0x2011, training: true), response);

        JetCodes(response).Should().BeEmpty();
    }

    [Theory]
    [InlineData(ReceiptCase.Order0x3004, "155")]
    [InlineData(ReceiptCase.Pay0x3005, "156")]
    public void CopyOfANoteOrAPaymentProof_CarriesItsJetItem(ReceiptCase copiedCase, string code)
    {
        var response = Response();

        FRJetItems.AddForCopy(Request(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010), response, "B-7", copiedCase);

        JetCodes(response).Should().Equal(code);
        response.ftSignatures.Single().Data.Should().Contain("copied.cbReceiptReference=B-7");
    }

    [Fact]
    public void CopyOfATicket_OrOfAnUnknownReceipt_CarriesNoJetItem()
    {
        var ticket = Response();
        var unknown = Response();

        FRJetItems.AddForCopy(Request(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010), ticket, "T-1", ReceiptCase.PointOfSaleReceipt0x0001);
        FRJetItems.AddForCopy(Request(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010), unknown, "X-1", null);

        JetCodes(ticket).Should().BeEmpty();
        JetCodes(unknown).Should().BeEmpty();
    }

    [Theory]
    [InlineData("1.2.40", "2.0.1", FRJetItems.MajorVersionInstalled)]
    [InlineData("2.0.1+abc", "2.0.3+def", FRJetItems.MinorVersionInstalled)]
    [InlineData("2.0.1", "2.1.0", FRJetItems.MinorVersionInstalled)]
    [InlineData("2.0.1+abc", "2.0.1+def", null)]
    [InlineData("2.0.1", "2.0.1", null)]
    [InlineData(null, "2.0.1", null)]
    public void VersionChange_IsMajorOrMinor(string? previous, string current, int? expected)
        => FRJetWatch.VersionChange(previous, current).Should().Be(expected);

    [Fact]
    public void Compare_RaisesTheCodeOfEachChange()
    {
        var previous = new FRJetWatch.Installation("2.0.1", "CERT-1", "LNE");

        Codes(FRJetWatch.Compare(previous, previous with { })).Should().BeEmpty();
        Codes(FRJetWatch.Compare(previous, previous with { CertificateSerialNumber = "CERT-2" })).Should().Equal(450);
        Codes(FRJetWatch.Compare(previous, new FRJetWatch.Installation("2.0.2", "CERT-2", "SelfCertified"))).Should().Equal(255, 450, 270);
        Codes(FRJetWatch.Compare(previous, previous with { CertificateSerialNumber = null })).Should().BeEmpty("an unknown serial is not a change");

        static IEnumerable<int> Codes(IEnumerable<SignatureItem> items)
            => items.Select(x => int.Parse(JsonDocument.Parse(x.Data).RootElement.GetProperty("FR").GetProperty("Code").GetString()!));
    }

    [Theory]
    [InlineData("eyJhbGciOiJFUzI1NiJ9.eyJjc24iOiJDRVJULTEifQ.c2ln", false, "InfoCert")]
    [InlineData("a|b|c|CERT-1|d|e|f.c2ln", true, "LNE")]
    [InlineData("a|b|c|CERT-1|d|e|f.c2ln", false, "SelfCertified")]
    [InlineData(null, true, null)]
    public void ScuKind_IsReadFromTheReceipt(string? qrCode, bool hasCertificationBody, string? expected)
        => FRJetWatch.ScuKindOf(qrCode, hasCertificationBody).Should().Be(expected);

    [Fact]
    public async Task FirstReceiptAfterAMigrationFromV12_AsksForJet250_Once()
    {
        var repository = new InMemoryQueueItemRepository();
        await repository.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftQueueRow = 1, version = "v0", ProcessingVersion = "1.2.40", request = "{}", response = "{\"ftState\":5135673274548224}" });
        var first = Response();
        var second = Response();
        await repository.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = first.ftQueueItemID, ftQueueRow = 2, version = "v2", ProcessingVersion = "2.0.1+abc", request = "{}" });
        var sut = new FRJetWatch(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(repository)));

        await sut.CheckOnceAsync(Request(ReceiptCase.PointOfSaleReceipt0x0001), first);
        await repository.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = second.ftQueueItemID, ftQueueRow = 3, version = "v2", ProcessingVersion = "2.0.1+abc", request = "{}" });
        await sut.CheckOnceAsync(Request(ReceiptCase.PointOfSaleReceipt0x0001), second);

        JetCodes(first).Should().Equal("250");
        first.ftSignatures.Single().Data.Should().Contain("previousVersion=1.2.40").And.Contain("version=2.0.1+abc");
        JetCodes(second).Should().BeEmpty("the installation is checked once per process");
    }

    [Fact]
    public async Task FirstReceiptOfANewQueue_AsksForNothing()
    {
        var repository = new InMemoryQueueItemRepository();
        var first = Response();
        await repository.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = first.ftQueueItemID, ftQueueRow = 1, version = "v2", ProcessingVersion = "2.0.1", request = "{}" });
        var sut = new FRJetWatch(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(repository)));

        await sut.CheckOnceAsync(Request(ReceiptCase.InitialOperationReceipt0x4001), first);

        JetCodes(first).Should().BeEmpty();
    }
}
