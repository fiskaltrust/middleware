using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using SharpCompress.Readers;
using Xunit;
using Xunit.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using static fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.NativeFunctionPointer;
using System.IO;
using fiskaltrust.ifPOS.v1.de;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Exceptions;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.IntegrationTest
{
    public class SwissbitProxyTests : IClassFixture<SwissbitHardwareFixture>, IDisposable
    {
        private readonly INativeFunctionPointerFactory _nativeFunctionPointerFactory = new FunctionPointerFactory();
        private readonly SwissbitHardwareFixture _hardwareFixtures;
        private readonly ITestOutputHelper _outputHelper;

        public SwissbitProxyTests(ITestOutputHelper outputHelper, SwissbitHardwareFixture hardwareFixtures)
        {
            _outputHelper = outputHelper;
            _hardwareFixtures = hardwareFixtures;

            sut = new SwissbitProxy(_hardwareFixtures.MountPoint, _hardwareFixtures.AdminPin, _hardwareFixtures.TimeAdminPin, _nativeFunctionPointerFactory, new LockingHelper(NullLogger<LockingHelper>.Instance), NullLogger.Instance);
            sut.InitAsync().Wait();
        }

        private readonly ISwissbitProxy sut;

        public void Dispose()
        {
            sut?.Dispose();
        }


#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task Instantiate()
        {
            _outputHelper.WriteLine(await sut.GetVersionAsync());
            _outputHelper.WriteLine(await sut.GetLogTimeFormatAsync());
            _outputHelper.WriteLine(await sut.GetSignatureAlgorithmAsync());
            _outputHelper.WriteLine(JsonConvert.SerializeObject(await sut.GetTseStatusAsync()));
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task GetVersion()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            _outputHelper.WriteLine(await sut.GetVersionAsync());
            _outputHelper.WriteLine(await sut.GetSignatureAlgorithmAsync());
            _outputHelper.WriteLine(await sut.GetLogTimeFormatAsync());
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task SetupTse()
        {
            var status = await sut.GetTseStatusAsync();

            status.initializationState
                .Should().Be(
                Interop.NativeFunctionPointer.WormInitializationState.WORM_INIT_UNINITIALIZED,
                "Swissbit TSE need to be in factory reset state");

            await sut.TseSetupAsync(_hardwareFixtures.Seed, _hardwareFixtures.AdminPuk, _hardwareFixtures.AdminPin, _hardwareFixtures.TimeAdminPin);


            status = await sut.GetTseStatusAsync();
            status.initializationState
                .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED);
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task ClientRegisterUnregister()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
                .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
                "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            List<string> clientList;
            var clientId = Helpers.RandomStringHelper.RandomString(20);
            clientList = await sut.TseGetRegisteredClientsAsync();
            clientList.Should().NotContain(clientId);

            await sut.TseRegisterClientAsync(clientId);
            clientList = await sut.TseGetRegisteredClientsAsync();
            clientList.Should().Contain(clientId);

            await sut.TseDeregisterClientAsync(clientId);
            clientList = await sut.TseGetRegisteredClientsAsync();
            clientList.Should().NotContain(clientId);
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task UpdateTime()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
                .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
                "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");


            await sut.TseUpdateTimeAsync();

            status = await sut.GetTseStatusAsync();
            status.HasValidTime.Should().Be(true);
        }


#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task Decommission()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
                .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
                "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");


            await sut.TseDecommissionAsync();

            status = await sut.GetTseStatusAsync();
            status.initializationState.Should().Be(WormInitializationState.WORM_INIT_DECOMMISSIONED,
                "Swissbit TSE need to be in WORM_INIT_DECOMMISSIONED state");

        }


#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task Transaction()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }
            await sut.TseRegisterClientAsync(_hardwareFixtures.ClientId);

            status.initializationState
                .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
                "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            var startProcessType = string.Empty;
            var startProcessData = Array.Empty<byte>();
            var startResponse = await sut.TransactionStartAsync(_hardwareFixtures.ClientId, startProcessData, startProcessType);
            _outputHelper.WriteLine($"transactionStart, TransactionNumber: {startResponse.TransactionNumber}, LogTime: {startResponse.LogTime}, SignatureCounter: {startResponse.SignatureCounter}, Signature: {startResponse.SignatureBase64}, SerialNumber: {startResponse.SerialNumber.ToOctetString()}");

            var updateProcessType = string.Empty;
            var updateProcessData = Array.Empty<byte>();
            var updateResponse = await sut.TransactionUpdateAsync(_hardwareFixtures.ClientId, startResponse.TransactionNumber, updateProcessData, updateProcessType);
            _outputHelper.WriteLine($"transactionUpdate, TransactionNumber: {updateResponse.TransactionNumber}, LogTime: {updateResponse.LogTime}, SignatureCounter: {updateResponse.SignatureCounter}, Signature: {updateResponse.SignatureBase64}, SerialNumber: {updateResponse.SerialNumber.ToOctetString()}");
            updateResponse.TransactionNumber.Should().Be(startResponse.TransactionNumber);
            updateResponse.SignatureCounter.Should().Be(startResponse.SignatureCounter + 1);

            var finishProcessType = "Kassenbeleg-V1";
            var finishPrcessData = System.Text.Encoding.ASCII.GetBytes("AVRechnung^0,00_84,20_0,00_0,00_0,00^84,20:Bar");
            var finishResponse = await sut.TransactionFinishAsync(_hardwareFixtures.ClientId, startResponse.TransactionNumber, finishPrcessData, finishProcessType);
            _outputHelper.WriteLine($"transactionFinish, TransactionNumber: {finishResponse.TransactionNumber}, LogTime: {finishResponse.LogTime}, SignatureCounter: {finishResponse.SignatureCounter}, Signature: {finishResponse.SignatureBase64}, SerialNumber: {finishResponse.SerialNumber.ToOctetString()}");
            finishResponse.TransactionNumber.Should().Be(startResponse.TransactionNumber);
            finishResponse.SignatureCounter.Should().Be(updateResponse.SignatureCounter + 1);
        }


