using System.ComponentModel.DataAnnotations;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;

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

    public Language Language { get; set; } = Language.NL;

    public int TimeoutSeconds { get; set; } = 30;

    public string VatNo { get; set; } = string.Empty;

    public string EstNo { get; set; } = string.Empty;
}