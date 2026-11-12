using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.InfoCert;

/// <summary>
/// The strings the Infocert SCU signs as its JWS payload (market-fr#108, #221): R13 v4.2 §7, R12 v4.2 §7 and R19 v4.2,
/// formatted per their §4 (cents, AAAAMMJJHHMMSS, rate:amount|…, ',' between fields), and the 25-field data set on chain E.
/// </summary>
public class InfoCertSignedStringTests
{
    private const string PreviousSignature = "cHJldmlvdXMtc2lnbmF0dXJl";

    private static InfoCertConfiguration Configuration() => new()
    {
        Siret = FRTestData.Siret,
        PrivateKey = "unused",
        CertificateSerialNumber = "CERT-4711",
        Rcs = "RCS Nanterre B 642036065",
        Naf = "4711D",
        LegalForm = "SAS, au capital de 500000 EUR",
        VatOption = "receipts",
        ElectronicAddress = "123456789",
    };

    private static string Build(ProcessRequest request, string? previousSignature = null)
        => InfoCertSignedString.Build(request, previousSignature, Configuration());

    private static ProcessRequest WithCase(long receiptCase, string identification, bool training = false)
    {
        var request = FRTestData.Request();
        var value = ((ReceiptCase) receiptCase).WithCountry("FR");
        request.ReceiptRequest.ftReceiptCase = training ? value.WithFlag(ReceiptCaseFlags.Training) : value;
        request.ReceiptResponse.ftReceiptIdentification = identification;
        return request;
    }

    private static void StateData(ProcessRequest request, Dictionary<string, object> fr)
        => request.ReceiptResponse.ftStateData = JsonSerializer.SerializeToElement(new Dictionary<string, object> { ["FR"] = fr });

