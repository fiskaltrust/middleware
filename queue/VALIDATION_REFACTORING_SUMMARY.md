# Receipt Validation Refactoring Summary

## Overview
This refactoring improves the inbound validation structure for Portugal receipts by:
1. **Returning structured validation results** - Validation methods return `ValidationResult` objects with detailed error information
2. **Combining error messages** - All validation errors are collected and can be accessed individually or combined
3. **Creating a separate validation class** - Shared validation logic is centralized and reusable
4. **Maintaining backward compatibility** - Existing code continues to work through wrapper methods

## New Structure

### 1. `ValidationResult.cs` (New)
**Location**: `src\fiskaltrust.Middleware.Localization.QueuePT\Validation\ValidationResult.cs`

This class represents the result of validation operations with structured error information:

```csharp
public class ValidationResult
{
    public bool IsValid { get; } // True if no errors
    public List<ValidationError> Errors { get; } // Collection of errors
    
    public string GetCombinedErrorMessage(string separator = " | ")
    public IEnumerable<string> GetErrorCodes()
    
    public static ValidationResult Success()
    public static ValidationResult Failed(ValidationError error)
    public static ValidationResult Failed(string message, string? code = null)
}

public class ValidationError
{
    public string? Code { get; set; }           // Error code (e.g., "EEEE_UnsupportedVatRate")
    public string Message { get; set; }         // Human-readable message
    public ValidationSeverity Severity { get; set; } // Error, Warning, Info, Critical
    public string? Field { get; set; }          // Field name (e.g., "cbChargeItems")
    public int? ItemIndex { get; set; }         // Index in collection
    public Dictionary<string, object>? Context { get; set; } // Additional data
}

public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
```

**Benefits**:
- **Structured error data** - Error codes, field names, item indices, and context
- **Programmatic access** - Can filter, log, or handle errors based on code or severity
- **Rich context** - Additional data about what went wrong (expected vs actual values, etc.)
- **Combined or individual** - Access errors individually or as a combined message

### 2. `PortugalValidationRules.cs` (Updated)
**Location**: `src\fiskaltrust.Middleware.Localization.QueuePT\Validation\PortugalValidationRules.cs`

This static class contains all the core validation logic, now returning `ValidationResult`:

```csharp
public static class PortugalValidationRules
{
    public static ValidationResult ValidateSupportedVatRates(ReceiptRequest request)
    {
        var result = new ValidationResult();
        // ... validation logic ...
        if (error)
        {
            result.AddError(new ValidationError(message, code, field, index)
                .WithContext("VatRate", vatRate));
        }
        return result;
    }
    
    // ... other validation methods
}
```

**Validation Rules** (all return `ValidationResult`):
- `ValidateSupportedVatRates` - Ensures only supported VAT rates are used
- `ValidateSupportedChargeItemCases` - Validates service types
- `ValidateVatRateAndAmount` - Checks VAT rate category and amount calculation
- `ValidateNegativeAmountsAndQuantities` - Prevents negative values in non-refund receipts
- `ValidateReceiptBalance` - Ensures charge items sum equals pay items sum
- `ValidateUserStructure` - Validates PTUserObject format
- `ValidateUserPresenceForSignatures` - Ensures cbUser is present for signatures
- `ValidateCashPaymentLimit` - Enforces 3000€ cash payment limit
- `ValidatePosReceiptNetAmountLimit` - Enforces 1000€ net amount limit
- `ValidateOtherServiceNetAmountLimit` - Enforces 100€ OtherService limit
- `ValidateRefundHasPreviousReference` - Ensures refunds have cbPreviousReceiptReference

### 3. `ReceiptValidator.cs` (Updated)
**Location**: `src\fiskaltrust.Middleware.Localization.QueuePT\Validation\ReceiptValidator.cs`

This class provides **comprehensive validation** that combines all errors:

```csharp
var validator = new ReceiptValidator(request.ReceiptRequest);
var validationResult = validator.Validate(new ReceiptValidationContext
{
    IsRefund = isRefund,
    GeneratesSignature = true,
    IsHandwritten = false
});

if (!validationResult.IsValid)
{
    // Access combined message
    var errorMessage = validationResult.GetCombinedErrorMessage();
    
    // Or access individual errors
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"[{error.Code}] {error.Field}: {error.Message}");
        // Access context data
        if (error.Context?.ContainsKey("VatRate") == true)
        {
            var vatRate = error.Context["VatRate"];
        }
    }
}
```

**Benefits**:
- **Single validation call** - All rules checked in one method
- **All errors at once** - Users see complete validation feedback
- **Structured access** - Can log, analyze, or display errors programmatically
- **Context-aware** - Runs different validations based on receipt type

### 4. `PortugalReceiptValidation.cs` (Updated)
**Location**: `src\fiskaltrust.Middleware.Localization.QueuePT\Helpers\PortugalReceiptValidation.cs`

This legacy helper class now **delegates to PortugalValidationRules** and converts results to strings:

