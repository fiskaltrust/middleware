using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class FDocumentLottery
{
    public DocumentDataLottery? document { get; set; }
    public List<DocumentItemData> items { get; set; } = new List<DocumentItemData>();
    public List<DocumentTaxData> taxs { get; set; } = new List<DocumentTaxData>();
}