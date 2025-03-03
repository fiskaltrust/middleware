using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

[Collection("Sequential")]
public class PTCertificationTests
{
    private readonly Func<string, Task<string>> _signMethod;
    private readonly Func<string, Task<string>> _journalMethod;
    private readonly Guid _cashboxid;

    public async Task<ftCashBoxConfiguration> GetConfigurationAsync(Guid cashBoxId, string accessToken)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.BaseAddress = new Uri("https://helipad-sandbox.fiskaltrust.cloud");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxId.ToString());
            httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);
            var result = await httpClient.GetAsync("api/configuration");
            var content = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                if (string.IsNullOrEmpty(content))
                {
                    throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
                }

                var configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content) ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
                configuration.TimeStamp = DateTime.UtcNow.Ticks;
                return configuration;
            }
            else
            {
                throw new Exception($"{content}");
            }
        }
    }

    public async Task<(QueuePTBootstrapper bootstrapper, Guid cashBoxId)> InitializeQueueGRBootstrapperAsync()
    {
        var cashBoxId = Guid.Parse(Constants.CASHBOX_CERTIFICATION_ID);
        var accessToken = Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN;
        var configuration = await GetConfigurationAsync(cashBoxId, accessToken);
        var queue = configuration.ftQueues?.First() ?? throw new Exception($"The configuration for {cashBoxId} is empty and therefore not valid.");
        var bootstrapper = new QueuePTBootstrapper(queue.Id, new LoggerFactory(), queue.Configuration ?? new Dictionary<string, object>());
        return (bootstrapper, cashBoxId);
    }

    private async Task ValidateMyData(ReceiptRequest receiptRequest, [CallerMemberName] string caller = "")
    {
        using var scope = new AssertionScope();
        await ExecuteMiddleware(receiptRequest, caller);
    }

    public PTCertificationTests()
    {
        (var bootstrapper, var cashBoxId) = Task.Run(() => InitializeQueueGRBootstrapperAsync()).Result;
        _signMethod = bootstrapper.RegisterForSign();
        _journalMethod = bootstrapper.RegisterForJournal();
        _cashboxid = cashBoxId;
    }

#pragma warning disable
    private async Task ExecuteMiddleware(ReceiptRequest receiptRequest, string caller)
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
        await StoreDataAsync(caller, caller, ticks, receiptRequest, receiptResponse);
    }

    private async Task<IssueResponse?> SendIssueAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://possystem-api-sandbox.fiskaltrust.eu/v2/issue");
        request.Headers.Add("x-cashbox-id", Constants.CASHBOX_CERTIFICATION_ID);
        request.Headers.Add("x-cashbox-accesstoken", Constants.CASHBOX_CERTIFICATION_ACCESSTOKEN);
        var data = JsonSerializer.Serialize(new
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        });
        request.Headers.Add("x-operation-id", Guid.NewGuid().ToString());
        var content = new StringContent(data, null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        return await response.Content.ReadFromJsonAsync<IssueResponse>();
    }

    public async Task StoreDataAsync(string folder, string casename, long ticks,  ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var result = await SendIssueAsync(receiptRequest, receiptResponse);

        var pdfdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf");
        var pngdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=png");

        var xmlData = await _journalMethod(JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
        {
            ftJournalType = 0x5054_2000_0000_0001,
            From = ticks
        }));

        var base_path = Path.Combine("C:\\Users\\stefa\\OneDrive\\Desktop\\Portugal_Registration\\Examples", folder);
        if (!Directory.Exists(base_path))
        {
            Directory.CreateDirectory(base_path);
        }
        File.WriteAllText($"{base_path}\\{casename}.receiptrequest.json", JsonSerializer.Serialize(receiptRequest, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
        File.WriteAllText($"{base_path}\\{casename}.receiptresponse.json", JsonSerializer.Serialize(receiptResponse, new JsonSerializerOptions
        {
            WriteIndented = true
        }));


        File.WriteAllBytes($"{base_path}\\{casename}.receipt.pdf", await pdfdata.Content.ReadAsByteArrayAsync());
        File.WriteAllBytes($"{base_path}\\{casename}.receipt.png", await pngdata.Content.ReadAsByteArrayAsync());
        File.WriteAllText($"{base_path}\\{casename}_saft.xml", xmlData);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_1()
    {
        var receiptRequest = PTCertificationExamples.Case_5_1();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_2()
    {
        var receiptRequest = PTCertificationExamples.Case_5_2();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_3()
    {
        var receiptRequest = PTCertificationExamples.Case_5_3();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_4()
    {
        var receiptRequest = PTCertificationExamples.Case_5_4();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_5()
    {
        var receiptRequest = PTCertificationExamples.Case_5_5();
        await ValidateMyData(receiptRequest);
    }


    [Fact]
    public async Task PTCertificationExamples_Case_5_6()
    {
        var receiptRequest = PTCertificationExamples.Case_5_6();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_7()
    {
        var receiptRequest = PTCertificationExamples.Case_5_7();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_8()
    {
        var receiptRequest = PTCertificationExamples.Case_5_8();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_9()
    {
        var receiptRequest = PTCertificationExamples.Case_5_9();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_10()
    {
        var receiptRequest = PTCertificationExamples.Case_5_10();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_11()
    {
        var receiptRequest = PTCertificationExamples.Case_5_11();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_12()
    {
        var receiptRequest = PTCertificationExamples.Case_5_12();
        await ValidateMyData(receiptRequest);
    }

    [Fact]
    public async Task PTCertificationExamples_Case_5_13()
    {
        var receiptRequest = PTCertificationExamples.Case_5_13();
        await ValidateMyData(receiptRequest);
    }
}