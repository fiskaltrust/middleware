using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.PostFiscalization.Contracts;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest.PostFiscalization
{
    /// <summary>A scriptable eInvoicing/eReporting service that records every call.</summary>
    public sealed class FakePostFiscalizationService : IEInvoicingService, IEReportingService
    {
        public List<ValidateRequest> ValidateCalls { get; } = new List<ValidateRequest>();

        public List<ProcessRequest> ProcessCalls { get; } = new List<ProcessRequest>();

        public Func<ValidateRequest, Task<ValidateResponse>> OnValidate { get; set; } = _ => Task.FromResult(new ValidateResponse { Applies = true });

        public Func<ProcessRequest, Task<ProcessResponse>> OnProcess { get; set; } = request => Task.FromResult(new ProcessResponse { ReceiptResponse = request.ReceiptResponse });

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
}
