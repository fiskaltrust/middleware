using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class DailyClosingReceiptCommand : ClosingReceiptCommand
    {
        protected override string ClosingReceiptName => "Daily-Closing";
    }
}
