using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Constants;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public class TarFileCleanupService : ITarFileCleanupService
    {
        private readonly ILogger<TarFileCleanupService> _logger;
        private readonly IMiddlewareJournalDERepository _journalDERepository;
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        protected bool _storeTemporaryExportFiles = false;

        public TarFileCleanupService(ILogger<TarFileCleanupService> logger, IHostApplicationLifetime lifetime, IMiddlewareJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration)
        {
            _logger = logger;
            _journalDERepository = journalDERepository;
            _middlewareConfiguration = middlewareConfiguration;


            if (_middlewareConfiguration.Configuration.ContainsKey(ConfigurationKeys.STORE_TEMPORARY_FILES_KEY))
            {
                _storeTemporaryExportFiles = bool.TryParse(_middlewareConfiguration.Configuration[ConfigurationKeys.STORE_TEMPORARY_FILES_KEY].ToString(), out var val) && val;
            }

            if (!_storeTemporaryExportFiles)
            {
                lifetime.ApplicationStarted.Register(CleanupAllTarFiles);
            }
        }

        public async Task CleanupTarFile(Guid journalDEId, string filePath, string checkSum)
        {
            var dbJournalDE = await _journalDERepository.GetAsync(journalDEId).ConfigureAwait(false);

            var uploadSuccess = false;

            try
            {
                var dbCheckSum = GetHashFromCompressedBase64(dbJournalDE.FileContentBase64);

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
                _logger.LogWarning("Failed to insert Tar export into database. Tar export file can be found here {file}", filePath);
            }
        }

        public void CleanupTarFileDirectory(string workingDirectory)
        {
            if (!_storeTemporaryExportFiles && Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        private async void CleanupAllTarFiles()
        {
            var basePath = Path.Combine(_middlewareConfiguration.ServiceFolder, "Exports", _middlewareConfiguration.QueueId.ToString(), "TAR");
            foreach (var directory in Directory.GetDirectories(basePath))
            {
                CleanupTarFileDirectory(directory);
            }

            foreach (var export in Directory.GetFiles(basePath))
            {
                var journalDE = await _journalDERepository.GetByFileName(Path.GetFileNameWithoutExtension(export)).FirstOrDefaultAsync();

                using var stream = new FileStream(export, FileMode.Open, FileAccess.Read);

                using var sha256 = SHA256.Create();
                var checkSum = Convert.ToBase64String(sha256.ComputeHash(stream));

                await CleanupTarFile(journalDE.ftJournalDEId, $"{journalDE.FileName}.zip", checkSum);
            }
        }

        private static string GetHashFromCompressedBase64(string zippedBase64)
        {
            using var ms = new MemoryStream(Convert.FromBase64String(zippedBase64));
            using var arch = new ZipArchive(ms);

            using var sha256 = SHA256.Create();
            var dbCheckSum = Convert.ToBase64String(sha256.ComputeHash(arch.Entries.First().Open()));

            return dbCheckSum;
        }
    }
}