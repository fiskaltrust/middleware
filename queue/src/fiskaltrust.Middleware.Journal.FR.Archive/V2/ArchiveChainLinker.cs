using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// Fills <see cref="ArchiveEntry.PreviousSignature"/> and <see cref="ArchiveEntry.StartOfChain"/>.
    /// Neither is a response field: the SCU signs them (ADR 0001), and the LNE SCU stores no copy of its
    /// data set, so the archive takes them from the previous entry of the same chain. Feed every entry
    /// of the queue in queue-row order, including those before the archived period: only the last
    /// signature per chain is kept, so the memory used does not grow with the queue.
    /// </summary>
    public sealed class ArchiveChainLinker
    {
        private readonly Dictionary<string, (long Counter, string Signature)> _last = new Dictionary<string, (long, string)>(StringComparer.Ordinal);

        public void Link(ArchiveEntry entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));

            if (_last.TryGetValue(entry.Chain, out var previous) && previous.Counter == entry.ChainCounter - 1)
            {
                entry.PreviousSignature = previous.Signature;
                entry.StartOfChain = false;
            }
            else
            {
                // No previous entry seen: the start of the chain when the counter is 1, or when the SCU
                // signed that the entry carries over no previous signature (the first v2 entry after a
                // half migration whose v1 signature was not readable, ADR 0003). Otherwise the previous
                // entry lies outside what was fed, and the column stays empty rather than guessed.
                entry.PreviousSignature = null;
                entry.StartOfChain = entry.ChainCounter == 1 || entry.SignedStartOfChain == true;
            }
            _last[entry.Chain] = (entry.ChainCounter, entry.ChainedSignature);
        }
    }
}
