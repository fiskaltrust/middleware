using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers;
using fiskaltrust.storage.serialization.V0;
using FluentAssertions;

var market = Environment.GetEnvironmentVariable("MW_MARKET") ?? "PL";

// The cashbox configuration is what a cashbox is: ftCashBoxId, the queue package under ftQueues with
// its init_ tables, and the local SCU under ftSignaturCreationDevices — read with the same
// JsonConvert call the production launcher uses. Ids are not resolved from anywhere: they are pinned
// by standing in the file.
var cashBoxConfigurationFile = Environment.GetEnvironmentVariable("MW_CASHBOX_CONFIGURATION");
var cashBoxConfigurationName = cashBoxConfigurationFile ?? Path.Join("configuration", market, "cashbox-configuration.json");
var cashBoxConfigurationPath = Path.Join(AppContext.BaseDirectory, cashBoxConfigurationName);

// A file named explicitly and then not found is a mistake worth stopping for. The default one being
// absent is not: a market whose configuration carries certificates does not commit it (see
// .gitignore), and those markets are served by the per-package files below.
var cashBoxConfigurationText = File.Exists(cashBoxConfigurationPath)
    ? await File.ReadAllTextAsync(cashBoxConfigurationPath)
    : cashBoxConfigurationFile is { Length: > 0 }
        ? throw new FileNotFoundException($"The cashbox configuration '{cashBoxConfigurationName}' was not found.", cashBoxConfigurationPath)
        : null;

var cashBoxConfiguration = cashBoxConfigurationText is null
    ? null
    : Newtonsoft.Json.JsonConvert.DeserializeObject<ftCashBoxConfiguration>(cashBoxConfigurationText)
        ?? throw new InvalidOperationException($"The cashbox configuration '{cashBoxConfigurationName}' is empty.");

// ftPosSystemId is this launcher's one addition to the format: a pos system belongs to the caller,
// not to the cashbox, so ftCashBoxConfiguration has no property for it and ignores it. Read off the
// same text rather than through a type of its own — a portal export simply has none, and the pos
// system is then generated per start as before.
var posSystemId = cashBoxConfigurationText is null
    ? null
    : Newtonsoft.Json.Linq.JObject.Parse(cashBoxConfigurationText)["ftPosSystemId"]?.ToObject<Guid?>();

// Per-package files stay the override — swapping one SCU for another is a one-variable change — and
// they are the only source for a market that has no cashbox configuration. Their default names are
// the ones this launcher has always used, in the folder it has always read them from: none of them
// is committed (see .gitignore), so a market's files are the ones its developer put there, and
// moving where they are looked for would silently break that market's setup.
var queueConfiguration = Environment.GetEnvironmentVariable("MW_QUEUE_CONFIGURATION") is { Length: > 0 } queueFile
    ? PackageConfigurationFile.Read(queueFile)
    : Single(cashBoxConfiguration?.ftQueues, nameof(ftCashBoxConfiguration.ftQueues))
        ?? PackageConfigurationFile.Read(market == "PL" ? "queue-configuration-pl.json" : "queue-configuration.json");

var configuredScu = Single(cashBoxConfiguration?.ftSignaturCreationDevices, nameof(ftCashBoxConfiguration.ftSignaturCreationDevices));
var scuOverrideFile = Environment.GetEnvironmentVariable("MW_SCU_CONFIGURATION");
var scuConfiguration = scuOverrideFile is { Length: > 0 }
    ? PackageConfigurationFile.Read(scuOverrideFile)
    : configuredScu ?? PackageConfigurationFile.Read(market == "PL" ? "scu-configuration-pl-inmemory.json" : "scu-configuration-bizkaia.json");

