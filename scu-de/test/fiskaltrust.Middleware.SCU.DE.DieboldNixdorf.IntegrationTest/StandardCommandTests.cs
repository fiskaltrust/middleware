using System;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.IntegrationTest
{
    public class StandardCommandTests : IDisposable
    {
        private readonly string _adminUserId = "1";
        private readonly string _adminPin = "12345";

        private readonly SerialPortCommunicationQueue _serialCommunicationProvder;
        private readonly TseCommunicationCommandHelper _tseCommunicationCommandHelper;
        private readonly AuthenticationTseCommandProvider _authenticationTseCommandProvider;
        private bool _disposed;

        public StandardCommandTests()
        {
            _serialCommunicationProvder = new SerialPortCommunicationQueue(Mock.Of<ILogger<SerialPortCommunicationQueue>>(), "COM3", 1500, 1500, true);
            _tseCommunicationCommandHelper = new TseCommunicationCommandHelper(Mock.Of<ILogger<TseCommunicationCommandHelper>>(), _serialCommunicationProvder, 1);
            _authenticationTseCommandProvider = new AuthenticationTseCommandProvider(Mock.Of<ILogger<AuthenticationTseCommandProvider>>(), _tseCommunicationCommandHelper);
            var standard = new StandardTseCommandsProvider(_tseCommunicationCommandHelper);
            standard.DisableAsb();
            var utilities = new UtilityTseCommandsProvider( _tseCommunicationCommandHelper);
            var sut = new MaintenanceTseCommandProvider(_tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());

            var timeUntilNextSelfTest = utilities.GetTimeUntilNextSelfTest();
            if (timeUntilNextSelfTest == 0)
            {
                var result = sut.RunSelfTest("DN TSEProduction ef82abcedf");
                _authenticationTseCommandProvider.ExecuteAuthorized(_adminUserId, _adminPin, () => sut.RegisterClient(TseCommunicationCommandHelper.ManagementClientId));
            }
        }

#if DEBUG
        [Fact]
#endif
        public void GetCountryInfo_ShouldReturnCorrectObject()
        {
            var sut = new StandardTseCommandsProvider(_tseCommunicationCommandHelper);
            var countryInfo = sut.GetCountryInfo();
            countryInfo.ApiMajorVersion.Should().Be(2);
            countryInfo.ApiMinorVersion.Should().Be(2);
            countryInfo.CountryId.Should().Be(23);
            countryInfo.HardwareId.Should().Be(DieboldNixdorfHardwareId.SingleTSE);
        }

#if DEBUG
        [Fact]
#endif
        public void GetMfcStatus_ShouldReturnCorrectObject()
        {
            var sut = new StandardTseCommandsProvider(_tseCommunicationCommandHelper);
            var mfcStatus = _tseCommunicationCommandHelper.GetMfcStatus();
            mfcStatus.MfcError.Should().BeEquivalentTo(new byte[] { 0, 0 });
            mfcStatus.MfcState.Should().Be(0b0000_0000);
        }

#if DEBUG
        [Fact]
#endif
        public void GetFirmwareInfo_ShouldReturnCorrectObject()
        {
            var sut = new StandardTseCommandsProvider(_tseCommunicationCommandHelper);
            var firmwareInfo = sut.GetFirmwareInfoForSingleTSE();
            firmwareInfo.FwMajorVersion.Should().Be(1);
            firmwareInfo.FwMinorVersion.Should().Be(3);
            firmwareInfo.FwBuildNo.Should().Be(497);
            firmwareInfo.LdrMajorVersion.Should().Be(1);
            firmwareInfo.LdrMinorVersion.Should().Be(1);
            firmwareInfo.LdrBuildNo.Should().Be(497);
        }

#if DEBUG
        [Fact]
#endif
        public void SetASB_ON_ShouldReturnCorrectObject()
        {
            var sut = new StandardTseCommandsProvider(_tseCommunicationCommandHelper);
            sut.DisableAsb();
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

