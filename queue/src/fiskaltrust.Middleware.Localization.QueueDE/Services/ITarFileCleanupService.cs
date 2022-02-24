using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public interface ITarFileCleanupService
    {
        Task CleanupTarFile(Guid journalDEId, string filePath, string checkSum);

        void CleanupTarFileDirectory(string workingDirectory);
    }
}