namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class YearlyClosingReceiptCommand : Contracts.RequestCommands.YearlyClosingReceiptCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;
    }
}
