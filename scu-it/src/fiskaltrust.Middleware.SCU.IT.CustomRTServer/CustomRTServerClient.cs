using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class CustomRTServerClient
{
    private readonly HttpClient _httpClient;
    private readonly string _password;
    private readonly CustomRTServerConfiguration _customRTServerConfiguration;
    private readonly ILogger<CustomRTServerClient> _logger;

    public CustomRTServerClient(CustomRTServerConfiguration customRTServerConfiguration, ILogger<CustomRTServerClient> logger)
    {
        if (string.IsNullOrEmpty(customRTServerConfiguration.ServerUrl))
        {
            throw new NullReferenceException("ServerUrl is not set.");
        }

        if (customRTServerConfiguration.DisabelSSLValidation)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(customRTServerConfiguration.ServerUrl),
                Timeout = TimeSpan.FromMilliseconds(customRTServerConfiguration.RTServerHttpTimeoutInMs)
            };
        }
        else
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(customRTServerConfiguration.ServerUrl),
                Timeout = TimeSpan.FromMilliseconds(customRTServerConfiguration.RTServerHttpTimeoutInMs)
            };
        }

        if (!string.IsNullOrEmpty(customRTServerConfiguration.AccountMasterData))
        {
            var accountMasterData = JsonConvert.DeserializeObject<AccountMasterData>(customRTServerConfiguration.AccountMasterData);
            _password = accountMasterData.AccountId.ToString();
        }
        else
        {
            _password = customRTServerConfiguration.Password;
        }
        _customRTServerConfiguration = customRTServerConfiguration;
        _logger = logger;
    }

    public async Task<GetDeviceMemStatusResponse> GetDeviceMemStatusAsync() => await PerformCallToRTServerWithAdminAsync<GetDeviceMemStatusResponse>("/getDeviceMemStatus.php", JsonConvert.SerializeObject(new
    {
        data = new
        {
            type = "3"
        }
    }));

    public async Task<GetDailyStatusArrayResponse> GetDailyStatusArrayAsync() => await PerformCallToRTServerWithAdminAsync<GetDailyStatusArrayResponse>("/getDailyStatusArray.php", JsonConvert.SerializeObject(new
    {
        data = new { }
    }));

    public async Task<GetDailyStatusResponse> GetDailyStatusAsync(string cashuuid)
    {
        var request = new
        {
            data = new
            {
                cashuuid
            }
        };

        return await PerformCallToRTServerAsync<GetDailyStatusResponse>("/getDailyStatus.php", cashuuid, JsonConvert.SerializeObject(request));
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

        return await PerformCallToRTServerAsync<GetDailyOpenResponse>("/getDailyOpen.php", cashuuid, JsonConvert.SerializeObject(request));
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
        return await PerformCallToRTServerAsync<InsertZDocumentResponse>("/insertZDocument.php", cashuuid, JsonConvert.SerializeObject(request));
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentAsync(string cashuuid, CommercialDocument commercialDocument) => await PerformCallToRTServerAsync<InsertFiscalDocumentResponse>("/insertFiscalDocument2.php", cashuuid, JsonConvert.SerializeObject(commercialDocument));

    private static void ThrowExceptionForErrorCode(string endpoint, CustomRTDResponse data)
    {
        if (data.responseCode == 1201)
        {
            var message = $"""
Calling endpoint '{endpoint}' failed with error code {data.responseCode}. 
Messagio: Reso/annulo: documento non travoto
Esempio: È stato indicato un documento di refierimento inesistente
Azione Corretiva: Verificare i dati insertiti e ripetere l'operazione. 
""";
            throw new CustomRTServerCommunicationException(message, data.responseCode);
        }

        if (data.responseCode == 1206)
        {
            var message = $"""
Calling endpoint '{endpoint}' failed with error code {data.responseCode}. 
Messagio: Valore non valido
Esempio: Il valore del campo "amount" del metodo "insertFiscalDocument" o del metodo "insertFiscalDocument2" è negatiov o non numerico
Azione Corretiva: Verificare i dati insertiti e ripetere l'operazione.
""";
            throw new CustomRTServerCommunicationException(message, data.responseCode);
        }

        if (data.responseCode != 0)
        {
            var message = $"""
Calling endpoint '{endpoint}' failed with error code {data.responseCode}. 

{data.responseDesc}
""";
            throw new CustomRTServerCommunicationException(message, data.responseCode);
        }
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayAsync(string cashuuid, List<CommercialDocument> commercialDocuments)
    {
        var request = new
        {
            fiscalArray = commercialDocuments
        };
        return await PerformCallToRTServerAsync<InsertFiscalDocumentArrayResponse>("/insertFiscalDocumentArray.php", cashuuid, JsonConvert.SerializeObject(request));
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
        return await PerformCallToRTServerWithAdminAsync<InsertCashRegisterAsyncResponse>("/insertCashRegister.php", JsonConvert.SerializeObject(request));
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
        return await PerformCallToRTServerAsync<CancelCashRegisterResponse>("/updateCashRegister.php", cashuuid, JsonConvert.SerializeObject(request));
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentLotteryAsync(string cashuuid, FDocumentLottery fiscalData, QrCodeData qrCodeData)
    {
        var request = new
        {
            fiscalData,
            qrCodeData
        };
        return await PerformCallToRTServerAsync<InsertFiscalDocumentResponse>("/insertFiscalDocumentLottery.php", cashuuid, JsonConvert.SerializeObject(request));
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayLotteryAsync(string cashuuid, List<CommercialDocument> commercialDocuments)
    {
        var request = new
        {
            fiscalArray = commercialDocuments
        };
        return await PerformCallToRTServerAsync<InsertFiscalDocumentArrayResponse>("/insertFiscalDocumentArrayLottery.php", cashuuid, JsonConvert.SerializeObject(request));
    }

    private async Task<TResponse> PerformCallToRTServerAsync<TResponse>(string endpoint, string cashuuid, string payload) where TResponse : CustomRTDResponse
    {
        if (string.IsNullOrEmpty(_password))
        {
            return await PerformCallToRTServerWithAdminAsync<TResponse>(endpoint, payload);
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload)
        };
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cashuuid}:{_password}"));
        requestMessage.Headers.Add("Authorization", $"Basic {authHeader}");
        var result = await _httpClient.SendAsync(requestMessage);
        var resultContent = await result.Content.ReadAsStringAsync();
        if (result.IsSuccessStatusCode)
        {
            var data = JsonConvert.DeserializeObject<TResponse>(resultContent);
            if (data.responseCode != 0)
            {
                #pragma warning disable
                try
                {
                    ThrowExceptionForErrorCode(endpoint, data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Calling endpoint '{endpoint}' failed with error code {data.responseCode}. {data.responseDesc}");
                    if(!_customRTServerConfiguration.IgnoreRTServerErrors)
                    {
                        throw;
                    }
                }
            }
            return data;
        }
        else
        {
            throw new Exception($"Something went wrong while communicating with the RT Server. Statuscode: {result.StatusCode}. Reasonphrase: {result.ReasonPhrase}. Content: {resultContent}.");
        }
    }

    private async Task<TResponse> PerformCallToRTServerWithAdminAsync<TResponse>(string endpoint, string payload) where TResponse : CustomRTDResponse
    {
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_customRTServerConfiguration.Username}:{_customRTServerConfiguration.Password}"));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload)
        };
        requestMessage.Headers.Add("Authorization", $"Basic {authHeader}");
        var result = await _httpClient.SendAsync(requestMessage);
        var resultContent = await result.Content.ReadAsStringAsync();
        if (result.IsSuccessStatusCode)
        {
            var data = JsonConvert.DeserializeObject<TResponse>(resultContent);
            if (data.responseCode != 0)
            {
                try
                {
                    ThrowExceptionForErrorCode(endpoint, data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Calling endpoint '{endpoint}' failed with error code {data.responseCode}. {data.responseDesc}");
                }
            }
            return data;
        }
        else
        {
            throw new Exception($"Something went wrong while communicating with the RT Server. Statuscode: {result.StatusCode}. Reasonphrase: {result.ReasonPhrase}. Content: {resultContent}.");
        }
    }
}
