using System.IO.Pipelines;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using FluentAssertions.Primitives;

namespace fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;

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

    public static async Task<IssueResponse?> SendIssueAsync(Guid cashBoxId, string accessToken, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://possystem-api-sandbox.fiskaltrust.eu/v2/issue");
        request.Headers.Add("x-cashbox-id", cashBoxId.ToString());
        request.Headers.Add("x-cashbox-accesstoken", accessToken);
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

    public static async Task StoreDataAsync(Guid cashBoxId, string accessToken, string folder, string casename, long ticks, byte[] journalData, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var result = await SendIssueAsync(cashBoxId, accessToken, receiptRequest, receiptResponse);
        var pdfdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf");
        var pdfcopydata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=pdf&copy=true");
        var pngdata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=png");
        var pngcopydata = await new HttpClient().GetAsync(result?.DocumentURL + "?format=png&copy=true");
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
        File.WriteAllText($"{base_path}/{casename}_saft.xml", Encoding.GetEncoding("windows-1252").GetString(journalData), Encoding.GetEncoding("windows-1252"));
    }
}
