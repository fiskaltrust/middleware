using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers;
using fiskaltrust.storage.serialization.V0;
using FluentAssertions;
using Newtonsoft.Json;

var builder = new CashBoxBuilder(
    "ES" switch
    {
        "ES" => new CashBoxBuilderES(SCUTypesES.TicketBAIAraba),
        _ => throw new NotImplementedException(),
    },
    JsonConvert.DeserializeObject<PackageConfiguration>(await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, "queue-configuration.json"))),
    JsonConvert.DeserializeObject<PackageConfiguration>(await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, "scu-configuration.json")))
);

var middleware = builder.Build();

{
    var response = await middleware.Echo(new EchoRequest { Message = "Hello, Middleware!" });
    response.Should().NotBeNull();
    response.Message.Should().BeEquivalentTo("Hello, Middleware!");
}

{
    var response = await middleware.Sign(new ReceiptRequest
    {
        ftCashBoxID = builder.CashBoxId,
        cbReceiptMoment = DateTime.UtcNow,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8),
        cbChargeItems = [],
        cbPayItems = [],
        ftReceiptCase = ReceiptCase.InitialOperationReceipt0x4001.WithCountry(builder.Market)
    }).ConfigureAwait(false);
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
}

var requests = Directory.EnumerateDirectories(Path.Join(AppContext.BaseDirectory, "json-requests", builder.Market.ToUpperInvariant())).ToDictionary(k => Path.GetFileName(k)!, d => Directory.EnumerateFiles(d));

{
    var response = await middleware.SignJson(await File.ReadAllTextAsync(requests["SignRequestReceipt_ZeroReceipt"].First()));
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
}

// {
//     var response = await sign(new ReceiptRequest
//     {
//         ftCashBoxID = builder.CashBoxId,
//         cbReceiptMoment = DateTime.UtcNow,
//         cbTerminalID = "1",
//         cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8),
//         cbChargeItems = [
//             builder.ChargeItem
//                 .WithAmount(100)
//                 .WithQuantity(1)
//                 .WithCase(ChargeItemCase.NormalVatRate
//                     .WithVersion(2)
//                     .WithTypeOfService(ChargeItemCaseTypeOfService.Delivery))
//                 .Build()
//         ],
//         cbPayItems = [
//             new PayItem{
//                 Description = "cash",
//                 Quantity = 1,
//                 Amount = 100,
//                 ftPayItemCase = PayItemCase.CashPayment.WithVersion(2)
//             }
//         ],
//         cbReceiptAmount = 100,
//         ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry(builder.Market)
//     }).ConfigureAwait(false);
//     response.Should().NotBeNull();
//     response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
// }