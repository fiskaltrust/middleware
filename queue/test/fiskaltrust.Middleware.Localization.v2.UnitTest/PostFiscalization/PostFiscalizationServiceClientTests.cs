using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.PostFiscalization;
using fiskaltrust.Middleware.Localization.v2.PostFiscalization.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.PostFiscalization;

public class PostFiscalizationServiceClientTests
{
    private static readonly Guid _cashBoxId = Guid.Parse("5f9a1c72-3e4b-4a21-9c8f-2b7d6e5a1f30");
    private const string _accessToken = "secret-token";

    private sealed record RecordedRequest(Uri Uri, HttpMethod Method, Dictionary<string, string> Headers, string? ContentType, string Body);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<int, CancellationToken, Task<HttpResponseMessage>> _respond;

        public StubHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> respond) => _respond = respond;

        public List<RecordedRequest> Requests { get; } = [];

        public static StubHandler Returning(params Func<HttpResponseMessage>[] responses)
            => new((attempt, _) => Task.FromResult(responses[Math.Min(attempt, responses.Length) - 1]()));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase);
            Requests.Add(new RecordedRequest(request.RequestUri!, request.Method, headers, request.Content?.Headers.ContentType?.ToString(), body));
            return await _respond(Requests.Count, cancellationToken);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static PostFiscalizationServiceClient CreateClient(StubHandler handler, string endpoint = "https://einvoicing.example.com/v2/", long? timeoutMs = null, int? maxRetries = null, PostFiscalizationService service = PostFiscalizationService.EInvoicing)
        => new(service, new PostFiscalizationServiceConfiguration { Endpoint = endpoint, TimeoutMs = timeoutMs, MaxRetries = maxRetries }, _cashBoxId, _accessToken, NullLogger.Instance, handler);

    private static ReceiptRequest Request(string reference = "R-2026-0001") => new()
    {
        ftCashBoxID = _cashBoxId,
        cbReceiptReference = reference,
        cbTerminalID = "T1",
        ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001,
        cbChargeItems = [],
        cbPayItems = [],
    };

    [Fact]
    public async Task ValidateReceiptAsync_PostsToValidateRoute_WithCashboxHeadersAndJsonBody()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, """{ "Applies": true, "Errors": [] }"""));
        using var client = CreateClient(handler);

        var response = await client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() });

        response.Applies.Should().BeTrue();
        response.Errors.Should().BeEmpty();
        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Post);
        request.Uri.Should().Be(new Uri("https://einvoicing.example.com/v2/validate"));
        request.Headers[PostFiscalizationServiceClient.CashBoxIdHeader].Should().Be(_cashBoxId.ToString());
        request.Headers[PostFiscalizationServiceClient.AccessTokenHeader].Should().Be(_accessToken);
        request.ContentType.Should().StartWith("application/json");
        using var body = JsonDocument.Parse(request.Body);
        body.RootElement.GetProperty("ReceiptRequest").GetProperty("cbReceiptReference").GetString().Should().Be("R-2026-0001");
        body.RootElement.TryGetProperty("ReceiptResponse", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessReceiptAsync_PostsToProcessRoute_AndReturnsTheParsedResponse()
    {
        var queueItemId = Guid.NewGuid();
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, $$"""
            { "ReceiptResponse": { "ftQueueItemID": "{{queueItemId}}", "ftReceiptIdentification": "ft1A2B#",
              "ftSignatures": [ { "ftSignatureFormat": 1, "ftSignatureType": 0, "Caption": "einvoice-id", "Data": "urn:peppol:1" } ] } }
            """));
        using var client = CreateClient(handler, endpoint: "https://einvoicing.example.com/v2");

        var response = await client.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = Request(),
            ReceiptResponse = new ReceiptResponse { ftQueueItemID = queueItemId, ftReceiptIdentification = "ft1A2B#" },
        });

        response.ReceiptResponse.ftQueueItemID.Should().Be(queueItemId);
        response.ReceiptResponse.ftSignatures.Should().ContainSingle(signature => signature.Caption == "einvoice-id" && signature.Data == "urn:peppol:1");
        var request = handler.Requests.Should().ContainSingle().Subject;
        request.Uri.Should().Be(new Uri("https://einvoicing.example.com/v2/process"));
        using var body = JsonDocument.Parse(request.Body);
        body.RootElement.GetProperty("ReceiptRequest").GetProperty("cbReceiptReference").GetString().Should().Be("R-2026-0001");
        body.RootElement.GetProperty("ReceiptResponse").GetProperty("ftQueueItemID").GetGuid().Should().Be(queueItemId);
    }

    [Fact]
    public async Task Body_UsesRelaxedJsonEscaping_LikeTheSignEndpoint()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, """{ "Applies": true }"""));
        using var client = CreateClient(handler);

        await client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request("R<1>&ä") });

        handler.Requests.Single().Body.Should().Contain("\"cbReceiptReference\":\"R<1>&ä\"");
    }

    [Fact]
    public async Task EReportingClient_UsesItsOwnEndpoint_LoopbackHttpIsAllowed()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, """{ "Applies": false }"""));
        using var client = CreateClient(handler, endpoint: "http://localhost:5001/ereporting", service: PostFiscalizationService.EReporting);

        var response = await client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() });

        response.Applies.Should().BeFalse();
        handler.Requests.Single().Uri.Should().Be(new Uri("http://localhost:5001/ereporting/validate"));
    }

    [Fact]
    public async Task ParsesResponsePropertyNamesCaseInsensitively()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, """{ "applies": false, "errors": [ "customer missing" ] }"""));
        using var client = CreateClient(handler);

        var response = await client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() });

        response.Applies.Should().BeFalse();
        response.Errors.Should().Equal("customer missing");
    }

    [Fact]
    public async Task Retries_On5xx_ThenSucceeds()
    {
        var handler = StubHandler.Returning(
            () => Json(HttpStatusCode.ServiceUnavailable, "down"),
            () => Json(HttpStatusCode.OK, """{ "Applies": true }"""));
        using var client = CreateClient(handler);

        var response = await client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() });

        response.Applies.Should().BeTrue();
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Retries_On5xx_OnlyUpToMaxRetries_ThenThrowsWithDetail()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.InternalServerError, """{ "error": "database is down" }"""));
        using var client = CreateClient(handler, maxRetries: 2);

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() }));

        handler.Requests.Should().HaveCount(3);
        exception.Message.Should().Contain("HTTP 500").And.Contain("after 3 attempts");
        exception.Detail.Should().Contain("database is down");
    }

    [Fact]
    public async Task DoesNotRetry_On4xx()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.BadRequest, """{ "error": "unknown cashbox" }"""));
        using var client = CreateClient(handler, maxRetries: 3);

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = Request(), ReceiptResponse = new ReceiptResponse() }));

        handler.Requests.Should().ContainSingle();
        exception.Message.Should().Be("HTTP 400 Bad Request");
        exception.Detail.Should().Contain("unknown cashbox");
    }

    [Fact]
    public async Task DoesNotRetry_OnMalformedBody()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, "<html>not json</html>"));
        using var client = CreateClient(handler, maxRetries: 3);

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() }));

        handler.Requests.Should().ContainSingle();
        exception.Message.Should().StartWith("malformed response body (HTTP 200)");
        exception.Detail.Should().Contain("<html>");
    }

    [Fact]
    public async Task DoesNotRetry_WhenARequiredPropertyIsMissing()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, """{ "Errors": [] }"""));
        using var client = CreateClient(handler, maxRetries: 3);

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() }));

        handler.Requests.Should().ContainSingle();
        exception.Message.Should().StartWith("malformed response body");
    }

    [Fact]
    public async Task ProcessResponseWithoutReceiptResponse_IsMalformed()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.OK, """{ }"""));
        using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = Request(), ReceiptResponse = new ReceiptResponse() }));

        exception.Message.Should().StartWith("malformed response body");
    }

    [Fact]
    public async Task Retries_OnTimeout_AndCancelsEachAttempt()
    {
        var observedTokens = new List<CancellationToken>();
        var handler = new StubHandler(async (_, cancellationToken) =>
        {
            observedTokens.Add(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json(HttpStatusCode.OK, """{ "Applies": true }""");
        });
        using var client = CreateClient(handler, timeoutMs: 150, maxRetries: 1);
        var stopwatch = Stopwatch.StartNew();

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() }));

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        handler.Requests.Should().HaveCount(2);
        observedTokens.Should().HaveCount(2).And.OnlyContain(token => token.IsCancellationRequested);
        exception.Message.Should().Be("timeout after 2 attempt(s) of 150 ms each");
    }

    [Fact]
    public async Task Retries_OnConnectionFailure_ThenThrowsUnreachable()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("Connection refused"));
        using var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() }));

        handler.Requests.Should().HaveCount(2);
        exception.Message.Should().Be("unreachable after 2 attempt(s): Connection refused");
        exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task ZeroRetries_MeansExactlyOneAttempt()
    {
        var handler = StubHandler.Returning(() => Json(HttpStatusCode.ServiceUnavailable, ""));
        using var client = CreateClient(handler, maxRetries: 0);

        var exception = await Assert.ThrowsAsync<PostFiscalizationServiceException>(() => client.ValidateReceiptAsync(new ValidateRequest { ReceiptRequest = Request() }));

        handler.Requests.Should().ContainSingle();
        exception.Message.Should().Be("HTTP 503 Service Unavailable");
        exception.Detail.Should().BeNull();
    }

    [Fact]
    public void Constructor_ValidatesTheEndpoint()
    {
        var act = () => CreateClient(StubHandler.Returning(() => Json(HttpStatusCode.OK, "{}")), endpoint: "http://einvoicing.example.com/v2");

        act.Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*must use https*");
    }
}
