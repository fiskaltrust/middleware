namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class DocumentData
{
    public string cashuuid { get; set; } = string.Empty;
    public int doctype { get; set; }
    public string dtime { get; set; } = string.Empty;
    public int docnumber { get; set; }
    public int docznumber { get; set; }
    public int amount { get; set; }
    public string fiscalcode { get; set; } = string.Empty;
    public string vatcode { get; set; } = string.Empty;
    public string fiscaloperator { get; set; } = string.Empty;
    public string? businessname { get; set; } = null;
    public string? prevSignature { get; set; } = string.Empty;
    public string type_signature_id => "1";
    public int grandTotal { get; set; }
    public long referenceClosurenumber { get; set; }
    public long referenceDocnumber { get; set; }
    public string? referenceDtime { get; set; } = null;
    //public string? referenceCashuuid { get; set; } = string.Empty;

    public string? errSignature => null;
    public int? err_number => null;
    public int? err_znumber => null;
}
