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
    private readonly EpsonRTPrinterSCUConfiguration _configuration;
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
        _configuration = configuration;
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
        try
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
                return Helpers.CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
            }

            if (request.ReceiptRequest.IsReprint())
            {
                return await ProcessPerformReprint(request);
            }

            switch (receiptCase)
            {
                case (long) ITReceiptCases.UnknownReceipt0x0000:
                case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
                case (long) ITReceiptCases.PaymentTransfer0x0002:
                case (long) ITReceiptCases.Protocol0x0005:
                    return Helpers.CreateResponse(await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
            }
            request.ReceiptResponse.SetReceiptResponseErrored($"The given receiptcase 0x{receiptCase.ToString("X")} is not supported by Epson RT Printer.");
            return Helpers.CreateResponse(request.ReceiptResponse);
        }
        catch (Exception ex)
        {
            var signatures = new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "epson-printer-generic-error",
                    Data = $"{ex}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954_2000_0000_3000
                }
            };
            request.ReceiptResponse.ftState |= 0xEEEE_EEEE;
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
        }
    }

    private async Task<FiscalReceiptResponse> SetReceiptResponse(PrinterResponse? result)
    {
        var fiscalReceiptResponse = new FiscalReceiptResponse
        {
            Success = result?.Success ?? false
        };
        if (result?.Success == false)
        {
            fiscalReceiptResponse.SSCDErrorInfo = GetErrorInfo(result.Code, result.Status, result?.Receipt?.PrinterStatus);
            await ResetPrinter();
        }
        else
        {
            fiscalReceiptResponse.ReceiptNumber = result?.Receipt?.FiscalReceiptNumber != null ? long.Parse(result.Receipt.FiscalReceiptNumber) : 0;
            fiscalReceiptResponse.ZRepNumber = result?.Receipt?.ZRepNumber != null ? long.Parse(result.Receipt.ZRepNumber) : 0;
            if (result?.Receipt?.FiscalReceiptDate != null && result?.Receipt?.FiscalReceiptTime != null)
            {
                fiscalReceiptResponse.ReceiptDateTime = DateTime.ParseExact(result.Receipt.FiscalReceiptDate, "d/M/yyyy", CultureInfo.InvariantCulture);
                var time = TimeSpan.Parse(result.Receipt.FiscalReceiptTime);
                fiscalReceiptResponse.ReceiptDateTime = fiscalReceiptResponse.ReceiptDateTime + time;
            }
            else
            {
                fiscalReceiptResponse.ReceiptDateTime = DateTime.Now; // ???????
            }
        }
        return fiscalReceiptResponse;
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

            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                receiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return receiptResponse;
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
            receiptResponse.SetReceiptResponseErrored(response.SSCDErrorInfo?.Info ?? "");
            return receiptResponse;
        }
    }

    private async Task<ProcessResponse> ProcessPerformReprint(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            request.ReceiptResponse.SetReceiptResponseErrored("Cannot refund receipt without references.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        FiscalReceiptResponse fiscalResponse;
        try
        {

            await LoginAsync();
            var response = await PerformReprint("11", "10", "23", long.Parse(referenceDocNumber));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(responseContent);
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            fiscalResponse = fiscalReceiptResponse;
            await ResetPrinter();
        }
        catch (Exception e)
        {
            fiscalResponse = Helpers.ExceptionInfo(e);
        }

        if (!fiscalResponse.Success)
        {
            request.ReceiptResponse.SetReceiptResponseErrored(fiscalResponse.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
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


    private async Task<ProcessResponse> ProcessRefundReceipt(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            request.ReceiptResponse.SetReceiptResponseErrored("Cannot refund receipt without references.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
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
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            fiscalResponse = fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            fiscalResponse = Helpers.ExceptionInfo(e);
        }

        if (!fiscalResponse.Success)
        {
            request.ReceiptResponse.SetReceiptResponseErrored(fiscalResponse.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
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
            request.ReceiptResponse.SetReceiptResponseErrored("Cannot void receipt without references.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
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
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            fiscalResponse = fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            fiscalResponse = Helpers.ExceptionInfo(e);
        }

        if (!fiscalResponse.Success)
        {
            request.ReceiptResponse.SetReceiptResponseErrored(fiscalResponse.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
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

    private async Task<string> GetSerialNumberAsync(string rtType)
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

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptResponse receiptResponse)
    {
        try
        {
            var fiscalReport = new FiscalReport
            {
                ZReport = new ZReport()
            };
            var response = await SendRequestAsync(SoapSerializer.Serialize(fiscalReport));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<ReportResponse>(responseContent);
            if (!result?.Success ?? false)
            {
                var errorInfo = GetErrorInfo(result?.Code, result?.Status, null);
                await ResetPrinter();
                receiptResponse.SetReceiptResponseErrored(errorInfo.Info);
                return receiptResponse;
            }

            var zRepNumber = result?.ReportInfo?.ZRepNumber != null ? long.Parse(result.ReportInfo.ZRepNumber) : 0;
            receiptResponse.ftSignatures = SignatureFactory.CreateDailyClosingReceiptSignatures(zRepNumber);
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
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

    private async Task<HttpResponseMessage> LoginAsync()
    {
        var data = $"""
<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
    <s:Body>
        <printerCommand>
            <directIO command="4038" data="02{_configuration.Password}                                                                                                                               " />
        </printerCommand>
    </s:Body>
</s:Envelope>
""";
        return await SendRequestAsync(data);
    }

    private async Task<HttpResponseMessage> PerformReprint(string day, string month, string year, long receiptNumber)
    {
        var data = $"""
<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
    <s:Body>
        <printerCommand>
            <directIO command="3098" data="01{day}{month}{year}{receiptNumber.ToString().PadLeft(4, '0')}{receiptNumber.ToString().PadLeft(4, '0')}" />
        </printerCommand>
    </s:Body>
</s:Envelope>
""";
        return await SendRequestAsync(data);
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



public class FiscalReceiptResponse
{
    public bool Success { get; set; }
    public SSCDErrorInfo? SSCDErrorInfo { get; set; }
    public DateTime ReceiptDateTime { get; set; }
    public long ReceiptNumber { get; set; }
    public long ZRepNumber { get; set; }
}