using fiskaltrust.Middleware.Contracts.RequestCommands;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public abstract class RequestCommandIT : RequestCommand
    {
        public override long CountryBaseState => 0x4954000000000000;
   }
}
