using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.PostFiscalization.Contracts;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2.PostFiscalization;

/// <summary>
/// Runs the optional eInvoicing and eReporting services around fiscalization (RFC 712). The <see cref="SignProcessor"/>
/// calls <see cref="ValidateAsync"/> before the queue item is created and <see cref="FinalizeAsync"/> after the
/// country-specific processing succeeded; this class holds the two optional clients, carries the preflight result
/// between the seams and encapsulates ordering (eInvoicing, then eReporting) and error handling.
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

    /// <summary>
    /// Builds the processor from the queue configuration. Validates the configuration and resolves the cashbox
    /// identity eagerly, so a misconfigured queue fails at startup with a clear message instead of failing every receipt.
    /// </summary>
    /// <exception cref="PostFiscalizationConfigurationException">A configured section is invalid or the access token is missing.</exception>
    public PostFiscalizationProcessor(ILogger<PostFiscalizationProcessor> logger, PostFiscalizationConfiguration configuration, MiddlewareConfiguration middlewareConfiguration)
    {
        _logger = logger;
        configuration.Validate();
        if (!configuration.IsEnabled)
        {
            return;
        }

        var (cashBoxId, accessToken) = ResolveCashBoxIdentity(middlewareConfiguration);
        if (configuration.EInvoicing is not null)
        {
            _eInvoicingService = new PostFiscalizationServiceClient(PostFiscalizationService.EInvoicing, configuration.EInvoicing, cashBoxId, accessToken, logger);
            _logger.LogInformation("eInvoicing service enabled for queue {QueueId}: {Endpoint} (timeout {Timeout} ms per attempt, {Retries} retries)", middlewareConfiguration.QueueId, configuration.EInvoicing.Endpoint, configuration.EInvoicing.Timeout.TotalMilliseconds, configuration.EInvoicing.EffectiveMaxRetries);
        }

        if (configuration.EReporting is not null)
        {
            _eReportingService = new PostFiscalizationServiceClient(PostFiscalizationService.EReporting, configuration.EReporting, cashBoxId, accessToken, logger);
            _logger.LogInformation("eReporting service enabled for queue {QueueId}: {Endpoint} (timeout {Timeout} ms per attempt, {Retries} retries)", middlewareConfiguration.QueueId, configuration.EReporting.Endpoint, configuration.EReporting.Timeout.TotalMilliseconds, configuration.EReporting.EffectiveMaxRetries);
        }
    }

    /// <summary>Builds the processor from service instances. Pass <c>null</c> for a service that is not configured.</summary>
    public PostFiscalizationProcessor(ILogger<PostFiscalizationProcessor> logger, IEInvoicingService? eInvoicingService, IEReportingService? eReportingService)
    {
        _logger = logger;
        _eInvoicingService = eInvoicingService;
        _eReportingService = eReportingService;
    }

    /// <summary>A processor with neither service configured, which leaves receipt processing exactly as it is.</summary>
    public static PostFiscalizationProcessor Disabled(ILogger<PostFiscalizationProcessor> logger) => new(logger, eInvoicingService: null, eReportingService: null);

    public bool IsEnabled => _eInvoicingService is not null || _eReportingService is not null;

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

        var (eInvoicing, rejection) = await PreflightAsync(PostFiscalizationService.EInvoicing, _eInvoicingService is null ? null : _eInvoicingService.ValidateReceiptAsync, receiptRequest).ConfigureAwait(false);

        var eReporting = _eReportingService is null ? PostFiscalizationServiceOutcome.Disabled : PostFiscalizationServiceOutcome.Skipped;
        if (rejection is null)
        {
            (eReporting, rejection) = await PreflightAsync(PostFiscalizationService.EReporting, _eReportingService is null ? null : _eReportingService.ValidateReceiptAsync, receiptRequest).ConfigureAwait(false);
        }

        SetActivityTags(eInvoicing, eReporting);
        return new PostFiscalizationPreflight
        {
            EInvoicing = eInvoicing,
            EReporting = eReporting,
            Rejection = rejection,
        };
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

        if (failureSignatures.Count > 0)
        {
            EnsureFailurePersists(current, failureSignatures);
        }

        WriteOutcome(current, eInvoicing, eReporting, queueItem, actionJournals);
        SetActivityTags(eInvoicing, eReporting);
        return current;
    }

    /// <summary>The failure signature shape shared by rejections and finalize failures, mirroring the uncaught-exception signature of the <see cref="SignProcessor"/>.</summary>
    public static SignatureItem CreateFailureSignature(ReceiptRequest receiptRequest, string caption, string data) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = receiptRequest.ftReceiptCase.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure),
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

            return (response.Applies ? PostFiscalizationServiceOutcome.Applies : PostFiscalizationServiceOutcome.NotApplicable, null);
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

            if (returned.ftState.IsState(State.Error) && !receiptResponse.ftState.IsState(State.Error))
            {
                // The service flipped the response to an error state: same meaning as a non-2xx, the two channels exist
                // for convenience. The service's own signatures are kept for diagnosis in the action journal; the response
                // used from here on is the pre-call one. (A response that already carried the error state because an
                // earlier service failed is legitimately returned as is, and is not a failure of this service.)
                throw new PostFiscalizationServiceException($"the service reported an error state (ftState 0x{returned.ftState:X})", DescribeAddedSignatures(receiptResponse, returned));
            }

            RestoreIdentity(service, receiptResponse, returned, queueItem, actionJournals);
            ReportDroppedSignatures(service, receiptResponse, returned, queueItem, actionJournals);
            _logger.LogDebug("{Service} process call succeeded for queue item {QueueItemId}.", service.DisplayName(), queueItem.ftQueueItemId);
            return (returned, PostFiscalizationServiceOutcome.Ok);
        }
        catch (Exception ex)
        {
            // The receipt is fiscalized: mark it as failed (0xEEEE_EEEE) with a signature naming the service, and record
            // the technical detail. The POS must recover via ReceiptRequested, never by resending the receipt.
            var (reason, detail) = Describe(ex);
            _logger.LogError(ex, "{Service} process call failed for queue item {QueueItemId}; the receipt is fiscalized and is marked as failed.", service.DisplayName(), queueItem.ftQueueItemId);

            receiptResponse.MarkAsFailed();
            var signature = CreateFailureSignature(receiptRequest, service.FailedCaption(), $"{service.DisplayName()} process call failed after fiscalization: {reason}");
            receiptResponse.AddSignatureItem(signature);
            failureSignatures.Add(signature);
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

    /// <summary>A later service must not undo an earlier finalize failure.</summary>
    private static void EnsureFailurePersists(ReceiptResponse response, List<SignatureItem> failureSignatures)
    {
        response.MarkAsFailed();
        foreach (var signature in failureSignatures)
        {
            if (!response.ftSignatures.Any(candidate => SameSignature(candidate, signature)))
            {
                response.AddSignatureItem(signature);
            }
        }
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
            var message = $"The PostFiscalization outcome could not be written into ftStateData of queue item {queueItem.ftQueueItemId}: the existing state data could not be interpreted and was kept unchanged.";
            _logger.LogError(ex, message);
            actionJournals.Add(CreateActionJournal(queueItem, _actionJournalPriorityError, "postfiscalization-statedata", message, new { outcome, error = ex.ToString() }));
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

    private static (Guid cashBoxId, string accessToken) ResolveCashBoxIdentity(MiddlewareConfiguration middlewareConfiguration)
    {
        // The hosting environment injects 'cashboxid' and 'accesstoken' into every queue's configuration dictionary.
        var configuration = middlewareConfiguration.Configuration ?? [];
        var cashBoxId = middlewareConfiguration.CashBoxId;
        if (TryGetConfigurationValue(configuration, CashBoxIdConfigurationKey, out var configuredCashBoxId) && Guid.TryParse(configuredCashBoxId, out var parsedCashBoxId))
        {
            cashBoxId = parsedCashBoxId;
        }

        if (!TryGetConfigurationValue(configuration, AccessTokenConfigurationKey, out var accessToken))
        {
            throw new PostFiscalizationConfigurationException($"An eInvoicing or eReporting service is configured, but the queue configuration contains no '{AccessTokenConfigurationKey}'. The hosting environment injects the cashbox access token into every queue's configuration; without it the services cannot authenticate the queue.");
        }

        return (cashBoxId, accessToken);
    }

    private static bool TryGetConfigurationValue(Dictionary<string, object> configuration, string key, [NotNullWhen(true)] out string? value)
    {
        var entry = configuration.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
        value = entry.Key is null ? null : entry.Value?.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }
}
