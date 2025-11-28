# Adding Receipt Moment Order Validation

## Summary

The `ValidateReceiptMomentOrder` validation has been added to the centralized validation system. This validation ensures that receipt moments are in chronological order for non-handwritten receipts.

## What Was Added

### New Validation Rule in `PortugalValidationRules.cs`

```csharp
/// <summary>
/// Validates that receipt moment is not earlier than the last recorded receipt moment for the series.
/// This ensures chronological order of receipts (except for handwritten receipts which may be backdated).
/// Returns a single ValidationResult if validation fails.
/// </summary>
public static IEnumerable<ValidationResult> ValidateReceiptMomentOrder(
    ReceiptRequest request, 
    object series, 
    bool isHandwritten)
{
    // Uses reflection to access NumberSeries properties
    // Returns ValidationResult with error code "EEEE_CbReceiptMomentBeforeLastMoment"
}
```

## Key Characteristics

### 1. **Context-Dependent Validation**
Unlike other validations that only need the `ReceiptRequest`, this validation requires:
- The `ReceiptRequest` - for the current receipt moment
- The `NumberSeries` object - for the last recorded moment
- The `isHandwritten` flag - handwritten receipts can be backdated

### 2. **Uses Reflection**
Since `NumberSeries` is defined in the storage layer and we want to keep validation layer independent, the validation uses reflection to access:
- `LastCbReceiptMoment` property
- `Identifier` property

### 3. **Called After Storage Initialization**
This validation must be called **after** retrieving the `NumberSeries` from storage:

```csharp
// Get series from storage first
var series = staticNumberStorage.CreditNoteSeries;

// Then validate
var momentOrderErrors = PortugalValidationRules.ValidateReceiptMomentOrder(
    request.ReceiptRequest, 
    series, 
    isHandwritten: false
).ToList();

if (momentOrderErrors.Any())
{
    request.ReceiptResponse.SetReceiptResponseError(
        momentOrderErrors.First().GetCombinedErrorMessage()
    );
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

## Usage Examples

### Example 1: Invoice Processing (Credit Note)
```csharp
if (isRefund)
{
    var series = staticNumberStorage.CreditNoteSeries;
    
    // Validate chronological order
    var momentOrderErrors = PortugalValidationRules.ValidateReceiptMomentOrder(
        request.ReceiptRequest, 
        series, 
        isHandwritten: false
    ).ToList();
    
    if (momentOrderErrors.Any())
    {
        var error = momentOrderErrors.First().Errors.Single();
        _logger.LogError("Receipt moment order validation failed: {Code}", error.Code);
        request.ReceiptResponse.SetReceiptResponseError(
            momentOrderErrors.First().GetCombinedErrorMessage()
        );
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }
    
    // Continue processing...
}
```

### Example 2: POS Receipt (Non-Handwritten)
```csharp
var series = staticNumberStorage.SimplifiedInvoiceSeries;

// Non-handwritten receipts must be in chronological order
var momentOrderErrors = PortugalValidationRules.ValidateReceiptMomentOrder(
    request.ReceiptRequest, 
    series, 
    isHandwritten: false
).ToList();

