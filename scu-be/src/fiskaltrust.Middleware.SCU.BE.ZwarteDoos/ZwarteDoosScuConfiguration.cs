using System.ComponentModel.DataAnnotations;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosScuConfiguration
{
    [Required]
    public string ServiceUrl { get; set; } = null!;

    [Required]
    public string ApiKey { get; set; } = null!;

    [Required]
    public string CompanyId { get; set; } = null!;

    public bool SandboxMode { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 30;

    public bool EnableLogging { get; set; } = true;

    public string? CertificatePath { get; set; }

    public string? CertificatePassword { get; set; }
}