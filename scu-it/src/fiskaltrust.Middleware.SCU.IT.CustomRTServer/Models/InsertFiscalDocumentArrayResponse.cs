using System.Collections.Generic;

public class InsertFiscalDocumentArrayResponse : CustomRTDetailedResponse
{
    public string responseSubCode { get; set; } = string.Empty;
    public List<InsertFiscalDocumentArraySubResponse> ArrayResponse { get; set; } = new List<InsertFiscalDocumentArraySubResponse>();
}
