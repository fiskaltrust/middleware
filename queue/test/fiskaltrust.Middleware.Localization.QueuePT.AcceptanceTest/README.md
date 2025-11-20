# Portuguese Queue Refund Validation - Acceptance Tests

## Overview

This test suite serves as the **official baseline for certification and documentation** of the Portuguese refund validation implementation in the fiskaltrust middleware. These acceptance tests validate compliance with Portuguese fiscal regulations regarding refunds and credit notes.

## Portuguese Fiscal Regulations

### Key Requirements

1. **Full Refunds (Credit Notes - "Nota de Crédito")**
   - Must reference the original invoice via `cbPreviousReceiptReference`
   - Must contain exactly the same items as the original invoice
   - Quantities and amounts must match precisely
   - Only ONE full refund is allowed per invoice
   - Uses the `ReceiptCaseFlags.Refund` flag on the receipt case

2. **Partial Refunds**
   - Must reference the original invoice via `cbPreviousReceiptReference`
   - ALL items in the receipt must have the `ChargeItemCaseFlags.Refund` flag
   - **Mixing refund and non-refund items in the same receipt is PROHIBITED**
   - Multiple partial refunds are allowed
   - Total refunded quantity/amount cannot exceed the original invoice
   - Does NOT use the receipt case refund flag (only item-level flags)

3. **General Rules**
   - No mixing of refunds with new sales in the same receipt
   - All refund items must be traceable to an original invoice
   - Refund validation applies to all invoice types: B2C, B2B, B2G

## Test Scenarios

### Scenario 1: Simple Full Refund ?
**Business Case**: Customer returns everything for a full refund

**Test**: `Scenario1_SimpleFullRefund_ShouldSucceed`

**Steps**:
1. Create invoice: Product A (2x @ 50€), Product B (1x @ 30€)
2. Create credit note with all items

**Expected**: Credit note accepted, proper signatures generated

---

### Scenario 2: Full Refund Validation Failures ?

#### 2a: Missing Item
**Test**: `Scenario2a_FullRefund_WithMissingItem_ShouldFail`

**Error**: `EEEE_FullRefundItemsMismatch`

**Business Case**: Cashier forgets to include an item in the credit note

#### 2b: Incorrect Quantity
**Test**: `Scenario2b_FullRefund_WithIncorrectQuantity_ShouldFail`

**Error**: `EEEE_FullRefundItemsMismatch`

**Business Case**: Wrong quantity entered in credit note

#### 2c: Duplicate Full Refund
**Test**: `Scenario2c_SecondFullRefund_ShouldFail`

**Error**: `EEEE_RefundAlreadyExists`

**Business Case**: Attempt to create a second credit note for an already refunded invoice

---

### Scenario 3: Partial Refunds ?

#### 3a: Simple Partial Refund
**Test**: `Scenario3a_SimplePartialRefund_ShouldSucceed`

**Business Case**: Customer returns 1 of 5 items purchased

**Implementation**:
```csharp
// Item has refund flag, receipt case does NOT
ftReceiptCase = ReceiptCase.InvoiceB2C0x1001
ftChargeItemCase = PTVATRates.NormalCase | ChargeItemCaseFlags.Refund
```

#### 3b: Multiple Partial Refunds
**Test**: `Scenario3b_MultiplePartialRefunds_ShouldSucceed`

**Business Case**: Customer returns items across multiple transactions
- Original: 5 items
- First return: 2 items
- Second return: 2 items
- Total refunded: 4 of 5 items ?

#### 3c: Multiple Products Partial Refund
**Test**: `Scenario3c_PartialRefundMultipleProducts_ShouldSucceed`

**Business Case**: Customer returns some items from a multi-product purchase

---

### Scenario 4: Partial Refund Validation Failures ?

#### 4a: Mixed Items (CRITICAL COMPLIANCE ISSUE)
**Test**: `Scenario4a_PartialRefund_WithMixedItems_ShouldFail`

**Error**: `EEEE_MixedRefundItemsNotAllowed`

**Business Case**: Cashier tries to combine refund with new sale (PROHIBITED in Portugal)

