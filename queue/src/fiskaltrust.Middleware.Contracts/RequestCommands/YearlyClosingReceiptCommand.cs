using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.RequestCommands
{
    public abstract class YearlyClosingReceiptCommand : ClosingReceiptCommand
    {
        protected override string ClosingReceiptName => "Yearly-Closing";

        public YearlyClosingReceiptCommand(IReadOnlyConfigurationRepository configurationRepository) : base(configurationRepository) { }
    }
}
