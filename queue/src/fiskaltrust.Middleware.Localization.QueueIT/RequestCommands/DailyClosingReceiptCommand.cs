using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class DailyClosingReceiptCommand : Contracts.RequestCommands.DailyClosingReceiptCommand
    {
        public override long CountryBaseState => Constants.Cases.BASE_STATE;

        public DailyClosingReceiptCommand(IReadOnlyConfigurationRepository configurationRepository) : base(configurationRepository) { }
    }
}
