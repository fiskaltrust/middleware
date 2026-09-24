using System;

namespace fiskaltrust.Middleware.Localization.QueuePL.Models;

public class ActivateQueuePL
{
    public Guid CashBoxId { get; set; }
    public Guid QueueId { get; set; }
    public DateTime Moment { get; set; }
    public bool IsStartReceipt { get; set; }
    public string Version { get; set; } = "V0";
}

public class DeactivateQueuePL
{
    public Guid CashBoxId { get; set; }
    public Guid QueueId { get; set; }
    public DateTime Moment { get; set; }
    public bool IsStopReceipt { get; set; }
    public string Version { get; set; } = "V0";
}
