using System.IO.Pipelines;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

public class TestHelpers
{
    public static async Task<ftCashBoxConfiguration> GetConfigurationAsync(Guid cashBoxId, string accessToken)
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

    public static async Task<IssueResponse?> SendIssueAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
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

    public static async Task StoreDataAsync(string folder, string casename, long ticks, Func<string, Task<(ContentType contentType, PipeReader reader)>> journalMethod, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var result = await SendIssueAsync(receiptRequest, receiptResponse);

        var pdfdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf");
        var pdfcopydata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf&copy=true");
        var pngdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=png");
        var pngcopydata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=png&copy=true");

        var xmlData = await journalMethod(JsonSerializer.Serialize(new ifPOS.v1.JournalRequest
        {
            ftJournalType = 0x5054_2000_0000_0001,
            From = ticks
        }));
        // read data from pipereader


        var base_path = Path.Combine(folder);
        if (!Directory.Exists(base_path))
        {
            Directory.CreateDirectory(base_path);
        }
        File.WriteAllText($"{base_path}/{casename}.receiptrequest.json", JsonSerializer.Serialize(receiptRequest, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
        File.WriteAllText($"{base_path}/{casename}.receiptresponse.json", JsonSerializer.Serialize(receiptResponse, new JsonSerializerOptions
        {
            WriteIndented = true
        }));


        File.WriteAllBytes($"{base_path}/{casename}.receipt.pdf", await pdfdata.Content.ReadAsByteArrayAsync());
        File.WriteAllBytes($"{base_path}/{casename}.receipt.copy.pdf", await pdfcopydata.Content.ReadAsByteArrayAsync());
        File.WriteAllBytes($"{base_path}/{casename}.receipt.png", await pngdata.Content.ReadAsByteArrayAsync());
        File.WriteAllBytes($"{base_path}/{casename}.receipt.copy.png", await pngcopydata.Content.ReadAsByteArrayAsync());
        //File.WriteAllText($"{base_path}/{casename}_saft.xml", xmlData.reader.);
    }

}
