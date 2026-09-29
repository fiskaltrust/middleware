using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// a classification override on the ordinary line(s) of an invoice that ALSO carries a
/// fee line (environmental / plastic-bag fee, type-of-service 0xF0, recType 2).
///
/// AADE forbids an incomeClassification on a fee line (error 231 "incomeClassification is forbidden
/// for invoice detail N"), so the caller must leave the fee line unclassified. Before the fix the
/// middleware's all-or-nothing consistency check counted the fee line too and threw
/// "…every charge item must have a classification override", making the request impossible to satisfy.
/// </summary>
public class Issue302OverrideOnFeeLineTests
{
    private const string PlasticBagFeeDescription =
        "Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο";

    private static AADEFactory CreateFactory() =>
        new(new MasterDataConfiguration
        {
            Account = new AccountMasterData { VatId = "098000979" },
            Outlet = new OutletMasterData { LocationId = "0" }
        }, "https://receipts.example.com");

    private static object IncomeClassificationOverride(string type, string category) =>
        new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoicedetails = new
                    {
                        incomeClassification = new[]
                        {
                            new { classificationType = type, classificationCategory = category }
                        }
                    }
                }
            }
        };

    private static ReceiptRequest CreateRequest(object? feeLineCaseData)
    {
        return new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "T001",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = new DateTime(2025, 6, 18, 10, 44, 19, DateTimeKind.Utc),
            Currency = Currency.EUR,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "112545020",
                CustomerName = "PRIVESHOP EPE",
                CustomerCountry = "GR"
            },
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
            cbChargeItems =
            [
                // Ordinary line — carries an incomeClassification override.
                new ChargeItem
                {
                    Position = 1,
                    Quantity = 1,
                    Description = "ΓΕΝΙΚΕΣ ΧΡΕΩΣΕΙΣ",
                    Amount = 124m,
                    VATRate = 24,
                    VATAmount = 24m,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                    ftChargeItemCaseData = IncomeClassificationOverride("E3_561_001", "category1_1")
                },
                // Fee line — environmental / plastic-bag fee (recType 2). AADE forbids a classification here.
                new ChargeItem
                {
                    Position = 2,
                    Quantity = 1,
                    Description = PlasticBagFeeDescription,
                    Amount = 0.09m,
                    VATRate = 24,
                    VATAmount = 0.02m,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_00F3,
                    ftChargeItemCaseData = feeLineCaseData
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Description = "ΜΕΤΡΗΤΑ",
                    Amount = 124.09m,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
                }
            ]
        };
    }

    private static ReceiptResponse CreateResponse(ReceiptRequest request) =>
        new()
        {
            cbReceiptReference = request.cbReceiptReference,
            ftReceiptIdentification = "ft123456789",
            ftCashBoxIdentification = "CB001",
            ftState = (State) 0x4752000000000000,
            ftSignatures = []
        };

    [Fact]
    public void MapToInvoicesDoc_ClassifiedLine_WithUnclassifiedFeeLine_IsAccepted()
    {
        // Fee line carries NO override
        var factory = CreateFactory();
        var request = CreateRequest(feeLineCaseData: null);
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull("a fee line must not force the all-or-nothing classification rule (issue #302)");
        doc.Should().NotBeNull();

        var rows = doc!.invoice[0].invoiceDetails;
        rows.Should().HaveCount(2);

        var ordinaryRow = rows.Single(r => !(r.recTypeSpecified && r.recType == 2));
        ordinaryRow.incomeClassification.Should().NotBeNull();
        ordinaryRow.incomeClassification[0].classificationType.Should().Be(IncomeClassificationValueType.E3_561_001);
        ordinaryRow.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_1);

        var feeRow = rows.Single(r => r.recTypeSpecified && r.recType == 2);
        feeRow.feesPercentCategorySpecified.Should().BeTrue();
        feeRow.feesPercentCategory.Should().Be(8, "the plastic-bag environmental fee maps to fee code 8");
        feeRow.incomeClassification.Should().BeNull("AADE forbids an incomeClassification on a fee line (error 231)");
    }

    [Fact]
    public void MapToInvoicesDoc_IncomeClassificationOverride_OnFeeLine_IsIgnored()
    {
        // Even if the caller mistakenly puts an incomeClassification on the fee line, it must not be emitted.
        var factory = CreateFactory();
        var request = CreateRequest(feeLineCaseData: IncomeClassificationOverride("E3_561_001", "category1_1"));
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();

        var feeRow = doc!.invoice[0].invoiceDetails.Single(r => r.recTypeSpecified && r.recType == 2);
        feeRow.incomeClassification.Should().BeNull("an override must not re-introduce the AADE-forbidden classification on a fee line");
    }
}
