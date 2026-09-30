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
/// AADE forbids income/expenses classifications on a fee line (error 231 "incomeClassification is forbidden
/// for invoice detail N"), so the caller must leave the fee line unclassified; an override there is rejected. Before the fix the
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

    private static object ExpensesClassificationOverride(string type, string category) =>
        new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoicedetails = new
                    {
                        expensesClassification = new[]
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

    [Theory]
    [InlineData("income")]
    [InlineData("expenses")]
    public void MapToInvoicesDoc_ClassificationOverride_OnFeeLine_IsRejected(string kind)
    {
        // AADE forbids income and expenses classifications on a fee line, so a caller-supplied one is rejected
        // with a clear message instead of being silently dropped.
        var factory = CreateFactory();
        var feeLineCaseData = kind == "income"
            ? IncomeClassificationOverride("E3_561_001", "category1_1")
            : ExpensesClassificationOverride("E3_102_001", "category2_1");
        var request = CreateRequest(feeLineCaseData);
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        doc.Should().BeNull();
        error.Should().NotBeNull();
        error!.Exception.Should().BeOfType<ArgumentException>()
            .Which.Message.Should().Contain("not allowed on special-tax/fee charge items").And.Contain("position 2");
    }

    [Fact]
    public void MapToInvoicesDoc_ClassificationOverride_OnFeeLine_WithRecTypeOverride_IsRejected()
    {
        // Fee-ness is decided by the charge item (type of service 0xF0), not by the row's recType,
        // so overriding recType on the fee line must not let a classification through.
        var factory = CreateFactory();
        var request = CreateRequest(feeLineCaseData: new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoicedetails = new
                    {
                        recType = 1,
                        incomeClassification = new[]
                        {
                            new { classificationType = "E3_561_001", classificationCategory = "category1_1" }
                        }
                    }
                }
            }
        });
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        doc.Should().BeNull();
        error!.Exception.Should().BeOfType<ArgumentException>()
            .Which.Message.Should().Contain("not allowed on special-tax/fee charge items");
    }

    [Fact]
    public void MapToInvoicesDoc_SecondOrdinaryLineWithoutOverride_WithFeeLine_IsRejected()
    {
        // Excluding fee lines must not relax the all-or-nothing rule for ordinary lines.
        var factory = CreateFactory();
        var request = CreateRequest(feeLineCaseData: null);
        request.cbChargeItems.Add(new ChargeItem
        {
            Position = 3,
            Quantity = 1,
            Description = "ΕΠΙΠΛΕΟΝ ΧΡΕΩΣΗ",
            Amount = 12.4m,
            VATRate = 24,
            VATAmount = 2.4m,
            ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013
        });
        request.cbPayItems[0].Amount += 12.4m;
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        doc.Should().BeNull();
        error.Should().NotBeNull();
        error!.Exception.Should().BeOfType<ArgumentException>()
            .Which.Message.Should().Contain("must have a classification override");
    }

    private static ChargeItem OrdinaryItemWithRecType2Override(bool withClassification) => new()
    {
        Position = 3,
        Quantity = 1,
        Description = "ΕΠΙΠΛΕΟΝ ΧΡΕΩΣΗ",
        Amount = 12.4m,
        VATRate = 24,
        VATAmount = 2.4m,
        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
        ftChargeItemCaseData = withClassification
            ? new
            {
                GR = new
                {
                    mydataoverride = new
                    {
                        invoicedetails = new
                        {
                            recType = 2,
                            incomeClassification = new[]
                            {
                                new { classificationType = "E3_561_001", classificationCategory = "category1_1" }
                            }
                        }
                    }
                }
            }
            : new { GR = new { mydataoverride = new { invoicedetails = new { recType = 2 } } } }
    };

    [Fact]
    public void MapToInvoicesDoc_ClassificationOverride_OnOrdinaryLineOverriddenToRecType2_IsRejected()
    {
        // An ordinary item whose override turns it into a fee row (recType 2) is a fee line too,
        // so a classification override on it is rejected just like on a 0xF0 item.
        var factory = CreateFactory();
        var request = CreateRequest(feeLineCaseData: null);
        request.cbChargeItems.Add(OrdinaryItemWithRecType2Override(withClassification: true));
        request.cbPayItems[0].Amount += 12.4m;
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        doc.Should().BeNull();
        error!.Exception.Should().BeOfType<ArgumentException>()
            .Which.Message.Should().Contain("not allowed on special-tax/fee charge items").And.Contain("position 3");
    }

    [Fact]
    public void MapToInvoicesDoc_OrdinaryLineOverriddenToRecType2_WithoutClassification_IsAcceptedAndUnclassified()
    {
        // The recType-2 line is exempt from the all-or-nothing rule, and its auto-generated
        // classification is stripped so AADE does not reject it with error 231.
        var factory = CreateFactory();
        var request = CreateRequest(feeLineCaseData: null);
        request.cbChargeItems.Add(OrdinaryItemWithRecType2Override(withClassification: false));
        request.cbPayItems[0].Amount += 12.4m;
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var row = doc!.invoice[0].invoiceDetails.Single(r => r.lineNumber == 3);
        row.recType.Should().Be(2);
        row.incomeClassification.Should().BeNull();
        row.expensesClassification.Should().BeNull();
    }
}
