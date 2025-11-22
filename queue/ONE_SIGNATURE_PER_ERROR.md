# Validation Signature: One Result Per Error

## Overview

The validation system has been refined to return **one `ValidationResult` per error** instead of collecting multiple errors into a single `ValidationResult`. This provides better granularity and allows for more precise per-error handling.

## Key Change

### Before: Multiple Errors in One Result
```csharp
public static ValidationResult ValidateSupportedVatRates(ReceiptRequest request)
{
    var result = new ValidationResult();
    
    // Collect multiple errors into one result
    foreach (var error in FindErrors())
    {
        result.AddError(error);
    }
    
    return result; // Returns one result with many errors
}
```

### After: One Result Per Error
```csharp
public static IEnumerable<ValidationResult> ValidateSupportedVatRates(ReceiptRequest request)
{
    // Yield one ValidationResult per error found
    foreach (var error in FindErrors())
    {
        yield return ValidationResult.Failed(error); // Each result has one error
    }
}
```

## Benefits

### 1. **Granular Error Handling**
Each error is independent and can be handled separately:

```csharp
var validationResults = validator.Validate(context);

foreach (var result in validationResults)
{
    // Each result represents a single error
    var error = result.Errors.Single(); // Always has exactly one error
    
    // Handle based on error code
    switch (error.Code)
    {
        case "EEEE_UnsupportedVatRate":
            _vatErrorHandler.Handle(error);
            break;
        case "EEEE_CashPaymentExceedsLimit":
            _paymentErrorHandler.Handle(error);
            break;
    }
}
```

### 2. **Streaming/Lazy Evaluation**
Using `IEnumerable` with `yield return` allows for lazy evaluation:

```csharp
// Can stop at first error if needed
var firstError = validator.Validate(context).FirstOrDefault();

// Or evaluate only what you need
var firstTwoErrors = validator.Validate(context).Take(2);

// Full evaluation only when needed
var allErrors = validator.Validate(context).ToList();
```

### 3. **Better Telemetry**
Each error can be tracked individually:

```csharp
foreach (var result in validationResults)
{
    var error = result.Errors.Single();
    
    _telemetry.TrackEvent("ValidationError", new
    {
        ErrorCode = error.Code,
        Field = error.Field,
        ItemIndex = error.ItemIndex,
        Context = JsonSerializer.Serialize(error.Context)
    });
}
```

### 4. **Clearer Intent**
The signature explicitly communicates "multiple results possible":

```csharp
// Clear: returns multiple results
IEnumerable<ValidationResult> ValidateSupportedVatRates(...)

// vs unclear: one result that might have many errors
ValidationResult ValidateSupportedVatRates(...)
```

## API Changes

### Validation Rules

**Before:**
```csharp
ValidationResult result = PortugalValidationRules.ValidateCashPaymentLimit(request);
if (!result.IsValid)
{
    // result contains all errors from this rule
}
```

**After:**
```csharp
IEnumerable<ValidationResult> results = PortugalValidationRules.ValidateCashPaymentLimit(request);
if (results.Any())
{
    // Each result contains exactly one error
    foreach (var result in results)
    {
        var error = result.Errors.Single();
        // Handle individual error
    }
}
```

### ReceiptValidator

**Before:**
```csharp
var validator = new ReceiptValidator(request);
var result = validator.Validate(context);

if (!result.IsValid)
{
    // result.Errors contains all errors from all rules
}
```

**After:**
```csharp
var validator = new ReceiptValidator(request);

// Option 1: Stream processing
foreach (var result in validator.Validate(context))
{
    var error = result.Errors.Single();
    // Process each error as it's generated
}

// Option 2: Collect all (convenience method)
var results = validator.ValidateAndCollect(context);
if (!results.IsValid)
{
    // results.AllErrors contains all errors from all rules
    // results.Results contains one ValidationResult per error
}
```

## New Helper Class

### ValidationResultCollection

A convenience class for working with multiple `ValidationResult` objects:

```csharp
public class ValidationResultCollection
{
    // All validation results (one per error)
    public IReadOnlyList<ValidationResult> Results { get; }
    
    // True if all validations passed
    public bool IsValid { get; }
    
    // All errors flattened
    public IEnumerable<ValidationError> AllErrors { get; }
    
    // Combined error message
    public string GetCombinedErrorMessage(string separator = " | ")
    
    // All unique error codes
    public IEnumerable<string> GetAllErrorCodes()
}
```

**Usage:**
```csharp
var validator = new ReceiptValidator(request);
var collection = validator.ValidateAndCollect(context);

if (!collection.IsValid)
{
    // Access individual results
    foreach (var result in collection.Results)
    {
        var error = result.Errors.Single();
        Console.WriteLine($"Error: {error.Code}");
    }
    
    // Or get combined message
    var message = collection.GetCombinedErrorMessage();
    
    // Or get all error codes
    var codes = collection.GetAllErrorCodes();
}
```

## Examples

