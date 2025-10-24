using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosApiClient
{
    private readonly ZwarteDoosApiClientConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZwarteDoosApiClient> _logger;

    public ZwarteDoosApiClient(ZwarteDoosApiClientConfiguration configuration, HttpClient httpClient, ILogger<ZwarteDoosApiClient> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);
    }

    public async Task<DeviceInfo> GetDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        const string query = "{\"query\":\"{device{id}}\"}";
        
        var response = await ExecuteGraphQLRequestAsync<DeviceResponse>(query, cancellationToken);
        return response.Device;
    }

    public async Task<SignOrderData> OrderAsync(object orderData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignOrder($data:OrderInput! $isTraining:Boolean!) {signOrder(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignOrder",
            variables = new
            {
                data = orderData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignOrderResponse>(requestBody, cancellationToken);
        return response.SignOrder;
    }

    public async Task<SignOrderData> ReportTurnoverXAsync(object reportData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignReportTurnoverX($data:ReportTurnoverXInput! $isTraining:Boolean!) {signReportTurnoverX(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignReportTurnoverX",
            variables = new
            {
                data = reportData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignReportTurnoverXResponse>(requestBody, cancellationToken);
        return response.SignReportTurnoverX;
    }

    public async Task<SignOrderData> ReportTurnoverZAsync(object reportData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignReportTurnoverZ($data:ReportTurnoverZInput! $isTraining:Boolean!) {signReportTurnoverZ(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignReportTurnoverZ",
            variables = new
            {
                data = reportData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignReportTurnoverZResponse>(requestBody, cancellationToken);
        return response.SignReportTurnoverZ;
    }

    public async Task<SignOrderData> ReportUserXAsync(object reportData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignReportUserX($data:ReportUserXInput! $isTraining:Boolean!) {signReportUserX(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignReportUserX",
            variables = new
            {
                data = reportData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignReportUserXResponse>(requestBody, cancellationToken);
        return response.SignReportUserX;
    }

    public async Task<SignOrderData> ReportUserZAsync(object reportData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignReportUserZ($data:ReportUserZInput! $isTraining:Boolean!) {signReportUserZ(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignReportUserZ",
            variables = new
            {
                data = reportData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignReportUserZResponse>(requestBody, cancellationToken);
        return response.SignReportUserZ;
    }

    public async Task<SignOrderData> WorkInAsync(object workData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignWorkIn($data:WorkInOutInput! $isTraining:Boolean!) {signWorkIn(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignWorkIn",
            variables = new
            {
                data = workData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignWorkInResponse>(requestBody, cancellationToken);
        return response.SignWorkIn;
    }

    public async Task<SignOrderData> WorkOutAsync(object workData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignWorkOut($data:WorkInOutInput! $isTraining:Boolean!) {signWorkOut(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignWorkOut",
            variables = new
            {
                data = workData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignWorkOutResponse>(requestBody, cancellationToken);
        return response.SignWorkOut;
    }

    public async Task<SignOrderData> InvoiceAsync(object invoiceData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignInvoice($data:InvoiceInput! $isTraining:Boolean!) {signInvoice(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignInvoice",
            variables = new
            {
                data = invoiceData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignInvoiceResponse>(requestBody, cancellationToken);
        return response.SignInvoice;
    }

    public async Task<SignOrderData> CostCenterChangeAsync(object changeData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignCostCenterChange($data:CostCenterChangeInput! $isTraining:Boolean!) {signCostCenterChange(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignCostCenterChange",
            variables = new
            {
                data = changeData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignCostCenterChangeResponse>(requestBody, cancellationToken);
        return response.SignCostCenterChange;
    }

    public async Task<SignOrderData> PreBillAsync(object billData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignPreBill($data:PreBillInput! $isTraining:Boolean!) {signPreBill(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignPreBill",
            variables = new
            {
                data = billData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignPreBillResponse>(requestBody, cancellationToken);
        return response.SignPreBill;
    }

    public async Task<SignSaleData> SaleAsync(object saleData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignSale($data:SaleInput! $isTraining:Boolean!) {signSale(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature shortSignature verificationUrl vatCalc {label rate taxableAmount vatAmount totalAmount outOfScope} bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignSale",
            variables = new
            {
                data = saleData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignSaleResponse>(requestBody, cancellationToken);
        return response.SignSale;
    }

    public async Task<SignOrderData> PaymentCorrectionAsync(object correctionData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignPaymentCorrection($data:PaymentCorrectionInput! $isTraining:Boolean!) {signPaymentCorrection(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignPaymentCorrection",
            variables = new
            {
                data = correctionData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignPaymentCorrectionResponse>(requestBody, cancellationToken);
        return response.SignPaymentCorrection;
    }

    public async Task<SignOrderData> MoneyInOutAsync(object moneyData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignMoneyInOut($data:MoneyInOutInput! $isTraining:Boolean!) {signMoneyInOut(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignMoneyInOut",
            variables = new
            {
                data = moneyData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignMoneyInOutResponse>(requestBody, cancellationToken);
        return response.SignMoneyInOut;
    }

    public async Task<SignOrderData> DrawerOpenAsync(object drawerData, bool isTraining = false, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query = "mutation SignDrawerOpen($data:DrawerOpenInput! $isTraining:Boolean!) {signDrawerOpen(data: $data isTraining: $isTraining) {posId posFiscalTicketNo posDateTime terminalId deviceId eventOperation fdmRef {fdmId fdmDateTime eventLabel eventCounter totalCounter} fdmSwVersion digitalSignature bufferCapacityUsed warnings {message locations {line column} extensions {category code data {name value} showPos}} informations {message locations {line column} extensions {category code data {name value} showPos}} footer }}",
            operationName = "SignDrawerOpen",
            variables = new
            {
                data = drawerData,
                isTraining = isTraining
            }
        };

        var requestBody = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await ExecuteGraphQLRequestAsync<SignDrawerOpenResponse>(requestBody, cancellationToken);
        return response.SignDrawerOpen;
    }

    private async Task<T> ExecuteGraphQLRequestAsync<T>(string requestBody, CancellationToken cancellationToken)
    {
        var url = $"{_configuration.BaseUrl}/{_configuration.DeviceId}/graphql";
        
        _logger.LogDebug("Making GraphQL request to {Url}", url);

        // Generate FDM authentication
        var dateTime = DateTime.Now;
        var gmt = dateTime.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT";
        
        var stringToHash = "POST" + gmt + _configuration.SharedSecret + requestBody;
        var authorizationHeader = GenerateAuthorizationHeader(stringToHash);

        _logger.LogDebug("Generated authorization header for request");

        // Create HTTP request
        using var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = httpContent
        };

        request.Headers.Add("Date", gmt);
        request.Headers.Add("Authorization", $"FDM {authorizationHeader}");

        // Execute request
        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("Received response with status {StatusCode}: {ResponseBody}", 
            response.StatusCode, responseBody);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"ZwarteDoos API request failed with status {response.StatusCode}: {responseBody}");
        }

        // Parse GraphQL response
        var graphQLResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (graphQLResponse?.Errors != null && graphQLResponse.Errors.Length > 0)
        {
            var errorMessage = string.Join("; ", Array.ConvertAll(graphQLResponse.Errors, e => e.Message));
            throw new InvalidOperationException($"GraphQL errors: {errorMessage}");
        }

        if (graphQLResponse == null || graphQLResponse.Data == null)
        {
            throw new InvalidOperationException("GraphQL response contains no data");
        }

        return graphQLResponse.Data;
    }

    private static string GenerateAuthorizationHeader(string stringToHash)
    {
        byte[] hash;
        using (var sha = SHA1.Create())
        {
            hash = sha.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
        }
        
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
