using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDE.Repositories
{
    internal class JournalDERepositoryRangeDecorator : IReadOnlyJournalDERepository
    {
        private readonly IMiddlewareRepository<ftJournalDE> _journalDEMiddlewareRepository;
        private readonly IReadOnlyJournalDERepository _journalDERepository;
        private readonly long _fromInclusive;
        private readonly long _toInclusive;

        public JournalDERepositoryRangeDecorator(IMiddlewareRepository<ftJournalDE> journalDEMiddlewareRepository, IReadOnlyJournalDERepository journalDERepository, long fromInclusive, long toInclusive)
        {
            _journalDEMiddlewareRepository = journalDEMiddlewareRepository;
            _journalDERepository = journalDERepository;
            _fromInclusive = fromInclusive;
            _toInclusive = toInclusive;
        }

        public Task<IEnumerable<ftJournalDE>> GetAsync()
        {
            if (_toInclusive < 0)
            {
                return Task.FromResult(_journalDEMiddlewareRepository.GetEntriesOnOrAfterTimeStampAsync(_fromInclusive, take: (int) -_toInclusive).ToEnumerable());
            }
            else if (_toInclusive == 0)
            {
                return Task.FromResult(_journalDEMiddlewareRepository.GetEntriesOnOrAfterTimeStampAsync(_fromInclusive).ToEnumerable());
            }
            else
            {
                return Task.FromResult(_journalDEMiddlewareRepository.GetByTimeStampRangeAsync(_fromInclusive, _toInclusive).ToEnumerable());
            }
        }

        public async Task<ftJournalDE> GetAsync(Guid id) => await _journalDERepository.GetAsync(id).ConfigureAwait(false);
    }
}