    [Theory]
    [InlineData("68.55", "6855")]
    [InlineData("-11.00", "-1100")]
    [InlineData("0.005", "1")]
    [InlineData("0", "0")]
    public void Money_IsWrittenInCents(string amount, string expected)
        => InfoCertStringFormat.Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).Should().Be(expected);

    [Theory]
    [InlineData("20", "2000")]
    [InlineData("10", "1000")]
    [InlineData("5.5", "0550")]
    [InlineData("2.1", "0210")]
    [InlineData("0", "0000")]
    public void Rate_IsWrittenOnFourDigits(string rate, string expected)
        => InfoCertStringFormat.Rate(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)).Should().Be(expected);

    [Theory]
    [InlineData("100")]
    [InlineData("-1")]
    public void Rate_OutOfRange_IsRefused(string rate)
    {
        var act = () => InfoCertStringFormat.Rate(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture));
        act.Should().Throw<FRValidationException>();
    }

    [Fact]
    public void Timestamp_IsUtcToTheSecond()
        => InfoCertStringFormat.Timestamp(new DateTime(2026, 3, 1, 10, 0, 7, 450, DateTimeKind.Utc)).Should().Be("20260301100007");

    [Fact]
    public void Text_ReplacesTheSeparators()
        => InfoCertStringFormat.Text("Dupont, Fils | Cie\r\n").Should().Be("Dupont; Fils / Cie  ");

    [Fact]
    public void Ticket_IsTheR13String()
    {
        // 12.00 at 20 % and 5.50 at 5.5 %, valued TTC, highest rate first (R13 v4.2 §4).
        Build(FRTestData.Request()).Should().Be("2000:1200|0550:550,1750,20260301100000,T1,VENTE,N,");
    }

    [Fact]
    public void Ticket_CarriesTheFlagAndThePreviousSignatureLast()
    {
        var request = FRTestData.Request(2);
        request.ReceiptResponse.ftReceiptIdentification = "T2";

        Build(request, PreviousSignature).Should().Be($"2000:1200|0550:550,1750,20260301100000,T2,VENTE,O,{PreviousSignature}");
    }

    [Theory]
    [InlineData(ReceiptCaseFlags.Refund, "RETOUR")]
    [InlineData(ReceiptCaseFlags.Void, "ANNULATION")]
    public void Ticket_OperationTypeFollowsTheFlags(ReceiptCaseFlags flag, string operation)
    {
        var request = FRTestData.Request();
        request.ReceiptRequest.ftReceiptCase = request.ReceiptRequest.ftReceiptCase.WithFlag(flag);

        Build(request).Split(',')[4].Should().Be(operation);
    }

    [Fact]
    public void Ticket_DownPaymentItemsCountInTheDocumentAmount()
    {
        var request = FRTestData.Request();
        request.ReceiptRequest.cbChargeItems!.Add(new ChargeItem { Amount = -6.00m, VATRate = 20m, Quantity = 1, Description = "Acompte", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithFlag(ChargeItemCaseFlags.Downpayment) });

        Build(request).Should().StartWith("2000:600|0550:550,1150,");
    }

    [Theory]
    [InlineData(0x0006, "B3")]
    [InlineData(0x0003, "B3")]
    [InlineData(0x0005, "B3")]
    [InlineData(0x3004, "B3")]
    [InlineData(0x0004, "T3")]
    public void NotesAndTickets_SignTheNoteTicketString(long receiptCase, string identification)
    {
        var fields = Build(WithCase(receiptCase, identification)).Split(',');

        fields.Should().HaveCount(7);
        fields[3].Should().Be(identification);
    }

    [Fact]
    public void Training_SignsTheOriginalTypesStringWithTheMarker()
    {
        Build(WithCase(0x0001, "X5", training: true)).Should().Be("2000:1200|0550:550,1750,20260301100000,X5,FORMATION-VENTE,N,");
    }

    [Fact]
    public void PaymentProof_IsTheR13String_WithThePosReferenceAsOrigin()
    {
        var request = WithCase(0x0002, "P4");
        request.ReceiptRequest.cbPreviousReceiptReference = "ticket-17";

        Build(request).Should().Be("P4,1750,20260301100000,ticket-17,N,");
    }

    [Fact]
    public void PaymentProof_PrefersTheOriginsFiscalNumberHandedOverByTheQueue()
    {
        var request = WithCase(0x3005, "P4");
        request.ReceiptRequest.cbPreviousReceiptReference = "ticket-17";
        StateData(request, new() { ["OriginReceiptIdentification"] = "T17" });

        Build(request).Split(',')[3].Should().Be("T17");
    }

    /// <summary>A payment proof that references several tickets signs every origin number, joined with ';'.</summary>
    [Fact]
    public void PaymentProof_OfSeveralTickets_SignsEveryOriginJoinedWithSemicolons()
    {
        var group = WithCase(0x0002, "P4");
        group.ReceiptRequest.cbPreviousReceiptReference = new[] { "ticket-17", "ticket-18" };
        var handedOver = WithCase(0x0002, "P5");
        StateData(handedOver, new() { ["OriginReceiptIdentification"] = "T17;T18" });

        Build(group).Split(',')[3].Should().Be("ticket-17;ticket-18");
        Build(handedOver).Split(',')[3].Should().Be("T17;T18");
    }

    [Fact]
    public void Invoice_IsTheR12String()
    {
        var request = WithCase(0x1002, "I8");
        request.ReceiptRequest.cbCustomer = new Dictionary<string, object> { ["CustomerName"] = "La Société, Nouvelle", ["CustomerZip"] = "75008", ["CustomerVATId"] = "FR12345678912" };

        Build(request).Should().Be("2000:1200|0550:550,1750,20260301100000,I8,FACTURE,La Société; Nouvelle,75008,FR12345678912,N,");
    }

    [Fact]
    public void Invoice_WithoutCustomerVatNumber_SignsAnEmptyField()
    {
        var fields = Build(WithCase(0x1001, "I8")).Split(',');

        fields.Should().HaveCount(10);
        fields[7].Should().BeEmpty("R12 fn 1: replaced by an empty string for a private individual");
    }

    [Fact]
    public void ProForma_SignsR12OnChainB()
        => Build(WithCase(0x0007, "B9")).Split(',')[4].Should().Be("PROFORMA");

    [Fact]
    public void CreditNote_IsAnAvoir()
    {
        var request = WithCase(0x1001, "I8");
        request.ReceiptRequest.ftReceiptCase = request.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);

        Build(request).Split(',')[4].Should().Be("AVOIR");
    }

    [Fact]
    public void DownPaymentInvoice_IsAnAcompte()
    {
        var request = WithCase(0x1001, "I8");
        request.ReceiptRequest.cbChargeItems = [new ChargeItem { Amount = 50m, VATRate = 20m, Quantity = 1, Description = "Acompte", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithFlag(ChargeItemCaseFlags.Downpayment) }];

        Build(request).Split(',')[4].Should().Be("ACOMPTE");
    }

    [Fact]
    public void CopyOfATicket_IsTheR13TicketDuplicate_WithoutReason()
    {
        var request = WithCase(0x3010, "D2");
        request.ReceiptRequest.cbUser = "Marie";
        request.ReceiptRequest.cbPreviousReceiptReference = "ticket-17";
        request.ReceiptRequest.ftReceiptCaseData = new Dictionary<string, object> { ["FR"] = new Dictionary<string, object> { ["CopyReason"] = "client" } };
        StateData(request, new() { ["CopyNumber"] = 3, ["CopyType"] = "ticket", ["CopiedReceiptIdentification"] = "T17" });

        Build(request).Should().Be("D2,TICKET,3,Marie,20260301100000,T17,N,");
    }

    [Theory]
    [InlineData("note", "NOTE")]
    [InlineData("order", "NOTE")]
    [InlineData("payment-proof", "JUSTIFICATIF")]
    public void CopyOfANoteOrPaymentProof_SignsTheReason(string copyType, string type)
    {
        var request = WithCase(0x3010, "D2");
        request.ReceiptRequest.cbUser = "Marie";
        request.ReceiptRequest.ftReceiptCaseData = new Dictionary<string, object> { ["FR"] = new Dictionary<string, object> { ["CopyReason"] = "perte, client" } };
        StateData(request, new() { ["CopyNumber"] = 1, ["CopyType"] = copyType, ["CopiedReceiptIdentification"] = "B4" });

        Build(request).Should().Be($"D2,{type},1,Marie,20260301100000,B4,perte; client,N,");
    }

    /// <summary>R12 §7.2: TAG-DUP-TYP from the copied receipt's case and flags (FR.CopiedReceiptCase), as a number or hex.</summary>
    [Theory]
    [InlineData(0x4652_2000_0000_1001L, null, "FACTURE")]
    [InlineData(0x4652_2000_0100_1001L, null, "AVOIR")]
    [InlineData(0x4652_2000_0004_1002L, null, "AVOIR")]
    [InlineData(0x4652_2000_0000_1001L, "down-payment-invoice", "ACOMPTE")]
    [InlineData(0x4652_2000_0000_0007L, null, "PROFORMA")]
    public void CopyOfAnR12Document_TakesItsTypeFromTheCopiedCase(long copiedCase, string? copyType, string type)
    {
        foreach (var value in new object[] { copiedCase, $"0x{copiedCase:X16}" })
        {
            var request = WithCase(0x3010, "D2");
            var state = new Dictionary<string, object> { ["CopyNumber"] = 1, ["CopiedReceiptCase"] = value, ["CopiedReceiptIdentification"] = "I4" };
            if (copyType is not null)
            {
                state["CopyType"] = copyType;
            }
            StateData(request, state);

            Build(request).Should().Be($"D2,{type},1,,20260301100000,I4,N,");
        }
    }

    [Theory]
    [InlineData(0x4652_2000_0000_0001L, "TICKET")]
    [InlineData(0x4652_2000_0000_3005L, "JUSTIFICATIF")]
    [InlineData(0x4652_2000_0000_3004L, "NOTE")]
    public void CopyOfAnR13Document_TakesItsStringFromTheCopiedCase(long copiedCase, string type)
    {
        var request = WithCase(0x3010, "D2");
        StateData(request, new() { ["CopyNumber"] = 1, ["CopiedReceiptCase"] = copiedCase, ["CopyType"] = "invoice" });

        Build(request).Split(',')[1].Should().Be(type, "the copied case wins over the copy type");
    }

    [Theory]
    [InlineData("invoice", "FACTURE")]
    [InlineData("pro-forma", "PROFORMA")]
    public void CopyOfAnR12Document_IsTheR12Duplicate_WithoutReason(string copyType, string type)
    {
        var request = WithCase(0x3010, "D2");
        request.ReceiptRequest.ftReceiptCaseData = new Dictionary<string, object> { ["FR"] = new Dictionary<string, object> { ["CopyReason"] = "client" } };
        StateData(request, new() { ["CopyNumber"] = 1, ["CopyType"] = copyType, ["CopiedReceiptIdentification"] = "I4" });

        Build(request).Should().Be($"D2,{type},1,,20260301100000,I4,N,");
    }

    [Fact]
    public void GrandTotals_SignThePeriodStartInThePeriodId()
    {
        var request = WithCase(0x2011, "Z42");
        request.ReceiptRequest.cbChargeItems = [];
        request.PeriodTotals = new FRPeriodTotals
        {
            Period = FRTotalsPeriod.Day,
            Current = new FRTotals { Totalizer = 24.00m, CINormal = 18.50m, CIReduced1 = 5.50m },
            Perpetual = new FRTotals { Totalizer = 1000.00m },
        };
        StateData(request, new() { ["Closing"] = new Dictionary<string, object> { ["Periods"] = new[] { new Dictionary<string, object> { ["Period"] = "Day", ["Start"] = new DateTime(2026, 2, 28, 9, 0, 0, DateTimeKind.Utc) } } } });

        Build(request, PreviousSignature).Should().Be($"2000:1850|0550:550,2400,100000,20260301100000,Z42:20260228090000-20260301100000,O,{PreviousSignature}");
    }

    [Fact]
    public void ArchiveReceipt_SignsTheBoundariesInTagArc()
    {
        var request = FRTestData.Archive();
        request.ReceiptResponse.ftReceiptIdentification = "A3";

        var fields = Build(request).Split(',');

        fields.Should().HaveCount(29);
        fields[0].Should().Be("A3");
        fields[1].Should().Be(FRTestData.PreviousArchive.ToString("D"));
        fields[3].Should().Be("20260101080000");
        fields[10].Should().Be("1250", "the day's archive total, in cents");
        fields[27].Should().Be("N");
    }

    [Fact]
    public void TrainingArchiveReceipt_KeepsTheShapeWithEmptyBoundaries()
    {
        var fields = Build(WithCase(0x3011, "X6", training: true)).Split(',');

        fields.Should().HaveCount(29);
        fields[0].Should().Be("X6");
        fields.Skip(1).Take(26).Should().OnlyContain(x => x == "");
    }

    [Fact]
    public void JetEvent_IsTheR19String_WithTheReceiptIdentificationAsId()
    {
        var request = WithCase(0x3001, "L7");
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];
        request.ReceiptRequest.cbUser = "Marie";
        request.ReceiptRequest.ftReceiptCaseData = new Dictionary<string, object> { ["FR"] = new Dictionary<string, object> { ["Code"] = 20, ["Message"] = "Archivage", ["Information"] = "période, 2026-02" } };

        Build(request, PreviousSignature).Should().Be($"L7,20,période; 2026-02,20260301100000,Marie,cashbox-fr,O,{PreviousSignature}");
    }

    [Fact]
    public void ChainE_SignsThe25FieldDataSet()
    {
        var request = WithCase(0x2000, "E9");
        request.ChainCounter = 9;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];

        var fields = Build(request, PreviousSignature).Split('|');

        fields.Should().HaveCount(25);
        fields[0].Should().Be(FRTestData.Siret);
        fields[1].Should().Be("RCS Nanterre B 642036065");
        fields[3].Should().Be("SAS, au capital de 500000 EUR");
        fields[5].Should().BeEmpty("field 6 is reserved: the configured electronic address is not signed");
        fields[8].Should().StartWith("1:", "the canonical request value, as the LNE SCU signs it");
        fields[10].Should().Be("E9");
        fields[21].Should().Be("CERT-4711");
        fields[22..].Should().Equal("9", "O", PreviousSignature);
    }

    [Fact]
    public void FinishMigration_SignsTheMigrationEventInField20()
    {
        var request = WithCase(0x4022, "E2");
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];
        StateData(request, new() { ["Migration"] = new FRMigrationEvent { Chains = [new FRV1ChainHead { Chain = "T", Counter = 41, LastHash = "h", LastSignature = "s" }] } });

        Build(request).Split('|')[19].Should().Be("T,41,h,s");
    }

    [Theory]
    [InlineData(0x0001, "NoteTicket")]
    [InlineData(0x0002, "PaymentProof")]
    [InlineData(0x0007, "Invoice")]
    [InlineData(0x1003, "Invoice")]
    [InlineData(0x3010, "Duplicate")]
    [InlineData(0x2013, "GrandTotals")]
    [InlineData(0x3011, "Archive")]
    [InlineData(0x3001, "Jet")]
    [InlineData(0x2000, "DataSet")]
    [InlineData(0x4002, "DataSet")]
    [InlineData(0x3002, "DataSet")]
    [InlineData(0x3000, "DataSet")]
    public void KindOf_FollowsTheReceiptCase(long receiptCase, string kind)
        => InfoCertSignedString.KindOf(((ReceiptCase) receiptCase).WithCountry("FR")).ToString().Should().Be(kind);

    [Fact]
    public async Task Scu_SignsTheStringAsTheJwsPayload_AndTheSignatureVerifies()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var configuration = Configuration();
        configuration.PrivateKey = privateKey;
        using var sut = new InfoCertFRSSCD(configuration);

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var jws = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data.Split('.');
        Encoding.UTF8.GetString(FRTestData.FromBase64Url(jws[1])).Should().Be("2000:1200|0550:550,1750,20260301100000,T1,VENTE,N,");
        jws[2].Should().Be(signature);
        key.VerifyData(Encoding.ASCII.GetBytes($"{jws[0]}.{jws[1]}"), FRTestData.FromBase64Url(jws[2]), System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            .Should().BeTrue();
    }
}
