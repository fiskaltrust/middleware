namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

public class ActivateQueueFR
{
    public Guid CashBoxId { get; set; }
    public Guid QueueId { get; set; }
    public DateTime Moment { get; set; }
    public bool IsStartReceipt { get; set; }
    public string Version { get; set; } = "V0";
}

public class DeactivateQueueFR
{
    public Guid CashBoxId { get; set; }
    public Guid QueueId { get; set; }
    public DateTime Moment { get; set; }
    public bool IsStopReceipt { get; set; }
    public string Version { get; set; } = "V0";
}
