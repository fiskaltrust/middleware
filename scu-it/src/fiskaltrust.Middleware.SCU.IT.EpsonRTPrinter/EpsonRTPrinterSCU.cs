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
    private string _serialnr;

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

            if (request.ReceiptRequest.IsReprint())
            {
                return await ProcessPerformReprint(request);
            }

            if (receiptCase == (long) ITReceiptCases.ProtocolUnspecified0x3000)
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
            var data = SoapSerializer.Serialize(content);;
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

    private static string GetAmountString(decimal amount, int length)
    {
        var amountText = string.Format("{0:0.00}", amount).Replace(".",",");
        
        if (amountText.Length < length)
        {
            amountText = new string(' ', length-amountText.Length) + amountText;
        }

        return amountText;
    }

    private static (List<PrintItem>, List<string>) GetChargeItemLines(ChargeItem chargeItem, string vatText, string vatLegendText)
    {
        var resultItems = new List<PrintItem>();
        var resultVatLegend = new List<string>();

        var isRefundOrVoid = ReceiptCaseHelper.IsVoid(chargeItem) || ReceiptCaseHelper.IsRefund(chargeItem);
        var quantity = isRefundOrVoid ? -chargeItem.Quantity : chargeItem.Quantity;
        var amount = isRefundOrVoid ? -chargeItem.Amount : chargeItem.Amount;
        var description = chargeItem.Description;

        if (quantity == 0)
        {
            while (description.Length > 0)
            {
                var desc = description.Length <= 46 ? description : description.Substring(0, 46);
                resultItems.Add(new PrintNormal() { Operator = "1", Data = desc });
                description = description.Substring(desc.Length);
            }
            if (!string.IsNullOrWhiteSpace(chargeItem.ftChargeItemCaseData))
            {
                switch (chargeItem.ftChargeItemCase & 0x0000_00F0_0000_0000)
                {
                    case 0x0000_0010_0000_0000: //BMP
                    {
                        resultItems.Add(new PrintGraphicCoupon() { Operator = "1", GraphicFormat = PrintGraphicCouponGraphicFormat.BMP, Base64Data = chargeItem.ftChargeItemCaseData });
                        break;
                    }
                    case 0x0000_0020_0000_0000: //Raster
                    {
                        resultItems.Add(new PrintGraphicCoupon() { Operator = "1", GraphicFormat = PrintGraphicCouponGraphicFormat.Raster, Base64Data = chargeItem.ftChargeItemCaseData });
                        break;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(chargeItem.ProductBarcode))
            {
                switch (chargeItem.ftChargeItemCase & 0x0000_000F_0000_0000)
                {
                    case 0x0000_0000_0000_0000: //EAN13
                    {
                        if ((chargeItem.ProductBarcode.Length != 13) || 
                            !chargeItem.ProductBarcode.All(char.IsDigit))
                        {
                            throw new Exception("EAN 13 code must be 13 numeric chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.EAN13, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_0001_0000_0000: //EAN8
                    {
                        if ((chargeItem.ProductBarcode.Length != 8) || 
                            !chargeItem.ProductBarcode.All(char.IsDigit))
                        {
                            throw new Exception("EAN 8 code must be 8 numeric chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.EAN8, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_0002_0000_0000: //UPCA
                    {
                        if ((chargeItem.ProductBarcode.Length != 12) || 
                            !chargeItem.ProductBarcode.All(char.IsDigit))
                        {
                            throw new Exception("UPC-A code must be 12 numeric chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.UPCA, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_0003_0000_0000: //UPCE
                    {
                        if ((chargeItem.ProductBarcode.Length != 12) || 
                            !chargeItem.ProductBarcode.All(char.IsDigit) || 
                            !chargeItem.ProductBarcode.StartsWith("0"))
                        {
                            throw new Exception("UPC-E code must be 12 numeric chars length and start with 0!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.UPCE, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_0004_0000_0000: //CODE39
                    {
                        if ((chargeItem.ProductBarcode.Length < 1) || 
                            (chargeItem.ProductBarcode.Length > 34))
                        {
                            throw new Exception("CODE39 code must be 1 to 34 chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODE39, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                        break;
                    }
                    case 0x0000_0005_0000_0000: //CODE93
                    {
                        if ((chargeItem.ProductBarcode.Length < 1) ||
                            (chargeItem.ProductBarcode.Length > 59))
                        {
                            throw new Exception("CODE93 code must be 1 to 59 chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODE93, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                        break;
                    }
                    case 0x0000_0006_0000_0000: //CODE128
                    {
                        if ((chargeItem.ProductBarcode.Length < 3) ||
                            (chargeItem.ProductBarcode.Length > 100) || 
                            (!chargeItem.ProductBarcode.StartsWith("{A") && 
                            !chargeItem.ProductBarcode.StartsWith("{B") &&
                            !chargeItem.ProductBarcode.StartsWith("{C")))
                        {
                            throw new Exception("CODE128 code must be 3 to 100 chars length and must start with either {A or {B or {C!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODE128, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                        break;
                    }
                    case 0x0000_0007_0000_0000: //CODABAR
                    {
                        if ((chargeItem.ProductBarcode.Length < 1) ||
                            (chargeItem.ProductBarcode.Length > 47))
                        {
                            throw new Exception("CODABAR code must be 1 to 47 chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CODABAR, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_0008_0000_0000: //ITF
                    {
                        if ((chargeItem.ProductBarcode.Length < 2) ||
                            (chargeItem.ProductBarcode.Length > 62) ||
                            (chargeItem.ProductBarcode.Length % 2 == 1) ||
                            !chargeItem.ProductBarcode.All(char.IsDigit))
                        {
                            throw new Exception("ITF code must be 2 to 62 numeric chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.ITF, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_0009_0000_0000: //QRCODE1
                    {
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.QRCODE1, QRCodeAlignment = PrintBarCodeQRCodeAlignment.Centred, QRCodeDataType = PrintBarCodeQRCodeDataType.AlphaNumeric, QRCodeErrorCorrection = 0, QRCodeSize = 4 });
                        break;
                    }
                    case 0x0000_000A_0000_0000: //QRCODE2
                    {
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.QRCODE2, QRCodeAlignment = PrintBarCodeQRCodeAlignment.Centred, QRCodeDataType = PrintBarCodeQRCodeDataType.AlphaNumeric, QRCodeErrorCorrection = 2, QRCodeSize = 4 });
                        break;
                    }
                    case 0x0000_000B_0000_0000: //CodeType74
                    {
                        if ((chargeItem.ProductBarcode.Length < 2) ||
                            (chargeItem.ProductBarcode.Length > 96))
                        {
                            throw new Exception("74 code must be 2 to 96 chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType74, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_000C_0000_0000: //CodeType75
                    {
                        if ((chargeItem.ProductBarcode.Length != 13) ||
                            !chargeItem.ProductBarcode.All(char.IsDigit))
                        {
                            throw new Exception("75 code must be 13 numeric chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType75, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_000D_0000_0000: //CodeType76
                    {
                        if ((chargeItem.ProductBarcode.Length != 13) ||
                            !chargeItem.ProductBarcode.All(char.IsDigit))
                        {
                            throw new Exception("76 code must be 13 numeric chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType76, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_000E_0000_0000: //CodeType77
                    {
                        if ((chargeItem.ProductBarcode.Length != 13) ||
                            !chargeItem.ProductBarcode.All(char.IsDigit) ||
                            (!chargeItem.ProductBarcode.StartsWith("0") &&
                            !chargeItem.ProductBarcode.StartsWith("1")))
                        {
                            throw new Exception("77 code must be 13 numeric chars length and start with 0 or 1!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType77, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width3 });
                        break;
                    }
                    case 0x0000_000F_0000_0000: //CodeType78
                    {
                        if ((chargeItem.ProductBarcode.Length < 2) ||
                            (chargeItem.ProductBarcode.Length > 70))
                        {
                            throw new Exception("78 code must be 2 to 70 chars length!");
                        }
                        resultItems.Add(new PrintBarCode() { Operator = "1", Code = chargeItem.ProductBarcode, CodeType = PrintBarCodeType.CodeType78, Height = 128, HRIFont = PrintBarCodeHRIFont.A, HRIPosition = PrintBarCodeHRIPosition.Below, Position = "901", Width = PrintBarCodeWidth.Width2 });
                        break;
                    }
                }
            }
        }
        else if (quantity > 0)
        {
            if (!string.IsNullOrWhiteSpace(vatLegendText) && !resultVatLegend.Contains(vatLegendText))
            {
                resultVatLegend.Add(vatLegendText);
            }

            var amountText = GetAmountString(amount, 13);

            description = description.Length <= 38 ? description : description.Substring(0, 38);
            if (description.Length <= 25)
            {
                var desc = description.Length <= 25 ? description + new string(' ', 25 - description.Length) : description.Substring(0, 25);
                resultItems.Add(new PrintNormal() { Operator = "1", Data = $"{desc} {vatText} {amountText}" });
            }
            else
            {
                var desc = description.Length <= 25 ? description + new string(' ', 25 - description.Length) : description.Substring(0, 25);
                resultItems.Add(new PrintNormal() { Operator = "1", Data = $"{desc}" });
                desc = description.Substring(25);
                desc += new string(' ', 25 - desc.Length);
                resultItems.Add(new PrintNormal() { Operator = "1", Data = $"{desc} {vatText} {amountText}" });
            }
            if (quantity > 1)
            {
                resultItems.Add(new PrintNormal() { Operator = "1", Data = $"  n.{quantity} * {amount / quantity:0.00}" });
            }
        }
        if (!string.IsNullOrWhiteSpace(chargeItem.ProductBarcode))
        {
            //TODO establish the string content
        }

        return (resultItems, resultVatLegend);
    }

    private static PrinterNonFiscal PerformUnspecifiedProtocolReceipt(ReceiptRequest request)
    {
        var content = new PrinterNonFiscal();

        content.BeginNonFiscal = new BeginNonFiscal() { Operator = "1" };
        content.EndNonFiscal = new EndNonFiscal() { Operator = "1" };
        content.PrintItems = new List<PrintItem>();

        var vatLegend = new List<string>();

        var isReceiptLike = request.cbChargeItems.Where(x => x.Amount != 0).Count() > 0 && request.cbPayItems.Where(x => x.Amount != 0).Count() > 0;

        if (isReceiptLike)
        {
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"DESCRIZIONE                 IVA      Prezzo(€)" });
        }

        var totalCi = 0M;
        var vat = 0M;

        foreach (var ci in request.cbChargeItems)
        {
            var vatValues = EpsonCommandFactory.GetVatInfo(ci);

            var cil = GetChargeItemLines(ci, vatValues.Item1, vatValues.Item2);
            content.PrintItems.AddRange(cil.Item1);
            vatLegend.AddRange(cil.Item2);
            totalCi += ci.Amount;
            vat += ci.Amount * vatValues.Item3 / 100;
        }

        if (isReceiptLike)
        {
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"" });
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Subtotale                            {GetAmountString(totalCi, 9)}" });
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"" });
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"TOTALE COMPLESSIVO                   {GetAmountString(totalCi, 9)}" });
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"DI CUI IVA                           {GetAmountString(vat, 9)}" });
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"" });
        }

        var totalPi = 0M;
        var totalPaid = 0M;
        var piAmounts = new Dictionary<int, decimal>();
        var piDesc = new Dictionary<string, decimal>();

        foreach (var pi in request.cbPayItems)
        {
            var pt = EpsonCommandFactory.GetEpsonPaymentType(pi);
            
            if (!piAmounts.ContainsKey(pt.PaymentType))
            {
                piAmounts.Add(pt.PaymentType, 0);
            }

            piAmounts[pt.PaymentType] += pi.Amount;

            if (!piDesc.ContainsKey(pi.Description))
            {
                piDesc.Add(pi.Description, 0);
            }

            piDesc[pi.Description] += pi.Amount;

            if (pt.PaymentType != 5 && pt.PaymentType != 6)
            {
                totalPaid += pi.Amount;
            }

            totalPi += pi.Amount;
        }

        if (isReceiptLike)
        {
            foreach (var piAmount in piAmounts.Keys.OrderBy(x => x))
            {
                switch (piAmount)
                {
                    case 0:
                    {
                        content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Pagamento contanti               {GetAmountString(piAmounts[piAmount], 13)}" });
                        break;
                    }
                    case 1:
                    {
                        content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Assegni                          {GetAmountString(piAmounts[piAmount], 13)}" });
                        break;
                    }
                    case 2:
                    {
                        content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Pagamento elettronico            {GetAmountString(piAmounts[piAmount], 13)}" });
                        break;
                    }
                    case 3:
                    {
                        content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Buono                            {GetAmountString(piAmounts[piAmount], 13)}" });
                        break;
                    }
                    case 4:
                    {
                        content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Buoni                            {GetAmountString(piAmounts[piAmount], 13)}" });
                        break;
                    }
                    case 5:
                    {
                        content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Non riscosso                     {GetAmountString(piAmounts[piAmount], 13)}" });
                        break;
                    }
                    case 6:
                    {
                        content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Sconto a pagare                  {GetAmountString(piAmounts[piAmount], 13)}" });
                        break;
                    }
                }
            }

            if (totalPi > totalCi)
            {
                content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Resto                                {GetAmountString(totalPi - totalCi, 9)}" });
                totalPaid -= totalPi - totalCi;
            }
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"Importo pagato                       {GetAmountString(totalPaid, 9)}" });
            content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"" });

            foreach (var vatLegendLine in vatLegend)
            {
                content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = vatLegendLine });
            }
        }

        //content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = $"" });
        //content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = GetCenteredText($"{request.cbReceiptMoment.Day:00}-{request.cbReceiptMoment.Month:00}-{request.cbReceiptMoment.Year:0000} {request.cbReceiptMoment.Hour:00}:{request.cbReceiptMoment.Minute:00}", 46)} );
        //content.PrintItems.Add(new PrintNormal() { Operator = "1", Data = GetCenteredText($"DOCUMENTO GESTIONALE N.{request.cbReceiptReference}", 46) });

        return content;
    }

    private async Task<ProcessResponse> ProcessUnspecifiedProtocolReceipt(ProcessRequest request)
    {
        try
        {
            var content = PerformUnspecifiedProtocolReceipt(request.ReceiptRequest);
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

    private async Task<HttpResponseMessage> LoginAsync()
    {
        var password = (_configuration.Password ?? "").PadRight(100, ' ').PadRight(32, ' ');
        var data = $"""
<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
    <s:Body>
        <printerCommand>
            <directIO command="4038" data="02{password}" />
        </printerCommand>
    </s:Body>
</s:Envelope>
""";
        return await _httpClient.SendCommandAsync(data);
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
        return await _httpClient.SendCommandAsync(data);
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
