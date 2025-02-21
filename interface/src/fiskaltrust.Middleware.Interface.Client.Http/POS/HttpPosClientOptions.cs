using System;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    /// <summary>
    /// Used to pass client options to the underlying HTTP client
    /// </summary>
    public class HttpPosClientOptions : ClientOptions
    {
        public HttpCommunicationType CommunicationType { get; set; }
        public bool UseUnversionedLegacyUrls { get; set; } = false;
        public Guid? CashboxId { get; set; }
        public string AccessToken { get; set; }
        public bool? DisableSslValidation { get; set; }
    }

    public enum HttpCommunicationType
    {
        Json,
        Xml
    }
}