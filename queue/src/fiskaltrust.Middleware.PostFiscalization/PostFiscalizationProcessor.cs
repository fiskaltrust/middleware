using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.PostFiscalization.Contracts;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>
/// Runs the optional eInvoicing and eReporting services around fiscalization (RFC 712). The sign processor of each
/// stack calls <see cref="ValidateAsync"/> before the queue item is created and <see cref="FinalizeAsync"/> after the
/// country-specific processing succeeded; this class holds the two optional clients, carries the preflight result
/// between the seams and encapsulates ordering (eInvoicing, then eReporting) and error handling. It speaks the v2
/// contract only; the legacy stack maps its v1 pair at the boundary.
/// <para>
/// Nothing in here throws into the sign pipeline: a preflight problem becomes a <see cref="PostFiscalizationRejection"/>,
/// a finalize problem marks the fiscalized receipt as failed, and a middleware-side bug is handled the same way.
/// When neither section is configured every method is a no-op.
/// </para>
/// </summary>
public class PostFiscalizationProcessor
{
    public const string CashBoxIdConfigurationKey = "cashboxid";
    public const string AccessTokenConfigurationKey = "accesstoken";
    public const string EInvoicingActivityTag = "queue.PostFiscalization.einvoicing";
    public const string EReportingActivityTag = "queue.PostFiscalization.ereporting";

    /// <summary>Key under which existing <c>ftStateData</c> that cannot be merged into is preserved next to the <c>PostFiscalization</c> section.</summary>
    public const string OriginalStateDataKey = "ftStateDataOriginal";

    private const int _actionJournalPriorityError = 0x10;
    private const int _actionJournalPriorityWarning = 0x20;
    private static readonly TimeSpan _receiptMomentTolerance = TimeSpan.FromSeconds(1);

    private static readonly JsonSerializerOptions _journalSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly ILogger<PostFiscalizationProcessor> _logger;
    private readonly IEInvoicingService? _eInvoicingService;
    private readonly IEReportingService? _eReportingService;
    private readonly Func<ReceiptRequest, SignatureType> _failureSignatureType;
    private readonly Func<ReceiptRequest, bool> _eInvoicingReceiptFilter;

    /// <summary>
    /// Builds the processor from the queue configuration. Validates the configuration and resolves the cashbox
    /// identity eagerly, so a misconfigured queue fails at startup with a clear message instead of failing every receipt.
    /// </summary>
    /// <param name="configuration">The parsed <c>einvoicing</c> / <c>ereporting</c> sections.</param>
    /// <param name="cashBoxId">The queue's cashbox id, sent as <c>x-cashbox-id</c> (a <c>cashboxid</c> entry in the queue configuration takes precedence).</param>
    /// <param name="queueConfiguration">The queue's configuration dictionary; the hosting environment injects <c>cashboxid</c> and <c>accesstoken</c> into it.</param>
    /// <param name="isSandbox">Whether the queue is a sandbox queue; known services are then called at their sandbox endpoint.</param>
    /// <param name="failureSignatureType">How the <c>ftSignatureType</c> of failure signatures is derived from the request. Defaults to the v2 rule; the legacy stack passes its own convention.</param>
    /// <param name="eInvoicingReceiptFilter">Which receipts the eInvoicing service is called for at all. Defaults to <see cref="IsInvoiceDocument"/>; a market can pass an allow list of its own receipt cases.</param>
    /// <exception cref="PostFiscalizationConfigurationException">A configured section is invalid or the access token is missing.</exception>
    public PostFiscalizationProcessor(ILogger<PostFiscalizationProcessor> logger, PostFiscalizationConfiguration configuration, Guid cashBoxId, Dictionary<string, object>? queueConfiguration, bool isSandbox, Func<ReceiptRequest, SignatureType>? failureSignatureType = null, Func<ReceiptRequest, bool>? eInvoicingReceiptFilter = null)
    {
        _logger = logger;
        _failureSignatureType = failureSignatureType ?? DefaultFailureSignatureType;
        _eInvoicingReceiptFilter = eInvoicingReceiptFilter ?? IsInvoiceDocument;
        ValidateAtStartup(configuration, queueConfiguration);
        if (!configuration.IsEnabled)
        {
            return;
        }

        var (headerCashBoxId, accessToken) = ResolveCashBoxIdentity(cashBoxId, queueConfiguration);
        if (configuration.EInvoicing is not null)
        {
            var client = new PostFiscalizationServiceClient(PostFiscalizationService.EInvoicing, configuration.EInvoicing, isSandbox, headerCashBoxId, accessToken, logger);
            _eInvoicingService = client;
            _logger.LogInformation("eInvoicing service '{Service}' enabled for cashbox {CashBoxId}: {Endpoint} (sandbox: {Sandbox}, timeout {Timeout} ms per attempt, {Retries} retries)", configuration.EInvoicing.Service ?? "endpoint override", headerCashBoxId, client.ValidateUri, isSandbox, configuration.EInvoicing.Timeout.TotalMilliseconds, configuration.EInvoicing.EffectiveMaxRetries);
        }

        if (configuration.EReporting is not null)
        {
            var client = new PostFiscalizationServiceClient(PostFiscalizationService.EReporting, configuration.EReporting, isSandbox, headerCashBoxId, accessToken, logger);
            _eReportingService = client;
            _logger.LogInformation("eReporting service '{Service}' enabled for cashbox {CashBoxId}: {Endpoint} (sandbox: {Sandbox}, timeout {Timeout} ms per attempt, {Retries} retries)", configuration.EReporting.Service ?? "endpoint override", headerCashBoxId, client.ValidateUri, isSandbox, configuration.EReporting.Timeout.TotalMilliseconds, configuration.EReporting.EffectiveMaxRetries);
        }
    }

