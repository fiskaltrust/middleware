# Receipt Validation Usage Examples

This document provides practical examples of how to use the new validation system with structured `ValidationResult`.

## Using the ReceiptValidator (Recommended for New Code)

### Example 1: Basic Validation with Structured Result

```csharp
using fiskaltrust.Middleware.Localization.QueuePT.Validation;

public Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
{
    // Determine receipt characteristics
    var isRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund);
    var isHandwritten = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten);
    
    // Create validator and validate
    var validator = new ReceiptValidator(request.ReceiptRequest);
    var validationResult = validator.Validate(new ReceiptValidationContext
    {
        IsRefund = isRefund,
        GeneratesSignature = true,
        IsHandwritten = isHandwritten
    });
    
    // Handle validation errors
    if (!validationResult.IsValid)
    {
        // Simple approach: Get combined error message
        var errorMessage = validationResult.GetCombinedErrorMessage();
        request.ReceiptResponse.SetReceiptResponseError(errorMessage);
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }
    
    // Continue with receipt processing...
}
```

### Example 2: Accessing Structured Error Information

```csharp
var validator = new ReceiptValidator(request.ReceiptRequest);
var validationResult = validator.Validate(context);

if (!validationResult.IsValid)
{
    // Access individual errors
    foreach (var error in validationResult.Errors)
    {
        // Log with structured data
        _logger.LogWarning(
            "Validation failed: [{Code}] {Field} at index {Index}: {Message}",
            error.Code,
            error.Field,
            error.ItemIndex,
            error.Message
        );
        
        // Access context data
        if (error.Context != null)
        {
            foreach (var kvp in error.Context)
            {
                _logger.LogDebug("  {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }
    }
    
    // Send telemetry with error codes
    _telemetry.TrackValidationFailure(
        validationResult.GetErrorCodes(),
        validationResult.Errors.Count
    );
    
    // Return combined message to user
    request.ReceiptResponse.SetReceiptResponseError(
        validationResult.GetCombinedErrorMessage()
    );
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

### Example 3: Filtering by Error Code

```csharp
var validationResult = validator.Validate(context);

