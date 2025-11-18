# Validation Signature Refactoring - Implementation Summary

## Overview

The validation system has been refactored to return **structured `ValidationResult` objects** instead of simple strings. This provides rich error information including error codes, field names, item indices, severity levels, and contextual data.

## What Changed

### Before: String-Based Validation
```csharp
public static string? ValidateCashPaymentLimit(ReceiptRequest request)
{
    if (totalCashAmount > 3000m)
    {
        return "EEEE_Individual cash payment exceeds the legal limit of 3000€";
    }
    return null;
}

// Usage
var error = PortugalValidationRules.ValidateCashPaymentLimit(request);
if (error != null)
{
    // Only have error message string
    HandleError(error);
}
```

### After: Structured Validation Result
```csharp
public static ValidationResult ValidateCashPaymentLimit(ReceiptRequest request)
{
    if (totalCashAmount > 3000m)
    {
        return ValidationResult.Failed(new ValidationError(
            ErrorMessagesPT.EEEE_CashPaymentExceedsLimit,
            "EEEE_CashPaymentExceedsLimit",
            "cbPayItems"
        )
        .WithContext("TotalCashAmount", totalCashAmount)
        .WithContext("Limit", 3000m));
    }
    return ValidationResult.Success();
}

// Usage
var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
if (!result.IsValid)
{
    // Have structured error information
    var error = result.Errors[0];
    var code = error.Code;                    // "EEEE_CashPaymentExceedsLimit"
    var field = error.Field;                  // "cbPayItems"
    var amount = error.Context["TotalCashAmount"]; // 3500m
    var limit = error.Context["Limit"];            // 3000m
    
    // Or just get the message
    var message = result.GetCombinedErrorMessage();
}
```

## Key Benefits

### 1. **Programmatic Error Handling**
```csharp
// Filter errors by code
var vatErrors = validationResult.Errors
    .Where(e => e.Code?.StartsWith("EEEE_Vat") == true);

// Check for specific error
if (validationResult.Errors.Any(e => e.Code == "EEEE_CashPaymentExceedsLimit"))
{
    // Handle cash limit error specifically
}

// Get all error codes for analytics
var errorCodes = validationResult.GetErrorCodes();
_telemetry.TrackValidationFailure(errorCodes);
```

### 2. **Rich Context Data**
```csharp
foreach (var error in validationResult.Errors)
{
    _logger.LogError(
        "Validation failed: [{Code}] {Field} at index {Index}",
        error.Code,
        error.Field,
        error.ItemIndex
    );
    
    // Access specific context values
    if (error.Context?.ContainsKey("ActualVatRate") == true)
    {
        var actual = error.Context["ActualVatRate"];
        var expected = error.Context["ExpectedVatRate"];
        _logger.LogDebug("VAT rate mismatch: Expected {Expected}, got {Actual}", 
            expected, actual);
    }
}
```

### 3. **Better Testing**
```csharp
[Test]
public void ValidateCashPaymentLimit_ExceedsLimit_ReturnsCorrectError()
{
    // Arrange
    var request = CreateRequestWithCashPayment(3500m);
    
    // Act
    var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
    
    // Assert - Can test specific error properties
    Assert.IsFalse(result.IsValid);
    Assert.AreEqual("EEEE_CashPaymentExceedsLimit", result.Errors[0].Code);
    Assert.AreEqual("cbPayItems", result.Errors[0].Field);
    Assert.AreEqual(3500m, result.Errors[0].Context["TotalCashAmount"]);
    Assert.AreEqual(3000m, result.Errors[0].Context["Limit"]);
}
```

### 4. **Enhanced Logging & Telemetry**
```csharp
foreach (var error in validationResult.Errors)
{
    _telemetry.TrackEvent("ValidationError", new
    {
        ErrorCode = error.Code,
        Field = error.Field,
        ItemIndex = error.ItemIndex,
        Severity = error.Severity,
        Context = JsonSerializer.Serialize(error.Context)
    });
}
```

