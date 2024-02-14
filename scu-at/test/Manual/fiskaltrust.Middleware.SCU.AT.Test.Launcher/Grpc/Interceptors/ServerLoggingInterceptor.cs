using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.AT.Test.Launcher.Grpc.Interceptors
{
    public class ServerLoggingInterceptor : Interceptor
    {
        private readonly ILogger<ServerLoggingInterceptor> _logger;

        public ServerLoggingInterceptor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ServerLoggingInterceptor>();
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            LogIncomingRequest<TRequest, TResponse>(MethodType.Unary, context, request);
            var response = await continuation(request, context);
            LogOutgoingResponse<TRequest, TResponse>(MethodType.Unary, context, response);

            return response;
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogIncomingRequest<TRequest, TResponse>(MethodType.ClientStreaming, context);
            var response = await base.ClientStreamingServerHandler(requestStream, context, continuation);
            LogOutgoingResponse<TRequest, TResponse>(MethodType.ClientStreaming, context, response);

            return response;
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogIncomingRequest<TRequest, TResponse>(MethodType.ServerStreaming, context, request);
            await base.ServerStreamingServerHandler(request, responseStream, context, continuation);
            LogOutgoingResponse<TRequest, TResponse>(MethodType.ServerStreaming, context);
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogIncomingRequest<TRequest, TResponse>(MethodType.DuplexStreaming, context);
            await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
            LogOutgoingResponse<TRequest, TResponse>(MethodType.DuplexStreaming, context);
        }

        private void LogIncomingRequest<TRequest, TResponse>(MethodType methodType, ServerCallContext context, TRequest request = null)
            where TRequest : class
        {
            var message = "grpc Request | Method type: {0} | Method name: {1} | Request type: {2} | Response type: {3}";
            if (request != null)
            {
                message += " | Response:" + System.Environment.NewLine + "{4}";
            }
            _logger.LogDebug(message, methodType, context.Method, typeof(TRequest), typeof(TResponse), JsonConvert.SerializeObject(request, Formatting.Indented));
        }

        private void LogOutgoingResponse<TRequest, TResponse>(MethodType methodType, ServerCallContext context, TResponse response = null)
            where TResponse : class
        {
            var message = "grpc Response | Method type: {0} | Method name: {1} | Request type: {2} | Response type: {3} | Status: {4} ({5})";
            if (response != null)
            {
                message += " | Response:" + System.Environment.NewLine + "{6}";
            }
            _logger.LogDebug(message, methodType, context.Method, typeof(TRequest), typeof(TResponse), context.Status.StatusCode, context.Status.Detail, JsonConvert.SerializeObject(response, Formatting.Indented));
        }
    }
}