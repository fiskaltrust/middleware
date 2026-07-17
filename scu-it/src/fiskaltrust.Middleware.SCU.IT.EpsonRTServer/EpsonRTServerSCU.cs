using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
    // The 0x...0020_.... group flags the signature as internal/technical so it is NOT rendered on the fiscal
    // document (PDF) shown to the customer — used for non-blocking anomaly warnings.
    private const long SignatureTypeWarning = 0x4954_2000_0020_2000;

    private readonly Guid _id;
    private readonly ILogger<EpsonRTServerSCU> _logger;
    private readonly EpsonRTServerConfiguration _configuration;
    private readonly IEpsonRTServerClient _client;
    private readonly EpsonRTServerCommunicationQueue _queue;
    private readonly string _scuCacheFolder;
    private bool _persistDisabled;

    private Dictionary<Guid, TillState> _tillStates = new();

    // The SCU is registered as scoped, so multiple instances can process receipts for the same SCU id
    // concurrently. Serialize per SCU id to protect the CCDC chain counters and the state-cache file.
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _scuLocks = new();

    private string StateCacheFilePath => Path.Combine(_scuCacheFolder, $"{_id}_epsonrtserver_statecache.json");

    public EpsonRTServerSCU(Guid id, ILogger<EpsonRTServerSCU> logger, EpsonRTServerConfiguration configuration, IEpsonRTServerClient client, EpsonRTServerCommunicationQueue queue, Func<string>? personalFolderProvider = null)
    {
        _id = id;
        _logger = logger;
        _configuration = configuration;
        _client = client;
        _queue = queue;

        var personalFolder = personalFolderProvider ?? (() => Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        _scuCacheFolder = string.IsNullOrEmpty(configuration.ServiceFolder)
            ? personalFolder()
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
        var scuLock = _scuLocks.GetOrAdd(_id, _ => new SemaphoreSlim(1, 1));
        await scuLock.WaitAsync().ConfigureAwait(false);
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
        finally
        {
            scuLock.Release();
        }
    }

    private async Task<(List<SignaturItem> signatures, long ftState)> PerformInitOperationAsync(ReceiptResponse receiptResponse)
    {
        try
        {
            var tillId = receiptResponse.ftCashBoxIdentification;
            if (_configuration.AutoProgramTillMap)
            {
                // createTills REPLACES the whole till map: read the current map first and only reprogram when
                // our till is missing, keeping all existing tills (the device may be shared with other queues).
                var tillMapResponse = await _client.GetTillMapAsync().ConfigureAwait(false);
                var existingTills = ParseTillIds(tillMapResponse.RawResponse);
                if (!existingTills.Contains(tillId))
                {
                    await _client.CreateTillsAsync(_configuration.Username, _configuration.Password, existingTills.Concat(new[] { tillId }), new[] { tillId }).ConfigureAwait(false);
                    await _client.RebootWebServerAsync().ConfigureAwait(false);
                    // The on-board web server needs a moment to come back after the map change.
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
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
        var response = await SendDocumentAsync(tillState, document).ConfigureAwait(false);

        if (response != null && EpsonRTServerErrorCodes.IsLocalStateOutOfSync(response.CodeAsInt))
        {
            // The local state is out of sync with the server (-21 blockchain / -22 hash / -23 daily amount /
            // -25 receipt number, e.g. after an externally triggered closure). Per the security protocol the
            // recovery is a fresh Token: reseed the chain and counters, rebuild the document and retry once.
            _logger.LogWarning("RT Server reported {code} ({description}); requesting a new token and retrying once.",
                response.Code, EpsonRTServerErrorCodes.Describe(response.CodeAsInt));
            tillState = await InitializeTillStateAsync(receiptResponse, requestToken: true).ConfigureAwait(false);
            document = EpsonRTServerMapping.BuildFiscalDocument(receiptRequest, tillState, docType, referenceZNumber, referenceDocNumber, referenceDocMoment, referenceTillId);
            response = await SendDocumentAsync(tillState, document).ConfigureAwait(false);
        }

        if (response != null && !response.Success && response.CodeAsInt < 0
            && !EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning(response.CodeAsInt))
        {
            // Blocking rejection ("Receipt not accepted"): do NOT advance the local chain, otherwise every
            // following document would be refused too. Codes flagged as "accepted with error in log" are NOT
            // rejections — they fall through, the chain advances and a warning signature is added below.
            throw new EpsonRTServerCommunicationException(
                $"The RT Server rejected the document with code {response.Code}: {EpsonRTServerErrorCodes.Describe(response.CodeAsInt)}",
                response.CodeAsInt);
        }

        // Advance the local blockchain state (Section A of the next document = this document's CCDC). In async
        // mode (response == null) the document is cached on disk and the chain advances optimistically so
        // follow-up documents keep chaining while offline.
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
        var signatures = SignatureFactory.CreateDocumentoCommercialeSignatures(signatureData).ToList();

        // The RT Server accepted the document but flagged a non-blocking anomaly (logged only). Surface it as
        // a warning signature (kept off the fiscal PDF via SignatureTypeWarning). -43/-44 additionally mean the
        // lottery code was not registered.
        if (response != null && EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning(response.CodeAsInt))
        {
            signatures.Add(new SignaturItem
            {
                Caption = EpsonRTServerErrorCodes.IsLotteryNotRegistered(response.CodeAsInt)
                    ? "rt-server-lottery-not-registered"
                    : "rt-server-receipt-warning",
                Data = $"{response.Code}: {EpsonRTServerErrorCodes.Describe(response.CodeAsInt)}",
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = SignatureTypeWarning
            });
        }
        return signatures;
    }

    /// <summary>
    /// Sends the document through the queue. Client-side rejection exceptions carrying an RT error code are
    /// converted into an inspectable response so the chain-recovery logic can react to -21/-22 regardless of
    /// the IgnoreRTServerErrors setting.
    /// </summary>
    private async Task<RtServerResponse?> SendDocumentAsync(TillState tillState, FiscalDocumentResult document)
    {
        try
        {
            return await _queue.EnqueueDocument(tillState.TillId, document.CreateReceiptXml, document.ZNumber, document.DocNumber).ConfigureAwait(false);
        }
        catch (EpsonRTServerCommunicationException ex) when (ex.ResponseCode < 0)
        {
            return new RtServerResponse { Success = false, Code = ex.ResponseCode.ToString(), Status = ex.Message };
        }
    }

    private async Task<(List<SignaturItem> signatures, long ftState)> PerformDailyClosingAsync(ReceiptResponse receiptResponse, TillState tillState)
    {
        try
        {
            await _queue.ProcessAllReceipts(tillState.TillId).ConfigureAwait(false);
            await _client.CreateDailyClosureAsync(tillState.TillId, 0).ConfigureAwait(false);
            if (_configuration.PerformServerZReportOnDailyClosing)
            {
                // Device-wide operation (transmits to the tax authority, long busy window) — opt-in only.
                _ = await _client.PrintServerZReportAsync().ConfigureAwait(false);
            }

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
        if (string.IsNullOrEmpty(tillId) || tillId.Length != 8)
        {
            throw new InvalidOperationException(
                $"The ftCashBoxIdentification '{tillId}' is not a valid Epson RT Server till id: it must be exactly 8 characters (4-char store id + 4-char till id) and present in the RT Server till map.");
        }

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

    /// <summary>Extracts the till ids from the raw tillMap report (nested tillCountN elements).</summary>
    private static List<string> ParseTillIds(string rawTillMapResponse)
        => System.Text.RegularExpressions.Regex.Matches(rawTillMapResponse, "tillId=\"([^\"]+)\"")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(x => x.Groups[1].Value)
            .Distinct()
            .ToList();

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
        if (string.IsNullOrEmpty(_scuCacheFolder) || !File.Exists(StateCacheFilePath))
        {
            return;
        }
        try
        {
            _tillStates = JsonConvert.DeserializeObject<Dictionary<Guid, TillState>>(File.ReadAllText(StateCacheFilePath)) ?? new Dictionary<Guid, TillState>();
        }
        catch (JsonException ex)
        {
            // A corrupt cache is recoverable: the state is re-seeded from the server (token + fiscalInformation).
            _logger.LogWarning(ex, "The state cache '{path}' is corrupt and will be rebuilt from the RT Server.", StateCacheFilePath);
            _tillStates = new Dictionary<Guid, TillState>();
        }
    }

    private void PersistState()
    {
        if (_persistDisabled || string.IsNullOrEmpty(_scuCacheFolder))
        {
            return;
        }
        try
        {
            if (!Directory.Exists(_scuCacheFolder))
            {
                Directory.CreateDirectory(_scuCacheFolder);
            }
            // Write-temp-then-swap: a crash mid-write must not corrupt the chain state (the fingerprint in this
            // file is the Section A of the next document).
            var tempPath = StateCacheFilePath + ".tmp";
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(_tillStates));
            if (File.Exists(StateCacheFilePath))
            {
                File.Replace(tempPath, StateCacheFilePath, null);
            }
            else
            {
                File.Move(tempPath, StateCacheFilePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // No writable service folder (e.g. stateless cloud host): till state is a pure cache re-seeded from
            // the RT Server via createToken, so persistence is best-effort. Disable to avoid repeated churn.
            _persistDisabled = true;
            _logger.LogWarning(ex, "Could not persist Epson RT Server till state to '{folder}'; continuing in-memory (state is re-seeded from the device via createToken).", _scuCacheFolder);
        }
    }
}
