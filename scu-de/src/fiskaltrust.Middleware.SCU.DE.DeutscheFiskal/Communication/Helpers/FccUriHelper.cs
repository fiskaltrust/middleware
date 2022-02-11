using System;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication.Helpers
{
    public static class FccUriHelper
    {
        public static Uri GetFccUri(DeutscheFiskalSCUConfiguration configuration)
        {
            return new Uri($"{GetBaseUrl(configuration)}:{ configuration.FccPort ?? DeutscheFiskalConstants.DefaultPort}");
        }
        public static string GetBaseUrl(DeutscheFiskalSCUConfiguration configuration) => string.IsNullOrEmpty(configuration.FccUri) ? "http://localhost" : configuration.FccUri;
    }
}