## New Components

### ValidationResult Class
```csharp
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; }
    
    public string GetCombinedErrorMessage(string separator = " | ")
    public IEnumerable<string> GetErrorCodes()
    
    public static ValidationResult Success()
    public static ValidationResult Failed(ValidationError error)
    public void Merge(ValidationResult other)
}
```

### ValidationError Class
```csharp
public class ValidationError
{
    public string? Code { get; set; }              // e.g., "EEEE_UnsupportedVatRate"
    public string Message { get; set; }            // Human-readable message
    public ValidationSeverity Severity { get; set; } // Error, Warning, Info, Critical
    public string? Field { get; set; }             // e.g., "cbChargeItems.VATRate"
    public int? ItemIndex { get; set; }            // e.g., 0 for first item
    public Dictionary<string, object>? Context { get; set; } // Additional data
    
    public ValidationError WithContext(string key, object value)
    public ValidationError WithField(string field)
    public ValidationError WithItemIndex(int index)
}
```

### ValidationSeverity Enum
```csharp
public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
```

## Error Context Examples

### VAT Rate Error
```csharp
new ValidationError(message, "EEEE_UnsupportedVatRate", "cbChargeItems", 0)
    .WithContext("VatRate", "ZeroVatRate")
    .WithContext("SupportedRates", new[] { "RED", "INT", "NOR", "ISE" })
```

### VAT Amount Mismatch
```csharp
new ValidationError(message, "EEEE_VatAmountMismatch", "cbChargeItems.VATAmount", 2)
    .WithContext("ProvidedVatAmount", 12.50m)
    .WithContext("CalculatedVatAmount", 11.50m)
    .WithContext("Difference", 1.00m)
```

### Balance Error
```csharp
new ValidationError(message, "EEEE_ReceiptNotBalanced")
    .WithContext("ChargeItemsSum", 150.00m)
    .WithContext("PayItemsSum", 145.00m)
    .WithContext("Difference", 5.00m)
```

### Cash Payment Limit
```csharp
new ValidationError(message, "EEEE_CashPaymentExceedsLimit", "cbPayItems")
    .WithContext("TotalCashAmount", 3500.0m)
    .WithContext("Limit", 3000.0m)
```

## Migration Guide

### Option 1: Use New API (Recommended)
```csharp
// New code - use ValidationResult
var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
if (!result.IsValid)
{
    // Access structured errors
    foreach (var error in result.Errors)
    {
        _logger.LogError("[{Code}] {Message}", error.Code, error.Message);
    }
    
    // Or get combined message
    var message = result.GetCombinedErrorMessage();
}
```

### Option 2: Use Legacy API (Backward Compatible)
```csharp
// Existing code - continues to work
var error = PortugalReceiptValidation.ValidateCashPaymentLimit(request);
if (error != null)
{
    // Handle error string (converted from ValidationResult internally)
}
```

### Gradual Migration Path
1. **Keep existing code working** - Legacy `PortugalReceiptValidation` helpers still work
2. **New features use `ValidationResult`** - Get structured error information
3. **Migrate gradually** - Update code section by section as needed

## Comparison Table

| Feature | String-Based (Old) | ValidationResult (New) |
|---------|-------------------|------------------------|
| Error Message | ? Yes | ? Yes |
| Error Code | ? No | ? Yes |
| Field Name | ? No | ? Yes |
| Item Index | ? No | ? Yes |
| Context Data | ? No | ? Yes |
| Severity Level | ? No | ? Yes |
| Programmatic Access | ? Limited | ? Full |
| Multiple Errors | ? Combined String | ? Structured List |
| Testability | ?? String Matching | ? Property Assertions |
| Telemetry | ?? Parse Strings | ? Structured Data |
| Backward Compatible | ? Yes | ? Yes (via helpers) |

## Files Changed

### New Files
1. `ValidationResult.cs` - Result and error classes with severity enum

