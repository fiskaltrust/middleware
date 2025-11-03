using System.ComponentModel.DataAnnotations;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosScuConfiguration
{
    [Required]
    public string DeviceId { get; set; } = null!;

    [Required]
    public string BaseUrl { get; set; } = null!;

    [Required]
    public string SharedSecret { get; set; } = null!;

    [Required]
    public string CompanyId { get; set; } = null!;

    public int TimeoutSeconds { get; set; } = 30;
}