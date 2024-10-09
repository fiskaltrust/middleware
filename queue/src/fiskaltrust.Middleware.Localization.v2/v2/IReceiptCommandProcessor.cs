using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2.v2;

public interface IReceiptCommandProcessor
{
    Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request);
    Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request);
}