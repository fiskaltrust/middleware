namespace fiskaltrust.Middleware.Localization.QueueGR.Validation;

public class MiddlewareValidationError
{
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public MiddlewareValidationError(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
