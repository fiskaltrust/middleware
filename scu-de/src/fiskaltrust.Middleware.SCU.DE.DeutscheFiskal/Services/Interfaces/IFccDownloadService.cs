using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces
{
    public interface IFccDownloadService
    {
        Task DownloadAndSetupIfRequiredAsync(string fccDirectory);
    }
}