#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task TransactionX100()
        {
            var n = 10000;

            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }
            await sut.TseRegisterClientAsync(_hardwareFixtures.ClientId);

            status.initializationState
                .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
                "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");
            for (var i = 0; i < n; i++)
            {
                var startProcessType = string.Empty;
                var startProcessData = Array.Empty<byte>();
                var startResponse = await sut.TransactionStartAsync(_hardwareFixtures.ClientId, startProcessData, startProcessType);
                _outputHelper.WriteLine($"transactionStart, TransactionNumber: {startResponse.TransactionNumber}, LogTime: {startResponse.LogTime}, SignatureCounter: {startResponse.SignatureCounter}, Signature: {startResponse.SignatureBase64}, SerialNumber: {startResponse.SerialNumber.ToOctetString()}");

                var updateProcessType = string.Empty;
                var updateProcessData = Array.Empty<byte>();
                var updateResponse = await sut.TransactionUpdateAsync(_hardwareFixtures.ClientId, startResponse.TransactionNumber, updateProcessData, updateProcessType);
                _outputHelper.WriteLine($"transactionUpdate, TransactionNumber: {updateResponse.TransactionNumber}, LogTime: {updateResponse.LogTime}, SignatureCounter: {updateResponse.SignatureCounter}, Signature: {updateResponse.SignatureBase64}, SerialNumber: {updateResponse.SerialNumber.ToOctetString()}");
                updateResponse.TransactionNumber.Should().Be(startResponse.TransactionNumber);
                updateResponse.SignatureCounter.Should().Be(startResponse.SignatureCounter + 1);

                var finishProcessType = "Kassenbeleg-V1";
                var finishPrcessData = System.Text.Encoding.ASCII.GetBytes("AVRechnung^0,00_84,20_0,00_0,00_0,00^84,20:Bar");
                var finishResponse = await sut.TransactionFinishAsync(_hardwareFixtures.ClientId, startResponse.TransactionNumber, finishPrcessData, finishProcessType);
                _outputHelper.WriteLine($"transactionFinish, TransactionNumber: {finishResponse.TransactionNumber}, LogTime: {finishResponse.LogTime}, SignatureCounter: {finishResponse.SignatureCounter}, Signature: {finishResponse.SignatureBase64}, SerialNumber: {finishResponse.SerialNumber.ToOctetString()}");
                finishResponse.TransactionNumber.Should().Be(startResponse.TransactionNumber);
                finishResponse.SignatureCounter.Should().Be(updateResponse.SignatureCounter + 1);
            }
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task ExportTarStream()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            using (var ms = new MemoryStream())
            {
                await sut.ExportTarAsync(ms);

                ms.Length.Should().BeGreaterThan(0);

                File.WriteAllBytes("test.tar", ms.ToArray());
                ms.Position = 0;
                using (var reader = ReaderFactory.Open(ms))
                {
                    while (reader.MoveToNextEntry())
                    {
                        _outputHelper.WriteLine($"{reader.Entry.Key}");
                    }
                }

                _outputHelper.WriteLine($"{Convert.ToBase64String(ms.ToArray())}");
            }
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task GetTseStatus()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");


            _outputHelper.WriteLine($"LogTimeFormat: {await sut.GetLogTimeFormatAsync()}");
            _outputHelper.WriteLine($"SignatureAlgorithm: {await sut.GetSignatureAlgorithmAsync()}");
            _outputHelper.WriteLine($"Version: {await sut.GetVersionAsync()}");



            status = await sut.GetTseStatusAsync();
            _outputHelper.WriteLine(JsonConvert.SerializeObject(status));

            var serialNumberBytes = status.TseSerialNumber;
            _outputHelper.WriteLine($"SerialNumber: {BitConverter.ToString(serialNumberBytes)}");

            var publicKeyBytes = status.TsePublicKey;
            _outputHelper.WriteLine($"PublicKey: {BitConverter.ToString(publicKeyBytes)}");

            var sha384 = new SHA384Managed();
            _outputHelper.WriteLine($"SHA384(PublicKey): {BitConverter.ToString(sha384.ComputeHash(publicKeyBytes))}");

            var sha256 = new SHA256Managed();
            _outputHelper.WriteLine($"SHA256(PublicKey): {BitConverter.ToString(sha256.ComputeHash(publicKeyBytes))}");

        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task GetCertificate()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            var certificateBytes = await sut.GetLogMessageCertificateAsync();

            {

                var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificateBytes);

                _outputHelper.WriteLine($"Subject: {cert.Subject}");
                _outputHelper.WriteLine($"Issuer: {cert.Issuer}");

                _outputHelper.WriteLine($"SerialNumber: {BitConverter.ToString(cert.GetSerialNumber())}");
                _outputHelper.WriteLine($"SerialNumberBase64: {Convert.ToBase64String(cert.GetSerialNumber())}");
                _outputHelper.WriteLine($"PublicKey: {BitConverter.ToString(cert.GetPublicKey())}");
                _outputHelper.WriteLine($"PublicKeyBase64: {Convert.ToBase64String(cert.GetPublicKey())}");
            }

            {

                var parser = new Org.BouncyCastle.X509.X509CertificateParser();
                foreach (Org.BouncyCastle.X509.X509Certificate item in parser.ReadCertificates(certificateBytes))
                {
                    _outputHelper.WriteLine($"Subject: {item.SubjectDN.ToString()}");
                    _outputHelper.WriteLine($"Issuer: {item.IssuerDN.ToString()}");

                    _outputHelper.WriteLine($"SerialNumber: {BitConverter.ToString(item.SerialNumber.ToByteArray())}");
                    _outputHelper.WriteLine($"SerialNumberBase64: {Convert.ToBase64String(item.SerialNumber.ToByteArray())}");

                }
            }
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task ExportTarFilteredTimeStream()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(
               WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            using (var ms = new MemoryStream())
            {
                Int64 timeInfinite = -1;
                await sut.ExportTarFilteredTimeAsync(ms, 0, (UInt64) timeInfinite, string.Empty);
                _outputHelper.WriteLine($"{Convert.ToBase64String(ms.ToArray())}");

                ms.Length.Should().BeGreaterThan(0);
            }
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task ExportTarFilteredTransactionStream()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(
               WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            using (var ms = new MemoryStream())
            {
                Int64 transactionInfinite = -1;
                await sut.ExportTarFilteredTimeAsync(ms, 0, (UInt64) transactionInfinite, string.Empty);
                _outputHelper.WriteLine($"{Convert.ToBase64String(ms.ToArray())}");

                ms.Length.Should().BeGreaterThan(0);
            }
        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task AdminLoginLogout()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(
               WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            await sut.UserLoginAsync(WormUserId.WORM_USER_ADMIN, _hardwareFixtures.AdminPin);

            await sut.UserLogoutAsync(WormUserId.WORM_USER_ADMIN);

            //logout of not logged in user should fail.
            var ex = await Assert.ThrowsAsync<SwissbitException>(() => sut.UserLogoutAsync(WormUserId.WORM_USER_ADMIN));
            ex.Message.Should().Be("Given user is not authenticated. ");

        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task DeleteStoredDataFail()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(
               WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            //do a transaction
            await Transaction();

            //delete data should fail when not authenticated as admin
            var exAuthentication = await Assert.ThrowsAsync<SwissbitException>(() => sut.DeleteStoredDataAsync());
            exAuthentication.Message.Should().Be("Not authorized. ");

            await sut.UserLoginAsync(WormUserId.WORM_USER_ADMIN, _hardwareFixtures.AdminPin);

            //delete data should fail when no export is done before
            var exNoExport = await Assert.ThrowsAsync<SwissbitException>(() => sut.DeleteStoredDataAsync());
            exNoExport.Message.Should().Be("Failed to delete, data not completely exported. ");

            await sut.UserLogoutAsync(WormUserId.WORM_USER_ADMIN);

        }

#if DEBUG
        [Fact]
#endif
        [Trait("Category", "SkipWhenLiveUnitTesting")]
        public async Task DeleteStoredData()
        {
            var status = await sut.GetTseStatusAsync();
            if (status.HasPassedSelfTest == false)
            {
                await sut.TseRunSelfTestAsnyc();
            }
            if (status.HasValidTime == false)
            {
                await sut.TseUpdateTimeAsync();
            }

            status.initializationState
               .Should().Be(
               WormInitializationState.WORM_INIT_INITIALIZED,
               "Swissbit TSE need to be in WORM_INIT_INITIALIZED state");

            //do a transaction
            await Transaction();

            //login
            await sut.UserLoginAsync(WormUserId.WORM_USER_ADMIN, _hardwareFixtures.AdminPin);

            //do export1
            long len1 = 0;
            using (var ms = new MemoryStream())
            {
                await sut.ExportTarAsync(ms);
                len1 = ms.Length;
            }


            //delete data
            await sut.DeleteStoredDataAsync();

            //do export 2
            long len2 = 0;
            using (var ms = new MemoryStream())
            {
                await sut.ExportTarAsync(ms);
                len2 = ms.Length;
            }

            //export 2 will be smaler than export 1
            len2.Should().BeLessThan(len1);

            //logout
            await sut.UserLogoutAsync(WormUserId.WORM_USER_ADMIN);
        }
    }
}