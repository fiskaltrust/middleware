using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Queue.Test.Launcher.Helpers;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Serilog;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Grpc.Interceptors
{
    public class ExceptionHandlingInterceptor : Interceptor
    {
        private const string METADATA_KEY_MESSAGE = "exception-message";
        private const string METADATA_KEY_TYPE = "exception-type";
        private const string METADATA_KEY_PACKAGE = "origin";
        private const string METADATA_KEY_TRACE = "stack-trace";

        private readonly Type _packageType;

        public ExceptionHandlingInterceptor(Type type)
        {
            _packageType = type;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation(request, context);
            }
            catch (Exception ex)
            {
                throw CreateRpcException(ex);
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await base.ClientStreamingServerHandler(requestStream, context, continuation);
            }
            catch (Exception ex)
            {
                throw CreateRpcException(ex);
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                await base.ServerStreamingServerHandler(request, responseStream, context, continuation);
            }
            catch (Exception ex)
            {
                throw CreateRpcException(ex);
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
            }
            catch (Exception ex)
            {
                throw CreateRpcException(ex);
            }
        }

        private RpcException CreateRpcException(Exception ex)
        {
            static string GetNextRethrowKey(Metadata md)
            {
                const string PREFIX = "rethrown-by-";
                return $"{PREFIX}{md.Count(x => x.Key.StartsWith(PREFIX)) + 1}";
            }

            static string EscapeString(string str) =>
                str.Replace(Environment.NewLine, "; ")
                   .Replace("\n", "; ")
                   .Replace("\t", " ");

            var innerException = ex.Flatten().OfType<RpcException>().SingleOrDefault();
            if (innerException != null)
            {
                innerException.Trailers.Add(GetNextRethrowKey(innerException.Trailers), _packageType.Assembly.FullName);
                return innerException;
            }

            var metadata = new Metadata
            {
                { METADATA_KEY_MESSAGE, EscapeString(ex.ToString()) },
                { METADATA_KEY_TYPE, ex.GetType().Name },
                { METADATA_KEY_PACKAGE, _packageType.Assembly.FullName },
                { METADATA_KEY_TRACE, EscapeString(ex.StackTrace)}
            };

            return new RpcException(new Status(StatusCode.Unknown, "An exception occured in the fiskaltrust.Middleware."), metadata);
        }
    }
}