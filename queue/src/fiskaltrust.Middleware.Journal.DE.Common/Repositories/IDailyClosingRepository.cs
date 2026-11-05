using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.Common.Repositories
{
    public interface IDailyClosingRepository
    {
        Task<List<DailyClosingReceipt>> GetAsync();
    }
}
