using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;
using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.Epson;

#nullable enable
public sealed class EpsonSCU : IITSSCD
{
    private readonly ILogger<EpsonSCU> _logger;
    private readonly EpsonScuConfiguration _configuration;
    private readonly EpsonCommandFactory _epsonXmlWriter;
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;
    private readonly ErrorCodeFactory _errorCodeFactory = new();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

    public EpsonSCU(ILogger<EpsonSCU> logger, EpsonScuConfiguration configuration, EpsonCommandFactory epsonXmlWriter)
    {
        _logger = logger;
        _configuration = configuration;
        _epsonXmlWriter = epsonXmlWriter;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration.DeviceUrl),
            Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs)
        };
        _commandUrl = $"/cgi-bin/fpmate.cgi?timeout={configuration.ServerTimeoutMs}";
    }

    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public async Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request)
    {
        try
        {
            _semaphore.Wait(_configuration.LockTimeoutMs);

            var content = _epsonXmlWriter.CreateInvoiceRequestContent(request);
            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<ReceiptResponse>(responseContent);
            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };

            var status = GetPrinterStatus(result?.Receipt?.PrinterStatus);
            if (status != null)
            {
                _logger.LogDebug("Printer status: {PrinterStatus}", status);
                fiscalReceiptResponse.ErrorInfo = $"Status: {status}";
            }

            if (result?.Success == false)
            {
                if (result?.Code != null)
                {
                    var error = _errorCodeFactory.GetErrorInfo(result.Code);
                    _logger.LogError("Printer status: {Error}", error);
                    fiscalReceiptResponse.ErrorInfo += $", Error: {error}";
                }
                await ResetPrinter();
            }
            else
            {
                await SetResponseAsync(request?.Payments, result, fiscalReceiptResponse);
            }
            return fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }
            return new FiscalReceiptResponse() { Success = false, ErrorInfo = msg };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request)
    {
        try
        {
            _semaphore.Wait(_configuration.LockTimeoutMs);

            var content = _epsonXmlWriter.CreateRefundRequestContent(request);
            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<ReceiptResponse>(responseContent);
            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };

            var status = GetPrinterStatus(result?.Receipt?.PrinterStatus);
            if (status != null)
            {
                _logger.LogDebug("Printer status: {PrinterStatus}", status);
                fiscalReceiptResponse.ErrorInfo = $"Status: {status}";
            }

            if (result?.Success == false)
            {
                if (result?.Code != null)
                {
                    var error = _errorCodeFactory.GetErrorInfo(result.Code);
                    _logger.LogError("Printer status: {Error}", error);
                    fiscalReceiptResponse.ErrorInfo += $", Error: {error}";
                }
                await ResetPrinter();
            }
            else
            {
                await SetResponseAsync(request?.Payments, result, fiscalReceiptResponse);
            }
            return fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }
            return new FiscalReceiptResponse() { Success = false, ErrorInfo = msg };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request)
    {
        try
        {
            _semaphore.Wait(_configuration.LockTimeoutMs);

            var content = _epsonXmlWriter.CreatePrintZReportRequestContent(request);
            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<ReportResponse>(responseContent);
            var dailyClosingResponse = new DailyClosingResponse()
            {
                Success = result?.Success ?? false
            };

            var status = GetPrinterStatus(result?.ReportInfo?.PrinterStatus);
            if (status != null)
            {
                _logger.LogDebug("Printer status: {PrinterStatus}", status);
                dailyClosingResponse.ErrorInfo = $"Status: {status}";
            }

            if (result?.Success == false)
            {
                if (result?.Code != null)
                {
                    var error = _errorCodeFactory.GetErrorInfo(result.Code);
                    _logger.LogError("Printer status: {Error}", error);
                    dailyClosingResponse.ErrorInfo += $", Error: {error}";
                }
                await ResetPrinter();
            }
            else
            {
                dailyClosingResponse.ZRepNumber = result?.ReportInfo?.ZRepNumber != null ? ulong.Parse(result.ReportInfo.ZRepNumber) : 0;
                dailyClosingResponse.DailyAmount = result?.ReportInfo?.DailyAmount != null ? decimal.Parse(result.ReportInfo.DailyAmount, new CultureInfo("it-It", false)) : 0;
                dailyClosingResponse.RecordDataJson = await DownloadJsonAsync("www/json_files/zrep.json");
            }
            return dailyClosingResponse;
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }
            return new DailyClosingResponse() { Success = false, ErrorInfo = msg };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        var content = _epsonXmlWriter.CreateQueryPrinterStatusRequestContent();
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));
        using var responseContent = await response.Content.ReadAsStreamAsync();
        var result = SoapSerializer.Deserialize<StatusResponse>(responseContent);

        _logger.LogInformation(JsonConvert.SerializeObject(result));

        return new DeviceInfo
        {
            DailyOpen = result?.Printerstatus?.DailyOpen == "1",
            DeviceStatus = ParseStatus(result?.Printerstatus?.MfStatus), // TODO Create enum
            ExpireDeviceCertificateDate = result?.Printerstatus?.ExpiryCD, // TODO Use Datetime; this value seemingly can also be 20
            ExpireTACommunicationCertificateDate = result?.Printerstatus?.ExpiryCA // TODO use DateTime?
        };
    }

    private async Task SetResponseAsync(List<Payment>? payments, ReceiptResponse? result, FiscalReceiptResponse fiscalReceiptResponse)
    {
        decimal.TryParse(result?.Receipt?.FiscalReceiptAmount, NumberStyles.Any, new CultureInfo("it-It", false), out var amount);
        if (result?.Success == true && amount == 0)
        {
            amount = payments?.Sum(x => x.Amount) ?? 0;
        }
        fiscalReceiptResponse.Amount = amount;
        fiscalReceiptResponse.RecNumber = result?.Receipt?.FiscalReceiptNumber != null ? ulong.Parse(result.Receipt.FiscalReceiptNumber) : 0;
        fiscalReceiptResponse.ZRecNumber = result?.Receipt?.ZRepNumber != null ? ulong.Parse(result.Receipt.ZRepNumber) : 0;
        fiscalReceiptResponse.RecordDataJson = await DownloadJsonAsync("www/json_files/rec.json");

        if (result?.Receipt?.FiscalReceiptDate != null && result?.Receipt?.FiscalReceiptTime != null)
        {
            fiscalReceiptResponse.TimeStamp = DateTime.ParseExact(result.Receipt.FiscalReceiptDate, "d/M/yyyy", CultureInfo.InvariantCulture);
            var time = TimeSpan.Parse(result.Receipt.FiscalReceiptTime);
            fiscalReceiptResponse.TimeStamp = fiscalReceiptResponse.TimeStamp + time;
        }
        else
        {
            fiscalReceiptResponse.TimeStamp = DateTime.Now;
        }
    }

    private string? GetPrinterStatus(string? printerStatus)
    {
        var pst = printerStatus?.ToCharArray();
        if (pst != null)
        {
            var printerstatus = new DeviceStatus(Array.ConvertAll(pst, c => (int) char.GetNumericValue(c)));
            return JsonConvert.SerializeObject(printerstatus);
        }

        return null;
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

    private async Task ResetPrinter()
    {
        var resetCommand = new ResetPrinterCommand() { ResetPrinter = new ResetPrinter() { Operator = "" } };
        var xml = SoapSerializer.Serialize(resetCommand);
        await SendRequestAsync(xml);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string content)
    {
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occured while sending a request to the Epson device (StatusCode: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()})");
        }

        return response;
    }

    private async Task<string?> DownloadJsonAsync(string path)
    {
        var response = await _httpClient.GetAsync(path); 
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Could not download JSON file from device (URL: {Url}, Path: {Path}, Response content: {Content}", _httpClient.BaseAddress?.ToString(), path, content);
            return null; // TODO: Or better throw?
        }

        return content;
    }
}
