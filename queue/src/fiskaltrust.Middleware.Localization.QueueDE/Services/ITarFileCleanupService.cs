using System;
using System.Threading;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.QueueDE.Services
{
    public interface ITarFileCleanupService
    {
        Task CleanupTarFileAsync(Guid? journalDEId, string filePath, string checkSum, bool useSharpCompress = false);

        void CleanupTarFileDirectory(string workingDirectory);

        Task CleanupAllTarFilesAsync();
    }
}