if (momentOrderErrors.Any())
{
    request.ReceiptResponse.SetReceiptResponseError(
        momentOrderErrors.First().GetCombinedErrorMessage()
    );
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

### Example 3: Handwritten Receipt (Backdating Allowed)
```csharp
var series = staticNumberStorage.HandWrittenFSSeries;

// Handwritten receipts are not validated for order
// (isHandwritten: true means validation is skipped)
var momentOrderErrors = PortugalValidationRules.ValidateReceiptMomentOrder(
    request.ReceiptRequest, 
    series, 
    isHandwritten: true  // Validation will return no errors
).ToList();
```

## Error Information

When validation fails, the error includes:

### Error Code
```
"EEEE_CbReceiptMomentBeforeLastMoment"
```

### Error Message
```
"EEEE_cbReceiptMoment (2024-01-15T10:00:00) must not be earlier than the last recorded 
cbReceiptMoment for series 'FS/2024'. Only handwritten receipts may be backdated."
```

### Context Data
```csharp
error.Context = {
    "SeriesIdentifier": "FS/2024",
    "LastReceiptMoment": DateTime,    // Last recorded moment
    "CurrentReceiptMoment": DateTime  // Current receipt moment (earlier)
}
```

### Field
```
"cbReceiptMoment"
```

## Integration Points

### Modified Files

1. **`PortugalValidationRules.cs`**
   - Added `ValidateReceiptMomentOrder` method
   - Uses reflection to access `NumberSeries` properties

2. **`ReceiptCommandProcessorPT.cs`**
   - Removed local `ValidateReceiptMomentOrder` method
   - Now calls centralized validation for:
     - `SimplifiedInvoiceSeries` (POS receipts)
     - `CreditNoteSeries` (refunds)
     - `PaymentSeries` (payment transfers)

3. **`InvoiceCommandProcessorPT.cs`**
   - Removed local `ValidateReceiptMomentOrder` method
   - Now calls centralized validation for:
     - `InvoiceSeries` (invoices)
     - `CreditNoteSeries` (credit notes)

## Why Reflection?

The validation layer (`PortugalValidationRules`) is designed to be independent of storage models. Using reflection allows:

1. **Layer Independence**: Validation doesn't need to reference storage types
2. **Type Flexibility**: Works with any object that has the required properties
3. **Future-Proofing**: New series types automatically supported

### Alternative Approaches Considered

#### Option 1: Strong Typing (Rejected)
```csharp
// Would require reference to storage models
public static IEnumerable<ValidationResult> ValidateReceiptMomentOrder(
    ReceiptRequest request, 
    NumberSeries series,  // Strong type from storage layer
    bool isHandwritten)
```
**Reason for rejection**: Creates circular dependencies between layers

#### Option 2: Interface (Rejected)
```csharp
public interface ISeriesInfo
{
    DateTime? LastCbReceiptMoment { get; }
    string Identifier { get; }
}

public static IEnumerable<ValidationResult> ValidateReceiptMomentOrder(
    ReceiptRequest request, 
    ISeriesInfo series,
    bool isHandwritten)
```
**Reason for rejection**: Requires modifying storage models to implement interface

#### Option 3: Reflection (Chosen)
```csharp
public static IEnumerable<ValidationResult> ValidateReceiptMomentOrder(
    ReceiptRequest request, 
    object series,  // Any object with the required properties
    bool isHandwritten)
```
**Reason for selection**: 
- No coupling between layers
- Works with existing code
- Properties checked at runtime

## Testing

### Unit Test Example
```csharp
[Test]
public void ValidateReceiptMomentOrder_EarlierMoment_ReturnsError()
{
    // Arrange
    var request = new ReceiptRequest
    {
        cbReceiptMoment = new DateTime(2024, 1, 15, 10, 0, 0)
    };
    
    var series = new
    {
        LastCbReceiptMoment = new DateTime(2024, 1, 15, 11, 0, 0),
        Identifier = "FS/2024"
    };
    
    // Act
    var results = PortugalValidationRules.ValidateReceiptMomentOrder(
        request, 
        series, 
        isHandwritten: false
    ).ToList();
    
    // Assert
    Assert.AreEqual(1, results.Count);
    var error = results[0].Errors.Single();
    Assert.AreEqual("EEEE_CbReceiptMomentBeforeLastMoment", error.Code);
    Assert.AreEqual("cbReceiptMoment", error.Field);
    Assert.AreEqual("FS/2024", error.Context["SeriesIdentifier"]);
}
```

### Integration Test Example
```csharp
[Test]
public async Task ProcessInvoice_OutOfOrder_ReturnsValidationError()
{
    // Arrange: Process first invoice at 11:00
    var firstInvoice = CreateInvoiceRequest(new DateTime(2024, 1, 15, 11, 0, 0));
    await processor.InvoiceB2C0x1001Async(firstInvoice);
    
    // Act: Try to process second invoice at 10:00 (earlier)
    var secondInvoice = CreateInvoiceRequest(new DateTime(2024, 1, 15, 10, 0, 0));
    var response = await processor.InvoiceB2C0x1001Async(secondInvoice);
    
    // Assert
    Assert.IsFalse(response.ReceiptResponse.ftState.HasFlag(State.Success));
    Assert.That(response.ReceiptResponse.ftStateData, 
        Contains.Substring("EEEE_CbReceiptMomentBeforeLastMoment"));
}
```

## Benefits

1. **Centralized Logic**: Single implementation used by all processors
2. **Consistent Errors**: Same error format and codes across all receipt types
3. **Better Testing**: Can unit test validation independently
4. **Structured Context**: Rich error information for debugging
5. **One Result Per Error**: Follows the established pattern

## Migration Path

### Before (Local Method)
```csharp
private static bool ValidateReceiptMomentOrder(
    ProcessCommandRequest request, 
    NumberSeries series)
{
    if (series.LastCbReceiptMoment.HasValue &&
        !request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) &&
        request.ReceiptRequest.cbReceiptMoment < series.LastCbReceiptMoment.Value)
    {
        request.ReceiptResponse.SetReceiptResponseError(
            ErrorMessagesPT.EEEE_CbReceiptMomentBeforeLastMoment(
                series.Identifier, 
                series.LastCbReceiptMoment.Value
            )
        );
        return false;
    }
    return true;
}

// Usage
if (!ValidateReceiptMomentOrder(request, series))
{
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

### After (Centralized)
```csharp
// No local method needed

// Usage
var momentOrderErrors = PortugalValidationRules.ValidateReceiptMomentOrder(
    request.ReceiptRequest, 
    series, 
    isHandwritten: false
).ToList();

if (momentOrderErrors.Any())
{
    request.ReceiptResponse.SetReceiptResponseError(
        momentOrderErrors.First().GetCombinedErrorMessage()
    );
    return new ProcessCommandResponse(request.ReceiptResponse, []);
}
```

## Conclusion

The `ValidateReceiptMomentOrder` validation is now part of the centralized validation system, providing:
- ? Consistent error handling
- ? Structured error information
- ? Layer independence through reflection
- ? Reusability across all processors
- ? One ValidationResult per error
- ? Rich context data for debugging

This completes the migration of all validation logic to the centralized system.
