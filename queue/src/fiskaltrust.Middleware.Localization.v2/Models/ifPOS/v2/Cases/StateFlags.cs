using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum StateFlags : long
{
    SecurityMechanismDeactivated = 0x0000_0000_0000_0001,
    SCUTemporaryOutOfService = 0x0000_0000_0000_0002,
    LateSigningModeIsActive = 0x0000_0000_0000_0008,
    MessageIsPending = 0x0000_0000_0000_0040,
    DailyClosingIsDue = 0x0000_0000_0000_0100,
}

public static class StateFlagsExt
{
    public static State WithFlag(this State self, StateFlags flag) => (State) ((long) self | (long) flag);
    public static bool IsFlag(this State self, StateFlags flag) => ((long) self & (long) flag) == (long) flag;
}
