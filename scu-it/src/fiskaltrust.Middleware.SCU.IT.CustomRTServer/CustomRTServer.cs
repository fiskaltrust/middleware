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
        var result = await _client.GetDailyStatusArrayAsync();
        return new RTInfo
        {
            SerialNumber = result.ArrayResponse.FirstOrDefault()?.fiscalBoxId,
            InfoData = JsonConvert.SerializeObject(result.ArrayResponse)
        };
    }

    public bool IsNoActionCase(ReceiptRequest request)
    {
        return _nonProcessingCases.Select(x => (long) x).Contains(request.GetReceiptCase());
    }

    private static ProcessResponse CreateResponse(ReceiptResponse response, string stateData, List<SignaturItem> signaturItems)
    {
        response.ftSignatures = signaturItems.ToArray();
        response.ftStateData = stateData;
        return new ProcessResponse
        {
            ReceiptResponse = response
        };
    }

    private static ProcessResponse CreateResponse(ReceiptResponse response, List<SignaturItem> signaturItems)
    {
        response.ftSignatures = signaturItems.ToArray();
        return new ProcessResponse
        {
            ReceiptResponse = response
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
            return CreateResponse(request.ReceiptResponse, await PerformInitOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsOutOfOperationReceipt())
        {
            return CreateResponse(request.ReceiptResponse, await PerformOutOfOperationAsync(request.ReceiptRequest, request.ReceiptResponse, request.ReceiptResponse.ftCashBoxIdentification));
        }

        if (request.ReceiptRequest.IsZeroReceipt())
        {
            (var signatures, var stateData) = await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse, request.ReceiptResponse.ftCashBoxIdentification);
            return CreateResponse(request.ReceiptResponse, stateData, signatures);
        }

        if (IsNoActionCase(request.ReceiptRequest))
        {
            return CreateResponse(request.ReceiptResponse, new List<SignaturItem>());
        }

        if (!CashUUIdMappings.ContainsKey(Guid.Parse(request.ReceiptResponse.ftQueueID)))
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
            return CreateResponse(request.ReceiptResponse, await PerformDailyCosing(request.ReceiptRequest, request.ReceiptResponse, cashuuid));
        }

        switch (receiptCase)
        {
            case (long) ITReceiptCases.UnknownReceipt0x0000:
            case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
            case (long) ITReceiptCases.PaymentTransfer0x0002:
            case (long) ITReceiptCases.Protocol0x0005:
            default:
                return CreateResponse(request.ReceiptResponse, await PerformClassicReceiptAsync(request.ReceiptRequest, request.ReceiptResponse, cashuuid));
        }
    }

    private async Task<List<SignaturItem>> PerformInitOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var shop = receiptResponse.ftCashBoxIdentification.Substring(0, 4);
        var name = receiptResponse.ftCashBoxIdentification.Substring(4, 4);
        var result = await _client.InsertCashRegisterAsync(receiptResponse.ftQueueID, shop, name, _configuration.AccountMasterData.AccountId.ToString(), _configuration.AccountMasterData.VatId ?? _configuration.AccountMasterData.TaxId);
        var dailyOpen = await _client.GetDailyOpenAsync(result.cashUuid, receiptRequest.cbReceiptMoment);
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)] = new QueueIdentification
        {
            RTServerSerialNumber = dailyOpen.fiscalBoxId,
            CashHmacKey = dailyOpen.cashHmacKey,
            LastZNumber = int.Parse(dailyOpen.numberClosure),
            LastDocNumber = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.cashLastDocNumber) ? "0" : dailyOpen.cashLastDocNumber),
            CashUuId = result.cashUuid,
            LastSignature = dailyOpen.cashToken,
            CurrentGrandTotal = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.grandTotalDB) ? "0" : dailyOpen.grandTotalDB),
        };
        var signatures = SignatureFactory.CreateInitialOperationSignatures().ToList();
        signatures.Add(new SignaturItem
        {
            Caption = "<customrtserver-cashuuid>",
            Data = result.cashUuid,
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.CustomRTServerInfo
        });
        return signatures;
    }

    private async Task<(List<SignaturItem> signaturItems, string ftStateData)> PerformZeroReceiptOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string cashUuid)
    {
        var result = await _client.GetDailyStatusAsync(cashUuid);
        var resultMemStatus = await _client.GetDeviceMemStatusAsync();
        var signatures = SignatureFactory.CreateZeroReceiptSignatures().ToList();
        var stateData = JsonConvert.SerializeObject(new
        {
            DeviceMemStatus = resultMemStatus,
            DeviceDailyStatus = result
        });
        return (signatures, stateData);
    }

    private async Task<List<SignaturItem>> PerformOutOfOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string cashUuid)
    {
        var result = await _client.CancelCashRegisterAsync(cashUuid, _configuration.AccountMasterData.VatId);
        var signatures = SignatureFactory.CreateOutOfOperationSignatures().ToList();
        signatures.Add(new SignaturItem
        {
            Caption = "<customrtserver-cashuuid>",
            Data = cashUuid,
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.CustomRTServerInfo
        });
        return signatures;
    }

    private async Task ReloadCashUUID(ReceiptResponse receiptResponse)
    {
        var dailyOpen = await _client.GetDailyStatusAsync(receiptResponse.ftCashBoxIdentification);
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)] = new QueueIdentification
        {
            RTServerSerialNumber = dailyOpen.fiscalBoxId,
            CashHmacKey = dailyOpen.cashHmacKey,
            LastZNumber = int.Parse(dailyOpen.numberClosure),
            LastDocNumber = int.Parse(dailyOpen.cashLastDocNumber),
            CashUuId = receiptResponse.ftCashBoxIdentification,
            LastSignature = dailyOpen.cashToken,
            CurrentGrandTotal = int.Parse(dailyOpen.grandTotalDB)
        };
    }

    private async Task UpdatedCashUUID(ReceiptResponse receiptResponse, FDocument fDocument, QrCodeData qrCodeData)
    {
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].LastDocNumber = fDocument.document.docnumber;
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].LastSignature = qrCodeData.signature;
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].CurrentGrandTotal = CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].CurrentGrandTotal +  fDocument.document.amount;
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
        (var commercialDocument, var fiscalDocument) = CustomRTServerMapping.CreateResoDocument(receiptRequest, cashuuid);
        _receiptQueue.Add(commercialDocument);
        receiptResponse.ftSignatures = SignatureFactory.CreatePosReceiptSignatures(fiscalDocument.document.docnumber, fiscalDocument.document.docznumber, receiptRequest.cbReceiptMoment);
        return receiptResponse;
    }

    private async Task<List<SignaturItem>> PerformClassicReceiptAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification cashuuid)
    {
        (var commercialDocument, var fiscalDocument) = CustomRTServerMapping.GenerateFiscalDocument(receiptRequest, cashuuid);
        // Todo queue mechanism
        Task.Run(() => _client.InsertFiscalDocumentAsync(commercialDocument)).ContinueWith(x =>
        {
            if (x.IsFaulted)
            {
                _logger.LogError("Failed to insert fiscal document", x.Exception);
            }
            else
            {
                _logger.LogInformation("Transmitted commercial document with sha {shametadata} DOC {znumber}-{docnumber}.", commercialDocument.qrData.shaMetadata, fiscalDocument.document.docznumber.ToString().PadLeft(4, '0'), fiscalDocument.document.docnumber.ToString().PadLeft(4, '0'));
            }
        });
        await UpdatedCashUUID(receiptResponse, fiscalDocument, commercialDocument.qrData);
        var signatures = CreatePosReceiptCustomRTServerSignatures(fiscalDocument.document.docnumber, fiscalDocument.document.docznumber, commercialDocument.qrData.shaMetadata, cashuuid).ToList();
        return signatures;
    }

    public static SignaturItem[] CreatePosReceiptCustomRTServerSignatures(long receiptNumber, long zRepNumber, string shaMetadata, QueueIdentification cashuuid)
    {
        return new SignaturItem[]
        {
            new SignaturItem
            {
                Caption = "<z-number>",
                Data = zRepNumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.RTZNumber
            },
            new SignaturItem
            {
                Caption = "<receipt-number>",
                Data = receiptNumber.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 |(long) SignatureTypesIT.RTDocumentNumber
            },
            new SignaturItem
            {
                Caption = "<rt-serialnumber>",
                Data = cashuuid.RTServerSerialNumber,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 |(long) SignatureTypesIT.RTSerialNumber
            },
            new SignaturItem
            {
                Caption = "<rt-server-shametadata>",
                Data = shaMetadata,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954000000000000 |(long) SignatureTypesIT.CustomRTServerShaMetadata
            }
        };
    }

    private async Task<List<SignaturItem>> PerformDailyCosing(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification cashuuid)
    {
        var status = await _client.GetDailyStatusAsync(cashuuid.CashUuId);
        var currentDailyClosing = status.numberClosure;
        // process left over receipts
        var dailyClosing = await _client.InsertZDocumentAsync(cashuuid.CashUuId, receiptRequest.cbReceiptMoment, int.Parse(currentDailyClosing) + 1, status.grandTotalDB);
        var beforeStatus = await _client.GetDailyStatusAsync(cashuuid.CashUuId);
        // TODO should we really check the status? 
        var dailyOpen = await _client.GetDailyOpenAsync(cashuuid.CashUuId, receiptRequest.cbReceiptMoment);
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)] = new QueueIdentification
        {
            RTServerSerialNumber = dailyOpen.fiscalBoxId,
            CashHmacKey = dailyOpen.cashHmacKey,
            LastZNumber = int.Parse(dailyOpen.numberClosure),
            LastDocNumber = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.cashLastDocNumber) ? "0" : dailyOpen.cashLastDocNumber),
            CashUuId = cashuuid.CashUuId,
            LastSignature = dailyOpen.cashToken,
            CurrentGrandTotal = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.grandTotalDB) ? "0" : dailyOpen.grandTotalDB),
        };
        var signatures = SignatureFactory.CreateDailyClosingReceiptSignatures(long.Parse(currentDailyClosing)).ToList();
        return signatures;
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