using fiskaltrust.Middleware.Localization.v2.PostFiscalization.Contracts;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.PostFiscalization;

/// <summary>A scriptable eInvoicing/eReporting service that records every call.</summary>
public sealed class FakePostFiscalizationService : IEInvoicingService, IEReportingService
{
    public List<ValidateRequest> ValidateCalls { get; } = [];

    public List<ProcessRequest> ProcessCalls { get; } = [];

    public Func<ValidateRequest, Task<ValidateResponse>> OnValidate { get; set; } = _ => Task.FromResult(new ValidateResponse { Applies = true });

    public Func<ProcessRequest, Task<ProcessResponse>> OnProcess { get; set; } = request => Task.FromResult(new ProcessResponse { ReceiptResponse = request.ReceiptResponse });

    public static FakePostFiscalizationService Applying() => new();

    public static FakePostFiscalizationService NotApplying() => new() { OnValidate = _ => Task.FromResult(new ValidateResponse { Applies = false }) };

    public static FakePostFiscalizationService Rejecting(params string[] errors) => new() { OnValidate = _ => Task.FromResult(new ValidateResponse { Applies = true, Errors = errors.ToList() }) };

    public static FakePostFiscalizationService FailingValidate(Exception exception) => new() { OnValidate = _ => throw exception };

    public static FakePostFiscalizationService FailingProcess(Exception exception) => new() { OnProcess = _ => throw exception };

    public Task<ValidateResponse> ValidateReceiptAsync(ValidateRequest request)
    {
        ValidateCalls.Add(request);
        return OnValidate(request);
    }

    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        ProcessCalls.Add(request);
        return OnProcess(request);
    }
}
