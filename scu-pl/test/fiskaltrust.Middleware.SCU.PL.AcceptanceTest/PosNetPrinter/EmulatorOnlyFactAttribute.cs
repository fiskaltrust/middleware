using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;

/// <summary>
/// A test whose expectation is a property of the emulated device — a scripted failure (rejected
/// command, silent printer, refused port) or a known device state such as being fiscalized. On a
/// hardware run it is skipped rather than failed: the scenario is not reproducible on the printer
/// in front of you, not broken.
/// </summary>
public sealed class EmulatorOnlyFactAttribute : FactAttribute
{
    public EmulatorOnlyFactAttribute()
    {
        if (PosNetTestTarget.RunsAgainstHardware)
        {
            Skip = $"The expected device state or failure is only reproducible on the emulator ({PosNetTestTarget.DeviceUrlVariable} is set).";
        }
    }
}
