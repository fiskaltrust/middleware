using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface IJournalProcessor
    {
        IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request);
    }
}