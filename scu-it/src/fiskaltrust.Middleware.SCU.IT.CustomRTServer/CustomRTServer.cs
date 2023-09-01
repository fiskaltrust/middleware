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
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

#pragma warning disable
#nullable enable
public sealed class CustomRTServer : IITSSCD
{
    private readonly ILogger<CustomRTServer> _logger;
    private readonly CustomRTServerConfiguration _configuration;
    private readonly CustomRTServerClient _client;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

    private Dictionary<Guid, QueueIdentification> CashUUIdMappings = new Dictionary<Guid, QueueIdentification>();

    private List<CommercialDocument> _receiptQueue = new List<CommercialDocument>();

    private List<ITReceiptCases> _nonProcessingCases = new List<ITReceiptCases>
        {
            ITReceiptCases.PointOfSaleReceiptWithoutObligation0x0003,
            ITReceiptCases.ECommerce0x0004,
            ITReceiptCases.InvoiceUnknown0x1000,
            ITReceiptCases.InvoiceB2C0x1001,
            ITReceiptCases.InvoiceB2B0x1002,
            ITReceiptCases.InvoiceB2G0x1003,
            ITReceiptCases.ZeroReceipt0x200,
            ITReceiptCases.OneReceipt0x2001,
            ITReceiptCases.ShiftClosing0x2010,
            ITReceiptCases.MonthlyClosing0x2012,
            ITReceiptCases.YearlyClosing0x2013,
            ITReceiptCases.ProtocolUnspecified0x3000,
            ITReceiptCases.ProtocolTechnicalEvent0x3001,
            ITReceiptCases.ProtocolAccountingEvent0x3002,
            ITReceiptCases.InternalUsageMaterialConsumption0x3003,
            ITReceiptCases.InitSCUSwitch0x4011,
            ITReceiptCases.FinishSCUSwitch0x4012,
        };

