using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public class SimpleTseCommand : TseCommand
    {
        public SimpleTseCommand(TseCommandCodeEnum command)
            : base(command) { }
    }
}
