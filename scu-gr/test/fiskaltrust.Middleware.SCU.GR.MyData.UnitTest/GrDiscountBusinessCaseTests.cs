using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// Test-first reproduction for market-gr#103 (entire-sum discount) and market-gr#110
/// (100% line discount). Each test mirrors one example from the businesscase folder
/// example/SignRequestReceipt_GR_Discounts (merged, verified on the myDATA dev sandbox
/// 2026-07-03).
///
/// Shared root cause: AADEFactory maps an ExtraOrDiscount modifier to deductionsAmount
/// and leaves netValue at the pre-discount value. deductionsAmount is a statutory
/// RETENTION, not a discount — so partial discounts are accepted but overstate income,
/// and 100% discounts are rejected by AADE rule 241 (deduction must be &lt;= line net).
///
/// Target after the fix: fold the modifier into netValue/vatAmount, set discountOption,
/// emit NO deductionsAmount, and for a resulting zero-net line on a 1.x invoice emit
/// recType=6 with no incomeClassification (AADE rule 222).
///
/// These tests are RED against current committed code, except the retail-zero regression
/// guard (Case110_Retail_*) which is GREEN today and must stay green.
/// </summary>
public class GrDiscountBusinessCaseTests
{
    #region Helpers
    private static AADEFactory CreateFactory() =>
        new(new MasterDataConfiguration
        {
            Account = new AccountMasterData { VatId = "112545020" },
            Outlet = new OutletMasterData { LocationId = "0" }
        }, "https://receipts.example.com");

    private static ReceiptResponse CreateResponse(ReceiptRequest request) => new()
    {
        cbReceiptReference = request.cbReceiptReference,
        ftReceiptIdentification = "ft123ABC#",
        ftCashBoxIdentification = "TEST-001"
    };

    private static readonly object GrCustomer = new
    {
        CustomerVATId = "EL026883248",
        CustomerName = "Πελάτης A.E.",
        CustomerCountry = "GR"
    };

    private static ChargeItemCase VatBracketFor(decimal vatRate) => vatRate switch
    {
        24m => ChargeItemCase.NormalVatRate,
        13m => ChargeItemCase.DiscountedVatRate1,
        6m => ChargeItemCase.DiscountedVatRate1,
        _ => throw new ArgumentOutOfRangeException(nameof(vatRate),
                $"Unsupported VAT rate {vatRate} for these fixtures.")
    };

    // Good: TypeOfService = Delivery (0x_1) — matches businesscase 0x..._0013 (24%) / 0x..._0011 (reduced).
    private static ChargeItem Good(decimal position, decimal grossAmount, decimal vatRate = 24m, string description = "Item") => new()
    {
        Position = position,
        Quantity = 1,
        Description = description,
        Amount = grossAmount,
        VATRate = vatRate,
        ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
            .WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            .WithVat(VatBracketFor(vatRate))
    };

    // Per-line discount modifier carrying the parent line's own VAT rate
    // (businesscase 0x..._0004_0013 / 0x..._0004_0011).
    private static ChargeItem DiscountModifier(decimal position, decimal grossAmount, decimal vatRate = 24m, string description = "Discount") => new()
    {
        Position = position,
        Quantity = 1,
        Description = description,
        Amount = grossAmount,
        VATRate = vatRate,
        ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
            .WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            .WithVat(VatBracketFor(vatRate))
            .WithFlag(ChargeItemCaseFlags.ExtraOrDiscount)
    };

    // Rate-agnostic whole-basket discount: ExtraOrDiscount + Delivery + Unknown VAT (nibble 0),
    // VATRate 0, Position 0 — businesscase 0x0000_2000_0004_0010. The middleware is expected to
    // distribute this across all base lines by gross (feature not yet implemented).
    private static ChargeItem BasketDiscount(decimal grossAmount, string description = "Whole-basket discount") => new()
    {
        Position = 0,
        Quantity = 1,
        Description = description,
        Amount = grossAmount,
        VATRate = 0,
        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0004_0010
    };

    private static PayItem Cash(decimal amount) => new()
    {
        Position = 1,
        Description = "Cash",
        Amount = amount,
        ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
    };

