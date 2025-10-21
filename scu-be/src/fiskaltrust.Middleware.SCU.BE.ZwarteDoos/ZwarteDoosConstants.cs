namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public static class ZwarteDoosConstants
{
    public const string DefaultServiceUrl = "https://api.zwartedoos.be";
    public const string SandboxServiceUrl = "https://sandbox.api.zwartedoos.be";
    
    public const string InvoiceEndpoint = "/api/v1/invoices";
    public const string StatusEndpoint = "/api/v1/status";
    
    public const int DefaultTimeoutSeconds = 30;
    public const int MaxRetryAttempts = 3;
    
    public const string ApiKeyHeaderName = "X-API-Key";
    public const string ContentTypeJson = "application/json";
    
    public const string SignatureAlgorithm = "SHA256";
}