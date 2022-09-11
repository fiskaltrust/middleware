using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR
{
    public interface IArchiveProcessor
    {
        Task ExportArchiveDataAsync(string targetFile, ArchivePayload archivePayload, ftSignaturCreationUnitFR signatureCreationUnitFR);
    }
}