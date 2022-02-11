using System;
using Castle.Core.Logging;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.IntegrationTest
{
    public class MaintenanceCommandTests : IDisposable
    {
        private readonly string _adminUserId = "1";
        private readonly string _adminPin = "12345";

        private readonly SerialPortCommunicationQueue _serialCommunicationProvder;
        private readonly TseCommunicationCommandHelper _tseCommunicationCommandHelper;
        private readonly AuthenticationTseCommandProvider _authenticationTseCommandProvider;
        private bool _disposed;

        public MaintenanceCommandTests()
        {
            _serialCommunicationProvder = new SerialPortCommunicationQueue(Mock.Of<ILogger<SerialPortCommunicationQueue>>(), "COM3", 1500, 1500, true);
            _tseCommunicationCommandHelper = new TseCommunicationCommandHelper(Mock.Of<ILogger<TseCommunicationCommandHelper>>(), _serialCommunicationProvder, 1);
            _authenticationTseCommandProvider = new AuthenticationTseCommandProvider(Mock.Of<ILogger<AuthenticationTseCommandProvider>>(), _tseCommunicationCommandHelper);
            var maintenance = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            var utilities = new UtilityTseCommandsProvider(_tseCommunicationCommandHelper);
            var timeUntilNextSelfTest = utilities.GetTimeUntilNextSelfTest();
            if (timeUntilNextSelfTest == 0)
            {
                var result = maintenance.RunSelfTest(TseCommunicationCommandHelper.DieboldNixdorfDefaultClientId);
            }

            var standard = new StandardTseCommandsProvider(_tseCommunicationCommandHelper);
            standard.DisableAsb();

            _authenticationTseCommandProvider.ExecuteAuthorized(_adminUserId, _adminPin, () =>
            {
                var maintenanceTseCommandProvider = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
                maintenanceTseCommandProvider.RegisterClient(TseCommunicationCommandHelper.ManagementClientId);

                var mfcStatus = _tseCommunicationCommandHelper.GetMfcStatus();
                if (mfcStatus.IsInitialized)
                {
                    maintenanceTseCommandProvider.Initialize("");
                }

                maintenanceTseCommandProvider.UpdateTime();
            });
        }

#if DEBUG
        [Fact]
#endif
        public void Initialize_ShouldInitializeTseAndSetMfcStatus()
        {
            TestHelpers.PerformResetWithSelfTest(_serialCommunicationProvder);
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            _authenticationTseCommandProvider.ExecuteAuthorized(_adminUserId, _adminPin, () => sut.Initialize(""));

            var mfcStatus = _tseCommunicationCommandHelper.GetMfcStatus();
            mfcStatus.MfcError.Should().BeEquivalentTo(new byte[] { 0, 0 });

            mfcStatus.IsInitialized.Should().BeTrue();
        }

#if DEBUG
        [Fact]
#endif
        public void GetMemoryInfo_ShouldReturnCorrectObject()
        {
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            var memoryInfo = sut.GetMemoryInfo();
            memoryInfo.Capacity.Should().BeGreaterThan(1);
            memoryInfo.FreeSpace.Should().BeLessOrEqualTo(memoryInfo.Capacity);
        }

#if DEBUG
        [Fact]
#endif
        public void Disable_ShouldChangeTSEState()
        {
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            _authenticationTseCommandProvider.ExecuteAuthorized(_adminUserId, _adminPin, () => sut.Disable());
            var mfcStatus = _tseCommunicationCommandHelper.GetMfcStatus();
            mfcStatus.MfcError.Should().BeEquivalentTo(new byte[] { 0, 0 });
            mfcStatus.IsInitialized.Should().BeFalse();

            TestHelpers.PerformResetWithSelfTest(_serialCommunicationProvder);
        }

#if DEBUG
        [Fact]
#endif
        public void RegisteredClient_ShouldReturnRegisteredClients()
        {
            var utilityTseCommandsProvider = new UtilityTseCommandsProvider(_tseCommunicationCommandHelper);
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            _authenticationTseCommandProvider.ExecuteAuthorized(_adminUserId, _adminPin, () =>
            {
                var registeredClients = utilityTseCommandsProvider.GetRegisteredClients();
                registeredClients.Should().Contain("DN TSEProduction ef82abcedf");
                sut.RegisterClient("POS001");
                registeredClients = utilityTseCommandsProvider.GetRegisteredClients();
                registeredClients.Should().Contain("DN TSEProduction ef82abcedf");
                registeredClients.Should().Contain("POS001");
            });
        }

#if DEBUG
        [Fact]
#endif
        public void GetSlotInfo_ShouldReturnCorrectObject()
        {
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            var slotInfo = sut.GetSlotInfo();

            (slotInfo.SlotStatus & (SlotStates.TseInserted | SlotStates.OperationalMode)).Should().Be(SlotStates.TseInserted | SlotStates.OperationalMode);
            (slotInfo.TseStatus & SlotTseStates.Initialized).Should().Be(SlotTseStates.Initialized);
            slotInfo.CryptoVendor.Should().Be(0);
            slotInfo.CryptoInfo.Should().Be("0001000200010002");
            slotInfo.Capacity.Should().Be(6979321856);
            slotInfo.FreeSpace.Should().BeGreaterThan(1000);
            slotInfo.CertExpDate.Should().BeAfter(DateTime.Today);
            slotInfo.AvailSig.Should().BeGreaterThan(19999000);
            slotInfo.SigAlgorithm.Should().Be("ecdsa-plain-SHA384");
            slotInfo.CryptoFwType.Should().Be(1);
        }

#if DEBUG
        [Fact]
#endif
        public void RunSelfTest_ShouldSucceed()
        {
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            var selfTestResult = sut.RunSelfTest();

            selfTestResult.Should().Be(SelfTestResult.Ok);
        }

#if DEBUG
        [Fact]
#endif        
        public void GetRegisteredClients_ShouldReturnRegisteredClients()
        {
            var utilityTseCommandsProvider = new UtilityTseCommandsProvider(_tseCommunicationCommandHelper);
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            _authenticationTseCommandProvider.ExecuteAuthorized(_adminUserId, _adminPin, () =>
            {
                var registeredClients = utilityTseCommandsProvider.GetRegisteredClients();
                registeredClients.Should().Contain("DN TSEProduction ef82abcedf");
                sut.RegisterClient("POS001");
                registeredClients = utilityTseCommandsProvider.GetRegisteredClients();
                registeredClients.Should().Contain("DN TSEProduction ef82abcedf");
                registeredClients.Should().Contain("POS001");
            });
        }

#if DEBUG
        [Fact]
#endif
        public void GetStartedTransactions_ShouldReturnStartedTransactions()
        {
            var utilityTseCommandsProvider = new UtilityTseCommandsProvider(_tseCommunicationCommandHelper);
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());

            var startedTransactions = utilityTseCommandsProvider.GetStartedTransactions();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _serialCommunicationProvder.Dispose();
                _disposed = true;
            }
        }
    }
}