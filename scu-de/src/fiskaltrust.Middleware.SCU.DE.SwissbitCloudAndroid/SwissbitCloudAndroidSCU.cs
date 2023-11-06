using DE.Fiskal.Connector.Android.Api;
using DE.Fiskal.Connector.Android.Api.Model;
using DE.Fiskal.Connector.Android.Client.Library;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Callbacks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Constants;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Management;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid
{
    public class SwissbitCloudAndroidSCU : ifPOS.v1.de.IDESSCD, IDisposable
    {
        private static readonly byte[] _emptyProcessData = Encoding.UTF8.GetBytes("<none>");

        private readonly FccClient _fccClient;
        private readonly SwissbitCloudAndroidSCUConfiguration _configuration;
        private readonly ISwissbitCloudManagementClient _managementClient;
        private readonly ConcurrentDictionary<ulong, DateTime> _startTransactionTimeStampCache;

        private IFccAndroid _fccAndroid;
        private DateTime? _sessionExpiresOn;
        private ifPOS.v1.de.TseInfo _lastTseInfo;
        private long _lastSignatureCount = -1;

        public SwissbitCloudAndroidSCU(SwissbitCloudAndroidSCUConfiguration configuration, ISwissbitCloudManagementClient managementClient)
        {
            _configuration = configuration;
            _managementClient = managementClient;
            _startTransactionTimeStampCache = new ConcurrentDictionary<ulong, DateTime>();

            _fccClient = new FccClient();
            if (_fccClient.IsFiskalCloudConnectorInstalled(Android.App.Application.Context))
            {
                _fccClient.RequestAppInstallation(Android.App.Application.Context);
                throw new Exception("Fiskal Cloud Connector is not installed. Please install 'DF Fiskal Cloud Connect' and 'Swissbit Fiskal Cloud Connect' from Google Play, and try again.");
            }
        }

        private async Task EnsureFccIsInitializedAsync(string clientId = null)
        {
            if (_fccAndroid == null)
            {
                _fccAndroid = await ExecuteAsync<IFccAndroid>(tcs => _fccClient.Start(new FccClientStartCallback(tcs))).ConfigureAwait(false);
            }

            if (!_fccAndroid.IsFccLibraryInitialized)
            {
                var initSuccess = await ExecuteAsync<Java.Lang.Boolean>(
                    (tcs) => _fccAndroid.InitFcc(new InitData(_configuration.FccId, _configuration.FccSecret, null, null, null, true, null), new ResultCallback<Java.Lang.Boolean>(tcs)));
            }
            if (clientId != null && _sessionExpiresOn != null && _sessionExpiresOn.Value.AddMinutes(-5) >= DateTime.UtcNow)
            {
                var loginContext = await ExecuteAsync<LoginContext>((tcs) => _fccAndroid.StartSession(clientId, new ResultCallback<LoginContext>(tcs)));
                _sessionExpiresOn = DateTimeUtil.DateTimeFromUnixTimestampMillis(loginContext.ExpirationTimeMillis);
            }
        }

        public async Task<ifPOS.v1.de.ScuDeEchoResponse> EchoAsync(ifPOS.v1.de.ScuDeEchoRequest request) => await Task.FromResult(new ifPOS.v1.de.ScuDeEchoResponse
        {
            Message = request.Message
        });

        public async Task<ifPOS.v1.de.StartTransactionResponse> StartTransactionAsync(ifPOS.v1.de.StartTransactionRequest request)
        {
            await EnsureFccIsInitializedAsync(request.ClientId);
            if (_lastTseInfo == null)
            {
                await GetTseInfoAsync();
            }

            var startTransactionResponse = await ExecuteAsync<StartTransactionResponse>(
                (tcs) => _fccAndroid.ErsApiInstance.StartTransaction(new StartTransactionRequest(request.ProcessDataBase64 != null ? Convert.FromBase64String(request.ProcessDataBase64) : _emptyProcessData, request.ProcessType ?? "", null), new ResultCallback<StartTransactionResponse>(tcs)));

            var logTime = DateTimeUtil.DateTimeFromUnixTimestampSeconds(startTransactionResponse.LogTimeSeconds.LongValue());
            _startTransactionTimeStampCache.AddOrUpdate((ulong) startTransactionResponse.TransactionId, logTime, (key, oldValue) => logTime);
            _lastSignatureCount = startTransactionResponse.SignatureCounter.LongValue();

            return new ifPOS.v1.de.StartTransactionResponse
            {
                TransactionNumber = (ulong) startTransactionResponse.TransactionId,
                TseSerialNumberOctet = _lastTseInfo.SerialNumberOctet,
                ClientId = request.ClientId,
                TimeStamp = logTime,
                SignatureData = new ifPOS.v1.de.TseSignatureData()
                {
                    SignatureBase64 = Convert.ToBase64String(startTransactionResponse.GetSignatureValue()),
                    SignatureCounter = (ulong) startTransactionResponse.SignatureCounter.LongValue(),
                    SignatureAlgorithm = _lastTseInfo.SignatureAlgorithm,
                    PublicKeyBase64 = _lastTseInfo.PublicKeyBase64,
                }
            };
        }

        public async Task<ifPOS.v1.de.UpdateTransactionResponse> UpdateTransactionAsync(ifPOS.v1.de.UpdateTransactionRequest request)
        {
            await EnsureFccIsInitializedAsync(request.ClientId);
            if (_lastTseInfo == null)
            {
                await GetTseInfoAsync();
            }

            var updateTransactionResponse = await ExecuteAsync<UpdateTransactionResponse>(
                (tcs) => _fccAndroid.ErsApiInstance.UpdateTransaction(new UpdateTransactionRequest((long) request.TransactionNumber, request.ProcessDataBase64 != null ? Convert.FromBase64String(request.ProcessDataBase64) : _emptyProcessData, request.ProcessType ?? ""), new ResultCallback<UpdateTransactionResponse>(tcs)));
            _lastSignatureCount = updateTransactionResponse.SignatureCounter.LongValue();

            return new ifPOS.v1.de.UpdateTransactionResponse
            {
                TransactionNumber = request.TransactionNumber,
                TseSerialNumberOctet = _lastTseInfo.SerialNumberOctet,
                ClientId = request.ClientId,
                ProcessDataBase64 = request.ProcessDataBase64,
                ProcessType = request.ProcessType,
                TimeStamp = DateTimeUtil.DateTimeFromUnixTimestampSeconds(updateTransactionResponse.LogTimeSeconds.LongValue()),
                SignatureData = new ifPOS.v1.de.TseSignatureData
                {
                    SignatureBase64 = Convert.ToBase64String(updateTransactionResponse.GetSignatureValue()),
                    SignatureCounter = (ulong) updateTransactionResponse.SignatureCounter.LongValue(),
                    SignatureAlgorithm = _lastTseInfo.SignatureAlgorithm,
                    PublicKeyBase64 = _lastTseInfo.PublicKeyBase64
                }
            };
        }

        public async Task<ifPOS.v1.de.FinishTransactionResponse> FinishTransactionAsync(ifPOS.v1.de.FinishTransactionRequest request)
        {
            await EnsureFccIsInitializedAsync(request.ClientId);
            if (_lastTseInfo == null)
            {
                await GetTseInfoAsync();
            }

            var finishTransactionResponse = await ExecuteAsync<FinishTransactionResponse>(
                (tcs) => _fccAndroid.ErsApiInstance.FinishTransaction(new FinishTransactionRequest((long) request.TransactionNumber, request.ProcessDataBase64 != null ? Convert.FromBase64String(request.ProcessDataBase64) : _emptyProcessData, request.ProcessType), new ResultCallback<FinishTransactionResponse>(tcs)));
            _lastSignatureCount = finishTransactionResponse.SignatureCounter;

            if (!_startTransactionTimeStampCache.TryRemove(request.TransactionNumber, out var startTransactionTimeStamp))
            {
                var logs = await GetLogsForTransaction(request.TransactionNumber);
                var startTransactionLog = logs.FirstOrDefault(x => x.OperationType.Contains(SwissbitCloudAndroidConstants.TransactionType.StartTransaction));
                startTransactionTimeStamp = startTransactionLog?.LogTime ?? default;
            }

            return new ifPOS.v1.de.FinishTransactionResponse
            {
                TransactionNumber = request.TransactionNumber,
                TseSerialNumberOctet = _lastTseInfo.SerialNumberOctet,
                ClientId = request.ClientId,
                ProcessDataBase64 = request.ProcessDataBase64,
                ProcessType = request.ProcessType,
                StartTransactionTimeStamp = startTransactionTimeStamp,
                SignatureData = new ifPOS.v1.de.TseSignatureData
                {
                    SignatureBase64 = Convert.ToBase64String(finishTransactionResponse.GetSignatureValue()),
                    SignatureCounter = (ulong) finishTransactionResponse.SignatureCounter,
                    SignatureAlgorithm = _lastTseInfo.SignatureAlgorithm,
                    PublicKeyBase64 = _lastTseInfo.PublicKeyBase64
                },
                TseTimeStampFormat = _lastTseInfo.LogTimeFormat,
                TimeStamp = DateTimeUtil.DateTimeFromUnixTimestampSeconds(finishTransactionResponse.LogTimeSeconds.LongValue())
            };
        }


        public async Task<ifPOS.v1.de.TseInfo> GetTseInfoAsync()
        {
            await EnsureFccIsInitializedAsync();

            var transactionList = await ExecuteAsync<LookupTransactionsList>(
                (tcs) => _fccAndroid.ErsApiInstance.LookupOpenTransactions(new ResultCallback<LookupTransactionsList>(tcs)));
            var tssDetails = await ExecuteAsync<TssDetails>(
                (tcs) => _fccAndroid.ErsApiInstance.GetTssDetails(new ResultCallback<TssDetails>(tcs)));
            var fccInfo = await ExecuteAsync<InfoResponse>(
                (tcs) => _fccAndroid.ErsApiInstance.GetInfo(new ResultCallback<InfoResponse>(tcs)));
            var (clientIds, remoteCspVersion, status) = await _managementClient.GetTseDetailsAsync(_configuration.FccId, _configuration.FccSecret, _configuration.SscdId);

            _lastTseInfo = new ifPOS.v1.de.TseInfo
            {
                CurrentNumberOfClients = clientIds.Count,
                CurrentNumberOfStartedTransactions = fccInfo.CurrentNumberOfTransactions.LongValue(),
                SerialNumberOctet = tssDetails.SerialNumberHex,
                PublicKeyBase64 = Convert.ToBase64String(tssDetails.GetPublicKey()),
                FirmwareIdentification = remoteCspVersion,
                CertificationIdentification = _configuration.CertificationId,
                MaxNumberOfClients = fccInfo.MaxNumberErs.LongValue(),
                MaxNumberOfStartedTransactions = fccInfo.MaxNumberTransactions.LongValue(),
                CertificatesBase64 = new List<string> { Convert.ToBase64String(tssDetails.GetLeafCertificate()) },
                CurrentClientIds = clientIds,
                SignatureAlgorithm = tssDetails.Algorithm.Value,
                CurrentLogMemorySize = -1,
                CurrentNumberOfSignatures = _lastSignatureCount,
                LogTimeFormat = tssDetails.GetTimeFormat().Value,
                MaxLogMemorySize = long.MaxValue,
                MaxNumberOfSignatures = long.MaxValue,
                CurrentStartedTransactionNumbers = transactionList.Transactions.Select(x => (ulong)x.TransactionNumber).ToList(),
                CurrentState = ConvertStatusToTseState(status)
            };

            return _lastTseInfo;
        }

        private ifPOS.v1.de.TseStates ConvertStatusToTseState(string status)
        {
            return status switch
            {
                "active" => ifPOS.v1.de.TseStates.Initialized,
                "inactive" => ifPOS.v1.de.TseStates.Uninitialized,
                _ => ifPOS.v1.de.TseStates.Terminated
            };
        }

        public Task<ifPOS.v1.de.TseState> SetTseStateAsync(ifPOS.v1.de.TseState state) => throw new NotImplementedException();
        public Task<ifPOS.v1.de.RegisterClientIdResponse> RegisterClientIdAsync(ifPOS.v1.de.RegisterClientIdRequest request) => throw new NotImplementedException();
        public Task<ifPOS.v1.de.UnregisterClientIdResponse> UnregisterClientIdAsync(ifPOS.v1.de.UnregisterClientIdRequest request) => throw new NotImplementedException();

        public Task ExecuteSelfTestAsync() => throw new NotImplementedException();
        public Task ExecuteSetTseTimeAsync() => throw new NotImplementedException();

        public Task<ifPOS.v1.de.StartExportSessionResponse> StartExportSessionAsync(ifPOS.v1.de.StartExportSessionRequest request) => throw new NotImplementedException();
        public Task<ifPOS.v1.de.StartExportSessionResponse> StartExportSessionByTimeStampAsync(ifPOS.v1.de.StartExportSessionByTimeStampRequest request) => throw new NotImplementedException();
        public Task<ifPOS.v1.de.StartExportSessionResponse> StartExportSessionByTransactionAsync(ifPOS.v1.de.StartExportSessionByTransactionRequest request) => throw new NotImplementedException();
        public Task<ifPOS.v1.de.ExportDataResponse> ExportDataAsync(ifPOS.v1.de.ExportDataRequest request) => throw new NotImplementedException();
        public Task<ifPOS.v1.de.EndExportSessionResponse> EndExportSessionAsync(ifPOS.v1.de.EndExportSessionRequest request) => throw new NotImplementedException();

        private Task<List<TransactionLogMessage>> GetLogsForTransaction(ulong transactionNumber)
        {
            // TODO implement if possible
            throw new NotImplementedException();
        }

        private async Task<TResult> ExecuteAsync<TResult>(Action<TaskCompletionSource<TResult>> func)
        {
            var tcs = new TaskCompletionSource<TResult>();
            func(tcs);
            return await tcs.Task;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fccClient?.Stop();
            }
        }



        //private const string FCC_ID = "sfcc-ftde-83f9-87uz";
        //private const string FCC_SECRET = "zl6SggQ9FJ";
        //private const string ADMIN_PUK = "123456789";
        //private const string ERS_IDENTIFIER = "tsc-android-test-2022-1";
        //private const string BUSINESS_PREMISE_ID = "c5f746ae-0f27-4b5d-a3fa-51639de1bbec";
        //private const string TSS_ID = "a04f7bda-1544-4462-ba5b-8c774160dbc2";
        //public async Task DoItAsync()
        //{
        //    try
        //    {
        //        var fccClient = new FccClient();
        //        var isFccInstalled = fccClient.IsFiskalCloudConnectorInstalled(Android.App.Application.Context);

        //        var fccAndroid = await ExecuteAsync<global::DE.Fiskal.Connector.Android.Api.IFccAndroid>(tcs => fccClient.Start(new FccClientStartCallback(tcs)));

        //        Console.WriteLine();
        //        if (!fccAndroid.IsFccLibraryInitialized)
        //        {
        //            // TODO test
        //            var initSuccess = await ExecuteAsync<Java.Lang.Boolean>(
        //                (tcs) => fccAndroid.InitFcc(new InitData(FCC_ID, FCC_SECRET, ADMIN_PUK, null, null, true, null), new ResultCallback<Java.Lang.Boolean>(tcs)));
        //        }

        //        //// ERS needs to exist for Android to work - we need to create this e.g. with a separate API
        //        var loginContext = await ExecuteAsync<LoginContext>(
        //                (tcs) => fccAndroid.StartSession(ERS_IDENTIFIER, new ResultCallback<LoginContext>(tcs)));

        //        //Fails while deserializing..we should ask DF to add
        //        var selfCheckResponse = await ExecuteAsync<SelfCheckResponse>(
        //                (tcs) => fccAndroid.ErsApiInstance.SelfCheck(true, new ResultCallback<SelfCheckResponse>(tcs)));

        //        var tssDetails = await ExecuteAsync<TssDetails>(
        //                (tcs) => fccAndroid.ErsApiInstance.GetTssDetails(new ResultCallback<TssDetails>(tcs)));

        //        var startTransactionResponse = await ExecuteAsync<StartTransactionResponse>(
        //                (tcs) => fccAndroid.ErsApiInstance.StartTransaction(new StartTransactionRequest(Encoding.UTF8.GetBytes("<none>"), "", null), new ResultCallback<StartTransactionResponse>(tcs)));

        //        var positions = new List<OrderPosition>
        //        {
        //            new OrderPosition(2, "Bier", 4),
        //            new OrderPosition(1, "Schnitzel", 12)
        //        };
        //        var finishTransactionResponse = await ExecuteAsync<FinishTransactionResponse>(
        //                (tcs) => fccAndroid.ErsApiInstance.FinishOrderTransaction(new FinishOrderTransactionRequest(startTransactionResponse.TransactionId, positions, "Bestellung-V1"), new ResultCallback<FinishTransactionResponse>(tcs)));

        //        Console.WriteLine();
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Console.WriteLine(ex);
        //        throw;
        //    }
        //}
    }
}
