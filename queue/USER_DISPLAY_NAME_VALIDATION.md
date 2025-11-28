# UserDisplayName Minimum Length Validation

## Summary

Added validation to ensure that `UserDisplayName` in the `cbUser` object is at least 3 characters long when provided.

## Validation Rule

### Location
`PortugalValidationRules.ValidateUserStructure()`

### Validation Logic
```csharp
if (!string.IsNullOrWhiteSpace(userObject.UserDisplayName) && 
    userObject.UserDisplayName.Length < 3)
{
    // Validation error
}
```

### Key Points
- **Only validates if UserDisplayName is provided** - If null, empty, or whitespace, validation is skipped
- **Minimum length: 3 characters** - UserDisplayName must have at least 3 characters
- **Integrated with existing validation** - Part of the `ValidateUserStructure` method
- **Structured error information** - Includes actual length and minimum required length in context

## Error Information

### Error Code
```
"EEEE_InvalidUserStructure"
```

### Field
```
"cbUser.UserDisplayName"
```

### Error Message Example
```
"EEEE_cbUser.UserDisplayName must be at least 3 characters long. Current length: 2"
```

### Context Data
```csharp
{
    "ActualLength": 2,
    "MinimumLength": 3
}
```

## Usage Examples

### Example 1: Valid UserDisplayName (? 3 characters)
```csharp
var request = new ReceiptRequest
{
    cbUser = new
    {
        UserId = "USR001",
        UserDisplayName = "John"  // ? Valid - 4 characters
    }
};

var results = PortugalValidationRules.ValidateUserStructure(request).ToList();
// results.Count == 0 (no errors)
```

### Example 2: Invalid UserDisplayName (< 3 characters)
```csharp
var request = new ReceiptRequest
{
    cbUser = new
    {
        UserId = "USR001",
        UserDisplayName = "Jo"  // ? Invalid - only 2 characters
    }
};

var results = PortugalValidationRules.ValidateUserStructure(request).ToList();
// results.Count == 1

var error = results[0].Errors.Single();
// error.Code == "EEEE_InvalidUserStructure"
// error.Field == "cbUser.UserDisplayName"
// error.Message == "EEEE_cbUser.UserDisplayName must be at least 3 characters long. Current length: 2"
// error.Context["ActualLength"] == 2
// error.Context["MinimumLength"] == 3
```

### Example 3: UserDisplayName Not Provided (Valid)
```csharp
var request = new ReceiptRequest
{
    cbUser = new
    {
        UserId = "USR001"
        // UserDisplayName not provided - ? Valid
    }
};

var results = PortugalValidationRules.ValidateUserStructure(request).ToList();
// results.Count == 0 (no errors)
```

### Example 4: UserDisplayName Empty/Null (Valid)
```csharp
var request = new ReceiptRequest
{
    cbUser = new
    {
        UserId = "USR001",
        UserDisplayName = ""  // ? Valid - empty is allowed
    }
};

var results = PortugalValidationRules.ValidateUserStructure(request).ToList();
// results.Count == 0 (no errors)
```

## Integration with Full Validation

The validation is automatically included when using the `ReceiptValidator`:

```csharp
var validator = new ReceiptValidator(request);
var validationResults = validator.ValidateAndCollect(new ReceiptValidationContext
{
    IsRefund = false,
    GeneratesSignature = true,
    IsHandwritten = false,
    NumberSeries = series
});

if (!validationResults.IsValid)
{
    // All errors including UserDisplayName length check
    var errorMessage = validationResults.GetCombinedErrorMessage();
    // Example: "EEEE_cbUser.UserDisplayName must be at least 3 characters long. Current length: 2"
}
```

## Multiple Validation Errors

The `ValidateUserStructure` method can now return multiple errors:

```csharp
var request = new ReceiptRequest
{
    cbUser = new
    {
        UserId = "",  // ? Invalid - empty
        UserDisplayName = "AB"  // ? Invalid - too short
    }
};

var results = PortugalValidationRules.ValidateUserStructure(request).ToList();
// results.Count == 2

// Error 1: Empty UserId
var error1 = results[0].Errors.Single();
// error1.Code == "EEEE_InvalidUserStructure"
// error1.Field == "cbUser.UserId"
// error1.Message == "EEEE_cbUser must contain a non-empty 'UserId' property."

// Error 2: Short UserDisplayName
var error2 = results[1].Errors.Single();
// error2.Code == "EEEE_InvalidUserStructure"
// error2.Field == "cbUser.UserDisplayName"
// error2.Message == "EEEE_cbUser.UserDisplayName must be at least 3 characters long. Current length: 2"
```

