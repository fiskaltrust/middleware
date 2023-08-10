using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Dummy.Services
{
    /// <summary>
    /// Class responsible for handling SSCD (Security Signature Creation Device) for the specific market "XX".
    /// </summary>
    /// <remarks>
    /// Implement the logic to determine if the SSCD is available for the specific market, replacing "XX" with the actual market name and including necessary checks and validations.
    /// For an example of the implementation, refer to <see cref="fiskaltrust.Middleware.Localization.QueueIT.Services.SscdIT"/> in the Italian market folder.
    /// </remarks>
    public class SscdXX : ISSCD
    {
        private readonly object _client;
        private readonly ILogger _logger;

        public SscdXX(IXXSSCDProvider Object, ILogger<SscdXX> logger)
        {
            _client = Object.Instance;
            _logger = logger;
        }

        public Task<bool> IsSSCDAvailable()
        {
            throw new NotImplementedException("The method to check SSCD availability is not implemented yet.");
        }
    }
}