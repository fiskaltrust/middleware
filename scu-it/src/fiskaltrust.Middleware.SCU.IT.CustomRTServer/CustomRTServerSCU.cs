using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using System.Linq;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public sealed class CustomRTServerSCU : LegacySCU
{
#pragma warning disable IDE0052 // Remove unread private members
    private readonly ILogger<CustomRTServerSCU> _logger;
#pragma warning restore IDE0052 // Remove unread private members
    private readonly CustomRTServerClient _client;
    private readonly CustomRTServerCommunicationQueue _customRTServerCommunicationQueue;
    private readonly AccountMasterData? _accountMasterData;
    private readonly Dictionary<Guid, QueueIdentification> CashUUIdMappings = new Dictionary<Guid, QueueIdentification>();


    public CustomRTServerSCU(ILogger<CustomRTServerSCU> logger, CustomRTServerConfiguration configuration, CustomRTServerClient client, CustomRTServerCommunicationQueue customRTServerCommunicationQueue)
    {
        _logger = logger;
        _client = client;
        _customRTServerCommunicationQueue = customRTServerCommunicationQueue;
        if (!string.IsNullOrEmpty(configuration.AccountMasterData))
        {
            _accountMasterData = JsonConvert.DeserializeObject<AccountMasterData>(configuration.AccountMasterData);
        }
    }

    public override async Task<RTInfo> GetRTInfoAsync()
    {
        var result = await _client.GetDailyStatusArrayAsync();
        return new RTInfo
        {
            SerialNumber = result.ArrayResponse.FirstOrDefault()?.fiscalBoxId,
            InfoData = JsonConvert.SerializeObject(result.ArrayResponse)
        };
    }

    public override async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var receiptCase = request.ReceiptRequest.GetReceiptCase();
        if (request.ReceiptRequest.IsInitialOperationReceipt())
        {
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, await PerformInitOperationAsync(request.ReceiptRequest, request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsOutOfOperationReceipt())
        {
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, await PerformOutOfOperationAsync(request.ReceiptResponse));
        }

        if (request.ReceiptRequest.IsZeroReceipt())
        {
            (var signatures, var stateData) = await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse, request.ReceiptResponse.ftCashBoxIdentification);
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, stateData, signatures);
        }

        if (!CashUUIdMappings.ContainsKey(Guid.Parse(request.ReceiptResponse.ftQueueID)))
        {
            await ReloadCashUUID(request.ReceiptResponse);
        }

        var cashuuid = CashUUIdMappings[Guid.Parse(request.ReceiptResponse.ftQueueID)];

        if (request.ReceiptRequest.IsVoid())
        {
            (var commercialDocument, var fiscalDocument) = CustomRTServerMapping.CreateAnnuloDocument(request.ReceiptRequest, cashuuid, request.ReceiptResponse);
            var signatures = await ProcessFiscalDocumentAsync(request.ReceiptResponse, cashuuid, commercialDocument, fiscalDocument);
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
        }

        if (request.ReceiptRequest.IsRefund())
        {
            (var commercialDocument, var fiscalDocument) = CustomRTServerMapping.CreateResoDocument(request.ReceiptRequest, cashuuid, request.ReceiptResponse);
            var signatures = await ProcessFiscalDocumentAsync(request.ReceiptResponse, cashuuid, commercialDocument, fiscalDocument);
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
        }

        if (request.ReceiptRequest.IsDailyClosing())
        {
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, await PerformDailyCosingAsync(request.ReceiptRequest, request.ReceiptResponse, cashuuid));
        }

        switch (receiptCase)
        {
            case (long) ITReceiptCases.UnknownReceipt0x0000:
            case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
            case (long) ITReceiptCases.PaymentTransfer0x0002:
            case (long) ITReceiptCases.Protocol0x0005:
                (var commercialDocument, var fiscalDocument) = CustomRTServerMapping.GenerateFiscalDocument(request.ReceiptRequest, cashuuid);
                var signatures = await ProcessFiscalDocumentAsync(request.ReceiptResponse, cashuuid, commercialDocument, fiscalDocument);
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
        }

        throw new Exception($"The given receiptcase 0x{receiptCase.ToString("X")} is not supported.");
    }

    private async Task<List<SignaturItem>> PerformInitOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var shop = receiptResponse.ftCashBoxIdentification.Substring(0, 4);
        var name = receiptResponse.ftCashBoxIdentification.Substring(4, 4);
        var result = await _client.InsertCashRegisterAsync(receiptResponse.ftQueueID, shop, name, _accountMasterData?.AccountId.ToString() ?? "", _accountMasterData?.VatId ?? _accountMasterData?.TaxId ?? "");
        await ReloadCashUUID(receiptResponse);
        var cashuuid = CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)];
        if (cashuuid.CashStatus == "0")
        {
            await OpenNewdayAsync(receiptRequest, receiptResponse, cashuuid.CashUuId);
        }
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

    private async Task<List<SignaturItem>> PerformOutOfOperationAsync(ReceiptResponse receiptResponse)
    {
        _ = await _client.CancelCashRegisterAsync(receiptResponse.ftCashBoxIdentification, _accountMasterData?.VatId ?? "");
        var signatures = SignatureFactory.CreateOutOfOperationSignatures().ToList();
        signatures.Add(new SignaturItem
        {
            Caption = "<customrtserver-cashuuid>",
            Data = receiptResponse.ftCashBoxIdentification,
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.CustomRTServerInfo
        });
        return signatures;
    }

    private async Task OpenNewdayAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string cashUuid)
    {
        var dailyOpen = await _client.GetDailyOpenAsync(cashUuid, receiptRequest.cbReceiptMoment);
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)] = new QueueIdentification
        {
            RTServerSerialNumber = dailyOpen.fiscalBoxId,
            CashHmacKey = dailyOpen.cashHmacKey,
            LastZNumber = int.Parse(dailyOpen.numberClosure),
            LastDocNumber = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.cashLastDocNumber) ? "0" : dailyOpen.cashLastDocNumber),
            CashUuId = cashUuid,
            LastSignature = dailyOpen.cashToken,
            CurrentGrandTotal = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.grandTotalDB) ? "0" : dailyOpen.grandTotalDB),
        };
    }

    private async Task<(List<SignaturItem> signaturItems, string ftStateData)> PerformZeroReceiptOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string cashUuid)
    {
        var result = await _client.GetDailyStatusAsync(cashUuid);
        if (result.cashStatus == "0")
        {
            await OpenNewdayAsync(receiptRequest, receiptResponse, cashUuid);
            // TODO let's check if we really should auto open a day
        }
        var resultMemStatus = await _client.GetDeviceMemStatusAsync();
        var signatures = SignatureFactory.CreateZeroReceiptSignatures().ToList();
        var stateData = JsonConvert.SerializeObject(new
        {
            DeviceMemStatus = resultMemStatus,
            DeviceDailyStatus = result
        });
        return (signatures, stateData);
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
            CurrentGrandTotal = int.Parse(dailyOpen.grandTotalDB),
            CashStatus = dailyOpen.cashStatus
        };
    }

    private void UpdatedCashUUID(ReceiptResponse receiptResponse, DocumentData document, QrCodeData qrCodeData)
    {
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].LastDocNumber = document.docnumber;
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].LastSignature = qrCodeData.signature;
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].CurrentGrandTotal = CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].CurrentGrandTotal + document.amount;
    }

    private async Task<List<SignaturItem>> ProcessFiscalDocumentAsync(ReceiptResponse receiptResponse, QueueIdentification cashuuid, CommercialDocument commercialDocument, FDocument fiscalDocument)
    {
        await _customRTServerCommunicationQueue.EnqueueDocument(commercialDocument);
        UpdatedCashUUID(receiptResponse, fiscalDocument.document, commercialDocument.qrData);
        return RTServerSignaturFactory.CreateDocumentoCommercialeSignatures(fiscalDocument.document, commercialDocument, cashuuid.RTServerSerialNumber).ToList();
    }

    private async Task<List<SignaturItem>> PerformDailyCosingAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification cashuuid)
    {
        await _customRTServerCommunicationQueue.ProcessAllReceipts();
        
        var status = await _client.GetDailyStatusAsync(cashuuid.CashUuId);
        var currentDailyClosing = status.numberClosure;
        // process left over receipts
        //var dailyClosing = await _client.InsertZDocumentAsync(cashuuid.CashUuId, receiptRequest.cbReceiptMoment, int.Parse(currentDailyClosing) + 1, status.grandTotalDB);
        //var beforeStatus = await _client.GetDailyStatusAsync(cashuuid.CashUuId);
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
            CashStatus = dailyOpen.cashStatus
        };
        return SignatureFactory.CreateDailyClosingReceiptSignatures(long.Parse(currentDailyClosing)).ToList();
    }
}
