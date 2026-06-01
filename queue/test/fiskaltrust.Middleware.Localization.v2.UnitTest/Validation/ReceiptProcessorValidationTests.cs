using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class ReceiptProcessorValidationTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ReceiptRequest CreateRequest() => new()
    {
        ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private static ftQueue CreateQueue() => new() { CountryCode = "ES" };
    private static ftQueueItem CreateQueueItem() => new();

    private static ValidationFailure Failure(FluentValidation.Severity severity, string code = "TestCode", string message = "Test message") =>
        new("field", message) { Severity = severity, ErrorCode = code };

    private static IMarketValidator ValidatorReturning(params ValidationFailure[] failures)
    {
        var mock = new Mock<IMarketValidator>();
        var result = new ValidationResult(failures);
        mock.Setup(x => x.ValidateAsync(It.IsAny<ReceiptRequest>(), It.IsAny<ftQueue>(), It.IsAny<ReceiptResponse>(), It.IsAny<object>()))
            .ReturnsAsync(result);
        mock.Setup(x => x.LastResult).Returns(result);
        return mock.Object;
    }

    private static IReceiptCommandProcessor ProcessorReturning(ReceiptResponse response)
    {
        var mock = new Mock<IReceiptCommandProcessor>();
        mock.Setup(x => x.PointOfSaleReceipt0x0001Async(It.IsAny<ProcessCommandRequest>()))
            .ReturnsAsync(new ProcessCommandResponse(response, []));
        return mock.Object;
    }

    private sealed class CapturingLogger : ILogger<ReceiptProcessor>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    private static (ReceiptProcessor processor, CapturingLogger logger) CreateProcessor(
        IMarketValidator validator,
        ValidationConfiguration? config = null,
        IReceiptCommandProcessor? receiptCommandProcessor = null)
    {
        var logger = new CapturingLogger();
        receiptCommandProcessor ??= ProcessorReturning(new ReceiptResponse());

        var processor = new ReceiptProcessor(
            logger,
            validator,
            Mock.Of<ILifecycleCommandProcessor>(),
            receiptCommandProcessor,
            Mock.Of<IDailyOperationsCommandProcessor>(),
            Mock.Of<IInvoiceCommandProcessor>(),
            Mock.Of<IProtocolCommandProcessor>(),
            config);

        return (processor, logger);
    }

    // ─── ValidationLevel = null ────────────────────────────────────────────────

    [Fact]
    public async Task ValidationLevel_Null_ErrorSeverity_ReceiptProceeds()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Error)),
            config: null);

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
    }

    [Fact]
    public async Task ValidationLevel_Null_WarningSeverity_ReceiptProceeds()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Warning)),
            config: null);

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
    }

    // ─── ValidationLevel = Error ───────────────────────────────────────────────

    [Fact]
    public async Task ValidationLevel_Error_ErrorSeverity_ReceiptFails()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Error)),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Error });

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task ValidationLevel_Error_WarningSeverityOnly_ReceiptProceeds()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Warning)),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Error });

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
    }

    // ─── ValidationLevel = Warning ─────────────────────────────────────────────

    [Fact]
    public async Task ValidationLevel_Warning_WarningSeverity_ReceiptFails()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Warning)),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Warning });

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task ValidationLevel_Warning_InfoSeverityOnly_ReceiptProceeds()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Info)),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Warning });

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
    }

    // ─── ValidationLevel = Info ────────────────────────────────────────────────

    [Fact]
    public async Task ValidationLevel_Info_InfoSeverity_ReceiptFails()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Info)),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Info });

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeTrue();
    }

    // ─── Signatures on failure ─────────────────────────────────────────────────

    [Fact]
    public async Task ValidationFails_SignaturesAlwaysAdded_WhenValidationsInSignaturesFalse()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Error, "ErrCode", "err msg")),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Error, ValidationsInSignatures = false });

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftSignatures.Should().NotBeEmpty();
        response.ftSignatures.Should().ContainSingle(s => s.Caption!.Contains("ErrCode") && s.Data == "err msg");
    }

    [Fact]
    public async Task ValidationFails_AllErrorsAppearInSignatures()
    {
        var (processor, _) = CreateProcessor(
            ValidatorReturning(
                Failure(FluentValidation.Severity.Error, "Code1", "msg1"),
                Failure(FluentValidation.Severity.Error, "Code2", "msg2")),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Error });

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftSignatures.Should().HaveCount(2);
    }

    // ─── ValidationsInSignatures ───────────────────────────────────────────────

    [Fact]
    public async Task ValidationsInSignatures_True_SignaturesAdded_WhenReceiptProceeds()
    {
        var processorResponse = new ReceiptResponse();
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Warning, "WarnCode")),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Error, ValidationsInSignatures = true },
            ProcessorReturning(processorResponse));

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftSignatures.Should().ContainSingle(s => s.Caption!.Contains("WarnCode"));
    }

    [Fact]
    public async Task ValidationsInSignatures_False_NoSignatures_WhenReceiptProceeds()
    {
        var processorResponse = new ReceiptResponse();
        var (processor, _) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Warning, "WarnCode")),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Error, ValidationsInSignatures = false },
            ProcessorReturning(processorResponse));

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftSignatures.Should().BeEmpty();
    }

    // ─── Severity-matched logging ──────────────────────────────────────────────

    [Fact]
    public async Task ErrorSeverity_LogsAtErrorLevel()
    {
        var (processor, logger) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Error)));

        await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task WarningSeverity_LogsAtWarningLevel()
    {
        var (processor, logger) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Warning)));

        await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task InfoSeverity_LogsAtInformationLevel()
    {
        var (processor, logger) = CreateProcessor(
            ValidatorReturning(Failure(FluentValidation.Severity.Info)));

        await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Information);
    }

    // ─── No validation errors ──────────────────────────────────────────────────

    [Fact]
    public async Task NoValidationErrors_ReceiptProceeds_NoSignatures()
    {
        var processorResponse = new ReceiptResponse();
        var (processor, _) = CreateProcessor(
            ValidatorReturning(),
            new ValidationConfiguration { ValidationLevel = ValidationLevel.Error, ValidationsInSignatures = true },
            ProcessorReturning(processorResponse));

        var (response, _) = await processor.ProcessAsync(CreateRequest(), new ReceiptResponse(), CreateQueue(), CreateQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
        response.ftSignatures.Should().BeEmpty();
    }
}
