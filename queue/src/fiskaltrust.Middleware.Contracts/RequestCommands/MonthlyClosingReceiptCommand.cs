using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class MonthlyClosingReceiptCommand : ClosingReceiptCommand
    {
        protected override string ClosingReceiptName => "Monthly-Closing";
    }
}
