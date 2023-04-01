using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class MonthlyClosingReceiptCommand : ClosingReceiptCommand
    {
        protected override string ClosingReceiptName => "Monthly-Closing";
    }
}
