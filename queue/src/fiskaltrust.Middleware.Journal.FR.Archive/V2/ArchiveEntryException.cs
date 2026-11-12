using System;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// A stored queue item the archive cannot read. An export fails on it rather than leave the entry out
    /// (Ref LNE Ex. 10: the archive is complete, or it is not produced).
    /// </summary>
    public sealed class ArchiveEntryException : Exception
    {
        public ArchiveEntryException(Guid queueItemId, long queueRow, string message, Exception? inner = null)
            : base(message, inner)
        {
            QueueItemId = queueItemId;
            QueueRow = queueRow;
        }

        public Guid QueueItemId { get; }

        public long QueueRow { get; }
    }
}
