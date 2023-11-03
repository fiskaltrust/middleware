using System.Collections.Generic;

public class InsertFiscalDocumentArrayResponse : CustomRTDetailedResponse
{
    public List<int> responseArraySubCode { get; set; } = new List<int>();
    public List<InsertFiscalDocumentArraySubResponse> ArrayResponse { get; set; } = new List<InsertFiscalDocumentArraySubResponse>();
}
