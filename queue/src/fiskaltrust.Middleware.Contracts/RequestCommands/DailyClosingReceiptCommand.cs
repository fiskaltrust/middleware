using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class DailyClosingReceiptCommand : ClosingReceiptCommand
    {
        protected override string ClosingReceiptName => "Daily-Closing";

        public DailyClosingReceiptCommand(IReadOnlyConfigurationRepository configurationRepository) : base(configurationRepository) { }
    }
}
