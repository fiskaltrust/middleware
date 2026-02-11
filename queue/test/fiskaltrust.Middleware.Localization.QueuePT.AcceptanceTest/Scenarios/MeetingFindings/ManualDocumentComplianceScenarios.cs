using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios.MeetingFindings;

public class ManualDocumentComplianceScenarios : Scenarios.AbstractScenarioTests
{
    [Fact]
    public async Task Scenario1_ManualInvoiceExport_ShouldSetSourceBillingHashControlAndDates()
    {
        var manualInvoice = """
            {
              "cbReceiptReference": "manual-abc-1",
              "cbReceiptMoment": "2024-05-04T11:30:00Z",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "Manual item",
                  "Amount": 50,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 50,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "{{cashboxid}}",
              "ftReceiptCase": 5788286605450547201,
              "ftReceiptCaseData": {
                "PT": {
                  "Series": "ABC",
                  "Number": 1
                }
              },
              "cbUser": "Stefan Kert"
            }
            """;

        var (_, response) = await ProcessReceiptAsync(manualInvoice);
        response.ftState.State().Should().Be(State.Success);
        response.ftSignatures.Should().Contain(s => s.ftSignatureType.IsType(SignatureTypePT.Hash));
        response.ftSignatures.Should().Contain(s =>
            s.ftSignatureType.IsType(SignatureTypePT.PTAdditional) &&
            s.ftSignatureFormat == SignatureFormat.Text &&
            s.Data == "Cópia do documento original - FTM ABC/0001");

        var auditFile = await ExportAuditFileAsync();
        var invoiceNo = response.ftReceiptIdentification.Split("#").Last();
        var invoice = auditFile.SourceDocuments.SalesInvoices.Invoice.Single(i => i.InvoiceNo == invoiceNo);

        invoice.DocumentStatus.SourceBilling.Should().Be("M");
        invoice.HashControl.Should().Be("1-FTM ABC/0001");
        invoice.HashControl.Should().NotContain("FT M");

        invoice.InvoiceDate.ToString("yyyy-MM-dd").Should().Be("2024-05-04");
        invoice.SystemEntryDate.Should().BeAfter(invoice.InvoiceDate);
    }

    [Fact]
    public async Task Scenario2_Export_ShouldTrimProductAndCustomerNames_AndDefaultCountry()
    {
        var invoiceJson = """
            {
              "cbReceiptReference": "trim-check-1",
              "cbReceiptMoment": "{{$isoTimestamp}}",
              "cbChargeItems": [
                {
                  "Quantity": 1,
                  "Description": "  Product With Spaces  ",
                  "Amount": 12.30,
                  "VATRate": 23,
                  "ftChargeItemCase": 5788286605450018835
                }
              ],
              "cbPayItems": [
                {
                  "Description": "Numerario",
                  "Amount": 12.30,
                  "ftPayItemCase": 5788286605450018817
                }
              ],
              "ftCashBoxID": "{{cashboxid}}",
              "ftReceiptCase": 5788286605450022913,
              "cbUser": "Stefan Kert",
              "cbCustomer": {
                "CustomerName": "  Cliente Com Espacos  ",
                "CustomerStreet": " Rua 1 ",
                "CustomerZip": " 1000-000 ",
                "CustomerCity": " Lisboa ",
                "CustomerVATId": "999999990"
              }
            }
            """;

        var (_, response) = await ProcessReceiptAsync(invoiceJson);
        response.ftState.State().Should().Be(State.Success);
        response.ftSignatures.Should().Contain(s => s.ftSignatureType.IsType(SignatureTypePT.Hash));
        response.ftSignatures.Should().Contain(s => s.ftSignatureType.IsType(SignatureTypePT.ATCUD));
        response.ftSignatures.Should().Contain(s => s.ftSignatureFormat == SignatureFormat.QRCode);
        response.ftSignatures.Should().NotContain(s => s.Data.StartsWith("Cópia do documento original - FTM "));

        var auditFile = await ExportAuditFileAsync();
        auditFile.MasterFiles.Product.Should().Contain(p => p.ProductDescription == "Product With Spaces");
        auditFile.MasterFiles.Customer.Should().Contain(c =>
            c.CompanyName == "Cliente Com Espacos" &&
            c.BillingAddress.Country == "Desconhecido");
    }

    private async Task<AuditFile> ExportAuditFileAsync()
    {
        var xmlData = await ExecuteJournal(new JournalRequest
        {
            From = DateTime.Parse("2020-01-01T00:00:00Z").Ticks,
            To = DateTime.Parse("2030-01-01T00:00:00Z").Ticks,
            ftJournalType = (JournalType) 0x5054_2000_0000_0001
        });

        using var stream = new MemoryStream(xmlData);
        var serializer = new XmlSerializer(typeof(AuditFile));
        return (AuditFile) serializer.Deserialize(stream)!;
    }
}
