using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.Epson.Models;
using fiskaltrust.Middleware.SCU.IT.Epson.QueueLogic.Exceptions;
using fiskaltrust.Middleware.SCU.IT.Epson.QueueLogic.Extensions;
using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.Epson;

#nullable enable
public sealed class EpsonSCU : IITSSCD
{
    private readonly ILogger<EpsonSCU> _logger;
    private readonly EpsonCommandFactory _epsonXmlWriter;
    private readonly EpsonCommunicationClientV2 _epsonCommunicationClientV2;
    private readonly HttpClient _httpClient;
    private readonly string _commandUrl;
    private readonly ErrorInfoFactory _errorCodeFactory = new();
    private string _serialnr = "";

    public EpsonSCU(ILogger<EpsonSCU> logger, EpsonScuConfiguration configuration, EpsonCommandFactory epsonXmlWriter, EpsonCommunicationClientV2 epsonCommunicationClientV2)
    {
        _logger = logger;
        _epsonXmlWriter = epsonXmlWriter;
        _epsonCommunicationClientV2 = epsonCommunicationClientV2;
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

    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public async Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request)
    {
        try
        {
            var content = _epsonXmlWriter.CreateInvoiceRequestContent(request);

            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<PrinterResponse>(responseContent);
            var fiscalReceiptResponse = new FiscalReceiptResponse()
            {
                Success = result?.Success ?? false
            };
            await SetReceiptResponse(request?.Payments, result, fiscalReceiptResponse);
            return fiscalReceiptResponse;
        }
        catch (Exception e)
        {
            return ExceptionInfo(e);
        }
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
            return ExceptionInfo(e);
        }
    }

    public async Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request)
    {
        try
        {
            var content = _epsonXmlWriter.CreatePrintZReportRequestContent(request);
            var response = await SendRequestAsync(content);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var result = SoapSerializer.Deserialize<ReportResponse>(responseContent);
            var dailyClosingResponse = new DailyClosingResponse()
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
            return dailyClosingResponse;
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }
            if (IsConnectionException(e))
            {
                return new DailyClosingResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.Connection } };
            }
            return new DailyClosingResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.General } };
        }
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
            DeviceStatus = ParseStatus(result?.Printerstatus?.MfStatus), // TODO Create enum
            ExpireDeviceCertificateDate = result?.Printerstatus?.ExpiryCD, // TODO Use Datetime; this value seemingly can also be 20
            ExpireTACommunicationCertificateDate = result?.Printerstatus?.ExpiryCA, // TODO use DateTime?
            SerialNumber = _serialnr

        };
    }

    public async Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request)
    {
        try
        {
            var content = _epsonXmlWriter.CreateNonFiscalReceipt(request);
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
            return response;
        }
        catch (Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                msg = msg + " " + e.InnerException.Message;
            }
            if (IsConnectionException(e))
            {
                return new Response() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.Connection } };
            }
            return new Response() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.General } };
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
        var resetCommand = new PrinterCommand() { ResetPrinter = new ResetPrinter() { Operator = "" } };
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

    private bool IsConnectionException(Exception e)
    {
        if (e.GetType().IsAssignableFrom(typeof(EndpointNotFoundException)) ||
            e.GetType().IsAssignableFrom(typeof(WebException)) ||
            e.GetType().IsAssignableFrom(typeof(CommunicationException)) ||
            e.GetType().IsAssignableFrom(typeof(TaskCanceledException)) ||
            e.GetType().IsAssignableFrom(typeof(HttpRequestException)))
        {
            return true;
        }
        return false;
    }

    private FiscalReceiptResponse ExceptionInfo(Exception e)
    {
        var msg = e.Message;
        if (e.InnerException != null)
        {
            msg += " " + e.InnerException.Message;
        }
        if (IsConnectionException(e))
        {
            return new FiscalReceiptResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.Connection } };
        }
        return new FiscalReceiptResponse() { Success = false, SSCDErrorInfo = new SSCDErrorInfo() { Info = msg, Type = SSCDErrorType.General } };
    }

    private SSCDErrorInfo GetErrorInfo(string? code, string? status, string? printerStatus)
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
        var state = GetPrinterStatus(printerStatus);
        if (state != null)
        {
            errorInf += $"\n Printer state {state}";
        }
        _logger.LogError(errorInf);
        return new SSCDErrorInfo() { Info = errorInf, Type = SSCDErrorType.Device };
    }

    private async Task<ReceiptResponse> CreateNonFiscalRequestAsync(ReceiptResponse receiptResponse, ReceiptRequest request)
    {
        var nonFiscalRequest = new NonFiscalRequest
        {
            NonFiscalPrints = new List<NonFiscalPrint>()
        };
        if (request.cbChargeItems != null)
        {
            foreach (var chargeItem in request.cbChargeItems.Where(x => x.IsV0MultiUseVoucherSale()))
            {
                AddVoucherNonFiscalPrints(nonFiscalRequest.NonFiscalPrints, chargeItem.Amount, chargeItem.ftChargeItemCaseData);
            }
        }
        if (request.cbPayItems != null)
        {
            foreach (var payItem in request.cbPayItems.Where(x => x.IsV0VoucherSale()))
            {
                AddVoucherNonFiscalPrints(nonFiscalRequest.NonFiscalPrints, payItem.Amount, payItem.ftPayItemCaseData);
            }
        }
        var response = await NonFiscalReceiptAsync(nonFiscalRequest);
        if (response.Success)
        {
            receiptResponse.ftSignatures = SignatureFactory.CreateVoucherSignatures(nonFiscalRequest);
        }
        return receiptResponse;
    }

    private static void AddVoucherNonFiscalPrints(List<NonFiscalPrint> nonFiscalPrints, decimal amount, string info)
    {
        nonFiscalPrints.Add(new NonFiscalPrint() { Data = "***Voucher***", Font = 2 });
        if (!string.IsNullOrEmpty(info))
        {
            nonFiscalPrints.Add(new NonFiscalPrint() { Data = info, Font = 2 });
        }
        nonFiscalPrints.Add(new NonFiscalPrint()
        {
            Data = Math.Abs(amount).ToString(new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "",
                CurrencyDecimalDigits = 2
            }),
            Font = 2
        });
    }

    private static FiscalReceiptInvoice CreateInvoice(ReceiptRequest request)
    {
        var fiscalReceiptRequest = new FiscalReceiptInvoice()
        {
            //Barcode = ChargeItem.ProductBarcode,
            //TODO DisplayText = "Message on customer display",
            Operator = request.cbUser,
            Items = request.cbChargeItems.Where(x => !x.IsV0PaymentAdjustment()).Select(p => new Item
            {
                Description = p.Description,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice ?? p.Amount / p.Quantity,
                Amount = p.Amount,
                VatGroup = p.GetV0VatGroup(),
                AdditionalInformation = p.ftChargeItemCaseData
            }).ToList(),
            PaymentAdjustments = request.GetV0PaymentAdjustments(),
            Payments = request.GetV0Payments()
        };
        return fiscalReceiptRequest;
    }

    private async Task<FiscalReceiptRefund> CreateRefundAsync(ReceiptRequest request, long receiptnumber, long zReceiptNumber, DateTime receiptDateTime)
    {
        var deviceInfo = await GetDeviceInfoAsync();
        var fiscalReceiptRequest = new FiscalReceiptRefund()
        {
            //TODO Barcode = "0123456789" 
            Operator = "1",
            DisplayText = $"REFUND {zReceiptNumber:D4} {receiptnumber:D4} {receiptDateTime:ddMMyyyy} {deviceInfo.SerialNumber}",
            Refunds = request.cbChargeItems?.Select(p => new Refund
            {
                Description = p.Description,
                Quantity = Math.Abs(p.Quantity),
                UnitPrice = p.UnitPrice ?? 0,
                Amount = Math.Abs(p.Amount),
                VatGroup = p.GetV0VatGroup()
            }).ToList(),
            PaymentAdjustments = request.GetV0PaymentAdjustments(),
            Payments = request.cbPayItems?.Select(p => new Payment
            {
                Amount = p.Amount,
                Description = p.Description,
                PaymentType = p.GetV0PaymentType(),
            }).ToList()
        };
        return fiscalReceiptRequest;
    }

    public async Task<RTInfo> GetRTInfoAsync()
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

        return new RTInfo
        {
            SerialNumber = _serialnr,
            InfoData = JsonConvert.SerializeObject(new DeviceInfo
            {
                DailyOpen = result?.Printerstatus?.DailyOpen == "1",
                DeviceStatus = ParseStatus(result?.Printerstatus?.MfStatus), // TODO Create enum
                ExpireDeviceCertificateDate = result?.Printerstatus?.ExpiryCD, // TODO Use Datetime; this value seemingly can also be 20
                ExpireTACommunicationCertificateDate = result?.Printerstatus?.ExpiryCA, // TODO use DateTime?
                SerialNumber = _serialnr

            })
        };
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        if (!request.ReceiptRequest.IsV2Receipt())
        {
            var receiptResponse = request.ReceiptResponse;
            if (request.ReceiptRequest.IsV0MultiUseVoucherSale())
            {
                return new ProcessResponse
                {
                    ReceiptResponse = await CreateNonFiscalRequestAsync(receiptResponse, request.ReceiptRequest).ConfigureAwait(false)
                };
            }

            FiscalReceiptResponse fiscalResponse;
            if (request.ReceiptRequest.IsV0Void())
            {
                // TODO how will we get the refund information? ==> signatures??
                var fiscalReceiptRefund = await CreateRefundAsync(request.ReceiptRequest, -1, -1, DateTime.MinValue).ConfigureAwait(false);
                fiscalResponse = await FiscalReceiptRefundAsync(fiscalReceiptRefund).ConfigureAwait(false);
            }
            else
            {
                var fiscalReceiptinvoice = CreateInvoice(request.ReceiptRequest);
                fiscalResponse = await FiscalReceiptInvoiceAsync(fiscalReceiptinvoice).ConfigureAwait(false);
            }
            if (!fiscalResponse.Success)
            {
                throw new SSCDErrorException(fiscalResponse.SSCDErrorInfo.Type, fiscalResponse.SSCDErrorInfo.Info);
            }
            else
            {
                receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(fiscalResponse.ReceiptNumber, fiscalResponse.ZRepNumber, fiscalResponse.Amount, fiscalResponse.ReceiptDateTime);
            }
            return new ProcessResponse
            {
                ReceiptResponse = receiptResponse
            };
        }
        else
        {
            return await _epsonCommunicationClientV2.ProcessReceiptAsync(request);
        }
    }
}
