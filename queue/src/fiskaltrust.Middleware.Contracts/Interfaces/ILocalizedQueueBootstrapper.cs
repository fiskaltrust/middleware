using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface ILocalizedQueueBootstrapper
    {
        void ConfigureServices(IServiceCollection services);
    }
}
