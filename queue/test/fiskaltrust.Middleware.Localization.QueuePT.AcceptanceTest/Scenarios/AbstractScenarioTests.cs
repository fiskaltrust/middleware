using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;
using fiskaltrust.storage.V0;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

public class AbstractScenarioTests
{
    private readonly Func<string, Task<string>> _signProcessor;
    private readonly Guid _queueId;
    private readonly Guid _cashBoxId;

    public AbstractScenarioTests()
    {
        _queueId = Guid.NewGuid();
        _cashBoxId = Guid.NewGuid();

        var mockSscd = new MockPTSSCD();

        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox
                {
                    ftCashBoxId = _cashBoxId,
                    TimeStamp = DateTime.UtcNow.Ticks
                }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue>
            {
                new ftQueue
                {
                    ftQueueId = _queueId,
                    ftCashBoxId = _cashBoxId,
                    StartMoment = DateTime.UtcNow
                }
            }) },
            { "init_ftQueuePT", JsonSerializer.Serialize(new List<ftQueuePT>
            {
                new ftQueuePT
                {
                    ftQueuePTId = _queueId,
                    IssuerTIN = "123456789"
                }
            }) },
            { "init_ftSignaturCreationUnitPT", JsonSerializer.Serialize(new List<ftSignaturCreationUnitPT>()) }
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(_queueId, configuration, loggerFactory);
        var bootstrapper = new QueuePTBootstrapper(_queueId, loggerFactory, configuration, mockSscd, storageProvider);
        _signProcessor = bootstrapper.RegisterForSign();
    }

    public async Task<(ReceiptRequest request, ReceiptResponse response)> ProcessReceiptAsync(string rawJson, long? ftReceiptCase = null)
    {
        var preparedJson = rawJson.Replace("{{$guid}}", Guid.NewGuid().ToString())
            .Replace("{{$isoTimestamp}}", DateTime.UtcNow.ToString("o"))
            .Replace("{{cashboxid}}", _cashBoxId.ToString());

        if (ftReceiptCase.HasValue)
        {
            preparedJson = preparedJson.Replace("{{ftReceiptCase}}", ftReceiptCase.Value.ToString());
        }

        var request = JsonSerializer.Deserialize<ReceiptRequest>(preparedJson)!;
        var responseJson = await _signProcessor(preparedJson);
        var response = JsonSerializer.Deserialize<ReceiptResponse>(responseJson)!;

        return (request, response);
    }
}
