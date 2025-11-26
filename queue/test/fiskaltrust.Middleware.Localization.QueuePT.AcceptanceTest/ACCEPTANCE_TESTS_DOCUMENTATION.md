# Acceptance Tests Documentation
## fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest

This document provides comprehensive documentation of all acceptance tests for the Portuguese localization of the fiskaltrust middleware.

---

## Data Model Overview

### Core Request Model: ReceiptRequest

The `ReceiptRequest` is the primary data structure used by the Portuguese middleware to process fiscal receipts. It follows the fiskaltrust interface specification (ifPOS v2).

**Key Properties:**
```json
{
  "ftCashBoxID": "guid",           // Unique identifier for the cash box
  "ftQueueID": "guid",             // Queue identifier (optional, defaults to cashbox)
  "cbReceiptReference": "string",  // Unique reference from POS system
  "cbReceiptMoment": "ISO8601",    // Transaction timestamp
  "ftReceiptCase": "long",         // Receipt type and flags (0x5054_xxxx_xxxx_xxxx)
  "cbChargeItems": [...],          // Array of items sold/charged
  "cbPayItems": [...],             // Array of payment methods
  "cbUser": "string",              // User who created the receipt (min 3 chars)
  "cbCustomer": {...},             // Customer information (optional)
  "cbPreviousReceiptReference": "string or array", // Reference to previous receipt(s)
  "ftReceiptCaseData": {...},      // Additional PT-specific data
  "Currency": "EUR"                // Currency (only EUR supported)
}
```

### Charge Items Model

```json
{
  "Position": 1,                    // Line number (optional)
  "Quantity": 1.0,                  // Item quantity
  "Description": "Product Name",    // Item description (min 3 chars)
  "Amount": 100.00,                 // Total amount (gross)
  "VATRate": 23.0,                  // VAT percentage
  "ftChargeItemCase": "long",       // Item type and VAT category (0x5054_xxxx_xxxx_xxxx)
  "ProductNumber": "SKU123",        // Product identifier (optional)
  "ProductBarcode": "string",       // Barcode (optional)
  "Unit": "piece",                  // Unit of measure (optional)
  "UnitPrice": 100.00,              // Price per unit (optional)
  "UnitQuantity": 1.0,              // Quantity per unit (optional)
  "Moment": "ISO8601"               // Item timestamp (optional)
}
```

### Pay Items Model

```json
{
  "Position": 1,                    // Line number (optional)
  "Quantity": 1.0,                  // Payment quantity (usually 1)
  "Description": "Cash",            // Payment method description
  "Amount": 100.00,                 // Payment amount
  "ftPayItemCase": "long",          // Payment type (0x5054_xxxx_xxxx_xxxx)
  "Moment": "ISO8601",              // Payment timestamp (optional)
  "MoneyGroup": 1,                  // Money group identifier (optional)
  "MoneyGroupKey": "string"         // Money group key (optional)
}
```

### Customer Model

```json
{
  "CustomerVATId": "199998132",     // Portuguese NIF (9 digits with check digit)
  "CustomerName": "Company Name",   // Customer name
  "CustomerStreet": "Street",       // Street address
  "CustomerZip": "1050-189",        // Postal code
  "CustomerCity": "Lisboa",         // City
  "CustomerCountry": "PT",          // Country code
  "CustomerId": "string",           // Internal customer ID (optional)
  "CustomerType": "string"          // Customer type (optional)
}
```

### Response Model: ReceiptResponse

```json
{
  "ftCashBoxID": "guid",
  "ftQueueID": "guid",
  "ftQueueItemID": "guid",
  "ftQueueRow": "long",
  "cbTerminalID": "string",
  "cbReceiptReference": "string",
  "ftReceiptIdentification": "ft0#FS ft20257d14/1",  // PT-specific receipt ID
  "ftReceiptMoment": "ISO8601",
  "ftReceiptHeader": ["line1", "line2"],  // Header lines for printing
  "ftChargeItems": [...],           // Processed charge items
  "ftPayItems": [...],              // Processed pay items
  "ftSignatures": [...],            // Signatures including QR codes, ATCUD, errors
  "ftState": "long",                // State flags (0x5054_xxxx_xxxx_xxxx)
  "ftStateData": "string"           // JSON with additional state data
}
```

### Receipt Case Flags (ftReceiptCase)

Portuguese receipts use the format: `0x5054_RRRR_0000_FFFF`
- `0x5054`: Country code for Portugal
- `RRRR`: Receipt type (0x0001, 0x1001, etc.)
- `FFFF`: Flags (Void: 0x0001, Refund: 0x0002, etc.)

**Common Receipt Cases:**
- `0x5054_0000_0000_0001`: Unknown Receipt
- `0x5054_0000_0000_0001`: Point of Sale Receipt (POS)
- `0x5054_0000_0000_0002`: Payment Transfer
- `0x5054_0000_0000_0005`: Delivery Note / Proforma
- `0x5054_0000_0000_0006`: Table Check / Consultation
- `0x5054_0000_0001_0000`: Invoice (Unknown type)
- `0x5054_0000_0001_0001`: Invoice B2C
- `0x5054_0000_0001_0002`: Invoice B2B
- `0x5054_0000_0001_0003`: Invoice B2G
- `0x5054_0000_0003_0010`: Copy Receipt

### Charge Item Cases (ftChargeItemCase)

Format: `0x5054_VVVV_0000_NNNN`
- `VVVV`: VAT rate category
- `NNNN`: Nature/exemption reason (for 0% VAT)

**VAT Rate Cases:**
- `0x5054_0000_0000_0003`: Normal rate (23%)
- `0x5054_0000_0000_0001`: Reduced rate 1 (6%)
- `0x5054_0000_0000_0002`: Reduced rate 2 (13%)
- `0x5054_0000_0000_0008`: Not taxable (0% with exemption reason)

