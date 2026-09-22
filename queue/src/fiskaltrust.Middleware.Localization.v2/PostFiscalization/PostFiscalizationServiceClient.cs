using System.Net;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.PostFiscalization.Contracts;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2.PostFiscalization;

/// <summary>
/// The middleware's own HTTP client for eInvoicing and eReporting services (RFC 712, "HTTP wire protocol").
/// It deliberately does not go through <c>IClientFactory</c>/<c>ClientConfiguration</c>: the SCU client
/// infrastructure counts retries differently, never cancels the in-flight request on timeout, and derives its
/// routes from interface method names.
/// <list type="bullet">
/// <item><c>POST {endpoint}/validate</c> with a <see cref="ValidateRequest"/>, <c>POST {endpoint}/process</c> with a <see cref="ProcessRequest"/>.</item>
/// <item>Bodies are System.Text.Json with <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>, like the sign endpoint.</item>
/// <item>Every request carries <c>x-cashbox-id</c> and <c>x-cashbox-accesstoken</c>.</item>
/// <item>The timeout bounds each attempt and is enforced with a <see cref="CancellationToken"/>, so the request is actually cancelled.</item>
/// <item>Retries happen on timeout, 5xx and connection-level failures only. A 4xx or a malformed body is a deliberate answer and is never retried.</item>
/// <item>Any outcome other than a 2xx with a well-formed body surfaces as a <see cref="PostFiscalizationServiceException"/>.</item>
/// </list>
/// </summary>
public sealed class PostFiscalizationServiceClient : IEInvoicingService, IEReportingService, IDisposable
{
    public const string CashBoxIdHeader = "x-cashbox-id";
    public const string AccessTokenHeader = "x-cashbox-accesstoken";
    private const int _detailExcerptLength = 2000;

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions _deserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly PostFiscalizationService _service;
    private readonly Uri _validateUri;
    private readonly Uri _processUri;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly string _cashBoxId;
    private readonly string _accessToken;

    public PostFiscalizationServiceClient(PostFiscalizationService service, PostFiscalizationServiceConfiguration configuration, Guid cashBoxId, string accessToken, ILogger logger)
        : this(service, configuration, cashBoxId, accessToken, logger, new HttpClient()) { }

    /// <summary>Constructor for tests, which inject a stub <see cref="HttpMessageHandler"/>.</summary>
    public PostFiscalizationServiceClient(PostFiscalizationService service, PostFiscalizationServiceConfiguration configuration, Guid cashBoxId, string accessToken, ILogger logger, HttpMessageHandler handler)
        : this(service, configuration, cashBoxId, accessToken, logger, new HttpClient(handler)) { }

    private PostFiscalizationServiceClient(PostFiscalizationService service, PostFiscalizationServiceConfiguration configuration, Guid cashBoxId, string accessToken, ILogger logger, HttpClient httpClient)
    {
        var endpoint = configuration.Validate(service.Key());
        var baseUrl = endpoint.AbsoluteUri.TrimEnd('/');
        _service = service;
        _validateUri = new Uri($"{baseUrl}/validate");
        _processUri = new Uri($"{baseUrl}/process");
        _timeout = configuration.Timeout;
        _maxRetries = configuration.EffectiveMaxRetries;
        _cashBoxId = cashBoxId.ToString();
        _accessToken = accessToken;
        _logger = logger;
        _httpClient = httpClient;
        // The per-attempt timeout is enforced below with a CancellationToken; HttpClient's own timeout (100s by
        // default) would otherwise cap or race with a configured timeout.
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    public Uri ValidateUri => _validateUri;

    public Uri ProcessUri => _processUri;

    public Task<ValidateResponse> ValidateReceiptAsync(ValidateRequest request) => SendAsync<ValidateRequest, ValidateResponse>(_validateUri, request);

    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => SendAsync<ProcessRequest, ProcessResponse>(_processUri, request);

    public void Dispose() => _httpClient.Dispose();

    private async Task<TResponse> SendAsync<TRequest, TResponse>(Uri uri, TRequest body)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(body, _serializerOptions);
        var attempts = _maxRetries + 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var isLastAttempt = attempt == attempts;
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new ByteArrayContent(payload),
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.TryAddWithoutValidation(CashBoxIdHeader, _cashBoxId);
            request.Headers.TryAddWithoutValidation(AccessTokenHeader, _accessToken);

            using var timeout = new CancellationTokenSource(_timeout);
            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return Parse<TResponse>(response.StatusCode, content);
                }

                var status = $"HTTP {(int) response.StatusCode} {response.ReasonPhrase}".TrimEnd();
                if ((int) response.StatusCode >= 500 && !isLastAttempt)
                {
                    _logger.LogWarning("{Service} service answered {Status} on attempt {Attempt}/{Attempts} for {Uri}, retrying.", _service.DisplayName(), status, attempt, attempts, uri);
                    continue;
                }

                throw new PostFiscalizationServiceException(attempt > 1 ? $"{status} after {attempt} attempts" : status, Excerpt(content));
            }
            catch (OperationCanceledException ex) when (timeout.IsCancellationRequested)
            {
                if (!isLastAttempt)
                {
                    _logger.LogWarning("{Service} service did not answer within {Timeout} ms on attempt {Attempt}/{Attempts} for {Uri}, retrying.", _service.DisplayName(), _timeout.TotalMilliseconds, attempt, attempts, uri);
                    continue;
                }

                throw new PostFiscalizationServiceException($"timeout after {attempts} attempt(s) of {_timeout.TotalMilliseconds:0} ms each", $"{uri}: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                if (!isLastAttempt)
                {
                    _logger.LogWarning(ex, "{Service} service could not be reached on attempt {Attempt}/{Attempts} for {Uri}, retrying.", _service.DisplayName(), attempt, attempts, uri);
                    continue;
                }

                throw new PostFiscalizationServiceException($"unreachable after {attempts} attempt(s): {ex.Message}", $"{uri}: {ex}", ex);
            }
        }

        throw new InvalidOperationException("Retry loop ended without a result or an exception.");
    }

    private static TResponse Parse<TResponse>(HttpStatusCode statusCode, string content)
    {
        try
        {
            return JsonSerializer.Deserialize<TResponse>(content, _deserializerOptions)
                ?? throw new PostFiscalizationServiceException($"malformed response body (HTTP {(int) statusCode}): the body was empty", Excerpt(content));
        }
        catch (JsonException ex)
        {
            throw new PostFiscalizationServiceException($"malformed response body (HTTP {(int) statusCode}): {ex.Message}", Excerpt(content), ex);
        }
    }

    private static string? Excerpt(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        return content.Length <= _detailExcerptLength ? content : content[.._detailExcerptLength] + "…";
    }
}
