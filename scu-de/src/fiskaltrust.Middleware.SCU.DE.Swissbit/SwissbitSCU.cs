using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using fiskaltrust.Middleware.SCU.DE.SwissbitBase.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public class SwissbitSCU : SwissbitBase
    {
        public const string libraryFileKeyName = "libraryFile";

        public SwissbitSCU(Dictionary<string, object> configuration)
            : this(
                  new ConfigurationDictionary(configuration),
                  new Interop.DynamicLib.FunctionPointerFactory(configuration.ContainsKey(libraryFileKeyName) ? configuration[libraryFileKeyName] as string : null),
                  NullLogger<SwissbitBase>.Instance,
                  new LockingHelper(NullLogger<LockingHelper>.Instance))
        { }

        public SwissbitSCU(ConfigurationDictionary configurationDictionary, INativeFunctionPointerFactory nativeFunctionPointerFactory, ILogger<SwissbitBase> logger, LockingHelper lockingHelper)
            : base(configurationDictionary, nativeFunctionPointerFactory, logger, lockingHelper) { }
    }
}
