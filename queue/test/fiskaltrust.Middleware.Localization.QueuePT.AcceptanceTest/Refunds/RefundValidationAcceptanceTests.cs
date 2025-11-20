using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using System.Text.Json;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Refunds;

/// <summary>
/// Acceptance tests for Portuguese refund validation according to Portuguese fiscal regulations.
/// These tests serve as the baseline for certification and documentation.
/// 
/// Portuguese Regulations:
/// - Full refunds (Credit Notes) must match the original invoice exactly
/// - Partial refunds must not mix refund and non-refund items in the same receipt
/// - Multiple partial refunds are allowed but cannot exceed the original quantities/amounts
/// - Only one full refund is allowed per invoice
/// </summary>
public class RefundValidationAcceptanceTests : IDisposable
{
    private readonly InMemoryQueueItemRepository _queueItemRepository;
    private readonly InvoiceCommandProcessorPT _processor;
    private readonly ftQueuePT _queuePT;
    private readonly MockPTSSCD _mockSscd;

    public RefundValidationAcceptanceTests()
    {
        _queueItemRepository = new InMemoryQueueItemRepository();
        _mockSscd = new MockPTSSCD();
        
        _queuePT = new ftQueuePT
        {
            ftQueuePTId = Guid.NewGuid(),
            IssuerTIN = "123456789",
            NumeratorStorage = new NumeratorStorage
            {
                InvoiceSeries = new NumberSeries
                {
                    TypeCode = "FT",
                    ATCUD = "ATCUD-2024-001",
                    Series = "2024",
                    Numerator = 0,
                    LastHash = "0"
                },
                CreditNoteSeries = new NumberSeries
                {
                    TypeCode = "NC",
                    ATCUD = "ATCUD-2024-CN",
                    Series = "2024",
                    Numerator = 0,
                    LastHash = "0"
                }
            }
        };

        _processor = new InvoiceCommandProcessorPT(
            _mockSscd,
            _queuePT,
            new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(_queueItemRepository))
        );
    }

    #region Scenario 1: Simple Full Refund (Credit Note)

    /// <summary>
    /// Scenario 1: Simple Full Refund
    /// 
    /// Story: A customer purchases 2 items and later returns everything for a full refund.
    /// 
    /// Steps:
    /// 1. Create an invoice with 2 items (Product A: 2x @ 50€, Product B: 1x @ 30€)
    /// 2. Create a full refund (Credit Note) for the entire invoice
    /// 
    /// Expected Result: Full refund is accepted
    /// </summary>
    [Fact]
    public async Task Scenario1_SimpleFullRefund_ShouldSucceed()
    {
        // Step 1: Create original invoice
        var originalInvoice = await CreateAndProcessInvoice("INV-2024-001",
            CreateChargeItem("PROD-A", "Product A", 2, 100m, 23m),
            CreateChargeItem("PROD-B", "Product B", 1, 30m, 23m)
        );

        originalInvoice.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE, 
            "Original invoice should be processed successfully");

        // Step 2: Create full refund (Credit Note)
        var fullRefund = await CreateAndProcessFullRefund(
            "INV-2024-001", 
            "CN-2024-001",
            CreateChargeItem("PROD-A", "Product A", -2, -100m, 23m),
            CreateChargeItem("PROD-B", "Product B", -1, -30m, 23m)
        );

        // Assert
        fullRefund.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "Full refund matching original invoice should be accepted");
        
        fullRefund.receiptResponse.ftSignatures.Should().Contain(s => s.ftSignatureType == (SignatureType) 0x5054_0000_0000_0001,
            "Credit Note should have proper signature");
    }

    #endregion

    #region Scenario 2: Full Refund Validation Failures

    /// <summary>
    /// Scenario 2a: Full Refund with Missing Item
    /// 
    /// Story: A customer tries to create a credit note but forgets to include one of the items.
    /// 
    /// Expected Result: Refund is rejected with error EEEE_FullRefundItemsMismatch
    /// </summary>
    [Fact]
    public async Task Scenario2a_FullRefund_WithMissingItem_ShouldFail()
    {
        // Step 1: Create original invoice with 2 items
        await CreateAndProcessInvoice("INV-2024-002",
            CreateChargeItem("PROD-A", "Product A", 2, 100m, 23m),
            CreateChargeItem("PROD-B", "Product B", 1, 30m, 23m)
        );

        // Step 2: Attempt full refund with only 1 item
        var fullRefund = await CreateAndProcessFullRefund(
            "INV-2024-002",
            "CN-2024-002",
            CreateChargeItem("PROD-A", "Product A", -2, -100m, 23m)
            // Missing PROD-B
        );

        // Assert
        fullRefund.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Full refund with missing items should be rejected");
        
        fullRefund.receiptResponse.ftStateData.ToString().Should().Contain("EEEE_FullRefundItemsMismatch",
            "Error message should indicate items don't match");
    }

    /// <summary>
    /// Scenario 2b: Full Refund with Incorrect Quantity
    /// 
    /// Story: A cashier enters wrong quantity in the credit note.
    /// 
    /// Expected Result: Refund is rejected with error EEEE_FullRefundItemsMismatch
    /// </summary>
    [Fact]
    public async Task Scenario2b_FullRefund_WithIncorrectQuantity_ShouldFail()
    {
        // Step 1: Create original invoice
        await CreateAndProcessInvoice("INV-2024-003",
            CreateChargeItem("PROD-A", "Product A", 2, 100m, 23m)
        );

        // Step 2: Attempt full refund with wrong quantity
        var fullRefund = await CreateAndProcessFullRefund(
            "INV-2024-003",
            "CN-2024-003",
            CreateChargeItem("PROD-A", "Product A", -3, -100m, 23m) // Wrong quantity
        );

        // Assert
        fullRefund.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Full refund with incorrect quantity should be rejected");
    }

    /// <summary>
    /// Scenario 2c: Attempt Second Full Refund
    /// 
    /// Story: Someone tries to create a second credit note for an invoice that was already fully refunded.
    /// 
    /// Expected Result: Second refund is rejected with error EEEE_RefundAlreadyExists
    /// </summary>
    [Fact]
    public async Task Scenario2c_SecondFullRefund_ShouldFail()
    {
        // Step 1: Create original invoice
        await CreateAndProcessInvoice("INV-2024-004",
            CreateChargeItem("PROD-A", "Product A", 2, 100m, 23m)
        );

        // Step 2: Create first full refund
        await CreateAndProcessFullRefund(
            "INV-2024-004",
            "CN-2024-004-1",
            CreateChargeItem("PROD-A", "Product A", -2, -100m, 23m)
        );

        // Step 3: Attempt second full refund
        var secondRefund = await CreateAndProcessFullRefund(
            "INV-2024-004",
            "CN-2024-004-2",
            CreateChargeItem("PROD-A", "Product A", -2, -100m, 23m)
        );

        // Assert
        secondRefund.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Second full refund should be rejected");
        
        secondRefund.receiptResponse.ftStateData.ToString().Should().Contain("EEEE_RefundAlreadyExists",
            "Error should indicate refund already exists");
    }

    #endregion

    #region Scenario 3: Partial Refunds

    /// <summary>
    /// Scenario 3a: Simple Partial Refund
    /// 
    /// Story: A customer returns 1 of 5 items purchased.
    /// 
    /// Steps:
    /// 1. Create invoice with 5 items
    /// 2. Create partial refund for 1 item (using item refund flag, not receipt refund flag)
    /// 
    /// Expected Result: Partial refund is accepted
    /// </summary>
    [Fact]
    public async Task Scenario3a_SimplePartialRefund_ShouldSucceed()
    {
        // Step 1: Create original invoice
        await CreateAndProcessInvoice("INV-2024-005",
            CreateChargeItem("PROD-A", "Product A", 5, 250m, 23m)
        );

        // Step 2: Create partial refund for 1 item
        var partialRefund = await CreateAndProcessPartialRefund(
            "INV-2024-005",
            "PREF-2024-001",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -1, -50m, 23m)
        );

        // Assert
        partialRefund.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "Partial refund within limits should be accepted");
    }

    /// <summary>
    /// Scenario 3b: Multiple Partial Refunds
    /// 
    /// Story: A customer returns items in multiple transactions:
    /// - First return: 2 items
    /// - Second return: 2 more items
    /// - Original purchase: 5 items
    /// 
    /// Expected Result: Both partial refunds are accepted (total 4 of 5 items refunded)
    /// </summary>
    [Fact]
    public async Task Scenario3b_MultiplePartialRefunds_ShouldSucceed()
    {
        // Step 1: Create original invoice
        await CreateAndProcessInvoice("INV-2024-006",
            CreateChargeItem("PROD-A", "Product A", 5, 250m, 23m)
        );

        // Step 2: First partial refund (2 items)
        var firstRefund = await CreateAndProcessPartialRefund(
            "INV-2024-006",
            "PREF-2024-002-1",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -2, -100m, 23m)
        );

        firstRefund.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "First partial refund should be accepted");

        // Step 3: Second partial refund (2 more items, total 4 of 5)
        var secondRefund = await CreateAndProcessPartialRefund(
            "INV-2024-006",
            "PREF-2024-002-2",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -2, -100m, 23m)
        );

        // Assert
        secondRefund.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "Second partial refund should be accepted (total 4 of 5 items)");
    }

    /// <summary>
    /// Scenario 3c: Partial Refund with Multiple Products
    /// 
    /// Story: Customer returns some items from a multi-product purchase
    /// 
    /// Expected Result: Partial refund with multiple products is accepted
    /// </summary>
    [Fact]
    public async Task Scenario3c_PartialRefundMultipleProducts_ShouldSucceed()
    {
        // Step 1: Create original invoice with multiple products
        await CreateAndProcessInvoice("INV-2024-007",
            CreateChargeItem("PROD-A", "Product A", 5, 250m, 23m),
            CreateChargeItem("PROD-B", "Product B", 3, 150m, 23m),
            CreateChargeItem("PROD-C", "Product C", 2, 100m, 23m)
        );

        // Step 2: Partial refund of some items from each product
        var partialRefund = await CreateAndProcessPartialRefund(
            "INV-2024-007",
            "PREF-2024-003",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -2, -100m, 23m),
            CreateChargeItemWithRefundFlag("PROD-B", "Product B", -1, -50m, 23m)
        );

        // Assert
        partialRefund.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "Partial refund with multiple products should be accepted");
    }

    #endregion

    #region Scenario 4: Partial Refund Validation Failures

    /// <summary>
    /// Scenario 4a: Partial Refund with Mixed Items
    /// 
    /// Story: Cashier tries to combine a refund with a new sale in the same receipt
    /// (This is not allowed in Portugal)
    /// 
    /// Expected Result: Receipt is rejected with error EEEE_MixedRefundItemsNotAllowed
    /// </summary>
    [Fact]
    public async Task Scenario4a_PartialRefund_WithMixedItems_ShouldFail()
    {
        // Step 1: Create original invoice
        await CreateAndProcessInvoice("INV-2024-008",
            CreateChargeItem("PROD-A", "Product A", 5, 250m, 23m)
        );

        // Step 2: Attempt receipt with both refund and sale items
        var mixedReceipt = await CreateAndProcessPartialRefund(
            "INV-2024-008",
            "MIX-2024-001",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -1, -50m, 23m), // Refund
            CreateChargeItem("PROD-B", "Product B", 1, 30m, 23m) // New sale - NOT ALLOWED
        );

        // Assert
        mixedReceipt.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Mixed refund and sale items should be rejected");
        
        mixedReceipt.receiptResponse.ftStateData.ToString().Should().Contain("EEEE_MixedRefundItemsNotAllowed",
            "Error should indicate mixed items are not allowed");
    }

    /// <summary>
    /// Scenario 4b: Partial Refund Exceeding Original Quantity
    /// 
    /// Story: Cashier enters a refund quantity greater than what was purchased
    /// 
    /// Expected Result: Refund is rejected with error EEEE_PartialRefundExceedsOriginalQuantity
    /// </summary>
    [Fact]
    public async Task Scenario4b_PartialRefund_ExceedingQuantity_ShouldFail()
    {
        // Step 1: Create original invoice (2 items)
        await CreateAndProcessInvoice("INV-2024-009",
            CreateChargeItem("PROD-A", "Product A", 2, 100m, 23m)
        );

        // Step 2: Attempt partial refund for 3 items (more than purchased)
        var excessRefund = await CreateAndProcessPartialRefund(
            "INV-2024-009",
            "PREF-2024-004",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -3, -150m, 23m)
        );

        // Assert
        excessRefund.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Partial refund exceeding quantity should be rejected");
        
        excessRefund.receiptResponse.ftStateData.ToString().Should().Contain("exceeds the original quantity",
            "Error should indicate quantity exceeded");
    }

    /// <summary>
    /// Scenario 4c: Multiple Partial Refunds Exceeding Total
    /// 
    /// Story: Customer makes multiple returns that together exceed what was purchased
    /// - Original: 5 items
    /// - First refund: 2 items (OK)
    /// - Second refund: 4 items (total would be 6 - NOT OK)
    /// 
    /// Expected Result: Second refund is rejected
    /// </summary>
    [Fact]
    public async Task Scenario4c_MultiplePartialRefunds_ExceedingTotal_ShouldFail()
    {
        // Step 1: Create original invoice
        await CreateAndProcessInvoice("INV-2024-010",
            CreateChargeItem("PROD-A", "Product A", 5, 250m, 23m)
        );

        // Step 2: First partial refund (2 items - OK)
        await CreateAndProcessPartialRefund(
            "INV-2024-010",
            "PREF-2024-005-1",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -2, -100m, 23m)
        );

        // Step 3: Second partial refund (4 items - total 6, exceeds 5)
        var excessRefund = await CreateAndProcessPartialRefund(
            "INV-2024-010",
            "PREF-2024-005-2",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -4, -200m, 23m)
        );

        // Assert
        excessRefund.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Partial refund that would exceed original total should be rejected");
        
        excessRefund.receiptResponse.ftStateData.ToString().Should().Contain("exceeds the original",
            "Error should indicate exceeded original");
    }

    /// <summary>
    /// Scenario 4d: Partial Refund Exceeding Original Amount
    /// 
    /// Story: Cashier tries to refund more money than the original price
    /// 
    /// Expected Result: Refund is rejected with error EEEE_PartialRefundExceedsOriginalAmount
    /// </summary>
    [Fact]
    public async Task Scenario4d_PartialRefund_ExceedingAmount_ShouldFail()
    {
        // Step 1: Create original invoice
        await CreateAndProcessInvoice("INV-2024-011",
            CreateChargeItem("PROD-A", "Product A", 2, 100m, 23m)
        );

        // Step 2: Attempt partial refund with excessive amount
        var excessRefund = await CreateAndProcessPartialRefund(
            "INV-2024-011",
            "PREF-2024-006",
            CreateChargeItemWithRefundFlag("PROD-A", "Product A", -2, -120m, 23m) // More than original 100€
        );

        // Assert
        excessRefund.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Partial refund exceeding amount should be rejected");
        
        excessRefund.receiptResponse.ftStateData.ToString().Should().Contain("exceeds the original amount",
            "Error should indicate amount exceeded");
    }

    /// <summary>
    /// Scenario 4e: Partial Refund Without Previous Receipt Reference
    /// 
    /// Story: Cashier forgets to link the refund to the original invoice
    /// 
    /// Expected Result: Refund is rejected
    /// </summary>
    [Fact]
    public async Task Scenario4e_PartialRefund_WithoutReference_ShouldFail()
    {
        // Attempt partial refund without cbPreviousReceiptReference
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001, // No refund flag
            cbReceiptReference = "PREF-2024-007",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            // No cbPreviousReceiptReference!
            cbChargeItems = new List<ChargeItem>
            {
                CreateChargeItemWithRefundFlag("PROD-A", "Product A", -1, -50m, 23m)
            }
        };

        var response = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State)0x5054_0000_0000_0000
        };

        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, response));

        // Assert
        result.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE,
            "Partial refund without reference should be rejected");
    }

    #endregion

    #region Scenario 5: Complex Real-World Scenarios

    /// <summary>
    /// Scenario 5a: Restaurant - Mixed Products Partial Refund
    /// 
    /// Story: Restaurant receipt with multiple items, customer sends back one dish
    /// - Original: 2x Steak, 3x Salad, 1x Wine
    /// - Refund: 1x Steak (customer didn't like it)
    /// 
    /// Expected Result: Partial refund is accepted
    /// </summary>
    [Fact]
    public async Task Scenario5a_Restaurant_PartialRefund_ShouldSucceed()
    {
        // Step 1: Original restaurant order
        await CreateAndProcessInvoice("REST-2024-001",
            CreateChargeItem("STEAK-001", "Ribeye Steak", 2, 60m, 23m),
            CreateChargeItem("SALAD-001", "Caesar Salad", 3, 24m, 6m),
            CreateChargeItem("WINE-001", "House Red Wine", 1, 15m, 23m)
        );

        // Step 2: Customer returns one steak
        var partialRefund = await CreateAndProcessPartialRefund(
            "REST-2024-001",
            "REST-REF-001",
            CreateChargeItemWithRefundFlag("STEAK-001", "Ribeye Steak", -1, -30m, 23m)
        );

        // Assert
        partialRefund.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "Restaurant partial refund should be accepted");
    }

    /// <summary>
    /// Scenario 5b: Retail - Complete Order Cancellation
    /// 
    /// Story: Customer orders multiple items, decides to cancel everything
    /// 
    /// Expected Result: Full refund (Credit Note) is accepted
    /// </summary>
    [Fact]
    public async Task Scenario5b_Retail_CompleteOrderCancellation_ShouldSucceed()
    {
        // Step 1: Original retail order
        await CreateAndProcessInvoice("RETAIL-2024-001",
            CreateChargeItem("SHOE-001", "Running Shoes", 1, 89.99m, 23m),
            CreateChargeItem("SOCK-001", "Sports Socks", 3, 15m, 23m),
            CreateChargeItem("SHIRT-001", "T-Shirt", 2, 25m, 23m)
        );

        // Step 2: Complete cancellation
        var fullRefund = await CreateAndProcessFullRefund(
            "RETAIL-2024-001",
            "CN-RETAIL-001",
            CreateChargeItem("SHOE-001", "Running Shoes", -1, -89.99m, 23m),
            CreateChargeItem("SOCK-001", "Sports Socks", -3, -15m, 23m),
            CreateChargeItem("SHIRT-001", "T-Shirt", -2, -25m, 23m)
        );

        // Assert
        fullRefund.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "Complete order cancellation should be accepted");
    }

    /// <summary>
    /// Scenario 5c: Retail - Progressive Returns
    /// 
    /// Story: Customer buys 10 items, returns them progressively over multiple visits
    /// - Original: 10 items @ 50€ each = 500€
    /// - Visit 1: Return 3 items
    /// - Visit 2: Return 4 items
    /// - Visit 3: Return 3 items (complete)
    /// 
    /// Expected Result: All three partial refunds are accepted
    /// </summary>
    [Fact]
    public async Task Scenario5c_Retail_ProgressiveReturns_ShouldSucceed()
    {
        // Step 1: Original purchase (10 items)
        await CreateAndProcessInvoice("BULK-2024-001",
            CreateChargeItem("ITEM-001", "Bulk Item", 10, 500m, 23m)
        );

        // Step 2: First return (3 items)
        var refund1 = await CreateAndProcessPartialRefund(
            "BULK-2024-001",
            "BULK-REF-001",
            CreateChargeItemWithRefundFlag("ITEM-001", "Bulk Item", -3, -150m, 23m)
        );
        refund1.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE);

        // Step 3: Second return (4 items)
        var refund2 = await CreateAndProcessPartialRefund(
            "BULK-2024-001",
            "BULK-REF-002",
            CreateChargeItemWithRefundFlag("ITEM-001", "Bulk Item", -4, -200m, 23m)
        );
        refund2.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE);

        // Step 4: Third return (3 items - completes the refund)
        var refund3 = await CreateAndProcessPartialRefund(
            "BULK-2024-001",
            "BULK-REF-003",
            CreateChargeItemWithRefundFlag("ITEM-001", "Bulk Item", -3, -150m, 23m)
        );

        // Assert
        refund3.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE,
            "Final partial refund completing the total should be accepted");
    }

    #endregion

    #region Helper Methods

    private async Task<ProcessCommandResponse> CreateAndProcessInvoice(
        string receiptReference,
        params ChargeItem[] chargeItems)
    {
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = receiptReference,
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = chargeItems.ToList()
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State)0x5054_0000_0000_0000
        };

        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Store in repository for reference lookup
        await _queueItemRepository.InsertAsync(new ftQueueItem
        {
            ftQueueItemId = receiptResponse.ftQueueItemID,
            ftQueueId = _queuePT.ftQueuePTId,
            cbReceiptReference = receiptReference,
            cbTerminalID = "TERM-001",
            request = JsonSerializer.Serialize(receiptRequest),
            response = JsonSerializer.Serialize(result.receiptResponse),
            TimeStamp = DateTime.UtcNow.Ticks
        });

        return result;
    }

    private async Task<ProcessCommandResponse> CreateAndProcessFullRefund(
        string originalReceiptReference,
        string refundReceiptReference,
        params ChargeItem[] chargeItems)
    {
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.InvoiceB2C0x1001 | (long)ReceiptCaseFlags.Refund),
            cbReceiptReference = refundReceiptReference,
            cbPreviousReceiptReference = originalReceiptReference,
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = chargeItems.ToList()
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State)0x5054_0000_0000_0000
        };

        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Store in repository
        await _queueItemRepository.InsertAsync(new ftQueueItem
        {
            ftQueueItemId = receiptResponse.ftQueueItemID,
            ftQueueId = _queuePT.ftQueuePTId,
            cbReceiptReference = refundReceiptReference,
            cbTerminalID = "TERM-001",
            request = JsonSerializer.Serialize(receiptRequest),
            response = JsonSerializer.Serialize(result.receiptResponse),
            TimeStamp = DateTime.UtcNow.Ticks
        });

        return result;
    }

    private async Task<ProcessCommandResponse> CreateAndProcessPartialRefund(
        string originalReceiptReference,
        string refundReceiptReference,
        params ChargeItem[] chargeItems)
    {
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001, // No refund flag on receipt case
            cbReceiptReference = refundReceiptReference,
            cbPreviousReceiptReference = originalReceiptReference,
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = chargeItems.ToList()
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State)0x5054_0000_0000_0000
        };

        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Store in repository
        await _queueItemRepository.InsertAsync(new ftQueueItem
        {
            ftQueueItemId = receiptResponse.ftQueueItemID,
            ftQueueId = _queuePT.ftQueuePTId,
            cbReceiptReference = refundReceiptReference,
            cbTerminalID = "TERM-001",
            request = JsonSerializer.Serialize(receiptRequest),
            response = JsonSerializer.Serialize(result.receiptResponse),
            TimeStamp = DateTime.UtcNow.Ticks
        });

        return result;
    }

    private static ChargeItem CreateChargeItem(
        string productNumber,
        string description,
        decimal quantity,
        decimal amount,
        decimal vatRate)
    {
        return new ChargeItem
        {
            ProductNumber = productNumber,
            Description = description,
            Quantity = quantity,
            Amount = amount,
            VATRate = vatRate,
            ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
        };
    }

    private static ChargeItem CreateChargeItemWithRefundFlag(
        string productNumber,
        string description,
        decimal quantity,
        decimal amount,
        decimal vatRate)
    {
        return new ChargeItem
        {
            ProductNumber = productNumber,
            Description = description,
            Quantity = quantity,
            Amount = amount,
            VATRate = vatRate,
            ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
        };
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #endregion

    #region Mock SSCD

    private class MockPTSSCD : IPTSSCD
    {
        private int _counter = 0;

        public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => throw new NotImplementedException();
        public Task<PTSSCDInfo> GetInfoAsync() => throw new NotImplementedException();

        public Task<(ProcessResponse, string)> ProcessReceiptAsync(ProcessRequest request, string lastHash)
        {
            _counter++;
            var hash = $"HASH-{_counter:D40}".PadRight(40, '0');
            
            return Task.FromResult((
                new ProcessResponse { ReceiptResponse = request.ReceiptResponse },
                hash
            ));
        }
    }

    #endregion
}
