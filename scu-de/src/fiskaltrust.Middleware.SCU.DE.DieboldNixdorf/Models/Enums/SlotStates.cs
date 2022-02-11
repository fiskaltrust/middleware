using System;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums
{
    [Flags]
    public enum SlotStates
    {
        NotUsed7 = 128,
        NotUsed6 = 64,
        NotUsed5 = 32,
        NotUsed4 = 16,
        MaintenanceModeActive = 8,
        MaintenanceModeRequested = 4,
        OperationalMode = 2,
        TseInserted = 1,
        Occupied = 0
    }
}

