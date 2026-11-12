using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// Data-set field 20 (archive-v2 §5, market-fr#221) is chosen by the receipt case: the archive boundaries on 0x3011
/// (Archive receipt), the migration event on 0x4022 (Finish migration), the closed periods on 0x2010–0x2013
/// (Shift / Daily / Monthly / Yearly closing receipt) and 0x4002 (Queue-stop receipt). Records are separated by ';',
/// values by ','.
/// </summary>
public class FRSignedRecordsTests
{
    private static readonly DateTime DayStart = new(2026, 11, 29, 22, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MonthStart = new(2026, 10, 31, 22, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime YearStart = new(2025, 12, 31, 22, 0, 0, DateTimeKind.Utc);

    private static ReceiptRequest Request(long receiptCase, bool training = false)
    {
        var value = ((ReceiptCase) receiptCase).WithCountry("FR");
        return new ReceiptRequest { ftReceiptCase = training ? value.WithFlag(ReceiptCaseFlags.Training) : value };
    }

    private static ReceiptResponse Response(Dictionary<string, object> fr, bool asJson = true)
    {
        object stateData = new Dictionary<string, object> { ["FR"] = fr };
        return new ReceiptResponse { ftStateData = asJson ? JsonSerializer.SerializeToElement(stateData) : stateData };
    }

    private static Dictionary<string, object> Closing(params (string Period, DateTime Start)[] periods) => new()
    {
        ["Closing"] = new Dictionary<string, object>
        {
            ["ClosingType"] = "monthly",
            ["Periods"] = periods.Select(x => new Dictionary<string, object> { ["Period"] = x.Period, ["Start"] = x.Start }).ToList(),
        },
    };

    private static Dictionary<string, object> Migration() => new()
    {
        ["Migration"] = new FRMigrationEvent
        {
            Chains =
            [
                new FRV1ChainHead { Chain = "T", Counter = 42, LastHash = "aGFzaC10", LastSignature = "c2lnLXQ" },
                new FRV1ChainHead { Chain = "A", Counter = 3, LastHash = "aGFzaC1h" },
            ],
        },
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMonthlyClosing_SignsTheDayAndTheMonth_ShortestFirst(bool asJson)
        => FRSignedRecords.Field20(Request(0x2012), Response(Closing(("Month", MonthStart), ("Day", DayStart)), asJson))
            .Should().Be("Day,2026-11-29T22:00:00.0000000Z;Month,2026-10-31T22:00:00.0000000Z");

    [Fact]
    public void TheQueueStop_SignsTheDayTheMonthAndTheYear()
        => FRSignedRecords.Field20(Request(0x4002), Response(Closing(("Day", DayStart), ("Month", MonthStart), ("Year", YearStart))))
            .Should().Be("Day,2026-11-29T22:00:00.0000000Z;Month,2026-10-31T22:00:00.0000000Z;Year,2025-12-31T22:00:00.0000000Z");

    [Fact]
    public void TheQueueStart_SignsNoPeriod()
        => FRSignedRecords.Field20(Request(0x4001), Response(Closing(("Day", DayStart)))).Should().BeEmpty("a v2 queue accepts 0x4001 only while new");

    [Fact]
    public void AFinishMigration_SignsOneRecordPerV1Chain()
        => FRSignedRecords.Field20(Request(0x4022), Response(Migration())).Should().Be("T,42,aGFzaC10,c2lnLXQ;A,3,aGFzaC1h,");

    [Fact]
    public void AnArchiveReceipt_SignsItsBoundaries_AsBefore()
    {
        var request = FRTestData.Archive();
        var boundaries = FRStateData.Archive(request.ReceiptResponse)!;

        FRSignedRecords.Field20(request.ReceiptRequest, request.ReceiptResponse).Should().Be(FRArchiveBoundaries.SignedField(boundaries)).And.NotContain(";");
    }

    [Fact]
    public void ContentForAnotherCase_IsNotSigned()
    {
        FRSignedRecords.Field20(Request(0x0001), Response(Migration())).Should().BeEmpty("a ticket");
        FRSignedRecords.Field20(Request(0x3011), Response(Closing(("Day", DayStart)))).Should().BeEmpty("an archive receipt signs boundaries only");
        FRSignedRecords.Field20(Request(0x2011, training: true), Response(Closing(("Day", DayStart)))).Should().BeEmpty("training closes nothing");
    }

    [Fact]
    public void AValueWithASeparator_IsRefused()
    {
        var migration = Migration();
        ((FRMigrationEvent) migration["Migration"]).Chains[0].LastHash = "a;b";

        var act = () => FRSignedRecords.Field20(Request(0x4022), Response(migration));

        act.Should().Throw<FRValidationException>().WithMessage("*a;b*");
    }

    // Field 21 (market-fr#221, as v1.2): every period the receipt closes, shortest first, then the perpetual totals.

    private const string Zeros = "0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00";

    private static FRPeriodTotals Monthly() => new()
    {
        Period = FRTotalsPeriod.Month,
        Current = new FRTotals { Totalizer = 13m },
        Perpetual = new FRTotals { Totalizer = 113m },
        Closed =
        [
            new FRPeriodTotal { Period = FRTotalsPeriod.Day, Totals = new FRTotals { Totalizer = 3m } },
            new FRPeriodTotal { Period = FRTotalsPeriod.Month, Totals = new FRTotals { Totalizer = 13m } },
        ],
    };

    [Fact]
    public void AMonthlyClosing_SignsTheDayTheMonthAndThePerpetualTotals()
        => FRSignedRecords.Field21(Monthly()).Should().Be($"Day,3.00,{Zeros};Month,13.00,{Zeros};Perpetual,113.00,{Zeros}");

    [Fact]
    public void AReceiptReportingItsOwnPeriodOnly_SignsItAndThePerpetualTotals()
        => FRSignedRecords.Field21(new FRPeriodTotals { Period = FRTotalsPeriod.Archive, Current = new FRTotals { Totalizer = 5m }, Perpetual = new FRTotals { Totalizer = 7m } })
            .Should().Be($"Archive,5.00,{Zeros};Perpetual,7.00,{Zeros}");

    [Fact]
    public void NoPeriodTotals_IsEmpty() => FRSignedRecords.Field21(null).Should().BeEmpty();

    [Fact]
    public void AMonthlyClosing_PrintsTheDayAndTheMonthThenThePerpetualTotals()
    {
        var response = new ReceiptResponse { ftSignatures = [] };

        response.AddPeriodTotalsItems(Monthly());

        response.ftSignatures.Select(x => x.Caption).Should().Equal("Day Totals", "Month Totals", "Totals");
        response.ftSignatures[0].Data.Should().StartWith("Total TTC: 3.00");
    }

    [Fact]
    public async Task TheLneDataSet_SignsField21()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var lne = new LneFRSSCD(new LneConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C", LneCertificateNumber = "L" });
        var request = FRTestData.Request();
        request.ReceiptRequest.ftReceiptCase = ((ReceiptCase) 0x2012).WithCountry("FR");
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];
        request.PeriodTotals = Monthly();

        var (response, _) = await lne.ProcessReceiptAsync(request, null);

        var fields = FRReceiptQrCode.SignedContentOf(response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data).Split('|');
        fields.Should().HaveCount(25);
        fields[20].Should().Be(FRSignedRecords.Field21(Monthly()));
    }

    [Fact]
    public async Task TheLneAndSelfCertifiedDataSets_SignField20()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var lne = new LneFRSSCD(new LneConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C", LneCertificateNumber = "L" });
        using var selfCertified = new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C" });

        foreach (var scu in new IFRSSCD[] { lne, selfCertified })
        {
            var request = FRTestData.Request();
            request.ReceiptRequest.ftReceiptCase = ((ReceiptCase) 0x4022).WithCountry("FR");
            request.ReceiptRequest.cbChargeItems = [];
            request.ReceiptRequest.cbPayItems = [];
            request.ReceiptResponse.ftStateData = JsonSerializer.SerializeToElement(new Dictionary<string, object> { ["FR"] = Migration() });

            var (response, _) = await scu.ProcessReceiptAsync(request, null);

            var fields = FRReceiptQrCode.SignedContentOf(response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data).Split('|');
            fields.Should().HaveCount(25);
            fields[19].Should().Be("T,42,aGFzaC10,c2lnLXQ;A,3,aGFzaC1h,");
        }
    }
}
