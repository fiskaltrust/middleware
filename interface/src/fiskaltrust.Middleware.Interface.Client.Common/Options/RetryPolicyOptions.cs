using System;

namespace fiskaltrust.Middleware.Interface.Client
{
    /// <summary>
    /// Set the Retries for calling the SCU, the Delay between the Retries and the Timeout for one call.
    /// </summary>
    public class RetryPolicyOptions
    {
        public int Retries { get; set; }
        public TimeSpan DelayBetweenRetries { get; set; }
        public TimeSpan ClientTimeout { get; set; }

        public static RetryPolicyOptions Default => new RetryPolicyOptions
        {
            Retries = 3,
            DelayBetweenRetries = TimeSpan.FromSeconds(2),
            ClientTimeout = TimeSpan.FromSeconds(15)
        };
    }
}