if (!validationResult.IsValid)
{
    // Check for specific error types
    var vatErrors = validationResult.Errors
        .Where(e => e.Code?.StartsWith("EEEE_Vat") == true)
        .ToList();
    
    if (vatErrors.Any())
    {
        _logger.LogError("VAT-related validation errors detected: {Count}", vatErrors.Count);
        
        // Special handling for VAT errors
        foreach (var error in vatErrors)
        {
            if (error.Context?.ContainsKey("ActualVatRate") == true)
            {
                var actualRate = error.Context["ActualVatRate"];
                var expectedRate = error.Context["ExpectedVatRate"];
                // ... custom logic
            }
        }
    }
    
    // Return all errors
    request.ReceiptResponse.SetReceiptResponseError(
        validationResult.GetCombinedErrorMessage()
    );
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

### Example 4: Custom Error Formatting

```csharp
var validationResult = validator.Validate(context);

if (!validationResult.IsValid)
{
    // Format errors with more detail
    var formattedErrors = validationResult.Errors.Select(e =>
    {
        var parts = new List<string> { e.Message };
        
        if (e.ItemIndex.HasValue)
        {
            parts.Add($"(Item {e.ItemIndex + 1})");
        }
        
        if (e.Context?.ContainsKey("Limit") == true)
        {
            parts.Add($"Limit: {e.Context["Limit"]}");
        }
        
        return string.Join(" ", parts);
    });
    
    var errorMessage = string.Join("\n", formattedErrors);
    request.ReceiptResponse.SetReceiptResponseError(errorMessage);
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

## Using Individual Validation Rules

### Example 5: Validate Specific Aspects with Structured Results

```csharp
using fiskaltrust.Middleware.Localization.QueuePT.Validation;

// Validate only VAT rates
var vatResult = PortugalValidationRules.ValidateSupportedVatRates(receiptRequest);
if (!vatResult.IsValid)
{
    // Access structured error information
    foreach (var error in vatResult.Errors)
    {
        Console.WriteLine($"VAT Error at index {error.ItemIndex}: {error.Message}");
        Console.WriteLine($"  VAT Rate: {error.Context["VatRate"]}");
    }
}

// Validate only cash payment limit
var cashResult = PortugalValidationRules.ValidateCashPaymentLimit(receiptRequest);
if (!cashResult.IsValid)
{
    var error = cashResult.Errors.First();
    var amount = error.Context["TotalCashAmount"];
    var limit = error.Context["Limit"];
    Console.WriteLine($"Cash payment of {amount} exceeds limit of {limit}");
}
```

### Example 6: Custom Validation Combination

```csharp
// Create a custom validation set
var result = new ValidationResult();

// Only validate financial aspects
result.Merge(PortugalValidationRules.ValidateReceiptBalance(receiptRequest));
result.Merge(PortugalValidationRules.ValidateCashPaymentLimit(receiptRequest));
result.Merge(PortugalValidationRules.ValidatePosReceiptNetAmountLimit(receiptRequest));

if (!result.IsValid)
{
    // Get error codes for analytics
    var errorCodes = result.GetErrorCodes().ToList();
    _analytics.TrackEvent("FinancialValidationFailed", new { Codes = errorCodes });
    
    // Return combined message
    var message = result.GetCombinedErrorMessage(" | ");
    return ValidationResult.Failed(message);
}
```

### Example 7: Adding Custom Context

```csharp
var result = new ValidationResult();

// Custom validation with context
if (receiptRequest.cbChargeItems.Any(ci => ci.Amount > 10000))
{
    var error = new ValidationError(
        "Individual line items exceed recommended limit",
        "CUSTOM_HighValueItem",
        "cbChargeItems"
    )
    .WithContext("MaxAmount", receiptRequest.cbChargeItems.Max(ci => ci.Amount))
    .WithContext("RecommendedLimit", 10000m)
    .WithContext("ItemCount", receiptRequest.cbChargeItems.Count(ci => ci.Amount > 10000));
    
    result.AddError(error);
}

// Merge with standard validations
result.Merge(PortugalValidationRules.ValidateVatRateAndAmount(receiptRequest));

return result;
```

## Using Legacy PortugalReceiptValidation (Backward Compatible)

### Example 8: Existing Code Continues to Work

```csharp
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;

// Old code using individual validations (returns string?)
var vatRateError = PortugalReceiptValidation.ValidateSupportedVatRates(request.ReceiptRequest);
if (vatRateError != null)
{
    request.ReceiptResponse.SetReceiptResponseError(vatRateError);
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}

var chargeItemCaseError = PortugalReceiptValidation.ValidateSupportedChargeItemCases(request.ReceiptRequest);
if (chargeItemCaseError != null)
{
    request.ReceiptResponse.SetReceiptResponseError(chargeItemCaseError);
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
// ... continues to work exactly as before
```

## Validation Result Examples

### Example 9: Successful Validation
```csharp
var result = validator.Validate(context);
// result.IsValid == true
// result.Errors.Count == 0
// result.GetCombinedErrorMessage() == ""
```

### Example 10: Single Validation Error
```csharp
var result = validator.Validate(context);
// result.IsValid == false
// result.Errors.Count == 1
// result.Errors[0].Code == "EEEE_CashPaymentExceedsLimit"
// result.Errors[0].Message == "EEEE_Individual cash payment exceeds..."
// result.Errors[0].Field == "cbPayItems"
// result.Errors[0].Context["TotalCashAmount"] == 3500m
// result.Errors[0].Context["Limit"] == 3000m
```

### Example 11: Multiple Validation Errors
```csharp
var result = validator.Validate(context);
// result.IsValid == false
// result.Errors.Count == 3

// Combined message:
// "EEEE_Charge item at position 0 uses unsupported VAT rate 'ZeroVatRate' | 
//  EEEE_Receipt is not balanced: Sum of charge items (150.00) does not match sum of pay items (145.00) | 
//  EEEE_cbUser is mandatory for all receipts that generate signatures"

// Individual error access:
var vatError = result.Errors[0];
// vatError.Code == "EEEE_UnsupportedVatRate"
// vatError.ItemIndex == 0
// vatError.Context["VatRate"] == "ZeroVatRate"

var balanceError = result.Errors[1];
// balanceError.Code == "EEEE_ReceiptNotBalanced"
// balanceError.Context["Difference"] == 5.00m

var userError = result.Errors[2];
// userError.Code == "EEEE_UserRequiredForSignatures"
// userError.Field == "cbUser"
```

## Advanced Scenarios

### Example 12: Unit Testing with Structured Results

```csharp
[Test]
public void ValidateCashPaymentLimit_ExceedsLimit_ReturnsStructuredError()
{
    // Arrange
    var request = CreateRequestWithCashPayment(3500m);
    
    // Act
    var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
    
    // Assert
    Assert.IsFalse(result.IsValid);
    Assert.AreEqual(1, result.Errors.Count);
    
    var error = result.Errors[0];
    Assert.AreEqual("EEEE_CashPaymentExceedsLimit", error.Code);
    Assert.AreEqual("cbPayItems", error.Field);
    Assert.AreEqual(3500m, error.Context["TotalCashAmount"]);
    Assert.AreEqual(3000m, error.Context["Limit"]);
}
```

### Example 13: Telemetry Integration

```csharp
var validationResult = validator.Validate(context);

if (!validationResult.IsValid)
{
    // Track each error with context
    foreach (var error in validationResult.Errors)
    {
        _telemetry.TrackEvent("ValidationError", new Dictionary<string, object>
        {
            ["ErrorCode"] = error.Code ?? "Unknown",
            ["Field"] = error.Field ?? "Unknown",
            ["ItemIndex"] = error.ItemIndex ?? -1,
            ["Severity"] = error.Severity.ToString(),
            ["Message"] = error.Message,
            ["Context"] = error.Context != null 
                ? JsonSerializer.Serialize(error.Context) 
                : null
        });
    }
    
    // Track summary
    _telemetry.TrackMetric("ValidationErrorCount", validationResult.Errors.Count);
}
```

### Example 14: Localized Error Messages

```csharp
var validationResult = validator.Validate(context);

if (!validationResult.IsValid)
{
    var localizedErrors = validationResult.Errors.Select(error =>
    {
        // Translate based on error code
        var localizedMessage = _localizationService.Translate(
            error.Code ?? "GenericError",
            error.Context
        );
        
        return localizedMessage;
    });
    
    var message = string.Join(" | ", localizedErrors);
    request.ReceiptResponse.SetReceiptResponseError(message);
}
```

## Best Practices

1. **Use ValidationResult for new code** - Access structured error information
2. **Log error codes and context** - Better debugging and analytics
3. **Test error codes** - Assert on specific validation failures
4. **Preserve context data** - Helps with troubleshooting
5. **Handle errors gracefully** - Provide clear feedback to users
6. **Track validation metrics** - Monitor validation failure patterns
7. **Document custom error codes** - If adding new validations
