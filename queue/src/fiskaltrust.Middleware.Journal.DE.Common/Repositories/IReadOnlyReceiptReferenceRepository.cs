using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.Common.Repositories
{
    public interface IReadOnlyReceiptReferenceRepository
    {
        Task<HashSet<ReceiptReferencesGroupedData>> GetReceiptReferenceAsync(long? from, long? to, List<DailyClosingReceipt> dailyClosings);
    }
}
