namespace fiskaltrust.Middleware.Localization.v2.Validation;

/// Represents the result of a validation operation.

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; } = new();

    public static ValidationResult Success() => new();

    public static ValidationResult Failed(ValidationError error)
    {
        var result = new ValidationResult();
        result.Errors.Add(error);
        return result;
    }

    public static ValidationResult Failed(string message, string? code = null, string? field = null)
    {
        return Failed(new ValidationError(message, code, field));
    }
}

/// Represents a single validation error.

public class ValidationError
{
    public string Message { get; }
    public string? Code { get; }
    public string? Field { get; }
    public int? ItemIndex { get; }
    public Dictionary<string, object>? Context { get; private set; }

    public ValidationError(string message, string? code = null, string? field = null, int? itemIndex = null)
    {
        Message = message;
        Code = code;
        Field = field;
        ItemIndex = itemIndex;
    }

    public ValidationError WithContext(string key, object value)
    {
        Context ??= new Dictionary<string, object>();
        Context[key] = value;
        return this;
    }
}

public class ValidationResultCollection
{
    public List<ValidationResult> Results { get; }
    public bool IsValid => Results.All(r => r.IsValid);

    public ValidationResultCollection(List<ValidationResult> results)
    {
        Results = results;
    }
}
