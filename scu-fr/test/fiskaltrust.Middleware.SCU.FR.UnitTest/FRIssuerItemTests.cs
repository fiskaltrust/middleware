using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// The issuer item (market-fr#221, ADR 0021): the seller's identity from the SCU configuration, printed after
/// the header of notes, tickets, payment proofs and invoices, one labelled line per configured value, not signed.
/// </summary>
public class FRIssuerItemTests
{
    private static readonly FRIssuer Full = new(
        Siret: "12345678901234", Name: "Café de la Gare", NameType: "Enseigne", Address: "1 place de la Gare", PostalCode: "75010",
        City: "Paris", Country: "FR", VatNumber: "FR12123456789", Rcs: "RCS Paris B 123456789", Naf: "5630Z",
        LegalForm: "SAS", ShareCapital: "10000 EUR", VatOption: "receipts");

    [Fact]
    public void Data_PrintsEveryValueUnderItsLabel_InTheFixedOrder()
    {
        FRIssuerItem.Data(Full).Should().Be(string.Join("\n",
            "SIRET : 12345678901234",
            "SIREN : 123456789",
            "Dénomination : Café de la Gare",
            "Type de dénomination : Enseigne",
            "Adresse : 1 place de la Gare",
            "Code postal : 75010",
            "Ville : Paris",
            "Pays : FR",
            "N° TVA intracommunautaire : FR12123456789",
            "RCS : RCS Paris B 123456789",
            "NAF : 5630Z",
            "Forme juridique : SAS",
            "Capital social : 10000 EUR",
            "Option TVA : receipts"));
    }

    [Fact]
    public void Data_LeavesOutWhatIsNotConfigured_AndFoldsLineBreaks()
    {
        var issuer = new FRIssuer("12345678901234", null, null, "1 place\nde la Gare", null, null, null, null, null, null, null, null, null);

        FRIssuerItem.Data(issuer).Should().Be("SIRET : 12345678901234\nSIREN : 123456789\nAdresse : 1 place de la Gare");
        FRIssuerItem.Data(new FRIssuer(null, null, null, null, null, null, null, null, null, null, null, null, null)).Should().BeNull();
    }

    [Fact]
    public void Parse_GivesBackTheValuesByLabel()
    {
        var values = FRIssuerItem.Parse(FRIssuerItem.Data(Full));

        values.Should().HaveCount(14);
        values["Capital social"].Should().Be("10000 EUR");
        values["RCS"].Should().Be("RCS Paris B 123456789", "a value may contain ' : ' only after the first separator");
    }

    [Theory]
    [InlineData(0x0001, true)]
    [InlineData(0x0002, true)]
    [InlineData(0x0006, true)]
    [InlineData(0x1002, true)]
    [InlineData(0x3005, true)]
    [InlineData(0x2011, false)]
    [InlineData(0x3010, false)]
    [InlineData(0x3011, false)]
    [InlineData(0x3001, false)]
    [InlineData(0x2000, false)]
    public void IsDocument_CoversNotesTicketsPaymentProofsAndInvoices(long receiptCase, bool expected)
        => FRIssuerItem.IsDocument(((ReceiptCase) receiptCase).WithCountry("FR")).Should().Be(expected);

    [Fact]
    public async Task InfocertScu_PrintsTheIssuerAfterTheHeaderOfATicket_AndNotOnAClosing()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(new InfoCertConfiguration
        {
            Siret = FRTestData.Siret,
            PrivateKey = privateKey,
            CertificateSerialNumber = "CERT-4711",
            IssuerName = "Café de la Gare",
            ShareCapital = "10000 EUR",
        });
        var closing = FRTestData.Request();
        closing.ReceiptRequest.ftReceiptCase = ((ReceiptCase) 0x2011).WithCountry("FR");

        var (ticket, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);
        var (closed, _) = await sut.ProcessReceiptAsync(closing, null);

        var item = ticket.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Issuer));
        item.Caption.Should().Be("Émetteur");
        item.Data.Should().Be($"SIRET : {FRTestData.Siret}\nSIREN : 123456789\nDénomination : Café de la Gare\nCapital social : 10000 EUR");
        ((long) item.ftSignatureFormat & 0xFFFF_0000).Should().Be((long) SignatureFormatPosition.AfterHeader);
        ((long) item.ftSignatureFormat & 0xFFFF).Should().Be((long) SignatureFormat.Text);
        FRJws.Decode(ticket.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data).SignedString
            .Should().NotContain("Café de la Gare", "the issuer item is not signed");
        closed.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.Issuer));
    }
}
