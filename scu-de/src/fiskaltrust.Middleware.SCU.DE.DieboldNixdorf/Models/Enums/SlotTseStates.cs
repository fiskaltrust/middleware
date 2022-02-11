using System;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums
{
    [Flags]
    public enum SlotTseStates
    {
        NotUsed7 = 128,
        NotUsed6 = 64,
        NotUsed5 = 32,
        NotUsed4 = 16,
        NotUsed3 = 8,
        NearFullCondition = 4,
        Disabled = 2,
        Initialized = 1,
        Uninitialized = 0
    }
}

