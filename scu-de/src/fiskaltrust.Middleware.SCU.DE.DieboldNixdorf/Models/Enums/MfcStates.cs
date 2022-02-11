using System;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums
{
    [Flags]
    public enum MfcStates
    {
        NotUsed7 = 128,
        NotUsed6 = 64,
        NotUsed5 = 32,
        MfcService = 16,
        MfcMaintMode = 8,
        MfcTransStarted = 4,
        MfcTransCmdOpen = 2,
        MfcFiscalMode = 1,
        Nothing = 0
    }
}

