using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.StaticLib;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.SCU.DE.SwissbitBase.Helpers;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitAndroid
{
    public class SwissbitSCU : Swissbit.SwissbitBase
    {
        public SwissbitSCU(Dictionary<string, object> configuration)
            : this(new ConfigurationDictionary(configuration), new FunctionPointerFactory(), NullLogger<Swissbit.SwissbitBase>.Instance, new LockingHelper(NullLogger<LockingHelper>.Instance))
        {
        }

        public SwissbitSCU(ConfigurationDictionary configurationDictionary, INativeFunctionPointerFactory nativeFunctionPointerFactory, ILogger<Swissbit.SwissbitBase> logger, LockingHelper lockingHelper)
            : base(configurationDictionary, nativeFunctionPointerFactory, logger, lockingHelper) { }

    }
}