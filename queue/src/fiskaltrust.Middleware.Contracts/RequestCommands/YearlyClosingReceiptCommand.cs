namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class YearlyClosingReceiptCommand : ClosingReceiptCommand
    {
        protected override string ClosingReceiptName => "Yearly-Closing";
    }
}
