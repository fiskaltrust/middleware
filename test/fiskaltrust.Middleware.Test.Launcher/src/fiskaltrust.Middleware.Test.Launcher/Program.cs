using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Test.Launcher.Helpers;
using fiskaltrust.Middleware.Test.Launcher.Helpers.IT;
using fiskaltrust.storage.serialization.V0;
using FluentAssertions;
using Newtonsoft.Json;

var builder = new CashBoxBuilder("IT" switch
{
    "IT" => new CashBoxBuilderIT(),
    _ => throw new NotImplementedException()
},
    JsonConvert.DeserializeObject<PackageConfiguration>(await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, "queue-configuration.json"))),
    JsonConvert.DeserializeObject<PackageConfiguration>(await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, "scu-configuration.json")))
);

var middleware = builder.Build();

{
    var response = await middleware.EchoAsync(new EchoRequest { Message = "Hello, Middleware!" });
    response.Should().NotBeNull();
    response.Message.Should().BeEquivalentTo("Hello, Middleware!");
}

{
    var response = await middleware.SignAsync(new ReceiptRequest
    {
        ftCashBoxID = builder.CashBoxId.ToString(),
        cbReceiptMoment = DateTime.UtcNow,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8),
        cbChargeItems = [],
        cbPayItems = [],
        ftReceiptCase = (long) (builder.CountryCode | 0x2000_0000_0000 | 0x4001)
    }).ConfigureAwait(false);
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => (x | 0xFFFF_FFFF) == 0x0, because: response.ftSignatures[0].Data);
    response.ftSignatures.Should().NotBeEmpty();
}

{
    var response = await middleware.SignAsync(new ReceiptRequest
    {
        ftCashBoxID = builder.CashBoxId.ToString(),
        cbReceiptMoment = DateTime.UtcNow,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8),
        cbChargeItems = [
            new ChargeItem {
                Description = "test",
                Amount = 120,
                Quantity = 1,
                VATRate = 20,
                VATAmount = 20,
                ftChargeItemCase = (long) (builder.CountryCode | 0x2000_0000_0000 | 0x0013)
            }
        ],
        cbPayItems = [
            new PayItem{
                Description = "cash",
                Quantity = 1,
                Amount = 120,
                ftPayItemCase = (long) (builder.CountryCode | 0x2000_0000_0000 | 0x0001)
            }
        ],
        cbReceiptAmount = 120,
        ftReceiptCase = (long) (builder.CountryCode | 0x2000_0000_0000 | 0x0001)
    }).ConfigureAwait(false);
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => (x | 0xFFFF_FFFF) != 0xEEEE_EEEE && (x | 0xFFFF_FFFF) != 0xFFFF_FFFF);
    response.ftState.Should().Match(x => (x | 0xFFFF_FFFF) == 0, because: response.ftSignatures[0].Data);
}