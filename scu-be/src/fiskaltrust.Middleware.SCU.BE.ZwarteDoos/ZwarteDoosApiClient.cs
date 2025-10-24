using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosApiClientConfiguration
{
    public string DeviceId { get; set; } = null!;
    public string SharedSecret { get; set; } = null!;
    public string BaseUrl { get; set; } = "https://sdk.zwartedoos.be";
    public int TimeoutSeconds { get; set; } = 30;
}

public class DeviceInfo
{
    public string Id { get; set; } = null!;
}

public class DeviceResponse
{
    public DeviceInfo Device { get; set; } = null!;
}

public class SignOrderResponse
{
    public SignOrderData SignOrder { get; set; } = null!;
}

public class SignReportTurnoverXResponse
{
    public SignOrderData SignReportTurnoverX { get; set; } = null!;
}

public class SignReportTurnoverZResponse
{
    public SignOrderData SignReportTurnoverZ { get; set; } = null!;
}

public class SignReportUserXResponse
{
    public SignOrderData SignReportUserX { get; set; } = null!;
}

public class SignReportUserZResponse
{
    public SignOrderData SignReportUserZ { get; set; } = null!;
}

public class SignOrderData
{
    public string PosId { get; set; } = null!;
    public int PosFiscalTicketNo { get; set; }
    public string PosDateTime { get; set; } = null!;
    public string TerminalId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string EventOperation { get; set; } = null!;
    public FdmRef FdmRef { get; set; } = null!;
    public string FdmSwVersion { get; set; } = null!;
    public string DigitalSignature { get; set; } = null!;
    public decimal BufferCapacityUsed { get; set; }
    public List<ApiMessage> Warnings { get; set; } = [];
    public List<ApiMessage> Informations { get; set; } = [];
    public List<string> Footer { get; set; } = null!;
}

public class FdmRef
{
    public string FdmId { get; set; } = null!;
    public string FdmDateTime { get; set; } = null!;
    public string EventLabel { get; set; } = null!;
    public int EventCounter { get; set; }
    public int TotalCounter { get; set; }
}

public class ApiMessage
{
    public string Message { get; set; } = null!;
    public Location[] Locations { get; set; } = Array.Empty<Location>();
    public Extensions Extensions { get; set; } = null!;
}

public class Location
{
    public int Line { get; set; }
    public int Column { get; set; }
}

public class Extensions
{
    public string Category { get; set; } = null!;
    public string Code { get; set; } = null!;
    public ExtensionData[] Data { get; set; } = Array.Empty<ExtensionData>();
    public bool ShowPos { get; set; }
}

public class ExtensionData
{
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;
}

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

internal class GraphQLResponse<T>
{
    public T? Data { get; set; }
    public GraphQLError[]? Errors { get; set; }
}

internal class GraphQLError
{
    public string Message { get; set; } = null!;
    public Location[]? Locations { get; set; }
    public string[]? Path { get; set; }
}