namespace fiskaltrust.Middleware.Localization.QueuePT.Models;

public class DeactivateQueuePT
{
    public Guid CashBoxId { get; set; }
    public Guid QueueId { get; set; }
    public DateTime Moment { get; set; }
    public bool IsStopReceipt { get; set; }
    public required string Version { get; set; }
}