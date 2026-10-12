using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public interface IEpsonFpMateClient
{
    /// <param name="timeout">
    /// How long to wait for this particular command, when it differs from the client's own. The recovery uses
    /// it to keep its status queries short: it asks the printer the same question several times, so a query
    /// that waited as long as a print command would spend the whole recovery window on a single attempt.
    /// </param>
    Task<HttpResponseMessage> SendCommandAsync(string payload, TimeSpan? timeout = null);
}