    /// <summary>Builds the processor from service instances. Pass <c>null</c> for a service that is not configured.</summary>
    public PostFiscalizationProcessor(ILogger<PostFiscalizationProcessor> logger, IEInvoicingService? eInvoicingService, IEReportingService? eReportingService, Func<ReceiptRequest, SignatureType>? failureSignatureType = null, Func<ReceiptRequest, bool>? eInvoicingReceiptFilter = null)
    {
        _logger = logger;
        _eInvoicingService = eInvoicingService;
        _eReportingService = eReportingService;
        _failureSignatureType = failureSignatureType ?? DefaultFailureSignatureType;
        _eInvoicingReceiptFilter = eInvoicingReceiptFilter ?? IsInvoiceDocument;
    }

    /// <summary>
    /// The receipts the eInvoicing service is called for: invoice document types, recognized by the <c>0x1000</c> type
    /// nibble of <c>ftReceiptCase</c> (<c>ReceiptCaseType.Invoice</c>: InvoiceUnknown, B2C, B2B, B2G). The Italian
    /// receipt cases carry the same nibble, so this works for IT on the legacy stack as well. v1 receipt cases without a
    /// type nibble (DE, AT, FR) are never invoices to this rule; the legacy stack adds its per-market allow list on top
    /// (<c>LegacyInvoiceReceiptCases</c>).
    /// </summary>
    public static bool IsInvoiceDocument(ReceiptRequest receiptRequest) => receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Invoice);

    /// <summary>A processor with neither service configured, which leaves receipt processing exactly as it is.</summary>
    public static PostFiscalizationProcessor Disabled(ILogger<PostFiscalizationProcessor> logger) => new(logger, eInvoicingService: null, eReportingService: null);

    /// <summary>The v2 rule for the <c>ftSignatureType</c> of a failure signature, mirroring the uncaught-exception signature of the v2 sign processor.</summary>
    public static SignatureType DefaultFailureSignatureType(ReceiptRequest receiptRequest) => receiptRequest.ftReceiptCase.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure);

    /// <summary>
    /// Everything that must hold before a queue may start: a present section is usable, and when a service is
    /// configured the access token the services authenticate against is available.
    /// </summary>
    /// <exception cref="PostFiscalizationConfigurationException">A configured section is invalid or the access token is missing.</exception>
    public static void ValidateAtStartup(PostFiscalizationConfiguration configuration, Dictionary<string, object>? queueConfiguration)
    {
        configuration.Validate();
        if (configuration.IsEnabled && GetConfigurationValue(queueConfiguration, AccessTokenConfigurationKey) is null)
        {
            throw new PostFiscalizationConfigurationException($"An eInvoicing or eReporting service is configured, but the queue configuration contains no '{AccessTokenConfigurationKey}'. The hosting environment injects the cashbox access token into every queue's configuration; without it the services cannot authenticate the queue.");
        }
    }

    public bool IsEnabled => _eInvoicingService is not null || _eReportingService is not null;

    public bool IsConfigured(PostFiscalizationService service) => service switch
    {
        PostFiscalizationService.EInvoicing => _eInvoicingService is not null,
        PostFiscalizationService.EReporting => _eReportingService is not null,
        _ => false,
    };

    /// <summary>The first configured service in processing order, or <c>null</c> when disabled.</summary>
    public PostFiscalizationService? FirstConfiguredService => _eInvoicingService is not null
        ? PostFiscalizationService.EInvoicing
        : _eReportingService is not null ? PostFiscalizationService.EReporting : null;

    /// <summary>
    /// Phase 1: asks every configured service, in order, whether the receipt is valid and whether it acts on it.
    /// Must run before any bookkeeping that produces a fiscal record. A rejection, or a transport or protocol
    /// failure, refuses the receipt: nothing is fiscalized and the POS can correct and resend.
    /// </summary>
    public async Task<PostFiscalizationPreflight> ValidateAsync(ReceiptRequest receiptRequest)
    {
        if (!IsEnabled)
        {
            SetActivityTags(PostFiscalizationServiceOutcome.Disabled, PostFiscalizationServiceOutcome.Disabled);
            return PostFiscalizationPreflight.Disabled;
        }

        PostFiscalizationServiceOutcome eInvoicing;
        PostFiscalizationRejection? rejection;
        if (_eInvoicingService is not null && !_eInvoicingReceiptFilter(receiptRequest))
        {
            // eInvoicing is only ever called for invoice document types; every other receipt is not applicable without a call.
            _logger.LogDebug("eInvoicing skipped for receipt {ReceiptReference}: ftReceiptCase 0x{ReceiptCase:X} is not an invoice document type.", receiptRequest.cbReceiptReference, receiptRequest.ftReceiptCase);
            (eInvoicing, rejection) = (PostFiscalizationServiceOutcome.NotApplicable, null);
        }
        else
        {
            (eInvoicing, rejection) = await PreflightAsync(PostFiscalizationService.EInvoicing, _eInvoicingService is null ? null : _eInvoicingService.ValidateReceiptAsync, receiptRequest).ConfigureAwait(false);
        }

        var eReporting = _eReportingService is null ? PostFiscalizationServiceOutcome.Disabled : PostFiscalizationServiceOutcome.Skipped;
        if (rejection is null)
        {
            (eReporting, rejection) = await PreflightAsync(PostFiscalizationService.EReporting, _eReportingService is null ? null : _eReportingService.ValidateReceiptAsync, receiptRequest).ConfigureAwait(false);
        }

        SetActivityTags(eInvoicing, eReporting);
        return new PostFiscalizationPreflight(eInvoicing, eReporting, rejection);
    }

    /// <summary>
    /// Phase 3: hands the fully fiscalized receipt to every service whose preflight answered <c>Applies</c>, in order,
    /// each one seeing what the previous one added, and merges the returned responses. A failing service marks the
    /// receipt as failed (<c>ftState</c> error, a <c>*-failed</c> signature, an action journal entry) but does not
    /// prevent the call to the next one. Finally the outcome is written into <c>ftStateData.PostFiscalization</c>.
    /// Only call this when fiscalization succeeded.
    /// </summary>
    public async Task<ReceiptResponse> FinalizeAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueueItem queueItem, PostFiscalizationPreflight preflight, List<ftActionJournal> actionJournals)
    {
        if (!IsEnabled)
        {
            return receiptResponse;
        }

        var current = receiptResponse;
        var failureSignatures = new List<SignatureItem>();
        var eInvoicing = preflight.EInvoicing;
        var eReporting = preflight.EReporting;

        if (eInvoicing == PostFiscalizationServiceOutcome.Applies && _eInvoicingService is not null)
        {
            (current, eInvoicing) = await FinalizeServiceAsync(PostFiscalizationService.EInvoicing, _eInvoicingService.ProcessReceiptAsync, receiptRequest, current, queueItem, actionJournals, failureSignatures).ConfigureAwait(false);
        }

        if (eReporting == PostFiscalizationServiceOutcome.Applies && _eReportingService is not null)
        {
            (current, eReporting) = await FinalizeServiceAsync(PostFiscalizationService.EReporting, _eReportingService.ProcessReceiptAsync, receiptRequest, current, queueItem, actionJournals, failureSignatures).ConfigureAwait(false);
        }

        // The failure marks are applied once every service has returned. Each service is therefore handed the response
        // as fiscalization and the successful services before it produced it: an error state on a returned response is
        // unambiguously that service's own signal, and no later service can undo an earlier failure.
        if (failureSignatures.Count > 0)
        {
            current.MarkAsFailed();
            foreach (var signature in failureSignatures)
            {
                current.ftSignatures.Add(signature);
            }
        }

        WriteOutcome(current, eInvoicing, eReporting, queueItem, actionJournals);
        SetActivityTags(eInvoicing, eReporting);
        return current;
    }

    /// <summary>The failure signature shape shared by rejections and finalize failures.</summary>
    public SignatureItem CreateFailureSignature(ReceiptRequest receiptRequest, string caption, string data) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = _failureSignatureType(receiptRequest),
        Caption = caption,
        Data = data,
    };

    private async Task<(PostFiscalizationServiceOutcome outcome, PostFiscalizationRejection? rejection)> PreflightAsync(PostFiscalizationService service, Func<ValidateRequest, Task<ValidateResponse>>? validate, ReceiptRequest receiptRequest)
    {
        if (validate is null)
        {
            return (PostFiscalizationServiceOutcome.Disabled, null);
        }

        try
        {
            var response = await validate(new ValidateRequest { ReceiptRequest = receiptRequest }).ConfigureAwait(false)
                ?? throw new PostFiscalizationServiceException("the service returned no validation result");

            var errors = response.Errors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToList() ?? [];
            if (errors.Count > 0)
            {
                var reason = string.Join("; ", errors);
                _logger.LogWarning("{Service} service rejected receipt {ReceiptReference} before fiscalization: {Reason}", service.DisplayName(), receiptRequest.cbReceiptReference, reason);
                return (PostFiscalizationServiceOutcome.Rejected, new PostFiscalizationRejection(service, PostFiscalizationServiceOutcome.Rejected, reason, JsonSerializer.Serialize(new { errors }, _journalSerializerOptions)));
            }

            if (response.Applies is null)
            {
                throw new PostFiscalizationServiceException("the validation result does not say whether the service applies");
            }

            return (response.Applies.Value ? PostFiscalizationServiceOutcome.Applies : PostFiscalizationServiceOutcome.NotApplicable, null);
        }
        catch (Exception ex)
        {
            // Unreachable, timeout, non-2xx, malformed body or a middleware-side bug: the queue cannot know whether the
            // receipt is invoiceable, and refusing before fiscalization is the safe direction.
            var (reason, detail) = Describe(ex);
            _logger.LogError(ex, "{Service} validate call failed for receipt {ReceiptReference}; the receipt is refused before fiscalization.", service.DisplayName(), receiptRequest.cbReceiptReference);
            return (PostFiscalizationServiceOutcome.Failed, new PostFiscalizationRejection(service, PostFiscalizationServiceOutcome.Failed, $"{service.DisplayName()} validate call failed: {reason}", detail));
        }
    }

    private async Task<(ReceiptResponse response, PostFiscalizationServiceOutcome outcome)> FinalizeServiceAsync(PostFiscalizationService service, Func<ProcessRequest, Task<ProcessResponse>> process, ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, ftQueueItem queueItem, List<ftActionJournal> actionJournals, List<SignatureItem> failureSignatures)
    {
        try
        {
            var processResponse = await process(new ProcessRequest { ReceiptRequest = receiptRequest, ReceiptResponse = receiptResponse }).ConfigureAwait(false);
            var returned = processResponse?.ReceiptResponse ?? throw new PostFiscalizationServiceException("the service returned no receipt response");
            returned.ftSignatures ??= [];

            if (returned.ftState.IsState(State.Error))
            {
                // Same meaning as a non-2xx: the two channels exist for convenience. The request never carries an error
                // state (fiscalization succeeded and earlier failures are marked only after all services returned), so
                // this is unambiguously the service's own signal. Its signatures are kept for diagnosis in the action
                // journal; the response used from here on is the pre-call one.
                throw new PostFiscalizationServiceException($"the service reported an error state (ftState 0x{returned.ftState:X})", DescribeAddedSignatures(receiptResponse, returned));
            }

            RestoreIdentity(service, receiptResponse, returned, queueItem, actionJournals);
            RestoreDroppedStateData(service, receiptResponse, returned, queueItem, actionJournals);
            ReportDroppedSignatures(service, receiptResponse, returned, queueItem, actionJournals);
            _logger.LogDebug("{Service} process call succeeded for queue item {QueueItemId}.", service.DisplayName(), queueItem.ftQueueItemId);
            return (returned, PostFiscalizationServiceOutcome.Ok);
        }
        catch (Exception ex)
        {
            // The receipt is fiscalized: it will be marked as failed (0xEEEE_EEEE) with a signature naming the service once
            // all services returned, and the technical detail is recorded. The POS must recover via ReceiptRequested,
            // never by resending the receipt.
            var (reason, detail) = Describe(ex);
            _logger.LogError(ex, "{Service} process call failed for queue item {QueueItemId}; the receipt is fiscalized and is marked as failed.", service.DisplayName(), queueItem.ftQueueItemId);

            failureSignatures.Add(CreateFailureSignature(receiptRequest, service.FailedCaption(), $"{service.DisplayName()} process call failed after fiscalization: {reason}"));
            actionJournals.Add(CreateActionJournal(queueItem, _actionJournalPriorityError, service.FailedCaption(),
                $"{service.DisplayName()} process call failed for queue item {queueItem.ftQueueItemId} after fiscalization: {reason}",
                new { service = service.Key(), phase = "process", reason, detail }));
            return (receiptResponse, PostFiscalizationServiceOutcome.Failed);
        }
    }

    /// <summary>
    /// The returned response is authoritative, except for the fields that identify the queue item: a service must not
    /// change them, and persisting changed ones would corrupt the queue item and break later lookups.
    /// </summary>
    private void RestoreIdentity(PostFiscalizationService service, ReceiptResponse before, ReceiptResponse after, ftQueueItem queueItem, List<ftActionJournal> actionJournals)
    {
        var changed = new List<string>();
        if (after.ftQueueItemID != before.ftQueueItemID)
        {
            changed.Add(nameof(after.ftQueueItemID));
        }
        if (after.ftQueueID != before.ftQueueID)
        {
            changed.Add(nameof(after.ftQueueID));
        }
        if (after.ftQueueRow != before.ftQueueRow)
        {
            changed.Add(nameof(after.ftQueueRow));
        }
        if (after.ftCashBoxID != before.ftCashBoxID)
        {
            changed.Add(nameof(after.ftCashBoxID));
        }
        if (!string.Equals(after.cbReceiptReference, before.cbReceiptReference, StringComparison.Ordinal))
        {
            changed.Add(nameof(after.cbReceiptReference));
        }
        if (!string.Equals(after.cbTerminalID, before.cbTerminalID, StringComparison.Ordinal))
        {
            changed.Add(nameof(after.cbTerminalID));
        }
        if (!string.Equals(after.ftReceiptIdentification, before.ftReceiptIdentification, StringComparison.Ordinal))
        {
            changed.Add(nameof(after.ftReceiptIdentification));
        }
        if (!string.Equals(after.ftCashBoxIdentification, before.ftCashBoxIdentification, StringComparison.Ordinal))
        {
            changed.Add(nameof(after.ftCashBoxIdentification));
        }
        if ((after.ftReceiptMoment - before.ftReceiptMoment).Duration() > _receiptMomentTolerance)
        {
            changed.Add(nameof(after.ftReceiptMoment));
        }

        after.ftQueueItemID = before.ftQueueItemID;
        after.ftQueueID = before.ftQueueID;
        after.ftQueueRow = before.ftQueueRow;
        after.ftCashBoxID = before.ftCashBoxID;
        after.cbReceiptReference = before.cbReceiptReference;
        after.cbTerminalID = before.cbTerminalID;
        after.ftReceiptIdentification = before.ftReceiptIdentification;
        after.ftCashBoxIdentification = before.ftCashBoxIdentification;
        after.ftReceiptMoment = before.ftReceiptMoment;

        if (changed.Count == 0)
        {
            return;
        }

        var message = $"The {service.DisplayName()} service changed identifying fields ({string.Join(", ", changed)}) of queue item {queueItem.ftQueueItemId}; the original values were restored.";
        _logger.LogWarning(message);
        actionJournals.Add(CreateActionJournal(queueItem, _actionJournalPriorityWarning, $"{service.Key()}-contract-violation", message, new { service = service.Key(), changedFields = changed }));
    }

    /// <summary>
    /// A service must merge into the existing <c>ftStateData</c>, never replace it. When the returned response dropped
    /// the state data or replaced it with something that is not a JSON object, the state data from before the call is
    /// restored so that neither the previous receipt references nor the market sections are lost.
    /// </summary>
    private void RestoreDroppedStateData(PostFiscalizationService service, ReceiptResponse before, ReceiptResponse after, ftQueueItem queueItem, List<ftActionJournal> actionJournals)
    {
        string? problem = null;
        if (after.ftStateData is null && before.ftStateData is not null)
        {
            problem = "dropped the existing ftStateData";
        }
        else if (after.ftStateData is not null && !IsJsonObject(after.ftStateData))
        {
            problem = "returned ftStateData that is not a JSON object";
        }

        if (problem is null)
        {
            return;
        }

        after.ftStateData = before.ftStateData;
        var message = $"The {service.DisplayName()} service {problem} of queue item {queueItem.ftQueueItemId}; the state data from before the call was restored.";
        _logger.LogWarning(message);
        actionJournals.Add(CreateActionJournal(queueItem, _actionJournalPriorityWarning, $"{service.Key()}-contract-violation", message, new { service = service.Key(), problem }));
    }

    private static bool IsJsonObject(object stateData) => stateData switch
    {
        JsonElement element => element.ValueKind == JsonValueKind.Object,
        string => false,
        ValueType => false,
        System.Collections.IDictionary => true,
        System.Collections.IEnumerable => false,
        _ => true,
    };

    private void ReportDroppedSignatures(PostFiscalizationService service, ReceiptResponse before, ReceiptResponse after, ftQueueItem queueItem, List<ftActionJournal> actionJournals)
    {
        var dropped = before.ftSignatures.Where(signature => !after.ftSignatures.Any(candidate => SameSignature(candidate, signature))).ToList();
        if (dropped.Count == 0)
        {
            return;
        }

        var captions = string.Join(", ", dropped.Select(signature => signature.Caption ?? $"0x{signature.ftSignatureType:X}"));
        var message = $"The {service.DisplayName()} service dropped {dropped.Count} previously present signature(s) ({captions}) from queue item {queueItem.ftQueueItemId}. The returned response is used as is.";
        _logger.LogWarning(message);
        actionJournals.Add(CreateActionJournal(queueItem, _actionJournalPriorityWarning, $"{service.Key()}-dropped-signatures", message, new { service = service.Key(), droppedSignatures = dropped }));
    }

    /// <summary>
    /// Writes the outcome into a section the middleware owns, after the last service returned so that no service can
    /// overwrite it. Existing state data (previous receipt references, market sections) is merged into, never replaced.
    /// </summary>
    private void WriteOutcome(ReceiptResponse response, PostFiscalizationServiceOutcome eInvoicing, PostFiscalizationServiceOutcome eReporting, ftQueueItem queueItem, List<ftActionJournal> actionJournals)
    {
        var outcome = new PostFiscalizationStateData
        {
            FiscalizationSucceeded = true,
            EInvoicing = ToStateValue(eInvoicing),
            EReporting = ToStateValue(eReporting),
        };

        try
        {
            var stateData = ToMiddlewareStateData(response.ftStateData);
            stateData.PostFiscalization = outcome;
            response.ftStateData = stateData;
        }
        catch (Exception ex)
        {
            // The existing state data is not something the middleware can merge into. The middleware-owned section must
            // still be persisted, because without FiscalizationSucceeded a receipt whose finalize call failed would look
            // unfiscalized to IsFiscalized() and become unreferenceable. The original data is preserved under its own key.
            var wrapped = new MiddlewareStateData { PostFiscalization = outcome };
            wrapped.ExtraData[OriginalStateDataKey] = ToJsonElement(response.ftStateData);
            response.ftStateData = wrapped;

            var message = $"The existing ftStateData of queue item {queueItem.ftQueueItemId} could not be interpreted as an object; it was preserved under '{OriginalStateDataKey}' next to the PostFiscalization outcome.";
            _logger.LogError(ex, message);
            actionJournals.Add(CreateActionJournal(queueItem, _actionJournalPriorityError, "postfiscalization-statedata", message, new { outcome, error = ex.ToString() }));
        }
    }

    private static JsonElement ToJsonElement(object? value)
    {
        try
        {
            return JsonSerializer.SerializeToElement(value, _journalSerializerOptions);
        }
        catch (Exception)
        {
            return JsonSerializer.SerializeToElement(value?.ToString());
        }
    }

    private static MiddlewareStateData ToMiddlewareStateData(object? stateData) => stateData switch
    {
        null => new MiddlewareStateData(),
        MiddlewareStateData typed => typed,
        JsonElement element => JsonSerializer.Deserialize<MiddlewareStateData>(element.GetRawText()) ?? new MiddlewareStateData(),
        string json => JsonSerializer.Deserialize<MiddlewareStateData>(json) ?? new MiddlewareStateData(),
        _ => JsonSerializer.Deserialize<MiddlewareStateData>(JsonSerializer.Serialize(stateData)) ?? new MiddlewareStateData(),
    };

    private static string ToStateValue(PostFiscalizationServiceOutcome outcome) => outcome switch
    {
        PostFiscalizationServiceOutcome.Disabled => PostFiscalizationStateData.Disabled,
        PostFiscalizationServiceOutcome.NotApplicable => PostFiscalizationStateData.NotApplicable,
        PostFiscalizationServiceOutcome.Ok => PostFiscalizationStateData.Ok,
        _ => PostFiscalizationStateData.Failed,
    };

    private static void SetActivityTags(PostFiscalizationServiceOutcome eInvoicing, PostFiscalizationServiceOutcome eReporting)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag(EInvoicingActivityTag, eInvoicing.ToTagValue());
        activity.SetTag(EReportingActivityTag, eReporting.ToTagValue());
    }

    private static (string reason, string? detail) Describe(Exception exception) => exception switch
    {
        PostFiscalizationServiceException serviceException => (serviceException.Message, serviceException.Detail),
        _ => ($"unexpected error: {exception.Message}", exception.ToString()),
    };

    private static string? DescribeAddedSignatures(ReceiptResponse before, ReceiptResponse after)
    {
        var added = after.ftSignatures.Where(signature => !before.ftSignatures.Any(candidate => SameSignature(candidate, signature))).ToList();
        return added.Count == 0 ? null : JsonSerializer.Serialize(new { signaturesReturnedByService = added }, _journalSerializerOptions);
    }

    private static bool SameSignature(SignatureItem a, SignatureItem b)
        => a.ftSignatureType == b.ftSignatureType
        && a.ftSignatureFormat == b.ftSignatureFormat
        && string.Equals(a.Caption, b.Caption, StringComparison.Ordinal)
        && string.Equals(a.Data, b.Data, StringComparison.Ordinal);

    private static ftActionJournal CreateActionJournal(ftQueueItem queueItem, int priority, string type, string message, object? data) => new()
    {
        ftActionJournalId = Guid.NewGuid(),
        ftQueueId = queueItem.ftQueueId,
        ftQueueItemId = queueItem.ftQueueItemId,
        Moment = DateTime.UtcNow,
        Priority = priority,
        Type = type,
        Message = message,
        DataJson = data is null ? null : JsonSerializer.Serialize(data, _journalSerializerOptions),
    };

    private static (Guid cashBoxId, string accessToken) ResolveCashBoxIdentity(Guid cashBoxId, Dictionary<string, object>? queueConfiguration)
    {
        // The hosting environment injects 'cashboxid' and 'accesstoken' into every queue's configuration dictionary.
        if (GetConfigurationValue(queueConfiguration, CashBoxIdConfigurationKey) is { } configuredCashBoxId && Guid.TryParse(configuredCashBoxId, out var parsedCashBoxId))
        {
            cashBoxId = parsedCashBoxId;
        }

        var accessToken = GetConfigurationValue(queueConfiguration, AccessTokenConfigurationKey)
            ?? throw new PostFiscalizationConfigurationException($"An eInvoicing or eReporting service is configured, but the queue configuration contains no '{AccessTokenConfigurationKey}'.");
        return (cashBoxId, accessToken);
    }

    private static string? GetConfigurationValue(Dictionary<string, object>? configuration, string key)
    {
        if (configuration is null)
        {
            return null;
        }

        var entry = configuration.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
        var value = entry.Key is null ? null : entry.Value?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

internal static class PostFiscalizationReceiptResponseExtensions
{
    internal static void MarkAsFailed(this ReceiptResponse receiptResponse) => receiptResponse.ftState = receiptResponse.ftState.WithState(State.Error);
}
