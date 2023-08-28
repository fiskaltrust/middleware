using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueIT.Services
{
    public interface IITSSCDProvider
    {
        Task RegisterCurrentScuAsync();

        Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

        Task<RTInfo> GetRTInfoAsync();
    }
}
