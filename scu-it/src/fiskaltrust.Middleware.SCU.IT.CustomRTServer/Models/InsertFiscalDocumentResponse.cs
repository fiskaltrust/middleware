using System.Collections.Generic;

public class InsertFiscalDocumentResponse : CustomRTDetailedResponse
{
    public List<string> responseSubCode { get; set; } = new List<string>();
    public int fiscalDocId { get; set; }
}
