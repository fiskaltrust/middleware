using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer;

public sealed class EpsonRTServerSCU : LegacySCU
{
    private const long StateFailed = 0x4954_2001_EEEE_EEEE;
    private const long StateOk = 0x4954_2000_0000_0000;
    private const long SignatureTypeError = 0x4954_2000_0000_3000;
    private const long SignatureTypeWarning = 0x4954_2000_0000_2000;

    private readonly Guid _id;
    private readonly ILogger<EpsonRTServerSCU> _logger;
    private readonly EpsonRTServerConfiguration _configuration;
    private readonly IEpsonRTServerClient _client;
    private readonly EpsonRTServerCommunicationQueue _queue;
    private readonly string _scuCacheFolder;

    private Dictionary<Guid, TillState> _tillStates = new();

    private string StateCacheFilePath => Path.Combine(_scuCacheFolder, $"{_id}_epsonrtserver_statecache.json");

    public EpsonRTServerSCU(Guid id, ILogger<EpsonRTServerSCU> logger, EpsonRTServerConfiguration configuration, IEpsonRTServerClient client, EpsonRTServerCommunicationQueue queue)
    {
        _id = id;
        _logger = logger;
        _configuration = configuration;
        _client = client;
        _queue = queue;

        _scuCacheFolder = string.IsNullOrEmpty(configuration.ServiceFolder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            : configuration.ServiceFolder!;
    }

    public override Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => Task.FromResult(new ScuItEchoResponse { Message = request.Message });

    public override async Task<RTInfo> GetRTInfoAsync()
    {
        try
        {
            var info = await _client.GetServerInfoAsync().ConfigureAwait(false);
            return new RTInfo
            {
                SerialNumber = info.GetAddInfo("rtSerialNumber"),
                InfoData = JsonConvert.SerializeObject(info.AddInfo)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read RT Server info.");
            return new RTInfo();
        }
    }

    public override async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        try
        {
            var receiptCase = request.ReceiptRequest.GetReceiptCase();

            if (request.ReceiptRequest.IsInitialOperationReceipt())
            {
                (var signatures, var state) = await PerformInitOperationAsync(request.ReceiptResponse);
                request.ReceiptResponse.ftState = state;
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
            }

            if (request.ReceiptRequest.IsOutOfOperationReceipt())
            {
                request.ReceiptResponse.ftState = StateOk;
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, SignatureFactory.CreateOutOfOperationSignatures().ToList());
            }

            if (request.ReceiptRequest.IsZeroReceipt())
            {
                (var signatures, var stateData, var state) = await PerformZeroReceiptOperationAsync(request.ReceiptResponse);
                request.ReceiptResponse.ftState = state;
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, stateData, signatures);
            }

            var tillState = await GetOrLoadTillStateAsync(request.ReceiptResponse);

            if (request.ReceiptRequest.IsVoid())
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, await ProcessFiscalDocumentAsync(request.ReceiptRequest, request.ReceiptResponse, tillState, 3));
            }

            if (request.ReceiptRequest.IsRefund())
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, await ProcessFiscalDocumentAsync(request.ReceiptRequest, request.ReceiptResponse, tillState, 1));
            }

            if (request.ReceiptRequest.IsDailyClosing() || request.ReceiptRequest.IsMonthlyClosing() || request.ReceiptRequest.IsYearlyClosing())
            {
                (var signatures, var state) = await PerformDailyClosingAsync(request.ReceiptResponse, tillState);
                request.ReceiptResponse.ftState = state;
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
            }

            if (receiptCase == (long) ITReceiptCases.ProtocolUnspecified0x3000)
            {
                return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, new List<SignaturItem>());
            }

            switch (receiptCase)
            {
                case (long) ITReceiptCases.UnknownReceipt0x0000:
                case (long) ITReceiptCases.PointOfSaleReceipt0x0001:
                case (long) ITReceiptCases.PaymentTransfer0x0002:
                case (long) ITReceiptCases.DeliveryNote0x0005:
                    return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, await ProcessFiscalDocumentAsync(request.ReceiptRequest, request.ReceiptResponse, tillState, 0));
            }

            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, new List<SignaturItem>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process receipt on the Epson RT Server.");
            var signatures = new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "rt-server-generic-error",
                    Data = $"{ex}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = SignatureTypeError
                }
            };
            request.ReceiptResponse.ftState = StateFailed;
            return ProcessResponseHelpers.CreateResponse(request.ReceiptResponse, signatures);
        }
    }

    private async Task<(List<SignaturItem> signatures, long ftState)> PerformInitOperationAsync(ReceiptResponse receiptResponse)
    {
        try
        {
            var tillId = receiptResponse.ftCashBoxIdentification;
            if (_configuration.AutoProgramTillMap)
            {
                await _client.CreateTillsAsync(_configuration.Username, _configuration.Password, new[] { tillId }).ConfigureAwait(false);
                await _client.RebootWebServerAsync().ConfigureAwait(false);
            }
            await InitializeTillStateAsync(receiptResponse, requestToken: true).ConfigureAwait(false);
            return (SignatureFactory.CreateInitialOperationSignatures().ToList(), StateOk);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to perform the initial-operation receipt on the Epson RT Server.");
            return (new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "rt-server-initoperation-error",
                    Data = $"{ex}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = SignatureTypeError
                }
            }, StateFailed);
        }
    }

    private async Task<(List<SignaturItem> signatures, string? stateData, long ftState)> PerformZeroReceiptOperationAsync(ReceiptResponse receiptResponse)
    {
        try
        {
            var info = await _client.GetServerInfoAsync().ConfigureAwait(false);
            var stateData = JsonConvert.SerializeObject(new
            {
                ServerInfo = info.AddInfo,
                DocumentsInCache = _queue.GetCountOfDocumentsForInCache(receiptResponse.ftCashBoxIdentification),
                SigningDeviceAvailable = true
            });
            return (SignatureFactory.CreateZeroReceiptSignatures().ToList(), stateData, StateOk);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reach the Epson RT Server during the zero receipt.");
            var stateData = JsonConvert.SerializeObject(new
            {
                DocumentsInCache = _queue.GetCountOfDocumentsForInCache(receiptResponse.ftCashBoxIdentification),
                SigningDeviceAvailable = false
            });
            return (new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "rt-server-zeroreceipt-warning",
                    Data = $"{ex}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = SignatureTypeWarning
                }
            }, stateData, 0x4954_2001_0000_0000);
        }
    }

    private async Task<List<SignaturItem>> ProcessFiscalDocumentAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, TillState tillState, int docType)
    {
        long? referenceZNumber = null;
        long? referenceDocNumber = null;
        DateTime? referenceDocMoment = null;
        string? referenceTillId = null;

        if (docType != 0)
        {
            var zNumberString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceZNumber)?.Data;
            var docNumberString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentNumber)?.Data;
            var momentString = receiptResponse.GetSignaturItem(SignatureTypesIT.RTReferenceDocumentMoment)?.Data;
            if (!string.IsNullOrEmpty(zNumberString) && !string.IsNullOrEmpty(docNumberString) && !string.IsNullOrEmpty(momentString))
            {
                referenceZNumber = long.Parse(zNumberString);
                referenceDocNumber = long.Parse(docNumberString);
                referenceDocMoment = DateTime.Parse(momentString);
                referenceTillId = tillState.TillId;
            }
            else if (string.IsNullOrEmpty(receiptRequest.cbPreviousReceiptReference))
            {
                // Unreferenced refund/void: use neutral references and the current moment.
                referenceZNumber = 0;
                referenceDocNumber = 0;
                referenceDocMoment = receiptRequest.cbReceiptMoment;
                referenceTillId = tillState.TillId;
            }
            else
            {
                throw new InvalidOperationException($"Cannot process refund/void without RT references (cbPreviousReceiptReference '{receiptRequest.cbPreviousReceiptReference}').");
            }
        }

        var document = EpsonRTServerMapping.BuildFiscalDocument(receiptRequest, tillState, docType, referenceZNumber, referenceDocNumber, referenceDocMoment, referenceTillId);

        await _queue.EnqueueDocument(tillState.TillId, document.CreateReceiptXml, document.ZNumber, document.DocNumber).ConfigureAwait(false);

        // Advance the local blockchain state (Section A of the next document = this document's CCDC).
        tillState.LastFingerPrint = document.Ccdc;
        tillState.LastDocNumber = document.DocNumber;
        if (docType == 0)
        {
            tillState.CurrentDailyAmount += document.AmountCents;
        }
        PersistState();

        var signatureData = new POSReceiptSignatureData
        {
            RTSerialNumber = tillState.RTServerSerialNumber,
            RTZNumber = document.ZNumber,
            RTDocNumber = document.DocNumber,
            RTDocMoment = document.DocMoment,
            RTDocType = docType switch { 1 => "REFUND", 3 => "VOID", _ => "POSRECEIPT" },
            RTServerSHAMetadata = document.Ccdc,
            RTCodiceLotteria = document.LotteryCode,
            RTCustomerID = "",
            RTReferenceZNumber = document.ReferenceZNumber,
            RTReferenceDocNumber = document.ReferenceDocNumber,
            RTReferenceDocMoment = document.ReferenceDocMoment
        };
        return SignatureFactory.CreateDocumentoCommercialeSignatures(signatureData);
    }

    private async Task<(List<SignaturItem> signatures, long ftState)> PerformDailyClosingAsync(ReceiptResponse receiptResponse, TillState tillState)
    {
        try
        {
            await _queue.ProcessAllReceipts(tillState.TillId).ConfigureAwait(false);
            await _client.CreateDailyClosureAsync(tillState.TillId, 0).ConfigureAwait(false);
            var zReport = await _client.PrintServerZReportAsync().ConfigureAwait(false);

            var currentZNumber = tillState.LastZNumber;
            // Re-seed the till for the next session (new Z number, reset counters, new token chain).
            await InitializeTillStateAsync(receiptResponse, requestToken: true).ConfigureAwait(false);

            var signatures = SignatureFactory.CreateDailyClosingReceiptSignatures(currentZNumber).ToList();
            return (signatures, StateOk);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform the daily closing on the Epson RT Server.");
            return (new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "rt-server-dailyclosing-error",
                    Data = $"{ex}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = SignatureTypeError
                },
                new SignaturItem
                {
                    Caption = "rt-server-dailyclosing-cached-documents",
                    Data = $"{_queue.GetCountOfDocumentsForInCache(receiptResponse.ftCashBoxIdentification)}",
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = SignatureTypeWarning
                }
            }, StateFailed);
        }
    }

    private async Task<TillState> GetOrLoadTillStateAsync(ReceiptResponse receiptResponse)
    {
        var queueId = Guid.Parse(receiptResponse.ftQueueID);
        LoadStateFromDisk();
        if (_tillStates.TryGetValue(queueId, out var existing))
        {
            return existing;
        }
        return await InitializeTillStateAsync(receiptResponse, requestToken: true).ConfigureAwait(false);
    }

    private async Task<TillState> InitializeTillStateAsync(ReceiptResponse receiptResponse, bool requestToken)
    {
        var queueId = Guid.Parse(receiptResponse.ftQueueID);
        var tillId = receiptResponse.ftCashBoxIdentification;

        var serverInfo = await _client.GetServerInfoAsync().ConfigureAwait(false);
        var serverTime = await SafeGetServerTimeAsync().ConfigureAwait(false);
        var fiscalInformation = await SafeGetFiscalInformationAsync(tillId).ConfigureAwait(false);

        var tillState = new TillState
        {
            TillId = tillId,
            RTServerSerialNumber = serverInfo.GetAddInfo("rtSerialNumber") ?? string.Empty,
            SrtUtcOffset = int.TryParse(serverTime?.GetAddInfo("srtUtcOffset"), out var offset) ? offset : 1,
            LastZNumber = long.TryParse(fiscalInformation?.GetAddInfo("zRepNumber"), out var zNum) ? zNum : 0,
            LastDocNumber = long.TryParse(fiscalInformation?.GetAddInfo("recNumber"), out var recNum) ? recNum : 0,
            CurrentDailyAmount = ParseAmountToCents(fiscalInformation?.GetAddInfo("dailyAmount"))
        };

        if (requestToken)
        {
            var tokenResponse = await _client.CreateTokenAsync(tillId).ConfigureAwait(false);
            var token = tokenResponse.GetAddInfo("token");
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("createToken for till {tillId} did not return a token in addInfo. The blockchain seed may be incorrect. Raw: {raw}", tillId, tokenResponse.RawResponse);
            }
            tillState.LastFingerPrint = token ?? string.Empty;
            tillState.TokenInitialized = true;

            // The token carries the authoritative Z number, next expected document number and daily amount for
            // the current session (validated against firmware 6.01). Prefer it over the ambiguous fiscalInformation
            // recNumber. LastDocNumber is stored as "last issued", so it is one less than the next expected number.
            var parsedToken = EpsonToken.TryParse(token);
            if (parsedToken != null)
            {
                tillState.LastZNumber = parsedToken.ZRepNumber;
                tillState.LastDocNumber = parsedToken.NextDocNumber - 1;
                tillState.CurrentDailyAmount = parsedToken.DailyAmountCents;
                if (string.IsNullOrEmpty(tillState.RTServerSerialNumber))
                {
                    tillState.RTServerSerialNumber = parsedToken.SerialNumber;
                }
            }
        }

        _tillStates[queueId] = tillState;
        PersistState();
        return tillState;
    }

    private async Task<RtServerResponse?> SafeGetServerTimeAsync()
    {
        try { return await _client.GetServerTimeAsync().ConfigureAwait(false); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to read RT Server time."); return null; }
    }

    private async Task<RtServerResponse?> SafeGetFiscalInformationAsync(string tillId)
    {
        try { return await _client.GetFiscalInformationAsync(tillId).ConfigureAwait(false); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to read fiscal information for till {tillId}.", tillId); return null; }
    }

    private static long ParseAmountToCents(string? amount)
    {
        if (string.IsNullOrEmpty(amount))
        {
            return 0;
        }
        var normalized = amount!.Replace(",", ".");
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? (long) Math.Round(value * 100, MidpointRounding.AwayFromZero)
            : 0;
    }

    private void LoadStateFromDisk()
    {
        if (File.Exists(StateCacheFilePath))
        {
            _tillStates = JsonConvert.DeserializeObject<Dictionary<Guid, TillState>>(File.ReadAllText(StateCacheFilePath)) ?? new Dictionary<Guid, TillState>();
        }
    }

    private void PersistState()
    {
        if (!Directory.Exists(_scuCacheFolder))
        {
            Directory.CreateDirectory(_scuCacheFolder);
        }
        File.WriteAllText(StateCacheFilePath, JsonConvert.SerializeObject(_tillStates));
    }
}
