using System.Collections.Generic;

public class InsertFiscalDocumentArraySubResponse
{
    public int id { get; set; }
    public string? signedDigest { get; set; }
    public string? publKey { get; set; }
    public List<string> responseSubCode { get; set; } = new List<string>();
    public string fiscalDocID { get; set; } = string.Empty;
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
}
