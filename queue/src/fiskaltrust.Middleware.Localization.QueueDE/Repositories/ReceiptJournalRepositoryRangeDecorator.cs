using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDE.Repositories
{
    internal class ReceiptJournalRepositoryRangeDecorator : IReadOnlyReceiptJournalRepository
    {
        private readonly IMiddlewareRepository<ftReceiptJournal> _receiptJournalMiddlewareRepository;
        private readonly IReadOnlyReceiptJournalRepository _receiptJournalRepository;
        private readonly long _fromInclusive;
        private readonly long _toInclusive;

        public ReceiptJournalRepositoryRangeDecorator(IMiddlewareRepository<ftReceiptJournal> receiptJournalMiddlewareRepository, IReadOnlyReceiptJournalRepository receiptJournalRepository, long fromInclusive, long toInclusive)
        {
            _receiptJournalMiddlewareRepository = receiptJournalMiddlewareRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _fromInclusive = fromInclusive;
            _toInclusive = toInclusive;
        }

        public Task<IEnumerable<ftReceiptJournal>> GetAsync()
        {
            if (_toInclusive < 0)
            {
                return Task.FromResult(_receiptJournalMiddlewareRepository.GetEntriesOnOrAfterTimeStampAsync(_fromInclusive, take: (int) -_toInclusive).ToEnumerable());
            }
            else if (_toInclusive == 0)
            {
                return Task.FromResult(_receiptJournalMiddlewareRepository.GetEntriesOnOrAfterTimeStampAsync(_fromInclusive).ToEnumerable());
            }
            else
            {
                return Task.FromResult(_receiptJournalMiddlewareRepository.GetByTimeStampRangeAsync(_fromInclusive, _toInclusive).ToEnumerable());
            }
        }

        public async Task<ftReceiptJournal> GetAsync(Guid id) => await _receiptJournalRepository.GetAsync(id).ConfigureAwait(false);
    }
}