## Combined Error Message

When multiple validation errors occur:

```csharp
var validationResults = validator.ValidateAndCollect(context);
var combinedMessage = validationResults.GetCombinedErrorMessage();

// Example output:
// "EEEE_cbUser must contain a non-empty 'UserId' property. | 
//  EEEE_cbUser.UserDisplayName must be at least 3 characters long. Current length: 2"
```

## Testing

### Unit Test Example
```csharp
[Test]
public void ValidateUserStructure_UserDisplayNameTooShort_ReturnsError()
{
    // Arrange
    var request = new ReceiptRequest
    {
        cbUser = new
        {
            UserId = "USER001",
            UserDisplayName = "AB"  // Only 2 characters
        }
    };

    // Act
    var results = PortugalValidationRules.ValidateUserStructure(request).ToList();

    // Assert
    Assert.AreEqual(1, results.Count);
    var error = results[0].Errors.Single();
    Assert.AreEqual("EEEE_InvalidUserStructure", error.Code);
    Assert.AreEqual("cbUser.UserDisplayName", error.Field);
    Assert.AreEqual(2, error.Context["ActualLength"]);
    Assert.AreEqual(3, error.Context["MinimumLength"]);
}

[Test]
public void ValidateUserStructure_UserDisplayNameValidLength_NoError()
{
    // Arrange
    var request = new ReceiptRequest
    {
        cbUser = new
        {
            UserId = "USER001",
            UserDisplayName = "John"  // 4 characters - valid
        }
    };

    // Act
    var results = PortugalValidationRules.ValidateUserStructure(request).ToList();

    // Assert
    Assert.AreEqual(0, results.Count);
}

[Test]
public void ValidateUserStructure_UserDisplayNameNotProvided_NoError()
{
    // Arrange
    var request = new ReceiptRequest
    {
        cbUser = new
        {
            UserId = "USER001"
            // UserDisplayName not provided
        }
    };

    // Act
    var results = PortugalValidationRules.ValidateUserStructure(request).ToList();

    // Assert
    Assert.AreEqual(0, results.Count);
}
```

## Why This Validation?

### Business Requirement
User display names should be meaningful and identifiable. A minimum length of 3 characters ensures:
- **Readability**: Names like "A" or "AB" are too short to be meaningful
- **Data Quality**: Encourages proper user identification
- **SAFT-PT Compliance**: Ensures proper user tracking in audit files

### Related SAFT-PT Fields
The `UserDisplayName` maps to the `SourceID` field in SAFT-PT documents, which should contain identifiable user information.

## Related Validations

The `ValidateUserStructure` method performs multiple checks:

1. **cbUser deserialization** - Ensures valid JSON structure
2. **UserId presence** - Must not be null or empty
3. **UserDisplayName length** - Must be at least 3 characters (if provided) ? **NEW**

All validations return individual `ValidationResult` objects following the "one result per error" pattern.

## Files Modified

- `PortugalValidationRules.cs` - Updated `ValidateUserStructure` method

## Benefits

1. **Data Quality**: Ensures meaningful user identification
2. **Early Detection**: Catches invalid data before processing
3. **Clear Feedback**: Users know exactly what's wrong and how to fix it
4. **Structured Context**: Error includes actual length and minimum required
5. **Optional Field**: Only validates if UserDisplayName is provided
6. **Consistent Pattern**: Follows established validation architecture

## Edge Cases Handled

| Case | UserDisplayName | Valid? | Reason |
|------|----------------|--------|--------|
| Not provided | `null` | ? | Optional field |
| Empty string | `""` | ? | Optional field |
| Whitespace | `"   "` | ? | Optional field |
| 1 character | `"A"` | ? | Too short |
| 2 characters | `"AB"` | ? | Too short |
| 3 characters | `"ABC"` | ? | Minimum met |
| 4+ characters | `"John"` | ? | Valid |

## Conclusion

The `UserDisplayName` minimum length validation ensures data quality while remaining optional. When provided, it must be at least 3 characters long, providing meaningful user identification in receipts and SAFT-PT exports.

**Validation Rules:**
- ? UserDisplayName is optional
- ? If provided, must be ? 3 characters
- ? Returns structured error with actual and minimum length
- ? Integrated with centralized validation system
