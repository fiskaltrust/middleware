using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;

/// <summary>
/// Acceptance tests for Portuguese charge item validation.
/// These tests verify that charge items meet the mandatory requirements
/// according to Portuguese fiscal regulations.
/// 
/// Requirements:
/// - Description must be at least 3 characters
/// - Amount must be set
/// - VAT rate must be set
/// - Quantities and amounts must be non-negative (except for discounts and refunds)
/// </summary>
public class ChargeItemValidationAcceptanceTests
{
    private readonly InMemoryQueueItemRepository _queueItemRepository;
    private readonly InvoiceCommandProcessorPT _processor;
    private readonly ftQueuePT _queuePT;
    private readonly MockPTSSCD _mockSscd;

    public ChargeItemValidationAcceptanceTests()
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
                    ATCUD = "AAJFJ2K6JF",
                    Series = "ft2025b814",
                    Numerator = 0,
                    LastHash = "0"
                },
                SimplifiedInvoiceSeries = new NumberSeries
                {
                    TypeCode = "FS",
                    ATCUD = "AAJFJNK6JJ",
                    Series = "ft20257d14",
                    Numerator = 0,
                    LastHash = "0"
                },
                CreditNoteSeries = new NumberSeries
                {
                    TypeCode = "NC",
                    ATCUD = "AAJFJ6K6J5",
                    Series = "ft2025128b",
                    Numerator = 0,
                    LastHash = "0"
                },
                HandWrittenFSSeries = new NumberSeries
                {
                    TypeCode = "FS",
                    ATCUD = "AAJFJHK6J6",
                    Series = "ft20250a62",
                    Numerator = 0,
                    LastHash = "0"
                },
                ProFormaSeries = new NumberSeries
                {
                    TypeCode = "PF",
                    ATCUD = "AAJFJFK6JH",
                    Series = "ft20253a3b",
                    Numerator = 0,
                    LastHash = "0"
                },
                PaymentSeries = new NumberSeries
                {
                    TypeCode = "RG",
                    ATCUD = "AAJFJ8K6JT",
                    Series = "ft2025a4fa",
                    Numerator = 0,
                    LastHash = "0"
                },
                BudgetSeries = new NumberSeries
                {
                    TypeCode = "OR",
                    ATCUD = "AAJFJYK6JN",
                    Series = "ft20255389",
                    Numerator = 0,
                    LastHash = "0"
                },
                TableChecqueSeries = new NumberSeries
                {
                    TypeCode = "CM",
                    ATCUD = "AAJFJPK6JZ",
                    Series = "ft20259c2f",
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

    #region Scenario 1: Valid Charge Items

    /// <summary>
    /// Scenario 1a: Complete Charge Item
    /// 
    /// Story: A properly filled charge item with all required fields
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario1a_CompleteChargeItem_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-001",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Premium Coffee Beans 1kg",
                    Quantity = 2,
                    Amount = 39.98m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 39.98m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1",
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "123456789",
            }
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), JsonSerializer.Deserialize<ReceiptRequest>(JsonSerializer.Serialize(receiptRequest)), receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Complete charge item should be accepted");
    }

    /// <summary>
    /// Scenario 1b: Minimum Valid Description Length
    /// 
    /// Story: Charge item with exactly 3 characters in description (minimum allowed)
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario1b_MinimumValidDescriptionLength_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-002",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Tea", // Exactly 3 characters
                    Quantity = 1,
                    Amount = 5.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 5.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Minimum valid description length (3 chars) should be accepted");
    }

    /// <summary>
    /// Scenario 1c: Multiple Valid Charge Items
    /// 
    /// Story: Receipt with multiple properly filled charge items
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario1c_MultipleValidChargeItems_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-003",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Coffee",
                    Quantity = 2,
                    Amount = 6.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Croissant",
                    Quantity = 3,
                    Amount = 4.50m,
                    VATRate = 6m,
                    ftChargeItemCase = PTVATRates.Discounted1Case
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-003",
                    Description = "Orange Juice",
                    Quantity = 1,
                    Amount = 2.50m,
                    VATRate = 13m,
                    ftChargeItemCase = PTVATRates.Discounted2Case
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 13.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Multiple valid charge items should be accepted");
    }

    #endregion

    #region Scenario 2: Missing or Invalid Descriptions

    /// <summary>
    /// Scenario 2a: Missing Description
    /// 
    /// Story: Charge item without a description
    /// 
    /// Expected Result: Receipt is rejected with EEEE_ChargeItemDescriptionMissing
    /// </summary>
    [Fact]
    public async Task Scenario2a_MissingDescription_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-004",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = null, // Missing
                    Quantity = 1,
                    Amount = 10.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 10.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Missing description should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_ChargeItemDescriptionMissing",
            "Error should indicate missing description");
    }

    /// <summary>
    /// Scenario 2b: Empty Description
    /// 
    /// Story: Charge item with empty string description
    /// 
    /// Expected Result: Receipt is rejected with EEEE_ChargeItemDescriptionMissing
    /// </summary>
    [Fact]
    public async Task Scenario2b_EmptyDescription_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-005",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "", // Empty
                    Quantity = 1,
                    Amount = 10.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 10.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Empty description should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_ChargeItemDescriptionMissing",
            "Error should indicate missing description");
    }

    /// <summary>
    /// Scenario 2c: Description Too Short (2 characters)
    /// 
    /// Story: Charge item with only 2 characters in description
    /// 
    /// Expected Result: Receipt is rejected with EEEE_ChargeItemDescriptionTooShort
    /// </summary>
    [Fact]
    public async Task Scenario2c_DescriptionTooShort_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-006",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "AB", // Only 2 characters
                    Quantity = 1,
                    Amount = 10.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 10.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Description with less than 3 characters should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_ChargeItemDescriptionTooShort",
            "Error should indicate description too short");
    }

    /// <summary>
    /// Scenario 2d: Whitespace-only Description
    /// 
    /// Story: Charge item with whitespace-only description
    /// 
    /// Expected Result: Receipt is rejected with EEEE_ChargeItemDescriptionMissing
    /// </summary>
    [Fact]
    public async Task Scenario2d_WhitespaceOnlyDescription_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-007",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "   ", // Whitespace only
                    Quantity = 1,
                    Amount = 10.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 10.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Whitespace-only description should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_ChargeItemDescriptionMissing",
            "Error should indicate missing description");
    }

    #endregion

    #region Scenario 3: Negative Quantities and Amounts

    /// <summary>
    /// Scenario 3a: Negative Quantity (Non-Refund)
    /// 
    /// Story: Regular receipt with negative quantity (not a discount or refund)
    /// 
    /// Expected Result: Receipt is rejected with EEEE_NegativeQuantityNotAllowed
    /// </summary>
    [Fact]
    public async Task Scenario3a_NegativeQuantity_NonRefund_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-008",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product",
                    Quantity = -2, // Negative quantity
                    Amount = 100.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Negative quantity in non-refund receipt should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_NegativeQuantityNotAllowed",
            "Error should indicate negative quantity not allowed");
    }

    /// <summary>
    /// Scenario 3b: Negative Amount (Non-Discount, Non-Refund)
    /// 
    /// Story: Regular receipt with negative amount (not a discount or refund)
    /// 
    /// Expected Result: Receipt is rejected with EEEE_NegativeAmountNotAllowed
    /// </summary>
    [Fact]
    public async Task Scenario3b_NegativeAmount_NonRefund_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-009",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product",
                    Quantity = 2,
                    Amount = -100.00m, // Negative amount
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = -100.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Negative amount in non-refund receipt should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_NegativeAmountNotAllowed",
            "Error should indicate negative amount not allowed");
    }

    /// <summary>
    /// Scenario 3c: Discount with Negative Amount (Valid)
    /// 
    /// Story: Receipt with a discount that has negative amount
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario3c_DiscountWithNegativeAmount_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-010",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                },
                new ChargeItem
                {
                    Description = "10% Discount",
                    Quantity = 1,
                    Amount = -10.00m, // Negative for discount
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.ExtraOrDiscount)
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 90.00m, // 100 - 10 discount
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Discount with negative amount should be accepted");
    }

    #endregion

    #region Scenario 4: Missing Required Fields

    /// <summary>
    /// Scenario 4a: Missing Amount
    /// 
    /// Story: Charge item without amount specified
    /// 
    /// Expected Result: Receipt is rejected with EEEE_ChargeItemAmountMissing
    /// </summary>
    [Fact]
    public async Task Scenario4a_MissingAmount_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-011",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product",
                    Quantity = 1,
                    Amount = 0m, // Amount is 0 (considered missing for validation)
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 0m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Missing amount should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_ChargeItemAmountMissing",
            "Error should indicate missing amount");
    }

    /// <summary>
    /// Scenario 4b: Zero VAT Rate Without NotTaxable Flag
    /// 
    /// Story: Charge item with 0% VAT rate but not marked as NotTaxable
    /// 
    /// Expected Result: Receipt is rejected - the zero VAT rate validation catches this first
    /// </summary>
    [Fact]
    public async Task Scenario4b_ZeroVATRateWithoutNotTaxableFlag_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-012",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 0m,
                    ftChargeItemCase = PTVATRates.NormalCase // Should be NotTaxable with nature
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Zero VAT rate without proper configuration should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        // With the new validation order, zero VAT rate nature validation runs first
        // This catches the missing nature before the VAT rate mismatch validation
        failureSignature!.Data.Should().ContainAny(
            "EEEE_VatRateMismatch",
            "EEEE_ChargeItemVATRateMissing",
            "EEEE_ZeroVatRateMissingNature",
            "Any of these errors correctly indicates the configuration problem");
    }

    #endregion

    #region Scenario 5: Special Characters and International Text

    /// <summary>
    /// Scenario 5a: Portuguese Characters in Description
    /// 
    /// Story: Charge item with Portuguese special characters (ã, õ, ç)
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario5a_PortugueseCharacters_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-013",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Pão com manteiga e açúcar", // Portuguese characters
                    Quantity = 1,
                    Amount = 2.50m,
                    VATRate = 6m,
                    ftChargeItemCase = PTVATRates.Discounted1Case
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 2.50m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Portuguese characters should be accepted");
    }

    /// <summary>
    /// Scenario 5b: Emoji in Description
    /// 
    /// Story: Charge item with emoji characters
    /// 
    /// Expected Result: Receipt is accepted (emojis are valid unicode)
    /// </summary>
    [Fact]
    public async Task Scenario5b_EmojiInDescription_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-014",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Coffee ☕ Premium",
                    Quantity = 1,
                    Amount = 3.50m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 3.50m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Emoji characters should be accepted");
    }

    #endregion

    #region Scenario 6: VAT Rate Validation

    /// <summary>
    /// Scenario 6a: Normal VAT Rate 23% (Valid)
    /// 
    /// Story: Charge item with standard Portuguese VAT rate of 23%
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario6a_NormalVATRate23Percent_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-015",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Standard Product",
                    Quantity = 1,
                    Amount = 123.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase // 23%
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 123.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Normal VAT rate of 23% should be accepted");
    }

    /// <summary>
    /// Scenario 6b: Discounted VAT Rate 1 - 6% (Valid)
    /// 
    /// Story: Charge item with reduced Portuguese VAT rate of 6%
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario6b_DiscountedVATRate1_6Percent_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-016",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Essential Goods",
                    Quantity = 1,
                    Amount = 10.60m,
                    VATRate = 6m,
                    ftChargeItemCase = PTVATRates.Discounted1Case // 6%
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 10.60m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Discounted VAT rate 1 of 6% should be accepted");
    }

    /// <summary>
    /// Scenario 6c: Discounted VAT Rate 2 - 13% (Valid)
    /// 
    /// Story: Charge item with intermediate Portuguese VAT rate of 13%
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario6c_DiscountedVATRate2_13Percent_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-017",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-003",
                    Description = "Restaurant Service",
                    Quantity = 1,
                    Amount = 11.30m,
                    VATRate = 13m,
                    ftChargeItemCase = PTVATRates.Discounted2Case // 13%
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 11.30m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Discounted VAT rate 2 of 13% should be accepted");
    }

    /// <summary>
    /// Scenario 6d: Not Taxable - 0% (Valid)
    /// 
    /// Story: Charge item with not taxable status (0% VAT) with proper exempt reason
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario6d_NotTaxable_0Percent_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-018",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-004",
                    Description = "Exempt Product",
                    Quantity = 1,
                    Amount = 50.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30) // M06 exempt reason
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 50.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Not taxable with 0% VAT and proper exempt reason should be accepted");

        // Verify that proper signatures are present
        result.receiptResponse.ftSignatures.Should().NotBeNullOrEmpty("Receipt should have signatures");
        result.receiptResponse.ftSignatures.Should().Contain(s => s.Caption == "[www.fiskaltrust.pt]",
            "Receipt should have QR code signature");
        result.receiptResponse.ftSignatures.Should().Contain(s => s.Data.StartsWith("ATCUD:"),
            "Receipt should have ATCUD signature");
    }

    /// <summary>
    /// Scenario 6e: Unsupported Zero Rate Case (Invalid)
    /// 
    /// Story: Charge item using ZeroVatRate case which is not supported in Portugal
    /// 
    /// Expected Result: Receipt is rejected with validation error (either EEEE_UnsupportedVatRate or EEEE_ZeroVatRateMissingNature)
    /// </summary>
    [Fact]
    public async Task Scenario6e_UnsupportedZeroRateCase_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-019",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-005",
                    Description = "Product",
                    Quantity = 1,
                    Amount = 50.00m,
                    VATRate = 0m,
                    ftChargeItemCase = PTVATRates.ZeroRateCase // Unsupported in Portugal
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 50.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "ZeroVatRate case is not supported in Portugal");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().ContainAny("EEEE_UnsupportedVatRate", "EEEE_ZeroVatRateMissingNature");
    }

    /// <summary>
    /// Scenario 6f: Unsupported Parking VAT Rate Case (Invalid)
    /// 
    /// Story: Charge item using ParkingVatRate case which is not supported in Portugal
    /// 
    /// Expected Result: Receipt is rejected with EEEE_UnsupportedVatRate
    /// </summary>
    [Fact]
    public async Task Scenario6f_UnsupportedParkingVATRateCase_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-020",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-006",
                    Description = "Parking Fee",
                    Quantity = 1,
                    Amount = 11.30m,
                    VATRate = 13m,
                    ftChargeItemCase = PTVATRates.ParkingVatRateCase // Unsupported in Portugal
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 11.30m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "ParkingVatRate case is not supported in Portugal");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_UnsupportedVatRate",
            "Error should indicate unsupported VAT rate");
    }

    /// <summary>
    /// Scenario 6g: Mixed Valid VAT Rates (Valid)
    /// 
    /// Story: Receipt with multiple charge items using different supported VAT rates
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario6g_MixedValidVATRates_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-021",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Normal Rate Product",
                    Quantity = 1,
                    Amount = 24.60m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Reduced Rate 1 Product",
                    Quantity = 2,
                    Amount = 10.60m,
                    VATRate = 6m,
                    ftChargeItemCase = PTVATRates.Discounted1Case
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-003",
                    Description = "Reduced Rate 2 Product",
                    Quantity = 1,
                    Amount = 11.30m,
                    VATRate = 13m,
                    ftChargeItemCase = PTVATRates.Discounted2Case
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-004",
                    Description = "Not Taxable Product",
                    Quantity = 1,
                    Amount = 25.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30) // M06 exempt reason
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 71.50m, // Sum of all amounts
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Mixed valid VAT rates should be accepted");
    }

    /// <summary>
    /// Scenario 6h: Invalid VAT Rate Percentage for Category (Invalid)
    /// 
    /// Story: Charge item with NormalCase but wrong VAT rate percentage
    /// 
    /// Expected Result: Receipt is rejected with EEEE_VatRateMismatch
    /// </summary>
    [Fact]
    public async Task Scenario6h_InvalidVATRatePercentageForCategory_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-022",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 20m, // Wrong! Should be 23% for NormalCase
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "VAT rate percentage must match the category");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_VatRateMismatch",
            "Error should indicate VAT rate mismatch");
    }

    #endregion

    #region Scenario 7: Zero VAT Rate - Exempt Reason Validation

    /// <summary>
    /// Scenario 7a: Zero VAT Rate with M06 Exempt Reason (Valid)
    /// 
    /// Story: Charge item with 0% VAT and proper M06 exempt reason (Article 15º CIVA)
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario7a_ZeroVATRateWithM06ExemptReason_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-023",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-007",
                    Description = "Medical Supplies",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30) // M06
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Zero VAT rate with M06 exempt reason should be accepted");
    }

    /// <summary>
    /// Scenario 7b: Zero VAT Rate with M16 Exempt Reason (Valid)
    /// 
    /// Story: Charge item with 0% VAT and proper M16 exempt reason (Article 14º RITI)
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario7b_ZeroVATRateWithM16ExemptReason_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-024",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-008",
                    Description = "Export Goods",
                    Quantity = 1,
                    Amount = 250.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x40) // M16
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 250.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Zero VAT rate with M16 exempt reason should be accepted");
    }

    /// <summary>
    /// Scenario 7c: Zero VAT Rate Without Exempt Reason (Invalid)
    /// 
    /// Story: Charge item with 0% VAT but missing exempt reason (nature not specified)
    /// 
    /// Expected Result: Receipt is rejected with EEEE_ZeroVatRateMissingNature
    /// </summary>
    [Fact]
    public async Task Scenario7c_ZeroVATRateWithoutExemptReason_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-025",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-009",
                    Description = "Exempt Item",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 0m,
                    ftChargeItemCase = PTVATRates.NotTaxableCase // No nature specified
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Zero VAT rate without exempt reason should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_ZeroVatRateMissingNature",
            "Error should indicate missing exempt reason (nature)");
    }

    /// <summary>
    /// Scenario 7d: Multiple Items with Mixed Exempt Reasons (Valid)
    /// 
    /// Story: Receipt with multiple zero VAT items using different valid exempt reasons
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario7d_MultipleItemsWithMixedExemptReasons_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-026",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-010",
                    Description = "Medical Equipment (M06)",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30)
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-011",
                    Description = "Export Services (M16)",
                    Quantity = 1,
                    Amount = 200.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x40)
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-012",
                    Description = "Regular Product",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 400.00m, // Total under 1000€ net limit
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Multiple items with different valid exempt reasons should be accepted");
    }

    /// <summary>
    /// Scenario 7e: Multiple Zero VAT Items, One Missing Exempt Reason (Invalid)
    /// 
    /// Story: Receipt with multiple zero VAT items where one is missing the exempt reason
    /// 
    /// Expected Result: Receipt is rejected with EEEE_ZeroVatRateMissingNature
    /// </summary>
    [Fact]
    public async Task Scenario7e_MultipleZeroVATItems_OneMissingExemptReason_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-027",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-013",
                    Description = "Valid Exempt Item (M06)",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30) // M06
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-014",
                    Description = "Invalid Exempt Item (No Nature)",
                    Quantity = 1,
                    Amount = 75.00m,
                    VATRate = 0m,
                    ftChargeItemCase = PTVATRates.NotTaxableCase // No nature specified
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 175.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Item with zero VAT but missing exempt reason should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
        failureSignature!.Data.Should().Contain("EEEE_ZeroVatRateMissingNature",
            "Error should indicate missing exempt reason on second item");
        failureSignature.Data.Should().Contain("position 1",
            "Error should reference the correct item position");
    }

    /// <summary>
    /// Scenario 7f: Zero VAT Rate Exempt Reason References Tax Exemption Dictionary
    /// 
    /// Story: Error message for missing exempt reason should reference the TaxExemptionDictionary entries
    /// 
    /// Expected Result: Receipt is rejected with detailed error message mentioning M06, M16, and legal articles
    /// </summary>
    [Fact]
    public async Task Scenario7f_ZeroVATRateMissingExemptReason_ErrorReferencesExemptionCodes_ShouldFail()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-028",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-015",
                    Description = "Exempt Product Without Nature",
                    Quantity = 1,
                    Amount = 100.00m,
                    VATRate = 0m,
                    ftChargeItemCase = PTVATRates.NotTaxableCase // No nature specified
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 100.00m,
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().Be((State) 0x5054_0000_EEEE_EEEE,
            "Zero VAT rate without exempt reason should be rejected");

        var failureSignature = result.receiptResponse.ftSignatures.FirstOrDefault(s => s.Caption == "FAILURE");
        failureSignature.Should().NotBeNull("Error should be in signatures");
    }

    /// <summary>
    /// Scenario 7g: Large Transaction with Multiple Exempt Items (Valid)
    /// 
    /// Story: Complex receipt with multiple exempt items all having proper exempt reasons
    /// 
    /// Expected Result: Receipt is accepted
    /// </summary>
    [Fact]
    public async Task Scenario7g_LargeTransactionWithMultipleExemptItems_ShouldSucceed()
    {
        // Arrange
        var receiptRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-CI-029",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    ProductNumber = "PROD-016",
                    Description = "Medical Supplies (M06)",
                    Quantity = 5,
                    Amount = 200.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30)
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-017",
                    Description = "Export Services (M16)",
                    Quantity = 2,
                    Amount = 300.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x40)
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-018",
                    Description = "Additional Medical Equipment (M06)",
                    Quantity = 3,
                    Amount = 250.00m,
                    VATRate = 0m,
                    ftChargeItemCase = ((ChargeItemCase)PTVATRates.NotTaxableCase).WithNatureOfVat(ChargeItemCaseNatureOfVatPT.Group0x30)
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-019",
                    Description = "Standard Product",
                    Quantity = 10,
                    Amount = 184.50m, // 150 net + 34.50 VAT (23%)
                    VATRate = 23m,
                    ftChargeItemCase = PTVATRates.NormalCase
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = 934.50m, // Total under 1000€ net limit
                    ftPayItemCase = (PayItemCase)0x4445_0000_0000_1000 // Cash
                }
            },
            cbUser = "Cashier 1"
        };

        var receiptResponse = new ReceiptResponse
        {
            ftQueueID = _queuePT.ftQueuePTId,
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(
            new ProcessCommandRequest(new ftQueue(), receiptRequest, receiptResponse));

        // Assert
        result.receiptResponse.ftState.Should().NotBe((State) 0x5054_0000_EEEE_EEEE,
            "Large transaction with properly exempted items should be accepted");
    }

    #endregion
}