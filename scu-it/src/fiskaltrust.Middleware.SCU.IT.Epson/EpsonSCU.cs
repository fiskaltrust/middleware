using System;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Configuration;
using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.Epson;

#nullable enable
public sealed class EpsonSCU : IITSSCD 
{
    private readonly EpsonScuConfiguration _configuration;
    private readonly ILogger<EpsonSCU> _logger;
    private readonly HttpClient _httpClient;
    private readonly EpsonXmlWriter _epsonXmlWriter;


    public EpsonSCU(ILogger<EpsonSCU> logger, EpsonScuConfiguration configuration, EpsonXmlWriter epsonXmlWriter)
    {
        _logger = logger;
        _configuration = configuration;
        _epsonXmlWriter = epsonXmlWriter;
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(configuration.PrinterUrl),
            Timeout = TimeSpan.FromMilliseconds(configuration.PrinterClientTimeoutMs)
        };
    }

    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });
    public Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request) => throw new NotImplementedException();
    public async Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request)
    {
        try
        {
            var response = await _httpClient.PutAsync("", new StreamContent(_epsonXmlWriter.FiscalReceiptToXml(request)));
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseContent);
            //Todo parse
            return new FiscalReceiptResponse();
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " +e.InnerException.Message;
            }
            return new FiscalReceiptResponse() { Success = false, ErrorInfo = msg};
        }
    }
    public async Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request)
    {
        try
        {
            var response = await _httpClient.PutAsync("", new StreamContent(_epsonXmlWriter.FiscalReceiptToXml(request)));

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseContent);
            return new FiscalReceiptResponse();
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }
            return new FiscalReceiptResponse() { Success = true, ErrorInfo = msg };
        }

    }
    public Task<PrinterStatus> GetPrinterInfoAsync() => throw new NotImplementedException();
    public Task<PrinterStatus> GetPrinterStatusAsync() => throw new NotImplementedException();
    public Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => throw new NotImplementedException();
}
