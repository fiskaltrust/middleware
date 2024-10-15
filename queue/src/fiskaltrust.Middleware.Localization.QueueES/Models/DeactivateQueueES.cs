namespace fiskaltrust.Middleware.Localization.QueueES.Models;

public class DeactivateQueueES
{
    public Guid CashBoxId { get; set; }
    public Guid QueueId { get; set; }
    public DateTime Moment { get; set; }
    public bool IsStopReceipt { get; set; }
    public required string Version { get; set; }
}