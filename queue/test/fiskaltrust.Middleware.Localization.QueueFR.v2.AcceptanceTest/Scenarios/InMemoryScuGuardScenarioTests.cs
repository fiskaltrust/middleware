using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.SCU.FR.InMemory;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// market-fr#221: a sandbox or production queue never signs with the in-memory SCU; only a queue marked as a local
/// or development run does.
/// </summary>
public class InMemoryScuGuardScenarioTests
{
    private static async Task<ReceiptResponse> SignASaleAsync(Dictionary<string, object> overrides)
    {
        var (signProcessor, cashBoxId) = FRTestQueue.Create(new InMemorySCU(), overrides);
        var request = new ReceiptRequest
        {
            ftCashBoxID = cashBoxId,
            cbTerminalID = "1",
            cbReceiptReference = "R-1",
            cbReceiptMoment = DateTime.UtcNow,
            Currency = Currency.EUR,
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
            cbChargeItems = [new ChargeItem { Amount = 12m, VATAmount = 2m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
            cbPayItems = [new PayItem { Amount = 12m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }],
        };
        return JsonSerializer.Deserialize<ReceiptResponse>(await signProcessor(JsonSerializer.Serialize(request)))!;
    }

    [Fact]
    public async Task AQueueWithoutTheLocalMarker_SignsNothing()
        => (await SignASaleAsync(new() { ["FRLocalDevelopment"] = false })).ftState.IsState(State.Error).Should().BeTrue();

    [Fact]
    public async Task ASandboxQueue_SignsNothing()
        => (await SignASaleAsync(new() { ["sandbox"] = true })).ftState.IsState(State.Error).Should().BeTrue("the local marker does not hold on a sandbox queue");

    [Fact]
    public async Task ALocalQueue_Signs()
        => (await SignASaleAsync([])).ftReceiptIdentification.Should().EndWith("T1");
}