    public CustomRTServer(ILogger<CustomRTServer> logger, CustomRTServerConfiguration configuration, CustomRTServerClient client)
    {
        _logger = logger;
        _configuration = configuration;
        _client = client;
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
        if (!request.ReceiptRequest.IsV2Receipt())
        {
            receiptCase = ITConstants.ConvertToV2Case(receiptCase);
        }

        if (request.ReceiptRequest.IsInitialOperationReceipt())
        {
            return CreateResponse(await PerformInitOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsOutOfOperationReceipt())
        {
            return CreateResponse(await PerformOutOfOperationAsync(request.ReceiptRequest, request.ReceiptResponse, request.ReceiptResponse.ftCashBoxIdentification));
        }

        if (request.ReceiptRequest.IsZeroReceipt())
        {
            return CreateResponse(await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse, request.ReceiptResponse.ftCashBoxIdentification));
        }

        if (IsNoActionCase(request.ReceiptRequest))
        {
            return CreateResponse(request.ReceiptResponse);
        }

        if (CashUUIdMappings.ContainsKey(Guid.Parse(request.ReceiptResponse.ftQueueID)))
        {
            await ReloadCashUUID(request.ReceiptResponse);
        }

        var cashuuid = CashUUIdMappings[Guid.Parse(request.ReceiptResponse.ftQueueID)];

        if (request.ReceiptRequest.IsVoid())
        {
            return await ProcessVoidReceipt(request, cashuuid);
        }

        if (request.ReceiptRequest.IsDailyClosing())
        {
            return CreateResponse(await PerformDailyCosing(request.ReceiptRequest, request.ReceiptResponse, cashuuid));
        }

        switch (receiptCase)
        {
            case (long) ITReceiptCases.UnknownReceipt0x0000:
            case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
            case (long) ITReceiptCases.PaymentTransfer0x0002:
            case (long) ITReceiptCases.Protocol0x0005:
            default:
                return CreateResponse(await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse, cashuuid));
        }
    }

    private async Task<ReceiptResponse> PerformInitOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var shop = receiptResponse.ftCashBoxIdentification.Substring(0, 4);
        var name = receiptResponse.ftCashBoxIdentification.Substring(4, 4);
        var result = await _client.InsertCashRegisterAsync(receiptResponse.ftQueueID, shop, name, _configuration.AccountMasterData.AccountId.ToString(), _configuration.AccountMasterData.VatId ?? _configuration.AccountMasterData.TaxId);
        receiptResponse.ftSignatures = SignatureFactory.CreateInitialOperationSignatures();
        return receiptResponse;
    }

    private async Task<ReceiptResponse> PerformZeroReceiptOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string cashUuid)
    {
     //   var result = await _client.GetDailyStatusAsync(cashUuid);
        var resultMemStatus = await _client.GetDeviceMemStatusAsync();
        receiptResponse.ftSignatures = SignatureFactory.CreateOutOfOperationSignatures();
        receiptResponse.ftStateData = JsonConvert.SerializeObject(new
        {
            DeviceMemStatus = resultMemStatus,
           // DeviceDailyStatus = result
        });
        return receiptResponse;
    }

    private async Task<ReceiptResponse> PerformOutOfOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string cashUuid)
    {
        var result = await _client.CancelCashRegisterAsync(cashUuid, _configuration.AccountMasterData.VatId);
        receiptResponse.ftSignatures = SignatureFactory.CreateOutOfOperationSignatures();
        return receiptResponse;
    }

    private async Task ReloadCashUUID(ReceiptResponse receiptResponse)
    {
        var dailyOpen = await _client.GetDailyStatusAsync(receiptResponse.ftCashBoxIdentification);
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)] = new QueueIdentification
        {
            CashHmacKey = dailyOpen.cashHmacKey,
            CurrentZNumber = int.Parse(dailyOpen.numberClosure),
            CashUuId = receiptResponse.ftCashBoxIdentification,
            CashToken = dailyOpen.cashToken,
            LastSignature = dailyOpen.cashToken
        };
    }

    private async Task<ProcessResponse> ProcessVoidReceipt(ProcessRequest request, QueueIdentification cashuuid)
    {
        return new ProcessResponse
        {
            ReceiptResponse = await PerformRefundReceiptAsync(request.ReceiptRequest, request.ReceiptResponse, cashuuid)
        };
    }

    private async Task<ReceiptResponse> PerformRefundReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification cashuuid)
    {
        var commercialDocument = CustomRTServerMapping.CreateResoDocument(receiptRequest, receiptResponse, cashuuid);
        _receiptQueue.Add(commercialDocument);
        receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(commercialDocument.fiscalData.document.docnumber, commercialDocument.fiscalData.document.docznumber, receiptRequest.cbReceiptMoment);
        return receiptResponse;
    }

    private async Task<ReceiptResponse> PerformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification cashuuid)
    {
        var commercialDocument = CustomRTServerMapping.GenerateFiscalDocument(receiptRequest, receiptResponse, cashuuid);
        _receiptQueue.Add(commercialDocument);
        receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(commercialDocument.fiscalData.document.docnumber, commercialDocument.fiscalData.document.docznumber, receiptRequest.cbReceiptMoment);
        return receiptResponse;
    }

    private async Task<ReceiptResponse> PerformDailyCosing(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification cashuuid)
    {
        var status = await _client.GetDailyStatusAsync(cashuuid.CashUuId);
        var currentDailyClosing = status.numberClosure;
        // process left over receipts
        var dailyClosing = await _client.InsertZDocumentAsync(cashuuid.CashUuId, receiptRequest.cbReceiptMoment, int.Parse(currentDailyClosing), status.grandTotalDB);
        var beforeStatus = await _client.GetDailyStatusAsync(cashuuid.CashUuId);
        // TODO should we really check the status? 
        var dailyOpen = await _client.GetDailyOpenAsync(cashuuid.CashUuId, receiptRequest.cbReceiptMoment);
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)] = new QueueIdentification
        {
            CashHmacKey = dailyOpen.cashHmacKey,
            CurrentZNumber = int.Parse(dailyOpen.numberClosure),
            CashUuId = cashuuid.CashUuId,
            CashToken = dailyOpen.cashToken,
            LastSignature = dailyOpen.cashToken
        };
        receiptResponse.ftSignatures = SignatureFactory.CreateDailyClosingReceiptSignatures(long.Parse(currentDailyClosing));
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