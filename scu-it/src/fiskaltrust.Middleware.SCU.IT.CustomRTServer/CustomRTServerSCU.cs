using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using System.Linq;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer.Models;
using System.IO;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public sealed class CustomRTServerSCU : LegacySCU
{
    private readonly Guid _id;
#pragma warning disable IDE0052 // Remove unread private members
    private readonly ILogger<CustomRTServerSCU> _logger;
#pragma warning restore IDE0052 // Remove unread private members
    private readonly CustomRTServerClient _client;
    private readonly CustomRTServerCommunicationQueue _customRTServerCommunicationQueue;
    private readonly AccountMasterData? _accountMasterData;
    private Dictionary<Guid, QueueIdentification> CashUUIdMappings = new Dictionary<Guid, QueueIdentification>();
    private readonly string _scuCacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

    private string _stateCacheFilePath => Path.Combine(_scuCacheFolder, $"{_id}_customrtserver_statecache.json");

    public CustomRTServerSCU(Guid id, ILogger<CustomRTServerSCU> logger, CustomRTServerConfiguration configuration, CustomRTServerClient client, CustomRTServerCommunicationQueue customRTServerCommunicationQueue)
    {
        _id = id;
        _logger = logger;
        _client = client;
        _customRTServerCommunicationQueue = customRTServerCommunicationQueue;
        if (!string.IsNullOrEmpty(configuration.AccountMasterData))
        {
            _accountMasterData = JsonConvert.DeserializeObject<AccountMasterData>(configuration.AccountMasterData);
        }


        if (!string.IsNullOrEmpty(configuration.ServiceFolder))
        {
            _scuCacheFolder = configuration.ServiceFolder!;
        }
        if (string.IsNullOrEmpty(_scuCacheFolder))
        {
            _scuCacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }
    }

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

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
            (var signatures, var stateData, var state) = await PerformZeroReceiptOperationAsync(request.ReceiptRequest, request.ReceiptResponse, request.ReceiptResponse.ftCashBoxIdentification);
            request.ReceiptResponse.ftState = state;
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
            (var signatures, var state) = await PerformDailyCosingAsync(request.ReceiptRequest, request.ReceiptResponse, cashuuid);
            request.ReceiptResponse.ftState = state;
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
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
        _ = await _client.InsertCashRegisterAsync(receiptResponse.ftQueueID, shop, name, _accountMasterData?.AccountId.ToString() ?? "", _accountMasterData?.VatId ?? _accountMasterData?.TaxId ?? "");
        await ReloadCashUUID(receiptResponse);
        var cashuuid = CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)];
        if (cashuuid.CashStatus == "0")
        {
            await OpenNewdayAsync(receiptRequest, receiptResponse);
        }
        return SignatureFactory.CreateInitialOperationSignatures().ToList();
    }

    private async Task<List<SignaturItem>> PerformOutOfOperationAsync(ReceiptResponse receiptResponse)
    {
        _ = await _client.CancelCashRegisterAsync(receiptResponse.ftCashBoxIdentification, _accountMasterData?.VatId ?? "");
        return SignatureFactory.CreateOutOfOperationSignatures().ToList();
    }

    private async Task OpenNewdayAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        var dailyOpen = await _client.GetDailyOpenAsync(receiptResponse.ftCashBoxIdentification, receiptRequest.cbReceiptMoment);
        UpdateCashUUIDMappingsWithDay(receiptResponse, dailyOpen);
    }

    private async Task<(List<SignaturItem> signaturItems, string? ftStateData, long ftState)> PerformZeroReceiptOperationAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, string cashUuid)
    {
        try
        {
            var result = await _client.GetDailyStatusAsync(cashUuid);
            if (result.cashStatus == "0")
            {
                await OpenNewdayAsync(receiptRequest, receiptResponse);
                // TODO let's check if we really should auto open a day
            }
            var resultMemStatus = await _client.GetDeviceMemStatusAsync();
            var signatures = SignatureFactory.CreateZeroReceiptSignatures().ToList();
            var stateData = JsonConvert.SerializeObject(new
            {
                DeviceMemStatus = resultMemStatus,
                DeviceDailyStatus = result,
                DocumentsInCache = _customRTServerCommunicationQueue.GetCountOfDocumentsForInCache(cashUuid),
                SigningDeviceAvailable = true
            });
            return (signatures, stateData, 0x4954_2000_0000_0000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Faild to call RT Server");
            var stateData = JsonConvert.SerializeObject(new
            {
                DocumentsInCache = _customRTServerCommunicationQueue.GetCountOfDocumentsForInCache(cashUuid),
                SigningDeviceAvailable = false
            });
            return (new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "rt-server-zeroreceipt-warning",
                    Data = $"{ex}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954_2000_0000_2000
                },
                new SignaturItem
                {
                    Caption = "rt-server-zeroreceipt-cached-documents",
                    Data = $"{_customRTServerCommunicationQueue.GetCountOfDocumentsForInCache(cashUuid)}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954_2000_0000_2000
                }
            }, stateData, 0x4954_2001_0000_0000);
        }
    }

    private async Task ReloadCashUUID(ReceiptResponse receiptResponse)
    {
        if (File.Exists(_stateCacheFilePath))
        {
            CashUUIdMappings = JsonConvert.DeserializeObject<Dictionary<Guid, QueueIdentification>>(File.ReadAllText(_stateCacheFilePath));
        }
        if (CashUUIdMappings.ContainsKey(Guid.Parse(receiptResponse.ftQueueID)))
        {
            return;
        }

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
        File.WriteAllText(_stateCacheFilePath, JsonConvert.SerializeObject(CashUUIdMappings));
    }

    private void UpdateCashUUIDMappingsWithDay(ReceiptResponse receiptResponse, GetDailyOpenResponse dailyOpen)
    {
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)] = new QueueIdentification
        {
            RTServerSerialNumber = dailyOpen.fiscalBoxId,
            CashHmacKey = dailyOpen.cashHmacKey,
            LastZNumber = int.Parse(dailyOpen.numberClosure),
            LastDocNumber = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.cashLastDocNumber) ? "0" : dailyOpen.cashLastDocNumber),
            CashUuId = receiptResponse.ftCashBoxIdentification,
            LastSignature = dailyOpen.cashToken,
            CurrentGrandTotal = int.Parse(string.IsNullOrWhiteSpace(dailyOpen.grandTotalDB) ? "0" : dailyOpen.grandTotalDB),
            CashStatus = dailyOpen.cashStatus
        };
        File.WriteAllText(_stateCacheFilePath, JsonConvert.SerializeObject(CashUUIdMappings));
    }

    private void UpdatedCashUUID(ReceiptResponse receiptResponse, DocumentData document, QrCodeData qrCodeData)
    {
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].LastDocNumber = document.docnumber;
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].LastSignature = qrCodeData.signature;
        CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].CurrentGrandTotal = CashUUIdMappings[Guid.Parse(receiptResponse.ftQueueID)].CurrentGrandTotal + document.amount;
        File.WriteAllText(_stateCacheFilePath, JsonConvert.SerializeObject(CashUUIdMappings));
    }

    private async Task<List<SignaturItem>> ProcessFiscalDocumentAsync(ReceiptResponse receiptResponse, QueueIdentification cashuuid, CommercialDocument commercialDocument, FDocument fiscalDocument)
    {
        await _customRTServerCommunicationQueue.EnqueueDocument(receiptResponse.ftCashBoxIdentification, commercialDocument, fiscalDocument.document.docznumber, fiscalDocument.document.docnumber);
        UpdatedCashUUID(receiptResponse, fiscalDocument.document, commercialDocument.qrData);
        var docType = "";
        if (fiscalDocument.document.doctype == 5)
        {
            docType = "VOID";
        }
        else if (fiscalDocument.document.doctype == 3)
        {
            docType = "REFUND";
        }
        else if (fiscalDocument.document.doctype == 1)
        {
            docType = "POSRECEIPT";
        }
        var data = new POSReceiptSignatureData
        {
            RTSerialNumber = cashuuid.RTServerSerialNumber,
            RTZNumber = fiscalDocument.document.docznumber,
            RTDocNumber = fiscalDocument.document.docnumber,
            RTDocMoment = DateTime.Parse(fiscalDocument.document.dtime),
            RTDocType = docType,
            RTServerSHAMetadata = commercialDocument.qrData.shaMetadata,
            RTCodiceLotteria = "",
            RTCustomerID = "",
            RTReferenceZNumber = fiscalDocument.document.referenceClosurenumber,
            RTReferenceDocNumber = fiscalDocument.document.referenceDocnumber,
            RTReferenceDocMoment = string.IsNullOrEmpty(fiscalDocument.document.referenceDtime) ? null : DateTime.Parse(fiscalDocument.document.referenceDtime)
        };
        return SignatureFactory.CreateDocumentoCommercialeSignatures(data);
    }

    private async Task<(List<SignaturItem>, long ftState)> PerformDailyCosingAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, QueueIdentification cashuuid)
    {
        var warnings = new List<string>();

        GetDailyStatusResponse? status;
        try
        {
            status = await _client.GetDailyStatusAsync(receiptResponse.ftCashBoxIdentification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Faild to call RT Server");
            return (new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "rt-server-dailyclosing-error",
                    Data = $"{ex}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954_2000_0000_3000
                },
                new SignaturItem
                {
                    Caption = "rt-server-dailyclosing-cached-documents",
                    Data = $"{_customRTServerCommunicationQueue.GetCountOfDocumentsForInCache(receiptResponse.ftCashBoxIdentification)}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = 0x4954_2000_0000_1000
                }
            }, 0x4954_2001_EEEE_EEEE);
        }

        await _customRTServerCommunicationQueue.ProcessAllReceipts(receiptResponse.ftCashBoxIdentification);

        var currentZNumber = int.Parse(status.numberClosure) + 1;
        // process left over receipts
        var dailyClosingResponse = await _client.InsertZDocumentAsync(cashuuid.CashUuId, receiptRequest.cbReceiptMoment, currentZNumber, status.grandTotalDB);
        await OpenNewdayAsync(receiptRequest, receiptResponse);
        warnings.AddRange(dailyClosingResponse.responseSubCode);
        var signatures = SignatureFactory.CreateDailyClosingReceiptSignatures(currentZNumber).ToList();
        if (warnings.Count > 0)
        {
            signatures.Add(new SignaturItem
            {
                Caption = "rt-server-dailyclosing-warning",
                Data = $"[{string.Join(",", warnings.ToArray())}]",
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = 0x4954_2000_0000_2000
            });
        }
        return (signatures, 0x4954_2000_0000_0000);
    }
}
