namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class DocumentItemData
{
    public string type { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string amount { get; set; } = string.Empty;
    public string quantity { get; set; } = string.Empty;
    public string unitprice { get; set; } = string.Empty;
    public string vatvalue { get; set; } = string.Empty;
    public string paymentid { get; set; } = string.Empty;
    public string plu { get; set; } = string.Empty;
    public string department { get; set; } = string.Empty;

    // deprecated
    public string? vatcode { get; set; } // this field is not used
    public string? businesscode { get; set; } 
    public string? service { get; set; } 
    public string? fiscalvoid { get; set; }
    public string? signid { get; set; } 
}
