namespace fiskaltrust.Middleware.Localization.QueuePT.Models;

public class ActivateQueuePT
{
    public Guid CashBoxId { get; set; }
    public required Guid QueueId { get; set; }
    public required DateTime Moment { get; set; }
    public required bool IsStartReceipt { get; set; }
    public required string Version { get; set; }
}