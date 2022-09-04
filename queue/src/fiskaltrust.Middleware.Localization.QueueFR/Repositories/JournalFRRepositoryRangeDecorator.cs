using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.Repositories
{
    internal class JournalFRRepositoryRangeDecorator
    {
        private readonly IMiddlewareJournalFRRepository _journalFRRepository;
        private readonly long _fromInclusive;
        private readonly long _toInclusive;
        private readonly string _receiptType;

        public JournalFRRepositoryRangeDecorator(IMiddlewareJournalFRRepository journalFRRepository, long fromInclusive, long toInclusive, string receiptType)
        {
            _journalFRRepository = journalFRRepository;
            _fromInclusive = fromInclusive;
            _toInclusive = toInclusive;
            _receiptType = receiptType;
        }

        public IAsyncEnumerable<ftJournalFR> GetAsync()
        {
            if (_toInclusive < 0)
            {
                return _journalFRRepository.GetEntriesOnOrAfterTimeStampAsync(_fromInclusive, take: (int) -_toInclusive).Where(x => x.ReceiptType == _receiptType);
            }
            else if (_toInclusive == 0)
            {
                return _journalFRRepository.GetEntriesOnOrAfterTimeStampAsync(_fromInclusive).Where(x => x.ReceiptType == _receiptType);
            }
            else
            {
                return _journalFRRepository.GetByTimeStampRangeAsync(_fromInclusive, _toInclusive).Where(x => x.ReceiptType == _receiptType);
            }
        }
    }
}
