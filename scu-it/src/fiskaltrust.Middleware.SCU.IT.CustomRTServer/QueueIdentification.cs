namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class QueueIdentification
{
    public string CashUuId { get; set; } = string.Empty;
    public string LastSignature { get; set; } = string.Empty;
    public string CashHmacKey { get; set; } = string.Empty;
    public string CashToken { get; set; } = string.Empty;
    public int LastZNumber { get; set; }
    public int LastDocNumber { get; set; }
    public int CurrentGrandTotal { get; set; }
}
