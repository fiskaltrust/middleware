using ProtoBuf.Grpc.Configuration;
using System.Reflection;

namespace fiskaltrust.Middleware.SCU.DE.Test.Launcher.Grpc.Binders
{
    internal class RemoveMethodVersionPrefixBinder : ServiceBinder
    {
        public override bool IsOperationContract(MethodInfo method, out string name)
        {
            var result = base.IsOperationContract(method, out name);
            if (name.Contains("/"))
            {
                name = name.Substring(name.IndexOf("/") + 1);
            }

            return result;
        }
    }
}