// An override swaps the package, not the identity. The cashbox's init_ tables already name its SCU
// id — init_ftQueuePL.ftSignaturCreationUnitPLId points at it — and they are honoured as they stand,
// so adopting the override file's id as well would leave the queue wired to an SCU id that appears
// in no storage table.
if (scuOverrideFile is { Length: > 0 } && configuredScu is { } pinnedScu && pinnedScu.Id != Guid.Empty)
{
    if (scuConfiguration.Id != pinnedScu.Id)
    {
        Console.WriteLine(
            $"{cashBoxConfigurationName} pins the SCU id {pinnedScu.Id}; {scuOverrideFile} supplies the package "
            + $"{scuConfiguration.Package} under that id, not under its own.");
    }
    scuConfiguration.Id = pinnedScu.Id;
}

queueConfiguration.Configuration ??= [];
scuConfiguration.Configuration ??= [];

// Guid.Empty means "not pinned": the per-package files carry an all-zero Id, and the launcher then
// invents one per start as it always did.
var cashBoxId = Pinned(cashBoxConfiguration?.ftCashBoxId);

static Guid Pinned(Guid? configured) => configured is { } id && id != Guid.Empty ? id : Guid.NewGuid();

// More than one is refused rather than quietly reduced to the first: this hosts a single queue
// against a single SCU, and picking one of several would look like the file had been honoured.
static PackageConfiguration? Single(PackageConfiguration[]? packages, string property)
{
    var configured = packages?.Where(package => package is not null).ToList() ?? [];
    return configured.Count > 1
        ? throw new NotSupportedException($"The cashbox configuration has {configured.Count} entries in {property}; this launcher hosts one queue against one SCU.")
        : configured.FirstOrDefault();
}

var builder = new CashBoxBuilder(
    market switch
    {
        "ES" => (ICashBoxBuilder)new CashBoxBuilderES(),
        "PL" => new CashBoxBuilderPL(),
        _ => throw new NotImplementedException(),
    },
    queueConfiguration,
    scuConfiguration,
    cashBoxId,
    Pinned(posSystemId)
);

var middleware = builder.Build();

// With MW_HOST_URL the same middleware is served over HTTP instead of being driven by the fixed
// run below — for poking at business cases by hand, or pointing a POS at it.
if (Environment.GetEnvironmentVariable("MW_HOST_URL") is { Length: > 0 } hostUrl)
{
    await MiddlewareHost.RunAsync(builder, middleware, hostUrl, queueConfiguration, scuConfiguration);
    return;
}

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
        Currency = builder.Market == "PL" ? Currency.PLN : Currency.EUR,
        ftReceiptCase = ReceiptCase.InitialOperationReceipt0x4001.WithCountry(builder.Market)
    }).ConfigureAwait(false);
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
}

var requests = Directory.EnumerateDirectories(
        Path.Join(AppContext.BaseDirectory, "json-requests", builder.Market.ToUpperInvariant())
    )
    .ToDictionary(
        k => Path.GetFileName(k)!,
        d => Directory
            .EnumerateFiles(d)
            .Select<string, Func<Action<ReceiptRequest>, Task<ReceiptRequest>>>(d
                => async (b) =>
                {
                    var request = JsonSerializer.Deserialize<ReceiptRequest>(
                        (await File.ReadAllTextAsync(d))
                            .Replace("{{ ftCashBoxID }}", builder.CashBoxId.ToString())
                            .Replace("{{ ftPosSystemID }}", builder.PosSystemId.ToString()))!;
                    b(request);
                    return request;
                }
            )
        );

{
    var response = await middleware.Sign(await requests["SignRequestReceipt_ZeroReceipt"].First()(_ => { }));
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
}

{
    var response = await middleware.Sign(await requests["SignRequestReceipt_CashSaleReceipt"].First()(r => r.cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8)));
    response.Should().NotBeNull();
    response.ftState.Should().Match(x => !x!.Value.IsState(State.Error)).And.Match(x => !x!.Value.IsState(State.Fail));
}

{
    var response = await middleware.Sign(await requests["SignRequestReceipt_CashSaleReceipt"].First()(r => r.cbReceiptReference = Guid.NewGuid().ToString().Substring(0, 8)));
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