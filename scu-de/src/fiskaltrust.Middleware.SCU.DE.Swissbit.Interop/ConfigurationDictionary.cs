using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop
{
    [Serializable]
    public class ConfigurationDictionary : Dictionary<string, object>
    {
        public ConfigurationDictionary(IDictionary<string, object> configuration) : base(configuration) { }

        protected ConfigurationDictionary(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
