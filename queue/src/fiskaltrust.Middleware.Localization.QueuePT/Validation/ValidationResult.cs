namespace fiskaltrust.Middleware.Localization.QueuePT.Validation;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the validation was successful
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Collection of validation errors
    /// </summary>
    public List<ValidationError> Errors { get; } = new();

    /// <summary>
    /// Gets all error messages combined with a separator
    /// </summary>
    public string GetCombinedErrorMessage(string separator = " | ")
    {
        return string.Join(separator, Errors.Select(e => e.Message));
    }

    /// <summary>
    /// Gets all error codes
    /// </summary>
    public IEnumerable<string> GetErrorCodes()
    {
        return Errors.Where(e => !string.IsNullOrEmpty(e.Code)).Select(e => e.Code!);
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with a single error
    /// </summary>
    public static ValidationResult Failed(ValidationError error)
    {
        var result = new ValidationResult();
        result.Errors.Add(error);
        return result;
    }

    /// <summary>
    /// Creates a failed validation result with multiple errors
    /// </summary>
    public static ValidationResult Failed(IEnumerable<ValidationError> errors)
    {
        var result = new ValidationResult();
        result.Errors.AddRange(errors);
        return result;
    }

    /// <summary>
    /// Creates a failed validation result with a simple error message
    /// </summary>
    public static ValidationResult Failed(string message, string? code = null)
    {
        return Failed(new ValidationError(message, code));
    }

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    public void AddError(ValidationError error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Adds an error message to the validation result
    /// </summary>
    public void AddError(string message, string? code = null)
    {
        Errors.Add(new ValidationError(message, code));
    }

    /// <summary>
    /// Merges another validation result into this one
    /// </summary>
    public void Merge(ValidationResult other)
    {
        if (other != null)
        {
            Errors.AddRange(other.Errors);
        }
    }
}

/// <summary>
/// Represents a single validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Error code for programmatic identification
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Severity level of the error
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Field or property that caused the validation error
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Index of the item in a collection (if applicable)
    /// </summary>
    public int? ItemIndex { get; set; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }

    public ValidationError()
    {
        Message = string.Empty;
        Severity = ValidationSeverity.Error;
    }

    public ValidationError(string message, string? code = null, ValidationSeverity severity = ValidationSeverity.Error)
    {
        Message = message;
        Code = code;
        Severity = severity;
    }

    public ValidationError(string message, string? code, string? field, int? itemIndex = null)
    {
        Message = message;
        Code = code;
        Field = field;
        ItemIndex = itemIndex;
        Severity = ValidationSeverity.Error;
    }

    /// <summary>
    /// Adds context data to the error
    /// </summary>
    public ValidationError WithContext(string key, object value)
    {
        Context ??= new Dictionary<string, object>();
        Context[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the field that caused the error
    /// </summary>
    public ValidationError WithField(string field)
    {
        Field = field;
        return this;
    }

    /// <summary>
    /// Sets the item index that caused the error
    /// </summary>
    public ValidationError WithItemIndex(int index)
    {
        ItemIndex = index;
        return this;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(Code))
        {
            parts.Add($"[{Code}]");
        }

        if (!string.IsNullOrEmpty(Field))
        {
            parts.Add($"Field: {Field}");
        }

        if (ItemIndex.HasValue)
        {
            parts.Add($"Index: {ItemIndex}");
        }

        parts.Add(Message);

        return string.Join(" ", parts);
    }
}

/// <summary>
/// Severity levels for validation errors
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning that doesn't prevent processing
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error that prevents processing
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical error that indicates a severe problem
    /// </summary>
    Critical = 3
}