    private static ReceiptRequest B2C(string reference, IEnumerable<ChargeItem> items, decimal paid) => new()
    {
        cbTerminalID = "1",
        Currency = Currency.EUR,
        cbReceiptMoment = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc),
        cbReceiptReference = reference,
        ftPosSystemId = Guid.NewGuid(),
        ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
        cbChargeItems = items.ToList(),
        cbPayItems = new List<PayItem> { Cash(paid) }
    };

    private static ReceiptRequest B2B(string reference, IEnumerable<ChargeItem> items, decimal paid) => new()
    {
        cbTerminalID = "1",
        Currency = Currency.EUR,
        cbReceiptMoment = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc),
        cbReceiptReference = reference,
        ftPosSystemId = Guid.NewGuid(),
        ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002),
        cbCustomer = GrCustomer,
        cbChargeItems = items.ToList(),
        cbPayItems = new List<PayItem> { Cash(paid) }
    };
    #endregion

    // ===== #103 — entire-sum discount, MANUAL per-line entry point =====

    [Fact] // businesscase 103_A
    public void Case103A_PerLineDiscount_Uniform_B2C_FoldsIntoNetNoDeductions()
    {
        var request = B2C("GR-103-A", new[]
        {
            Good(1.0m, 10m, 24m, "Item 1"),
            DiscountModifier(1.1m, -5.45m, 24m, "Basket discount (share)"),
            Good(2.0m, 5m, 24m, "Item 2"),
            DiscountModifier(2.1m, -2.73m, 24m, "Basket discount (share)"),
            Good(3.0m, 7m, 24m, "Item 3"),
            DiscountModifier(3.1m, -3.82m, 24m, "Basket discount (share)")
        }, paid: 10m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var invoice = doc!.invoice[0];
        invoice.invoiceDetails.Should().HaveCount(3);

        AssertFoldedLine(invoice.invoiceDetails[0], expectedNet: 3.67m, expectedVat: 0.88m);
        AssertFoldedLine(invoice.invoiceDetails[1], expectedNet: 1.83m, expectedVat: 0.44m);
        AssertFoldedLine(invoice.invoiceDetails[2], expectedNet: 2.56m, expectedVat: 0.62m);

        invoice.invoiceSummary.totalNetValue.Should().Be(8.06m);
        invoice.invoiceSummary.totalVatAmount.Should().Be(1.94m);
        invoice.invoiceSummary.totalDeductionsAmount.Should().Be(0m);
        invoice.invoiceSummary.totalGrossValue.Should().Be(10.00m);
    }

    [Fact] // businesscase 103_D — mixed VAT 24/13/6, pre-split per line
    public void Case103D_PerLineDiscount_MixedVat_B2C_FoldsWithCorrectCategories()
    {
        var request = B2C("GR-103-D", new[]
        {
            Good(1.0m, 25m, 24m, "Item @24%"),
            DiscountModifier(1.1m, -12.5m, 24m, "Basket discount 24% part"),
            Good(2.0m, 15m, 13m, "Item @13%"),
            DiscountModifier(2.1m, -7.5m, 13m, "Basket discount 13% part"),
            Good(3.0m, 10m, 6m, "Item @6%"),
            DiscountModifier(3.1m, -5m, 6m, "Basket discount 6% part")
        }, paid: 25m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var details = doc!.invoice[0].invoiceDetails;
        details.Should().HaveCount(3);

        AssertFoldedLine(details[0], expectedNet: 10.08m, expectedVat: 2.42m);
        details[0].vatCategory.Should().Be(1, "24% → myDATA vatCategory 1");
        AssertFoldedLine(details[1], expectedNet: 6.64m, expectedVat: 0.86m);
        details[1].vatCategory.Should().Be(2, "13% → myDATA vatCategory 2");
        AssertFoldedLine(details[2], expectedNet: 4.72m, expectedVat: 0.28m);
        details[2].vatCategory.Should().Be(3, "6% → myDATA vatCategory 3");

        doc.invoice[0].invoiceSummary.totalDeductionsAmount.Should().Be(0m);
        doc.invoice[0].invoiceSummary.totalGrossValue.Should().Be(25.00m);
    }

    [Fact] // businesscase 103_E — same basket as 103_A but as a 1.1 B2B invoice
    public void Case103E_PerLineDiscount_Uniform_B2B_FoldsIntoNet()
    {
        var request = B2B("GR-103-E", new[]
        {
            Good(1.0m, 10m, 24m, "Item 1"),
            DiscountModifier(1.1m, -5.45m, 24m, "Basket discount (share)"),
            Good(2.0m, 5m, 24m, "Item 2"),
            DiscountModifier(2.1m, -2.73m, 24m, "Basket discount (share)"),
            Good(3.0m, 7m, 24m, "Item 3"),
            DiscountModifier(3.1m, -3.82m, 24m, "Basket discount (share)")
        }, paid: 10m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var invoice = doc!.invoice[0];
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item11);
        invoice.invoiceDetails.Should().HaveCount(3);
        AssertFoldedLine(invoice.invoiceDetails[0], expectedNet: 3.67m, expectedVat: 0.88m);
        AssertFoldedLine(invoice.invoiceDetails[1], expectedNet: 1.83m, expectedVat: 0.44m);
        AssertFoldedLine(invoice.invoiceDetails[2], expectedNet: 2.56m, expectedVat: 0.62m);
        invoice.invoiceSummary.totalDeductionsAmount.Should().Be(0m);
        invoice.invoiceSummary.totalGrossValue.Should().Be(10.00m);
    }

    // ===== #103 — entire-sum discount, ABSTRACTED basket entry point (NEW feature) =====

    [Fact] // businesscase 103_F — one rate-agnostic basket discount, uniform VAT
    public void Case103F_BasketDiscount_Uniform_B2C_DistributesByGross()
    {
        var request = B2C("GR-103-F", new[]
        {
            Good(1.0m, 10m, 24m, "Item 1"),
            Good(2.0m, 5m, 24m, "Item 2"),
            Good(3.0m, 7m, 24m, "Item 3"),
            BasketDiscount(-12m)
        }, paid: 10m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var invoice = doc!.invoice[0];
        invoice.invoiceDetails.Should().HaveCount(3, "the basket discount is distributed, not emitted as its own line");

        // 12 distributed by gross over 22 → 5.45 / 2.73 / 3.82, then folded per line.
        AssertFoldedLine(invoice.invoiceDetails[0], expectedNet: 3.67m, expectedVat: 0.88m);
        AssertFoldedLine(invoice.invoiceDetails[1], expectedNet: 1.83m, expectedVat: 0.44m);
        AssertFoldedLine(invoice.invoiceDetails[2], expectedNet: 2.56m, expectedVat: 0.62m);

        invoice.invoiceSummary.totalNetValue.Should().Be(8.06m);
        invoice.invoiceSummary.totalVatAmount.Should().Be(1.94m);
        invoice.invoiceSummary.totalDeductionsAmount.Should().Be(0m);
        invoice.invoiceSummary.totalGrossValue.Should().Be(10.00m);
    }

    [Fact] // businesscase 103_G — one rate-agnostic basket discount, mixed VAT
    public void Case103G_BasketDiscount_MixedVat_B2C_DistributesByGross()
    {
        var request = B2C("GR-103-G", new[]
        {
            Good(1.0m, 25m, 24m, "Item @24%"),
            Good(2.0m, 15m, 13m, "Item @13%"),
            Good(3.0m, 10m, 6m, "Item @6%"),
            BasketDiscount(-25m)
        }, paid: 25m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var details = doc!.invoice[0].invoiceDetails;
        details.Should().HaveCount(3);

        // 25 distributed by gross over 50 → 12.50 / 7.50 / 5.00, folded at each line's own rate.
        AssertFoldedLine(details[0], expectedNet: 10.08m, expectedVat: 2.42m);
        details[0].vatCategory.Should().Be(1);
        AssertFoldedLine(details[1], expectedNet: 6.64m, expectedVat: 0.86m);
        details[1].vatCategory.Should().Be(2);
        AssertFoldedLine(details[2], expectedNet: 4.72m, expectedVat: 0.28m);
        details[2].vatCategory.Should().Be(3);

        doc.invoice[0].invoiceSummary.totalDeductionsAmount.Should().Be(0m);
        doc.invoice[0].invoiceSummary.totalGrossValue.Should().Be(25.00m);
    }

    [Fact] // businesscase 103_C — anti-pattern: single trailing discount larger than its line.
    // rejects with rule 241 (deduction/negative net). The sanctioned path is the basket discount (103_F).
    public void Case103C_TrailingDiscountExceedsLine_IsRejected()
    {
        var request = B2C("GR-103-C", new[]
        {
            Good(1.0m, 10m, 24m, "Item 1"),
            Good(2.0m, 5m, 24m, "Item 2"),
            Good(3.0m, 7m, 24m, "Item 3"),
            DiscountModifier(3.1m, -12m, 24m, "Whole-basket discount (too large for line 3)")
        }, paid: 10m);

        var (_, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().NotBeNull(
            "a single-rate discount cannot exceed the line it attaches to; the middleware must reject it " +
            "instead of producing a negative/deductions line that AADE rejects with rule 241");
    }

    // ===== #110 — 100% discount / zero-value line =====

    [Fact] // businesscase 110_FullLine — B2B, line killed by a -100% modifier
    public void Case110_FullLine_B2B_EmitsRecType6ZeroLine()
    {
        var request = B2B("GR-110-FullLine", new[]
        {
            Good(1m, 124m, 24m, "Product A"),
            Good(2m, 10m, 24m, "Promo item"),
            DiscountModifier(2.1m, -10m, 24m, "100% discount on Promo item")
        }, paid: 124m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var invoice = doc!.invoice[0];
        invoice.invoiceDetails.Should().HaveCount(2);

        invoice.invoiceDetails[0].netValue.Should().Be(100m);
        invoice.invoiceDetails[0].vatAmount.Should().Be(24m);
        invoice.invoiceDetails[0].recTypeSpecified.Should().BeFalse();

        var zero = invoice.invoiceDetails[1];
        zero.netValue.Should().Be(0m);
        zero.vatAmount.Should().Be(0m);
        zero.recTypeSpecified.Should().BeTrue("rule 222 forces recType=6 on a zero-net line for 1.x");
        zero.recType.Should().Be(6);
        zero.discountOptionSpecified.Should().BeTrue();
        zero.discountOption.Should().BeTrue();
        zero.incomeClassification.Should().BeNullOrEmpty("recType=6 forbids incomeClassification");
        zero.deductionsAmountSpecified.Should().BeFalse();

        invoice.invoiceSummary.totalNetValue.Should().Be(100m);
        invoice.invoiceSummary.totalVatAmount.Should().Be(24m);
        invoice.invoiceSummary.totalDeductionsAmount.Should().Be(0m);
        invoice.invoiceSummary.totalGrossValue.Should().Be(124m);
    }

    [Fact] // businesscase 110_ZeroLine_Direct — B2B, free line sent directly as Amount 0
    public void Case110_ZeroDirect_B2B_EmitsRecType6ZeroLine()
    {
        var request = B2B("GR-110-ZeroDirect", new[]
        {
            Good(1m, 124m, 24m, "Product A"),
            Good(2m, 0m, 24m, "Free sample")
        }, paid: 124m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var zero = doc!.invoice[0].invoiceDetails[1];
        zero.netValue.Should().Be(0m);
        zero.recTypeSpecified.Should().BeTrue("a netValue=0 line on 1.x needs recType=6 (rule 222)");
        zero.recType.Should().Be(6);
        zero.incomeClassification.Should().BeNullOrEmpty();
    }

    [Fact] // businesscase 110_RetailReceipt_ZeroLine — REGRESSION GUARD
    public void Case110_Retail_ZeroLine_KeepsClassificationNoRecType()
    {
        var request = B2C("GR-110-A", new[]
        {
            Good(1m, 63.42m, 24m, "Coffee"),
            Good(2m, 0m, 24m, "Free sample")
        }, paid: 63.42m);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var invoice = doc!.invoice[0];
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item111);

        var zero = invoice.invoiceDetails[1];
        zero.netValue.Should().Be(0m);
        zero.recTypeSpecified.Should().BeFalse("retail 11.x does NOT enforce rule 222");
        zero.incomeClassification.Should().NotBeNullOrEmpty("retail zero line keeps its classification");
        zero.incomeClassification[0].classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_1);
        zero.incomeClassification[0].amount.Should().Be(0m);
    }

    // Calculation coverage

    [Theory] // arithmetic: finalGross = parentGross + modifier, vat re-derived at the line's rate, net = finalGross - vat
    [InlineData(24, 10.00, -5.45, 3.67, 0.88)]
    [InlineData(24, 124.00, -24.00, 80.65, 19.35)]
    [InlineData(24, 10.00, -9.99, 0.01, 0.00)]   // near-zero
    [InlineData(13, 15.00, -7.50, 6.64, 0.86)]
    [InlineData(13, 113.00, -13.00, 88.50, 11.50)]
    [InlineData(6, 10.00, -5.00, 4.72, 0.28)]
    public void Fold_RecomputesNetAndVat(decimal vatRate, decimal parentGross, decimal modifier, decimal expectedNet, decimal expectedVat)
    {
        var request = B2C("GR-calc-fold", new[]
        {
            Good(1.0m, parentGross, vatRate),
            DiscountModifier(1.1m, modifier, vatRate)
        }, paid: parentGross + modifier);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        AssertFoldedLine(doc!.invoice[0].invoiceDetails[0], expectedNet, expectedVat);
    }

    [Theory] // basket discount distributed by gross; second row exercises the remainder landing on the largest line
    [InlineData(10.00, 5.00, 7.00, -12.00, 3.67, 1.83, 2.56, 10.00)]
    [InlineData(10.00, 10.00, 10.00, -10.00, 5.37, 5.38, 5.38, 20.00)]
    public void BasketDiscount_DistributesByGross(decimal g1, decimal g2, decimal g3, decimal discount, decimal net1, decimal net2, decimal net3, decimal totalGross)
    {
        var request = B2C("GR-calc-basket", new[]
        {
            Good(1.0m, g1), Good(2.0m, g2), Good(3.0m, g3),
            BasketDiscount(discount)
        }, paid: g1 + g2 + g3 + discount);

        var (doc, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var d = doc!.invoice[0].invoiceDetails;
        d.Should().HaveCount(3, "the basket discount is distributed, not emitted as its own line");
        d[0].netValue.Should().Be(net1);
        d[1].netValue.Should().Be(net2);
        d[2].netValue.Should().Be(net3);
        d.Should().OnlyContain(l => !l.deductionsAmountSpecified);
        doc.invoice[0].invoiceSummary.totalGrossValue.Should().Be(totalGross);
        doc.invoice[0].invoiceSummary.totalDeductionsAmount.Should().Be(0m);
    }

    [Theory] // a basket discount larger than the basket total cannot be distributed → rejected
    [InlineData(23.00)]
    [InlineData(50.00)]
    public void BasketDiscount_ExceedsBasket_IsRejected(decimal discountAbs)
    {
        var request = B2C("GR-calc-basket-exceed", new[]
        {
            Good(1.0m, 10m), Good(2.0m, 5m), Good(3.0m, 7m),   // basket total = 22
            BasketDiscount(-discountAbs)
        }, paid: 22m - discountAbs);

        var (_, error) = CreateFactory().MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().NotBeNull("a basket discount exceeding the basket total must be rejected");
    }

    private static void AssertFoldedLine(InvoiceRowType line, decimal expectedNet, decimal expectedVat)
    {
        line.netValue.Should().Be(expectedNet, "discount folds into netValue");
        line.vatAmount.Should().Be(expectedVat, "VAT recomputed from the post-discount gross");
        line.deductionsAmountSpecified.Should().BeFalse("a discount is NEVER reported as deductionsAmount");
        line.discountOptionSpecified.Should().BeTrue();
        line.discountOption.Should().BeTrue();
        if (line.incomeClassification is { Length: > 0 })
        {
            line.incomeClassification[0].amount.Should().Be(expectedNet, "classification amount tracks the post-discount net");
        }
    }
}
