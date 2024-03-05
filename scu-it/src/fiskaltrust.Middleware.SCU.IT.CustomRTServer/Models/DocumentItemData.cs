namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class DocumentItemData
{
    public string type { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string amount { get; set; } = string.Empty;
    public string quantity { get; set; } = string.Empty;
    public string unitprice { get; set; } = string.Empty;
    public string vatvalue { get; set; } = string.Empty;
    public string? fiscalvoid { get; set; } = "0";
    public string signid { get; set; } = "1";
    public string paymentid { get; set; } = string.Empty;
    public string plu { get; set; } = string.Empty;
    public string department { get; set; } = string.Empty;
    public string? vatcode { get; set; } = null;
    public string? service { get; } = "0";
    public string? businesscode { get; } = "";
}
