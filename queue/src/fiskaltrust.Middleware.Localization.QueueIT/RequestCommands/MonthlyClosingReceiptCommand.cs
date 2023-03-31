using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class MonthlyClosingReceiptCommand : Contracts.RequestCommands.MonthlyClosingReceiptCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public MonthlyClosingReceiptCommand(IReadOnlyConfigurationRepository configurationRepository) : base(configurationRepository) { }
    }
}
