using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces
{
    public interface IFccProcessHost
    {
        bool IsRunning { get; }
        bool IsExtern { get; }
        void Dispose();
        Task StartAsync(string fccDirectory);
    }
}