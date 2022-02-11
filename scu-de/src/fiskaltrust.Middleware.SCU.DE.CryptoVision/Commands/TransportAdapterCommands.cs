namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public static class TransportAdapterCommands
    {
        public class EnableSuspendModeTseCommand : RawTseCommand
        {
            public EnableSuspendModeTseCommand()
                : base(new byte[] { 0x00, 0x00 }, 0x5345) { }
        }

        public class DisableSuspendModeTseCommand : RawTseCommand
        {
            public DisableSuspendModeTseCommand()
                : base(new byte[] { 0x00, 0x00 }, 0x5344) { }
        }

        public class ReadNextFragmentTseCommand : RawTseCommand
        {
            public ReadNextFragmentTseCommand()
                : base(new byte[] { 0xc5 }) { }
        }

        public class CancelReadFragmentsTseCommand : RawTseCommand
        {
            public CancelReadFragmentsTseCommand()
                : base(new byte[] { 0xc4 }) { }
        }
    }
}