**Additional Flags:**
- `0x0010`: Refund item (bit 4)
- `0x0020`: Extra or discount (bit 5)

---

## Table of Contents

1. [Data Model Overview](#data-model-overview)
2. [General Receipt Scenarios](#general-receipt-scenarios)
3. [Void Receipt Scenarios](#void-receipt-scenarios)
4. [Refund Receipt Scenarios](#refund-receipt-scenarios)
5. [Partial Refund Scenarios](#partial-refund-scenarios)
6. [Copy Receipt Scenarios](#copy-receipt-scenarios)
7. [Receipt Case: 0x0001 - Point of Sale Receipt](#receipt-case-0x0001---point-of-sale-receipt)
8. [Receipt Case: 0x0002 - Payment Transfer](#receipt-case-0x0002---payment-transfer)
9. [Receipt Case: 0x100x - Invoice Scenarios](#receipt-case-0x100x---invoice-scenarios)
10. [Receipt Case: 0x0006 - Table Check](#receipt-case-0x0006---table-check)
11. [Charge Item Validation Tests](#charge-item-validation-tests)
12. [Full Certification Scenarios](#full-certification-scenarios)

---

## General Receipt Scenarios

**Test File:** `GeneralScenarios.cs`

### Scenario 1: Transactions Without User
**Test:** `Scenario1_TransactionWithoutUser_ShouldFail`

**Description:** Validates that all fiscalization-relevant receipts require a user (cbUser) to be specified.

**JSON Sample:**
```json
{
    "cbReceiptReference": "550e8400-e29b-41d4-a716-446655440000",
    "cbReceiptMoment": "2025-01-15T10:30:00",
    "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": 20,
            "Description": "test product",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 20,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ]
    // Note: cbUser is missing
}
```

**What the Test Does:**
1. Creates a receipt request without the `cbUser` field
2. Attempts to process the receipt through the Portuguese middleware
3. Validates that the middleware rejects the receipt with an error state
4. Checks that the error indicates missing user information

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

**Business Rule:** Portuguese fiscal regulations require that every fiscally relevant transaction be attributed to a specific user for audit trail purposes.

---

### Scenario 2: Transactions With Short User Name
**Test:** `Scenario2_TransactionWithoutUserWithShortLength_ShouldFail`

**Description:** Validates that the cbUser field must contain at least 3 characters.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": 20,
            "Description": "test product",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 20,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "St"  // Only 2 characters - invalid
}
```

**What the Test Does:**
1. Creates a receipt with a user name containing only 2 characters
2. Processes the receipt
3. Validates rejection with specific error state
4. Ensures the middleware enforces the minimum length requirement

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

**Business Rule:** User identification must be meaningful and contain at least 3 characters to ensure proper audit trails.

---

### Scenario 3: Transactions With Short Article Description
**Test:** `Scenario3_TransactionWithShortArticleDescription_ShouldFail`

**Description:** Validates that charge item descriptions must contain at least 3 characters.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": 20,
            "Description": "te",  // Only 2 characters - invalid
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 20,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert"
}
```

**What the Test Does:**
1. Creates a receipt with a charge item description containing only 2 characters
2. Processes the receipt
3. Validates rejection with specific error state
4. Ensures the middleware enforces the minimum description length

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

**Business Rule:** Article descriptions must be sufficiently detailed (minimum 3 characters) to identify the product or service for audit and tax purposes.

---

### Scenario 4: Transactions With Negative Amount
**Test:** `Scenario4_TransactionWithNegativeAmount_ShouldFail`

**Description:** Validates that normal sales receipts cannot contain charge items with negative amounts (except for discounts/refunds with appropriate flags).

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": -20,  // Negative amount without discount flag
            "Description": "Test",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 20,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert"
}
```

**What the Test Does:**
1. Creates a receipt with a negative amount charge item
2. The charge item case does NOT have the discount flag (0x0020)
3. Attempts to process the receipt
4. Validates that the middleware rejects it

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

**Business Rule:** Negative amounts are only allowed for specific operations like discounts, refunds, or returns when the appropriate flags are set in `ftChargeItemCase`.

---

### Scenario 5: Transactions With Negative Quantity
**Test:** `Scenario5_TransactionWithNegativeQuantity_ShouldFail`

**Description:** Validates that normal sales receipts cannot contain charge items with negative quantities.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Quantity": -1,  // Negative quantity
            "Amount": 20,
            "Description": "Test",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 20,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert"
}
```

**What the Test Does:**
1. Creates a receipt with a negative quantity
2. The ftChargeItemCase does NOT have the refund flag (0x0010)
3. Processes the receipt
4. Validates that the middleware rejects negative quantities without proper flags

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

**Business Rule:** Negative quantities are only allowed for refunds or returns when the appropriate flags are set. Normal sales must have positive quantities.

---

### Scenario 6: Transactions With Illegal VAT Rate
**Test:** `Scenario6_TransactionWithIllegalVATRate_ShouldFail`

**Description:** Validates that only official Portuguese VAT rates are accepted.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Quantity": 1,
            "Amount": 20,
            "Description": "Test",
            "VATRate": 22,  // Invalid rate - not a Portuguese VAT rate
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 20,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert"
}
```

**What the Test Does:**
1. Creates a receipt with a VAT rate of 22%
2. 22% is not a valid Portuguese VAT rate
3. Processes the receipt
4. Validates that the middleware rejects invalid VAT rates

**Valid Portuguese VAT Rates (Continent):**
- **23%** - Normal rate (Taxa Normal)
- **13%** - Intermediate rate (Taxa Intermédia)
- **6%** - Reduced rate (Taxa Reduzida)
- **0%** - Exempt (with proper exemption reason codes)

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

**Business Rule:** Only officially recognized Portuguese VAT rates are allowed. Using incorrect rates can lead to tax reporting errors and legal issues.

---

### Scenario 7: Transactions With Excessive Discount
**Test:** `Scenario7_TransactionWithDiscountExceedingTotal_ShouldFail`

**Description:** Validates that discounts cannot exceed the total amount of line items they apply to.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": 55,
            "Quantity": 100,
            "Description": "Article 1",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        },
        {
            "Amount": -55.84,  // Discount exceeds line item amount (55.00)
            "Description": "Desconto",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450280979  // Discount flag (0x0020)
        },
        {
            "Amount": 13.8,
            "Description": "Line item 2",
            "VATRate": 23,
            "Quantity": 4,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 12.96,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert",
    "cbCustomer": {
        "CustomerVATId": "123456789"
    }
}
```

**What the Test Does:**
1. Creates a receipt with Article 1 for €55.00
2. Applies a discount of €55.84 (exceeds the article amount)
3. Adds a second article for €13.80
4. Attempts to process the receipt
5. Validates that the middleware detects the excessive discount

**Calculation:**
- Article 1: €55.00
- Discount: -€55.84 (exceeds by €0.84)
- Article 2: €13.80
- Total: €12.96 (but discount is invalid)

**Expected Result:** Receipt is rejected with error state.

**Business Rule:** Discounts must not result in a negative total for the affected line items. This prevents accounting errors and potential fraud.

---

### Scenario 8: Transactions With Charge/Pay Item Mismatch
**Test:** `Scenario8_TransactionWithMismatchForChargeItems`

**Description:** Validates that the total of charge items must match the total of pay items.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": 55,
            "Quantity": 100,
            "Description": "Article 1",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 20,  // Only 20 paid, but charged 55
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert",
    "cbCustomer": {
        "CustomerVATId": "123456789"
    }
}
```

**What the Test Does:**
1. Creates charge items totaling €55.00
2. Creates pay items totaling only €20.00
3. Processes the receipt
4. Validates that the middleware detects the mismatch

**Calculation:**
- Total Charged: €55.00
- Total Paid: €20.00
- Difference: €35.00 (mismatch)

**Expected Result:** Receipt is rejected with error state.

**Business Rule:** The sum of all charge items must equal the sum of all pay items to ensure balanced accounting and prevent errors in fiscal reporting.

**Note:** Some receipt cases (like Payment Transfer or partial payments) may have legitimate reasons for a mismatch, but for standard POS receipts, this validation ensures data integrity.

---

### Scenario 9: Transactions With Invalid Customer NIF
**Test:** `Scenario9_TransactionWithInvalidCustomerNIF`

**Description:** Validates the Portuguese Tax Identification Number (NIF) using check digit validation according to the official Portuguese tax authority algorithm.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": 55,
            "Quantity": 100,
            "Description": "Article 1",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 55,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert",
    "cbCustomer": {
        "CustomerVATId": "123456799"  // Invalid check digit (should be 123456789)
    }
}
```

**What the Test Does:**
1. Creates a receipt with an invalid Portuguese NIF
2. The NIF "123456799" has an incorrect check digit (last digit)
3. The middleware validates the NIF using the Portuguese algorithm
4. The test verifies the specific error message about invalid NIF

**NIF Validation Algorithm:**
The Portuguese NIF uses a modulo-11 check digit algorithm:
1. First 8 digits are multiplied by sequence: 9, 8, 7, 6, 5, 4, 3, 2
2. Sum all the results
3. Calculate: 11 - (sum % 11)
4. If result is 10 or 11, check digit is 0
5. Otherwise, check digit is the result

**Example for "123456789":**
- 1×9 + 2×8 + 3×7 + 4×6 + 5×5 + 6×4 + 7×3 + 8×2 = 165
- 165 % 11 = 0
- 11 - 0 = 11 ? check digit = 0 (but the NIF shows 9, so it's invalid if it was "123456799")

**Expected Result:** Receipt is rejected with error message: 
```
EEEE_Invalid Portuguese Tax Identification Number (NIF): '123456799'. 
The NIF must be a 9-digit number with a valid check digit according to 
the Portuguese tax authority validation algorithm.
```

**Business Rule:** Portuguese NIFs must be validated according to the official tax authority algorithm to ensure data quality and prevent fraud. Invalid NIFs cannot be used for invoicing.

---

### Scenario 10: Transactions With Foreign Currency
**Test:** `Scenario10_TransactionsWithForeignCurrenciesShouldBeBlocked`

**Description:** Validates that only EUR currency is supported in Portuguese fiscal receipts.

**JSON Sample:**
```json
{
    "cbReceiptReference": "{{$guid}}",
    "cbReceiptMoment": "{{$isoTimestamp}}",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018817,
    "cbChargeItems": [
        {
            "Amount": 55,
            "Quantity": 100,
            "Description": "Article 1",
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Amount": 55,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert",
    "cbCustomer": {
        "CustomerVATId": "123456789"
    },
    "Currency": "USD"  // Invalid - only EUR is allowed
}
```

**What the Test Does:**
1. Creates a receipt with "USD" as the currency
2. Processes the receipt
3. Validates that the middleware rejects non-EUR currencies
4. Checks for the specific error message about currency support

**Expected Result:** Receipt is rejected with error message containing:
```
EEEE_OnlyEuroCurrencySupported
```

**Business Rule:** Portuguese fiscal regulations require all transactions to be recorded in Euros (EUR). Foreign currency transactions must be converted to EUR before processing, or the original currency transaction should be recorded with the EUR equivalent.

**Supported Currency:** Only "EUR" (or null/empty, which defaults to EUR)

**Rejected Currencies:** All other ISO 4217 currency codes (USD, GBP, CHF, etc.)

---

## Void Receipt Scenarios

**Test File:** `VoidScenarios.cs`

### Scenario 1: Void Without Reference
**Test:** `Scenario1_TransactionWithVoidWithNoReference_ShouldFail`

**Description:** Validates that voiding a receipt requires a reference to the original receipt.

**Tested Receipt Cases:** All major receipt cases with Void flag

**Expected Result:** Receipt is rejected with error message: `EEEE_cbPreviousReceiptReference is mandatory and must be set for this receipt.`

**Business Rule:** Void receipts must reference the specific receipt being voided for audit trail purposes.

---

### Scenario 2: Void With Missing Reference
**Test:** `Scenario2_TransactionWithVoidWithMissingReference_ShouldFail`

**Description:** Validates that the referenced receipt must exist in the queue.

**Test Data:** cbPreviousReceiptReference = "FIXED" (non-existent)

**Expected Result:** Receipt is rejected with error message: `The given cbPreviousReceiptReference 'FIXED' didn't match with any of the items in the Queue.`

---

### Scenario 3: Void With Multiple Matching References
**Test:** `Scenario3_TransactionWithVoidWithMissingReference_ShouldFail`

**Description:** Validates that the referenced receipt must be unique.

**Test Setup:** Two receipts created with identical cbReceiptReference "FIXED-scenario3"

**Expected Result:** Receipt is rejected with error message: `The given cbPreviousReceiptReference 'FIXED-scenario3' did match with more than one item in the Queue.`

**Business Rule:** Receipt references must be unique to avoid ambiguity.

---

### Scenario 4: Void Must Match Original Receipt
**Test:** `Scenario4_TransactionWithVoidWithReference_ShouldMatchOriginal`

**Description:** Validates that void receipts with charge/pay items must match the original exactly.

**Test Setup:**
- Original receipt with specific items
- Void receipt with different items

**Expected Result:** Receipt is rejected with error message containing `EEEE_VoidItemsMismatch`.

**Business Rule:** Void receipts should be empty or match the original receipt exactly.

---

### Scenario 5: Void With Multiple References
**Test:** `Scenario6_TransactionsWithVoidWithMultipleReferences_ShouldFail`

**Description:** Validates that void receipts can only reference a single receipt.

**Test Data:** cbPreviousReceiptReference = ["FIXED", "Test"]

**Expected Result:** Receipt is rejected with error message: `Voiding a receipt is only supported with single references.`

---

## Refund Receipt Scenarios

**Test File:** `RefundScenarios.cs`

### Scenario 1: Refund Without Reference
**Test:** `Scenario1_TransactionWithRefundWithNoReference_ShouldFail`

**Description:** Validates that refunding a receipt requires a reference to the original receipt.

**Expected Result:** Receipt is rejected with error message: `EEEE_cbPreviousReceiptReference is mandatory and must be set for this receipt.`

---

### Scenario 2: Refund With Missing Reference
**Test:** `Scenario2_TransactionWithRefundWithMissingReference_ShouldFail`

**Description:** Validates that the referenced receipt must exist.

**Expected Result:** Receipt is rejected with error message indicating the reference was not found.

---

### Scenario 3: Refund With Multiple Matching References
**Test:** `Scenario3_TransactionWithRefundWithMissingReference_ShouldFail`

**Description:** Validates that the referenced receipt must be unique.

**Expected Result:** Receipt is rejected with error message indicating multiple matches.

---

### Scenario 4: Refund Must Match Original
**Test:** `Scenario4_TransactionWithRefundWithReference_ShouldMatchOriginal`

**Description:** Validates that full refunds must match the original receipt exactly.

**Test Setup:**
- Original receipt with items totaling €20
- Refund receipt with items totaling €10 (mismatch)

**Expected Result:** Receipt is rejected with error message: `EEEE_Full refund does not match the original invoice. All articles from the original invoice must be properly refunded with matching quantities and amounts.`

---

### Scenario 5: Refund Already Refunded Receipt
**Test:** `Scenario5_TransactionWithRefundForAlreadyRefundedReceipt_ShouldFail`

**Description:** Validates that a receipt cannot be refunded more than once.

**Test Setup:**
1. Create original receipt
2. Create refund (succeeds)
3. Attempt second refund (fails)

**Expected Result:** Second refund is rejected with error message: `EEEE_A refund for receipt '{reference}' already exists. Multiple refunds for the same receipt are not allowed.`

---

### Scenario 6: Refund With Multiple References
**Test:** `Scenario6_TransactionsWithRefundWithMultipleReferences_ShouldFail`

**Description:** Validates that refund receipts can only reference a single receipt.

**Expected Result:** Receipt is rejected with error message: `Refunding a receipt is only supported with single references.`

---

### Scenario 7: Refund With Customer Mismatch
**Test:** `Scenario7_TransactionWithRefundWithCustomerMismatch_ShouldFail`

**Description:** Validates that full refunds must match the customer information from the original receipt.

**Test Setup:**
- Original receipt with customer "Nuno Cazeiro"
- Refund receipt with customer name "Different Customer"

**Expected Result:** Receipt is rejected with error message about refund not matching original invoice.

---

### Scenario 8: Refund Already Voided Receipt
**Test:** `Scenario8_TransactionWithRefundForAlreadyVoidedReceipt_ShouldFail`

**Description:** Validates that voided receipts cannot be refunded.

**Test Setup:**
1. Create original receipt
2. Void the receipt
3. Attempt to refund (fails)

**Expected Result:** Refund is rejected with error message: `EEEE_HasBeenVoidedAlready`.

---

## Partial Refund Scenarios

**Test File:** `ParitalRefundScenarios.cs` (note: typo in filename)

### Scenario 1: Partial Refund Without Reference
**Test:** `Scenario1_TransactionWithRefundWithNoReference_ShouldFail`

**Description:** Validates that partial refunds require a reference to the original receipt.

**Expected Result:** Receipt is rejected with error message about missing cbPreviousReceiptReference.

---

### Scenario 2: Partial Refund With Missing Reference
**Test:** `Scenario2_TransactionWithRefundWithMissingReference_ShouldFail`

**Description:** Validates that the referenced receipt must exist.

---

### Scenario 3: Partial Refund With Multiple Matching References
**Test:** `Scenario3_TransactionWithRefundWithMissingReference_ShouldFail`

**Description:** Validates that the referenced receipt must be unique.

---

### Scenario 4: Partial Refund Validation
**Test:** `Scenario4_TransactionWithRefundWithReference_ShouldMatchOriginal`

**Description:** Validates that partial refunds are checked against the original receipt.

**Test Setup:**
- Original receipt with two items (€20 + €30)
- Partial refund with single item (€40) - doesn't match

**Expected Result:** Receipt is rejected with error message about refund not matching original invoice.

---

### Scenario 5: Partial Refund Already Refunded Receipt
**Test:** `Scenario5_TransactionWithRefundForAlreadyRefundedReceipt_ShouldFail`

**Description:** Validates that a receipt cannot be partially refunded after a full refund.

---

### Scenario 6: Partial Refund With Multiple References
**Test:** `Scenario6_TransactionsWithRefundWithMultipleReferences_ShouldFail`

**Description:** Validates that partial refunds can only reference a single receipt.

**Expected Result:** Receipt is rejected with error message: `Partial refunding a receipt is only supported with single references.`

---

### Scenario 7: Mixing Refund and Non-Refund Items
**Test:** `Scenario7_MixingRefundItemsAndNoneRefundItemsInPartialRefund_ShouldFail`

**Description:** Validates that partial refund receipts cannot mix refund and non-refund items.

**Test Setup:** Receipt with both refund items (negative amounts with refund flag) and regular items

**Expected Result:** Receipt is rejected with error message: `EEEE_Partial refund contains mixed refund and non-refund items. In Portugal, it is not allowed to mix refunds with non-refunds in the same receipt.`

---

### Scenario 8: Partial Refund Already Fully Refunded Receipt
**Test:** `Scenario8_TransactionWithRefundForAlreadyRefundedReceipt_ShouldFail`

**Description:** Validates that partial refunds cannot be created for already fully refunded receipts.

---

### Scenario 9: Partial Refund Already Voided Receipt
**Test:** `Scenario9_TransactionWithPartialRefundForAlreadyVoidedReceipt_ShouldFail`

**Description:** Validates that voided receipts cannot be partially refunded.

---

## Copy Receipt Scenarios

**Test File:** `CopyReceiptScenarios.cs`

### Scenario 1: Copy Without cbPreviousReceiptReference
**Test:** `Scenario1_Printing_a_copy_without_a_cbPreviousReceiptReference_should_fail`

**Description:** Validates that printing a copy requires a reference to the original receipt.

**Receipt Case:** CopyReceiptPrintExistingReceipt (0x3010)

**Expected Result:** Receipt is rejected with error message containing `EEEE_PreviousReceiptReference`.

---

### Scenario 2: Copy With Non-Existing Reference
**Test:** `Scenario2_Printing_a_copy_without_a_non_existing_cbPreviousReceiptReference_should_fail`

**Description:** Validates that the referenced receipt must exist.

**Test Data:** cbPreviousReceiptReference = "NON_EXISTING_REFERENCE"

**Expected Result:** Receipt is rejected with error message: `The given cbPreviousReceiptReference 'NON_EXISTING_REFERENCE' didn't match with any of the items in the Queue.`

---

### Scenario 3: Copy Of Unsupported Document
**Test:** `Scenario3_PrintingCopyOfANotSupportedDocument_ShouldFail`

**Description:** Validates that only certain receipt types support copy printing.

**Unsupported Receipt Types:**
- PointOfSaleReceiptWithoutObligation (0x0003)
- ECommerce (0x0004)
- ZeroReceipt (0x2000)
- OneReceipt (0x2001)
- ShiftClosing (0x2010)
- DailyClosing (0x2011)
- MonthlyClosing (0x2012)
- YearlyClosing (0x2013)
- ProtocolUnspecified (0x3000)
- ProtocolTechnicalEvent (0x3001)
- ProtocolAccountingEvent (0x3002)
- InternalUsageMaterialConsumption (0x3003)
- Order (0x3004)
- Pay (0x3005)
- InitialOperationReceipt (0x4001)
- InitSCUSwitch (0x4011)
- FinishSCUSwitch (0x4012)

**Expected Result:** Receipt is rejected with error message: `CopyReceiptNotSupportedForType`.

---

### Scenario 4: Valid Copy Receipt
**Test:** `Scenario4_PrintingCopyOfAnExistingDocument_ShouldIncludeOriginalInStateData`

**Description:** Validates that copy receipts include the original receipt in ftStateData.

**Supported Receipt Types:**
- UnknownReceipt (0x0000)
- PointOfSaleReceipt (0x0001)
- PaymentTransfer (0x0002)
- DeliveryNote (0x0005)
- TableCheck (0x0006)
- InvoiceUnknown (0x1000)
- InvoiceB2C (0x1001)
- InvoiceB2B (0x1002)
- InvoiceB2G (0x1003)

**Expected Result:** Receipt is accepted, and ftStateData contains the original receipt response.

**Validation:** 
```csharp
stateDataJson.PreviousReceiptReference.Should().HaveCount(1);
previousReceipt.Response.cbReceiptReference.Should().Be(originalResponse.cbReceiptReference);
```

---

## Receipt Case: 0x0001 - Point of Sale Receipt

**Test File:** `ReceiptCases_0x0001_PosReceipt_Scenarios.cs`

### Scenario 9: Transaction Without Payment
**Test:** `Scenario9_TransactionWithoutPayment_ShouldFail`

**Description:** Validates that point of sale receipts must have payment items.

**Test Data:**
- Charge items: 1 item (€20)
- Pay items: empty array

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

**Business Rule:** All point of sale transactions must include payment information.

---

### Scenario 10: Transaction With Negative Payment
**Test:** `Scenario10_TransactionWithNegativePayment_ShouldFail`

**Description:** Validates that payment amounts cannot be negative in normal transactions.

**Test Data:**
- Charge items: €20
- Pay items: -€20

**Expected Result:** Receipt is rejected with error state `0x5054_2000_EEEE_EEEE`.

---

### Scenario 21: Transaction Exceeding Net Amount Limit
**Test:** `Scenario21_TransactionWithNetAmountGreaterThan1000_ShouldFail`

**Description:** Validates the Portuguese legal limit of €1000 net for simplified invoices without full customer identification.

**Test Data:**
- Amount: €1,300
- Customer: VATId provided only

**Expected Result:** Receipt is rejected with error state.

**Business Rule:** Simplified invoices (Artigo 40 CIVA) have a maximum limit of €1,000 net amount.

---

## Receipt Case: 0x0002 - Payment Transfer

**Test File:** `ReceiptCases_0x0002_PaymentTransfer_Scenarios.cs`

### Scenario 0: Valid Payment Transfer
**Test:** `Scenari0_Positive_Base`

**Description:** Validates a complete payment transfer scenario where an invoice is created with "on credit" payment and later settled.

**Original Invoice JSON (with credit payment):**
```json
{
    "cbReceiptReference": "invoice-credit-001",
    "cbReceiptMoment": "2025-01-15T14:00:00",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450022913,  // Invoice
    "cbChargeItems": [
        {
            "Quantity": 1,
            "Amount": 10,
            "Description": "Test",
            "VATRate": 23,
            "ftChargeItemCase": 3
        }
    ],
    "cbPayItems": [
        {
            "Amount": 10,
            "Description": "On Credit",
            "ftPayItemCase": 5788286605450018825  // Credit payment
        }
    ],
    "cbUser": "Stefan Kert",
    "cbCustomer": {
        "CustomerVATId": "123456789"
    }
}
```

**Payment Transfer JSON:**
```json
{
    "cbReceiptReference": "payment-transfer-001",
    "cbReceiptMoment": "2025-01-20T10:00:00",
    "ftCashBoxID": "{{cashboxid}}",
    "ftReceiptCase": 5788286605450018818,  // Payment Transfer (0x0002)
    "cbChargeItems": [
        {
            "Quantity": 1,
            "Amount": 10,
            "Description": "Receivable Settlement",
            "VATRate": 0,
            "ftChargeItemCase": 5788286605450035224  // Accounts Receivable
        }
    ],
    "cbPayItems": [
        {
            "Amount": 10,
            "Description": "Cash",
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Stefan Kert",
    "cbPreviousReceiptReference": "invoice-credit-001"
}
```

**What the Test Does:**
1. Creates an invoice with payment "on credit" (deferred payment)
2. Successfully processes the invoice
3. Creates a payment transfer receipt to settle the credit
4. The payment transfer references the original invoice
5. Uses accounts receivable charge item case (0x5054_0000_0000_0098)
6. Validates that both receipts are accepted
7. Checks that the receipt identification contains "RG" (Recibo de Pagamento)

**Expected Result:** 
- Both receipts are accepted successfully
- Payment transfer receipt ID contains "RG" (e.g., "ft1#RG ft2025a4fa/1")
- SAF-T export includes proper Payment element linking to the invoice

**Business Rule:** Payment transfers in Portugal require:
- Reference to the original invoice
- Accounts receivable charge item
- Matching amounts between original on-credit payment and transfer payment
- Generation of "Recibo de Pagamento" document

---

## Charge Item Validation Tests

**Test File:** `ChargeItemValidationAcceptanceTests.cs`

### Scenario 6d: Not Taxable - 0% VAT with Exempt Reason
**Test:** `Scenario6d_NotTaxable_0Percent_ShouldSucceed`

**Description:** Tests charge item with 0% VAT and proper M06 exempt reason (Article 15º CIVA).

**JSON Sample:**
```json
{
    "ftReceiptCase": 5788286605450022913,  // InvoiceB2C
    "cbReceiptReference": "INV-EXEMPT-001",
    "cbTerminalID": "TERM-001",
    "cbReceiptMoment": "2025-10-02T04:16:53",
    "cbChargeItems": [
        {
            "ProductNumber": "MED-001",
            "Description": "Medical Supplies",
            "Quantity": 1,
            "Amount": 100.00,
            "VATRate": 0,
            "ftChargeItemCase": 5788286605450035224  // NotTaxable with M06 nature (0x5054_0000_0000_0030)
        }
    ],
    "cbPayItems": [
        {
            "Amount": 100.00,
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Cashier 1",
    "cbCustomer": {
        "CustomerVATId": "123456789"
    }
}
```

**What the Test Does:**
1. Creates a receipt with 0% VAT item
2. The charge item case includes the M06 exempt reason (nature 0x30)
3. Validates that the middleware accepts the exempt item
4. Checks that proper signatures are generated (QR code, ATCUD)
5. Verifies that SAF-T export includes TaxExemptionReason element

**ftChargeItemCase Breakdown:**
- `0x5054_0000_0000_0008`: Base NotTaxable case
- `+ 0x30`: Nature group for M06 (Article 15º CIVA)
- `= 0x5054_0000_0000_0038` (5788286605450035224 in decimal)

**Expected Result:** Receipt is accepted with proper tax exemption documentation.

**Legal Reference:** Article 15º of CIVA (Código do IVA) - exempt operations

---

### Scenario 7g: Large Transaction With Multiple Exempt Items
**Test:** `Scenario7g_LargeTransactionWithMultipleExemptItems_ShouldSucceed`

**Description:** Tests complex receipt with multiple exempt items all having proper exempt reasons.

**JSON Sample:**
```json
{
    "ftReceiptCase": 5788286605450022913,
    "cbReceiptReference": "INV-COMPLEX-001",
    "cbTerminalID": "TERM-001",
    "cbReceiptMoment": "2025-01-15T16:00:00",
    "cbChargeItems": [
        {
            "ProductNumber": "MED-001",
            "Description": "Medical Supplies (M06)",
            "Quantity": 5,
            "Amount": 200.00,
            "VATRate": 0,
            "ftChargeItemCase": 5788286605450035224  // M06 - Article 15º CIVA
        },
        {
            "ProductNumber": "EXP-001",
            "Description": "Export Services (M16)",
            "Quantity": 2,
            "Amount": 300.00,
            "VATRate": 0,
            "ftChargeItemCase": 5788286605450035240  // M16 - Article 14º RITI (0x40)
        },
        {
            "ProductNumber": "MED-002",
            "Description": "Additional Medical Equipment (M06)",
            "Quantity": 3,
            "Amount": 250.00,
            "VATRate": 0,
            "ftChargeItemCase": 5788286605450035224  // M06 again
        },
        {
            "ProductNumber": "STD-001",
            "Description": "Standard Product",
            "Quantity": 10,
            "Amount": 184.50,  // 150 net + 34.50 VAT (23%)
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835  // Normal rate
        }
    ],
    "cbPayItems": [
        {
            "Amount": 934.50,  // Total under 1000€ net limit
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "cbUser": "Cashier 1"
}
```

**What the Test Does:**
1. Creates a complex receipt with multiple line items
2. Three items with 0% VAT, each with proper exempt reasons
3. One item with normal 23% VAT
4. Total stays under €1000 net limit for simplified invoices
5. Validates that all items are processed correctly
6. Ensures each exempt item has its own TaxExemptionReason in SAF-T

**Exempt Reason Breakdown:**
- **M06** (0x30): Medical supplies - Article 15º CIVA
- **M16** (0x40): Export services - Article 14º RITI

**Expected Result:** Receipt is accepted with all items properly documented in SAF-T export.

---

## Full Certification Scenarios

**Test File:** `FullScenarios.cs`

### Receipt 5_1: Simplified Invoice with VAT Number
**Scenario:** A simplified invoice (Article 40 of CIVA) for customer with VAT number

**JSON Sample:**
```json
{
    "cbReceiptReference": "1dadd294-0f2e-4af5-b0a4-326d9d44a34d",
    "cbReceiptMoment": "2025-10-02T04:16:53",
    "cbChargeItems": [
        {
            "Quantity": 1,
            "Description": "Line item 1",
            "Amount": 100,
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835
        }
    ],
    "cbPayItems": [
        {
            "Description": "Numerario",
            "Amount": 100,
            "ftPayItemCase": 5788286605450018817
        }
    ],
    "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
    "ftReceiptCase": 5788286605450018817,
    "cbUser": "Stefan Kert",
    "cbCustomer": {
        "CustomerName": "Nuno Cazeiro",
        "CustomerStreet": "Demo street",
        "CustomerZip": "1050-189",
        "CustomerCity": "Lissbon",
        "CustomerVATId": "199998132"
    }
}
```

**What the Test Does:**
1. Creates a simplified invoice (Fatura Simplificada)
2. Customer provides NIF (Portuguese VAT number)
3. Amount is under €1000 net limit for simplified invoices
4. Validates successful processing
5. Checks that receipt identification matches expected format
6. Verifies QR code generation for AT (Autoridade Tributária)
7. Ensures ATCUD (unique document code) is generated

**Expected Response:**
```json
{
    "ftState": "0x5054_0000_0000_0001",  // Success
    "ftReceiptIdentification": "ft0#FS ft20257d14/1",
    "ftSignatures": [
        {
            "Caption": "[www.fiskaltrust.pt]",
            "Data": "https://portalservicos.portaldasfinancas.gov.pt/..."  // QR code
        },
        {
            "Caption": "ATCUD",
            "Data": "ATCUD:ft20257d14-1"
        }
    ]
}
```

**Receipt ID Format:** "ft0#FS ft20257d14/1"
- `ft0`: Sequential counter in this test run
- `#`: Separator
- `FS`: Document type (Fatura Simplificada)
- `ft20257d14`: Series identifier (internal hash)
- `/1`: Number in this series

**Business Rule:** Simplified invoices (Article 40 CIVA) can be issued for amounts up to €1000 net with minimal customer information.

---

### Receipt 5_6: Invoice With Multiple VAT Rates
**Scenario:** Invoice with all Portuguese VAT rates

**JSON Sample:**
```json
{
    "cbReceiptReference": "a5f94391-59e1-4a03-8ebf-3e2f9b548fb8",
    "cbReceiptMoment": "2025-10-02T04:21:53",
    "cbChargeItems": [
        {
            "Quantity": 1,
            "Description": "Line item 1",
            "Amount": 100,
            "VATRate": 6,
            "ftChargeItemCase": 5788286605450018833,  // Reduced rate 1
            "Position": 1
        },
        {
            "Quantity": 1,
            "Description": "Line item 2",
            "Amount": 50,
            "VATRate": 0,
            "ftChargeItemCase": 5788286605450035224,  // Exempt M06
            "Position": 2
        },
        {
            "Quantity": 1,
            "Description": "Line item 3",
            "Amount": 25,
            "VATRate": 13,
            "ftChargeItemCase": 5788286605450018834,  // Intermediate rate
            "Position": 3
        },
        {
            "Quantity": 1,
            "Description": "Service Line item 1",
            "Amount": 12.5,
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018851,  // Normal rate (service)
            "Position": 4
        }
    ],
    "cbPayItems": [
        {
            "Description": "Numerario",
            "Amount": 187.5,
            "ftPayItemCase": 5788286605450018817,
            "Position": 1
        }
    ],
    "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
    "ftReceiptCase": 5788286605450022913,
    "cbUser": "Stefan Kert",
    "cbCustomer": {
        "CustomerName": "Nuno Cazeiro",
        "CustomerStreet": "Demo street",
        "CustomerZip": "1050-189",
        "CustomerCity": "Lissbon",
        "CustomerVATId": "199998132"
    }
}
```

**What the Test Does:**
1. Creates an invoice with all 4 Portuguese VAT scenarios
2. Line 1: 6% reduced rate for essential goods
3. Line 2: 0% exempt (M06) with TaxExemptionReason
4. Line 3: 13% intermediate rate
5. Line 4: 23% normal rate for services
6. Validates proper VAT calculation and reporting
7. Ensures TaxExemptionReason element is generated in SAF-T for line 2

**Expected SAF-T Output (excerpt):**
```xml
<Line>
    <LineNumber>2</LineNumber>
    <ProductCode>PROD-002</ProductCode>
    <ProductDescription>Line item 2</ProductDescription>
    <Quantity>1</Quantity>
    <UnitOfMeasure>piece</UnitOfMeasure>
    <CreditAmount>50.00</CreditAmount>
    <Tax>
        <TaxType>IVA</TaxType>
        <TaxCountryRegion>PT</TaxCountryRegion>
        <TaxCode>ISE</TaxCode>
        <TaxPercentage>0.00</TaxPercentage>
    </Tax>
    <TaxExemptionReason>M06 - IVA - Artigo 15.º do CIVA</TaxExemptionReason>
    <SettlementAmount>0.00</SettlementAmount>
</Line>
```

---

### Receipt 5_7: Document With Discounts
**Scenario:** Document with line discount and settlement amount

**JSON Sample:**
```json
{
    "cbReceiptReference": "548fd241-0ae1-4cef-8a67-341ba9ed3e55",
    "cbReceiptMoment": "2025-10-02T04:22:53",
    "cbChargeItems": [
        {
            "Quantity": 100,
            "Description": "Line item 1",
            "Amount": 55.00,
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835,
            "Position": 1
        },
        {
            "Quantity": 1,
            "Description": "Discount Line item 1",
            "Amount": -4.84000,
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450280979,  // Discount flag (0x0020)
            "Position": 1  // Same position as discounted item
        },
        {
            "Quantity": 1,
            "Description": "Line item 2",
            "Amount": 12.5,
            "VATRate": 23,
            "ftChargeItemCase": 5788286605450018835,
            "Position": 2
        }
    ],
    "cbPayItems": [
        {
            "Description": "Numerario",
            "Amount": 62.66,
            "ftPayItemCase": 5788286605450018817,
            "Position": 1
        }
    ],
    "ftCashBoxID": "a8466a96-aa7e-40f7-bbaa-5303e60c7943",
    "ftReceiptCase": 5788286605450018817,
    "cbUser": "Stefan Kert"
}
```

**What the Test Does:**
1. Creates receipt with 100 units at €0.55 = €55.00
2. Applies 8.8% line discount: -€4.84
3. Adds second item: €12.50
4. Total before discount: €67.50
5. After discount: €62.66
6. Validates that discount is tied to correct line (Position: 1)
7. Ensures SettlementAmount element is generated in SAF-T
8. Checks that UnitPrice reflects the discounted amount

**Discount Calculation:**
- Original: 100 × €0.55 = €55.00
- Discount: €55.00 × 8.8% = €4.84
- Net: €55.00 - €4.84 = €50.16
- Second item: €12.50
- Total: €62.66

**Expected SAF-T Output (excerpt):**
```xml
<Line>
    <LineNumber>1</LineNumber>
    <ProductDescription>Line item 1</ProductDescription>
    <Quantity>100</Quantity>
    <UnitPrice>0.5016</UnitPrice>  <!-- Reflects discount -->
    <CreditAmount>50.16</CreditAmount>
    <SettlementAmount>4.84</SettlementAmount>  <!-- Discount amount -->
</Line>
```

---

## Template Variables

The test infrastructure supports template variables for dynamic data generation:

**Available Templates:**
- `{{$guid}}`: Generates a new GUID
- `{{$isoTimestamp}}`: Generates current ISO 8601 timestamp
- `{{cashboxid}}`: Replaced with test cashbox ID
- `{{ftReceiptCase}}`: Replaced with the receipt case being tested
- `{{cbPreviousReceiptReference}}`: Replaced programmatically with previous receipt reference

**Example Usage:**
```json
{
    "cbReceiptReference": "{{$guid}}",  // Auto-generates unique GUID
    "cbReceiptMoment": "{{$isoTimestamp}}",  // Current timestamp
    "ftCashBoxID": "{{cashboxid}}",  // Test cashbox ID
    "ftReceiptCase": {{ftReceiptCase}}  // Test-specific case
}
```

---

## Portuguese VAT Rates Reference

### Standard Rates (Continent)
- **Normal Rate:** 23%
- **Intermediate Rate:** 13%
- **Reduced Rate:** 6%

### Autonomous Regions
**Madeira:**
- Normal: 22%
- Intermediate: 12%
- Reduced: 5%

**Azores:**
- Normal: 16%
- Intermediate: 9%
- Reduced: 4%

### Exempt Reasons (0% VAT)
Must use specific codes from TaxExemptionDictionary:
- **M06:** Article 15º CIVA (Exempt operations)
- **M16:** Article 14º RITI (Intra-community exempt)
- **M02-M30:** Additional codes per legal requirements

---

## Running the Tests

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or later (recommended) or VS Code with C# extension

### Running All Tests
```bash
dotnet test test/fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest/
```

### Running Specific Test Class
```bash
dotnet test --filter FullyQualifiedName~GeneralScenarios
```

### Running Specific Test Method
```bash
dotnet test --filter FullyQualifiedName~Scenario1_TransactionWithoutUser_ShouldFail
```

### Test Output
Tests produce:
- XUnit test results
- Console output with detailed error messages
- SAF-T XML files (for full scenario tests in `C:\GitHub\market-pt\doc\certification\`)

---

*Document Generated: 2025*
*Last Updated: 2025*
*Test Framework: XUnit*
*Target Framework: .NET 8.0*
