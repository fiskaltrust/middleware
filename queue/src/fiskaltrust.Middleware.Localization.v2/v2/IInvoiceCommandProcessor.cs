using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IInvoiceCommandProcessor
{
    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request);
}