### Example 1: Process Each Error Individually

```csharp
var validator = new ReceiptValidator(request);

foreach (var result in validator.Validate(context))
{
    var error = result.Errors.Single();
    
    // Log each error with full context
    _logger.LogError(
        "Validation failed: [{Code}] {Field} at index {Index}: {Message}",
        error.Code,
        error.Field,
        error.ItemIndex,
        error.Message
    );
    
    // Send notification for critical errors
    if (error.Severity == ValidationSeverity.Critical)
    {
        await _notificationService.SendAlert(error);
    }
}
```

### Example 2: Stop at First Error

```csharp
var validator = new ReceiptValidator(request);
var firstError = validator.Validate(context).FirstOrDefault();

if (firstError != null)
{
    var error = firstError.Errors.Single();
    return $"Validation failed: {error.Message}";
}
```

### Example 3: Collect and Analyze

```csharp
var validator = new ReceiptValidator(request);
var collection = validator.ValidateAndCollect(context);

if (!collection.IsValid)
{
    // Analyze error distribution
    var errorsByCode = collection.Results
        .SelectMany(r => r.Errors)
        .GroupBy(e => e.Code)
        .Select(g => new { Code = g.Key, Count = g.Count() });
    
    // Log summary
    _logger.LogWarning(
        "Validation failed with {ErrorCount} errors: {ErrorCodes}",
        collection.Results.Count,
        string.Join(", ", collection.GetAllErrorCodes())
    );
    
    // Return combined message
    request.ReceiptResponse.SetReceiptResponseError(
        collection.GetCombinedErrorMessage()
    );
}
```

### Example 4: Filter Specific Errors

```csharp
var validator = new ReceiptValidator(request);

// Get only VAT-related errors
var vatErrors = validator.Validate(context)
    .Where(r => r.Errors.Any(e => e.Code?.Contains("Vat") == true))
    .ToList();

if (vatErrors.Any())
{
    // Special handling for VAT errors
    foreach (var result in vatErrors)
    {
        var error = result.Errors.Single();
        _vatService.LogDiscrepancy(
            error.Context["ActualVatRate"],
            error.Context["ExpectedVatRate"]
        );
    }
}
```

### Example 5: Parallel Processing

```csharp
var validator = new ReceiptValidator(request);
var results = validator.Validate(context).ToList();

// Process errors in parallel if needed
await Task.WhenAll(results.Select(async result =>
{
    var error = result.Errors.Single();
    await _errorProcessor.ProcessAsync(error);
}));
```

## Backward Compatibility

The legacy `PortugalReceiptValidation` helpers maintain backward compatibility by collecting results:

```csharp
public static string? ValidateCashPaymentLimit(ReceiptRequest request)
{
    // Collect all results and combine error messages
    var results = PortugalValidationRules.ValidateCashPaymentLimit(request).ToList();
    return results.Any() 
        ? string.Join(" | ", results.SelectMany(r => r.Errors).Select(e => e.Message)) 
        : null;
}
```

Existing code continues to work without changes:

```csharp
// Old code still works
var error = PortugalReceiptValidation.ValidateCashPaymentLimit(request);
if (error != null)
{
    // Handle error string
}
```

## Performance Considerations

### Memory Efficiency
- **Lazy evaluation**: Errors are generated only when enumerated
- **Streaming**: Can process errors one at a time without collecting all
- **Early exit**: Can stop at first error if needed

### Example: Early Exit
```csharp
// Only generates first error
var firstError = validator.Validate(context).FirstOrDefault();

// vs collecting all errors
var allErrors = validator.Validate(context).ToList(); // Forces evaluation
```

## Migration Guide

### From Previous Version

**Old approach (multiple errors in one result):**
```csharp
var result = validator.Validate(context);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        // Handle error
    }
}
```

**New approach (one result per error):**
```csharp
// Option 1: Stream processing
foreach (var result in validator.Validate(context))
{
    var error = result.Errors.Single();
    // Handle error
}

// Option 2: Collect first
var collection = validator.ValidateAndCollect(context);
if (!collection.IsValid)
{
    foreach (var error in collection.AllErrors)
    {
        // Handle error
    }
}
```

## Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Return Type** | `ValidationResult` | `IEnumerable<ValidationResult>` |
| **Errors per Result** | Multiple | One |
| **Evaluation** | Eager | Lazy (streaming) |
| **Early Exit** | Not possible | Supported |
| **Granularity** | Coarse | Fine |
| **Memory** | All errors allocated | Generated on demand |
| **Intent** | Unclear | Clear (multiple results) |

## Key Takeaway

**One `ValidationResult` per error** provides:
- ? Better granularity for error handling
- ? Lazy evaluation and streaming support
- ? Early exit capability
- ? Clearer API intent
- ? Individual error tracking
- ? Memory efficiency
- ? Backward compatibility maintained

This design follows the principle of **single responsibility** - each `ValidationResult` represents one validation failure, making the system more modular and easier to work with.
