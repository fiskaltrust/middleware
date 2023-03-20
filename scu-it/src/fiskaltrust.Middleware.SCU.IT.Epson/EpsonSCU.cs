using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;
using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.Epson;

#nullable enable
public sealed class EpsonSCU : IITSSCD 
{
    private readonly ILogger<EpsonSCU> _logger;
    private readonly EpsonCommandFactory _epsonXmlWriter;
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;

    public EpsonSCU(ILogger<EpsonSCU> logger, EpsonScuConfiguration configuration, EpsonCommandFactory epsonXmlWriter)
    {
        _logger = logger;
        _epsonXmlWriter = epsonXmlWriter;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration.DeviceUrl),
            Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs)
        };
        _commandUrl = $"/cgi-bin/fpmate.cgi?timeout={configuration.ServerTimeoutMs}"; ;
    }

    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public async Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request)
    {
        try
        {
            var content = _epsonXmlWriter.CreateInvoiceRequestContent(request);
            var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));
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
            var content = _epsonXmlWriter.CreateRefundRequestContent(request);
            var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));

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
    public async Task<PrinterStatus> GetPrinterInfoAsync()
    {
        try
        {
            var content = _epsonXmlWriter.CreateQueryPrinterStatusRequestContent();
            var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<PrinterResponse>(responseContent);

            return new PrinterStatus
            {
                DailyOpen = result?.AdditionalInfo?.DailyOpen == "1",
                DeviceStatus = ParseStatus(result?.AdditionalInfo?.MfStatus), // TODO Create enum
                ExpireDeviceCertificateDate = result?.AdditionalInfo?.ExpiryCD, // TODO Use Datetime; this value seemingly can also be 20
                ExpireTACommunicationCertificateDate = result?.AdditionalInfo?.ExpiryCA // TODO use DateTime?
            };
        }
        catch 
        {
            //var msg = e.Message;
            //if (e.InnerException != null)
            //{
            //    msg = msg + " " + e.InnerException.Message;
            //}
            return new PrinterStatus();
        }
    }

    private string ParseStatus(string? mfStatus)
    {
        return mfStatus switch
        {
            "01" => "Not in service",
            "02" => "In service",
            _ => "Undefined"
        };
    }

    public Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => throw new NotImplementedException();

    public Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request) => throw new NotImplementedException();
}
