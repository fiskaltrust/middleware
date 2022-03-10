using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Contracts
{
    public interface ILocalizedQueueBootstrapper
    {
        void ConfigureServices(IServiceCollection services);
    }
}
