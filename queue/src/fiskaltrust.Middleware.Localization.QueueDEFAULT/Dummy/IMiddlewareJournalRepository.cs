using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Dummy
{
    public interface IMiddlewareJournalRepository
    {
        Task<ftJournalIT> GetByQueueItemId(Guid queueItemId);
    }
}