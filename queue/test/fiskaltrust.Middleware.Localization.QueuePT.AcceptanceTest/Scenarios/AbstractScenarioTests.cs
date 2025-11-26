using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;
using fiskaltrust.storage.V0;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Validation;
using System.Net.Mime;
using System.IO.Pipelines;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Scenarios;

public class AbstractScenarioTests
{
    private readonly Func<string, Task<string>> _signProcessor;
    private readonly Func<string, Task<(ContentType contentType, PipeReader reader)>> _journalProcessor;
    private readonly Guid _queueId;
    private readonly Guid _cashBoxId;

    public AbstractScenarioTests() : this(Guid.NewGuid(), Guid.NewGuid())
    {

    }

    public AbstractScenarioTests(Guid cashBoxId, Guid queueId)
    {
        _queueId = queueId;
        _cashBoxId = cashBoxId;

        var mockSscd = new MockPTSSCD();

        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox
                {
                    ftCashBoxId = _cashBoxId,
                    TimeStamp = DateTime.UtcNow.Ticks
                }) 
            },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue>
            {
                new ftQueue
                {
                    ftQueueId = _queueId,
                    ftCashBoxId = _cashBoxId,
                    StartMoment = DateTime.UtcNow
                }
            })
            },
            { "init_ftQueuePT", JsonSerializer.Serialize(new List<ftQueuePT>
            {
                new ftQueuePT
                {
                    ftQueuePTId = _queueId,
                    IssuerTIN = "980833310"
                }
            })},
            { "init_ftSignaturCreationUnitPT", JsonSerializer.Serialize(new List<ftSignaturCreationUnitPT>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration
            {
                Account = new AccountMasterData
                {
                    AccountId = Guid.NewGuid(),
                    AccountName = "FISKALTRUST CONSULTING GMBH - SUCURSAL EM",
                    VatId = "980833310",
                    Street = "AV DA REPUBLICA N 35 4 ANDAR",
                    Zip = "1050-189",
                    City = "Lisboa",
                    Country = "PT",
                    TaxId = "980833310"
                }
            })}
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(_queueId, configuration, loggerFactory);
        var bootstrapper = new QueuePTBootstrapper(_queueId, loggerFactory, configuration, mockSscd, storageProvider);
        _signProcessor = bootstrapper.RegisterForSign();
        _journalProcessor = bootstrapper.RegisterForJournal();
    }

    public async Task<byte[]> ExecuteJournal(JournalRequest journalRequest)
    {
        var (contentType, reader) = await _journalProcessor(JsonSerializer.Serialize(journalRequest));
        using var ms = new MemoryStream();
        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;
            if (buffer.Length > 0)
            {
                foreach (var segment in buffer)
                {
                    await ms.WriteAsync(segment);
                }
            }
            reader.AdvanceTo(buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }
        return ms.ToArray();
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
