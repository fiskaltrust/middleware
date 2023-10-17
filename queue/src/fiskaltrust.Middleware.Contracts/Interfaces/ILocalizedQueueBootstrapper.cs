using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface ILocalizedQueueBootstrapper
    {
        void ConfigureServices(IServiceCollection services);
        Task<Func<IServiceProvider, Task>> ConfigureServicesAsync(IServiceCollection services);
    }
}