**Example**:
```csharp
// ? NOT ALLOWED
cbChargeItems = [
    new ChargeItem { ..., ftChargeItemCase = ... | ChargeItemCaseFlags.Refund }, // Refund
    new ChargeItem { ..., ftChargeItemCase = ... }  // New sale - ILLEGAL
]
```

#### 4b: Exceeding Original Quantity
**Test**: `Scenario4b_PartialRefund_ExceedingQuantity_ShouldFail`

**Error**: `EEEE_PartialRefundExceedsOriginalQuantity`

**Business Case**: Attempting to refund more items than were purchased

#### 4c: Multiple Refunds Exceeding Total
**Test**: `Scenario4c_MultiplePartialRefunds_ExceedingTotal_ShouldFail`

**Error**: `EEEE_PartialRefundExceedsOriginalQuantity`

**Business Case**: 
- Original: 5 items
- First refund: 2 items ?
- Second refund: 4 items ? (total 6 > original 5)

#### 4d: Exceeding Original Amount
**Test**: `Scenario4d_PartialRefund_ExceedingAmount_ShouldFail`

**Error**: `EEEE_PartialRefundExceedsOriginalAmount`

**Business Case**: Attempting to refund more money than the original price

#### 4e: Missing Receipt Reference
**Test**: `Scenario4e_PartialRefund_WithoutReference_ShouldFail`

**Error**: `EEEE_MixedRefundItemsNotAllowed`

**Business Case**: Refund not linked to original invoice

---

### Scenario 5: Real-World Complex Scenarios ?

#### 5a: Restaurant - Mixed Products
**Test**: `Scenario5a_Restaurant_PartialRefund_ShouldSucceed`

**Business Case**: 
- Order: 2x Steak, 3x Salad, 1x Wine
- Return: 1x Steak (customer complaint)

#### 5b: Retail - Complete Cancellation
**Test**: `Scenario5b_Retail_CompleteOrderCancellation_ShouldSucceed`

**Business Case**: Customer cancels entire order after purchase

#### 5c: Retail - Progressive Returns
**Test**: `Scenario5c_Retail_ProgressiveReturns_ShouldSucceed`

**Business Case**:
- Original: 10 items @ 50€ = 500€
- Visit 1: Return 3 items (150€)
- Visit 2: Return 4 items (200€)
- Visit 3: Return 3 items (150€)
- Total: All 10 items refunded over 3 transactions ?

---

## Technical Implementation

### Full Refund Detection
```csharp
var isFullRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund);
```

### Partial Refund Detection
```csharp
var isPartialRefund = !isFullRefund && 
                      request.ReceiptRequest.cbChargeItems
                          .Any(item => item.IsRefund());
```

### Validation Flow

1. **Receipt Type Detection**
   - Check if receipt case has `ReceiptCaseFlags.Refund` ? Full Refund
   - Check if items have `ChargeItemCaseFlags.Refund` ? Partial Refund

2. **Full Refund Validation**
   - Verify `cbPreviousReceiptReference` exists
   - Load original invoice from database
   - Check for existing full refund
   - Validate ALL items match original (quantity & amount)
   - Accept if valid, otherwise return `EEEE_FullRefundItemsMismatch`

3. **Partial Refund Validation**
   - Verify `cbPreviousReceiptReference` exists
   - Check ALL items have refund flag (no mixing)
   - Load all existing partial refunds for this invoice
   - Calculate total refunded per product
   - Verify totals don't exceed original
   - Accept if valid, otherwise return appropriate error

### Error Messages

| Error Code | Description | Scenario |
|------------|-------------|----------|
| `EEEE_FullRefundItemsMismatch` | Full refund doesn't match original | Missing/extra items, wrong quantities/amounts |
| `EEEE_MixedRefundItemsNotAllowed` | Mixing refunds with sales | Partial refund with non-refund items |
| `EEEE_RefundAlreadyExists` | Duplicate full refund | Second credit note attempt |
| `EEEE_PartialRefundExceedsOriginalQuantity` | Quantity exceeded | Refunding more items than purchased |
| `EEEE_PartialRefundExceedsOriginalAmount` | Amount exceeded | Refunding more money than original |

---

## Running the Tests

### Command Line
```bash
dotnet test test/fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest/
```

### Visual Studio
1. Open Test Explorer
2. Navigate to `RefundValidationAcceptanceTests`
3. Run all tests or specific scenarios

