using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.InMemory;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

public class InMemoryFRScenarioTests : FRScenarioTests
{
    protected override IFRSSCD CreateScu() => new InMemorySCU();
}
