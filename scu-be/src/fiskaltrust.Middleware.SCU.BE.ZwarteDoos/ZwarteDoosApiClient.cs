using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.GraphQL;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Device;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Financial;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Invoice;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Social;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosApiClient
{
    private readonly ZwarteDoosApiClientConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ZwarteDoosApiClient(ZwarteDoosApiClientConfiguration configuration, HttpClient httpClient, ILogger<ZwarteDoosApiClient> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);
    }

    public async Task<GraphQLResponse<DeviceResponse>> GetDeviceIdAsync(CancellationToken cancellationToken = default) => await ExecuteGraphQLQueryRequestAsync<DeviceResponse>(GraphQLRequestFactory.CreateQueryDeviceRequest(), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> OrderAsync(OrderInput orderData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignOrderRequest(orderData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> ReportTurnoverXAsync(ReportTurnoverXInput reportData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignReportTurnoverXRequest(reportData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> ReportTurnoverZAsync(ReportTurnoverZInput reportData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignReportTurnoverZRequest(reportData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> ReportUserXAsync(ReportUserXInput reportData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignReportUserXRequest(reportData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> ReportUserZAsync(ReportUserZInput reportData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignReportUserZRequest(reportData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> WorkInAsync(WorkInOutInput workData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignWorkInRequest(workData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> WorkOutAsync(WorkInOutInput workData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignWorkOutRequest(workData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> InvoiceAsync(InvoiceInput invoiceData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignInvoiceRequest(invoiceData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> CostCenterChangeAsync(CostCenterChangeInput changeData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignCostCenterChangeRequest(changeData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> PreBillAsync(PreBillInput billData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignPreBillRequest(billData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> SaleAsync(SaleInput saleData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignSaleRequest(saleData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> PaymentCorrectionAsync(PaymentCorrectionInput correctionData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignPaymentCorrectionRequest(correctionData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> MoneyInOutAsync(MoneyInOutInput moneyData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignMoneyInOutRequest(moneyData, isTraining), cancellationToken);

    public async Task<GraphQLResponse<SignResult>> DrawerOpenAsync(DrawerOpenInput drawerData, bool isTraining = false, CancellationToken cancellationToken = default) => await ExecuteGraphQLMutationRequestAsync(GraphQLRequestFactory.CreateSignDrawerOpenRequest(drawerData, isTraining), cancellationToken);

    private async Task<GraphQLResponse<SignResult>> ExecuteGraphQLMutationRequestAsync<T>(GraphQLMutationRequest<T> graphQLMutationRequest, CancellationToken cancellationToken)
    {
        var requestBody = JsonSerializer.Serialize(graphQLMutationRequest);
        return await CallGraphQL<SignResult>(requestBody, cancellationToken);
    }

    private async Task<GraphQLResponse<T>> ExecuteGraphQLQueryRequestAsync<T>(GraphQLQueryRequest graphQLQueryRequest, CancellationToken cancellationToken)
    {
        var requestBody = JsonSerializer.Serialize(graphQLQueryRequest);
        return await CallGraphQL<T>(requestBody, cancellationToken);
    }

    private async Task<GraphQLResponse<T>> CallGraphQL<T>(string requestBody, CancellationToken cancellationToken)
    {
        var url = $"{_configuration.BaseUrl}/{_configuration.DeviceId}/graphql";
        var dateTime = DateTime.Now;
        var gmt = dateTime.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT";
        var stringToHash = "POST" + gmt + _configuration.SharedSecret + requestBody;
        var authorizationHeader = GenerateAuthorizationHeader(stringToHash);
        using var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = httpContent
        };

        request.Headers.Add("Date", gmt);
        request.Headers.Add("Authorization", $"FDM {authorizationHeader}");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"ZwarteDoos API request failed with status {response.StatusCode}: {responseBody}");
        }
        var graphQLResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        if (graphQLResponse == null || graphQLResponse.Data == null)
        {
            throw new InvalidOperationException("GraphQL response contains no data");
        }
        return graphQLResponse;
    }

    private static string GenerateAuthorizationHeader(string stringToHash) => Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(stringToHash)));

    public void Dispose() => _httpClient?.Dispose();
}
