using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces
{
    public interface IFccDownloadService
    {
        Version UsedFCCVersion { get; }
        Task<bool> DownloadFccAsync(string fccDirectory);
        bool IsInstalled(string fccDirectory);
        bool IsLatestVersion(string fccDirectory, Version latestVersion);
        Task LogWarningIfFccPathsDontMatchAsync(string fccDirectory);
        bool IsPathInRunFccIdent(string path, string content, out string pathInFile);
    }
}