using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

#pragma warning disable

#nullable enable
public sealed class CustomRTServer : IITSSCD
{
    private readonly ILogger<CustomRTServer> _logger;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

    public CustomRTServer(ILogger<CustomRTServer> logger, CustomRTServerConfiguration configuration)
    {
        _logger = logger;
        if (string.IsNullOrEmpty(configuration.ServerUrl))
        {
            throw new NullReferenceException("ServerUrl is not set.");
        }
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration.ServerUrl),
            Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs)
        };
    }

    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => throw new NotImplementedException();
    public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotImplementedException();
    public Task<DeviceInfo> GetDeviceInfoAsync() => throw new NotImplementedException();
    public Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => throw new NotImplementedException();
}
