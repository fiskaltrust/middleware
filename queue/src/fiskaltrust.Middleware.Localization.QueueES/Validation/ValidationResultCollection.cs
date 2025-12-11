namespace fiskaltrust.Middleware.Localization.QueueES.Validation;

/// <summary>
/// Collection of validation results with helper methods
/// </summary>
public class ValidationResultCollection
{
    private readonly List<ValidationResult> _results;

    public ValidationResultCollection(List<ValidationResult> results)
    {
        _results = results;
    }

    /// <summary>
    /// Gets all validation results (one per error)
    /// </summary>
    public IReadOnlyList<ValidationResult> Results => _results;

    /// <summary>
    /// Returns true if all validations passed (no errors)
    /// </summary>
    public bool IsValid => !_results.Any(r => !r.IsValid);

    /// <summary>
    /// Gets all errors from all validation results
    /// </summary>
    public IEnumerable<ValidationError> AllErrors => _results.SelectMany(r => r.Errors);

    /// <summary>
    /// Gets combined error message from all results
    /// </summary>
    public string GetCombinedErrorMessage(string separator = " | ")
    {
        return string.Join(separator, _results.SelectMany(r => r.Errors).Select(e => e.Message));
    }

    /// <summary>
    /// Gets all unique error codes
    /// </summary>
    public IEnumerable<string> GetAllErrorCodes()
    {
        return _results
            .SelectMany(r => r.Errors)
            .Where(e => !string.IsNullOrEmpty(e.Code))
            .Select(e => e.Code!)
            .Distinct();
    }
}
