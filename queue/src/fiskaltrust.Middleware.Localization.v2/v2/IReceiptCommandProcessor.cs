using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IReceiptCommandProcessor
{
    public Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request);
}