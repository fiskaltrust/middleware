using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public interface IITSSCDProvider
    {
        Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

        Task<RTInfo> GetRTInfoAsync();
    }
}
