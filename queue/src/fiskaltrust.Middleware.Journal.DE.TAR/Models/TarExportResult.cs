using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Journal.DE.TAR.Models
{
    /// <summary>
    /// What <see cref="TarExporter.ExportAsync"/> did with the JournalDE files it was given.
    /// </summary>
    public sealed class TarExportResult
    {
        public TarExportResult(int journalFilesRead, IReadOnlyList<TarExportFailure> failures)
        {
            if (failures == null)
            {
                throw new ArgumentNullException(nameof(failures));
            }

            JournalFilesRead = journalFilesRead;
            Failures = failures;
        }

        /// <summary>
        /// The number of JournalDE files the repository delivered.
        /// </summary>
        public int JournalFilesRead { get; }

        /// <summary>
        /// The number of JournalDE files whose content was appended to the archive.
        /// </summary>
        public int JournalFilesPacked => JournalFilesRead - Failures.Count;

        /// <summary>
        /// The JournalDE files that could not be appended to the archive.
        /// </summary>
        public IReadOnlyList<TarExportFailure> Failures { get; }

        public bool IsComplete => Failures.Count == 0;
    }
}
