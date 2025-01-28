using System.Threading.Tasks;
using System.Net.Http;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public interface IEpsonFpMateClient
{
    Task<HttpResponseMessage> SendCommandAsync(string payload);
}
