using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class FDocument
{
    public DocumentData? document { get; set; }
    public List<DocumentItemData> items { get; set; } = new List<DocumentItemData>();
    public List<DocumentTaxData> taxs { get; set; } = new List<DocumentTaxData>();
}


public class FDocumentArray
{
    public List<FDocument> fiscalData { get; set; } = new List<FDocument>();
}