using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers;
using FluentAssertions;

var builder = new CashBoxBuilder("ES");

var (echo, sign, journal) = builder.Build();

{
    var response = await echo(new EchoRequest { Message = "Hello, Middleware!" });
    response.Should().NotBeNull();
    response.Message.Should().BeEquivalentTo("Hello, Middleware!");
}

{
    var response = await sign(new ReceiptRequest
    {
        ftCashBoxID = builder.CashBoxId,
        cbReceiptMoment = DateTime.UtcNow,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8),
        cbChargeItems = [],
        cbPayItems = [],
        ftReceiptCase = ReceiptCase.InitialOperationReceipt0x4001.WithCountry(builder.Market)
    });
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
}

{
    var response = await sign(new ReceiptRequest
    {
        ftCashBoxID = builder.CashBoxId,
        cbReceiptMoment = DateTime.UtcNow,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8),
        cbChargeItems = [
            builder.ChargeItem
                .WithAmount(100)
                .WithQuantity(1)
                .WithCase(ChargeItemCase.NormalVatRate
                    .WithVersion(2)
                    .WithTypeOfService(ChargeItemCaseTypeOfService.Delivery))
                .Build()
        ],
        cbPayItems = [
            new PayItem{
                Description = "cash",
                Quantity = 1,
                Amount = 100,
                ftPayItemCase = PayItemCase.CashPayment.WithVersion(2)
            }
        ],
        cbReceiptAmount = 100,
        ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry(builder.Market)
    });
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
}