using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.storage.V0;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

public class NewCashBox
{
    public string cashBoxId { get; set; }
    public string accessToken { get; set; }
    public ftCashBoxConfiguration configuration { get; set; }
}

public class TestRunner
{
    private readonly Func<string, Task<string>> _signMethod;
    private readonly Func<string, Task<(System.Net.Mime.ContentType contentType, PipeReader reader)>> _journalMethod;
    public readonly Guid _cashboxid;
    public readonly string _accessToken;

    private TestRunner(Guid cashBoxId, string accessToken)
    {
        _cashboxid = cashBoxId;
        _accessToken = accessToken;
        var bootstrapper = Task.Run(() => InitializeQueueGRBootstrapperAsync()).Result;
        _signMethod = bootstrapper.RegisterForSign();
        _journalMethod = bootstrapper.RegisterForJournal();
    }

    public async Task<byte[]> ExecuteJournal(JournalRequest journalRequest)
    {
        var (contentType, reader) = await _journalMethod(JsonSerializer.Serialize(journalRequest));
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

    public async Task<(long ticks, ReceiptResponse receiptResponse)> ExecuteSign(ReceiptRequest receiptRequest)
    {
        receiptRequest.ftCashBoxID = _cashboxid;
        var ticks = DateTime.UtcNow.Ticks;
        var exampleCashSalesResponse = await _signMethod(JsonSerializer.Serialize(receiptRequest));
        var receiptResponse = JsonSerializer.Deserialize<ReceiptResponse>(exampleCashSalesResponse)!;
        if ((receiptResponse.ftState & (State) 0xF) != State.Success)
        {
            var errors = "The receiptResponse.ftState is not Success";
            errors += Environment.NewLine;
            errors += string.Join(Environment.NewLine, receiptResponse.ftSignatures.Select(x => x.Data));
            throw new Exception(errors);
        }
        return (ticks, receiptResponse);
    }

    public static async Task<TestRunner> InitializeDryTestRun(Guid accountId, string accessToken)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://templates-sandbox.fiskaltrust.cloud/api/v1/configuration?outlet_number=1&description=SKE-Certification202506");
        request.Headers.Add("accountid", accountId.ToString());
        request.Headers.Add("accesstoken", accessToken);
        var content = new StringContent("{\r\n    \"ftCashBoxId\": \"|[cashbox_id]|\",\r\n    \"ftSignaturCreationDevices\": [],\r\n    \"ftQueues\": [\r\n        {\r\n            \"Id\": \"|[queue0_id]|\",\r\n            \"Package\": \"fiskaltrust.Middleware.Queue.AzureTableStorage\",\r\n            \"Configuration\": {\r\n                \"storageaccountname\": \"fta236wecloudcashbox001\",\r\n                \"init_ftQueue\": [\r\n                    {\r\n                        \"ftQueueId\": \"|[queue0_id]|\",\r\n                        \"ftCashBoxId\": \"|[cashbox_id]|\",\r\n                        \"CountryCode\": \"PT\",\r\n                        \"Timeout\": 15000\r\n                    }\r\n                ],\r\n                \"init_ftQueuePT\": [\r\n                    {\r\n                        \"ftQueuePTId\": \"|[queue0_id]|\",\r\n                        \"CashBoxIdentification\": \"fiskaltrust|[count]|\",\r\n                        \"TaxRegion\": \"PT\",\r\n                        \"IssuerTIN\": \"980833310\",\r\n                        \"ATCUD\": \"TESTATCUD\",\r\n                        \"SimplifiedInvoiceSeries\": \"TESTSEQUENCE\",\r\n                        \"SimplifiedInvoiceSeriesNumerator\": 0\r\n                    }\r\n                ],\r\n                \"init_ftSignaturCreationUnitPT\": [\r\n                    {\r\n                        \"Url\": \"https://signing-sandbox.fiskaltrust.pt/\"\r\n                    }\r\n                ]\r\n            },\r\n            \"Url\": [\r\n                \"https://cloucashbox-sandbox.fiskaltrust.pt/\"\r\n            ]\r\n        }\r\n    ]\r\n}", null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<NewCashBox>(responseContent);
        await UpdateReceiptSettigns(Guid.Parse(result.cashBoxId), result.accessToken, result.configuration.ftQueues!.First().Id);
        var testRunner = new TestRunner(Guid.Parse(result!.cashBoxId), result.accessToken);
        await testRunner.PrepareCashBox();
        return testRunner;
    }

    public static async Task UpdateReceiptSettigns(Guid cashBoxId, string accessToken, Guid queueId)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://receipts-sandbox.fiskaltrust.eu/v1/configuration/{queueId}/receiptsettings");
        request.Headers.Add("cashboxid", cashBoxId.ToString());
        request.Headers.Add("accesstoken", accessToken);
        var content = new StringContent("{\r\n    \"name1Text\": \"FISKALTRUST CONSULTING GMBH - SUCURSAL EM\",\r\n    \"address1Text\": \"AV DA REPUBLICA N 35 4 ANDAR\",\r\n    \"vat\": \"980833310\",\r\n    \"logourl\": null,\r\n    \"logowidth\": null,\r\n    \"logoheight\": null,\r\n    \"address1PostalCode\": \"1050-189\",\r\n    \"address1City\": \"Lisboa\",\r\n    \"Country\": \"PT\",\r\n    \"footertext\": null,\r\n    \"showFeedback\": null,\r\n    \"printBewirtungsBeleg\": null,\r\n    \"feedbackRedirectAppId\": null,\r\n    \"shareButtonAppIds\": null\r\n}", null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }

    public static async Task<TestRunner> InitializeWithCashBox(Guid cashBoxId, string accessToken)
    {
        return new TestRunner(cashBoxId, accessToken);
    }

    private async Task<QueuePTBootstrapper> InitializeQueueGRBootstrapperAsync()
    {
        var configuration = await TestHelpers.GetConfigurationAsync(_cashboxid, _accessToken);
        var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {_cashboxid} is empty and therefore not valid.");
        var ptSSCD = new InMemorySCU(new ftSignaturCreationUnitPT
        {
            PrivateKey = File.ReadAllText("C:\\secure\\PrivateKey.pem"),
            SoftwareCertificateNumber = "9999"
        });
        var bootstrapper = new QueuePTBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>(), ptSSCD);
        return bootstrapper;
    }

    private async Task PrepareCashBox()
    {
        var result = await _signMethod(JsonSerializer.Serialize(new ReceiptRequest
        {
            ftCashBoxID = _cashboxid,
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [],
            cbPayItems = [],
            cbUser = 1,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0000).WithCase(ReceiptCase.InitialOperationReceipt0x4001),
        }));
    }
}