```csharp
public static class PortugalReceiptValidation
{
    public static string? ValidateCashPaymentLimit(ReceiptRequest request)
    {
        var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
        return result.IsValid ? null : result.GetCombinedErrorMessage();
    }
    
    // ... other methods delegate similarly
}
```

**Benefits**:
- Existing code continues to work without changes
- No code duplication - all logic is in PortugalValidationRules
- Easy to migrate to new approach gradually

## Updated Processors

### `ReceiptCommandProcessorPT.cs`
**Before**: Multiple separate validation calls, stopping at first error
```csharp
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
// ... many more checks
```

**After**: Single validation call with structured result
```csharp
var validator = new ReceiptValidator(request.ReceiptRequest);
var validationResult = validator.Validate(new ReceiptValidationContext
{
    IsRefund = isRefund,
    GeneratesSignature = true,
    IsHandwritten = isHandwritten
});

if (!validationResult.IsValid)
{
    // Get combined message for user
    request.ReceiptResponse.SetReceiptResponseError(validationResult.GetCombinedErrorMessage());
    
    // Optionally log structured errors
    foreach (var error in validationResult.Errors)
    {
        _logger.LogWarning($"[{error.Code}] {error.Field}: {error.Message}");
    }
    
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

## Error Information Structure

Each validation error now includes:

### Basic Information
- **Message**: Human-readable description
- **Code**: Programmatic identifier (e.g., "EEEE_UnsupportedVatRate")

### Location Information
- **Field**: The property that failed (e.g., "cbChargeItems.VATRate")
- **ItemIndex**: Position in collection (e.g., 0 for first charge item)

### Context Data
Additional structured information about the error:
```csharp
error.Context = {
    "VatRate": "ZeroVatRate",
    "ExpectedVatRate": 23.0,
    "ActualVatRate": 0.0,
    "TotalCashAmount": 3500.0,
    "Limit": 3000.0
}
```

### Example Structured Error
```csharp
var error = new ValidationError(
    message: "EEEE_Charge item at position 0 uses unsupported VAT rate...",
    code: "EEEE_UnsupportedVatRate",
    field: "cbChargeItems",
    itemIndex: 0
)
.WithContext("VatRate", "ZeroVatRate")
.WithContext("SupportedRates", new[] { "RED", "INT", "NOR", "ISE" });
```

## Migration Path

### For New Code (Recommended)
Use `ReceiptValidator` for comprehensive validation with structured results:
```csharp
var validator = new ReceiptValidator(request);
var result = validator.Validate(context);

if (!result.IsValid)
{
    // Access combined message
    var message = result.GetCombinedErrorMessage();
    
    // Or process errors individually
    foreach (var error in result.Errors)
    {
        if (error.Code == "EEEE_UnsupportedVatRate")
        {
            // Handle specific error type
        }
    }
}
```

### For Advanced Scenarios
Use `PortugalValidationRules` directly for fine-grained control:
```csharp
var result = PortugalValidationRules.ValidateSupportedVatRates(request);
if (!result.IsValid)
{
    // Access structured errors
    foreach (var error in result.Errors)
    {
        _telemetry.TrackError(error.Code, error.Context);
    }
}
```

### For Existing Code
Continue using `PortugalReceiptValidation` (strings):
```csharp
var error = PortugalReceiptValidation.ValidateCashPaymentLimit(request);
if (error != null)
{
    // Handle error string
}
```

## Benefits of This Approach

1. **Rich Error Information**: Error codes, field names, indices, context data
2. **Better UX**: Users see all errors at once with precise information
3. **Programmatic Access**: Can filter, log, or analyze errors by code or severity
4. **Extensibility**: Easy to add new context data or error properties
5. **Backward Compatible**: Existing code works without changes
6. **Testability**: Can assert on specific error codes and context
7. **Maintainability**: Single source of truth for validation logic
8. **Telemetry-Friendly**: Structured data perfect for logging and monitoring

## Files Changed

### New Files
- `src\fiskaltrust.Middleware.Localization.QueuePT\Validation\ValidationResult.cs`

### Modified Files
- `src\fiskaltrust.Middleware.Localization.QueuePT\Validation\PortugalValidationRules.cs` - Returns ValidationResult
- `src\fiskaltrust.Middleware.Localization.QueuePT\Validation\ReceiptValidator.cs` - Returns ValidationResult
- `src\fiskaltrust.Middleware.Localization.QueuePT\Helpers\PortugalReceiptValidation.cs` - Converts ValidationResult to string
- `src\fiskaltrust.Middleware.Localization.QueuePT\Processors\ReceiptCommandProcessorPT.cs` - Uses ValidationResult
- `src\fiskaltrust.Middleware.Localization.QueuePT\Processors\InvoiceCommandProcessorPT.cs` - Uses ValidationResult

## Testing Recommendations

1. **Unit Tests**: Test each validation rule and assert on error codes and context
2. **Integration Tests**: Verify ValidationResult correctly combines multiple errors
3. **Regression Tests**: Ensure existing validation behavior is preserved
4. **Context Tests**: Validate that context data is correctly populated
5. **Severity Tests**: Ensure appropriate severity levels are assigned
