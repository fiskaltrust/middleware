using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using Newtonsoft.Json;

public class CustomRTServerClient
{
    private readonly HttpClient _httpClient;

    public CustomRTServerClient(CustomRTServerConfiguration customRTServerConfiguration)
    {
        if (string.IsNullOrEmpty(customRTServerConfiguration.ServerUrl))
        {
            throw new NullReferenceException("ServerUrl is not set.");
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(customRTServerConfiguration.ServerUrl),
        };
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{customRTServerConfiguration.Username}:{customRTServerConfiguration.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
    }

    public async Task<GetDeviceMemStatusResponse> GetDeviceMemStatusAsync()
    {
        var request = new
        {
            data = new
            {
                type = "3"
            }
        };
        var result = await _httpClient.PostAsync("/getDeviceMemStatus.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<GetDeviceMemStatusResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<GetDailyStatusResponse> GetDailyStatusAsync(string cashuuid)
    {
        var request = new
        {
            data = new
            {
                cashuuid
            }
        };
        var result = await _httpClient.PostAsync("/getDailyStatus.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<GetDailyStatusResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<GetDailyStatusArrayResponse> GetDailyStatusArrayAsync()
    {
        var request = new
        {
            data = new { }
        };
        var result = await _httpClient.PostAsync("/getDailyStatusArray.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<GetDailyStatusArrayResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<GetDailyOpenResponse> GetDailyOpenAsync(string cashuuid, DateTime dateTime)
    {
        var request = new
        {
            data = new
            {
                cashuuid,
                dtime = dateTime.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };
        var result = await _httpClient.PostAsync("/getDailyOpen.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<GetDailyOpenResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertZDocumentResponse> InsertZDocumentAsync(string cashuuid, DateTime dateTime, long znum, string amount)
    {
        var request = new
        {
            data = new
            {
                cashuuid,
                znum,
                dtime = dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                amount
            }
        };
        var result = await _httpClient.PostAsync("/insertZDocument.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertZDocumentResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentAsync(CommercialDocument commercialDocument)
    {
        var result = await _httpClient.PostAsync("/insertFiscalDocument2.php", new StringContent(JsonConvert.SerializeObject(commercialDocument)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertFiscalDocumentResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayAsync(FDocumentArray fiscalData)
    {
        var request = new
        {
            fiscalArray = fiscalData
        };
        var result = await _httpClient.PostAsync("/insertFiscalDocumentArray.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertFiscalDocumentArrayResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertCashRegisterAsyncResponse> InsertCashRegisterAsync(string description, string shop, string name, string password, string cf)
    {
        var request = new
        {
            data = new
            {
                description,
                shop,
                name,
                password,
                cf
            }
        };
        var result = await _httpClient.PostAsync("/insertCashRegister.php", new StringContent(JsonConvert.SerializeObject(request)));
        var resultContent = await result.Content.ReadAsStringAsync();
        if (result.IsSuccessStatusCode)
        {
            var data = JsonConvert.DeserializeObject<InsertCashRegisterAsyncResponse>(resultContent);
            if (data.responseCode != 0)
            {
                throw new Exception(data.responseDesc);
            }
            return data;
        }
        else
        {
            throw new Exception($"Something went wrong while communicating with the RT Server. Statuscode: {result.StatusCode}. Reasonphrase: {result.ReasonPhrase}. Content: {resultContent}.");
        }
    }

    public async Task<CancelCashRegisterResponse> CancelCashRegisterAsync(string cashuuid, string cf)
    {
        var request = new
        {
            data = new
            {
                type = 1,
                cf,
                cashuuid
            }
        };
        var result = await _httpClient.PostAsync("/updateCashRegister.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<CancelCashRegisterResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentLotteryAsync(FDocumentLottery fiscalData, QrCodeData qrCodeData)
    {
        var request = new
        {
            fiscalData,
            qrCodeData
        };
        var result = await _httpClient.PostAsync("/insertFiscalDocumentLottery.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertFiscalDocumentResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayLotteryAsync(FDocumentLotteryArray fiscalData)
    {
        var request = new
        {
            fiscalArray = fiscalData
        };
        var result = await _httpClient.PostAsync("/insertFiscalDocumentArrayLottery.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertFiscalDocumentArrayResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }
}
