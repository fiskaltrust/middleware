using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class DocumentTaxData
{
    public int gross { get; set; }
    public int tax { get; set; }
    public int vatvalue { get; set; }
    public string vatcode { get; set; } = string.Empty;

    // deprecated
    public string? businesscode { get; set; } 
    public List<AdditionalTaxData> additional_tax_data { get; set; } = new List<AdditionalTaxData>();
}
