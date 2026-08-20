using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// Απλοποιημένο Τιμολόγιο / simplified invoice, myDATA document type 11.3.
///
/// 11.3 is an INVOICE to a business issued in legally simplified form (ΕΛΠ ν.4308/2014 άρθρο 10 §1:
/// amount up to €100, €300 for fuel per ΠΟΛ.1003/2014, or an άρθρο 8 §3 corrective document). It is
/// NOT a retail receipt to a consumer. Its mandatory content deliberately omits the buyer's details,
/// which is why myDATA files it under A2 «Μη Αντικριζόμενα Παραστατικά Εκδότη» — no counterpart.
///
/// The classification, however, is WHOLESALE, because the buyer is a professional.
///     category1_1 / category1_2 / category1_3  ->  E3_561_001   (χονδρικές — επιτηδευματιών)
///     category1_4                              ->  E3_880_001
///     category1_5                              ->  E3_561_007
///     category1_6                              ->  E3_595
///     category1_7                              ->  E3_881_001
/// Sheet "11.3" lists E3_561_001 as the only permitted value for category1_1/1_2/1_3; sheet "11.1"
/// lists E3_561_003 there instead. AADE's esend-myDATA guidance does separately assign a blanket
/// category1_1 + E3_561_003 to 11.1/11.3/11.4, but only when a ΦΗΜ cannot characterize the lines or
/// esend cannot carry the characterizations — a fallback for un-characterized cash-register traffic,
/// not a rule for a provider that sends its own. That is why the old override transmissions were
/// accepted with E3_561_003 rather than rejected.
///
/// Business cases: businesscase/example/SignRequestReceipt_GR_SimplifiedInvoice (265_A..265_E), all
/// live-verified on myDATA dev 2026-08-19. Every one was ACCEPTED by AADE with a MARK, so AADE
/// neither rejects the unrecognised lll bit nor enforces the classification matrix — the current
/// behaviour is wrong in the merchant's books, not in transmission.
///
/// NOT COVERED HERE, intentional:
///  * category1_4 / category1_7 on 11.3. GetIncomeClassificationCategoryType never produces
///    category1_4, and category1_7 (NotOwnSales) is routed to 11.5 before the flag is reached, so
///    E3_880_001 / E3_881_001 are unreachable through this trigger. They are stated above as the
///    AADE rule, not as testable behaviour.
///  * The 11.4 classification question (separate issue — affects all GR retail refunds).
///  * άρθρο 39a on 11.3 (neither E3_561_004 nor E3_561_002 is permitted there; decision pending).
///  * The fuel vehicle-registration plate (deferred).
/// </summary>
public class SimplifiedInvoice113Tests
{
    private const long GR_V2 = 0x4752_2000_0000_0000;

    private static AADEFactory CreateFactory() => new AADEFactory(new MasterDataConfiguration
    {
        Account = new AccountMasterData { VatId = "123456789" },
        Outlet = new OutletMasterData { LocationId = "0" }
    }, "https://receipts.example.com");

    private static ChargeItem CreateChargeItem(ChargeItemCaseTypeOfService typeOfService, decimal amount = 100)
        => new ChargeItem
        {
            Position = 1,
            Amount = amount,
            VATRate = 24,
            VATAmount = decimal.Round(amount / 124M * 24M, 2, MidpointRounding.ToEven),
            ftChargeItemCase = ((ChargeItemCase) GR_V2)
                .WithTypeOfService(typeOfService)
                .WithVat(ChargeItemCase.NormalVatRate),
            Quantity = 1,
            Description = "Test Item"
        };

