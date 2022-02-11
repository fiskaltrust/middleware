using System;

namespace fiskaltrust.Middleware.Abstractions
{
    public class ClientConfiguration
    {
        public string UrlType { get; set; }
        public string Url { get; set; }
        public TimeSpan Timeout { get; set; }
        public int? RetryCount { get; set; }
        public TimeSpan DelayBetweenRetries { get; set; }
    }
}