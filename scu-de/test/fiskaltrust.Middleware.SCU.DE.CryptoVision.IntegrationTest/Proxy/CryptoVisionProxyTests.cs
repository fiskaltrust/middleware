using System;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop;
using Xunit;
using Xunit.Abstractions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.SerialNumbers;
using System.Linq;
using FluentAssertions;
using System.IO;
using SharpCompress.Readers;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.IntegrationTest
{
    public abstract class CryptoVisionProxyTests
    {
        protected HardwareFixtures HardwareFixtures;
        protected ITestOutputHelper OutputHelper;

        protected ICryptoVisionProxy sut;

        private static bool runStartOnlyOnce = false;

        [Fact]
        public async Task Start()
        {
            if (runStartOnlyOnce)
            {
                return;
            }

            (var result, var version, var serialnumber) = await sut.SeStartAsync();

            result.ThrowIfError();

            OutputHelper.WriteLine($"firmwareId: {version}, uniqueId:{BitConverter.ToString(serialnumber)}");

            await UpdateTime();
            runStartOnlyOnce = true;
        }


        [Fact]
        public async Task AuthenticateUser_TimeAdmin()
        {
            await Start();

            (var result, var authenticationResult, var remainingRetries) = await sut.SeAuthenticateUserAsync(HardwareFixtures.TimeAdminName, HardwareFixtures.TimeAdminPin);

            result.ThrowIfError();

            OutputHelper.WriteLine($"login {HardwareFixtures.TimeAdminName}: authenticationResult: {authenticationResult}, remainingRetries:{remainingRetries}");
        }


        [Fact]
        public async Task GetTimeSyncInterval()
        {
            await Start();

            (var result, var timeSyncInterval) = await sut.SeGetTimeSyncIntervalAsync();

            result.ThrowIfError();

            OutputHelper.WriteLine($"timesyncinterval: {timeSyncInterval}");
        }

        [Fact]
        public async Task GetCertificatiionId()
        {
            await Start();

            (var result, var certificationId) = await sut.SeGetCertificationIdAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"certification: {certificationId}");
        }

        [Fact]
        public async Task GetSignatureAlgorithm()
        {
            (var result, var signatureAlgorithmBytes) = await sut.SeGetSignatureAlgorithmAsync();
            result.ThrowIfError();



            var signatureAlgorithmOid = LogParser.GetSignaturAlgorithmOid(signatureAlgorithmBytes).FirstOrDefault();
            var signatureAlgorithmName = SignatureAlgorithm.NameFromOid(signatureAlgorithmOid);


            OutputHelper.WriteLine($"signatureAlgorithmBytes: {BitConverter.ToString(signatureAlgorithmBytes)}, signatureAlgorithmOID: {signatureAlgorithmOid}, signatureAlgorithmName: {signatureAlgorithmName}");
        }

        [Fact]
        public async Task GetCurrentNumberOfClients()
        {
            await Start();

            (var result, var currentNumberOfClients) = await sut.SeGetCurrentNumberOfClientsAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"currentNumberOfClients: {currentNumberOfClients}");
        }

        [Fact]
        public async Task GetMaxNumberOfClients()
        {
            await Start();

            (var result, var maxNumberOfClients) = await sut.SeGetMaxNumberOfClientsAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"maxNumberOfClients: {maxNumberOfClients}");
        }

        [Fact]
        public async Task GetCurrentNumberOfTransactions()
        {
            await Start();

            (var result, var currentNumberOfTransactions) = await sut.SeGetCurrentNumberOfTransactionsAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"currentNumberOfTransactions: {currentNumberOfTransactions}");
        }

        [Fact]
        public async Task GetMaxNumberOfTransactions()
        {
            await Start();

            (var result, var maxNumberOfTransactions) = await sut.SeGetMaxNumberOfTransactionsAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"maxNumberOfTransactions: {maxNumberOfTransactions}");
        }

        [Fact]
        public async Task GetOpenTransactions()
        {
            await Start();

            (var result, var openTransactions) = await sut.SeGetOpenTransactionsAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"openTransactions begin");
            foreach (var item in openTransactions)
            {
                OutputHelper.WriteLine($"{item}");
            }
            OutputHelper.WriteLine($"openTransactions end");
        }

        [Fact]
        public async Task GetERSMappings()
        {
            await Start();

            (var result, var getErsMappingsBytes) = await sut.SeGetERSMappingsAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"GetERSMappings begin");
            foreach (var item in ERSMappingHelper.ERSMappingsAsString(getErsMappingsBytes))
            {
                OutputHelper.WriteLine(item);
            }
            OutputHelper.WriteLine($"GetERSMappings end");
        }

        [Fact]
        public async Task UpdateTime_CryptoVisionException()
        {
            await Start();

            var unixTime = new DateTime(2020, 05, 17, 14, 0, 0, DateTimeKind.Utc).ToTimestamp();

            var result = await sut.SeUpdateTimeAsync();

            Assert.Throws<CryptoVisionException>(() => result.ThrowIfError());

            OutputHelper.WriteLine($"updatetime not authenticated");
        }

        [Fact]
        public async Task DeleteStoredData_CryptoVisionException()
        {
            await Start();

            await sut.SeAuthenticateUserAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPin);

            var result = await sut.SeDeleteStoredDataAsync();

            Assert.Throws<CryptoVisionException>(() => result.ThrowIfError());

            await sut.SeLogOutAsync(HardwareFixtures.AdminName);

            OutputHelper.WriteLine($"updatetime not authenticated");
        }

        [Fact]
        public async Task UpdateTime()
        {
            await Start();

            await sut.SeAuthenticateUserAsync(HardwareFixtures.TimeAdminName, HardwareFixtures.TimeAdminPin);

            var unixTime = new DateTime(2020, 05, 17, 14, 0, 0, DateTimeKind.Utc).ToTimestamp();

            (await sut.SeUpdateTimeAsync()).ThrowIfError();

            await sut.SeLogOutAsync(HardwareFixtures.TimeAdminName);

            OutputHelper.WriteLine($"updatetime");
        }

        [Fact]
        public async Task Logout_TimeAdmin()
        {
            await AuthenticateUser_TimeAdmin();

            var result = await sut.SeLogOutAsync(HardwareFixtures.TimeAdminName);

            result.ThrowIfError();

            OutputHelper.WriteLine($"logout {HardwareFixtures.TimeAdminName}");
        }

        [Fact]
        public async Task GetPinStates()
        {
            await Start();

            (var result, var adminPinInTransportState, var adminPukInTransportState, var timeAdminPinInTransportState, var timeAdminPukInTransportState) = await sut.SeGetPinStatesAsync();

            result.ThrowIfError();

            OutputHelper.WriteLine($"adminPinInTransportState: {adminPinInTransportState}, adminPukInTransportState: {adminPukInTransportState}, timeAdminPinInTransportState: {timeAdminPinInTransportState}, timeAdminPukInTransportState: {timeAdminPukInTransportState}");
        }

        [Fact]
        public async Task GetAvailableLogMemory()
        {
            await Start();

            (var result, var memory) = await sut.SeGetAvailableLogMemoryAsync();

            result.ThrowIfError();

            OutputHelper.WriteLine($"availableLogMemory: {memory}");
        }

        [Fact]
        public async Task GetTotalLogMemory()
        {
            await Start();

            (var result, var memory) = await sut.SeGetTotalLogMemoryAsync();

            result.ThrowIfError();

            OutputHelper.WriteLine($"totalLogMemory: {memory}");
        }

        [Fact]
        public async Task GetTransactionCounter()
        {
            await Start();

            (var result, var transactionCounter) = await sut.SeGetTransactionCounterAsync();

            result.ThrowIfError();

            OutputHelper.WriteLine($"transactionCounter: {transactionCounter}");
        }

        [Fact]
        public async Task GetWearIndicator()
        {
            await Start();

            (var result, var wearIndicator) = await sut.SeGetWearIndicatorAsync();

            result.ThrowIfError();

            OutputHelper.WriteLine($"wearIndicator: {wearIndicator}");
        }

        [Fact]
        public async Task GetLifeCycleState()
        {
            await Start();

            (var result, var lifeCycleState) = await sut.SeGetLifeCycleStateAsync();

            result.ThrowIfError();

            switch (lifeCycleState)
            {
                case SeLifeCycleState.lcsNotInitialized:
                    OutputHelper.WriteLine($"lifeCycleState: {lifeCycleState},   lcsNotInitialized");
                    break;
                case SeLifeCycleState.lcsNoTime:
                    OutputHelper.WriteLine($"lifeCycleState: {lifeCycleState},   lcsNoTime");
                    break;
                case SeLifeCycleState.lcsActive:
                    OutputHelper.WriteLine($"lifeCycleState: {lifeCycleState},   lcsActive");
                    break;
                case SeLifeCycleState.lcsDeactivated:
                    OutputHelper.WriteLine($"lifeCycleState: {lifeCycleState},   lcsDeactivated");
                    break;
                case SeLifeCycleState.lcsDisabled:
                    OutputHelper.WriteLine($"lifeCycleState: {lifeCycleState},   lcsDisabled");
                    break;
                case SeLifeCycleState.lcsUnknown:
                default:
                    OutputHelper.WriteLine($"lifeCycleState: {lifeCycleState},   lcsUnknown");
                    break;
            }
        }

        [Fact]
        public async Task GetSupportedTransactionUpdateVariant()
        {
            await Start();

            (var result, var transactionUpdateVariant) = await sut.SeGetSupportedTransactionUpdateVariantAsync();

            result.ThrowIfError();

            switch (transactionUpdateVariant)
            {
                case SeUpdateVariant.signedUpdate:
                    OutputHelper.WriteLine($"transactionUpdateVariant: {transactionUpdateVariant},   signedUpdate");
                    break;
                case SeUpdateVariant.unsignedUpdate:
                    OutputHelper.WriteLine($"transactionUpdateVariant: {transactionUpdateVariant},   unsignedUpdate");
                    break;
                case SeUpdateVariant.signedAndUnsignedUpdate:
                    OutputHelper.WriteLine($"transactionUpdateVariant: {transactionUpdateVariant},   signedAndUnsignedUpdate");
                    break;
                default:
                    OutputHelper.WriteLine($"transactionUpdateVariant: {transactionUpdateVariant},   unknown");
                    break;
            }
        }

        [Fact]
        public async Task GetTimeSyncVariant()
        {
            await Start();

            (var result, var timeSyncVariant) = await sut.SeGetTimeSyncVariantAsync();

            result.ThrowIfError();

            switch (timeSyncVariant)
            {
                case SeSyncVariant.noInput:
                    OutputHelper.WriteLine($"timeSyncVariant: {timeSyncVariant},   noInput");
                    break;
                case SeSyncVariant.utcTime:
                    OutputHelper.WriteLine($"timeSyncVariant: {timeSyncVariant},   utcTime");
                    break;
                case SeSyncVariant.generalizedTime:
                    OutputHelper.WriteLine($"timeSyncVariant: {timeSyncVariant},   generalizedTime");
                    break;
                case SeSyncVariant.unixTime:
                    OutputHelper.WriteLine($"timeSyncVariant: {timeSyncVariant},   unixTime");
                    break;
                default:
                    OutputHelper.WriteLine($"timeSyncVariant: {timeSyncVariant},   unknown");
                    break;
            }
        }


        [Fact]
        public async Task Activate()
        {
            await sut.SeAuthenticateUserAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPin);

            (await sut.SeActivateAsync()).ThrowIfError();

            await sut.SeLogOutAsync(HardwareFixtures.AdminName);

            OutputHelper.WriteLine($"Activate");
        }

        [Fact]
        public async Task Initialize()
        {

            (var result, var lifeCycleState) = await sut.SeGetLifeCycleStateAsync();
            result.ThrowIfError();

            (lifeCycleState == SeLifeCycleState.lcsNotInitialized).Should().BeTrue();

            (await sut.SeInitializeAsync()).ThrowIfError();

            OutputHelper.WriteLine($"Initialize");
        }

        [Fact]
        public async Task UnblockUser()
        {
            SeResult result;
            SeAuthenticationResult authenticationResult;
            int remainingRetries;

            // start, without setting time
            await sut.SeStartAsync();

            // block timeadmin by using wrong pin
            do
            {
                var wrongPin = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
                (result, authenticationResult, remainingRetries) = await sut.SeAuthenticateUserAsync(HardwareFixtures.TimeAdminName, wrongPin);
                result.ThrowIfError();

            } while (remainingRetries >= 0 && authenticationResult != SeAuthenticationResult.authenticationPinIsBlocked);


            // login should fail
            (result, authenticationResult, remainingRetries) = await sut.SeAuthenticateUserAsync(HardwareFixtures.TimeAdminName, HardwareFixtures.TimeAdminPin);
            result.ThrowIfError();
            (authenticationResult == SeAuthenticationResult.authenticationPinIsBlocked).Should().BeTrue();


            // unblock
            (result, authenticationResult) = await sut.SeUnblockUserAsync(HardwareFixtures.TimeAdminName, HardwareFixtures.TimeAdminPuk, HardwareFixtures.TimeAdminPin);
            result.ThrowIfError();
            (authenticationResult == SeAuthenticationResult.authenticationOk).Should().BeTrue();

            // login should work
            (result, authenticationResult, remainingRetries) = await sut.SeAuthenticateUserAsync(HardwareFixtures.TimeAdminName, HardwareFixtures.TimeAdminPin);
            result.ThrowIfError();
            (authenticationResult == SeAuthenticationResult.authenticationOk).Should().BeTrue();

            //logout
            await sut.SeLogOutAsync(HardwareFixtures.TimeAdminName);

            OutputHelper.WriteLine($"UnblockUser");
        }

        [Fact(Skip = "Handle with Care, TSE will not be useable any more. Ensure you are using a developer-sample which can be factory-reseted.")]
        public async Task DisableSecureElement()
        {
            await sut.SeAuthenticateUserAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPin);

            (await sut.SeDisableSecureElementAsync()).ThrowIfError();

            await sut.SeLogOutAsync(HardwareFixtures.AdminName);

            OutputHelper.WriteLine($"DisableSecureElement");
        }

        [Fact]
        public async Task Deactivate()
        {
            await UpdateTime();

            await sut.SeAuthenticateUserAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPin);

            (await sut.SeDeactivateAsync()).ThrowIfError();

            await sut.SeLogOutAsync(HardwareFixtures.AdminName);

            OutputHelper.WriteLine($"Deactivate");
        }

        [Fact]
        public async Task ReadLogMessage()
        {
            await Start();

            (var result, var logMessageBytes) = await sut.SeReadLogMessageAsync();
            result.ThrowIfError();

            OutputHelper.WriteLine($"logMessage: {BitConverter.ToString(logMessageBytes)}");
        }

        [Fact]
        public async Task ExportData_All()
        {
            await Start();

            using (var stream = new MemoryStream())
            {

                (await sut.SeExportDataAsync(stream, string.Empty)).ThrowIfError();

                OutputHelper.WriteLine($"exportData: len:{stream.Length}");

                stream.Position = 0;
                using (var reader = ReaderFactory.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        OutputHelper.WriteLine($"{reader.Entry.Key}");
                    }
                }
            }
        }

        [Fact]
        public async Task GetStartTransactionLogMessageTimeStampFromTar()
        {
            await UpdateTime();

            // map ers
            (var mapResult, var mapSerialNumbersBytes) = await sut.SeExportSerialNumbersAsnyc();
            mapResult.ThrowIfError();

            var serialNumber = SerialNumberParser.GetSerialNumbers(mapSerialNumbersBytes)[0].SerialNumber;

            await sut.SeAuthenticateUserAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPin);

            mapResult = await sut.SeMapERStoKeyAsync(HardwareFixtures.ClientId, serialNumber);
            mapResult.ThrowIfError();

            await sut.SeLogOutAsync(HardwareFixtures.AdminName);

            // start some transaction
            (var result, var startTransactionResponse) = await sut.SeStartTransactionAsync(HardwareFixtures.ClientId, Array.Empty<byte>(), string.Empty);
            result.ThrowIfError();

            var startTransactionMoment = startTransactionResponse.LogUnixTime.ToDateTime();

            // add more salt
            await sut.SeUpdateTransactionAsync(HardwareFixtures.ClientId, startTransactionResponse.TransactionNumber, Array.Empty<byte>(), string.Empty);
            await sut.SeUpdateTransactionAsync(HardwareFixtures.ClientId, startTransactionResponse.TransactionNumber, Array.Empty<byte>(), string.Empty);
            await sut.SeUpdateTransactionAsync(HardwareFixtures.ClientId, startTransactionResponse.TransactionNumber, Array.Empty<byte>(), string.Empty);
            await sut.SeUpdateTransactionAsync(HardwareFixtures.ClientId, startTransactionResponse.TransactionNumber, Array.Empty<byte>(), string.Empty);
            await sut.SeUpdateTransactionAsync(HardwareFixtures.ClientId, startTransactionResponse.TransactionNumber, Array.Empty<byte>(), string.Empty);

            using (var ms = new MemoryStream())
            {
                (await sut.SeExportTransactionDataAsync(ms, startTransactionResponse.TransactionNumber)).ThrowIfError();

                ms.Position = 0;
                foreach (var logMessage in LogParser.GetLogsFromTarStream(ms))
                {
                    OutputHelper.WriteLine(logMessage.FileName);
                }

                ms.Position = 0;
                var firstLogMessageMoment =
                    LogParser.GetLogsFromTarStream(ms)
                    .OfType<TransactionLogMessage>()
                    .First(l => l.OperationType.ToLower().Contains("start"))
                    .LogTime;

                startTransactionMoment.Should().Be(firstLogMessageMoment);
            }
        }

        [Fact]
        public async Task InitializeFromFactoryReset()
        {
            // startup
            (var result, var deviceFirmwareId, var deviceUniqueId) = await sut.SeStartAsync();
            result.ThrowIfError();

            // get lifecycle state
            SeLifeCycleState lifeCycleState;
            (result, lifeCycleState) = await sut.SeGetLifeCycleStateAsync();
            result.ThrowIfError();

            // check for pins in transport states
            bool adminPinInTransportState;
            bool adminPukInTransportState;
            bool timeAdminPinInTransportState;
            bool timeAdminPukInTransportState;
            (result, adminPinInTransportState, adminPukInTransportState, timeAdminPinInTransportState, timeAdminPukInTransportState) = await sut.SeGetPinStatesAsync();
            result.ThrowIfError();
            adminPinInTransportState.Should().BeTrue();
            adminPukInTransportState.Should().BeTrue();
            timeAdminPinInTransportState.Should().BeTrue();
            timeAdminPukInTransportState.Should().BeTrue();

            // initialize pins / puks
            var releases = new List<string> { "240346", "425545", "793041" };

            bool isV1Hardware = releases.Any(release => deviceFirmwareId.Contains(release));

            result = await (isV1Hardware
                ? sut.SeInitializePinsAsync(HardwareFixtures.AdminPuk, HardwareFixtures.AdminPin, HardwareFixtures.TimeAdminPuk, HardwareFixtures.TimeAdminPin)
                : sut.SeInitializePinsAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPuk));

            result.ThrowIfError();

            // get lifecycle state
            (result, lifeCycleState) = await sut.SeGetLifeCycleStateAsync();
            result.ThrowIfError();
            lifeCycleState.Should().Be(SeLifeCycleState.lcsNotInitialized);

            // initialize secure element
            SeAuthenticationResult authenticationResult;
            (result, authenticationResult, _) = await sut.SeAuthenticateUserAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPin);
            result.ThrowIfError();
            authenticationResult.Should().Be(SeAuthenticationResult.authenticationOk);

            result = await sut.SeInitializeAsync();
            result.ThrowIfError();

            // get lifecycle state
            (result, lifeCycleState) = await sut.SeGetLifeCycleStateAsync();
            result.ThrowIfError();
            lifeCycleState.Should().Be(SeLifeCycleState.lcsNoTime);

            result = await sut.SeUpdateTimeAsync();
            result.ThrowIfError();

            // get lifecycle state
            (result, lifeCycleState) = await sut.SeGetLifeCycleStateAsync();
            result.ThrowIfError();
            lifeCycleState.Should().Be(SeLifeCycleState.lcsActive);

            // get serial numbers of keys
            byte[] serialNumbersBytes;
            (result, serialNumbersBytes) = await sut.SeExportSerialNumbersAsnyc();
            result.ThrowIfError();
            var serialNumbers = SerialNumberParser.GetSerialNumbers(serialNumbersBytes);
            var serialNumber = serialNumbers.First(s => s.IsUsedForTransactionLogs).SerialNumber;

            // register client
            result = await sut.SeMapERStoKeyAsync(HardwareFixtures.ClientId, serialNumber);

            result = await sut.SeLogOutAsync(HardwareFixtures.AdminName);
            result.ThrowIfError();

            await ExportData_All();
        }

        [Fact]
        public async Task CheckInitializedState()
        {
            //check for initialized pin states
            (var result, var adminPinInTransportState, var adminPukInTransportState, var timeAdminPinInTransportState, var timeAdminPukInTransportState) = await sut.SeGetPinStatesAsync();
            result.ThrowIfError();
            adminPinInTransportState.Should().BeFalse();
            adminPukInTransportState.Should().BeFalse();
            timeAdminPinInTransportState.Should().BeFalse();
            timeAdminPukInTransportState.Should().BeFalse();
        }

        [Fact]
        public async Task FullStack()
        {
            await Start();

            SeResult result;

            // get serial numbers of keys
            byte[] serialNumbersBytes;
            (result, serialNumbersBytes) = await sut.SeExportSerialNumbersAsnyc();
            result.ThrowIfError();
            var serialNumbers = SerialNumberParser.GetSerialNumbers(serialNumbersBytes);
            var serialNumber = serialNumbers.First(s => s.IsUsedForTransactionLogs).SerialNumber;

            OutputHelper.WriteLine(BitConverter.ToString(serialNumber));

            // authenticate time-admin
            (await sut.SeAuthenticateUserAsync(HardwareFixtures.TimeAdminName, HardwareFixtures.TimeAdminPin)).Item1.ThrowIfError();

            // set time
            (await sut.SeUpdateTimeAsync()).ThrowIfError();

            // logout time-admin
            (await sut.SeLogOutAsync(HardwareFixtures.TimeAdminName)).ThrowIfError();

            // get current mapping of clientid's
            byte[] ersMappingBytes;
            (result, ersMappingBytes) = await sut.SeGetERSMappingsAsync();
            var ersMappingList = ERSMappingHelper.ERSMappingsAsString(ersMappingBytes);


            if (ersMappingList.Contains(HardwareFixtures.ClientId) == false)
            {
                // authenticate admin
                (await sut.SeAuthenticateUserAsync(HardwareFixtures.AdminName, HardwareFixtures.AdminPin)).Item1.ThrowIfError();
                // register clientid
                //(await ProxyBase.SeMapERStoKeyAsync(HardwareFixtures.ClientId, ProxyBase.TseSerialNumber)).ThrowIfError();
                (await sut.SeMapERStoKeyAsync(HardwareFixtures.ClientId, serialNumber)).ThrowIfError();
                // logout admin
                (await sut.SeLogOutAsync(HardwareFixtures.AdminName)).ThrowIfError();
            }

            const string ProcessType = "Beleg";

            // start transaction
            SeStartTransactionResult startTransactionResult;
            (result, startTransactionResult) = await sut.SeStartTransactionAsync(HardwareFixtures.ClientId, Array.Empty<byte>(), string.Empty);
            result.ThrowIfError();
            OutputHelper.WriteLine($"startTransactionResult, transactionNumber: {startTransactionResult.TransactionNumber}, serialnumber: {startTransactionResult.SerialNumber.ToOctetString()}");
            OutputHelper.WriteLine($"logTimestamp:{startTransactionResult.LogUnixTime}, logDateTime:{startTransactionResult.LogUnixTime.ToDateTime():G}");
            OutputHelper.WriteLine($"signatureCounter: {startTransactionResult.SignatureCounter}, signatureValue: {Convert.ToBase64String(startTransactionResult.SignatureValue)}");

            // update transaction
            SeTransactionResult transactionResult;
            //(result, transactionResult) = await ProxyBase.SeUpdateTransactionAsync(HardwareFixtures.ClientId, startTransactionResult.TransactionNumber, Guid.NewGuid().ToByteArray(), $"update{DateTime.UtcNow.Ticks:X}");
            (result, transactionResult) = await sut.SeUpdateTransactionAsync(HardwareFixtures.ClientId, startTransactionResult.TransactionNumber, Encoding.UTF8.GetBytes("123456789098765434567898765434567890987654356789"), ProcessType);
            result.ThrowIfError();
            OutputHelper.WriteLine($"updateTransactionResult, transactionNumber: {startTransactionResult.TransactionNumber}, serialnumber: {transactionResult.SerialNumber.ToOctetString()}");
            OutputHelper.WriteLine($"logTimestamp:{transactionResult.LogUnixTime}, logDateTime:{transactionResult.LogUnixTime.ToDateTime():G}");
            OutputHelper.WriteLine($"signatureCounter: {transactionResult.SignatureCounter}, signatureValue: {Convert.ToBase64String(transactionResult.SignatureValue)}");

            // finish transaction
            //(result, transactionResult) = await ProxyBase.SeFinishTransactionAsync(HardwareFixtures.ClientId, startTransactionResult.TransactionNumber, Guid.NewGuid().ToByteArray(), $"finish{DateTime.UtcNow.Ticks:X}");
            (result, transactionResult) = await sut.SeFinishTransactionAsync(HardwareFixtures.ClientId, startTransactionResult.TransactionNumber, Encoding.UTF8.GetBytes("123456789098765434567898765434567890987654356789"), ProcessType);
            result.ThrowIfError();
            OutputHelper.WriteLine($"finishTransactionResult, transactionNumber: {startTransactionResult.TransactionNumber}, serialnumber: {transactionResult.SerialNumber.ToOctetString()}");
            OutputHelper.WriteLine($"logTimestamp:{transactionResult.LogUnixTime}, logDateTime:{transactionResult.LogUnixTime.ToDateTime():G}");
            OutputHelper.WriteLine($"signatureCounter: {transactionResult.SignatureCounter}, signatureValue: {Convert.ToBase64String(transactionResult.SignatureValue)}");

            byte[] publicKey;
            (result, publicKey) = await sut.SeExportPublicKeyAsync(serialNumber);
            result.ThrowIfError();
            OutputHelper.WriteLine($"serialnumberOctet: {serialNumber.ToOctetString()}, publicKeyBase64: {Convert.ToBase64String(publicKey)}");

            ulong certificateExpirationTimestamp;
            (result, certificateExpirationTimestamp) = await sut.SeGetCertificateExpirationDateAsync(serialNumber);
            result.ThrowIfError();
            OutputHelper.WriteLine($"serialnumberOctet: {serialNumber.ToOctetString()}, certificateExpirationTimestamp: {certificateExpirationTimestamp}, certificateExpirationDateTime: {certificateExpirationTimestamp.ToDateTime():G}");

            uint signatureCounter;
            (result, signatureCounter) = await sut.SeGetSignatureCounterAsync(serialNumber);
            result.ThrowIfError();
            OutputHelper.WriteLine($"serialnumberOctet: {serialNumber.ToOctetString()}, signatureCounter: {signatureCounter}");

            using (var ms = new MemoryStream())
            {
                (await sut.SeExportDataAsync(ms, null)).ThrowIfError();

                OutputHelper.WriteLine($"exportData: len:{ms.Length}");

                ms.Position = 0;
                using (var reader = ReaderFactory.Open(ms))
                {
                    while (reader.MoveToNextEntry())
                    {
                        OutputHelper.WriteLine($"{reader.Entry.Key}");
                    }
                }
            }
        }
    }
}