    /// <summary>POS receipt carrying the GR-local IsSimplifiedInvoice flag (lll = 0x001).</summary>
    private static ReceiptRequest CreateSimplifiedInvoiceReceipt(
        ChargeItemCaseTypeOfService typeOfService = ChargeItemCaseTypeOfService.Delivery,
        bool isRefund = false,
        bool withCustomer = false,
        decimal amount = 100)
    {
        var ftReceiptCase = ((ReceiptCase) GR_V2)
            .WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
            .WithFlag(ReceiptCaseFlagsGR.IsSimplifiedInvoice);

        if (isRefund)
        {
            ftReceiptCase = ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        }

        var signedAmount = isRefund ? -amount : amount;

        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 8, 19, 10, 15, 0, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ftReceiptCase,
            cbChargeItems = [CreateChargeItem(typeOfService, signedAmount)],
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Amount = signedAmount,
                    ftPayItemCase = ((PayItemCase) GR_V2).WithCase(PayItemCase.CashPayment),
                    Description = "Cash"
                }
            ]
        };

        if (withCustomer)
        {
            request.cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "026883248",
                CustomerName = "Πελάτης Α.Ε.",
                CustomerStreet = "Κηφισίας 12",
                CustomerZip = "12345",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR"
            };
        }

        return request;
    }

    private static ReceiptResponse CreateResponse(ReceiptRequest request) => new ReceiptResponse
    {
        cbReceiptReference = request.cbReceiptReference,
        ftReceiptIdentification = "ft123ABC#",
        ftCashBoxIdentification = "TEST-001"
    };

    #region 1.THE TRIGGER — the flag must resolve the document type to 11.3

    /// <summary>
    /// άρθρο 10 covers goods AND services, so ONE flag has to serve both. That means
    /// IsSimplifiedInvoice must be evaluated BEFORE the services-vs-goods split which otherwise
    /// sends goods to 11.1 and a services-only receipt to 11.2.
    /// </summary>
    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.Delivery)]
    [InlineData(ChargeItemCaseTypeOfService.OtherService)]
    [InlineData(ChargeItemCaseTypeOfService.CatalogService)]
    public void Item_11_3_GetInvoiceType_RetailReceipt_WithSimplifiedInvoiceFlag_ReturnsItem113(
        ChargeItemCaseTypeOfService typeOfService)
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(typeOfService);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item113);
    }

    /// <summary>Without the flag nothing changes — goods stay 11.1.</summary>
    [Fact]
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithoutSimplifiedInvoiceFlag_StaysItem111()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt();
        receiptRequest.ftReceiptCase = ((ReceiptCase) GR_V2).WithCase(ReceiptCase.PointOfSaleReceipt0x0001);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item111);
    }

    #endregion

    #region 2.PRECEDENCE — other document types must still win over the flag

    /// <summary>
    /// There is no "credit simplified invoice" type in myDATA, so Refund must WIN over the flag and
    /// produce an 11.4 Πιστωτικό Στοιχείο Λιανικής.
    /// Live: 265_D MARK 400001968026868 -> 11.4.
    /// </summary>
    [Fact]
    public void Item_11_4_GetInvoiceType_RetailReceipt_RefundWithSimplifiedInvoiceFlag_ReturnsItem114()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(isRefund: true);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item114);
    }

    /// <summary>Agency business keeps its own type — 11.5 wins over the flag.</summary>
    [Fact]
    public void Item_11_5_GetInvoiceType_RetailReceipt_NotOwnSalesWithSimplifiedInvoiceFlag_ReturnsItem115()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(ChargeItemCaseTypeOfService.NotOwnSales);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item115);
    }

    /// <summary>Self-delivery keeps its own type — 6.1 wins over the flag.</summary>
    [Fact]
    public void Item_6_1_GetInvoiceType_RetailReceipt_OwnConsumptionWithSimplifiedInvoiceFlag_ReturnsItem61()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(ChargeItemCaseTypeOfService.OwnConsumption);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item61);
    }

    #endregion

    #region 3.INCOME CLASSIFICATION — 11.3 is wholesale, never E3_561_003
    
    /// <summary>
    /// Today the receipt branch of GetIncomeClassificationValueType returns E3_561_003 for all of
    /// these, which sheet "11.3" does not permit. Live proof: 265_A / 265_B / 265_D / 265_E all
    /// transmitted E3_561_003 and were still accepted by AADE.
    /// </summary>
    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.Delivery)]       // category1_1
    [InlineData(ChargeItemCaseTypeOfService.CatalogService)] // category1_2
    [InlineData(ChargeItemCaseTypeOfService.OtherService)]   // category1_3
    public void GetIncomeClassificationValueType_SimplifiedInvoice_ReturnsWholesaleE3_561_001(
        ChargeItemCaseTypeOfService typeOfService)
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(typeOfService);

        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, receiptRequest.cbChargeItems[0]);

        result.Should().Be(IncomeClassificationValueType.E3_561_001);
    }

    /// <summary>
    /// The pre-existing override route to 11.3 is broken in exactly the same way: overriding only
    /// invoiceHeader.invoiceType leaves the classification on the retail branch. Proven live —
    /// MARK 400001965099544 carried 11.3 + category1_1 + E3_561_003. Fixing the flag path must fix
    /// this too, otherwise partners already using the override keep filing wholesale sales as retail.
    /// </summary>
    [Fact]
    public void GetIncomeClassificationValueType_InvoiceTypeOverriddenTo113_ReturnsWholesaleE3_561_001()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt();
        // No flag — only the myDATA override, the way partners reach 11.3 today.
        receiptRequest.ftReceiptCase = ((ReceiptCase) GR_V2).WithCase(ReceiptCase.PointOfSaleReceipt0x0001);
        receiptRequest.ftReceiptCaseData = new
        {
            GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "11.3" } } } }
        };

        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, receiptRequest.cbChargeItems[0]);

        result.Should().Be(IncomeClassificationValueType.E3_561_001);
    }

    /// <summary>An ordinary POS receipt must keep the retail code — this is the 11.1 behaviour.</summary>
    [Fact]
    public void GetIncomeClassificationValueType_PlainRetailReceipt_StaysE3_561_003()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt();
        receiptRequest.ftReceiptCase = ((ReceiptCase) GR_V2).WithCase(ReceiptCase.PointOfSaleReceipt0x0001);

        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, receiptRequest.cbChargeItems[0]);

        result.Should().Be(IncomeClassificationValueType.E3_561_003);
    }

    /// <summary>
    /// Crediting a simplified invoice: the document type becomes 11.4, but the revenue being reversed
    /// was booked as WHOLESALE, so the reversal has to be wholesale too — otherwise the merchant's Ε3
    /// shows a wholesale sale cancelled by a retail credit and the two never net out.
    /// AADE agrees: sheet "11.4" carries the same wholesale set as "11.3" in both v1.0.10 and v2.0.1.
    ///
    /// NOTE: this is a genuine behaviour decision, not just a consequence. It is scoped to documents
    /// carrying the flag (or the 11.3 override), so ordinary 11.1 refunds are untouched and keep
    /// E3_561_003 — the separate, unresolved "is 11.4 always wholesale?" question is NOT decided here.
    /// </summary>
    [Fact]
    public void GetIncomeClassificationValueType_SimplifiedInvoiceRefund_ReturnsWholesaleE3_561_001()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(isRefund: true);

        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, receiptRequest.cbChargeItems[0]);

        result.Should().Be(IncomeClassificationValueType.E3_561_001);
    }

    /// <summary>A refund WITHOUT the flag stays retail — ordinary 11.1/11.4 behaviour is untouched.</summary>
    [Fact]
    public void GetIncomeClassificationValueType_PlainRefund_StaysE3_561_003()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(isRefund: true);
        receiptRequest.ftReceiptCase = ((ReceiptCase) GR_V2)
            .WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
            .WithFlag(ReceiptCaseFlags.Refund);

        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, receiptRequest.cbChargeItems[0]);

        result.Should().Be(IncomeClassificationValueType.E3_561_003);
    }

    /// <summary>
    /// Self-delivery short-circuits before the receipt branch, so E3_595 must be unaffected by the
    /// flag. Guard against the classification change reaching too far.
    /// </summary>
    [Fact]
    public void GetIncomeClassificationValueType_OwnConsumptionWithSimplifiedInvoiceFlag_StaysE3_595()
    {
        var receiptRequest = CreateSimplifiedInvoiceReceipt(ChargeItemCaseTypeOfService.OwnConsumption);

        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, receiptRequest.cbChargeItems[0]);

        result.Should().Be(IncomeClassificationValueType.E3_595);
    }

    #endregion

    #region 4.NO COUNTERPART — 11.3 is «Μη Αντικριζόμενο»; these pin CORRECT existing behaviour

    /// <summary>
    /// A supplied cbCustomer must NOT reach the transmitted document. Live proof: 265_E
    /// (MARK 400001968027108) produced a document identical to 265_A's — the customer had zero
    /// effect. That must remain true once the type becomes 11.3.
    /// </summary>
    [Fact]
    public void MapToInvoicesDoc_SimplifiedInvoiceWithCustomer_HasNoCounterpartAndIs113()
    {
        var factory = CreateFactory();
        var request = CreateSimplifiedInvoiceReceipt(withCustomer: true);

        var (doc, error) = factory.MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var invoice = doc!.invoice[0];
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item113);
        invoice.counterpart.Should().BeNull("11.3 is a «Μη Αντικριζόμενο» document — ΕΛΠ άρθρο 10 §2 omits the buyer's details");
    }

    /// <summary>11.3 must never demand customer info — the document type exists to avoid it.</summary>
    [Fact]
    public void RequiresCustomerInfo_Item113_IsFalse()
        => AADEMappings.RequiresCustomerInfo(InvoiceType.Item113).Should().BeFalse();

    /// <summary>
    /// Confirms the retracted "counterpart is being dropped" reading: dropping it is CORRECT for
    /// 11.3, per the ERP spec ("Non-Mirrored"), the provider spec ("Non-Counterparty Documents")
    /// and the A.1138/2020 A2 grouping.
    /// </summary>
    [Fact]
    public void SupportsCounterpart_Item113_IsFalse()
        => AADEMappings.SupportsCounterpart(InvoiceType.Item113).Should().BeFalse();

    #endregion

    #region 5.CORRELATED INVOICES — ΕΛΠ άρθρο 10 §1(b): a corrective 11.3 references the original

    /// <summary>
    /// A simplified invoice is not only the ≤€100 case: άρθρο 10 §1(b) also covers an άρθρο 8 §3
    /// document that amends and refers specifically to an ORIGINAL invoice. So an 11.3 must be able
    /// to carry a reference to the document it corrects, otherwise that whole leg of άρθρο 10 is
    /// unreachable.
    ///
    /// The spec puts the reference in multipleConnectedMarks, not correlatedInvoices: the ERP spec
    /// says multipleConnectedMarks "is not acceptable for documents of type 1.6, 2.4 and 5.1" —
    /// 11.3 is not excluded — which is exactly what SupportsMultipleConnectedMarks already encodes.
    /// SupportsCorrelatedInvoices(Item113) = false therefore costs nothing here: the non-refund
    /// branch of AADEFactory reaches multipleConnectedMarks first, so no reference is lost.
    ///
    /// Consequence for this test: only the invoiceType is red. The mark handling is already correct
    /// today (a POS receipt with a previous mark carries it), and must stay correct once the type
    /// becomes 11.3.
    /// </summary>
    [Fact]
    public void MapToInvoicesDoc_SimplifiedInvoiceWithPreviousMark_CarriesMultipleConnectedMarksAndIs113()
    {
        var factory = CreateFactory();
        var request = CreateSimplifiedInvoiceReceipt();
        // MARK supplied directly — the route for an original invoice this middleware did not issue.
        request.ftReceiptCaseData = new
        {
            GR = new { PreviousReceiptReference = new { invoiceMark = "400001968025746" } }
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item113);
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400001968025746L });
        header.correlatedInvoices.Should().BeNull("the non-refund branch uses multipleConnectedMarks for retail-family types");
    }

    /// <summary>
    /// Crediting a simplified invoice: Refund still wins the type (11.4), and the reference to the
    /// corrected document must survive. Green today — guard against the type change breaking it.
    /// </summary>
    [Fact]
    public void MapToInvoicesDoc_SimplifiedInvoiceRefundWithPreviousMark_CarriesMultipleConnectedMarksAndIs114()
    {
        var factory = CreateFactory();
        var request = CreateSimplifiedInvoiceReceipt(isRefund: true);
        request.ftReceiptCaseData = new
        {
            GR = new { PreviousReceiptReference = new { invoiceMark = "400001968025746" } }
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, CreateResponse(request));

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item114);
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400001968025746L });
    }

    /// <summary>
    /// Spec check, not a behaviour change: multipleConnectedMarks is unacceptable only for 1.6, 2.4
    /// and 5.1, so 11.3 must remain permitted. Pins why SupportsCorrelatedInvoices(Item113)=false is
    /// harmless.
    /// </summary>
    [Fact]
    public void SupportsMultipleConnectedMarks_Item113_IsTrue()
        => AADEMappings.SupportsMultipleConnectedMarks(InvoiceType.Item113).Should().BeTrue();

    #endregion
}
