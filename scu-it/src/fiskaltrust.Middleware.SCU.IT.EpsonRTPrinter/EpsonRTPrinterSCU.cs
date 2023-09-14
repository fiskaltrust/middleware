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
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public sealed class EpsonRTPrinterSCU : LegacySCU
{
    private readonly ILogger<EpsonRTPrinterSCU> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;
    private readonly ErrorInfoFactory _errorCodeFactory = new();
    private string _serialnr = "";

    public EpsonRTPrinterSCU(ILogger<EpsonRTPrinterSCU> logger, EpsonRTPrinterSCUConfiguration configuration)
    {
        _logger = logger;
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

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public override async Task<RTInfo> GetRTInfoAsync()
    {
        var result = await QueryPrinterStatusAsync();
        _logger.LogInformation(JsonConvert.SerializeObject(result));
        if (string.IsNullOrEmpty(_serialnr) && result?.Printerstatus?.RtType != null)
        {
            _serialnr = await GetSerialNumberAsync(result.Printerstatus.RtType).ConfigureAwait(false);
        }
        return new RTInfo
        {
            SerialNumber = _serialnr,
            InfoData = JsonConvert.SerializeObject(new DeviceInfo
            {
                DailyOpen = result?.Printerstatus?.DailyOpen == "1",
                DeviceStatus = Helpers.ParseStatus(result?.Printerstatus?.MfStatus),
                ExpireDeviceCertificateDate = result?.Printerstatus?.ExpiryCD,
                ExpireTACommunicationCertificateDate = result?.Printerstatus?.ExpiryCA,
                SerialNumber = _serialnr
            })
        };
    }

    private async Task<StatusResponse?> QueryPrinterStatusAsync()
    {
        var queryPrinterStatus = new QueryPrinterStatusCommand { QueryPrinterStatus = new QueryPrinterStatus { StatusType = 1 } };
        var response = await _httpClient.PostAsync(_commandUrl, new StringContent(SoapSerializer.Serialize(queryPrinterStatus), Encoding.UTF8, "application/xml"));
        using var responseContent = await response.Content.ReadAsStreamAsync();
        return SoapSerializer.DeserializeToSoapEnvelope<StatusResponse>(responseContent);
    }

    public override async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCase = request.ReceiptRequest.GetReceiptCase();
        if (string.IsNullOrEmpty(_serialnr))
        {
            var result = await QueryPrinterStatusAsync();
            _logger.LogInformation(JsonConvert.SerializeObject(result));
            _serialnr = await GetSerialNumberAsync(result?.Printerstatus?.RtType ?? "").ConfigureAwait(false);
        }
        if (request.ReceiptRequest.IsInitialOperationReceipt())
        {
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateInitialOperationSignatures().ToList());
        }

        if (request.ReceiptRequest.IsOutOfOperationReceipt())
        {
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateOutOfOperationSignatures().ToList());
        }

        if (request.ReceiptRequest.IsZeroReceipt())
        {
            (var signatures, var stateData) = await PerformZeroReceiptOperationAsync();
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, stateData, signatures);
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

        throw new Exception($"The given receiptcase 0x{receiptCase.ToString("X")} is not supported.");
    }

    private async Task SetReceiptResponse(PrinterResponse? result, FiscalReceiptResponse fiscalReceiptResponse)
    {
        if (result?.Success == false)
        {
            fiscalReceiptResponse.SSCDErrorInfo = GetErrorInfo(result.Code, result.Status, result?.Receipt?.PrinterStatus);
            await ResetPrinter();
        }
        else
        {
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
    }

    public async Task<ReceiptResponse> PerformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            var content = EpsonCommandFactory.CreateInvoiceRequestContent(receiptRequest);
            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await SendRequestAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }

            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };
            await SetReceiptResponse(result, fiscalReceiptResponse);
            if (!fiscalReceiptResponse.Success)
            {
                throw new SSCDErrorException(fiscalReceiptResponse.SSCDErrorInfo.Type, fiscalReceiptResponse.SSCDErrorInfo.Info);
            }
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data           
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
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
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            throw new Exception("Cannot refund receipt without references.");
        }

        FiscalReceiptResponse fiscalResponse;
        try
        {

            if (string.IsNullOrEmpty(_serialnr))
            {
                var rtinfo = await GetRTInfoAsync();
                _serialnr = rtinfo.SerialNumber;
            }
            var content = EpsonCommandFactory.CreateRefundRequestContent(request.ReceiptRequest, long.Parse(referenceDocNumber), long.Parse(referenceZNumber), DateTime.Parse(referenceDateTime), _serialnr!);
            var response = await SendRequestAsync(SoapSerializer.Serialize(content));

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(responseContent);
            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };
            await SetReceiptResponse(result, fiscalReceiptResponse);
            fiscalResponse = fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            fiscalResponse = Helpers.ExceptionInfo(e);
        }

        if (!fiscalResponse.Success)
        {
            throw new SSCDErrorException(fiscalResponse.SSCDErrorInfo.Type, fiscalResponse.SSCDErrorInfo.Info);
        }
        else
        {
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = fiscalResponse.ZRepNumber,
                RTDocNumber = fiscalResponse.ReceiptNumber,
                RTDocMoment = fiscalResponse.ReceiptDateTime,
                RTDocType = "REFUND",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data
                RTReferenceZNumber = long.Parse(referenceZNumber),
                RTReferenceDocNumber = long.Parse(referenceDocNumber),
                RTReferenceDocMoment = DateTime.Parse(referenceDateTime)
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
        }
        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    private async Task<ProcessResponse> ProcessVoidReceipt(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            throw new Exception("Cannot refund receipt without references.");
        }
        FiscalReceiptResponse fiscalResponse;
        try
        {

            if (string.IsNullOrEmpty(_serialnr))
            {
                var rtinfo = await GetRTInfoAsync();
                _serialnr = rtinfo.SerialNumber;
            }
            var content = EpsonCommandFactory.CreateVoidRequestContent(request.ReceiptRequest, long.Parse(referenceDocNumber), long.Parse(referenceZNumber), DateTime.Parse(referenceDateTime), _serialnr!);
            var response = await SendRequestAsync(SoapSerializer.Serialize(content));

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(responseContent);
            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };
            await SetReceiptResponse(result, fiscalReceiptResponse);
            fiscalResponse = fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            fiscalResponse = Helpers.ExceptionInfo(e);
        }

        if (!fiscalResponse.Success)
        {
            throw new SSCDErrorException(fiscalResponse.SSCDErrorInfo.Type, fiscalResponse.SSCDErrorInfo.Info);
        }
        else
        {
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = _serialnr,
                RTZNumber = fiscalResponse.ZRepNumber,
                RTDocNumber = fiscalResponse.ReceiptNumber,
                RTDocMoment = fiscalResponse.ReceiptDateTime,
                RTDocType = "VOID",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data
                RTReferenceZNumber = long.Parse(referenceZNumber),
                RTReferenceDocNumber = long.Parse(referenceDocNumber),
                RTReferenceDocMoment = DateTime.Parse(referenceDateTime)
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
        }
        return new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        };
    }

    public async Task<string> GetSerialNumberAsync(string rtType)
    {
        var serialQuery = new PrinterCommand() { DirectIO = DirectIO.GetSerialNrCommand() };
        var content = SoapSerializer.Serialize(serialQuery);
        var responseSerialnr = await SendRequestAsync(content);

        using var responseContent = await responseSerialnr.Content.ReadAsStreamAsync();
        var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterCommandResponse>(responseContent);

        var serialnr = result?.CommandResponse?.ResponseData;

        return serialnr?.Substring(10, 2) + rtType + serialnr?.Substring(8, 2) + serialnr?.Substring(2, 6);
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
            var fiscalReport = new FiscalReport
            {
                ZReport = new ZReport(),
                DisplayText = new DisplayText
                {
                    Data = receiptResponse.ftCashBoxIdentification + " " + receiptRequest.cbReceiptReference
                }
            };
            var response = await SendRequestAsync(SoapSerializer.Serialize(fiscalReport));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<ReportResponse>(responseContent);
            dailyClosingResponse = new DailyClosingResponse()
            {
                Success = result?.Success ?? false
            };

            if (!dailyClosingResponse.Success)
            {
                dailyClosingResponse.SSCDErrorInfo = GetErrorInfo(result?.Code, result?.Status, null);
                await ResetPrinter();
                if (!dailyClosingResponse.Success)
                {
                    throw new SSCDErrorException(dailyClosingResponse.SSCDErrorInfo.Type, dailyClosingResponse.SSCDErrorInfo.Info);
                }
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

    private async Task<(List<SignaturItem> signaturItems, string ftStateData)> PerformZeroReceiptOperationAsync()
    {
        await ResetPrinter();

        var result = await QueryPrinterStatusAsync();
        var signatures = SignatureFactory.CreateZeroReceiptSignatures().ToList();
        var stateData = JsonConvert.SerializeObject(new
        {
            PrinterStatus = result
        });
        return (signatures, stateData);
    }

    private async Task<ReceiptResponse> CreateMiddlewareNoFiscalRequestAsync(ReceiptResponse receiptResponse, ReceiptRequest request)
    {
        var nonFiscalRequest = new NonFiscalRequest
        {
            NonFiscalPrints = new List<NonFiscalPrint>()
        };

        try
        {
            var nonFiscalPrints = new List<NonFiscalPrint>
                {
                    new NonFiscalPrint
                    {
                        Data = $"{request.ftReceiptCase.ToString("x")} case for Queue {receiptResponse.ftCashBoxIdentification}"
                    },
                    new NonFiscalPrint
                    {
                        Data = $"Processing"
                    }
                };
            var printerNonFiscal = new PrinterNonFiscal
            {
                PrintNormals = nonFiscalPrints.Select(x => new PrintNormal() { Data = x.Data, Font = x.Font }).ToList()
            };
            var httpResponse = await SendRequestAsync(SoapSerializer.Serialize(printerNonFiscal));
            using var responseContent = await httpResponse.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(responseContent);
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
