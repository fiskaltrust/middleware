using System;

namespace fiskaltrust.Middleware.Interface.Client
{
    /// <summary>
    /// Common options to configure on client side.
    /// </summary>
    public class ClientOptions
    {
        public Uri Url { get; set; }
        public RetryPolicyOptions RetryPolicyOptions { get; set; }
    }
}
