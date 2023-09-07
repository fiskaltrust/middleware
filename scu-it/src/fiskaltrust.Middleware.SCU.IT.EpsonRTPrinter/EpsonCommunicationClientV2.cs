using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.QueueLogic.Exceptions;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public class EpsonCommunicationClientV2
{
    private string? _serialnr;
    private readonly ILogger<EpsonRTPrinterSCU> _logger;
    private readonly EpsonCommandFactory _epsonXmlWriter;
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;
    private readonly ErrorInfoFactory _errorCodeFactory = new();

    public EpsonCommunicationClientV2(ILogger<EpsonRTPrinterSCU> logger, EpsonRTPrinterSCUConfiguration configuration, EpsonCommandFactory epsonXmlWriter)
    {
        _logger = logger;
        _epsonXmlWriter = epsonXmlWriter;
        if (string.IsNullOrEmpty(configuration.DeviceUrl))
        {
            throw new NullReferenceException("EpsonScuConfiguration DeviceUrl not set.");
        }
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration.DeviceUrl),
            Timeout = TimeSpan.FromMilliseconds(configuration.ClientTimeoutMs)
        };
        _commandUrl = $"cgi-bin/fpmate.cgi?timeout={configuration.ServerTimeoutMs}";
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCase = request.ReceiptRequest.GetReceiptCase();
        if (request.ReceiptRequest.IsInitialOperationReceipt())
        {
            return Helpers.CreateResponse(await PerformInitOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsOutOfOperationReceipt())
        {
            return Helpers.CreateResponse(await PerformOutOfOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsZeroReceipt())
        {
            return Helpers.CreateResponse(await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (Helpers.IsNoActionCase(request.ReceiptRequest))
        {
            return Helpers.CreateResponse(request.ReceiptResponse);
        }

        if (request.ReceiptRequest.IsVoid())
        {
            return await ProcessVoidReceipt(request);
        }

        if (request.ReceiptRequest.IsRefund())
        {
            return await ProcessRefundReceipt(request);
        }

        if (request.ReceiptRequest.IsDailyClosing())
        {
            return Helpers.CreateResponse(await PerformDailyCosing(request.ReceiptRequest, request.ReceiptResponse));
        }

        switch (receiptCase)
        {
            case (long) ITReceiptCases.UnknownReceipt0x0000:
            case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
            case (long) ITReceiptCases.PaymentTransfer0x0002:
            case (long) ITReceiptCases.Protocol0x0005:
            default:
                return Helpers.CreateResponse(await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
        }
    }

    private async Task SetReceiptResponse(List<Payment>? payments, PrinterResponse? result, FiscalReceiptResponse fiscalReceiptResponse)
    {
        if (result?.Success == false)
        {
            fiscalReceiptResponse.SSCDErrorInfo = GetErrorInfo(result.Code, result.Status, result?.Receipt?.PrinterStatus);
            await ResetPrinter();
        }
        else
        {
            await SetResponseAsync(payments, result, fiscalReceiptResponse);
        }
    }

    private async Task SetResponseAsync(List<Payment>? payments, PrinterResponse? result, FiscalReceiptResponse fiscalReceiptResponse)
    {
        decimal.TryParse(result?.Receipt?.FiscalReceiptAmount, NumberStyles.Any, new CultureInfo("it-It", false), out var amount);
        if (result?.Success == true && amount == 0)
        {
            amount = payments?.Sum(x => x.Amount) ?? 0;
        }
        fiscalReceiptResponse.Amount = amount;
        fiscalReceiptResponse.ReceiptNumber = result?.Receipt?.FiscalReceiptNumber != null ? long.Parse(result.Receipt.FiscalReceiptNumber) : 0;
        fiscalReceiptResponse.ZRepNumber = result?.Receipt?.ZRepNumber != null ? long.Parse(result.Receipt.ZRepNumber) : 0;
        fiscalReceiptResponse.ReceiptDataJson = await DownloadJsonAsync("www/json_files/rec.json");

        if (result?.Receipt?.FiscalReceiptDate != null && result?.Receipt?.FiscalReceiptTime != null)
        {
            fiscalReceiptResponse.ReceiptDateTime = DateTime.ParseExact(result.Receipt.FiscalReceiptDate, "d/M/yyyy", CultureInfo.InvariantCulture);
            var time = TimeSpan.Parse(result.Receipt.FiscalReceiptTime);
            fiscalReceiptResponse.ReceiptDateTime = fiscalReceiptResponse.ReceiptDateTime + time;
        }
        else
        {
            fiscalReceiptResponse.ReceiptDateTime = DateTime.Now;
        }
    }

    public async Task<ReceiptResponse> PerformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            var request = Helpers.CreateInvoice(receiptRequest);
            var content = _epsonXmlWriter.CreateInvoiceRequestContent(request);

            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<PrinterResponse>(responseContent);
            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };
            await SetReceiptResponse(request?.Payments, result, fiscalReceiptResponse);
            if (!fiscalReceiptResponse.Success)
            {
                throw new SSCDErrorException(fiscalReceiptResponse.SSCDErrorInfo.Type, fiscalReceiptResponse.SSCDErrorInfo.Info);
            }
            receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(fiscalReceiptResponse.ReceiptNumber, fiscalReceiptResponse.ZRepNumber, fiscalReceiptResponse.Amount, fiscalReceiptResponse.ReceiptDateTime);
            return receiptResponse;
        }
        catch (Exception e)
        {
            var response = Helpers.ExceptionInfo(e);
            throw new SSCDErrorException(response.SSCDErrorInfo.Type, response.SSCDErrorInfo.Info);
        }
    }

    private async Task<ProcessResponse> ProcessRefundReceipt(ProcessRequest request)
    {
        var referenceZNumber = long.Parse(request.ReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTReferenceZNumber)).Data);
        var referenceDocNumber = long.Parse(request.ReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTReferenceDocumentNumber)).Data);
        var referenceDateTime = DateTime.Parse(request.ReceiptResponse.ftSignatures.First(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTDocumentMoment)).Data);

        _ = await GetDeviceInfoAsync();
        var fiscalReceiptRefund = Helpers.CreateRefund(request.ReceiptRequest, referenceDocNumber, referenceZNumber, referenceDateTime, _serialnr!);
        var fiscalResponse = await FiscalReceiptRefundAsync(fiscalReceiptRefund).ConfigureAwait(false);
        if (!fiscalResponse.Success)
        {
            throw new SSCDErrorException(fiscalResponse.SSCDErrorInfo.Type, fiscalResponse.SSCDErrorInfo.Info);
        }
        else
        {
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(fiscalResponse.ReceiptNumber, fiscalResponse.ZRepNumber, fiscalResponse.Amount, fiscalResponse.ReceiptDateTime);
        }
        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        var content = _epsonXmlWriter.CreateQueryPrinterStatusRequestContent();
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));
        using var responseContent = await response.Content.ReadAsStreamAsync();
        var result = SoapSerializer.Deserialize<StatusResponse>(responseContent);

        _logger.LogInformation(JsonConvert.SerializeObject(result));
        if (string.IsNullOrEmpty(_serialnr) && result?.Printerstatus?.RtType != null)
        {
            _serialnr = await GetSerialNumberAsync(result.Printerstatus.RtType).ConfigureAwait(false);
        }

        return new DeviceInfo
        {
            DailyOpen = result?.Printerstatus?.DailyOpen == "1",
            DeviceStatus = Helpers.ParseStatus(result?.Printerstatus?.MfStatus), // TODO Create enum
            ExpireDeviceCertificateDate = result?.Printerstatus?.ExpiryCD, // TODO Use Datetime; this value seemingly can also be 20
            ExpireTACommunicationCertificateDate = result?.Printerstatus?.ExpiryCA, // TODO use DateTime?
            SerialNumber = _serialnr

        };
    }

    public async Task<string> GetSerialNumberAsync(string rtType)
    {
        var serialQuery = new PrinterCommand() { DirectIO = DirectIO.GetSerialNrCommand() };
        var content = SoapSerializer.Serialize(serialQuery);
        var responseSerialnr = await SendRequestAsync(content);

        using var responseContent = await responseSerialnr.Content.ReadAsStreamAsync();
        var result = SoapSerializer.Deserialize<PrinterCommandResponse>(responseContent);

        var serialnr = result?.CommandResponse?.ResponseData;

        return serialnr?.Substring(10, 2) + rtType + serialnr?.Substring(8, 2) + serialnr?.Substring(2, 6);
    }

    private Task<ProcessResponse> ProcessVoidReceipt(ProcessRequest request) => throw new NotImplementedException();

    public async Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request)
    {
        try
        {
            var content = _epsonXmlWriter.CreateRefundRequestContent(request);
            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<PrinterResponse>(responseContent);
            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };
            await SetReceiptResponse(request.Payments, result, fiscalReceiptResponse);
            return fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            return Helpers.ExceptionInfo(e);
        }
    }

    private async Task ResetPrinter()
    {
        var resetCommand = new PrinterCommand() { ResetPrinter = new ResetPrinter() { Operator = "" } };
        var xml = SoapSerializer.Serialize(resetCommand);
        await SendRequestAsync(xml);
    }

    public async Task<ReceiptResponse> PerformDailyCosing(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        DailyClosingResponse dailyClosingResponse;
        try
        {
            var content = _epsonXmlWriter.CreatePrintZReportRequestContent(new DailyClosingRequest
            {
                Operator = "1",
                DisplayText = receiptResponse.ftCashBoxIdentification + " " + receiptRequest.cbReceiptReference
            });
            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<ReportResponse>(responseContent);
            dailyClosingResponse = new DailyClosingResponse()
            {
                Success = result?.Success ?? false
            };

            if (!dailyClosingResponse.Success)
            {
                dailyClosingResponse.SSCDErrorInfo = GetErrorInfo(result?.Code, result?.Status, null);
                await ResetPrinter();
            }
            else
            {
                dailyClosingResponse.ZRepNumber = result?.ReportInfo?.ZRepNumber != null ? long.Parse(result.ReportInfo.ZRepNumber) : 0;
                dailyClosingResponse.DailyAmount = result?.ReportInfo?.DailyAmount != null ? decimal.Parse(result.ReportInfo.DailyAmount, new CultureInfo("it-It", false)) : 0;
                dailyClosingResponse.ReportDataJson = await DownloadJsonAsync("www/json_files/zrep.json");
            }
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }

            if (Helpers.IsConnectionException(e))
            {
                dailyClosingResponse = new DailyClosingResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.Connection } };
            }
            else
            {
                dailyClosingResponse = new DailyClosingResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.General } };
            }
            throw new SSCDErrorException(dailyClosingResponse.SSCDErrorInfo.Type, dailyClosingResponse.SSCDErrorInfo.Info);
        }
        receiptResponse.ftSignatures = SignatureFactory.CreateDailyClosingReceiptSignatures(dailyClosingResponse.ZRepNumber);
        return receiptResponse;
    }

    private async Task<ReceiptResponse> PerformInitOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) => await CreateMiddlewareNoFiscalRequestAsync(receiptResponse, receiptRequest).ConfigureAwait(false);

    private async Task<ReceiptResponse> PerformZeroReceiptOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) => await CreateMiddlewareNoFiscalRequestAsync(receiptResponse, receiptRequest).ConfigureAwait(false);

    private async Task<ReceiptResponse> PerformOutOfOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) => await CreateMiddlewareNoFiscalRequestAsync(receiptResponse, receiptRequest).ConfigureAwait(false);

    private async Task<ReceiptResponse> CreateMiddlewareNoFiscalRequestAsync(ReceiptResponse receiptResponse, ReceiptRequest request)
    {
        var nonFiscalRequest = new NonFiscalRequest
        {
            NonFiscalPrints = new List<NonFiscalPrint>()
        };

        try
        {
            var content = _epsonXmlWriter.CreateNonFiscalReceipt(new NonFiscalRequest
            {
                NonFiscalPrints = new List<NonFiscalPrint>
                {
                    new NonFiscalPrint
                    {
                        Data = $"{request.ftReceiptCase.ToString("x")} case for Queue {receiptResponse.ftCashBoxIdentification}"
                    },
                    new NonFiscalPrint
                    {
                        Data = $"Processing"
                    }
                }
            });
            var httpResponse = await SendRequestAsync(content);

            using var responseContent = await httpResponse.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<PrinterResponse>(responseContent);
            var response = new Response()
            {
                Success = result?.Success ?? false
            };

            if (!response.Success)
            {
                response.SSCDErrorInfo = GetErrorInfo(result?.Code, result?.Status, null);
            }
            if (response.Success)
            {
                receiptResponse.ftSignatures = SignatureFactory.CreateVoucherSignatures(nonFiscalRequest);
            }
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }
            Response? response;
            if (Helpers.IsConnectionException(e))
            {
                response = new Response() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.Connection } };
            }
            else
            {
                response = new Response() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.General } };
            }

            throw new SSCDErrorException(response.SSCDErrorInfo.Type, response.SSCDErrorInfo.Info);
        }
        return receiptResponse;
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

    private async Task<HttpResponseMessage> SendRequestAsync(string content)
    {
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(content, Encoding.UTF8, "application/xml"));
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occured while sending a request to the Epson device (StatusCode: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()})");
        }
        return response;
    }

    public SSCDErrorInfo GetErrorInfo(string? code, string? status, string? printerStatus)
    {
        var errorInf = string.Empty;
        if (code != null)
        {
            errorInf += $"\n Error Code {code}: {_errorCodeFactory.GetCodeInfo(code)} ";
        }
        if (status != null)
        {
            errorInf += $"\n Status {status}: {_errorCodeFactory.GetStatusInfo(int.Parse(status))}";
        }
        var state = Helpers.GetPrinterStatus(printerStatus);
        if (state != null)
        {
            errorInf += $"\n Printer state {state}";
        }
        _logger.LogError(errorInf);
        return new SSCDErrorInfo() { Info = errorInf, Type = SSCDErrorType.Device };
    }
}
