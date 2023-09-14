using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using Newtonsoft.Json;

public class CustomRTServerClient
{
    private readonly HttpClient _adminHttpClient;
    private readonly HttpClient _queueHttpClient;
    private readonly string _password;

    public CustomRTServerClient(CustomRTServerConfiguration customRTServerConfiguration)
    {
        if (string.IsNullOrEmpty(customRTServerConfiguration.ServerUrl))
        {
            throw new NullReferenceException("ServerUrl is not set.");
        }

        _adminHttpClient = new HttpClient
        {
            BaseAddress = new Uri(customRTServerConfiguration.ServerUrl),
        };
        _queueHttpClient = new HttpClient
        {
            BaseAddress = new Uri(customRTServerConfiguration.ServerUrl),
        };
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{customRTServerConfiguration.Username}:{customRTServerConfiguration.Password}"));
        _adminHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
        if (!string.IsNullOrEmpty(customRTServerConfiguration.AccountMasterData))
        {
            var accountMasterData = JsonConvert.DeserializeObject<AccountMasterData>(customRTServerConfiguration.AccountMasterData);
            _password = accountMasterData.AccountId.ToString();
        }
        else
        {
            _password = customRTServerConfiguration.Password;
        }
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
        var result = await _adminHttpClient.PostAsync("/getDeviceMemStatus.php", new StringContent(JsonConvert.SerializeObject(request)));
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

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/getDailyStatus.php")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var result = await _queueHttpClient.SendAsync(requestMessage);
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
        var result = await _adminHttpClient.PostAsync("/getDailyStatusArray.php", new StringContent(JsonConvert.SerializeObject(request)));
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

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/getDailyOpen.php")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var result = await _queueHttpClient.SendAsync(requestMessage);
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

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/insertZDocument.php")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var result = await _queueHttpClient.SendAsync(requestMessage);
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertZDocumentResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentAsync(string cashuuid, CommercialDocument commercialDocument)
    {
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/insertFiscalDocument2.php")
        {
            Content = new StringContent(JsonConvert.SerializeObject(commercialDocument))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var result = await _queueHttpClient.SendAsync(requestMessage);
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertFiscalDocumentResponse>(resultContent);

        if (data.responseCode == 1201)
        {
            var message = $"""
Calling endpoint {result.RequestMessage?.RequestUri} failed with error code {data.responseCode}. 
Messagio: Reso/annulo: documento non travoto
Esempio: È stato indicato un documento di refierimento inesistente
Azione Corretiva: Verificare i dati insertiti e ripetere l'operazione. 
""";
            throw new CustomRTServerCommunicationException(message, data.responseCode);
        }

        if (data.responseCode == 1206)
        {
            var message = $"""
Calling endpoint {result.RequestMessage?.RequestUri} failed with error code {data.responseCode}. 
Messagio: Valore non valido
Esempio: Il valore del campo "amount" del metodo "insertFiscalDocument" o del metodo "insertFiscalDocument2" è negatiov o non numerico
Azione Corretiva: Verificare i dati insertiti e ripetere l'operazione.
""";
            throw new CustomRTServerCommunicationException(message, data.responseCode);
        }

        if (data.responseCode != 0)
        {
            var message = $"""
Calling endpoint {result.RequestMessage?.RequestUri} failed with error code {data.responseCode}. 
""";
            throw new CustomRTServerCommunicationException(message, data.responseCode);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayAsync(string cashuuid, List<CommercialDocument> commercialDocuments)
    {
        var request = new
        {
            fiscalArray = commercialDocuments
        };
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/insertFiscalDocumentArray.php")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var result = await _queueHttpClient.SendAsync(requestMessage);
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
        var result = await _adminHttpClient.PostAsync("/insertCashRegister.php", new StringContent(JsonConvert.SerializeObject(request)));
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
        var result = await _adminHttpClient.PostAsync("/updateCashRegister.php", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<CancelCashRegisterResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentLotteryAsync(string cashuuid, FDocumentLottery fiscalData, QrCodeData qrCodeData)
    {
        var request = new
        {
            fiscalData,
            qrCodeData
        };

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/insertFiscalDocumentLottery.php")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var result = await _queueHttpClient.SendAsync(requestMessage);
        // TODO Check error
        var resultContent = await result.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<InsertFiscalDocumentResponse>(resultContent);
        if (data.responseCode != 0)
        {
            throw new Exception(data.responseDesc);
        }
        return data;
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayLotteryAsync(string cashuuid, FDocumentLotteryArray fiscalData)
    {
        var request = new
        {
            fiscalArray = fiscalData
        };

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/insertFiscalDocumentArrayLottery.php")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var result = await _queueHttpClient.SendAsync(requestMessage);
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
