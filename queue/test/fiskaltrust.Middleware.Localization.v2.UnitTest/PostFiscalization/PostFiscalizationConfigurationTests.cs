using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.PostFiscalization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.PostFiscalization;

public class PostFiscalizationConfigurationTests
{
    private static MiddlewareConfiguration Middleware(Dictionary<string, object> configuration) => new()
    {
        QueueId = Guid.NewGuid(),
        CashBoxId = Guid.NewGuid(),
        Configuration = configuration,
    };

    private static PostFiscalizationConfiguration FromMiddleware(MiddlewareConfiguration middlewareConfiguration)
        => PostFiscalizationConfiguration.FromConfiguration(middlewareConfiguration.Configuration);

    private static PostFiscalizationProcessor Processor(MiddlewareConfiguration middlewareConfiguration)
        => new(NullLogger<PostFiscalizationProcessor>.Instance, FromMiddleware(middlewareConfiguration), middlewareConfiguration.CashBoxId, middlewareConfiguration.Configuration, middlewareConfiguration.IsSandbox);

    [Fact]
    public void FromMiddlewareConfiguration_WithoutSections_IsDisabled()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object>
        {
            ["scu-timeout-ms"] = 5000,
            ["cashboxid"] = Guid.NewGuid().ToString(),
        }));

        configuration.IsEnabled.Should().BeFalse();
        configuration.EInvoicing.Should().BeNull();
        configuration.EReporting.Should().BeNull();
        configuration.Invoking(c => c.Validate()).Should().NotThrow();
    }

    [Fact]
    public void FromMiddlewareConfiguration_WithNullDictionary_IsDisabled()
    {
        var configuration = FromMiddleware(new MiddlewareConfiguration { Configuration = null });

        configuration.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void FromMiddlewareConfiguration_ParsesNewtonsoftSections()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object>
        {
            ["einvoicing"] = JObject.Parse("""{ "endpoint": "https://einvoicing.example.com/v2", "timeout-ms": 20000, "max-retries": 3 }"""),
            ["ereporting"] = JObject.Parse("""{ "endpoint": "https://ereporting.example.com/v2" }"""),
        }));

        configuration.IsEnabled.Should().BeTrue();
        configuration.EInvoicing!.Endpoint.Should().Be("https://einvoicing.example.com/v2");
        configuration.EInvoicing.TimeoutMs.Should().Be(20000);
        configuration.EInvoicing.Timeout.Should().Be(TimeSpan.FromSeconds(20));
        configuration.EInvoicing.MaxRetries.Should().Be(3);
        configuration.EInvoicing.EffectiveMaxRetries.Should().Be(3);
        configuration.EReporting!.Endpoint.Should().Be("https://ereporting.example.com/v2");
        configuration.EReporting.TimeoutMs.Should().BeNull();
        configuration.EReporting.MaxRetries.Should().BeNull();
    }

    [Fact]
    public void FromMiddlewareConfiguration_ParsesSystemTextJsonSections()
    {
        var element = JsonSerializer.Deserialize<JsonElement>("""{ "endpoint": "https://einvoicing.example.com/v2", "timeout-ms": 1000 }""");

        var configuration = FromMiddleware(Middleware(new Dictionary<string, object> { ["einvoicing"] = element }));

        configuration.EInvoicing!.Endpoint.Should().Be("https://einvoicing.example.com/v2");
        configuration.EInvoicing.TimeoutMs.Should().Be(1000);
        configuration.EReporting.Should().BeNull();
    }

    [Fact]
    public void FromMiddlewareConfiguration_ParsesJsonStringSections()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object>
        {
            ["ereporting"] = """{ "endpoint": "https://ereporting.example.com/v2", "max-retries": 0 }""",
        }));

        configuration.EInvoicing.Should().BeNull();
        configuration.EReporting!.Endpoint.Should().Be("https://ereporting.example.com/v2");
        configuration.EReporting.EffectiveMaxRetries.Should().Be(0);
    }

    [Fact]
    public void FromMiddlewareConfiguration_ParsesNestedDictionarySections()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object>
        {
            ["einvoicing"] = new Dictionary<string, object> { ["endpoint"] = "https://einvoicing.example.com/v2", ["timeout-ms"] = 2500 },
        }));

        configuration.EInvoicing!.Endpoint.Should().Be("https://einvoicing.example.com/v2");
        configuration.EInvoicing.TimeoutMs.Should().Be(2500);
    }

    [Fact]
    public void FromMiddlewareConfiguration_MatchesSectionKeysCaseInsensitively()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object>
        {
            ["EInvoicing"] = JObject.Parse("""{ "Endpoint": "https://einvoicing.example.com/v2" }"""),
        }));

        configuration.EInvoicing!.Endpoint.Should().Be("https://einvoicing.example.com/v2");
    }

    [Fact]
    public void FromMiddlewareConfiguration_TreatsNullSectionAsDisabled()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object>
        {
            ["einvoicing"] = JValue.CreateNull(),
        }));

        configuration.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void FromMiddlewareConfiguration_ThrowsForUnparsableSection()
    {
        var act = () => FromMiddleware(Middleware(new Dictionary<string, object> { ["einvoicing"] = "this is not json" }));

        act.Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*'einvoicing'*could not be parsed*");
    }

    [Fact]
    public void Defaults_Are15SecondsPerAttemptAndOneRetry()
    {
        var section = new PostFiscalizationServiceConfiguration { Endpoint = "https://einvoicing.example.com/v2" };

        section.Timeout.Should().Be(TimeSpan.FromMilliseconds(15000));
        section.EffectiveMaxRetries.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsSectionWithoutServiceOrEndpoint(string? endpoint)
    {
        var configuration = new PostFiscalizationConfiguration { EInvoicing = new PostFiscalizationServiceConfiguration { Endpoint = endpoint } };

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*'einvoicing'*names no 'service'*'government-it'*");
    }

    [Fact]
    public void Validate_RejectsEmptySection()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object> { ["ereporting"] = new JObject() }));

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*'ereporting'*names no 'service'*");
    }

    [Fact]
    public void Validate_RejectsAnUnknownService()
    {
        var configuration = new PostFiscalizationConfiguration { EInvoicing = new PostFiscalizationServiceConfiguration { Service = "government-xx" } };

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*service 'government-xx' is unknown*'government-it'*");
    }

    [Theory]
    [InlineData("government-it")]
    [InlineData("Government-IT")]
    [InlineData(" government-it ")]
    public void Validate_AcceptsAKnownService_CaseInsensitively(string service)
    {
        var configuration = new PostFiscalizationConfiguration { EInvoicing = new PostFiscalizationServiceConfiguration { Service = service } };

        configuration.Invoking(c => c.Validate()).Should().NotThrow();
    }

    [Fact]
    public void ResolveEndpoint_UsesTheSandboxOrProductionEndpointOfTheKnownService()
    {
        var section = new PostFiscalizationServiceConfiguration { Service = KnownPostFiscalizationServices.GovernmentIt };

        section.ResolveEndpoint("einvoicing", isSandbox: true).Should().Be(new Uri("https://government-sandbox.fiskaltrust.it/v2/einvoicing"));
        section.ResolveEndpoint("einvoicing", isSandbox: false).Should().Be(new Uri("https://government.fiskaltrust.it/v2/einvoicing"));
        section.ResolveEndpoint("ereporting", isSandbox: true).Should().Be(new Uri("https://government-sandbox.fiskaltrust.it/v2/ereporting"), "the section a service is configured under is the concern segment");
    }

    [Fact]
    public void ResolveEndpoint_AnEndpointOverrideWinsOverTheService()
    {
        var section = new PostFiscalizationServiceConfiguration { Service = KnownPostFiscalizationServices.GovernmentIt, Endpoint = "http://localhost:5000/einvoicing" };

        section.ResolveEndpoint("einvoicing", isSandbox: true).Should().Be(new Uri("http://localhost:5000/einvoicing"));
    }

    [Fact]
    public void FromMiddlewareConfiguration_ParsesTheServiceKey()
    {
        var configuration = FromMiddleware(Middleware(new Dictionary<string, object>
        {
            ["einvoicing"] = JObject.Parse("""{ "service": "government-it" }"""),
        }));

        configuration.EInvoicing!.Service.Should().Be("government-it");
        configuration.EInvoicing.Endpoint.Should().BeNull();
        configuration.Invoking(c => c.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData("http://einvoicing.example.com/v2")]
    [InlineData("http://10.0.0.5:8080/v2")]
    public void Validate_RejectsPlainHttpForNonLoopbackEndpoints(string endpoint)
    {
        var configuration = new PostFiscalizationConfiguration { EReporting = new PostFiscalizationServiceConfiguration { Endpoint = endpoint } };

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*must use https*loopback*");
    }

    [Theory]
    [InlineData("http://localhost:5000/v2")]
    [InlineData("http://127.0.0.1/v2")]
    [InlineData("http://127.0.0.55:1234/einvoicing")]
    [InlineData("http://[::1]:8080/v2")]
    [InlineData("https://einvoicing.example.com/v2")]
    [InlineData("HTTPS://einvoicing.example.com/v2/")]
    public void Validate_AcceptsHttpsAndLoopbackHttp(string endpoint)
    {
        var configuration = new PostFiscalizationConfiguration { EInvoicing = new PostFiscalizationServiceConfiguration { Endpoint = endpoint } };

        configuration.Invoking(c => c.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData("grpc://einvoicing.example.com:10000")]
    [InlineData("ftp://einvoicing.example.com/v2")]
    public void Validate_RejectsUnsupportedSchemes(string endpoint)
    {
        var configuration = new PostFiscalizationConfiguration { EInvoicing = new PostFiscalizationServiceConfiguration { Endpoint = endpoint } };

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*unsupported scheme*");
    }

    [Fact]
    public void Validate_RejectsRelativeOrGarbageEndpoints()
    {
        var configuration = new PostFiscalizationConfiguration { EInvoicing = new PostFiscalizationServiceConfiguration { Endpoint = "not a url" } };

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*not a valid absolute URL*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsNonPositiveTimeout(long timeoutMs)
    {
        var configuration = new PostFiscalizationConfiguration { EInvoicing = new PostFiscalizationServiceConfiguration { Endpoint = "https://einvoicing.example.com", TimeoutMs = timeoutMs } };

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*'timeout-ms'*positive*");
    }

    [Fact]
    public void Validate_RejectsNegativeRetries()
    {
        var configuration = new PostFiscalizationConfiguration { EReporting = new PostFiscalizationServiceConfiguration { Endpoint = "https://ereporting.example.com", MaxRetries = -1 } };

        configuration.Invoking(c => c.Validate()).Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*'max-retries'*not be negative*");
    }

    [Fact]
    public void Processor_FromConfiguration_FailsQueueStartupForMisconfiguredSection()
    {
        var middleware = Middleware(new Dictionary<string, object>
        {
            ["einvoicing"] = JObject.Parse("""{ "endpoint": "http://einvoicing.example.com/v2" }"""),
            ["accesstoken"] = "token",
        });

        var act = () => Processor(middleware);

        act.Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*must use https*");
    }

    [Fact]
    public void Processor_FromConfiguration_FailsQueueStartupWhenAccessTokenIsMissing()
    {
        var middleware = Middleware(new Dictionary<string, object>
        {
            ["einvoicing"] = JObject.Parse("""{ "endpoint": "https://einvoicing.example.com/v2" }"""),
        });

        var act = () => Processor(middleware);

        act.Should().Throw<PostFiscalizationConfigurationException>().WithMessage("*'accesstoken'*");
    }

    [Fact]
    public void Processor_FromConfiguration_IsEnabledForValidSections()
    {
        var middleware = Middleware(new Dictionary<string, object>
        {
            ["einvoicing"] = JObject.Parse("""{ "endpoint": "https://einvoicing.example.com/v2" }"""),
            ["ereporting"] = JObject.Parse("""{ "endpoint": "http://localhost:5001/ereporting" }"""),
            ["cashboxid"] = Guid.NewGuid().ToString(),
            ["accesstoken"] = "token",
        });

        var processor = Processor(middleware);

        processor.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Processor_FromConfiguration_WithoutSections_IsDisabledAndNeedsNoAccessToken()
    {
        var middleware = Middleware(new Dictionary<string, object>());

        var processor = Processor(middleware);

        processor.IsEnabled.Should().BeFalse();
    }
}
