using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Constants;
using Microsoft.Extensions.Logging;
using SharpCompress.Readers;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public class TarFileCleanupService : ITarFileCleanupService
    {
        private readonly ILogger<TarFileCleanupService> _logger;
        private readonly IMiddlewareJournalDERepository _journalDERepository;
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        protected bool _storeTemporaryExportFiles = false;

        public TarFileCleanupService(ILogger<TarFileCleanupService> logger, IMiddlewareJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration)
        {
            _logger = logger;
            _journalDERepository = journalDERepository;
            _middlewareConfiguration = middlewareConfiguration;


            if (_middlewareConfiguration.Configuration.ContainsKey(ConfigurationKeys.STORE_TEMPORARY_FILES_KEY))
            {
                _storeTemporaryExportFiles = bool.TryParse(_middlewareConfiguration.Configuration[ConfigurationKeys.STORE_TEMPORARY_FILES_KEY].ToString(), out var val) && val;
            }
        }

        public async Task CleanupTarFileAsync(Guid journalDEId, string filePath, string checkSum, bool useSharpCompress = false)
        {
            var dbJournalDE = await _journalDERepository.GetAsync(journalDEId).ConfigureAwait(false);

            var uploadSuccess = false;

            try
            {
                var dbCheckSum = useSharpCompress
                                    ? GetHashFromCompressedBase64WithSharpCompress(dbJournalDE.FileContentBase64)
                                    : GetHashFromCompressedBase64(dbJournalDE.FileContentBase64);

                uploadSuccess = checkSum == dbCheckSum;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to check content equality.");
            }

            if (uploadSuccess)
            {
                if (!_storeTemporaryExportFiles && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            else
            {
                _logger.LogWarning("A content mismatch between the temporary local TAR file and the database was detected. The local TAR file will not be deleted and can be found at '{file}'", filePath);
            }
        }

        public void CleanupTarFileDirectory(string workingDirectory)
        {
            if (!_storeTemporaryExportFiles && Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        public async Task CleanupAllTarFilesAsync()
        {
            if (_storeTemporaryExportFiles) { return; }

            var basePath = Path.Combine(_middlewareConfiguration.ServiceFolder, "Exports", _middlewareConfiguration.QueueId.ToString(), "TAR");
            if(!Directory.Exists(basePath)) { return; }

            foreach (var directory in Directory.GetDirectories(basePath))
            {
                CleanupTarFileDirectory(directory);
            }

            foreach (var export in Directory.GetFiles(basePath))
            {
                var journalDE = await _journalDERepository.GetByFileName(Path.GetFileNameWithoutExtension(export)).FirstOrDefaultAsync();
                if(journalDE == null)
                {
                    continue;
                }

                string checkSum = null;
                using (var stream = new FileStream(export, FileMode.Open, FileAccess.Read))
                {
                    using var sha256 = SHA256.Create();
                    checkSum = Convert.ToBase64String(sha256.ComputeHash(stream));
                }

               // Use SharpCompress to be compatible with old archives without proper footer data
                await CleanupTarFileAsync(journalDE.ftJournalDEId, Path.Combine(basePath, $"{journalDE.FileName}.tar"), checkSum, useSharpCompress: true);
            }
        }

        private static string GetHashFromCompressedBase64WithSharpCompress(string zippedBase64)
        {
            using Stream archive = new MemoryStream(Convert.FromBase64String(zippedBase64));
            using var reader = ReaderFactory.Open(archive);

            reader.MoveToNextEntry();
            using var stream = reader.OpenEntryStream();

            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(stream));
        }

        private static string GetHashFromCompressedBase64(string zippedBase64)
        {
            using var ms = new MemoryStream(Convert.FromBase64String(zippedBase64));
            using var arch = new ZipArchive(ms);

            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(arch.Entries.First().Open()));
        }
    }
}