using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.TAR.Helpers;
using fiskaltrust.Middleware.Journal.DE.TAR.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.TAR
{
    public class TarExporter
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyJournalDERepository _journalDERepository;

        public TarExporter(ILogger logger, IReadOnlyJournalDERepository journalDERepository)
        {
            _logger = logger;
            _journalDERepository = journalDERepository;
        }

        /// <summary>
        /// Appends the TAR content of every JournalDE the repository delivers to <paramref name="targetFile"/>. A JournalDE
        /// that cannot be appended does not stop the export, but it is reported in the result: the caller decides whether an
        /// archive that is missing files may be delivered.
        /// </summary>
        public async Task<TarExportResult> ExportAsync(string targetFile)
        {
            _logger.LogInformation("Started TAR file export.");

            var journalFilesRead = 0;
            var failures = new List<TarExportFailure>();

            using (var tarArchive = new TarArchive(targetFile))
            {
                foreach (var journal in await _journalDERepository.GetAsync())
                {
                    journalFilesRead++;
                    var temporaryTarPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".tar";

                    try
                    {
                        ZipHelper.ExtractBase64ZipContentToDisk(journal.FileContentBase64, temporaryTarPath);
                        tarArchive.AppendExistingTarFile(temporaryTarPath);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error occurred while trying to append the TAR file {TarFileName} (ID: {TarFileId}). See below for further information.",
                            journal.FileName, journal.ftJournalDEId);
                        failures.Add(new TarExportFailure(journal.ftJournalDEId, journal.FileName, e));
                    }
                    finally
                    {
                        if (File.Exists(temporaryTarPath))
                        {
                            File.Delete(temporaryTarPath);
                        }
                    }
                }
            }

            var result = new TarExportResult(journalFilesRead, failures);
            _logger.LogInformation("TAR export finished: {PackedJournalFiles} of {ReadJournalFiles} JournalDE files were packed.", result.JournalFilesPacked, result.JournalFilesRead);
            return result;
        }
    }
}