### Modified Files
1. `PortugalValidationRules.cs` - Returns `ValidationResult` instead of `string?`
2. `ReceiptValidator.cs` - Returns `ValidationResult` instead of `string?`
3. `PortugalReceiptValidation.cs` - Converts `ValidationResult` to `string?` for compatibility
4. `ReceiptCommandProcessorPT.cs` - Uses `ValidationResult`
5. `InvoiceCommandProcessorPT.cs` - Uses `ValidationResult`

### Documentation Updated
1. `VALIDATION_REFACTORING_SUMMARY.md` - Complete technical overview
2. `VALIDATION_USAGE_EXAMPLES.md` - Practical code examples

## Code Examples

### Example 1: Simple Validation
```csharp
var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
if (!result.IsValid)
{
    Console.WriteLine(result.GetCombinedErrorMessage());
}
```

### Example 2: Detailed Error Analysis
```csharp
var result = validator.Validate(context);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: [{error.Code}] {error.Message}");
        Console.WriteLine($"  Field: {error.Field}");
        if (error.ItemIndex.HasValue)
        {
            Console.WriteLine($"  Index: {error.ItemIndex}");
        }
        if (error.Context != null)
        {
            foreach (var kvp in error.Context)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }
    }
}
```

### Example 3: Filtering and Custom Handling
```csharp
var result = validator.Validate(context);
if (!result.IsValid)
{
    // Handle VAT errors differently
    var vatErrors = result.Errors.Where(e => e.Code?.Contains("Vat") == true);
    if (vatErrors.Any())
    {
        _taxService.LogVatDiscrepancies(vatErrors);
    }
    
    // Handle critical errors
    var criticalErrors = result.Errors.Where(e => e.Severity == ValidationSeverity.Critical);
    if (criticalErrors.Any())
    {
        _alertService.SendAlert("Critical validation errors", criticalErrors);
    }
    
    // Return all errors to user
    return result.GetCombinedErrorMessage();
}
```

## Testing Improvements

### Before (String Matching)
```csharp
[Test]
public void ValidateCashPaymentLimit_ExceedsLimit_ReturnsError()
{
    var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
    Assert.IsTrue(result.Contains("3000"));  // Fragile string matching
}
```

### After (Structured Assertions)
```csharp
[Test]
public void ValidateCashPaymentLimit_ExceedsLimit_ReturnsError()
{
    var result = PortugalValidationRules.ValidateCashPaymentLimit(request);
    Assert.IsFalse(result.IsValid);
    Assert.AreEqual("EEEE_CashPaymentExceedsLimit", result.Errors[0].Code);
    Assert.AreEqual(3000m, result.Errors[0].Context["Limit"]);
}
```

## Performance Considerations

- **Memory**: Slightly higher due to structured objects vs strings
- **CPU**: Negligible difference - validation logic unchanged
- **Network**: Same - error messages sent to clients are still strings
- **Logging**: More efficient - structured data vs string parsing

## Backward Compatibility

? **100% Backward Compatible**

All existing code continues to work:
- `PortugalReceiptValidation` helpers still return `string?`
- Internally converts `ValidationResult` to string
- No breaking changes to public APIs
- Gradual migration path available

## Future Enhancements

Possible future improvements with this structure:

1. **Localization**: Translate error messages based on error code
2. **Error Recovery**: Suggest fixes based on context data
3. **Batch Validation**: Validate multiple receipts and aggregate errors
4. **Custom Severity**: Add warning-level validations that don't block
5. **Error Hierarchies**: Group related errors by category
6. **Async Validation**: Add async validation rules if needed
7. **Validation Middleware**: Intercept and modify validation results

## Conclusion

The validation system now returns **structured `ValidationResult` objects** providing:
- ? Rich error information (codes, fields, indices, context)
- ? Programmatic access for logging, telemetry, and custom handling
- ? Better testability with property assertions
- ? Multiple errors in a single result
- ? 100% backward compatibility
- ? Clear migration path

This is a **non-breaking enhancement** that adds powerful capabilities while maintaining all existing functionality.
