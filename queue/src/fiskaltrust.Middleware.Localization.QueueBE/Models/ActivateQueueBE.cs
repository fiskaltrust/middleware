namespace fiskaltrust.Middleware.Localization.QueueBE.Models;

public class ActivateQueueBE
{
    public Guid CashBoxId { get; set; }
    public required Guid QueueId { get; set; }
    public required DateTime Moment { get; set; }
    public required bool IsStartReceipt { get; set; }
    public required string Version { get; set; }
}