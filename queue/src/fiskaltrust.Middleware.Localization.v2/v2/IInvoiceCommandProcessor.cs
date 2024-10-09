using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2.v2;

public interface IInvoiceCommandProcessor
{
    Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request);
    Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request);
}