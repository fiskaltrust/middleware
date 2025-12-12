namespace fiskaltrust.Middleware.Localization.QueueES.Validation;

/// <summary>
/// Context information for receipt validation
/// </summary>
public class ReceiptValidationContext
{
    /// <summary>
    /// Whether this is a refund receipt
    /// </summary>
    public bool IsRefund { get; set; }

    /// <summary>
    /// Whether this receipt type generates a signature
    /// </summary>
    public bool GeneratesSignature { get; set; }

    /// <summary>
    /// Whether this is a handwritten receipt
    /// </summary>
    public bool IsHandwritten { get; set; }

    /// <summary>
    /// The NumberSeries object for receipt moment order validation.
    /// Optional - if not provided, receipt moment order validation is skipped.
    /// </summary>
    public object? NumberSeries { get; set; }
}
