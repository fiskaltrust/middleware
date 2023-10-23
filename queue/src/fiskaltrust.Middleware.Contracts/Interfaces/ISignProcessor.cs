using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface ISignProcessor
    {
        Task<ReceiptResponse> ProcessAsync(ReceiptRequest request);
    }
}