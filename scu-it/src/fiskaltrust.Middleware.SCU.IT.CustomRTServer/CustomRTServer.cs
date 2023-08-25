using System.Collections.Generic;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using System.Security.Cryptography;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

#pragma warning disable

#nullable enable
public sealed partial class CustomRTServer : IITSSCD
{
    private readonly ILogger<CustomRTServer> _logger;
    private readonly CustomRTServerClient _client;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

    private Dictionary<Guid, string> CashUUIdMappings = new Dictionary<Guid, string>();

    private List<CommercialDocument> _receiptQueue = new List<CommercialDocument>();
    private string _cashToken;
    private string _cashHmacKey;
    private int _currentZNumber;

    public CustomRTServer(ILogger<CustomRTServer> logger, CustomRTServerConfiguration configuration, CustomRTServerClient client)
    {
        _logger = logger;
        _client = client;
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

    /** These methods are kept for backwards compatibility with the interface but we will not use them **/

    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => throw new NotImplementedException();
    public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotImplementedException();
    public Task<DeviceInfo> GetDeviceInfoAsync() => throw new NotImplementedException();
    public Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => throw new NotImplementedException();

    public async Task<RTInfo> GetRTInfoAsync()
    {
        return new RTInfo
        {
            SerialNumber = null,
            InfoData = null
        };
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCaseVersion = request.ReceiptRequest.ftReceiptCase & 0xF000;
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFF;

        if (receiptCaseVersion == 0x0000)
        {
            receiptCase = GetMappedReceitCase(receiptCase);
        }

        switch (receiptCase)
        {
            case (long) ITReceiptCases.ZeroReceipt0x200:
                // TODO perform check for connection?
                break;
            case (long) ITReceiptCases.DailyClosing0x212:
                break;
            case (long) ITReceiptCases.ShiftClosing0x211:
            case (long) ITReceiptCases.YearlyClosing0x214:
            case (long) ITReceiptCases.MonthlyClosing0x213:
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };

            case (long) ITReceiptCases.InitialOperationReceipt0xF01:
                return new ProcessResponse
                {
                    ReceiptResponse = await PerformInitOperationAsync(request.ReceiptRequest, request.ReceiptResponse)
                };
            case (long) ITReceiptCases.OutOfOperationReceipt0xF02:
                return new ProcessResponse
                {
                    ReceiptResponse = await PerformOutOfOperationAsync(request.ReceiptRequest, request.ReceiptResponse)
                };

            case (long) ITReceiptCases.ProtocolTechnicalEvent0x301:
            case (long) ITReceiptCases.ProtocolAccountingEvent0x302:
            case (long) ITReceiptCases.ProtoclUnspecified0x303:
            case (long) ITReceiptCases.InternalUsageMaterialConsumption0x304:
            case (long) ITReceiptCases.ECommerce0x006:
                return new ProcessResponse
                {
                    ReceiptResponse = request.ReceiptResponse
                };

            case (long) ITReceiptCases.InvoiceUnpsecified0x101:
            case (long) ITReceiptCases.InvoiceB2B0x102:
            case (long) ITReceiptCases.InvoiceB2C0x103:
            case (long) ITReceiptCases.InvoiceB2G0x104:
            case (long) ITReceiptCases.CashDepositPayInn0x002:
            case (long) ITReceiptCases.CashPayOut0x003:
            case (long) ITReceiptCases.PaymentTransfer0x004:
            case (long) ITReceiptCases.POSReceiptWithoutCashRegisterObligation0x005:
            case (long) ITReceiptCases.SaleInForeignCountry0x007:
            case (long) ITReceiptCases.UnknownReceipt0x00:
            case (long) ITReceiptCases.POSReceipt0x001:
            default:
                return new ProcessResponse
                {
                    ReceiptResponse = await PreformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse)
                };
        }
    }

    private async Task<ReceiptResponse> PerformInitOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var result = await _client.InsertCashRegisterAsync(receiptResponse.ftCashBoxIdentification, "", "", "", "");
        receiptResponse.ftSignatures = SignatureFactory.CreateInitialOperationSignatures();
        return receiptResponse;
    }

    private async Task<ReceiptResponse> PerformOutOfOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var result = await _client.CancelCashRegisterAsync(GetCashUUID(receiptResponse), "");
        receiptResponse.ftSignatures = SignatureFactory.CreateInitialOperationSignatures();
        return receiptResponse;
    }

    private string GetCashUUID(ReceiptResponse receiptResponse)
    {
        return CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)];
    }

    private async Task<ReceiptResponse> PreformRefundReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var cashuuid = GetCashUUID(receiptResponse);
        var fiscalDocument = new FDocument();
        // TODO implement refund
        fiscalDocument.document = new DocumentData
        {
            doctype = 1,
            amount = (int) receiptRequest.cbChargeItems.Sum(x => x.Amount) * 100,
            businessname = "",
            cashuuid = cashuuid,
            docnumber = int.Parse(receiptResponse.ftReceiptIdentification.Split('#')[0]),
            docznumber = _currentZNumber,
            dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
            fiscalcode = "",
            fiscaloperator = "",
            grandTotal = (int) receiptRequest.cbChargeItems.Sum(x => x.Amount) * 100,
            prevSignature = "",
            vatcode = "",
            referenceClosurenumber = 999999,
            referenceDocnumber = 99999,
            referenceDtime = "",
        };
        fiscalDocument.items = GenerateItemDataForReceiptRequest(receiptRequest);
        fiscalDocument.taxs = GenerateTaxDataForReceiptRequest(receiptRequest);
        var qrCodeData = new QrCodeData
        {
            addInfo = "",
            shaMetadata = "",
            signature = "",
        };
        var commercialDocument = new CommercialDocument
        {
            fiscalData = fiscalDocument,
            qrCodeData = qrCodeData,
        };
        _receiptQueue.Add(commercialDocument);
        receiptResponse.ftSignatures = CreatePosReceiptSignatures(fiscalDocument.document.docnumber, fiscalDocument.document.docznumber, fiscalDocument.document.amount, receiptRequest.cbReceiptMoment);
        return receiptResponse;
    }

    private async Task<ReceiptResponse> PreformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var cashuuid = GetCashUUID(receiptResponse);
        var fiscalDocument = new FDocument();
        fiscalDocument.document = new DocumentData
        {
            doctype = 1,
            amount = (int) receiptRequest.cbChargeItems.Sum(x => x.Amount) * 100,
            businessname = "",
            cashuuid = cashuuid,
            docnumber = int.Parse(receiptResponse.ftReceiptIdentification.Split('#')[0]),
            docznumber = _currentZNumber,
            dtime = receiptRequest.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
            fiscalcode = "",
            fiscaloperator = "",
            grandTotal = (int) receiptRequest.cbChargeItems.Sum(x => x.Amount) * 100,
            prevSignature = "",
            vatcode = "",
            referenceClosurenumber = -1,
            referenceDocnumber = -1,
            referenceDtime = null,
        };
        fiscalDocument.items = GenerateItemDataForReceiptRequest(receiptRequest);
        fiscalDocument.taxs = GenerateTaxDataForReceiptRequest(receiptRequest);
        var qrCodeData = new QrCodeData
        {
            addInfo = "",
            shaMetadata = "",
            signature = "",
        };
        var commercialDocument = new CommercialDocument
        {
            fiscalData = fiscalDocument,
            qrCodeData = qrCodeData,
        };
        _receiptQueue.Add(commercialDocument);
        receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(fiscalDocument.document.docnumber, fiscalDocument.document.docznumber, fiscalDocument.document.amount, receiptRequest.cbReceiptMoment);
        return receiptResponse;
    }

    private List<DocumentItemData> GenerateItemDataForReceiptRequest(ReceiptRequest receiptRequest) => throw new NotImplementedException();

    private List<DocumentTaxData> GenerateTaxDataForReceiptRequest(ReceiptRequest receiptRequest) => throw new NotImplementedException();

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var cashuuid = GetCashUUID(receiptResponse);
        var amount = "0";
        var status = await _client.GetDailyStatusAsync(cashuuid);
        var currentDailyClosing = status.numberClosure;
        // process left over receipts
        var dailyClosing = await _client.InsertZDocumentAsync(cashuuid, receiptRequest.cbReceiptMoment, int.Parse(currentDailyClosing), amount);
        var beforeStatus = await _client.GetDailyStatusAsync(cashuuid);
        // TODO should we really check the status? 
        var dailyOpen = await _client.GetDailyOpenAsync(cashuuid, receiptRequest.cbReceiptMoment);
        _cashToken = dailyOpen.cashToken;
        _cashHmacKey = dailyOpen.cashHmacKey;
        receiptResponse.ftSignatures = SignatureFactory.CreateDailyClosingReceiptSignatures(long.Parse(currentDailyClosing));
        /*
        insertZDocument(cashuuid, znum=QueueITDailyCloingCounter, …) 

    getDailyStatus(cashuuid) => check for Stato cassa == Chiusa 

    getDailyOpen(cashuuid, …) 

    store cashToken in QueueIT 

    store cashHmacKey in QueueIT 

    QueueDailyClosingCounter++ 
        */
        return receiptResponse;
    }

    protected static NumberFormatInfo CurrencyFormatter = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "",
        CurrencyDecimalDigits = 2
    };

    private long GetMappedReceitCase(long legacyReceiptcase)
    {
        var value = legacyReceiptcase switch
        {
            0x002 => ITReceiptCases.ZeroReceipt0x200,
            0x003 => ITReceiptCases.InitialOperationReceipt0xF01,
            0x004 => ITReceiptCases.OutOfOperationReceipt0xF02,
            0x005 => ITReceiptCases.MonthlyClosing0x213,
            0x006 => ITReceiptCases.YearlyClosing0x214,
            0x007 => ITReceiptCases.DailyClosing0x212,
            0x000 => ITReceiptCases.UnknownReceipt0x00,
            0x001 => ITReceiptCases.POSReceipt0x001,
            _ => ITReceiptCases.UnknownReceipt0x00
        };
        return (long) value;
    }
}
