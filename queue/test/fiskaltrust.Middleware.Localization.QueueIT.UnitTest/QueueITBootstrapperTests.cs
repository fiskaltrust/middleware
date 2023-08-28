using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    public class QueueITBootstrapperTests
    {
        [Fact]
        public void TryToConstructSignProcessorIT()
        {
            var bootstrapper = new QueueITBootstrapper();
            var serviceCollection = new ServiceCollection();
            bootstrapper.ConfigureServices(serviceCollection);

            serviceCollection.BuildServiceProvider().GetRequiredService<SignProcessorIT>();
        }
    }
}
