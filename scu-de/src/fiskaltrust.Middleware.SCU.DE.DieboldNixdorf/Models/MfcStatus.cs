using System;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class MfcStatus
    {
        public byte[] MfcError { get; set; }

        public MfcStates MfcState { get; set; }

        public bool IsInitialized => (MfcState & MfcStates.MfcFiscalMode) == MfcStates.MfcFiscalMode;

        public bool IsTransactionOpen => (MfcState & MfcStates.MfcTransCmdOpen) == MfcStates.MfcTransCmdOpen;

        public bool IsTerminated => (MfcState & MfcStates.MfcFiscalMode) != MfcStates.MfcFiscalMode;
    }
}

