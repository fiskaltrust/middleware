using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using Newtonsoft.Json;

public class CustomRTServerClient
{
    private readonly HttpClient _httpClient;

    public CustomRTServerClient(HttpClient httpClient, CustomRTServerConfiguration customRTServerConfiguration)
    {
        _httpClient = httpClient;
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{customRTServerConfiguration.Username}:{customRTServerConfiguration.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
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
        var result = _httpClient.PostAsync("/getdailystatus.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GetDailyStatusResponse>(resultContent);
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
        var result = _httpClient.PostAsync("/getDailyOpen.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GetDailyOpenResponse>(resultContent);
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
        var result = _httpClient.PostAsync("/insertZDocument.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<InsertZDocumentResponse>(resultContent);
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentAsync(FDocument fiscalData, QrCodeData qrCodeData)
    {
        var request = new
        {
            fiscalData,
            qrCodeData
        };
        var result = _httpClient.PostAsync("/insertFiscalDocument.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<InsertFiscalDocumentResponse>(resultContent);
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayAsync(FDocumentArray fiscalData)
    {
        var request = new
        {
            fiscalArray = fiscalData
        };
        var result = _httpClient.PostAsync("/insertFiscalDocumentArray.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<InsertFiscalDocumentArrayResponse>(resultContent);
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
        var result = _httpClient.PostAsync("/insertCashRegister.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<InsertCashRegisterAsyncResponse>(resultContent);
    }

    public async Task<UpdateCashRegisterResponse> UpdateCashRegisterAsync(string cashuuid, string password, string description, string cf)
    {
        var request = new
        {
            data = new
            {
                password,
                type = 0,
                desc = description,
                cf,
                cashuuid
            }
        };
        var result = _httpClient.PostAsync("/updateCashRegister.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<UpdateCashRegisterResponse>(resultContent);
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
        var result = _httpClient.PostAsync("/updateCashRegister.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<CancelCashRegisterResponse>(resultContent);
    }

    public async Task<ReactivateCanceledCashRegisterResponse> ReactivateCanceledCashRegisterAsync(string cashuuid, string cf)
    {
        var request = new
        {
            data = new
            {
                type = 3,
                cf,
                cashuuid
            }
        };
        var result = _httpClient.PostAsync("/updateCashRegister.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<ReactivateCanceledCashRegisterResponse>(resultContent);
    }

    public async Task<InsertFiscalDocumentResponse> InsertFiscalDocumentLotteryAsync(FDocumentLottery fiscalData, QrCodeData qrCodeData)
    {
        var request = new
        {
            fiscalData,
            qrCodeData
        };
        var result = _httpClient.PostAsync("/insertFiscalDocumentLottery.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<InsertFiscalDocumentResponse>(resultContent);
    }

    public async Task<InsertFiscalDocumentArrayResponse> InsertFiscalDocumentArrayLotteryAsync(FDocumentLotteryArray fiscalData)
    {
        var request = new
        {
            fiscalArray = fiscalData
        };
        var result = _httpClient.PostAsync("/insertFiscalDocumentArrayLottery.php/", new StringContent(JsonConvert.SerializeObject(request)));
        // TODO Check error
        var resultContent = await result.Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<InsertFiscalDocumentArrayResponse>(resultContent);
    }
}

public class GetDailyStatusResponse
{
    public string numberClosure { get; set; } = string.Empty;
    public string idClosure { get; set; } = string.Empty;
    public string fiscalBoxId { get; set; } = string.Empty;
    public string cashName { get; set; } = string.Empty;
    public string cashShop { get; set; } = string.Empty;
    public string cashDesc { get; set; } = string.Empty;
    public string cashToken { get; set; } = string.Empty;
    public string cashHmacKey { get; set; } = string.Empty;
    public string cashStatus { get; set; } = string.Empty;
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
    public ResponseBodyErrory? responseErr { get; set; }
    public string cashLastDocNumber { get; set; } = string.Empty;
    public string grandTotalDB { get; set; } = string.Empty;
    public string dateTimeServer { get; set; } = string.Empty;
}

public class GetDailyOpenResponse
{
    public string numberClosure { get; set; } = string.Empty;
    public string idClosure { get; set; } = string.Empty;
    public string fiscalBoxId { get; set; } = string.Empty;
    public string cashName { get; set; } = string.Empty;
    public string cashShop { get; set; } = string.Empty;
    public string cashDesc { get; set; } = string.Empty;
    public string cashToken { get; set; } = string.Empty;
    public string cashHmacKey { get; set; } = string.Empty;
    public string cashStatus { get; set; } = string.Empty;
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
    public ResponseBodyErrory? responseErr { get; set; }
    public string cashLastDocNumber { get; set; } = string.Empty;
    public string grandTotalDB { get; set; } = string.Empty;
}

public class InsertZDocumentResponse
{
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
    public ResponseBodyErrory? responseErr { get; set; }
}

public class InsertFiscalDocumentResponse
{
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
    public string responseSubCode { get; set; } = string.Empty;
    public int fiscalDocId { get; set; }
    public ResponseBodyErrory? responseErr { get; set; }
}

public class InsertFiscalDocumentArrayResponse
{
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
    public string responseSubCode { get; set; } = string.Empty;
    public List<InsertFiscalDocumentArraySubResponse> ArrayResponse { get; set; } = new List<InsertFiscalDocumentArraySubResponse>();
    public ResponseBodyErrory? responseErr { get; set; }
}

public class InsertFiscalDocumentArraySubResponse
{
    public int id { get; set; }
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
    public string responseSubCode { get; set; } = string.Empty;
    public int fiscalDocId { get; set; }
}

public class InsertCashRegisterAsyncResponse
{
    public string uCashUuid { get; set; } = string.Empty;
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
}

public class UpdateCashRegisterResponse
{
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
}

public class CancelCashRegisterResponse
{
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
}

public class ReactivateCanceledCashRegisterResponse
{
    public int responseCode { get; set; }
    public string responseDesc { get; set; } = string.Empty;
}

public class ResponseBodyErrory
{
    public int err_fm_present { get; set; }
    public int err_ej_present { get; set; }
    public int err_mkey_present { get; set; }
    public int err_mkey_valid { get; set; }
    public int err_ej_full { get; set; }
    public int err_fm_full { get; set; }
    public int err_hwinit_max { get; set; }
    public int err_cert_expired { get; set; }
    public int err_count { get; set; }

    public int warn_ej_full { get; set; }
    public int warn_fm_full { get; set; }
    public int warn_hwinit_max { get; set; }
    public int warn_cert_expired { get; set; }
    public int warn_count { get; set; }
    public int warn_hwinit_val { get; set; }
    public int warn_fm_full_val { get; set; }
    public int warn_ej_full_val { get; set; }

    public int err_fm_status { get; set; }
}