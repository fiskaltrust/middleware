using System.Collections.Generic;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

#pragma warning disable
#nullable enable
public sealed class CustomRTServer : IITSSCD
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

    private List<ITReceiptCases> _nonProcessingCases = new List<ITReceiptCases>
        {
            ITReceiptCases.ZeroReceipt0x200,
            ITReceiptCases.OneReceipt0x2001,
            ITReceiptCases.ShiftClosing0x2010,
            ITReceiptCases.MonthlyClosing0x2012,
            ITReceiptCases.YearlyClosing0x2013,
            ITReceiptCases.ProtocolUnspecified0x3000,
            ITReceiptCases.ProtocolTechnicalEvent0x3001,
            ITReceiptCases.ProtocolAccountingEvent0x3002,
            ITReceiptCases.InternalUsageMaterialConsumption0x3003,
            ITReceiptCases.InitSCUSwitch,
            ITReceiptCases.FinishSCUSwitch,
        };

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

    public async Task<RTInfo> GetRTInfoAsync()
    {
        return new RTInfo
        {
            SerialNumber = null,
            InfoData = null
        };
    }

    public bool IsNoActionCase(ReceiptRequest request)
    {
        return _nonProcessingCases.Select(x => (long) x).Contains(request.GetReceiptCase());
    }

    private static ProcessResponse CreateResponse(ReceiptResponse receiptResponse)
    {
        return new ProcessResponse
        {
            ReceiptResponse = receiptResponse
        };
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCase = request.ReceiptRequest.GetReceiptCase();
        if (request.ReceiptRequest.IsLegacyReceipt())
        {
            receiptCase = ITConstants.ConvertToV2Case(receiptCase);
        }

        if (IsNoActionCase(request.ReceiptRequest))
        {
            return CreateResponse(request.ReceiptResponse);
        }

        if (request.ReceiptRequest.IsVoid())
        {
            return await ProcessVoidReceipt(request);
        }

        if (request.ReceiptRequest.IsInitialOperationReceipt())
        {
            return CreateResponse(await PerformInitOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsOutOfOperationReceipt())
        {
            return CreateResponse(await PerformOutOfOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsDailyClosing())
        {
            return CreateResponse(await PerformDailyCosing(request.ReceiptRequest, request.ReceiptResponse));
        }

        switch (receiptCase)
        {
            case (long) ITReceiptCases.UnknownReceipt0x0000:
            case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
            case (long) ITReceiptCases.PaymentTransfer0x0002:
            case (long) ITReceiptCases.PointOfSaleReceipt0x0003:
            case (long) ITReceiptCases.ECommerce0x0004:
            case (long) ITReceiptCases.Protocol0x0005:
            case (long) ITReceiptCases.InvoiceUnknown0x1000:
            case (long) ITReceiptCases.InvoiceB2C0x1001:
            case (long) ITReceiptCases.InvoiceB2B0x1002:
            case (long) ITReceiptCases.InvoiceB2G0x1003:
            default:
                return CreateResponse(await PreformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse));
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
        receiptResponse.ftSignatures = SignatureFactory.CreateOutOfOperationSignatures();
        return receiptResponse;
    }

    private string GetCashUUID(ReceiptResponse receiptResponse)
    {
        return CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)];
    }

    private async Task<ProcessResponse> ProcessVoidReceipt(ProcessRequest request)
    {
        return new ProcessResponse
        {
            ReceiptResponse = await PerformRefundReceiptAsync(request.ReceiptRequest, request.ReceiptResponse)
        };
    }

    private async Task<ReceiptResponse> PerformRefundReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
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
        receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(fiscalDocument.document.docnumber, fiscalDocument.document.docznumber, fiscalDocument.document.amount, receiptRequest.cbReceiptMoment);
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

    #region legacy
    public Task<DeviceInfo> GetDeviceInfoAsync() => throw new NotImplementedException();
    public Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => throw new NotImplementedException();
    public Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => throw new NotImplementedException();
    public Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => throw new NotImplementedException();
    public Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => throw new NotImplementedException();
    #endregion
}
