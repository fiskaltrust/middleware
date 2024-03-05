using System;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public class POSReceiptSignatureData
{
    public string RTSerialNumber { get; set; } = string.Empty;
    public long RTZNumber { get; set; }
    public long RTDocNumber { get; set; }
    public DateTime RTDocMoment { get; set; }
    public string RTDocType { get; set; } = string.Empty;
    public string? RTCodiceLotteria { get; set; }
    public string? RTCustomerID { get; set; }
    public string? RTServerSHAMetadata { get; set; }

    public long? RTReferenceZNumber { get; set; }
    public long? RTReferenceDocNumber { get; set; }
    public DateTime? RTReferenceDocMoment { get; set; }
}
