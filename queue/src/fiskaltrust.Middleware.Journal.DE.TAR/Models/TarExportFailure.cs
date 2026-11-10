using System;

namespace fiskaltrust.Middleware.Journal.DE.TAR.Models
{
    /// <summary>
    /// A JournalDE file whose content could not be appended to the TAR archive.
    /// </summary>
    public sealed class TarExportFailure
    {
        public TarExportFailure(Guid journalDEId, string fileName, Exception exception)
        {
            JournalDEId = journalDEId;
            FileName = fileName;
            Exception = exception;
        }

        public Guid JournalDEId { get; }

        public string FileName { get; }

        public Exception Exception { get; }

        public override string ToString() => $"{FileName} (ftJournalDEId {JournalDEId}): {Exception.GetType().Name}: {Exception.Message}";
    }
}
