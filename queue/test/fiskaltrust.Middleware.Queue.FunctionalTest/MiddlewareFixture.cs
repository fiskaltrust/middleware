using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Queue.FunctionalTest.Helper;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.FunctionalTest
{
    public class MiddlewareFixture : IDisposable
    {
        private readonly Process _launcherProcess;

        public virtual string ConfigurationFileName => Environment.GetEnvironmentVariable("MIDDLEWARE_CONFIGURATION") ?? "configuration_ef.json";

        public Guid CashBoxId { get; }

        public IPOS WcfProxy { get; private set; }
        public IPOS GrpcProxy { get; private set; }

        public MiddlewareFixture()
        {
            var configuration = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Data", ConfigurationFileName));
            var cashBoxConfiguration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(configuration);
            CashBoxId = cashBoxConfiguration.ftCashBoxId;
            string grpcQueueEndpoint;
            string wcfQueueEndpoint;
            (_launcherProcess, grpcQueueEndpoint, wcfQueueEndpoint) = Task.Run(() => LauncherPreparationHelper.PrepareOfflineLauncher(cashBoxConfiguration)).Result;
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            cts.CancelAfter(TimeSpan.FromSeconds(60));
            token.ThrowIfCancellationRequested();
            var echoRequest = new EchoRequest
            {
                Message = Guid.NewGuid().ToString()
            };
            var grpcWorkerTask = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (cts.IsCancellationRequested)
                        {
                            break;
                        }
                        GrpcProxy = ConnectionHelper.GetClient<IPOS>(grpcQueueEndpoint);
                        await GrpcProxy.EchoAsync(echoRequest);
                        break;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(1000);
                    }
                }
            }, token);
            var wcfWorkerTask = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (cts.IsCancellationRequested)
                        {
                            break;
                        }
                        WcfProxy = ConnectionHelper.GetClient<IPOS>(wcfQueueEndpoint);
                        await WcfProxy.EchoAsync(echoRequest);
                        break;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(1000);
                    }
                }
            }, token);
            Task.WhenAll(grpcWorkerTask, wcfWorkerTask).Wait();
        }

        public void Dispose()
        {
            WcfProxy = null;
            GrpcProxy = null;
            _launcherProcess.Kill();    
        }
    }
}
