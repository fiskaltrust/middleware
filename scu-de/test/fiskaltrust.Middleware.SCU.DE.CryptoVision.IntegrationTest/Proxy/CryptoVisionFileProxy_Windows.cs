using System;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.IntegrationTest
{
    public class CryptoVisionFileProxy_Windows : CryptoVisionProxyTests, IClassFixture<HardwareFixtures>, IDisposable
    {
        public CryptoVisionFileProxy_Windows(ITestOutputHelper outputHelper, HardwareFixtures hardwareFixtures)
        {
            OutputHelper = outputHelper;
            HardwareFixtures = hardwareFixtures;


            var config = new CryptoVisionConfiguration
            {
                DevicePath = hardwareFixtures.MountPoint,
                TseIOTimeout = 5000
            };
            transportAdapter = new MassStorageClassTransportAdapter(Mock.Of<ILogger<MassStorageClassTransportAdapter>>(), new WindowsFileIo(), config);
            sut = new CryptoVisionFileProxy(transportAdapter);
        }

        private readonly MassStorageClassTransportAdapter transportAdapter;

        public void Dispose()
        {
            transportAdapter?.Dispose();
        }
    }
}
