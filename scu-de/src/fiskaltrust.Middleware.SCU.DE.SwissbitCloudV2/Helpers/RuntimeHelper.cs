using System;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers
{
    public static class RuntimeHelper
    {
        public static bool IsMono => Type.GetType("Mono.Runtime") != null;
    }
}