### Expected Results
All tests should PASS ?

- ? Green tests = Scenario works as expected
- ? Red tests = Validation correctly rejects invalid scenarios

---

## Certification & Compliance

### Portuguese Tax Authority (AT) Requirements

These tests demonstrate compliance with:
- **Decreto-Lei n.º 28/2019** - Electronic invoicing requirements
- **Portaria n.º 195/2020** - Refund and credit note regulations
- **AT Guidelines** - Fiscal software certification requirements

### Documentation Purpose

This test suite serves as:
1. **Technical Specification** - Defines exact behavior for all refund scenarios
2. **Compliance Evidence** - Proves adherence to Portuguese regulations
3. **Regression Protection** - Prevents future changes from breaking compliance
4. **Integration Guide** - Shows POS developers how to implement refunds correctly

---

## POS Developer Integration Guide

### Creating a Full Refund (Credit Note)

```csharp
var refundRequest = new ReceiptRequest
{
    // Set refund flag on receipt case
    ftReceiptCase = ReceiptCase.InvoiceB2C0x1001 | ReceiptCaseFlags.Refund,
    
    // Link to original invoice
    cbPreviousReceiptReference = "ORIGINAL-INVOICE-REF",
    
    // Include ALL items from original with negative values
    cbChargeItems = new List<ChargeItem>
    {
        new ChargeItem
        {
            ProductNumber = "PROD-001",
            Quantity = -2,  // Negative
            Amount = -100,   // Negative
            VATRate = 23m,
            ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
        }
    }
};
```

### Creating a Partial Refund

```csharp
var partialRefundRequest = new ReceiptRequest
{
    // NO refund flag on receipt case
    ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
    
    // Link to original invoice
    cbPreviousReceiptReference = "ORIGINAL-INVOICE-REF",
    
    // Set refund flag on EACH item
    cbChargeItems = new List<ChargeItem>
    {
        new ChargeItem
        {
            ProductNumber = "PROD-001",
            Quantity = -1,  // Partial quantity
            Amount = -50,
            VATRate = 23m,
            // Refund flag on ITEM level
            ftChargeItemCase = (ChargeItemCase)(PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
        }
    }
};
```

### Common Mistakes to Avoid

? **DON'T**: Mix refunds with new sales
```csharp
cbChargeItems = [
    new ChargeItem { ..., ftChargeItemCase = ... | ChargeItemCaseFlags.Refund }, // Refund
    new ChargeItem { ..., ftChargeItemCase = ... }  // New sale - ERROR!
]
```

? **DON'T**: Create multiple full refunds for the same invoice
```csharp
// First credit note - OK
await CreateFullRefund("INV-001");

// Second credit note for same invoice - ERROR!
await CreateFullRefund("INV-001");
```

? **DON'T**: Forget to set item refund flag in partial refunds
```csharp
// Wrong - missing refund flag
ftChargeItemCase = PTVATRates.NormalCase

// Correct
ftChargeItemCase = (ChargeItemCase)(PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
```

? **DO**: Always link refunds to original invoice
```csharp
cbPreviousReceiptReference = "ORIGINAL-INVOICE-REF"  // Always required!
```

? **DO**: Validate quantities before refunding
```csharp
// Check available quantity for partial refund
var available = originalQuantity - alreadyRefunded;
if (refundQuantity > available) {
    // Error: cannot refund more than available
}
```

---

## Maintenance

### Adding New Test Scenarios

1. Create descriptive test method with scenario number
2. Add comprehensive XML documentation
3. Include business case description
4. Define clear expected results
5. Update this README with the new scenario

### Test Naming Convention

```
Scenario{Number}{Letter}_{Description}_{ShouldSucceed|ShouldFail}
```

Examples:
- `Scenario1_SimpleFullRefund_ShouldSucceed`
- `Scenario4a_PartialRefund_WithMixedItems_ShouldFail`

---

## Support & Questions

For questions about Portuguese refund validation:
- Technical: Check `RefundValidator.cs` implementation
- Business Rules: Refer to Portuguese AT documentation
- Integration: Review code examples in this README

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024 | Initial acceptance test suite |

---

## License

Copyright © fiskaltrust GmbH. All rights reserved.
