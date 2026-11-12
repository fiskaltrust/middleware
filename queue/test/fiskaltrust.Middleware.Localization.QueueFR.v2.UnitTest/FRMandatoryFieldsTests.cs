using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// What the archive contract marks mandatory is refused when missing (contract §1.1, §1.2; market-fr#140):
/// the manager on a training receipt, the original on a void, the customer on an InfoCert invoice, the
/// operator on InfoCert tickets and invoices, the terminal on LNE archive and log receipts.
/// </summary>
public class FRMandatoryFieldsTests
{
    private const string InfoCert = "Infocert";
    private const string Lne = "LNE";

    private static ReceiptRequest Sale(long receiptCase = 0x0001, object? caseData = null, object? customer = null) => new()
    {
        ftReceiptCase = (ReceiptCase) (0x4652_2000_0000_0000 | receiptCase),
        Currency = Currency.EUR,
        cbTerminalID = "T1",
        cbUser = "operator-1",
        ftReceiptCaseData = caseData,
        cbCustomer = customer,
        cbChargeItems = [new ChargeItem { Amount = 12m, VATRate = 20m, VATAmount = 2m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
        cbPayItems = [new PayItem { Amount = 12m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }],
    };

    private static List<string> Errors(ReceiptRequest request, string? certificationBody = null)
        => new ReceiptValidationsFR(certificationBody).Validate(request).Errors.Select(x => x.ErrorCode).ToList();

    private static object FullCustomer(string country = "DE", string? vatId = null, string? taxId = null) => new Dictionary<string, object?>
    {
        ["CustomerName"] = "Client SARL",
        ["CustomerId"] = "C-42",
        ["CustomerStreet"] = "1 rue de la Paix",
        ["CustomerZip"] = "75002",
        ["CustomerCity"] = "Paris",
        ["CustomerCountry"] = country,
        ["CustomerVATId"] = vatId,
        ["CustomerTaxId"] = taxId,
    };

    // Training (flag 0x0002_0000): ManagerId, contract §1.2 (D4).

    [Fact]
    public void ATrainingReceiptWithoutManagerId_IsRefused()
        => Errors(Sale(0x0002_0001)).Should().Contain("TrainingRequiresManagerId");

    [Fact]
    public void ATrainingReceiptWithAnEmptyManagerId_IsRefused()
        => Errors(Sale(0x0002_0001, caseData: new { FR = new { ManagerId = " " } })).Should().Contain("TrainingRequiresManagerId");

    [Fact]
    public void ATrainingReceiptWithManagerId_IsTaken()
        => Errors(Sale(0x0002_0001, caseData: new { FR = new { ManagerId = "manager-7" } })).Should().BeEmpty();

    [Fact]
    public void AReceiptOutsideTraining_NeedsNoManagerId()
        => Errors(Sale()).Should().BeEmpty();

    // Void (flag 0x0004_0000): linked to its original, contract §1.1 cbPreviousReceiptReference (LNE Ex. 4).

    [Fact]
    public void AVoidWithoutPreviousReceiptReference_IsRefused()
        => Errors(Sale(0x0004_0001)).Should().Contain("VoidMissingPreviousReceiptReference");

    [Fact]
    public void AVoidWithPreviousReceiptReference_IsTaken()
    {
        var request = Sale(0x0004_0001);
        request.cbPreviousReceiptReference = "receipt-1";
        Errors(request).Should().NotContain("VoidMissingPreviousReceiptReference");
    }

    // Charge-item description (LNE Ex. 3: a code alone is not enough): the shared MandatoryFields rule.

    [Fact]
    public void AnEmptyChargeItemDescription_IsRefused()
    {
        var request = Sale();
        request.cbChargeItems[0].Description = " ";
        Validator(InfoCert).ValidateAsync(request, Queue).Result.Errors.Should().Contain(x => x.PropertyName.EndsWith("Description"));
    }

    // Invoice customer, market-fr#140 with the 2026-10-26 amendment: InfoCert SCU only.

    [Theory]
    [InlineData(0x1000)]
    [InlineData(0x1001)]
    [InlineData(0x1002)]
    [InlineData(0x1003)]
    public void AnInfoCertInvoiceWithoutCustomer_IsRefused(long receiptCase)
        => Errors(Sale(receiptCase), InfoCert).Should().Contain("CustomerRequiredForInvoice");

    [Fact]
    public void AnInfoCertInvoice_NeedsTheR12ClientBlockAndTheCustomerNumber()
        => Errors(Sale(0x1001, customer: new { CustomerName = "Jean Dupont" }), InfoCert).Should()
            .Contain(["CustomerIdMissing", "CustomerStreetMissing", "CustomerZipMissing", "CustomerCityMissing", "CustomerCountryMissing"])
            .And.NotContain("CustomerNameMissing");

    [Fact]
    public void AnInfoCertB2CInvoiceWithTheClientBlock_IsTaken()
        => Errors(Sale(0x1001, customer: FullCustomer()), InfoCert).Should().BeEmpty();

    [Fact]
    public void AnInfoCertInvoice_RefusesACountryThatIsNotAnIsoAlpha2Code()
        => Errors(Sale(0x1001, customer: FullCustomer(country: "France")), InfoCert).Should().Contain("CustomerCountryInvalid");

    [Theory]
    [InlineData(0x1002)]
    [InlineData(0x1003)]
    public void AnInfoCertB2BOrB2GInvoice_NeedsTheCustomerVatId(long receiptCase)
        => Errors(Sale(receiptCase, customer: FullCustomer()), InfoCert).Should().Contain("CustomerVATIdMissing");

    [Fact]
    public void AnInfoCertInvoiceToACustomerWithATaxId_IsB2B_AndNeedsTheVatId()
        => Errors(Sale(0x1000, customer: FullCustomer(taxId: "DE-123")), InfoCert).Should().Contain("CustomerVATIdMissing");

    [Fact]
    public void AnInfoCertInvoiceToAFrenchBusiness_NeedsTheSiren()
        => Errors(Sale(0x1002, customer: FullCustomer(country: "FR", vatId: "FR40123456789")), InfoCert).Should().Contain("CustomerTaxIdMissing");

    [Theory]
    [InlineData("123456789")]
    [InlineData("12345678900012")]
    public void AnInfoCertInvoiceToAFrenchBusiness_TakesASirenOrASiret(string taxId)
        => Errors(Sale(0x1002, customer: FullCustomer(country: "FR", vatId: "FR40123456789", taxId: taxId)), InfoCert).Should().BeEmpty();

    [Theory]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    [InlineData("12345678A")]
    public void AnInfoCertInvoiceToAFrenchBusiness_RefusesAnythingElse(string taxId)
        => Errors(Sale(0x1002, customer: FullCustomer(country: "FR", vatId: "FR40123456789", taxId: taxId)), InfoCert).Should().Contain("CustomerTaxIdInvalid");

    [Fact]
    public void AForeignBusiness_NeedsNoSiren()
        => Errors(Sale(0x1002, customer: FullCustomer(country: "DE", vatId: "DE123456789")), InfoCert).Should().BeEmpty();

    [Theory]
    [InlineData(Lne)]
    [InlineData(null)]
    public void AnInvoiceWithAnotherScu_NeedsNoCustomer(string? certificationBody)
        => Errors(Sale(0x1002), certificationBody).Should().BeEmpty();

    [Fact]
    public void AnInfoCertTicket_NeedsNoCustomer()
        => Errors(Sale(0x0001), InfoCert).Should().BeEmpty();

    // Operator (cbUser): InfoCert M on T and I (R13 ENC-TIK-OPS-NID, R12 FAC-OPS-NID); LNE optional (D3).

    [Theory]
    [InlineData(0x0001)]
    [InlineData(0x1001)]
    public void AnInfoCertTicketOrInvoiceWithoutOperator_IsRefused(long receiptCase)
    {
        var request = Sale(receiptCase, customer: FullCustomer());
        request.cbUser = null;
        Errors(request, InfoCert).Should().Contain("UserRequired");
    }

    [Fact]
    public void AnLneTicketWithoutOperator_IsTaken()
    {
        var request = Sale();
        request.cbUser = null;
        Errors(request, Lne).Should().BeEmpty();
    }

    // Terminal (cbTerminalID): LNE M on A and L (Ex. 15); InfoCert allows it empty there.

    [Theory]
    [InlineData(0x3011)]
    [InlineData(0x3001)]
    [InlineData(0x3002)]
    public void AnLneArchiveOrLogReceiptWithoutTerminal_IsRefused(long receiptCase)
    {
        var request = Sale(receiptCase);
        request.cbChargeItems = [];
        request.cbPayItems = [];
        request.cbTerminalID = null;
        Errors(request, Lne).Should().Contain("TerminalIdRequired");
        Errors(request, InfoCert).Should().NotContain("TerminalIdRequired");
    }

    // Copy reason (R13 ENC-SIG-MOTIF, NF525 V2.3 §6.3.3.5): InfoCert M, LNE C (contract §1.2 Copy chain).

    private static ReceiptRequest CopyOf(string reference, object? caseData = null)
    {
        var request = Sale(0x3010, caseData);
        request.cbChargeItems = [];
        request.cbPayItems = [];
        request.cbPreviousReceiptReference = reference;
        return request;
    }

    [Fact]
    public void AnInfoCertCopyWithoutReason_IsRefused()
        => Errors(CopyOf("T-1"), InfoCert).Should().Contain("CopyReasonRequired");

    [Fact]
    public void AnInfoCertCopyWithAReason_IsTaken()
        => Errors(CopyOf("T-1", new { FR = new { CopyReason = "ticket perdu" } }), InfoCert).Should().NotContain("CopyReasonRequired");

    [Theory]
    [InlineData(Lne)]
    [InlineData(null)]
    public void ACopyWithAnotherScu_NeedsNoReason(string? certificationBody)
        => Errors(CopyOf("T-1"), certificationBody).Should().NotContain("CopyReasonRequired");

    // The validator asks the SCU which body certified it.

    [Fact]
    public async Task TheValidator_AppliesTheRulesOfItsScu()
    {
        var invoice = Sale(0x1002);

        (await Validator(InfoCert).ValidateAsync(invoice, Queue)).Errors.Should().Contain(x => x.ErrorCode == "CustomerRequiredForInvoice");
        (await Validator(Lne).ValidateAsync(invoice, Queue)).Errors.Should().NotContain(x => x.ErrorCode == "CustomerRequiredForInvoice");
    }

    private static readonly fiskaltrust.storage.V0.ftQueue Queue = new() { CountryCode = "FR" };

    private static ReceiptValidatorFR Validator(string certificationBody) => new(
        new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(new InMemoryQueueItemRepository()))),
        () => Task.FromResult<FRSSCDInfo?>(new FRSSCDInfo { InfoData = $"{{\"CertificationBody\":\"{certificationBody}\"}}" }));
}
