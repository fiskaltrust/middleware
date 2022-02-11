using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Queue.Bootstrapper.Interfaces
{
    public interface ILocalizedQueueBootstrapper
    {
        void ConfigureServices(IServiceCollection services);
    }
}
