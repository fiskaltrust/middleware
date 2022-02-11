using ProtoBuf.Grpc.Configuration;
using System.Reflection;

namespace fiskaltrust.service.launcher.Helpers.Grpc.Binders
{
    internal class SkipNonVersionedMethodsBinder : ServiceBinder
    {
        public override bool IsOperationContract(MethodInfo method, out string name)
        {
            base.IsOperationContract(method, out name);
            return name.Contains("/");
        }
    }
}
