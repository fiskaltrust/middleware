using Grpc.Core;
using ProtoBuf.Grpc.Configuration;
using System.IO;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Grpc.Binders
{
    internal class Binder : ServerBinder
    {
        private readonly TextWriter _log;
        private Binder(TextWriter log) => _log = log;
        private static readonly Binder _default = new Binder(null);
        public static Binder Create(TextWriter log) => log == null ? _default : new Binder(log);

        protected override bool TryBind<TService, TRequest, TResponse>(ServiceBindContext bindContext, Method<TRequest, TResponse> method, MethodStub<TService> stub)
        {
            var builder = (ServerServiceDefinition.Builder) bindContext.State;
            switch (method.Type)
            {
                case MethodType.Unary:
                    builder.AddMethod(method, stub.CreateDelegate<UnaryServerMethod<TRequest, TResponse>>());
                    break;
                case MethodType.ClientStreaming:
                    builder.AddMethod(method, stub.CreateDelegate<ClientStreamingServerMethod<TRequest, TResponse>>());
                    break;
                case MethodType.ServerStreaming:
                    builder.AddMethod(method, stub.CreateDelegate<ServerStreamingServerMethod<TRequest, TResponse>>());
                    break;
                case MethodType.DuplexStreaming:
                    builder.AddMethod(method, stub.CreateDelegate<DuplexStreamingServerMethod<TRequest, TResponse>>());
                    break;
                default:
                    return false;
            }
            _log?.WriteLine($"{method.ServiceName} / {method.Name} ({method.Type}) bound to {stub.Method.DeclaringType.Name}.{stub.Method.Name}");
            return true;
        }
    }
}

