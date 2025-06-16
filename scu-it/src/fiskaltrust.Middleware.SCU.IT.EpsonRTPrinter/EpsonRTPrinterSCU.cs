using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using Newtonsoft.Json;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Net.Http;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public sealed class EpsonRTPrinterSCU : LegacySCU
{
    private readonly ILogger<EpsonRTPrinterSCU> _logger;
    private readonly IEpsonFpMateClient _httpClient;
    private readonly EpsonRTPrinterSCUConfiguration _configuration;
    private readonly ErrorInfoFactory _errorCodeFactory = new();
    private string? _serialnr;

    public EpsonRTPrinterSCU(ILogger<EpsonRTPrinterSCU> logger, EpsonRTPrinterSCUConfiguration configuration, IEpsonFpMateClient epsonCloudHttpClient)
    {
        _logger = logger;
        _httpClient = epsonCloudHttpClient;
        _configuration = configuration;
    }

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public override async Task<RTInfo> GetRTInfoAsync()
    {
        var result = await QueryPrinterStatusAsync();
        _logger.LogInformation(JsonConvert.SerializeObject(result));
        _serialnr = "";
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
        var response = await _httpClient.SendCommandAsync(SoapSerializer.Serialize(queryPrinterStatus));
        using var responseContent = await response.Content.ReadAsStreamAsync();
        return SoapSerializer.DeserializeToSoapEnvelope<StatusResponse>(responseContent);
    }

    public override async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        try
        {
            var receiptCase = request.ReceiptRequest.GetReceiptCase();
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
                return await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse);
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

            if (request.ReceiptRequest.IsMonthlyClosing())
            {
                return Helpers.CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
            }

            if (request.ReceiptRequest.IsYearlyClosing())
            {
                return Helpers.CreateResponse(await PerformDailyCosing(request.ReceiptResponse));
            }
   
            if (request.ReceiptRequest.IsReprint())
            {
                return await ProcessPerformReprint(request);
            }

            if (receiptCase == (long) ITReceiptCases.ProtocolUnspecified0x3000 &&  ((request.ReceiptRequest.ftReceiptCase & 0x0000_0002_0000_0000) != 0))
            {
                return await ProcessUnspecifiedProtocolReceipt(request);
            }

            if (receiptCase == (long) ITReceiptCases.Protocol0x0005)
            {
                return Helpers.CreateResponse(await PerformProtocolReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
            }

            switch (receiptCase)
            {
                case (long) ITReceiptCases.UnknownReceipt0x0000:
                case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
                    return Helpers.CreateResponse(await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
            }
            request.ReceiptResponse.SetReceiptResponseErrored($"The given receiptcase 0x{receiptCase.ToString("X")} is not supported by Epson RT Printer.");
            return Helpers.CreateResponse(request.ReceiptResponse);
        }
        catch (Exception ex)
        {
            request.ReceiptResponse.SetReceiptResponseErrored("epson-printer-generic-error", ex.ToString());
            return Helpers.CreateResponse(request.ReceiptResponse);
        }
    }

    private async Task<FiscalReceiptResponse> SetReceiptResponse(PrinterReceiptResponse? result)
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

    public async Task<ReceiptResponse> PerformProtocolReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            var content = EpsonCommandFactory.CreateInvoiceRequestContent(_configuration, receiptRequest);
            var customerData = receiptRequest.GetCustomer();
            if (customerData != null)
            {
                if (content.PrintRecMessageType3 == null)
                {
                    content.PrintRecMessageType3 = new List<PrintRecMessage>();
                }
                content.PrintRecMessageType3?.Add(new PrintRecMessage
                {
                    MessageType = 2,
                    Index = "1",
                    Message = customerData.CustomerName ?? ""
                });
                content.PrintRecMessageType3?.Add(new PrintRecMessage
                {
                    MessageType = 2,
                    Index = "2",
                    Message = customerData.CustomerStreet ?? ""
                });
                content.PrintRecMessageType3?.Add(new PrintRecMessage
                {
                    MessageType = 2,
                    Index = "3",
                    Message = string.Format("{0} {1} {2}", customerData.CustomerCountry ?? "", customerData.CustomerZip ?? "", customerData.CustomerCity ?? "")
                });
            }

            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
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
                RTSerialNumber = result?.Receipt?.SerialNumber,
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data           
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();

            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                receiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }

            return receiptResponse;
        }
        catch (Exception e)
        {
            var response = Helpers.ExceptionInfo(e);
            receiptResponse.SetReceiptResponseErrored(response.SSCDErrorInfo?.Info ?? "");
            return receiptResponse;
        }
    }

    public async Task<ReceiptResponse> PerformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        try
        {
            var content = EpsonCommandFactory.CreateInvoiceRequestContent(_configuration, receiptRequest);
            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, data);
            var response = await _httpClient.SendCommandAsync(data);
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", receiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                _logger.LogError("Error while processing classic receipt: {error}", fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "NOERROR");
                receiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return receiptResponse;
            }
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber ?? "",
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "POSRECEIPT",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data           
            };
            receiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                receiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }
            return receiptResponse;
        }
        catch (Exception e)
        {
            var response = Helpers.ExceptionInfo(e);
            _logger.LogError(e, "Error while processing classic receipt: {error}", response.SSCDErrorInfo?.Info);
            receiptResponse.SetReceiptResponseErrored(response.SSCDErrorInfo?.Info ?? "");
            return receiptResponse;
        }
    }

    private async Task<ProcessResponse> ProcessUnspecifiedProtocolReceipt(ProcessRequest request)
    {
        try
        {
            var content = EpsonCommandFactory.PerformUnspecifiedProtocolReceipt(request.ReceiptRequest);
            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var printerResponse = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(responseContent);

            if (printerResponse?.Success == false)
            {
                var error = GetErrorInfo(printerResponse?.Code, printerResponse?.Status, printerResponse?.Receipt?.PrinterStatus)?.Info;
                request.ReceiptResponse.SetReceiptResponseErrored(error ?? "Failed to process unspecified protocol");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            await ResetPrinter();
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        catch (Exception e)
        {
            var errorInfo = Helpers.ExceptionInfo(e);
            request.ReceiptResponse.SetReceiptResponseErrored(errorInfo.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
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
        PrinterReceiptResponse result = null;
        try
        {
            if (!string.IsNullOrEmpty(_configuration.Password))
            {
                var loginResult = await LoginAsync();
                if (!loginResult.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"An error occured while sending a request to the Epson device (StatusCode: {loginResult.StatusCode}, Content: {await loginResult.Content.ReadAsStringAsync()})");
                }
                using var loginResultresponseContent = await loginResult.Content.ReadAsStreamAsync();
                var loginprinterresult = SoapSerializer.DeserializeToSoapEnvelope<PrinterResponse>(loginResultresponseContent);
                var loginReceiptResponse = await SetReceiptResponse(loginprinterresult);
                if (!loginReceiptResponse.Success)
                {
                    request.ReceiptResponse.SetReceiptResponseErrored($"Unable to login to the Printer. Please check the configured password. (Details: {loginReceiptResponse.SSCDErrorInfo?.Info ?? ""})");
                    return new ProcessResponse
                    {
                        ReceiptResponse = request.ReceiptResponse
                    };
                }
            }

            var date = DateTime.Parse(referenceDateTime);
            var response = await PerformReprint(date.ToString("dd"), date.ToString("MM"), date.ToString("yy"), long.Parse(referenceDocNumber));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
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
                RTSerialNumber = result?.Receipt?.SerialNumber,
                RTZNumber = fiscalResponse.ZRepNumber,
                RTDocNumber = fiscalResponse.ReceiptNumber,
                RTDocMoment = fiscalResponse.ReceiptDateTime,
                RTDocType = "Documento Gestionale",
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

        if (string.IsNullOrEmpty(_serialnr))
        {
            _ = await GetRTInfoAsync();
        }
        var content = EpsonCommandFactory.CreateRefundRequestContent(_configuration, request.ReceiptRequest, long.Parse(referenceDocNumber), long.Parse(referenceZNumber), DateTime.Parse(referenceDateTime), _serialnr);
        try
        {
           var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }

            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }

            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber,
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "REFUND",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data
                RTReferenceZNumber = long.Parse(referenceZNumber),
                RTReferenceDocNumber = long.Parse(referenceDocNumber),
                RTReferenceDocMoment = DateTime.Parse(referenceDateTime)
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();
            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                request.ReceiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        catch (Exception e)
        {
            var errorInfo = Helpers.ExceptionInfo(e);
            _logger.LogError(e, "Error while processing refund receipt: {error}", errorInfo.SSCDErrorInfo?.Info);
            request.ReceiptResponse.SetReceiptResponseErrored(errorInfo.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
    }

    private async Task<ProcessResponse> ProcessVoidReceipt(ProcessRequest request)
    {
        var referenceZNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
        var referenceDocNumber = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
        var referenceDateTime = request.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
        if (string.IsNullOrEmpty(referenceZNumber) || string.IsNullOrEmpty(referenceDocNumber) || string.IsNullOrEmpty(referenceDateTime))
        {
            request.ReceiptResponse.SetReceiptResponseErrored($"The given cbPreviousReceiptReference '{request.ReceiptRequest.cbPreviousReceiptReference}' does not reference a request with RT references.");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }

        if (string.IsNullOrEmpty(_serialnr))
        {
            _ = await GetRTInfoAsync();
        }
        var content = EpsonCommandFactory.CreateVoidRequestContent(_configuration, request.ReceiptRequest, long.Parse(referenceDocNumber), long.Parse(referenceZNumber), DateTime.Parse(referenceDateTime), _serialnr);
        try
        {
            var data = SoapSerializer.Serialize(content);
            _logger.LogDebug("Request content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(data));
            var response = await _httpClient.SendCommandAsync(data);
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterReceiptResponse>(responseContent);
            if (result != null)
            {
                _logger.LogDebug("Response content ({receiptreference}): {content}", request.ReceiptRequest.cbReceiptReference, SoapSerializer.Serialize(result));
            }
            var fiscalReceiptResponse = await SetReceiptResponse(result);
            if (!fiscalReceiptResponse.Success)
            {
                request.ReceiptResponse.SetReceiptResponseErrored(fiscalReceiptResponse.SSCDErrorInfo?.Info ?? "");
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };
            }
            var posReceiptSignatur = new POSReceiptSignatureData
            {
                RTSerialNumber = result?.Receipt?.SerialNumber,
                RTZNumber = fiscalReceiptResponse.ZRepNumber,
                RTDocNumber = fiscalReceiptResponse.ReceiptNumber,
                RTDocMoment = fiscalReceiptResponse.ReceiptDateTime,
                RTDocType = "VOID",
                RTCodiceLotteria = "",
                RTCustomerID = "", // Todo dread customerid from data
                RTReferenceZNumber = long.Parse(referenceZNumber),
                RTReferenceDocNumber = long.Parse(referenceDocNumber),
                RTReferenceDocMoment = DateTime.Parse(referenceDateTime)
            };
            request.ReceiptResponse.ftSignatures = SignatureFactory.CreateDocumentoCommercialeSignatures(posReceiptSignatur).ToArray();

            if (result?.Receipt?.PrinterStatus != null && !result.Receipt.PrinterStatus.StartsWith("0"))
            {
                request.ReceiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.Receipt?.PrinterStatus) ?? "");
            }
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
        catch (Exception e)
        {
            var errorInfo = Helpers.ExceptionInfo(e);
            _logger.LogError(e, "Error while processing void receipt: {error}", errorInfo.SSCDErrorInfo?.Info);
            request.ReceiptResponse.SetReceiptResponseErrored(errorInfo.SSCDErrorInfo?.Info ?? "");
            return new ProcessResponse
            {
                ReceiptResponse = request.ReceiptResponse
            };
        }
    }

    private async Task<string> GetSerialNumberAsync(string rtType)
    {
        var serialQuery = new PrinterCommand() { DirectIO = DirectIO.GetSerialNrCommand() };
        var content = SoapSerializer.Serialize(serialQuery);
        var responseSerialnr = await _httpClient.SendCommandAsync(content);

        using var responseContent = await responseSerialnr.Content.ReadAsStreamAsync();
        var result = SoapSerializer.DeserializeToSoapEnvelope<PrinterCommandResponse>(responseContent);

        var serialnr = result?.CommandResponse?.ResponseData;
        return serialnr?.Substring(10, 2) + rtType + serialnr?.Substring(8, 2) + serialnr?.Substring(2, 6);
    }

    private async Task ResetPrinter()
    {
        var resetCommand = new PrinterCommand() { ResetPrinter = new ResetPrinter() { Operator = "" } };
        var xml = SoapSerializer.Serialize(resetCommand);
        await _httpClient.SendCommandAsync(xml);
    }

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptResponse receiptResponse)
    {
        try
        {
            var fiscalReport = new FiscalReport
            {
                ZReport = new ZReport()
            };
            var response = await _httpClient.SendCommandAsync(SoapSerializer.Serialize(fiscalReport));
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
            if (result?.ReportInfo?.PrinterStatus != null && !result.ReportInfo.PrinterStatus.StartsWith("0"))
            {
                receiptResponse.AddWarningSignatureItem(Helpers.GetPrinterStatus(result?.ReportInfo?.PrinterStatus) ?? "");
            }
            return receiptResponse;
        }
        catch (Exception e)
        {
            receiptResponse.SetReceiptResponseErrored(e.Message);
            return receiptResponse;
        }
    }

    private async Task<ProcessResponse> PerformZeroReceiptOperationAsync(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        await ResetPrinter();
        var result = await QueryPrinterStatusAsync();
        var signatures = SignatureFactory.CreateZeroReceiptSignatures().ToList();
        if (request.IsXReportZeroReceipt())
        {
            var fiscalReport = new FiscalReport
            {
                XReport = new XReport()
            };
            var response = await _httpClient.SendCommandAsync(SoapSerializer.Serialize(fiscalReport));
            using var responseContent = await response.Content.ReadAsStreamAsync();
            var reportResponse = SoapSerializer.DeserializeToSoapEnvelope<ReportResponse>(responseContent);
            if (!(result?.Success ?? false))
            {
                var errorInfo = GetErrorInfo(result?.Code, result?.Status, null);
                await ResetPrinter();
                receiptResponse.SetReceiptResponseErrored(errorInfo.Info);
            }
        }
        var stateData = JsonConvert.SerializeObject(new
        {
            PrinterStatus = result
        });
        return ProcessResponseHelpers.CreateResponse(receiptResponse, stateData, signatures);
    }

    private async Task<HttpResponseMessage> LoginAsync() => await _httpClient.SendCommandAsync(EpsonCommandFactory.LoginCommand(_configuration.Password));

    private async Task<HttpResponseMessage> PerformReprint(string day, string month, string year, long receiptNumber) => await _httpClient.SendCommandAsync(EpsonCommandFactory.ReprintCommand(day, month, year, receiptNumber));

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
